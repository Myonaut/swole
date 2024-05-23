using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Swole.Script
{

    /// <summary>
    /// A proxy that can be used to set or get the value of a field or property of an object using reflection.
    /// </summary>
    public class SwoleVarLinkedInstance<T> : SwoleVarLinked<T>
    {

        private readonly bool valid;
        public bool IsValid => valid;

        private readonly object instance;

        private readonly List<MemberInfo> memberInfoChain;
        private readonly object[] memberValueChain;

        public delegate T ConvertToDelegate(object val);
        private readonly ConvertToDelegate convertTo;

        public delegate object ConvertFromDelegate(T val);
        private readonly ConvertFromDelegate convertFrom;

        public SwoleVarLinkedInstance(string name, object instance, ICollection<string> memberNameChain, ConvertToDelegate convertToMethod = null, ConvertFromDelegate convertFromMethod = null) : base(name, null)
        {
            this.instance = instance;
            this.convertTo = convertToMethod == null ? ((object val) => (T)Convert.ChangeType(val, typeof(T))) : convertToMethod;
            this.convertFrom = convertFromMethod == null ? ((T val) => val) : convertFromMethod;

            this.memberInfoChain = new List<MemberInfo>();
            memberValueChain = new object[memberNameChain.Count];

            object member = instance;
            System.Type memberType = instance.GetType();
            int i = 0;
            bool invalid = false;
            foreach(var memberName in memberNameChain)
            {
                MemberInfo memInfo = memberType.GetField(memberName); // Includes public static and public instance, and is case-sensitive.
                if (memInfo == null) memInfo = memberType.GetProperty(memberName); // Includes public static and public instance, and is case-sensitive.

                if (memInfo == null) 
                {
                    invalid = true;
                    break;
                }

                this.memberInfoChain.Add(memInfo);
                if (memInfo is FieldInfo fieldInfo)
                {
                    memberValueChain[i] = member = fieldInfo.GetValue(member);
                    memberType = fieldInfo.FieldType;
                } 
                else if (memInfo is PropertyInfo propertyInfo)
                {
                    memberValueChain[i] = member = propertyInfo.GetValue(member);
                    memberType = propertyInfo.PropertyType;
                } 
                else
                {
                    invalid = true;
                    break;
                }

                i++;
            }
            if (invalid)
            {
                this.memberInfoChain = null;
                memberValueChain = null;
            }
            valid = !invalid;
        }

        public override T GetValue()
        {
            if (!IsValid) return default;
            object val = instance;
            for (int a = 0; a < memberInfoChain.Count; a++)
            {
                var memInfo = memberInfoChain[a];
                if (memInfo is FieldInfo fieldInfo) 
                    val = fieldInfo.GetValue(val); 
                else if (memInfo is PropertyInfo propertyInfo) 
                    val = propertyInfo.GetValue(val);
            }

            value = convertTo(val);

            return value;

        }
        public override void SetValue(T val) 
        {
            if (!IsValid) return;
            
            value = val;

            MemberInfo memInfo;

            object member = instance;
            for (int a = 0; a < memberInfoChain.Count; a++)
            {
                memInfo = memberInfoChain[a];
                if (memInfo is FieldInfo fieldInfo)
                    member = fieldInfo.GetValue(member);
                else if (memInfo is PropertyInfo propertyInfo)
                    member = propertyInfo.GetValue(member);
                memberValueChain[a] = member;
            }

            memberValueChain[memberValueChain.Length - 1] = convertFrom(val);
            for (int a = memberInfoChain.Count - 2; a >= 0; a++)
            {
                memInfo = memberInfoChain[a + 1];
                if (memInfo is FieldInfo fieldInfo)
                    fieldInfo.SetValue(memberValueChain[a], memberValueChain[a + 1]);
                else if (memInfo is PropertyInfo propertyInfo)
                    propertyInfo.SetValue(memberValueChain[a], memberValueChain[a + 1]);
            }

            memInfo = memberInfoChain[0];
            if (memInfo is FieldInfo fieldInfo_)
                fieldInfo_.SetValue(instance, memberValueChain[0]);
            else if (memInfo is PropertyInfo propertyInfo_)
                propertyInfo_.SetValue(instance, memberValueChain[0]);
        }

        public static implicit operator T(SwoleVarLinkedInstance<T> v) => v.GetValue();

    }

}
