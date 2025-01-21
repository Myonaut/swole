namespace Swole
{

    public interface ISwoleSerializable
    {

        public string SerializedName { get; }
        public string AsJSON(bool prettyPrint = false);

    }

}
