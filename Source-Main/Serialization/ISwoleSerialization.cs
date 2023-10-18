namespace Swole
{

    public interface ISwoleSerialization<TMain, TSerialized> : ISwoleSerialization, ISwoleSerializable where TSerialized : struct, ISerializableContainer<TMain, TSerialized>
    {

        public TSerialized AsSerializableStruct();

    }

    public interface ISwoleSerialization
    {

        public object AsSerializableObject();

    }

}
