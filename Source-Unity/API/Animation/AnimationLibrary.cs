#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Swole.API.Unity.Animation;
using Swole.Animation;

namespace Swole.API.Unity
{
    public static class AnimationLibrary
    {

        private static List<CustomAnimationAsset> animationAssets;
        public static void ReloadAnimationAssets()
        {
            if (animationAssets == null) animationAssets = new List<CustomAnimationAsset>();

            var assets = Resources.LoadAll<CustomAnimationAsset>(string.Empty);
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
            if (asset == null && !string.IsNullOrWhiteSpace(animationName))
            {
                if (animationName.TrySplitPackageContentPath(out string pkgStr, out string contentName))
                {
                    return FindAnimation(pkgStr, contentName);
                }
            }
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

        private static List<CustomAvatarMaskAsset> avatarMasks;
        public static void ReloadAvatarMasks()
        {
            if (avatarMasks == null) avatarMasks = new List<CustomAvatarMaskAsset>();

            var assets = Resources.LoadAll<CustomAvatarMaskAsset>(string.Empty);
            if (assets != null)
            {
                avatarMasks.RemoveAll(i => i == null || i.mask == null);

                foreach (var asset in assets)
                {
                    if (asset == null) continue;

                    bool replaced = false;
                    foreach (var existingAsset in avatarMasks)
                    {
                        if (existingAsset == null) continue;
                        if (existingAsset.name == asset.name)
                        {
                            replaced = true;
                            existingAsset.mask = asset.mask;
                            break;
                        }
                    }
                    if (replaced) continue;

                    avatarMasks.Add(asset);
                }
            }
        }
        public static CustomAvatarMaskAsset FindAvatarMaskAsset(string assetName)
        {
            if (avatarMasks == null) ReloadAvatarMasks();

            foreach (var asset in avatarMasks) if (asset.name == assetName) return asset;
            assetName = assetName.AsID();
            foreach (var asset in avatarMasks) if (asset.name.AsID() == assetName) return asset;

            return null;
        }
        public static WeightedAvatarMask FindAvatarMask(string assetName)
        {
            var asset = FindAvatarMaskAsset(assetName);
            if (asset == null && !string.IsNullOrWhiteSpace(assetName))
            {
                if (assetName.TrySplitPackageContentPath(out string pkgStr, out string contentName))
                {
                    return FindAvatarMask(pkgStr, contentName);  
                }
            }
            return asset == null ? null : asset.mask;
        }
        public static WeightedAvatarMask FindAvatarMask(string packageString, string maskName)
        {
            if (string.IsNullOrEmpty(packageString)) return FindAvatarMask(maskName);

            return FindAvatarMask(new PackageIdentifier(packageString), maskName);
        }
        public static WeightedAvatarMask FindAvatarMask(PackageIdentifier package, string maskName)
        {
            if (string.IsNullOrEmpty(package.name)) return FindAvatarMask(maskName); 

            WeightedAvatarMask mask = null;
            var output = swole.FindContentPackage(package, out ContentPackage contentPackage, out _);
            if (output == swole.PackageActionResult.Success)
            {
                string maskNameLiberal = maskName.AsID();
                for (int a = 0; a < contentPackage.ContentCount; a++)
                {
                    var content = contentPackage.GetContent(a);
                    if (typeof(WeightedAvatarMask).IsAssignableFrom(content.GetType())) 
                    {
                        if (content.Name == maskName)
                        {
                            mask = (WeightedAvatarMask)content;
                            break;
                        }
                        else if (content.Name.AsID() == maskNameLiberal)
                        {
                            mask = (WeightedAvatarMask)content;
                        }
                    }
                }
            }
            return mask;
        }

    }
}

#endif
