#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Swole.API.Unity
{

    /// <summary>
    /// Translates bone names from one rig to another. Useful for remapping animations.
    /// </summary>
    [CreateAssetMenu(fileName = "RigRemapping", menuName = "Swole/Rigs/Remapping", order = 1)]
    public class RigRemapping : ScriptableObject, ISwoleSerialization<RigRemapping, RigRemapping.Serialized>
    {

        #region Serialization

        public string AsJSON(bool prettyPrint = false) => AsSerializableStruct().AsJSON(prettyPrint);

        [Serializable]
        public struct Serialized : ISerializableContainer<RigRemapping, RigRemapping.Serialized>
        {

            public string name;

            public Remapping[] remappedBones;

            public string SerializedName => name;

            public RigRemapping AsOriginalType(PackageInfo packageInfo = default) => NewInstance(this, packageInfo);
            public string AsJSON(bool prettyPrint = false) => swole.Engine.ToJson(this, prettyPrint);

            public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);

        }

        public static implicit operator Serialized(RigRemapping asset)
        {
            Serialized s = new Serialized();

            s.name = asset.name;
            s.remappedBones = asset.remappedBones;

            return s;
        }

        public RigRemapping.Serialized AsSerializableStruct() => this;
        public object AsSerializableObject() => AsSerializableStruct();

        public static RigRemapping NewInstance(RigRemapping.Serialized serializable, PackageInfo packageInfo = default)
        {
            var inst = NewInstance();

            inst.name = serializable.name;
            inst.remappedBones = serializable.remappedBones;

            return inst;
        }

        public string SerializedName => name;

        #endregion

        public static RigRemapping NewInstance()
        {
            RigRemapping asset = ScriptableObject.CreateInstance<RigRemapping>();
            return asset;
        }

        public static RigRemapping Create(string path = null, string fileName = null, bool incrementIfExists = false)
        {

            RigRemapping asset = NewInstance();

#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(fileName))
            {
                string fullPath = $"{(path + (path.EndsWith('/') ? "" : "/"))}{fileName}.asset";
                if (incrementIfExists) fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);
                AssetDatabase.CreateAsset(asset, fullPath);
                AssetDatabase.SaveAssets();
            }
#endif

            return asset;
        }

        [Serializable]
        public struct Remapping
        {
            public string inputName;
            public string outputName;
        }

        public Remapping[] remappedBones;

        public string Remap(string inputName)
        {
            if (remappedBones == null || string.IsNullOrEmpty(inputName)) return inputName;

            string inputName_ = inputName.Trim();
            foreach(Remapping remapping in remappedBones)
            {
                if (remapping.inputName.Trim() == inputName_) return remapping.outputName;
            }
            inputName_ = inputName.ToLower().Trim();
            foreach (Remapping remapping in remappedBones)
            {
                if (remapping.inputName.ToLower().Trim() == inputName_) return remapping.outputName;
            }

            return inputName;
        }

    }
}

#endif