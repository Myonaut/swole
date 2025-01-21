#if (UNITY_STANDALONE || UNITY_EDITOR)
#define IS_UNITY
#endif

using System;

namespace Swole
{
    public interface ICurve : ICloneable
    {

        public string Name { get; set; }
        public CurveType Type { get; }
        public float Evaluate(float t);
        public EngineInternal.Vector2 Evaluate2(float t);
        public EngineInternal.Vector3 Evaluate3(float t);

        public ISwoleSerializable Serialize();

    }

    [Serializable]
    public enum CurveType
    {
        Bezier, Animation
    }

    [Serializable]
    public struct SerializedCurve : ISerializableContainer<ICurve, SerializedCurve>
    {

        public string name;
        public string SerializedName => name;

        public CurveType curveType;
        public EngineInternal.Vector3[] points;  

        public string AsJSON(bool prettyPrint = false) => swole.Engine.ToJson(this, prettyPrint);

        public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);

        public ICurve AsOriginalType(PackageInfo packageInfo = default)
        {
#if BULKOUT_ENV
            if (curveType == CurveType.Bezier)
            {
                if (points != null)
                {
                    try
                    {
                        return new Swole.API.Unity.ExternalBezierCurve(this);
                    }
                    catch (Exception e)
                    {
                        swole.LogError(e);
                    }
                }
            }
#endif
            return null;
        }
    }

}
