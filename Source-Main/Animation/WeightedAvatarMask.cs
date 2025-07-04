using System;
using System.Collections;
using System.Collections.Generic;

namespace Swole.Animation
{
    [Serializable]
    public class WeightedAvatarMask : IContent, ICloneable
    {
        #region IContent

        public PackageInfo PackageInfo => throw new NotImplementedException();

        public ContentInfo ContentInfo => throw new NotImplementedException();

        public string Author => throw new NotImplementedException();

        public string CreationDate => throw new NotImplementedException();

        public string LastEditDate => throw new NotImplementedException();

        public string Description => throw new NotImplementedException();

        public string OriginPath => throw new NotImplementedException();

        public string RelativePath => throw new NotImplementedException();

        public bool IsInternalAsset { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string Name => throw new NotImplementedException();

        public bool IsValid => throw new NotImplementedException();

        public Type AssetType => throw new NotImplementedException();

        public object Asset => throw new NotImplementedException();

        public string CollectionID { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool HasCollectionID => throw new NotImplementedException();

        public IContent CreateShallowCopyAndReplaceContentInfo(ContentInfo info)
        {
            throw new NotImplementedException();
        }

        public IContent CreateCopyAndReplaceContentInfo(ContentInfo info)
        {
            throw new NotImplementedException();
        }

        public IContent SetOriginPath(string path)
        {
            throw new NotImplementedException();
        }

        public IContent SetRelativePath(string path)
        {
            throw new NotImplementedException();
        }

        public void Delete()
        {
            throw new NotImplementedException();
        }

        public void DisposeSelf()
        {
            throw new NotImplementedException();
        }

        public bool IsIdenticalAsset(ISwoleAsset otherAsset)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public List<PackageIdentifier> ExtractPackageDependencies(List<PackageIdentifier> dependencies = null)
        {
            throw new NotImplementedException();
        }

        #endregion

        [Serializable]
        public struct WeightedEntry
        {
            public string entryName;

#if UNITY_EDITOR
            [UnityEngine.Range(0, 1)]
#endif
            public float weight;

            public static WeightedEntry operator -(WeightedEntry a)
            {
                var val = a;
                val.weight = -val.weight;
                return val;
            }

            public static WeightedEntry operator *(WeightedEntry a, float b)
            {
                var val = a;
                val.weight = val.weight * b;
                return val;
            }
            public static WeightedEntry operator /(WeightedEntry a, float b)
            {
                var val = a;
                val.weight = val.weight / b;
                return val;
            }
            public static WeightedEntry operator +(WeightedEntry a, float b)
            {
                var val = a;
                val.weight = val.weight + b;
                return val;
            }
            public static WeightedEntry operator -(WeightedEntry a, float b)
            {
                var val = a;
                val.weight = val.weight - b;
                return val;
            }
            public static WeightedEntry operator +(float a, WeightedEntry b)
            {
                var val = b;
                val.weight = a + val.weight;
                return val;
            }
            public static WeightedEntry operator -(float a, WeightedEntry b)
            {
                var val = b;
                val.weight = a - val.weight; 
                return val;
            }
        }

        public float weightMultiplier = 1f;

        public List<WeightedEntry> boneMask;
        public List<WeightedEntry> propertyMask;

        public WeightedAvatarMask() { }
        public WeightedAvatarMask(WeightedAvatarMaskComposite composite)
        {
            this.boneMask = composite.boneMask;
            this.propertyMask = composite.propertyMask; 
        }

        public WeightedEntry this[int index]
        {
            get => boneMask[index] * weightMultiplier;
            set => boneMask[index] = value;
        }

        public float this[string boneName]
        {
            get => Get(boneName).weight * weightMultiplier;
            set => Set(boneName, value);
        }

        public int BoneCount => boneMask == null ? 0 : boneMask.Count;
        public int PropertyCount => propertyMask == null ? 0 : propertyMask.Count;

        public WeightedEntry Get(string boneName, float defaultWeight = 0f, bool invert = false)
        {
            if (boneMask == null) return default;

            foreach (var bm in boneMask) if (bm.entryName == boneName) return (invert ? new WeightedEntry() { entryName = bm.entryName, weight = 1 - (bm.weight * weightMultiplier) } : (bm * weightMultiplier));

            defaultWeight = defaultWeight * weightMultiplier;
            return new WeightedEntry() { entryName = boneName, weight = invert ? 1f - defaultWeight : defaultWeight }; 
        }
        public void Set(WeightedEntry bm) => Set(bm.entryName, bm.weight);
        public void Set(string boneName, float weight)
        {
            if (boneMask == null) boneMask = new List<WeightedEntry>();

            for(int a = 0; a < boneMask.Count; a++)
            {
                var bm = boneMask[a];
                if (bm.entryName == boneName)
                {
                    bm.weight = weight;
                    boneMask[a] = bm;
                }
            }
        }
        public bool Contains(string boneName)
        {
            if (boneMask == null) return false;

            foreach (var bm in boneMask) if (bm.entryName == boneName) return true;
            return false;
        }


        public WeightedEntry GetProperty(string propertyName, float defaultWeight = 0f, bool invert = false)
        {
            if (propertyMask == null) return default;

            foreach (var pm in propertyMask) if (pm.entryName == propertyName) return (invert ? new WeightedEntry() { entryName = pm.entryName, weight = 1 - (pm.weight * weightMultiplier) } : (pm * weightMultiplier));

            defaultWeight = defaultWeight * weightMultiplier;
            return new WeightedEntry() { entryName = propertyName, weight = invert ? 1f - defaultWeight : defaultWeight };
        }
        public void SetProperty(WeightedEntry pm) => SetProperty(pm.entryName, pm.weight);
        public void SetProperty(string propertyName, float weight)
        {
            if (propertyMask == null) propertyMask = new List<WeightedEntry>();

            for (int a = 0; a < propertyMask.Count; a++)
            {
                var bm = propertyMask[a];
                if (bm.entryName == propertyName)
                {
                    bm.weight = weight;
                    propertyMask[a] = bm;
                }
            }
        }
        public bool ContainsProperty(string propertyName)
        {
            if (propertyMask == null) return false;

            foreach (var pm in propertyMask) if (pm.entryName == propertyName) return true;
            return false;
        }

        public object Clone() => Duplicate();
        public WeightedAvatarMask Duplicate()
        {
            var newInstance = new WeightedAvatarMask();
            if (boneMask != null) newInstance.boneMask = new List<WeightedEntry>(boneMask);
            if (propertyMask != null) newInstance.propertyMask = new List<WeightedEntry>(propertyMask);
            return newInstance;
        }

        public static implicit operator WeightedAvatarMaskComposite(WeightedAvatarMask mask)
        {
            return new WeightedAvatarMaskComposite()  
            {
                boneMask = mask.boneMask == null ? new List<WeightedEntry>() : new List<WeightedEntry>(mask.boneMask),
                propertyMask = mask.propertyMask == null ? new List<WeightedEntry>() : new List<WeightedEntry>(mask.propertyMask) 
            };
        }
        public WeightedAvatarMaskComposite AsComposite(bool maintainReferences = true)
        {
            if (maintainReferences)
            {
                return new WeightedAvatarMaskComposite()
                {
                    boneMask = boneMask,
                    propertyMask = propertyMask
                };
            }

            return this;
        }

        public static WeightedAvatarMaskComposite operator -(WeightedAvatarMask a) => -(a.AsComposite(true));

        public static WeightedAvatarMaskComposite operator +(WeightedAvatarMask a, WeightedAvatarMask b) => ((a.AsComposite(true)) + (b.AsComposite(true)));
        public static WeightedAvatarMaskComposite operator -(WeightedAvatarMask a, WeightedAvatarMask b) => ((a.AsComposite(true)) - (b.AsComposite(true)));
        public static WeightedAvatarMaskComposite operator *(WeightedAvatarMask a, WeightedAvatarMask b) => ((a.AsComposite(true)) * (b.AsComposite(true)));
        public static WeightedAvatarMaskComposite operator /(WeightedAvatarMask a, WeightedAvatarMask b) => ((a.AsComposite(true)) / (b.AsComposite(true)));

        public static WeightedAvatarMaskComposite operator +(WeightedAvatarMask a, float b) => ((a.AsComposite(true)) + b);
        public static WeightedAvatarMaskComposite operator -(WeightedAvatarMask a, float b) => ((a.AsComposite(true)) - b);
        public static WeightedAvatarMaskComposite operator +(float a, WeightedAvatarMask b) => (a + (b.AsComposite(true)));
        public static WeightedAvatarMaskComposite operator -(float a, WeightedAvatarMask b) => (a - (b.AsComposite(true)));
        public static WeightedAvatarMaskComposite operator *(WeightedAvatarMask a, float b) => ((a.AsComposite(true)) * b);
        public static WeightedAvatarMaskComposite operator /(WeightedAvatarMask a, float b) => ((a.AsComposite(true)) / b); 
    }

    [Serializable]
    public struct WeightedAvatarMaskComposite
    {
        public List<WeightedAvatarMask.WeightedEntry> boneMask;
        public List<WeightedAvatarMask.WeightedEntry> propertyMask;

        public bool IsValid => boneMask != null || propertyMask != null;

        public WeightedAvatarMask.WeightedEntry this[int index]
        {
            get => boneMask[index];
            set => boneMask[index] = value;
        }

        public float this[string boneName]
        {
            get => Get(boneName).weight;
            set => Set(boneName, value);
        }

        public int BoneCount => boneMask == null ? 0 : boneMask.Count;
        public int PropertyCount => propertyMask == null ? 0 : propertyMask.Count;

        public WeightedAvatarMask.WeightedEntry Get(string boneName, float defaultWeight = 0f, bool invert = false)
        {
            if (boneMask == null) return default;

            foreach (var bm in boneMask) if (bm.entryName == boneName) return invert ? new WeightedAvatarMask.WeightedEntry() { entryName = bm.entryName, weight = 1 - bm.weight } : bm;
            return new WeightedAvatarMask.WeightedEntry() { entryName = boneName, weight = invert ? 1f - defaultWeight : defaultWeight };
        }
        public void Set(WeightedAvatarMask.WeightedEntry bm) => Set(bm.entryName, bm.weight);
        public void Set(string boneName, float weight)
        {
            if (boneMask == null) boneMask = new List<WeightedAvatarMask.WeightedEntry>();

            for (int a = 0; a < boneMask.Count; a++)
            {
                var bm = boneMask[a];
                if (bm.entryName == boneName)
                {
                    bm.weight = weight;
                    boneMask[a] = bm;
                }
            }
        }
        public bool Contains(string boneName)
        {
            if (boneMask == null) return false;

            foreach (var bm in boneMask) if (bm.entryName == boneName) return true;
            return false;
        }


        public WeightedAvatarMask.WeightedEntry GetProperty(string propertyId, float defaultWeight = 0f, bool invert = false)
        {
            if (propertyMask == null) return default;

            foreach (var pm in propertyMask) if (pm.entryName == propertyId) return invert ? new WeightedAvatarMask.WeightedEntry() { entryName = pm.entryName, weight = 1 - pm.weight } : pm;
            return new WeightedAvatarMask.WeightedEntry() { entryName = propertyId, weight = invert ? 1f - defaultWeight : defaultWeight };
        }
        public void SetProperty(WeightedAvatarMask.WeightedEntry pm) => SetProperty(pm.entryName, pm.weight);
        public void SetProperty(string propertyId, float weight)
        {
            if (propertyMask == null) propertyMask = new List<WeightedAvatarMask.WeightedEntry>();

            for (int a = 0; a < propertyMask.Count; a++)
            {
                var bm = propertyMask[a];
                if (bm.entryName == propertyId)
                {
                    bm.weight = weight;
                    propertyMask[a] = bm;
                }
            }
        }
        public bool ContainsProperty(string propertyName)
        {
            if (propertyMask == null) return false;

            foreach (var pm in propertyMask) if (pm.entryName == propertyName) return true;
            return false;
        }

        public static WeightedAvatarMaskComposite operator -(WeightedAvatarMaskComposite a)
        {
            var comp = new WeightedAvatarMaskComposite();

            comp.boneMask = a.boneMask == null ? new List<WeightedAvatarMask.WeightedEntry>() : new List<WeightedAvatarMask.WeightedEntry>(a.boneMask);
            comp.propertyMask = a.propertyMask == null ? new List<WeightedAvatarMask.WeightedEntry>() : new List<WeightedAvatarMask.WeightedEntry>(a.propertyMask);

            for (int i = 0; i < comp.boneMask.Count; i++)
            {
                var val = comp.boneMask[i];

                val.weight = -val.weight;
                comp.boneMask[i] = val;
            }
            for (int i = 0; i < comp.propertyMask.Count; i++)
            {
                var val = comp.propertyMask[i];

                val.weight = -val.weight;
                comp.propertyMask[i] = val;
            }

            return comp;
        }
        public static WeightedAvatarMaskComposite operator +(WeightedAvatarMaskComposite a, WeightedAvatarMask b) => a + b.AsComposite(true);
        public static WeightedAvatarMaskComposite operator +(WeightedAvatarMaskComposite a, WeightedAvatarMaskComposite b)
        {
            var comp = new WeightedAvatarMaskComposite();

            comp.boneMask = a.boneMask == null ? new List<WeightedAvatarMask.WeightedEntry>() : new List<WeightedAvatarMask.WeightedEntry>(a.boneMask);
            comp.propertyMask = a.propertyMask == null ? new List<WeightedAvatarMask.WeightedEntry>() : new List<WeightedAvatarMask.WeightedEntry>(a.propertyMask);

            if (b.boneMask != null)
            {
                foreach(var val in b.boneMask)
                {
                    bool flag = false;
                    for(int i = 0; i < comp.boneMask.Count; i++)
                    {
                        var val2 = comp.boneMask[i];
                        if (val2.entryName != val.entryName) continue;

                        val2.weight = val.weight + val2.weight;
                        comp.boneMask[i] = val2;
                        flag = true;
                    }
                    if (!flag) comp.boneMask.Add(val);
                }
            }
            if (b.propertyMask != null)
            {
                foreach (var val in b.propertyMask)
                {
                    bool flag = false;
                    for (int i = 0; i < comp.propertyMask.Count; i++)
                    {
                        var val2 = comp.propertyMask[i];
                        if (val2.entryName != val.entryName) continue;

                        val2.weight = val.weight + val2.weight;
                        comp.propertyMask[i] = val2;
                        flag = true;
                    }
                    if (!flag) comp.propertyMask.Add(val);
                }
            }

            return comp;
        }
        public static WeightedAvatarMaskComposite operator -(WeightedAvatarMaskComposite a, WeightedAvatarMask b) => a - b.AsComposite(true);
        public static WeightedAvatarMaskComposite operator -(WeightedAvatarMaskComposite a, WeightedAvatarMaskComposite b)
        {
            var comp = new WeightedAvatarMaskComposite();

            comp.boneMask = a.boneMask == null ? new List<WeightedAvatarMask.WeightedEntry>() : new List<WeightedAvatarMask.WeightedEntry>(a.boneMask);
            comp.propertyMask = a.propertyMask == null ? new List<WeightedAvatarMask.WeightedEntry>() : new List<WeightedAvatarMask.WeightedEntry>(a.propertyMask);

            if (b.boneMask != null)
            {
                foreach (var val in b.boneMask)
                {
                    bool flag = false;
                    for (int i = 0; i < comp.boneMask.Count; i++)
                    {
                        var val2 = comp.boneMask[i];
                        if (val2.entryName != val.entryName) continue;

                        val2.weight = val.weight - val2.weight;
                        comp.boneMask[i] = val2;
                        flag = true;
                    }
                    if (!flag) comp.boneMask.Add(-val);
                }
            }
            if (b.propertyMask != null)
            {
                foreach (var val in b.propertyMask)
                {
                    bool flag = false;
                    for (int i = 0; i < comp.propertyMask.Count; i++)
                    {
                        var val2 = comp.propertyMask[i];
                        if (val2.entryName != val.entryName) continue;

                        val2.weight = val.weight - val2.weight;
                        comp.propertyMask[i] = val2;
                        flag = true;
                    }
                    if (!flag) comp.propertyMask.Add(-val);
                }
            }

            return comp;
        }

        public static WeightedAvatarMaskComposite operator *(WeightedAvatarMaskComposite a, WeightedAvatarMask b) => a * b.AsComposite(true);
        public static WeightedAvatarMaskComposite operator *(WeightedAvatarMaskComposite a, WeightedAvatarMaskComposite b)
        {
            var comp = new WeightedAvatarMaskComposite();

            comp.boneMask = a.boneMask == null ? new List<WeightedAvatarMask.WeightedEntry>() : new List<WeightedAvatarMask.WeightedEntry>(a.boneMask);
            comp.propertyMask = a.propertyMask == null ? new List<WeightedAvatarMask.WeightedEntry>() : new List<WeightedAvatarMask.WeightedEntry>(a.propertyMask);

            if (b.boneMask != null)
            {
                foreach (var val in b.boneMask)
                {
                    for (int i = 0; i < comp.boneMask.Count; i++)
                    {
                        var val2 = comp.boneMask[i];
                        if (val2.entryName != val.entryName) continue;

                        val2.weight = val.weight * val2.weight;
                        comp.boneMask[i] = val2;
                    }
                }
            }
            if (b.propertyMask != null)
            {
                foreach (var val in b.propertyMask)
                {
                    for (int i = 0; i < comp.propertyMask.Count; i++)
                    {
                        var val2 = comp.propertyMask[i];
                        if (val2.entryName != val.entryName) continue;

                        val2.weight = val.weight * val2.weight;
                        comp.propertyMask[i] = val2;
                    }
                }
            }

            return comp;
        }

        public static WeightedAvatarMaskComposite operator /(WeightedAvatarMaskComposite a, WeightedAvatarMask b) => a / b.AsComposite(true);
        public static WeightedAvatarMaskComposite operator /(WeightedAvatarMaskComposite a, WeightedAvatarMaskComposite b)
        {
            var comp = new WeightedAvatarMaskComposite();

            comp.boneMask = a.boneMask == null ? new List<WeightedAvatarMask.WeightedEntry>() : new List<WeightedAvatarMask.WeightedEntry>(a.boneMask);
            comp.propertyMask = a.propertyMask == null ? new List<WeightedAvatarMask.WeightedEntry>() : new List<WeightedAvatarMask.WeightedEntry>(a.propertyMask);

            if (b.boneMask != null)
            {
                foreach (var val in b.boneMask)
                {
                    for (int i = 0; i < comp.boneMask.Count; i++)
                    {
                        var val2 = comp.boneMask[i];
                        if (val2.entryName != val.entryName) continue;

                        val2.weight = val.weight / val2.weight;
                        comp.boneMask[i] = val2;
                    }
                }
            }
            if (b.propertyMask != null)
            {
                foreach (var val in b.propertyMask)
                {
                    for (int i = 0; i < comp.propertyMask.Count; i++)
                    {
                        var val2 = comp.propertyMask[i];
                        if (val2.entryName != val.entryName) continue;

                        val2.weight = val.weight / val2.weight;
                        comp.propertyMask[i] = val2;
                    }
                }
            }

            return comp;
        }

        public static WeightedAvatarMaskComposite operator +(WeightedAvatarMaskComposite a, float b)
        {
            var comp = new WeightedAvatarMaskComposite();

            if (a.boneMask != null)
            {
                comp.boneMask = new List<WeightedAvatarMask.WeightedEntry>();

                foreach (var val in a.boneMask)
                {
                    comp.boneMask.Add(val + b);
                }
            }
            if (a.propertyMask != null)
            {
                comp.propertyMask = new List<WeightedAvatarMask.WeightedEntry>();

                foreach (var val in a.propertyMask)
                {
                    comp.propertyMask.Add(val + b);
                }
            }

            return comp;
        }
        public static WeightedAvatarMaskComposite operator -(WeightedAvatarMaskComposite a, float b)
        {
            var comp = new WeightedAvatarMaskComposite();

            if (a.boneMask != null)
            {
                comp.boneMask = new List<WeightedAvatarMask.WeightedEntry>();

                foreach (var val in a.boneMask)
                {
                    comp.boneMask.Add(val - b);
                }
            }
            if (a.propertyMask != null)
            {
                comp.propertyMask = new List<WeightedAvatarMask.WeightedEntry>();

                foreach (var val in a.propertyMask)
                {
                    comp.propertyMask.Add(val - b);
                }
            }

            return comp;
        }

        public static WeightedAvatarMaskComposite operator +(float a, WeightedAvatarMaskComposite b)
        {
            var comp = new WeightedAvatarMaskComposite();

            if (b.boneMask != null)
            {
                comp.boneMask = new List<WeightedAvatarMask.WeightedEntry>();

                foreach (var val in b.boneMask)
                {
                    comp.boneMask.Add(a + val);
                }
            }
            if (b.propertyMask != null)
            {
                comp.propertyMask = new List<WeightedAvatarMask.WeightedEntry>();

                foreach (var val in b.propertyMask)
                {
                    comp.propertyMask.Add(a + val);
                }
            }

            return comp;
        }
        public static WeightedAvatarMaskComposite operator -(float a, WeightedAvatarMaskComposite b)
        {
            var comp = new WeightedAvatarMaskComposite();

            if (b.boneMask != null)
            {
                comp.boneMask = new List<WeightedAvatarMask.WeightedEntry>();

                foreach (var val in b.boneMask)
                {
                    comp.boneMask.Add(a - val);
                }
            }
            if (b.propertyMask != null)
            {
                comp.propertyMask = new List<WeightedAvatarMask.WeightedEntry>();

                foreach (var val in b.propertyMask)
                {
                    comp.propertyMask.Add(a - val);
                }
            }

            return comp;
        }

        public static WeightedAvatarMaskComposite operator *(WeightedAvatarMaskComposite a, float b)
        {
            var comp = new WeightedAvatarMaskComposite();

            if (a.boneMask != null)
            {
                comp.boneMask = new List<WeightedAvatarMask.WeightedEntry>();

                foreach (var val in a.boneMask)
                {
                    comp.boneMask.Add(val * b);
                }
            }
            if (a.propertyMask != null)
            {
                comp.propertyMask = new List<WeightedAvatarMask.WeightedEntry>();

                foreach (var val in a.propertyMask)
                {
                    comp.propertyMask.Add(val * b);
                }
            }

            return comp;
        }

        public static WeightedAvatarMaskComposite operator /(WeightedAvatarMaskComposite a, float b)
        {
            var comp = new WeightedAvatarMaskComposite();

            if (a.boneMask != null)
            {
                comp.boneMask = new List<WeightedAvatarMask.WeightedEntry>();

                foreach (var val in a.boneMask)
                {
                    comp.boneMask.Add(val / b);
                }
            }
            if (a.propertyMask != null)
            {
                comp.propertyMask = new List<WeightedAvatarMask.WeightedEntry>();

                foreach (var val in a.propertyMask)
                {
                    comp.propertyMask.Add(val / b);
                }
            }

            return comp;
        }
    }
}