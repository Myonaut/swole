namespace Swole
{

    public interface IActorAsset<T1, T2> : IActorAsset, ISwoleSerialization<T1, T2> where T1 : IActorAsset where T2 : struct, ISerializableContainer<T1, T2>
    {
    }
    public interface IActorAsset : IContent, ISwoleSerialization
    {
    }

}