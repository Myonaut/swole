namespace Swolescript
{

    public class SwoleVarLinked<T> : SwoleVar<T>
    {

        public delegate T LinkedValueDelegate();

        private readonly LinkedValueDelegate getLinkedValue;

        public SwoleVarLinked(string name, LinkedValueDelegate getLinkedValue) : base(name, getLinkedValue == null ? default : getLinkedValue())
        {
            this.getLinkedValue = getLinkedValue;
        }

        public override T GetValue()
        {
            value = getLinkedValue == null ? default : getLinkedValue();
            return value;
        }
        public override void SetValue(T val) { }

        public static implicit operator T(SwoleVarLinked<T> v) => v.GetValue();

    }

}
