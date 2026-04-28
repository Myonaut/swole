using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Swole.API.Unity.Animation;
using Swole.API.Unity;

using Unity.Mathematics;
using UnityEngine.Events;

namespace Swole.Morphing
{

    public class CustomizableCharacterMeshPointTracker : MonoBehaviour
    {

#if UNITY_EDITOR
        public void OnDrawGizmosSelected()
        {
            if (meshTrackers != null && meshTrackers.Length > 0)
            {
                UnityEngine.Random.InitState(GetInstanceID());
                foreach(var tracker in meshTrackers)
                {
                    Gizmos.color = UnityEngine.Random.ColorHSV(0f, 1f, 0f, 1f, 0.5f, 1f); 
                    if (tracker.points != null && tracker.points.Length > 0)
                    {
                        foreach(var point in tracker.points)
                        {
                            Gizmos.DrawWireSphere(point.CurrentPosition, 0.01f);  
                        }
                    }
                }
            }
        }
#endif

        [Serializable]
        public class MeshTracker
        {
            public string name;

            private int index;
            public int Index => index;

            public GameObject characterMeshGameObject;

            [NonSerialized]
            private ICustomizableCharacter characterMesh;
            public ICustomizableCharacter CharacterMesh
            {
                get
                {
                    if (characterMesh == null) characterMesh = characterMeshGameObject == null ? default : characterMeshGameObject.GetComponent<ICustomizableCharacter>();
                    return characterMesh;
                }
            }

            public PointTracker[] points;

            private List<int> autoUpdatingPoints;

            public void Init(int index)
            {
                this.index = index;

                if (points != null)
                {
                    autoUpdatingPoints = new List<int>();

                    for (int i = 0; i < points.Length; i++)
                    {
                        var point = points[i];
                        point.Init(i);

                        if (!point.settings.onlyUpdateWhenFetched) autoUpdatingPoints.Add(i);
                    }
                }
            }

            public bool TryGetPoint(string name, out PointTracker point)
            {
                point = null;

                if (points != null)
                {
                    foreach(var point_ in points)
                    {
                        if (point_.name == name)
                        {
                            point = point_;
                            return true;
                        }
                    }
                }

                return false;
            }

            public void UpdatePoints()
            {
                if (CharacterMesh == null || (characterMesh is Behaviour b && b == null)) return;
                UpdatePointsNoCheck();
            }
            public void UpdatePointsNoCheck()
            {
                if (autoUpdatingPoints != null)
                {
                    foreach (var pointIndex in autoUpdatingPoints)
                    {
                        var point = points[pointIndex];
                        point.UpdatePosition(characterMesh, point.settings.forceAutoUpdate);
                    }
                }
            }

            public void UpdatePointIfNeeded(int pointIndex)
            {
                if (pointIndex < 0 || pointIndex >= points.Length) return;
                UpdatePointIfNeededUnsafe(pointIndex);
            }
            public void UpdatePointIfNeededUnsafe(int pointIndex)
            {
                points[pointIndex].UpdatePosition(characterMesh);
            }

            public void ForceUpdatePoint(int pointIndex)
            {
                if (pointIndex < 0 || pointIndex >= points.Length) return;
                ForceUpdatePointUnsafe(pointIndex);
            }
            public void ForceUpdatePointUnsafe(int pointIndex)
            {
                points[pointIndex].UpdatePosition(characterMesh, true);
            }

            public Vector3 GetPointPosition(int pointIndex)
            {
                if (pointIndex < 0 || pointIndex >= points.Length) return Vector3.zero;
                return GetPointPositionUnsafe(pointIndex);
            }
            public Vector3 GetPointPositionUnsafe(int pointIndex)
            {
                return points[pointIndex].CurrentPosition;
            }

            public Quaternion GetPointRotationOffset(int pointIndex)
            {
                if (pointIndex < 0 || pointIndex >= points.Length) return Quaternion.identity;
                return GetPointRotationOffsetUnsafe(pointIndex);
            }
            public Quaternion GetPointRotationOffsetUnsafe(int pointIndex)
            {
                return points[pointIndex].CurrentRotationOffset;
            }

            public Matrix4x4 GetPointL2W(int pointIndex)
            {
                if (pointIndex < 0 || pointIndex >= points.Length) return Matrix4x4.identity; 
                return GetPointL2WUnsafe(pointIndex);
            }
            public Matrix4x4 GetPointL2WUnsafe(int pointIndex)
            {
                return points[pointIndex].CurrentL2W;
            }

        }

        [Serializable]
        public struct PointTrackerSettings
        {
            public bool onlyUpdateWhenFetched;
            public bool forceAutoUpdate;
        }

        [Serializable]
        public class PointTrackingTransform
        {
            public Transform transform;

            public bool ignorePosition;
            public bool ignoreRotation;

            public Vector3 positionOffset;
            public Vector3 rotationOffset;
            [NonSerialized]
            public Quaternion rotationOffsetQuat;

            [NonSerialized]
            public Vector3 initPosition;
            [NonSerialized]
            public Quaternion initRotation;

            public void Init()
            {
                if (transform != null)
                {
                    initPosition = transform.position;
                    initRotation = transform.rotation;
                }

                rotationOffsetQuat = Quaternion.Euler(rotationOffset);
            }
        }

        [Serializable]
        public class PointTracker
        {
            public string name;

            [NonSerialized]
            private int indexInTracker;
            public int IndexInTracker => indexInTracker;

            public PointTrackerSettings settings;
            private int lastUpdateFrame = int.MinValue;

            public int boundVertexIndex;
            public Vector3 offset;

            public int boundSkinningVertexIndex;

            public List<PointTrackingTransform> transformsToSync;
            [NonSerialized]
            private bool syncTransforms;

            public void Init(int indexInTracker)
            {
                this.indexInTracker = indexInTracker;

                if (transformsToSync != null && transformsToSync.Count > 0)
                {
                    syncTransforms = true;
                    foreach (var transform in transformsToSync) transform.Init(); 
                }
            }

            private Vector3 currentPosition;
            public Vector3 CurrentPosition => currentPosition;

            private Quaternion currentRotationOffset;
            public Quaternion CurrentRotationOffset => currentRotationOffset;

            private Matrix4x4 currentL2W;
            public Matrix4x4 CurrentL2W => currentL2W;

            private Quaternion startRot;

            public UnityEvent<Vector3, Quaternion, Matrix4x4> OnPositionUpdate = new UnityEvent<Vector3, Quaternion, Matrix4x4>();

            public void UpdatePosition(ICustomizableCharacter characterMesh, bool forceUpdate = false)
            {
                int frame = Time.frameCount;

                if (forceUpdate || frame != lastUpdateFrame)
                {
                    lastUpdateFrame = frame;
                    var vertexPosition = characterMesh.GetVertexInWorld(0, boundVertexIndex, out var l2w, out var vertexDelta);
                    if (boundSkinningVertexIndex >= 0 && boundSkinningVertexIndex != boundVertexIndex)
                    {
                        vertexDelta = math.rotate(l2w, vertexDelta);
                        vertexPosition = characterMesh.GetVertexInWorld(0, boundSkinningVertexIndex, out l2w, out var vertexDelta2);
                        vertexDelta2 = math.rotate(l2w, vertexDelta2);
                        vertexPosition += vertexDelta - vertexDelta2;
                    }

                    var worldRot = l2w.GetRotation();
                    if (startRot.x == 0 && startRot.y == 0 && startRot.z == 0 && startRot.w == 0) startRot = Quaternion.Inverse(worldRot);

                    currentPosition = vertexPosition + math.rotate(l2w, offset);
                    currentRotationOffset = worldRot * startRot;
                    currentL2W = l2w;

                    OnPositionUpdate?.Invoke(currentPosition, currentRotationOffset, currentL2W);

                    if (syncTransforms)
                    {
                        foreach(var transform_ in transformsToSync)
                        {
                            if (transform_.transform != null)
                            {
                                if (!transform_.ignorePosition) transform_.transform.position = currentPosition + (currentRotationOffset * transform_.positionOffset);
                                if (!transform_.ignoreRotation) transform_.transform.rotation = currentRotationOffset * transform_.rotationOffsetQuat * transform_.initRotation;
                            }
                        }
                    }
                }
            }
        }

        [SerializeField]
        protected MeshTracker[] meshTrackers;

        public void AddTrackers(IEnumerable<MeshTracker> trackers, bool reinitialize = true) 
        {
            List<MeshTracker> trackers_ = new List<MeshTracker>();
            if (meshTrackers != null) trackers_.AddRange(meshTrackers);
            if (trackers != null)
            {
                foreach(var tracker in trackers) trackers_.Add(tracker);
            }

            meshTrackers = trackers_.ToArray();

            if (reinitialize) Initialize();
        }

        public MeshTracker GetTracker(int index)
        {
            if (index < 0 || meshTrackers == null || index >= meshTrackers.Length) return null;
            return GetTrackerUnsafe(index);
        }
        public MeshTracker GetTrackerUnsafe(int index) => meshTrackers[index];

        [NonSerialized]
        protected List<int> activeTrackers;

        public bool TryGetTracker(string name, out MeshTracker tracker) 
        {
            tracker = null;

            if (meshTrackers != null)
            {
                foreach (var tracker_ in meshTrackers)
                {
                    if (tracker_.name == name)
                    {
                        tracker = tracker_;
                        return true;
                    }
                }
            }

            return false;
        }
        public bool TryGetTracker(GameObject obj, out MeshTracker tracker)
        {
            tracker = null;

            if (meshTrackers != null)
            {
                foreach (var tracker_ in meshTrackers)
                {
                    if (tracker_.characterMeshGameObject == obj)
                    {
                        tracker = tracker_;
                        return true;
                    }
                }
            }

            return false;
        }
        public bool TryGetTracker(ICustomizableCharacter characterMesh, out MeshTracker tracker)
        {
            tracker = null;

            if (meshTrackers != null)
            {
                foreach (var tracker_ in meshTrackers)
                {
                    if (ReferenceEquals(tracker_.CharacterMesh, characterMesh))
                    {
                        tracker = tracker_;
                        return true;
                    }
                }
            }

            return false;
        } 

        protected void Awake()
        {
            Initialize();
        }

        protected void Start()
        {        
        } 

        public void Initialize()
        {
            if (meshTrackers != null)
            {
                activeTrackers = new List<int>();

                for (int i = 0; i < meshTrackers.Length; i++)
                {
                    var tracker = meshTrackers[i];

                    tracker.Init(i);
                    if (tracker.CharacterMesh != null) activeTrackers.Add(i); 
                }
            }
        }

        protected void OnEnable()
        {
            CustomizableCharacterMeshPointTrackerUpdater.Register(this);
        }

        protected void OnDisable()
        {
            CustomizableCharacterMeshPointTrackerUpdater.Unregister(this);
        }

        protected void OnDestroy()
        {
        }

        private readonly List<int> toRemove = new List<int>();
        public virtual void UpdatePoints()
        {
            if (activeTrackers != null)
            {
                foreach(var trackerIndex in activeTrackers)
                {
                    var tracker = meshTrackers[trackerIndex];
                    var characterMesh = tracker.CharacterMesh;
                    if (characterMesh == null || (characterMesh is Behaviour b && b == null))
                    {
                        toRemove.Add(trackerIndex);
                        continue;
                    }

                    tracker.UpdatePointsNoCheck();
                }

                if (toRemove.Count > 0)
                {
                    foreach(var trackerIndex in toRemove) activeTrackers.Remove(trackerIndex);
                    toRemove.Clear();
                }
            }
        }

        public void UpdatePointIfNeeded(int trackerIndex, int pointIndex)
        {
            if (trackerIndex < 0 || meshTrackers == null || trackerIndex >= meshTrackers.Length) return;
            meshTrackers[trackerIndex].UpdatePointIfNeeded(pointIndex);
        }
        public void UpdatePointIfNeededUnsafe(int trackerIndex, int pointIndex)
        {
            meshTrackers[trackerIndex].UpdatePointIfNeededUnsafe(pointIndex);
        }

        public void ForceUpdatePoint(int trackerIndex, int pointIndex)
        {
            if (trackerIndex < 0 || meshTrackers == null || trackerIndex >= meshTrackers.Length) return;
            meshTrackers[trackerIndex].ForceUpdatePoint(pointIndex);
        }
        public void ForceUpdatePointUnsafe(int trackerIndex, int pointIndex)
        {
            meshTrackers[trackerIndex].ForceUpdatePointUnsafe(pointIndex);
        }

        public Vector3 GetPointPosition(int trackerIndex, int pointIndex)
        {
            if (trackerIndex < 0 || meshTrackers == null || trackerIndex >= meshTrackers.Length) return Vector3.zero;
            return meshTrackers[trackerIndex].GetPointPosition(pointIndex);
        }
        public Vector3 GetPointPositionUnsafe(int trackerIndex, int pointIndex)
        {
            return meshTrackers[trackerIndex].GetPointPositionUnsafe(pointIndex);
        }

        public Quaternion GetPointRotationOffset(int trackerIndex, int pointIndex)
        {
            if (trackerIndex < 0 || meshTrackers == null || trackerIndex >= meshTrackers.Length) return Quaternion.identity;
            return meshTrackers[trackerIndex].GetPointRotationOffset(pointIndex);
        }
        public Quaternion GetPointRotationOffsetUnsafe(int trackerIndex, int pointIndex)
        {
            return meshTrackers[trackerIndex].GetPointRotationOffsetUnsafe(pointIndex);
        }

        public Matrix4x4 GetPointL2W(int trackerIndex, int pointIndex)
        {
            if (trackerIndex < 0 || meshTrackers == null || trackerIndex >= meshTrackers.Length) return Matrix4x4.identity;
            return meshTrackers[trackerIndex].GetPointL2W(pointIndex);
        }
        public Matrix4x4 GetPointL2WUnsafe(int trackerIndex, int pointIndex)
        {
            return meshTrackers[trackerIndex].GetPointL2WUnsafe(pointIndex);
        }

    }

    public class CustomizableCharacterMeshPointTrackerUpdater : SingletonBehaviour<CustomizableCharacterMeshPointTrackerUpdater>
    {

        public static int ExecutionPriority = Swole.Cloth.CustomizableCharacterClothBoneUpdater.ExecutionPriority + 1;

        public override int Priority => ExecutionPriority;

        protected readonly List<CustomizableCharacterMeshPointTracker> trackers = new List<CustomizableCharacterMeshPointTracker>();

        public void RegisterLocal(CustomizableCharacterMeshPointTracker tracker)
        {
            if (!trackers.Contains(tracker)) trackers.Add(tracker);
        }

        private readonly List<CustomizableCharacterMeshPointTracker> toRemove = new List<CustomizableCharacterMeshPointTracker>();
        public void UnregisterLocal(CustomizableCharacterMeshPointTracker tracker)
        {
            toRemove.Add(tracker);
        }

        public static void Register(CustomizableCharacterMeshPointTracker tracker)
        {
            var instance = Instance;
            if (instance == null) return;

            instance.RegisterLocal(tracker);
        }

        public static void Unregister(CustomizableCharacterMeshPointTracker tracker)
        {
            var instance = InstanceOrNull;
            if (instance == null) return;

            instance.UnregisterLocal(tracker);
        }

        public override void OnFixedUpdate()
        {
        }

        public override void OnUpdate()
        {
        }

        public override void OnLateUpdate()
        {
            foreach (var tracker in trackers)
            {
                if (tracker != null) tracker.UpdatePoints();
            }

            if (toRemove.Count > 0)
            {
                foreach (var tracker in toRemove) if (tracker != null) trackers.Remove(tracker);
                toRemove.Clear();

                trackers.RemoveAll(b => b == null);
            }
        }

    }

}
