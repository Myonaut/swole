namespace Swole
{

    public abstract class SwoleObject<TMain, TSerialized> : ISwoleSerialization<TMain, TSerialized> where TSerialized : struct, ISerializableContainer<TMain, TSerialized>
    {

        protected SwoleObject(TSerialized serializable) { }

        public abstract TSerialized AsSerializableStruct();
        public object AsSerializableObject() => AsSerializableStruct();
        public abstract string SerializedName { get; }

        public abstract string AsJSON(bool prettyPrint = false);

    }

}
