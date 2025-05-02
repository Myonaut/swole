namespace Swole
{
    public interface IMeshAsset<T1, T2> : IMeshAsset, ISwoleSerialization<T1, T2> where T1 : IMeshAsset where T2 : struct, ISerializableContainer<T1, T2>
    {
    }
    public interface IMeshAsset : IContent, ISwoleSerialization, EngineInternal.IEngineObject
    {
    }
}
