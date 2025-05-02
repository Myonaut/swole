namespace Swole
{

    public interface IAvatarAsset<T1, T2> : IAvatarAsset, ISwoleSerialization<T1, T2> where T1 : IAvatarAsset where T2 : struct, ISerializableContainer<T1, T2>
    {
    }
    public interface IAvatarAsset : IContent, ISwoleSerialization
    {
    }

}
