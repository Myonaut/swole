namespace Swolescript
{

    public struct DefaultHostData : IHostData
    {

        public string identifier;
        public string Identifier => identifier;

        public object data;
        public object Data => data;

        public override string ToString() => identifier;

    }

}
