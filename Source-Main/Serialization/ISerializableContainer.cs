namespace Swole
{

    public interface ISerializableContainer<TMain, TSerialized> : ISerializableContainer, ISwoleSerializable where TSerialized : struct, ISerializableContainer<TMain, TSerialized>
    {

        public TMain AsOriginalType(PackageInfo packageInfo = default);

    }

    public interface ISerializableContainer
    {

        public object AsNonserializableObject(PackageInfo packageInfo = default);

    }

}
