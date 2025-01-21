using System;

namespace Swole
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class AnimatablePropertyAttribute : System.Attribute
    {
        public bool hasDefaultValue;
        public float defaultValue;
        public AnimatablePropertyAttribute(bool hasDefaultValue = false, float defaultValue = 0)
        {
            this.hasDefaultValue = hasDefaultValue;
            this.defaultValue = defaultValue;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class AnimatablePropertyPrefixAttribute : System.Attribute
    {
        public string prefix;
        public AnimatablePropertyPrefixAttribute(string prefix)
        {
            this.prefix = prefix;
        }
    }
}
