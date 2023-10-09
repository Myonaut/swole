#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swole
{

    public static class Utils
    {

        public static Vector3 MousePositionWorld(Camera camera = null)
        {

            return MousePositionWorld(InputProxy.CursorPosition, camera);

        }

        public static Vector3 MousePositionWorld(Vector3 mousePos, Camera camera = null)
        {

            if (camera == null) camera = Camera.main;

            mousePos.z = camera.nearClipPlane + 0.001f;

            return camera.ScreenToWorldPoint(mousePos);

        }

    }

}

#endif
