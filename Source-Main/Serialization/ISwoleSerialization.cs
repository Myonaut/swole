namespace Swole
{

    public interface ISwoleSerialization<TMain, TSerialized> : ISwoleSerialization where TSerialized : struct, ISerializableContainer<TMain, TSerialized>
    {

        public TSerialized AsSerializableStruct();

    }

    public interface ISwoleSerialization : ISwoleSerializable
    {

        public object AsSerializableObject();

    }

}
