namespace Swole
{

    public interface IImageAsset<T1, T2> : IImageAsset, ISwoleSerialization<T1, T2> where T1 : IImageAsset where T2 : struct, ISerializableContainer<T1, T2>
    {
        public T1 Asset { get; }
    }
    public interface IImageAsset : IContent, ISwoleSerialization
    { 
        public object Instance { get; }
    }

}
