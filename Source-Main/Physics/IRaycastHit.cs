namespace Swole
{
    public interface IRaycastHit
    {

        public ICollider collider { get; }

        public int colliderInstanceID { get; }

        public EngineInternal.Vector3 point { get; set; }
        public EngineInternal.Vector3 normal { get; set; }
        public EngineInternal.Vector3 barycentricCoordinate { get; set; }

        public float distance { get; set; }

        public int triangleIndex { get; }

        public EngineInternal.Vector2 textureCoord { get; }

        public EngineInternal.Vector2 textureCoord2 { get; }

        public EngineInternal.ITransform transform { get; } 

        public IRigidbody rigidbody { get; }

        public EngineInternal.Vector2 lightmapCoord { get; }
    }
}
