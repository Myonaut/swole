namespace Swole
{
    public class VarRef<T>
    {
        public T value;

        public VarRef(T value) 
        {
            this.value = value;
        }

        public static implicit operator T(VarRef<T> val) => val.value;
    }
}
