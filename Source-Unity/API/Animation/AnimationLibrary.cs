#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Swole.Script;
using Swole.API.Unity.Animation;

namespace Swole.API.Unity
{
    public static class AnimationLibrary
    {

        private static List<CustomAnimationAsset> animationAssets;
        public static void ReloadAnimationAssets()
        {
            if (animationAssets == null) animationAssets = new List<CustomAnimationAsset>();

            var assets = Resources.LoadAll<CustomAnimationAsset>("");
            if (assets != null)
            {
                animationAssets.RemoveAll(i => i == null || i.Animation == null);

                foreach(var asset in assets)
                {
                    if (asset == null || asset.Animation == null) continue;

                    bool replaced = false;
                    foreach(var existingAsset in animationAssets)
                    {
                        if (existingAsset.Animation == null) continue;
                        if (existingAsset.Animation.Name == asset.Animation.Name)
                        {
                            replaced = true;
                            existingAsset.Animation = asset.Animation;
                            break;
                        }
                    }
                    if (replaced) continue;

                    animationAssets.Add(asset);
                }
            }
        }
        public static CustomAnimationAsset FindAnimationAsset(string animationName)
        {
            if (animationAssets == null) ReloadAnimationAssets();

            foreach (var asset in animationAssets) if (asset.name == animationName) return asset;
            animationName = animationName.AsID();
            foreach (var asset in animationAssets) if (asset.name.AsID() == animationName) return asset;

            return null;
        }
        public static CustomAnimation FindAnimation(string animationName)
        {
            var asset = FindAnimationAsset(animationName);
            return asset == null ? null : asset.Animation;
        }
        public static CustomAnimation FindAnimation(string packageString, string animationName)
        {
            if (string.IsNullOrEmpty(packageString)) return FindAnimation(animationName);

            return FindAnimation(new PackageIdentifier(packageString), animationName);
        }
        public static CustomAnimation FindAnimation(PackageIdentifier package, string animationName)
        {
            if (string.IsNullOrEmpty(package.name)) return FindAnimation(animationName);

            CustomAnimation anim = null;
            var output = swole.FindContentPackage(package, out ContentPackage contentPackage, out _);
            if (output == swole.PackageActionResult.Success)
            {
                string animationNameLiberal = animationName.AsID();
                for (int a = 0; a < contentPackage.ContentCount; a++)
                {
                    var content = contentPackage.GetContent(a);
                    if (typeof(CustomAnimation).IsAssignableFrom(content.GetType()))
                    {
                        if (content.Name == animationName)
                        {
                            anim = (CustomAnimation)content;
                            break;
                        }
                        else if (content.Name.AsID() == animationNameLiberal)
                        {
                            anim = (CustomAnimation)content;
                        }
                    }
                }
            }
            return anim;
        }

        private static AnimatableAsset[] animatableAssets;
        public static List<AnimatableAsset> GetAllAnimatables(List<AnimatableAsset> list = null)
        {
            if (list == null) list = new List<AnimatableAsset>();
            if (animatableAssets == null) animatableAssets = Resources.LoadAll<AnimatableAsset>("");
            list.AddRange(animatableAssets);
            return list;
        }
        public static AnimatableAsset FindAnimatable(string id)
        {
            if (animatableAssets == null) animatableAssets = Resources.LoadAll<AnimatableAsset>("");

            foreach (var asset in animatableAssets) if (asset.name == id) return asset;
            id = id.AsID();
            foreach (var asset in animatableAssets) if (asset.name.AsID() == id) return asset;

            return null;
        }

    }
}

#endif
