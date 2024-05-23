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

            return MousePositionWorld(UnityEngineHook.AsUnityVector(InputProxy.CursorScreenPosition), camera);

        }

        public static Vector3 MousePositionWorld(Vector3 mousePos, Camera camera = null)
        {

            if (camera == null)
            {
                camera = Camera.main;
                if (camera == null) return mousePos;
            }

            mousePos.z = camera.nearClipPlane + 0.001f;

            return camera.ScreenToWorldPoint(mousePos);

        }

        private const string _underscoreStr = "_";
        private const string _periodStr = ".";

        public const string _leftTagInnerUnderscore = "_L_";
        public const string _leftTagSuffixUnderscore = "_L";
        public const string _rightTagInnerUnderscore = "_R_";
        public const string _rightTagSuffixUnderscore = "_R"; 


        public static string GetMirroredName(string name, string delimiter) => GetMirroredName(name, _leftTagInnerUnderscore.Replace(_underscoreStr, delimiter), _leftTagSuffixUnderscore.Replace(_underscoreStr, delimiter), _rightTagInnerUnderscore.Replace(_underscoreStr, delimiter), _rightTagSuffixUnderscore.Replace(_underscoreStr, delimiter));     
        public static string GetMirroredName(string name, string leftTagInner, string leftTagSuffix, string rightTagInner, string rightTagSuffix)
        {
            string mirroredName = name;

            if (mirroredName.EndsWith(leftTagSuffix)) mirroredName = mirroredName.Substring(0, mirroredName.Length - leftTagSuffix.Length) + rightTagSuffix;
            else
            if (mirroredName.EndsWith(rightTagSuffix)) mirroredName = mirroredName.Substring(0, mirroredName.Length - rightTagSuffix.Length) + leftTagSuffix;
            else
            if (mirroredName.Contains(leftTagInner)) mirroredName = mirroredName.Replace(leftTagInner, rightTagInner);
            else
            if (mirroredName.Contains(rightTagInner)) mirroredName = mirroredName.Replace(rightTagInner, leftTagInner);

            return mirroredName;
        }

        public static string GetMirroredName(string name)
        {
            string mirroredName = GetMirroredName(name, _underscoreStr);
            if (mirroredName == name) mirroredName = GetMirroredName(name, _periodStr);
            return mirroredName;
        }

    }

}

#endif
