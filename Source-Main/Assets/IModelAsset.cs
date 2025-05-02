namespace Swole
{

    public interface IModelAsset<T1, T2> : IModelAsset, ISwoleSerialization<T1, T2> where T1 : IModelAsset where T2 : struct, ISerializableContainer<T1, T2>
    {
    }
    public interface IModelAsset : IContent, ISwoleSerialization
    {
    }

}