namespace Swole
{

    public interface IPoseRigAsset<T1, T2> : IPoseRigAsset, ISwoleSerialization<T1, T2> where T1 : IPoseRigAsset where T2 : struct, ISerializableContainer<T1, T2>
    {
    }
    public interface IPoseRigAsset : IContent, ISwoleSerialization
    {
    }

}