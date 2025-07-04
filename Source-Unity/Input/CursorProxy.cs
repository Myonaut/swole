#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;

namespace Swole
{

    public class CursorProxy : SingletonBehaviour<CursorProxy>
    {

        // Refrain from update calls
        public override bool ExecuteInStack => false;
        public override void OnUpdate() { }
        public override void OnLateUpdate() { }
        public override void OnFixedUpdate() { }
        //

        public override void OnDestroyed()
        {

            raycasters?.Clear();
            raycasters = null;

            objectsUnderCursor?.Clear();
            objectsUnderCursor = null; 

            base.OnDestroyed();

        }
         
        public static Vector3 ScreenPosition { get => UnityEngineHook.AsUnityVector(InputProxy.CursorScreenPosition); set => InputProxy.CursorScreenPosition = UnityEngineHook.AsSwoleVector(value); }
        public static Vector3 WorldPosition { get => UnityEngineHook.AsUnityVector(InputProxy.CursorWorldPositionMainCameraNCP); }

        public static CursorLockMode LockState
        {
            get => InputProxy.CursorLockState;
            set => InputProxy.CursorLockState = value;
        }
        public static bool Visible
        {
            get => InputProxy.IsCursorVisible;
            set => InputProxy.IsCursorVisible = value;
        }

        public static float AxisX => InputProxy.CursorAxisX;
        public static float AxisY => InputProxy.CursorAxisY;

        protected List<BaseRaycaster> raycasters;
        public List<BaseRaycaster> RaycastersList
        {
            get
            {
                if (raycasters == null) UpdateRaycasterList();
                return raycasters;
            }
        }

        public static List<BaseRaycaster> Raycasters => Instance.RaycastersList;

        public void UpdateRaycasterListLocal()
        {
            if (raycasters == null) raycasters = new List<BaseRaycaster>();
            raycasters.Clear();

            raycasters.AddRange(GameObject.FindObjectsOfType<BaseRaycaster>(true));
        }

        public static void UpdateRaycasterList()
        {
            Instance.UpdateRaycasterListLocal();
        }

        protected List<GameObject> objectsUnderCursor = new List<GameObject>();
        public static List<GameObject> ObjectsUnderCursor => GetObjectsUnderCursor();
        public static GameObject FirstObjectUnderCursor
        {
            get
            {
                var list = ObjectsUnderCursor;
                if (list == null || list.Count <= 0) return null;
                return list[0];
            }
        }

        protected readonly List<RaycastResult> results = new List<RaycastResult>();

        protected int lastQueryFrame;

        public List<GameObject> GetObjectsUnderCursorLocal(List<GameObject> appendList = null, bool forceQuery = false, bool updateRaycasters = false)
        {
            int frame = Time.frameCount;

            if ((lastQueryFrame == frame && !forceQuery) || IsQuitting()) 
            {
                if (appendList != null)
                {
                    appendList.AddRange(objectsUnderCursor);
                    return appendList;
                }
                return objectsUnderCursor;       
            }

            lastQueryFrame = frame;

            if (objectsUnderCursor == null) objectsUnderCursor = new List<GameObject>();
            objectsUnderCursor.Clear();

            EventSystem system = EventSystem.current;

            if (system == null) return objectsUnderCursor;

            var eventData = new PointerEventData(system) { position = ScreenPosition };

            results.Clear();

            if (updateRaycasters) 
            { 
                UpdateRaycasterListLocal();
                updateRaycasters = false;
            }
            foreach (var raycaster in RaycastersList)
            {
                if (raycaster == null)
                {
                    updateRaycasters = true;
                    continue;
                }

                raycaster.Raycast(eventData, results); 
            }
            if (updateRaycasters)
            {
                UpdateRaycasterListLocal();
                updateRaycasters = false;
            }

            results.Sort((RaycastResult x, RaycastResult y) => (int)Mathf.Sign(y.depth - x.depth));

            foreach(var result in results)
            {
                if (result.gameObject != null) objectsUnderCursor.Add(result.gameObject);
            }

            if (appendList != null)
            {
                appendList.AddRange(objectsUnderCursor);
                return appendList;
            }
            return objectsUnderCursor;
        }

        public static List<GameObject> GetObjectsUnderCursor(List<GameObject> appendList = null, bool forceQuery = false, bool updateRaycasters = false)
        {
            return Instance.GetObjectsUnderCursorLocal(appendList, forceQuery, updateRaycasters);
        }

    }

}

#endif
