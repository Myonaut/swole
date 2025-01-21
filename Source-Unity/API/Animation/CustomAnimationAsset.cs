#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using Swole.Animation;

namespace Swole.API.Unity.Animation
{

    public class CustomAnimationAsset : ScriptableObject, IAnimationAsset
    {

        public System.Type AssetType => GetType();
        public object Asset => this;

        private bool disposed;
        public bool isNotInternalAsset;
        public bool IsInternalAsset
        {
            get => !isNotInternalAsset;
            set => isNotInternalAsset = !value;
        }

        protected bool invalid;
        public bool IsValid => !invalid;
        public void Dispose()
        {
            if (!IsInternalAsset && !disposed)
            {
                GameObject.Destroy(this);
            }
            disposed = true;
            invalid = true;
        }
        public void Delete() => Dispose();

        #region IContent

        public PackageInfo PackageInfo => animation == null ? default : animation.PackageInfo;

        public ContentInfo ContentInfo => animation == null ? default : animation.ContentInfo;

        public string Author => animation == null ? default : animation.Author;

        public string CreationDate => animation == null ? default : animation.CreationDate;

        public string LastEditDate => animation == null ? default : animation.LastEditDate;

        public string Description => animation == null ? default : animation.Description;

        public string OriginPath => animation == null ? default : animation.OriginPath;

        public string RelativePath => animation == null ? default : animation.RelativePath;

        public string Name => animation == null ? default : animation.Name;

        public IContent CreateCopyAndReplaceContentInfo(ContentInfo info)
        {
            var obj = NewExternalInstance();
            if (animation != null) obj.animation = (CustomAnimation)animation.CreateCopyAndReplaceContentInfo(info);
            return obj;
        }

        public IContent SetOriginPath(string path)
        {
            if (animation == null) return default;
            return animation.SetOriginPath(path);
        }

        public IContent SetRelativePath(string path)
        {
            if (animation == null) return default;
            return animation.SetRelativePath(path);
        }

        public List<PackageIdentifier> ExtractPackageDependencies(List<PackageIdentifier> dependencies = null)
        {
            if (animation == null) return dependencies == null ? new List<PackageIdentifier>() : dependencies;
            return animation.ExtractPackageDependencies(dependencies);
        }

        #endregion

        #region ICloneable

        public object Clone()
        {
            var obj = ScriptableObject.CreateInstance<CustomAnimationAsset>();
            if (animation != null) obj.animation = (CustomAnimation)animation.Clone();
            return obj;
        }

        #endregion

        public string ID
        {
            get
            {
                return $"ANIMATION_ASSET[{name}]";
            }
        }

        public static CustomAnimationAsset Create(string path, string fileName, CustomAnimation animation, bool incrementIfExists = false)
        {

            CustomAnimationAsset asset = CreateInstance<CustomAnimationAsset>();
            asset.animation = animation;

#if UNITY_EDITOR
            string fullPath = $"{path + (path.EndsWith('/') ? "" : "/")}{fileName}.asset";
            if (incrementIfExists) fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);
            AssetDatabase.CreateAsset(asset, fullPath);
            AssetDatabase.SaveAssets();
#endif

            return asset;
        }
        public static CustomAnimationAsset NewExternalInstance()
        {
            var inst = ScriptableObject.CreateInstance<CustomAnimationAsset>();
            inst.isNotInternalAsset = true;
            return inst;
        }

        [SerializeField]
        private CustomAnimation animation;
        public CustomAnimation Animation 
        {
            get => animation;
            set => SetAnimation(value);
        }

        public delegate void SetAnimationDelegate(CustomAnimation animation);
        public event SetAnimationDelegate OnSetAnimation;

        public void SetAnimation(CustomAnimation animation)
        {
            this.animation = animation;
            OnSetAnimation?.Invoke(animation); 
        }

        public bool HasKeyframes => animation == null ? false : animation.HasKeyframes;
        public float GetClosestKeyframeTime(float referenceTime, bool includeReferenceTime = true, IntFromDecimalDelegate getFrameIndex = null)
        {
            if (animation == null) return 0;
            return animation.GetClosestKeyframeTime(referenceTime, includeReferenceTime, getFrameIndex);  
        }
    }

}

#endif
