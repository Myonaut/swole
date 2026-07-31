using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Swole
{

    public delegate void SwapBufferDelegate(ComputeBuffer activeBuffer);

    public interface IComputeBufferPool : IDisposable
    {
        public string Name { get; }

#if UNITY_EDITOR
        public string Stacktrace { get; }
#endif

        public void ListenForBufferSwap(SwapBufferDelegate listener);
        public void StopListeningForBufferSwap(SwapBufferDelegate listener);
        public void SetActiveBuffer(int bufferIndex);
        public bool IsValid();

        public void Upload();

        public ComputeBuffer ActiveBuffer { get; }
        public int Size { get; }
        public int Stride { get; }
        public int BufferPoolSize { get; }
    }
    public interface IDynamicComputeBufferPool : IComputeBufferPool
    {
        public void SetSize(int size, int updateActiveBufferFrameDelay);
    }

    public delegate void WriteToBufferDataDelegate<T>(NativeArray<T> bufferData) where T : unmanaged;
    public delegate void WriteToBufferDataFromStartIndexDelegate<T>(NativeArray<T> bufferData, int startIndex) where T : unmanaged;
    public class DynamicComputeBufferPool<T> : IDynamicComputeBufferPool where T : unmanaged
    {

        protected readonly List<SwapBufferDelegate> swapBufferListeners = new List<SwapBufferDelegate>();

        public void ListenForBufferSwap(SwapBufferDelegate listener)
        {
            swapBufferListeners.Add(listener);
        }
        public void StopListeningForBufferSwap(SwapBufferDelegate listener)
        {
            swapBufferListeners.Remove(listener);
        }

        protected ComputeBufferType bufferType;
        public ComputeBufferType BufferType => bufferType;

        protected ComputeBufferMode bufferMode;
        public ComputeBufferMode BufferMode => bufferMode;

        protected int stride;
        public int Stride => stride;

        protected int activeBuffer;
        protected int writeBuffer;
        protected ComputeBuffer[] bufferPool;
        public int BufferPoolSize => bufferPool.Length;
        public ComputeBuffer ActiveBuffer => bufferPool[activeBuffer];
        public void SetActiveBuffer(int bufferIndex)
        {
            activeBuffer = bufferIndex;

            var buffer = bufferPool[activeBuffer];
            foreach (var listener in swapBufferListeners) listener.Invoke(buffer);
        }
        public void SetActiveBufferToLastWritten()
        {
            int poolSize = BufferPoolSize;
            if (poolSize <= 0) return;

            int index = writeBuffer - 1;
            while (index < 0) index += poolSize; 

            SetActiveBuffer(index);
        }

        /// <summary>
        /// Number of frames to wait before swapping to last written buffer.
        /// </summary>
        public int framesToWaitBeforeSwap = 0;

        public IEnumerator WaitSetActiveBuffer(int bufferIndex, int framesToWait)
        {
            for(int a = 0; a < framesToWait; a++) yield return null;

            SetActiveBuffer(bufferIndex); 
        }

        protected NativeList<T> internalData;
        public T this[int index]
        {
            get => internalData[index];
            set => Write(index, value);
        }

        protected bool invalid;
        public bool IsValid() => bufferPool != null && !invalid;

        [NonSerialized]
        protected int writeStart;
        [NonSerialized]
        protected int writeEnd;

        [NonSerialized]
        protected int nextWriteStart;
        [NonSerialized]
        protected int nextWriteEnd;

        [NonSerialized]
        protected int writeStartCounter;
        [NonSerialized]
        protected int writeEndCounter;

        /// <summary>
        /// The write indices determine which portion of the internal data to upload to the buffers and GPU. The minimum and maximim indices remain persistent until the buffer pool has made a full rotation.
        /// </summary>
        public void TrySetWriteIndices(int startIndex, int count)
        {
            int poolSize = BufferPoolSize;
            if (writeStartCounter >= BufferPoolSize)
            {
                if (startIndex < nextWriteStart)
                {
                    writeStart = startIndex;
                } 
                else
                {
                    writeStart = nextWriteStart;
                }
                writeStartCounter = 0;

                nextWriteStart = internalData.Length;
            }
            else if (startIndex < writeStart)
            {
                writeStart = startIndex;
                writeStartCounter = 0;

                nextWriteStart = internalData.Length;
            } 
            else if (startIndex < nextWriteStart)
            {
                nextWriteStart = startIndex;
            }
             
            int endIndex = startIndex + (count - 1); 
            if (writeEndCounter >= BufferPoolSize)
            {
                if (endIndex > nextWriteEnd)
                {
                    writeEnd = endIndex;
                }
                else
                {
                    writeEnd = nextWriteEnd;
                }
                writeEndCounter = 0;

                nextWriteEnd = 0;
            }
            else if (endIndex > writeEnd)
            {
                writeEnd = endIndex;
                writeEndCounter = 0;

                nextWriteEnd = 0;
            }
            else if (endIndex > nextWriteEnd)
            {
                nextWriteEnd = endIndex; 
            }
        }

        [NonSerialized]
        protected bool queuedForUpload;
        public void RequestUpload()
        {
            if (queuedForUpload) return;

            ComputeBufferPoolUploader.Queue(this);
            queuedForUpload = true;
        }

        public void Write(int index, T data)
        {
            TrySetWriteIndices(index, 1);

            internalData[index] = data;
            RequestUpload(); 
        }
        /// <summary>
        /// WARNING: Does not update write indices or request upload to the gpu!
        /// </summary>
        public void WriteFast(int index, T data)
        {
            internalData[index] = data;
        }
        public void Write(WriteToBufferDataDelegate<T> writer, int startIndex, int count)
        {
            TrySetWriteIndices(startIndex, count);

            writer.Invoke(internalData.AsArray());
            RequestUpload();
        }
        /// <summary>
        /// WARNING: Does not update write indices or request upload to the gpu!
        /// </summary>
        public void WriteFast(WriteToBufferDataDelegate<T> writer, int startIndex, int count)
        {
            writer.Invoke(internalData.AsArray()); 
        }
        public void Write(WriteToBufferDataFromStartIndexDelegate<T> writer, int startIndex, int count) 
        {
            TrySetWriteIndices(startIndex, count);

            writer.Invoke(internalData.AsArray(), startIndex); 
            RequestUpload();
        }
        /// <summary>
        /// WARNING: Does not update write indices or request upload to the gpu!
        /// </summary>
        public void WriteFast(WriteToBufferDataFromStartIndexDelegate<T> writer, int startIndex, int count)
        {
            writer.Invoke(internalData.AsArray(), startIndex);
        }
        public void Write(NativeArray<T> data, int srcIndex, int dstIndex, int count)
        {
            TrySetWriteIndices(dstIndex, count);
            NativeArray<T>.Copy(data, srcIndex, internalData.AsArray(), dstIndex, count);
            RequestUpload();
        }
        /// <summary>
        /// WARNING: Does not update write indices or request upload to the gpu!
        /// </summary>
        public void WriteFast(NativeArray<T> data, int srcIndex, int dstIndex, int count)
        {
            NativeArray<T>.Copy(data, srcIndex, internalData.AsArray(), dstIndex, count);
        }

        public int Size => internalData.Length;

        public string name;
        public string Name => string.IsNullOrWhiteSpace(name) ? GetHashCode().ToString() : name;

#if UNITY_EDITOR
        private string stacktrace;
        public string Stacktrace => stacktrace;
#endif

        public DynamicComputeBufferPool(string name, int initialSize, int bufferPoolSize, ComputeBufferType bufferType, ComputeBufferMode bufferMode)
        {
            this.name = name;

            stride = UnsafeUtility.SizeOf(typeof(T));
            this.bufferType = bufferType;
            this.bufferMode = bufferMode;

            internalData = new NativeList<T>(initialSize, Allocator.Persistent);
            bufferPool = new ComputeBuffer[bufferPoolSize];
            SetSize(initialSize);

            framesToWaitBeforeSwap = 0;

#if UNITY_EDITOR
            System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace(true);
            stacktrace = stackTrace.ToString();
            ComputeBufferPoolUploader.Register(this);
#endif
        }

        private void UploadToBuffer(ComputeBuffer buffer, int startIndex, int count)
        {
            var writer = buffer.BeginWrite<T>(startIndex, count);
            NativeArray<T>.Copy(internalData.AsArray(), startIndex, writer, 0, count);
            buffer.EndWrite<T>(count);
        }
        public void Upload()
        {
            queuedForUpload = false;
              
            if (invalid || bufferPool == null) return;

            try
            {
                //writeStart = 0; // debug write entire array to buffer
                //writeEnd = internalData.Length - 1;

                int writeCount = (writeEnd - writeStart) + 1;
                var buffer = bufferPool[writeBuffer];
                UploadToBuffer(buffer, writeStart, writeCount);

                if (writeBuffer != activeBuffer) 
                {
                    if (framesToWaitBeforeSwap > 0)
                    {
                        CoroutineProxy.Start(WaitSetActiveBuffer(writeBuffer, framesToWaitBeforeSwap));
                    } 
                    else
                    {
                        SetActiveBuffer(writeBuffer);  
                    }
                }
            }
            finally
            {
                writeStartCounter++;
                writeEndCounter++;

                writeBuffer++;
                if (writeBuffer >= bufferPool.Length) writeBuffer = 0;
            }
        }
        public void UploadToAll()
        {
            queuedForUpload = false;

            if (invalid || bufferPool == null) return;

            try
            {
                int writeCount = (writeEnd - writeStart) + 1;
                foreach(var buffer in bufferPool) UploadToBuffer(buffer, writeStart, writeCount);

                if (writeBuffer != activeBuffer)
                {
                    if (framesToWaitBeforeSwap > 0)
                    {
                        CoroutineProxy.Start(WaitSetActiveBuffer(writeBuffer, framesToWaitBeforeSwap));
                    }
                    else
                    {
                        SetActiveBuffer(writeBuffer);
                    }
                }
            }
            finally
            {
                writeStartCounter++;
                writeEndCounter++;

                writeBuffer++;
                if (writeBuffer >= bufferPool.Length) writeBuffer = 0;
            }
        }

        public void Dispose()
        {
            invalid = true;

#if UNITY_EDITOR
            ComputeBufferPoolUploader.Unregister(this);
#endif

            swapBufferListeners.Clear();

            if (internalData.IsCreated)
            {
                internalData.Dispose();
                internalData = default;
            }

            if (bufferPool != null)
            {
                foreach(var buffer in bufferPool)
                {
                    if (buffer != null && buffer.IsValid())
                    {
                        buffer.Dispose();
                    }
                }

                bufferPool = null;
            }
        }

        public void SetSize(int size, int updateActiveBufferFrameDelay = 0)
        {
            writeStart = 0;
            writeEnd = size - 1;
            writeStartCounter = 0;
            writeEndCounter = 0;

            writeBuffer = 0; 

            if (size < internalData.Length) 
            { 
                internalData.RemoveRange(size, internalData.Length - size); 
            } 
            else
            {
                T val = default;
                internalData.AddReplicated(val, size - internalData.Length);
            }

            for (int a = 0; a < bufferPool.Length; a++)
            {
                var buffer = bufferPool[a];
                if (buffer != null && buffer.IsValid()) buffer.Dispose();

                buffer = new ComputeBuffer(size, stride, bufferType, bufferMode);  
                //buffer.SetData(internalData.AsArray());
                bufferPool[a] = buffer;
            }

            //RequestUpload();
            //Upload();
            UploadToAll();  

            if (updateActiveBufferFrameDelay <= 0) SetActiveBuffer(writeBuffer); else CoroutineProxy.Start(WaitSetActiveBuffer(writeBuffer, updateActiveBufferFrameDelay)); 
        }
    }

    [DefaultExecutionOrder(999999)]
    public class ComputeBufferPoolUploader : SingletonBehaviour<ComputeBufferPoolUploader>
    {

        public override bool DestroyOnLoad => false;

        public static int ExecutionPriority => 999999999;
        public override int Priority => ExecutionPriority;

        [NonSerialized]
        protected readonly HashSet<IComputeBufferPool> bufferQueue = new HashSet<IComputeBufferPool>();

#if UNITY_EDITOR
        [NonSerialized]
        protected readonly HashSet<IComputeBufferPool> registeredBuffers = new HashSet<IComputeBufferPool>(); 
        public static void Register(IComputeBufferPool buffer)
        {
            var instance = Instance;
            if (instance == null || buffer == null) return;
            instance.registeredBuffers.Add(buffer);
        }
        public static void Unregister(IComputeBufferPool buffer)
        {
            var instance = InstanceOrNull;
            if (instance == null || buffer == null) return;
            instance.registeredBuffers.Remove(buffer);
        }
#endif

        public void QueueLocal(IComputeBufferPool buffer)
        {
            bufferQueue.Add(buffer);
        }
        public static void Queue(IComputeBufferPool buffer)
        {
            var instance = Instance;
            if (instance == null) return;

            instance.QueueLocal(buffer);
        }

        [SerializeField]
        protected UnityEvent OnPostUpload;
        public void ListenPostUploadLocal(UnityAction listener)
        {
            if (OnPostUpload == null) OnPostUpload = new UnityEvent();
            OnPostUpload.AddListener(listener);
        }
        public void EndListenPostUploadLocal(UnityAction listener)
        {
            if (OnPostUpload == null) return;
            OnPostUpload.RemoveListener(listener);
        }

        public static void ListenPostUpload(UnityAction listener)
        {
            var instance = Instance;
            if (instance == null) return;
            instance.ListenPostUploadLocal(listener);
        }
        public static void EndListenPostUpload(UnityAction listener) 
        {
            var instance = InstanceOrNull;
            if (instance == null) return;
            instance.EndListenPostUploadLocal(listener);
        }

        protected readonly List<UnityAction> postUploadActions = new List<UnityAction>(); 
        public void QueuePostUploadActionLocal(UnityAction action)
        {
            postUploadActions.Add(action); 
        }
        public static void QueuePostUploadAction(UnityAction action)
        {
            var instance = Instance;
            if (instance == null) return;

            instance.QueuePostUploadActionLocal(action);
        }

        protected readonly List<IEnumerator> postUploadCoroutines = new List<IEnumerator>();
        public void QueuePostUploadActionLocal(IEnumerator routine)
        {
            postUploadCoroutines.Add(routine);
        }
        public void QueuePostUploadCoroutineLocal(IEnumerator routine) => QueuePostUploadAction(routine);

        public static void QueuePostUploadAction(IEnumerator routine)
        {
            var instance = Instance;
            if (instance == null) return;

            instance.QueuePostUploadActionLocal(routine);
        }
        public void QueuePostUploadCoroutine(IEnumerator routine) => QueuePostUploadAction(routine);  

        public override void OnFixedUpdate()
        {
        }

        public override void OnUpdate()
        {
        }

        public override void OnLateUpdate()
        {
            foreach (var buffer in bufferQueue)
            {
                try
                {
                    buffer.Upload();
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogError(ex);
#else
                    swole.LogError(ex);
#endif
                }
            }
            bufferQueue.Clear();

            OnPostUpload?.Invoke();
            if (postUploadActions.Count > 0)
            {
                foreach (var act in postUploadActions) act();
            }
            postUploadActions.Clear();
            if (postUploadCoroutines.Count > 0)
            {
                foreach (var act in postUploadCoroutines) StartCoroutine(act);
            }
            postUploadCoroutines.Clear();
        }

#if UNITY_EDITOR
        protected void OnApplicationQuit()
        {
            foreach(var buffer in registeredBuffers)
            {
                if (buffer == null || !buffer.IsValid()) continue;

                Debug.LogWarning($"ComputeBufferPool '{buffer.Name}' may not have been disposed! {buffer.Stacktrace}");
            }
        }
#endif

    }
}
