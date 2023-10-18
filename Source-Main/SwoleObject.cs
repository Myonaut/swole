namespace Swole
{

    public abstract class SwoleObject<TMain, TSerialized> : ISwoleSerialization<TMain, TSerialized> where TSerialized : struct, ISerializableContainer<TMain, TSerialized>
    {

        public SwoleObject(TSerialized serializable) { }

        public abstract TSerialized AsSerializableStruct();
        public object AsSerializableObject() => AsSerializableStruct();

        public abstract string AsJSON(bool prettyPrint = false);

    }

}
