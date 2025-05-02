#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Swole.Script;

namespace Swole.API.Unity
{
    [CreateAssetMenu(fileName = "newMaterialAsset", menuName = "Swole/Assets/MaterialAsset", order = 0)]
    public class MaterialAsset : ScriptableObject, IMaterialAsset<MaterialAsset, MaterialAsset.Serialized>
    {

#if UNITY_EDITOR
        public void OnValidate()
        {
            isInternalAsset = true;
        }
#endif

        public System.Type AssetType => typeof(Material);
        public object Asset => material;
        public static implicit operator Material(MaterialAsset asset) => asset.Material;

        public static MaterialAsset NewInstance() => ScriptableObject.CreateInstance<MaterialAsset>();
        public static MaterialAsset NewInstance(ContentInfo contentInfo, Material material, PackageInfo packageInfo = default)
        {
            var inst = NewInstance();

            inst.contentInfo = contentInfo;
            inst.packageInfo = packageInfo;

            inst.material = material;

            return inst;
        }
        public static MaterialAsset NewInstance(string name, string author, DateTime creationDate, DateTime lastEditDate, string description, Material material, PackageInfo packageInfo = default) =>
            NewInstance(new ContentInfo() { name = name, author = author, creationDate = creationDate.ToString(IContent.dateFormat), lastEditDate = lastEditDate.ToString(IContent.dateFormat), description = description }, material, packageInfo);

        public static MaterialAsset NewInstance(string name, string author, string creationDate, string lastEditDate, string description, Material material, PackageInfo packageInfo = default) =>
            NewInstance(new ContentInfo() { name = name, author = author, creationDate = creationDate, lastEditDate = lastEditDate, description = description }, material, packageInfo);

        public static MaterialAsset NewInstance(ContentInfo contentInfo, string baseMaterialID, PackageInfo packageInfo = default)
        {
            var inst = NewInstance();

            inst.contentInfo = contentInfo;
            inst.packageInfo = packageInfo;

            inst.baseMaterialID = baseMaterialID;

            return inst;
        }
        public static MaterialAsset NewInstance(string name, string author, DateTime creationDate, DateTime lastEditDate, string description, string baseMaterialID, PackageInfo packageInfo = default) =>
            NewInstance(new ContentInfo() { name = name, author = author, creationDate = creationDate.ToString(IContent.dateFormat), lastEditDate = lastEditDate.ToString(IContent.dateFormat), description = description }, baseMaterialID, packageInfo);

        public static MaterialAsset NewInstance(string name, string author, string creationDate, string lastEditDate, string description, string baseMaterialID, PackageInfo packageInfo = default) =>
            NewInstance(new ContentInfo() { name = name, author = author, creationDate = creationDate, lastEditDate = lastEditDate, description = description }, baseMaterialID, packageInfo);

        [SerializeField]
        public string baseMaterialID;

        [SerializeField]
        protected Material baseMaterial;
        public Material BaseMaterial
        {
            get
            {
                if (baseMaterial == null)
                {
                    if (material != null) return material;

                    ResourceLib.ResolveAssetIdString(baseMaterialID, out string assetName, out string collectionName, out bool isPackage);
                    baseMaterial = ResourceLib.FindMaterial(assetName, collectionName, isPackage, false);
                }

                return baseMaterial;
            }
        }

        [NonSerialized]
        protected Material material;
        public Material Material
        {
            get
            {
                if (material == null)
                {
                    if (baseMaterial != null)
                    {
                        material = UnityEngine.Material.Instantiate(baseMaterial);
                        material.name = Name;

                        ReapplyMaterialOverrides();
                    }
                }

                return material; 
            }
        }

        #region IMaterialAsset

        protected List<MaterialFloatPropertyOverride> floatPropertyOverrides; 
        public IEnumerable<MaterialFloatPropertyOverride> FloatPropertyOverrides => floatPropertyOverrides; 
        public int FloatPropertyOverrideCount => floatPropertyOverrides == null ? 0 : floatPropertyOverrides.Count;
        public MaterialFloatPropertyOverride GetFloatPropertyOverride(int index) => floatPropertyOverrides == null ? default : floatPropertyOverrides[index];
        public void SetFloatPropertyOverride(MaterialFloatPropertyOverride value) 
        {
            if (floatPropertyOverrides == null) return;
            for (int a = 0; a < floatPropertyOverrides.Count; a++)
            {
                var prop = floatPropertyOverrides[a];
                if (prop.propertyName != value.propertyName) continue;

                floatPropertyOverrides[a] = value;
                if (material != null && material.HasFloat(value.propertyName)) material.SetFloat(value.propertyName, value.value);
                return;
            }

            floatPropertyOverrides.Add(value);
            if (material != null && material.HasFloat(value.propertyName)) material.SetFloat(value.propertyName, value.value);
        }
        public void RemoveFloatPropertyOverride(string propertyName)
        {
            if (floatPropertyOverrides == null) return;
            floatPropertyOverrides.RemoveAll(i => i.propertyName == propertyName);
        }

        protected List<MaterialVectorPropertyOverride> vectorPropertyOverrides;
        public IEnumerable<MaterialVectorPropertyOverride> VectorPropertyOverrides => vectorPropertyOverrides;
        public int VectorPropertyOverrideCount => vectorPropertyOverrides == null ? 0 : vectorPropertyOverrides.Count;
        public MaterialVectorPropertyOverride GetVectorPropertyOverride(int index) => vectorPropertyOverrides == null ? default : vectorPropertyOverrides[index];
        public void SetVectorPropertyOverride(MaterialVectorPropertyOverride value)
        {
            if (vectorPropertyOverrides == null) return;
            for (int a = 0; a < vectorPropertyOverrides.Count; a++)
            {
                var prop = vectorPropertyOverrides[a];
                if (prop.propertyName != value.propertyName) continue;

                vectorPropertyOverrides[a] = value;
                if (material != null && material.HasVector(value.propertyName)) material.SetVector(value.propertyName, UnityEngineHook.AsUnityVector(value.value));
                return;
            }

            vectorPropertyOverrides.Add(value);
            if (material != null && material.HasVector(value.propertyName)) material.SetVector(value.propertyName, UnityEngineHook.AsUnityVector(value.value));
        }
        public void RemoveVectorPropertyOverride(string propertyName)
        {
            if (vectorPropertyOverrides == null) return;
            vectorPropertyOverrides.RemoveAll(i => i.propertyName == propertyName);
        }

        protected List<MaterialColorPropertyOverride> colorPropertyOverrides;
        public IEnumerable<MaterialColorPropertyOverride> ColorPropertyOverrides => colorPropertyOverrides;
        public int ColorPropertyOverrideCount => colorPropertyOverrides == null ? 0 : colorPropertyOverrides.Count;
        public MaterialColorPropertyOverride GetColorPropertyOverride(int index) => colorPropertyOverrides == null ? default : colorPropertyOverrides[index];
        public void SetColorPropertyOverride(MaterialColorPropertyOverride value)
        {
            if (colorPropertyOverrides == null) return;
            for (int a = 0; a < colorPropertyOverrides.Count; a++)
            {
                var prop = colorPropertyOverrides[a];
                if (prop.propertyName != value.propertyName) continue;

                colorPropertyOverrides[a] = value;
                if (material != null && material.HasColor(value.propertyName)) material.SetColor(value.propertyName, new Color(value.red, value.green, value.blue, value.alpha));
                return;
            }

            colorPropertyOverrides.Add(value);
            if (material != null && material.HasColor(value.propertyName)) material.SetColor(value.propertyName, new Color(value.red, value.green, value.blue, value.alpha));
        }
        public void RemoveColorPropertyOverride(string propertyName)
        {
            if (colorPropertyOverrides == null) return;
            colorPropertyOverrides.RemoveAll(i => i.propertyName == propertyName);
        }

        protected List<MaterialTexturePropertyOverride> texturePropertyOverrides;
        public IEnumerable<MaterialTexturePropertyOverride> TexturePropertyOverrides => texturePropertyOverrides;
        public int TexturePropertyOverrideCount => texturePropertyOverrides == null ? 0 : texturePropertyOverrides.Count;
        public MaterialTexturePropertyOverride GetTexturePropertyOverride(int index) => texturePropertyOverrides == null ? default : texturePropertyOverrides[index];
        public void SetTexturePropertyOverride(MaterialTexturePropertyOverride value)
        {
            if (texturePropertyOverrides == null) return;
            for (int a = 0; a < texturePropertyOverrides.Count; a++)
            {
                var prop = texturePropertyOverrides[a];
                if (prop.propertyName != value.propertyName) continue;   

                texturePropertyOverrides[a] = value;
                if (material != null && material.HasTexture(value.propertyName))
                {
                    ResourceLib.ResolveAssetIdString(value.textureAssetPath, out string assetName, out string collectionName, out bool isConfirmedPackage);
                    material.SetTexture(value.propertyName, ResourceLib.FindImage(assetName, collectionName, isConfirmedPackage, false, PackageInfo));
                }
                return;
            }

            texturePropertyOverrides.Add(value);
            if (material != null && material.HasTexture(value.propertyName))
            {
                ResourceLib.ResolveAssetIdString(value.textureAssetPath, out string assetName, out string collectionName, out bool isConfirmedPackage);
                material.SetTexture(value.propertyName, ResourceLib.FindImage(assetName, collectionName, isConfirmedPackage, false, PackageInfo)); 
            }
        }
        public void RemoveTexturePropertyOverride(string propertyName)
        {
            if (texturePropertyOverrides == null) return;
            texturePropertyOverrides.RemoveAll(i => i.propertyName == propertyName);
        }

        public void ReapplyMaterialOverrides()
        {
            if (material == null) return;

            if (floatPropertyOverrides != null)
            {
                foreach (var ovr in floatPropertyOverrides) if (material.HasFloat(ovr.propertyName)) material.SetFloat(ovr.propertyName, ovr.value);
                foreach (var ovr in vectorPropertyOverrides) if (material.HasVector(ovr.propertyName)) material.SetVector(ovr.propertyName, UnityEngineHook.AsUnityVector(ovr.value));
                foreach (var ovr in colorPropertyOverrides) if (material.HasColor(ovr.propertyName)) material.SetColor(ovr.propertyName, new Color(ovr.red, ovr.blue, ovr.green, ovr.alpha));
                foreach (var ovr in texturePropertyOverrides)
                {
                    if (material.HasTexture(ovr.propertyName)) 
                    {
                        ResourceLib.ResolveAssetIdString(ovr.textureAssetPath, out string assetName, out string collectionName, out bool isConfirmedPackage);
                        material.SetTexture(ovr.propertyName, ResourceLib.FindImage(assetName, collectionName, isConfirmedPackage, false, PackageInfo));
                    }
                }
            }
        }

        #endregion

        #region IEngineObject

        public object Instance => Material;

        public int InstanceID => material == null ? 0 : material.GetInstanceID();

        public bool IsDestroyed => material == null;

        public bool HasEventHandler => false;

        public IRuntimeEventHandler EventHandler => default;

        public void AdminDestroy(float timeDelay = 0) => swole.Engine.Object_AdminDestroy(this, timeDelay);

        public void Destroy(float timeDelay = 0) => swole.Engine.Object_Destroy(this, timeDelay);

        #endregion

        #region IContent

        [SerializeField]
        protected bool isInternalAsset;
        public bool IsInternalAsset
        {
            get => isInternalAsset;
            set => isInternalAsset = value;
        }

        [SerializeField]
        protected string collectionId;
        public string CollectionID
        {
            get => collectionId;
            set => collectionId = value;
        }
        public bool HasCollectionID => !string.IsNullOrWhiteSpace(CollectionID);

        public bool IsValid => material != null;
        public void Dispose()
        {
            if (!IsInternalAsset && material != null)
            {
                GameObject.Destroy(material);
            }

            DisposeSelf();
        }
        public void DisposeSelf()
        {
            material = null;
            UnityEngine.Object.Destroy(this);
        }
        public void Delete()
        {
            Dispose();
        }

        [NonSerialized]
        public string originPath;
        public string OriginPath => originPath;
        public IContent SetOriginPath(string path)
        {
            var content = this;
            content.originPath = path;
            return content;
        }

        [NonSerialized]
        public string relativePath;
        public string RelativePath => relativePath;
        public IContent SetRelativePath(string path)
        {
            var content = this;
            content.relativePath = path;
            return content;
        }

        public string Name => contentInfo.name;
        public string Author => contentInfo.author;
        public string CreationDate => contentInfo.creationDate;
        public string LastEditDate => contentInfo.lastEditDate;
        public string Description => contentInfo.description;

        public PackageInfo packageInfo;
        public PackageInfo PackageInfo => packageInfo;

        public ContentInfo contentInfo;
        public ContentInfo ContentInfo => contentInfo;
        public string SerializedName => contentInfo.name;

        public IContent CreateCopyAndReplaceContentInfo(ContentInfo info)
        {
            Material mat = null;
            if (material != null) 
            {
                mat = UnityEngine.Material.Instantiate(material);
                mat.name = material.name;
            }
            var content = NewInstance(info, mat, packageInfo);
            content.floatPropertyOverrides = floatPropertyOverrides == null ? null : new List<MaterialFloatPropertyOverride>(floatPropertyOverrides);
            content.vectorPropertyOverrides = vectorPropertyOverrides == null ? null : new List<MaterialVectorPropertyOverride>(vectorPropertyOverrides);
            content.colorPropertyOverrides = colorPropertyOverrides == null ? null : new List<MaterialColorPropertyOverride>(colorPropertyOverrides);
            content.texturePropertyOverrides = texturePropertyOverrides == null ? null : new List<MaterialTexturePropertyOverride>(texturePropertyOverrides);

            return content;
        }
        public IContent CreateShallowCopyAndReplaceContentInfo(ContentInfo info)
        {
            var content = NewInstance(info, material, packageInfo);
            content.floatPropertyOverrides = floatPropertyOverrides;
            content.vectorPropertyOverrides = vectorPropertyOverrides;
            content.colorPropertyOverrides = colorPropertyOverrides;
            content.texturePropertyOverrides = texturePropertyOverrides; 

            return content;
        }

        public List<PackageIdentifier> ExtractPackageDependencies(List<PackageIdentifier> dependencies = null)
        {
            if (dependencies == null) dependencies = new List<PackageIdentifier>();
            return dependencies;
        }

        public bool IsIdenticalAsset(ISwoleAsset otherAsset) => ReferenceEquals(this, otherAsset) || (otherAsset is IMaterialAsset asset && ReferenceEquals(Instance, asset.Instance));

        #endregion

        #region Serialization

        public string AsJSON(bool prettyPrint = false) => AsSerializableStruct().AsJSON(prettyPrint);

        [Serializable]
        public struct Serialized : ISerializableContainer<MaterialAsset, MaterialAsset.Serialized>
        {

            public ContentInfo contentInfo;

            public string baseMaterialID;
            public MaterialFloatPropertyOverride[] floatPropertyOverrides;
            public MaterialVectorPropertyOverride[] vectorPropertyOverrides;
            public MaterialColorPropertyOverride[] colorPropertyOverrides;
            public MaterialTexturePropertyOverride[] texturePropertyOverrides;
            
            public string SerializedName => contentInfo.name;

            public MaterialAsset AsOriginalType(PackageInfo packageInfo = default) => NewInstance(this, packageInfo);
            public string AsJSON(bool prettyPrint = false) => swole.Engine.ToJson(this, prettyPrint);

            public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);

        }

        public static implicit operator Serialized(MaterialAsset asset)
        {
            Serialized s = new Serialized();

            s.contentInfo = asset.contentInfo;

            s.baseMaterialID = asset.baseMaterialID;
            s.floatPropertyOverrides = asset.floatPropertyOverrides == null ? new MaterialFloatPropertyOverride[0] : asset.floatPropertyOverrides.ToArray();
            s.vectorPropertyOverrides = asset.vectorPropertyOverrides == null ? new MaterialVectorPropertyOverride[0] : asset.vectorPropertyOverrides.ToArray();
            s.colorPropertyOverrides = asset.colorPropertyOverrides == null ? new MaterialColorPropertyOverride[0] : asset.colorPropertyOverrides.ToArray();
            s.texturePropertyOverrides = asset.texturePropertyOverrides == null ? new MaterialTexturePropertyOverride[0] : asset.texturePropertyOverrides.ToArray();
              
            return s;
        }

        public MaterialAsset.Serialized AsSerializableStruct() => this;
        public object AsSerializableObject() => AsSerializableStruct();

        public static MaterialAsset NewInstance(MaterialAsset.Serialized serializable, PackageInfo packageInfo = default)
        {
            var inst = NewInstance();

            inst.packageInfo = packageInfo;
            inst.contentInfo = serializable.contentInfo;

            inst.baseMaterialID = serializable.baseMaterialID;
            if (serializable.floatPropertyOverrides != null && serializable.floatPropertyOverrides.Length > 0) inst.floatPropertyOverrides = new List<MaterialFloatPropertyOverride>(serializable.floatPropertyOverrides);
            if (serializable.vectorPropertyOverrides != null && serializable.vectorPropertyOverrides.Length > 0) inst.vectorPropertyOverrides = new List<MaterialVectorPropertyOverride>(serializable.vectorPropertyOverrides);
            if (serializable.colorPropertyOverrides != null && serializable.colorPropertyOverrides.Length > 0) inst.colorPropertyOverrides = new List<MaterialColorPropertyOverride>(serializable.colorPropertyOverrides);
            if (serializable.texturePropertyOverrides != null && serializable.texturePropertyOverrides.Length > 0) inst.texturePropertyOverrides = new List<MaterialTexturePropertyOverride>(serializable.texturePropertyOverrides);

            return inst;
        }

        #endregion

    }
}

#endif