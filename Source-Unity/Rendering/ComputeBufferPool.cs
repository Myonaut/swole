using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Pool;

namespace Swole.Unity
{

    public class ComputeBufferPool : SingletonBehaviour<ComputeBufferPool>
    {

        public override bool DestroyOnLoad => false;

        public override bool ExecuteInStack => true;

        /// <summary>
        /// The structural description of a specific buffer configuration
        /// </summary>
        public struct BufferDescriptor
        {
            public GraphicsBuffer.Target Target;
            public int Capacity;
            public int Stride;

            public BufferDescriptor(GraphicsBuffer.Target target, int capacity, int stride)
            {
                Target = target;
                Capacity = capacity;
                Stride = stride;
            }
        }

        // A fast, zero-allocation custom comparer that guarantees no boxing happens during lookups
        private class DescriptorComparer : IEqualityComparer<BufferDescriptor>
        {
            public bool Equals(BufferDescriptor x, BufferDescriptor y)
            {
                return x.Target == y.Target && x.Capacity == y.Capacity && x.Stride == y.Stride;
            }

            public int GetHashCode(BufferDescriptor obj)
            {
                // Avoids modern HashCode.Combine if it does internal structural allocations
                unchecked
                {
                    int hash = 17;
                    hash = hash * 23 + (int)obj.Target;
                    hash = hash * 23 + obj.Capacity;
                    hash = hash * 23 + obj.Stride;
                    return hash;
                }
            }
        }

        /// <summary>
        /// A metadata wrapper to support age-based cleanup and expiration
        /// </summary>
        private class PooledBufferEntry
        {
            public GraphicsBuffer Buffer { get; set; }
            public float LastReturnedTime { get; set; }
        }

        [Header("Sweeper Configurations")]
        [Tooltip("How long an inactive buffer can stay in VRAM before getting destroyed (in seconds).")]
        [SerializeField] private float maxIdleLifetime = 15f;
        [Tooltip("Time interval between active pool scans (in seconds).")]
        [SerializeField] private float sweepInterval = 5f;

        private float nextSweepTime = 0f; 

        /// <summary>
        /// Registry tracking available inactive buffers grouped by their descriptors
        /// </summary>
        private Dictionary<BufferDescriptor, List<PooledBufferEntry>> pool = new Dictionary<BufferDescriptor, List<PooledBufferEntry>>();

        /// <summary>
        /// Reverse lookup to track the configuration signatures of active leases
        /// </summary>
        private Dictionary<GraphicsBuffer, BufferDescriptor> activeLeases = new Dictionary<GraphicsBuffer, BufferDescriptor>();

        protected override void OnAwake()
        {
            base.OnAwake();
            // Passing the custom comparer instance here is the secret to 0-allocation lookups
            pool = new Dictionary<BufferDescriptor, List<PooledBufferEntry>>(new DescriptorComparer());
        }

        /// <summary>
        /// Requests a buffer. Searches for an exact match or an available, larger approximate match. Be aware of buffer count when using approximate matches.
        /// </summary>
        public GraphicsBuffer GetBufferLocal(GraphicsBuffer.Target target, int count, int stride, bool allowApproximateMatch = true)
        {
            if (count <= 0) count = 1;

            BufferDescriptor bestKey = new BufferDescriptor(target, count, stride);
            bool foundMatch = false;

            if (allowApproximateMatch)
            {
                int bestFitDelta = int.MaxValue;

                var enumerator = pool.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var kvp = enumerator.Current;
                    var desc = kvp.Key;
                    if (desc.Target == target && desc.Stride == stride && kvp.Value.Count > 0)
                    {
                        int delta = desc.Capacity - count;
                        if (delta >= 0 && delta < bestFitDelta && delta <= count)
                        {
                            bestFitDelta = delta;
                            bestKey = desc;
                            foundMatch = true;
                        }
                    }
                }
                enumerator.Dispose(); // Struct-based clean up costs 0 bytes
            }
            else
            {
                foundMatch = pool.TryGetValue(bestKey, out var checkList) && checkList.Count > 0;
            }

            if (foundMatch && pool.TryGetValue(bestKey, out var bufferList) && bufferList.Count > 0)
            {
                int lastIndex = bufferList.Count - 1;
                GraphicsBuffer pooledBuffer = bufferList[lastIndex].Buffer;
                bufferList.RemoveAt(lastIndex);

                activeLeases[pooledBuffer] = bestKey;
                return pooledBuffer;
            }

            GraphicsBuffer newBuffer = new GraphicsBuffer(target, count, stride);
            activeLeases[newBuffer] = bestKey;
            return newBuffer;
        }

        public static GraphicsBuffer GetBuffer(GraphicsBuffer.Target target, int count, int stride, bool allowApproximateMatch = true)
        {
            var instance = Instance;
            if (instance == null) return null;

            return instance.GetBufferLocal(target, count, stride, allowApproximateMatch);
        }

        /// <summary>
        /// Checks a leased buffer back into its cache slot and logs its timestamp.
        /// </summary>
        public void ReturnBufferLocal(GraphicsBuffer buffer)
        {
            if (buffer == null) return;

            if (activeLeases.TryGetValue(buffer, out var desc))
            {
                activeLeases.Remove(buffer);

                if (!pool.TryGetValue(desc, out var bufferList))
                {
                    bufferList = new List<PooledBufferEntry>();
                    pool[desc] = bufferList;
                }

                bufferList.Add(new PooledBufferEntry
                {
                    Buffer = buffer,
                    LastReturnedTime = Time.time
                });
            }
            else
            {
                Debug.LogError("[BufferPool] Returned an unregistered buffer instance.");
                buffer.Release();
            }
        }

        public static void ReturnBuffer(GraphicsBuffer buffer)
        {
            var instance = InstanceOrNull;
            if (instance == null) return;

            instance.ReturnBufferLocal(buffer); 
        }

        /// <summary>
        /// Hooked into your custom SingletonBehaviour framework engine updates.
        /// </summary>
        public override void OnUpdate()
        {
            if (Time.time < nextSweepTime) return;
            nextSweepTime = Time.time + sweepInterval;

            SweepExpiredBuffers();
        }
        public override void OnLateUpdate()
        {
        }
        public override void OnFixedUpdate()
        {
        }

        /// <summary>
        /// Scans the tracking collection, destroying resources that have been idle for too long.
        /// </summary>
        private void SweepExpiredBuffers()
        {
            float currentTime = Time.time;
            int destroyedCount = 0;

            // Allocation-Free dictionary iteration for the sweeper loop too
            var enumerator = pool.GetEnumerator();
            while (enumerator.MoveNext())
            {
                List<PooledBufferEntry> entries = enumerator.Current.Value;

                for (int i = entries.Count - 1; i >= 0; i--)
                {
                    if (currentTime - entries[i].LastReturnedTime > maxIdleLifetime)
                    {
                        entries[i].Buffer?.Release();
                        entries.RemoveAt(i);
                        destroyedCount++;
                    }
                }
            }
            enumerator.Dispose();

#if UNITY_EDITOR
            if (destroyedCount > 0)
            {
                Debug.Log($"[BufferPool Sweeper] Successfully pruned {destroyedCount} inactive compute structures from VRAM.");
            }
#endif
        }

        public override void OnDestroyed()
        {
            base.OnDestroyed();

            // Release all available inactive cache layers on shutdown
            var enumerator = pool.GetEnumerator();
            while (enumerator.MoveNext())
            {
                foreach (var entry in enumerator.Current.Value)
                {
                    entry.Buffer?.Release();
                }
            }
            enumerator.Dispose();
            pool.Clear();

            foreach (var buffer in activeLeases.Keys)
            {
                buffer?.Release();
            }
            activeLeases.Clear();
        }
    }

    public struct ComputeBufferLeaseScope : IDisposable
    {
        // A pooled list instance that costs 0 bytes of GC allocation to fetch
        private List<GraphicsBuffer> leasedBuffers; 

        public GraphicsBuffer Require(GraphicsBuffer.Target target, int count, int stride, bool allowApproximate = true) 
        {
            // Get an existing list from Unity's global static list pool instead of calling 'new'
            if (leasedBuffers == null)
                leasedBuffers = ListPool<GraphicsBuffer>.Get();

            GraphicsBuffer buffer = ComputeBufferPool.GetBuffer(target, count, stride, allowApproximate); 
            if (buffer != null) leasedBuffers.Add(buffer);
            return buffer;
        }

        public void Dispose()
        {
            if (leasedBuffers != null)
            {
                // 1. Return all tracked buffers back to your global ComputeBuffer pool
                for (int i = 0; i < leasedBuffers.Count; i++)
                {
                    ComputeBufferPool.ReturnBuffer(leasedBuffers[i]);
                }

                // 2. Clear the list and safely return it back to Unity's global ListPool
                ListPool<GraphicsBuffer>.Release(leasedBuffers);
                leasedBuffers = null;
            }
        }
    }

}