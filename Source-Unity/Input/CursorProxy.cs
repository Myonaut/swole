#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

        public static Vector3 Position { get => InputProxy.CursorPosition; set => InputProxy.CursorPosition = value; }
        public static CursorLockMode LockState => InputProxy.CursorLockState;
        public static bool Visible => InputProxy.IsCursorVisible;

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

            raycasters.AddRange(GameObject.FindObjectsOfType<BaseRaycaster>());

        }

        public static void UpdateRaycasterList()
        {

            Instance.UpdateRaycasterListLocal();

        }

        protected List<GameObject> objectsUnderCursor = new List<GameObject>();

        public static List<GameObject> ObjectsUnderCursor => GetObjectsUnderCursor();

        protected List<RaycastResult> results = new List<RaycastResult>();

        protected int lastQueryFrame;

        public List<GameObject> GetObjectsUnderCursorLocal()
        {

            int frame = Time.frameCount;

            if (lastQueryFrame == frame || IsQuitting()) return objectsUnderCursor;

            lastQueryFrame = frame;

            if (objectsUnderCursor == null) objectsUnderCursor = new List<GameObject>();

            objectsUnderCursor.Clear();

            EventSystem system = EventSystem.current;

            if (system == null) return objectsUnderCursor;

            var eventData = new PointerEventData(system) { position = Position };

            results.Clear();

            foreach (var raycaster in RaycastersList)
            {
                raycaster.Raycast(eventData, results);
            }

            foreach (var result in results)
            {
                if (result.gameObject != null) objectsUnderCursor.Add(result.gameObject);
            }

            return objectsUnderCursor;

        }

        public static List<GameObject> GetObjectsUnderCursor()
        {

            return Instance.GetObjectsUnderCursorLocal();

        }

    }

}

#endif
