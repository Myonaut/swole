#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

#if BULKOUT_ENV
using RLD; // Paid Asset Integration https://assetstore.unity.com/packages/tools/modeling/runtime-level-design-52325
#endif

namespace Swole.API.Unity
{

    /// <summary>
    /// Handles the manual manipulation of a scene during runtime.
    /// </summary>
    public class RuntimeEditor : MonoBehaviour
    {

        public delegate void ObjectsListDelegate(List<GameObject> objects);

        protected float disableSelectionTimer;
        protected bool disableSelectionTemp;
        public void DisableSelectionForTime(float time)
        {
            if (disableSelectionTimer <= 0) disableSelectionTemp = disableSelection; 

            disableSelectionTimer = time;    
            disableSelection = true;   
        }
        protected void DisableSelectionForTimeStep()
        {
            if (disableSelectionTimer > 0)
            {
                disableSelectionTimer -= Time.deltaTime;
                if (disableSelectionTimer <= 0)
                {
                    disableSelection = disableSelectionTemp;
                }
                else
                {
                    disableSelection = true;
                }
            }
            else disableSelectionTemp = disableSelection;
        }
        protected bool disableSelection;
        public bool DisableSelection
        {
            get => disableSelection;
            set
            {
                if (disableSelectionTimer > 0)
                {
                    if (!value)
                    {
                        disableSelectionTimer = 0; 
                    }
                    else
                    {
                        disableSelectionTemp = value;
                        return;
                    }
                }

                disableSelection = value;
                if (isRLD)
                {
#if BULKOUT_ENV
                    var rtSelection = RTObjectSelection.Get;
                    if (rtSelection != null) rtSelection.SetEnabled(!disableSelection);
#endif
                }
            }
        }

        protected bool disableSelectionBoundingBox;
        public bool DisableSelectionBoundingBox
        {
            get => disableSelectionBoundingBox;
            set
            {
                disableSelectionBoundingBox = value;
                if (isRLD)
                {
#if BULKOUT_ENV
                    var rtSelection = RTObjectSelection.Get;
                    if (rtSelection != null) rtSelection.LookAndFeel.DrawHighlight = !disableSelectionBoundingBox;
#endif
                }
            }
        }

        protected float disableClickSelectTimer;
        protected bool disableClickSelectTemp;
        public void DisableClickSelectForTime(float time)
        {
            if (disableClickSelectTimer <= 0) disableClickSelectTemp = disableClickSelect;

            disableClickSelectTimer = time;
            disableClickSelect = true;
        }
        protected void DisableClickSelectForTimeStep()
        {
            if (disableClickSelectTimer > 0)
            {
                disableClickSelectTimer -= Time.deltaTime;
                if (disableClickSelectTimer <= 0)
                {
                    disableClickSelect = disableClickSelectTemp;
                }
                else
                {
                    disableClickSelect = true;
                }
            }
            else disableClickSelectTemp = disableClickSelect;
        }
        protected bool disableClickSelect;
        public bool DisableClickSelect
        {
            get => disableClickSelect;
            set
            {
                if (disableSelectionTimer > 0)
                {
                    if (!value)
                    {
                        disableClickSelectTimer = 0;
                    }
                    else
                    {
                        disableClickSelectTemp = value;
                        return;
                    }
                }

                disableClickSelect = value;
            }
        }
#if BULKOUT_ENV
        protected void CanClickSelect(YesNoAnswer answer)
        {
            if (disableClickSelect) answer.No();
        }
#endif

        protected bool disableCameraControl;
        public bool DisableCameraControl
        {
            get => disableCameraControl;
            set
            {
                disableCameraControl = value;
                if (isRLD)
                {
#if BULKOUT_ENV
                    var rtCam = RTFocusCamera.Get;
                    if (rtCam != null) rtCam.Settings.CanProcessInput = !disableCameraControl;
                    
#endif
                }
            }
        }

        protected bool disableUndoRedo;
        public bool DisableUndoRedo
        {
            get => disableUndoRedo;
            set
            {
                disableUndoRedo = value;
                if (isRLD)
                {
#if BULKOUT_ENV
                    var rtUndoRedo = RTUndoRedo.Get;
                    if (rtUndoRedo != null) rtUndoRedo.SetEnabled(!disableUndoRedo);
#endif
                }
            }
        }

        protected bool disableGrid;
        public bool DisableGrid
        {
            get => disableGrid;
            set
            {
                disableGrid = value;
                if (isRLD)
                {
#if BULKOUT_ENV
                    var sceneGrid = RTSceneGrid.Get;
                    if (sceneGrid != null) sceneGrid.Settings.IsVisible = !disableGrid;
#endif
                }
            }
        }

        protected bool disableGroupSelect;
        public bool DisableGroupSelect
        {
            get => disableGroupSelect;
            set
            {
                disableGroupSelect = value;
                if (isRLD)
                {
#if BULKOUT_ENV
                    var rtSelectAll = ObjectSelectEntireHierarchy.Get;
                    if (rtSelectAll != null) rtSelectAll.SetActive(!disableGroupSelect);
#endif
                }
            }
        }

        protected bool disableMultiSelect;
        public bool DisableMultiSelect
        {
            get => disableMultiSelect;
            set
            {
                disableMultiSelect = value;
                if (isRLD)
                {
#if BULKOUT_ENV
                    var rtSelection = RTObjectSelection.Get;
                    if (rtSelection != null) rtSelection.Settings.CanMultiSelect = !disableMultiSelect;
#endif
                }
            }
        }

        protected bool disableSceneUpdates;
        public bool DisableSceneUpdates
        {
            get => disableSceneUpdates;
            set
            {
                disableSceneUpdates = value;
                if (isRLD)
                {
#if BULKOUT_ENV
                    var rtScene = RTScene.Get;
                    if (rtScene != null) rtScene.SetEnabled(!disableSceneUpdates); 
#endif
                }
            }
        }

        protected float cameraSpeed;
        protected float baseCameraSpeed;
        protected float baseCameraAlternateSpeed; 
        public float CameraSpeed
        {
            get => cameraSpeed;
            set
            {
                cameraSpeed = value;

#if BULKOUT_ENV
                if (isRLD)
                {
                    var rtCamera = RTFocusCamera.Get;
                    if (rtCamera != null)
                    {
                        rtCamera.MoveSettings.MoveSpeed = baseCameraSpeed * cameraSpeed;
                        rtCamera.MoveSettings.AlternateMoveSpeed = baseCameraAlternateSpeed * cameraSpeed;
                    }
                }
#endif
            }
        }

        protected float cameraFOV;
        public float CameraFOV
        {
            get => cameraFOV;
            set
            {
                cameraFOV = value;

#if BULKOUT_ENV
                if (isRLD)
                {
                    var rtCamera = RTFocusCamera.Get;
                    if (rtCamera != null)
                    {
                        rtCamera.SetFieldOfView(cameraFOV);
                    }
                }
#endif
            }
        }

        public Camera EditorCamera
        {
            get
            {
#if BULKOUT_ENV
                if (isRLD)
                {
                    var rtCamera = RTFocusCamera.Get;
                    if (rtCamera != null) return rtCamera.TargetCamera;
                }
#endif

                return null;
            }
        }

        public void SetDisabled(bool disable = true)
        {
            if (disable)
            {
#if BULKOUT_ENV
                if (rld != null) rld.gameObject.SetActive(false);
#endif
                gameObject.SetActive(false);
            } 
            else
            {
#if BULKOUT_ENV
                if (rld != null) rld.gameObject.SetActive(true);
#endif
                gameObject.SetActive(true);
            }
        }


        public bool isRLD;

        // TODO: consider writing a custom editor instead of using RLD (maybe) (probably not)

#if BULKOUT_ENV
        [SerializeField]
        private RLDApp rld;
#endif

        public int CurrentGizmoId
        {
            get
            {
#if BULKOUT_ENV
                var rtGizmos = RTObjectSelectionGizmos.Get;
                return rtGizmos.WorkGizmoId;
#else
                return 0;
#endif
            }
            set
            {
#if BULKOUT_ENV
                int id = Mathf.Clamp(value, ObjectSelectionGizmoId.None, ObjectSelectionGizmoId.ExtrudeGizmo);
                RTObjectSelectionGizmos.Get.SetWorkGizmo(id);
#endif
            }
        }

        private readonly List<GameObject> activeSelection = new List<GameObject>();

        public delegate void SelectionChangeDelegate(List<GameObject> fullSelection, List<GameObject> newlySelected, List<GameObject> deselected);
        public event SelectionChangeDelegate OnSelectionChanged;

#if BULKOUT_ENV
        private List<Gizmo> allGizmos;
#endif
        protected void Awake()
        {
            cameraSpeed = baseCameraSpeed = baseCameraAlternateSpeed = 1;
            cameraFOV = CameraProxy._defaultFieldOfView;
#if BULKOUT_ENV
            if (rld == null) rld = GameObject.FindFirstObjectByType<RLDApp>(); // Paid Asset Integration https://assetstore.unity.com/packages/tools/modeling/runtime-level-design-52325
            if (rld == null)
            {
                swole.LogError($"[{nameof(RuntimeEditor)}]: Could not locate {nameof(RLDApp)}!");
                Destroy(this);
            }
            else 
            { 
                isRLD = true;

                var rtCamera = RTFocusCamera.Get;
                if (rtCamera != null)
                {
                    baseCameraSpeed = rtCamera.MoveSettings.MoveSpeed;
                    baseCameraAlternateSpeed = rtCamera.MoveSettings.AlternateMoveSpeed;
                    if (rtCamera.TargetCamera != null) cameraFOV = rtCamera.TargetCamera.fieldOfView;  
                }

                var rtSelection = RTObjectSelection.Get;
                if (rtSelection != null)
                {
                    activeSelection.AddRange(rtSelection.SelectedObjects);
                    rtSelection.CanClickSelectDeselect += CanClickSelect;
                    rtSelection.PreSelectCustomize += OnPreSelectCustomizeRT;
                    rtSelection.WillBeDeleted += OnPreDeleteObjects;
                    rtSelection.Changed += OnSelectionChangeRT;
                    rtSelection.Duplicated += OnDuplicateObjectsRT;
                }
                 
                var rtGizmos = RTObjectSelectionGizmos.Get;
                if (rtGizmos != null)
                {
                    allGizmos = RTObjectSelectionGizmos.Get.GetAllGizmos();
                    IEnumerator WaitToRegister()
                    {
                        while(allGizmos == null || allGizmos.Count == 0)
                        {
                            yield return null;
                            allGizmos = RTObjectSelectionGizmos.Get.GetAllGizmos();
                        }
                        foreach (Gizmo gizmo in allGizmos)
                        {
                            gizmo.PostDragBegin += OnGizmoBeginDrag;
                            gizmo.PostDragUpdate += OnGizmoDrag;
                            gizmo.PostDragEnd += OnGizmoEndDrag;
                        }
                    }
                    StartCoroutine(WaitToRegister());
                }

                var rtPrefabLib = RTPrefabLibDb.Get;
                if (rtPrefabLib != null)
                {
                    rtPrefabLib.PrefabSpawned += OnSpawnRTPrefab; 
                }
            }
#endif

            activeSelection.Clear();

            DisableClickSelectForTime(0.5f); 
        }

        protected void Update()
        {

            DisableSelectionForTimeStep();
            DisableClickSelectForTimeStep();

            var eventSystem = EventSystem.current;
            if (eventSystem != null)
            {
                bool uiHasFocus = eventSystem.currentSelectedGameObject != null;
                bool uiIsUnderCursor = eventSystem.IsPointerOverGameObject();
                if (uiIsUnderCursor) DisableClickSelectForTime(0.35f);

                if (isRLD)
                { // Disable RLD interaction when UI elements have focus
#if BULKOUT_ENV
                    var rtCam = RTFocusCamera.Get;
                    if (rtCam != null)
                    {
                        rtCam.Settings.CanProcessInput = !DisableCameraControl && !uiHasFocus && !uiIsUnderCursor;
                    }
                    var rtSelection = RTObjectSelection.Get;
                    if (rtSelection != null)
                    {
                        rtSelection.SetEnabled(!DisableSelection && !uiHasFocus); 
                    }
                    var rtUndoRedo = RTUndoRedo.Get;
                    if (rtUndoRedo != null)
                    {
                        rtUndoRedo.SetEnabled(!DisableUndoRedo && !uiHasFocus);
                    }
#endif
                }

            }

        }
         
        public List<GameObject> QueryActiveSelection(List<GameObject> query = null)
        {
            if (query == null)
            {
                query = new List<GameObject>(activeSelection);
            }
            else
            {
                query.AddRange(activeSelection);
            }

            return query;
        }
        public bool IsSelected(GameObject go)
        {
            if (go == null) return false;
            return activeSelection.Contains(go);
        }

        //protected static readonly List<GameObject> emptyList = new List<GameObject>();
        protected static readonly List<GameObject> selectList = new List<GameObject>();
        protected static readonly List<GameObject> deselectList = new List<GameObject>();
        protected readonly List<GameObject> _tempSelect = new List<GameObject>();
        protected readonly List<GameObject> _tempIgnore = new List<GameObject>();
#if BULKOUT_ENV
        public void Select(GameObject obj, bool allowUndoRedo = true)
        {
            selectList.Clear();
            selectList.Add(obj);
            Select(selectList, allowUndoRedo);
        }
        public void Select(List<GameObject> objs, bool allowUndoRedo = true)
        {
            var selector = RTObjectSelection.Get;
            selector.AppendObjects(objs, allowUndoRedo);
        }

        public void Deselect(GameObject obj, bool allowUndoRedo = true)
        {
            deselectList.Clear();
            deselectList.Add(obj);
            Deselect(deselectList, allowUndoRedo);
        }
        public void Deselect(List<GameObject> objs, bool allowUndoRedo = true)
        {
            var selector = RTObjectSelection.Get;
            selector.RemoveObjects(objs, allowUndoRedo);
        }

        public void ToggleSelect(List<GameObject> objs, bool allowUndoRedo = true)
        {
            foreach (var obj in objs) ToggleSelect(obj, allowUndoRedo);
        }
        public void ToggleSelect(GameObject obj, bool allowUndoRedo = true)
        {
            if (activeSelection.Contains(obj)) Deselect(obj, allowUndoRedo); else Select(obj, allowUndoRedo);
        }

        public void SelectAll(Transform root = null, bool allowUndoRedo = true)
        {
            if (root == null)
            {
                selectList.Clear();
                SceneManager.GetActiveScene().GetRootGameObjects(selectList);
                Select(selectList, allowUndoRedo);
            }
            else
            {
                for (int a = 0; a < root.childCount; a++)
                {
                    var obj = root.GetChild(a).gameObject;
                    Select(obj, allowUndoRedo);
                }
            }
        }

        public void DeselectAll(Transform root = null, bool allowUndoRedo = true)
        {
            if (root == null)
            {
                Deselect(activeSelection, allowUndoRedo);
            }
            else
            {
                deselectList.Clear();
                for (int a = 0; a < activeSelection.Count; a++)
                {
                    var obj = activeSelection[a];
                    if (obj != null && obj.transform.IsChildOf(root)) deselectList.Add(obj);
                }
                Deselect(deselectList, allowUndoRedo);
            }
        }

        private void OnSelectionChangeRT(ObjectSelectionChangedEventArgs args)
        {
            /*
            emptyList.Clear();
            List<GameObject> selected, deselected;
            selected = deselected = emptyList;

            if (args.NumObjectsSelected > 0)
            {
                selected = args.ObjectsWhichWereSelected; // RLD creates a new list per query... gross.
                foreach (var obj in selected) if (obj != null) if (!activeSelection.Contains(obj)) activeSelection.Add(obj);
            }
            if (args.NumObjectsDeselected > 0)
            {
                deselected = args.ObjectsWhichWereDeselected; // RLD creates a new list per query... gross.
                activeSelection.RemoveAll(i => i == null || deselected.Contains(i));
            }

            OnSelectionChanged?.Invoke(activeSelection, selected, deselected);
            */
            // RLD has inconsistencies somewhere which causes bugs. Gonna have to do it the hard way.
            var selector = RTObjectSelection.Get;
            List<GameObject> newSelection = selector.SelectedObjects; // RLD creates a new list per query... gross.

            selectList.Clear();
            deselectList.Clear();

            deselectList.AddRange(activeSelection);
            foreach(var obj in newSelection)
            {
                int ind = deselectList.IndexOf(obj);
                if (ind < 0)
                {
                    selectList.Add(obj);
                    activeSelection.Add(obj);
                } 
                else
                {
                    deselectList.RemoveAt(ind); // Already selected
                }
            }
            foreach (var obj in deselectList) activeSelection.RemoveAll(i => i == obj);

            OnSelectionChanged?.Invoke(activeSelection, selectList, deselectList);
        }
#else
        public void Select(GameObject obj, bool allowUndoRedo = true)
        {
        }
        public void Select(List<GameObject> objs, bool allowUndoRedo = true)
        {
        }

        public void Deselect(GameObject obj, bool allowUndoRedo = true)
        {
        }
        public void Deselect(List<GameObject> objs, bool allowUndoRedo = true)
        {
        }

        public void ToggleSelect(List<GameObject> objs, bool allowUndoRedo = true)
        {
        }
        public void ToggleSelect(GameObject obj, bool allowUndoRedo = true)
        {
        }

        public void SelectAll(Transform root = null, bool allowUndoRedo = true)
        {
        }

        public void DeselectAll(Transform root = null, bool allowUndoRedo = true)
        {
        }
        private void OnSelectionChange()
        {
            OnSelectionChanged?.Invoke(activeSelection, selectList, deselectList);
        }
#endif

        public delegate void PreSelectCustomizeDelegate(int selectReason, List<GameObject> toSelect, List<GameObject> toDeselect);
        public event PreSelectCustomizeDelegate OnPreSelect;
#if BULKOUT_ENV
        void OnPreSelectCustomizeRT(ObjectPreSelectCustomizeInfo customizeInfo, List<GameObject> toBeSelected)
        {
            if (OnPreSelect == null) return;

            _tempSelect.Clear();
            _tempIgnore.Clear();
            if (toBeSelected != null) _tempSelect.AddRange(toBeSelected);

            OnPreSelect((int)customizeInfo.SelectReason, _tempSelect, _tempIgnore);

            customizeInfo.SelectThese(_tempSelect);
            customizeInfo.IgnoreThese(_tempIgnore);
        }
#endif

        [Serializable]
        public struct TransformManipulation
        {
            public Vector3 relativeOffset, relativeScale;
            public Quaternion relativeRotation;
        }
        private TransformManipulation currentTransformManipulation = default;
        private readonly List<Transform> tempManipTransforms = new List<Transform>();

        private int draggedGizmoCount;

#if BULKOUT_ENV
        public bool IsDraggingGizmo
        {
            get
            {
                if (allGizmos == null) return false;
                foreach(var gizmo in allGizmos) if (gizmo.IsDragged) return true; 
                return false;
            }
        }

        private void OnGizmoBeginDrag(Gizmo gizmo, int handleId)
        {
            //draggedGizmoCount++;

            try
            {
                currentTransformManipulation = default;
                currentTransformManipulation.relativeRotation = Quaternion.identity;

                tempManipTransforms.Clear();
                foreach (var obj in activeSelection) if (obj != null) tempManipTransforms.Add(obj.transform);
                OnBeginManipulateTransforms?.Invoke(tempManipTransforms);
                tempManipTransforms.Clear();
            } 
            catch(Exception e)
            {
                swole.LogError(e); 
            }
        }
        private void OnGizmoDrag(Gizmo gizmo, int handleId)
        {
            tempManipTransforms.Clear();

            GizmoDragChannel dragChannel = gizmo.ActiveDragChannel; 

            if (dragChannel == GizmoDragChannel.Offset)
            {
                currentTransformManipulation.relativeOffset = currentTransformManipulation.relativeOffset + gizmo.RelativeDragOffset;
            }
            else if (dragChannel == GizmoDragChannel.Rotation)
            {
                currentTransformManipulation.relativeRotation = gizmo.RelativeDragRotation * currentTransformManipulation.relativeRotation;
            }
            else if (dragChannel == GizmoDragChannel.Scale)
            {
                currentTransformManipulation.relativeScale = currentTransformManipulation.relativeScale + gizmo.RelativeDragScale;
            }

            foreach (var obj in activeSelection) if (obj != null) tempManipTransforms.Add(obj.transform);
            OnManipulateTransformsStep?.Invoke(tempManipTransforms, gizmo.RelativeDragOffset, gizmo.RelativeDragRotation, gizmo.RelativeDragScale, gizmo.Transform.Position3D, gizmo.Transform.Rotation3D);  
        }
        private void OnGizmoEndDrag(Gizmo gizmo, int handleId)
        {
            //draggedGizmoCount--;

            tempManipTransforms.Clear();

            if (currentTransformManipulation.relativeOffset.x == 0 && currentTransformManipulation.relativeOffset.y == 0 && currentTransformManipulation.relativeOffset.z == 0 &&
                currentTransformManipulation.relativeRotation.x == 0 && currentTransformManipulation.relativeRotation.y == 0 && currentTransformManipulation.relativeRotation.z == 0 && currentTransformManipulation.relativeRotation.w == 1 &&
                currentTransformManipulation.relativeScale.x == 0 && currentTransformManipulation.relativeScale.y == 0 && currentTransformManipulation.relativeScale.z == 0) return; 

            foreach (var obj in activeSelection) if (obj != null) tempManipTransforms.Add(obj.transform);
            OnManipulateTransforms?.Invoke(tempManipTransforms, currentTransformManipulation.relativeOffset, currentTransformManipulation.relativeRotation, currentTransformManipulation.relativeScale, gizmo.Transform.Position3D, gizmo.Transform.Rotation3D);
        }
#else
        public bool IsDraggingGizmo => draggedGizmoCount > 0;
#endif
        public delegate void BeginManipulateTransformsDelegate(List<Transform> transforms);
        public delegate void ManipulateTransformsDelegate(List<Transform> transforms, Vector3 relativeOffsetWorld, Quaternion relativeRotationWorld, Vector3 relativeScale, Vector3 gizmoWorldPosition, Quaternion gizmoWorldRotation);

        public event BeginManipulateTransformsDelegate OnBeginManipulateTransforms;
        public event ManipulateTransformsDelegate OnManipulateTransformsStep;
        public event ManipulateTransformsDelegate OnManipulateTransforms;

        public delegate void SpawnPrefabDelegate(GameObject prefab, GameObject instance);
        public event SpawnPrefabDelegate OnSpawnPrefab;

#if BULKOUT_ENV
        private void OnSpawnRTPrefab(RTPrefab prefab, GameObject instance)
        {
            try
            {
                if (instance != null && prefab != null && prefab.UnityPrefab != null)
                {
                    instance.name = prefab.UnityPrefab.name; // revert any changes to the object name 
                }
                OnSpawnPrefab?.Invoke(prefab.UnityPrefab, instance);
            } 
            catch(Exception ex)
            {
                swole.LogError($"[{nameof(RuntimeEditor)}:{nameof(OnSpawnRTPrefab)}] Encountered exception while notifying listeners about prefab spawn");
                swole.LogError(ex);
            }
        }

#endif

        public event ObjectsListDelegate OnDuplicateObjects; 

#if BULKOUT_ENV
        private void OnDuplicateObjectsRT(ObjectSelectionDuplicationResult result)
        {
            if (result == null || result.DuplicateParents == null) return;
            OnDuplicateObjects?.Invoke(result.DuplicateParents);
        }
#endif

        public event ObjectsListDelegate OnPreDelete;

        private void OnPreDeleteObjects(List<GameObject> toDelete)
        {
            OnPreDelete?.Invoke(toDelete);
        }

        public void SetExternalRootObjects(ICollection<GameObject> roots)
        {
#if BULKOUT_ENV
            var rtScene = RTScene.Get;
            if (rtScene != null) rtScene.SetExternalRootObjects(roots);
#endif
        }

    }
}

#endif
