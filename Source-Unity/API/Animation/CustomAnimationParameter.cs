#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

using Swole.Animation;

namespace Swole.API.Unity.Animation
{

    [Serializable]
    public abstract class CustomAnimationParameter : IAnimationParameter
    {
         
        [Serializable]
        public class Float : CustomAnimationParameter
        {

            public Float() { }
            public Float(string name, float defaultValue) : base(name, defaultValue)
            {
                this.defaultValue = defaultValue;
            }

            public override object Clone()
            {

                var clone = new Float();

                clone.name = name;
                clone.m_value = m_value;
                clone.defaultValue = defaultValue;

                return clone;

            }

            [SerializeField]
            protected float defaultValue;

            public override float GetDefaultValue() => defaultValue;

        }

        [Serializable]
        public class Boolean : CustomAnimationParameter, IAnimationParameterBoolean
        {

            public Boolean() { }
            public Boolean(string name, bool defaultValue) : base(name, defaultValue ? 1 : 0)
            {
                this.defaultValue = defaultValue;
            }

            public override object Clone()
            {

                var clone = new Boolean();

                clone.name = name;
                clone.m_value = m_value;
                clone.defaultValue = defaultValue;

                return clone;

            }

            public bool IsTrue => Value >= 0.5f;

            public void SetValue(bool value) => SetValue(value ? 1 : 0);

            [SerializeField]
            protected bool defaultValue;

            public override float GetDefaultValue() => defaultValue ? 1 : 0;

        }

        [Serializable]
        public class Trigger : Boolean, IAnimationParameterTrigger
        {

            public Trigger() { }
            public Trigger(string name) : base(name, false) { }

            public override object Clone()
            {

                var clone = new Trigger();

                clone.name = name;
                clone.m_value = m_value;
                clone.defaultValue = defaultValue;

                return clone;

            }

            public void Arm() => SetValue(1);

            public void SetValue() => Arm();

            public bool TryConsume()
            {

                if (!IsTrue) return false;

                m_value = 0;

                return true;

            }

            public override float UpdateAndGetValue()
            {

                return TryConsume() ? 1 : 0;

            }

        }

        /// <summary>
        /// A float parameter that is controlled by an external object
        /// </summary>
        [Serializable]
        public abstract class ExternalFloat : Float
        {

            public override void SetValue(float value) { }

        }

        /// <summary>
        /// A float parameter that is controlled by a field reference
        /// </summary>
        [Serializable]
        public class ExternalFloatField : ExternalFloat
        {

            public override object Clone()
            {

                var clone = new ExternalFloatField();

                clone.name = name;
                clone.m_value = m_value;
                clone.defaultValue = defaultValue;

                clone.fieldName = fieldName;
                clone.field = field;
                clone.objectReference = objectReference;

                return clone;

            }

            public string fieldName;

            [NonSerialized]
            protected FieldInfo field;

            [NonSerialized]
            protected object objectReference;

            public override void Initialize(IAnimator animator, object obj = null)
            {

                objectReference = obj;

                if (obj != null)
                {

                    var objType = obj.GetType();

                    if (objType != null)
                    {

                        field = objType.GetField(fieldName);
                        if (field.DeclaringType != typeof(float)) field = null;

                    }

                }

            }

            public override float Value
            {

                get
                {

                    if (objectReference != null && field != null) m_value = (float)field.GetValue(objectReference); else m_value = GetDefaultValue();

                    return m_value;

                }

            }

        }

        [Serializable]
        public abstract class MuscleGroupValue : ExternalFloat
        {

            public MuscleGroupIdentifier muscleGroupIdentifier;

            public abstract void OnMuscleValueUpdate(MuscleGroupInfo muscleData);

            [NonSerialized]
            protected MuscleValueListener listener;

            public override void Initialize(IAnimator animator, object obj = null)
            {

                if (listener != null) listener.Dispose();
                if (animator == null) return;

                if (obj == null) obj = animator;

                if (obj is Component component)
                {

                    obj = component.gameObject.GetComponent<MuscularRenderedCharacter>();

                }
                else if (obj is GameObject gameObject)
                {

                    obj = gameObject.GetComponent<MuscularRenderedCharacter>();

                }

                if (obj is MuscularRenderedCharacter character)
                {

                    character.Listen(character.GetMuscleGroupIndex(muscleGroupIdentifier), animator, OnMuscleValueUpdate, out listener);
                }

            }

            public override void Dispose()
            {

                base.Dispose();

                if (listener != null) listener.Dispose();

                listener = null;

            }

        }

        /// <summary>
        /// A float parameter that is synced to the mass of a muscle group
        /// </summary>
        [Serializable]
        public class MuscleGroupMass : MuscleGroupValue
        {

            public override object Clone()
            {

                var clone = new MuscleGroupMass();

                clone.name = name;
                clone.m_value = m_value;
                clone.defaultValue = defaultValue;

                clone.muscleGroupIdentifier = muscleGroupIdentifier;

                return clone;

            }

            public override void OnMuscleValueUpdate(MuscleGroupInfo muscleData) => m_value = muscleData.mass;

        }

        /// <summary>
        /// A float parameter that is synced to the flex of a muscle group
        /// </summary>
        [Serializable]
        public class MuscleGroupFlex : MuscleGroupValue
        {

            public override object Clone()
            {

                var clone = new MuscleGroupFlex();

                clone.name = name;
                clone.m_value = m_value;
                clone.defaultValue = defaultValue;

                clone.muscleGroupIdentifier = muscleGroupIdentifier;

                return clone;

            }

            public override void OnMuscleValueUpdate(MuscleGroupInfo muscleData) => m_value = muscleData.flex;

        }

        /// <summary>
        /// A float parameter that is synced to the pump of a muscle group
        /// </summary>
        [Serializable]
        public class MuscleGroupPump : MuscleGroupValue
        {

            public override object Clone()
            {

                var clone = new MuscleGroupPump();

                clone.name = name;
                clone.m_value = m_value;
                clone.defaultValue = defaultValue;

                clone.muscleGroupIdentifier = muscleGroupIdentifier;

                return clone;

            }

            public override void OnMuscleValueUpdate(MuscleGroupInfo muscleData) => m_value = muscleData.pump;

        }

        public string name;
        public string Name 
        {
            get => name;
            set => name = value;
        }

        [NonSerialized]
        public int indexInAnimator;
        public int IndexInAnimator
        {
            get => indexInAnimator;
            set => indexInAnimator = value;
        }

        [NonSerialized]
        protected float m_value;

        public virtual float Value => m_value;

        public CustomAnimationParameter() { }
        public CustomAnimationParameter(string name, float defaultValue) 
        {
            this.name = name;
            this.m_value = defaultValue;
        }

        public abstract float GetDefaultValue();

        public virtual float UpdateAndGetValue() => Value;

        public virtual void SetValue(float value)
        {
            m_value = value;
        }

        public abstract object Clone();

        public virtual void Initialize(IAnimator animator, object obj = null) { }

        public virtual void Dispose() { }

        public bool DisposeIfHasPrefix(string prefix)
        {
            bool dispose = name == null ? false : name.ToLower().Trim().StartsWith(prefix);
            if (dispose) Dispose();
            return dispose;
        }

    }

}

#endif