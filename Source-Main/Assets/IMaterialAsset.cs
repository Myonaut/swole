using System;
using System.Collections.Generic;

namespace Swole
{
    public interface IMaterialAsset<T1, T2> : IMaterialAsset, ISwoleSerialization<T1, T2> where T1 : IMaterialAsset where T2 : struct, ISerializableContainer<T1, T2>
    {
    }
    public interface IMaterialAsset : IContent, ISwoleSerialization, EngineInternal.IEngineObject
    {
        public IEnumerable<MaterialFloatPropertyOverride> FloatPropertyOverrides { get; } 
        public int FloatPropertyOverrideCount { get; }
        public MaterialFloatPropertyOverride GetFloatPropertyOverride(int index);
        public void SetFloatPropertyOverride(MaterialFloatPropertyOverride value);
        public void RemoveFloatPropertyOverride(string propertyName);

        public IEnumerable<MaterialVectorPropertyOverride> VectorPropertyOverrides { get; }
        public int VectorPropertyOverrideCount { get; }
        public MaterialVectorPropertyOverride GetVectorPropertyOverride(int index);
        public void SetVectorPropertyOverride(MaterialVectorPropertyOverride value);
        public void RemoveVectorPropertyOverride(string propertyName);

        public IEnumerable<MaterialTexturePropertyOverride> TexturePropertyOverrides { get; }
        public int TexturePropertyOverrideCount { get; }
        public MaterialTexturePropertyOverride GetTexturePropertyOverride(int index);
        public void SetTexturePropertyOverride(MaterialTexturePropertyOverride value);
        public void RemoveTexturePropertyOverride(string propertyName);

        public IEnumerable<MaterialColorPropertyOverride> ColorPropertyOverrides { get; }
        public int ColorPropertyOverrideCount { get; }
        public MaterialColorPropertyOverride GetColorPropertyOverride(int index);
        public void SetColorPropertyOverride(MaterialColorPropertyOverride value);
        public void RemoveColorPropertyOverride(string propertyName);
    }

    [Serializable]
    public struct MaterialFloatPropertyOverride
    {
        public string propertyName; 
        public float value;
    }
    [Serializable]
    public struct MaterialVectorPropertyOverride
    {
        public string propertyName;
        public EngineInternal.Vector4 value; 
    }
    [Serializable]
    public struct MaterialTexturePropertyOverride
    {
        public string propertyName;
        public string textureAssetPath;
    }
    [Serializable]
    public struct MaterialColorPropertyOverride
    {
        public string propertyName;

        public float red;
        public float blue;
        public float green;
        public float alpha;
    }

}
