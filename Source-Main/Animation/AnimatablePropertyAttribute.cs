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
        public bool hideReferenceChain;
        public AnimatablePropertyPrefixAttribute(string prefix, bool hideReferenceChain = false)
        {
            this.prefix = prefix;
            this.hideReferenceChain = hideReferenceChain;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)] 
    public class NonAnimatableAttribute : System.Attribute
    {
        public NonAnimatableAttribute()
        {
        }
    }
}
