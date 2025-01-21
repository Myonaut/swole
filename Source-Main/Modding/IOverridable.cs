using System;

namespace Swole.Modding 
{
    public interface IOverridable
    {
        public bool IsOverridden { get; set; }
        public object Value { get; set; }
        public Type ValueType { get; }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class OverrideDependencyAttribute : Attribute
    {
        private string dependencyFieldName;
        public string DependencyFieldName => dependencyFieldName;

        public OverrideDependencyAttribute(string dependencyFieldName)
        {
            this.dependencyFieldName = dependencyFieldName;
        }

        public bool IsOverridden(object instance)
        {
            if (instance == null) return false;

            var field = instance.GetType().GetField(dependencyFieldName);
            if (field == null) return false;

            var fieldVal = field.GetValue(instance);
            if (fieldVal is IOverridable overridable) return overridable.IsOverridden;

            return false;
        }
    }
    [AttributeUsage(AttributeTargets.Field)]
    public class OverrideSelectionAliasAttribute : Attribute
    {
        private string alias;
        public string Alias => alias;

        public OverrideSelectionAliasAttribute(string alias)
        {
            this.alias = alias;
        }
    }

    [Serializable]
    public struct OverridableFlag : IOverridable
    { 
        public bool doOverride;
        public object Value
        {
            get => doOverride;
            set => doOverride = (bool)value;
        }
        public bool IsOverridden
        {
            get => doOverride;
            set => doOverride = value;
        }

        public Type ValueType => null;
    }

    [Serializable]
    public struct OverridableBool : IOverridable
    {
        public bool doOverride;
        public bool value;
        public object Value
        {
            get => value;
            set => this.value = (bool)value;
        }
        public bool IsOverridden
        {
            get => doOverride;
            set => doOverride = value;
        }

        public Type ValueType => typeof(bool);

        [Serializable]
        public struct Array : IOverridable
        {
            public bool doOverride;
            public bool[] value;
            public object Value
            {
                get => value;
                set => this.value = (bool[])value;
            }
            public bool IsOverridden
            {
                get => doOverride;
                set => doOverride = value;
            }

            public Type ValueType => typeof(bool[]);
        }
    }
    [Serializable]
    public struct OverridableFloat : IOverridable
    {
        public bool doOverride;
        public float value;
        public object Value
        {
            get => value;
            set => this.value = (float)value;
        }
        public bool IsOverridden
        {
            get => doOverride;
            set => doOverride = value;
        }

        public Type ValueType => typeof(float);

        [Serializable]
        public struct Array : IOverridable
        {
            public bool doOverride;
            public float[] value;
            public object Value
            {
                get => value;
                set => this.value = (float[])value;
            }
            public bool IsOverridden
            {
                get => doOverride;
                set => doOverride = value;
            }

            public Type ValueType => typeof(float[]);
        }
    }
    [Serializable]
    public struct OverridableInt : IOverridable
    {
        public bool doOverride;
        public int value;
        public object Value
        {
            get => value;
            set => this.value = (int)value;
        }
        public bool IsOverridden
        {
            get => doOverride;
            set => doOverride = value;
        }

        public Type ValueType => typeof(int);

        [Serializable]
        public struct Array : IOverridable
        {
            public bool doOverride;
            public int[] value;
            public object Value
            {
                get => value;
                set => this.value = (int[])value;
            }
            public bool IsOverridden
            {
                get => doOverride;
                set => doOverride = value;
            }

            public Type ValueType => typeof(int[]);
        }
    }
    [Serializable]
    public struct OverridableString : IOverridable
    {
        public bool doOverride;
        public string value;
        public object Value
        {
            get => value;
            set
            {
                try
                {
                    this.value = (string)value;
                }
                catch
                {
                    this.value = value.ToString();
                }
            }
        }
        public bool IsOverridden
        {
            get => doOverride;
            set => doOverride = value;
        }

        public Type ValueType => typeof(string);

        [Serializable]
        public struct Array : IOverridable
        {
            public bool doOverride;
            public string[] value;
            public object Value
            {
                get => value;
                set => this.value = (string[])value;
            }
            public bool IsOverridden
            {
                get => doOverride;
                set => doOverride = value;
            }

            public Type ValueType => typeof(string[]);
        }
    }
    [Serializable]
    public struct OverridableVector2 : IOverridable
    {
        public bool doOverride;
        public EngineInternal.Vector2 value;
        public object Value
        {
            get => value;
            set => this.value = (EngineInternal.Vector2)UnityEngineHook.ConvertToSwoleType(value);
        }
        public bool IsOverridden
        {
            get => doOverride;
            set => doOverride = value;
        }

        public Type ValueType => typeof(EngineInternal.Vector2);

        [Serializable]
        public struct Array : IOverridable
        {
            public bool doOverride;
            public EngineInternal.Vector2[] value;
            public object Value
            {
                get => value;
                set => this.value = (EngineInternal.Vector2[])UnityEngineHook.ConvertToSwoleType(value);
            }
            public bool IsOverridden
            {
                get => doOverride;
                set => doOverride = value;
            }

            public Type ValueType => typeof(EngineInternal.Vector2[]);
        }
    }
    [Serializable]
    public struct OverridableVector3 : IOverridable
    {
        public bool doOverride;
        public EngineInternal.Vector3 value;
        public object Value
        {
            get => value;
            set => this.value = (EngineInternal.Vector3)UnityEngineHook.ConvertToSwoleType(value);
        }
        public bool IsOverridden
        {
            get => doOverride;
            set => doOverride = value;
        }

        public Type ValueType => typeof(EngineInternal.Vector3);

        [Serializable]
        public struct Array : IOverridable
        {
            public bool doOverride;
            public EngineInternal.Vector3[] value;
            public object Value
            {
                get => value;
                set => this.value = (EngineInternal.Vector3[])UnityEngineHook.ConvertToSwoleType(value);
            }
            public bool IsOverridden
            {
                get => doOverride;
                set => doOverride = value;
            }

            public Type ValueType => typeof(EngineInternal.Vector3[]);
        }
    }
    [Serializable]
    public struct OverridableVector4 : IOverridable
    {
        public bool doOverride;
        public EngineInternal.Vector4 value;
        public object Value
        {
            get => value;
            set => this.value = (EngineInternal.Vector4)UnityEngineHook.ConvertToSwoleType(value);
        }
        public bool IsOverridden
        {
            get => doOverride;
            set => doOverride = value;
        }

        public Type ValueType => typeof(EngineInternal.Vector4);

        [Serializable]
        public struct Array : IOverridable
        {
            public bool doOverride;
            public EngineInternal.Vector4[] value;
            public object Value
            {
                get => value;
                set => this.value = (EngineInternal.Vector4[])UnityEngineHook.ConvertToSwoleType(value);
            }
            public bool IsOverridden
            {
                get => doOverride;
                set => doOverride = value;
            }

            public Type ValueType => typeof(EngineInternal.Vector4[]);
        }
    }

    [Serializable]
    public struct OverridableTransformSnapshot : IOverridable
    {
        public bool doOverride;
        public TransformSnapshot value;
        public object Value
        {
            get => value;
            set => this.value = (TransformSnapshot)value;
        }
        public bool IsOverridden
        {
            get => doOverride;
            set => doOverride = value;
        }

        public Type ValueType => typeof(TransformSnapshot);

        [Serializable]
        public struct Array : IOverridable
        {
            public bool doOverride;
            public TransformSnapshot[] value;
            public object Value
            {
                get => value;
                set => this.value = (TransformSnapshot[])value;
            }
            public bool IsOverridden
            {
                get => doOverride;
                set => doOverride = value;
            }

            public Type ValueType => typeof(TransformSnapshot[]);
        }
    }
    [Serializable]
    public struct OverridableTransformSnapshotEuler : IOverridable
    {
        public bool doOverride;
        public TransformSnapshotEuler value;
        public object Value
        {
            get => value;
            set => this.value = (TransformSnapshotEuler)value;
        }
        public bool IsOverridden
        {
            get => doOverride;
            set => doOverride = value;
        }

        public Type ValueType => typeof(TransformSnapshotEuler);

        [Serializable]
        public struct Array : IOverridable
        {
            public bool doOverride;
            public TransformSnapshotEuler[] value;
            public object Value
            {
                get => value;
                set => this.value = (TransformSnapshotEuler[])value;
            }
            public bool IsOverridden
            {
                get => doOverride;
                set => doOverride = value;
            }

            public Type ValueType => typeof(TransformSnapshotEuler[]);
        }
    }
    [Serializable]
    public struct OverridableRectTransformSnapshot : IOverridable
    {
        public bool doOverride;
        public RectTransformSnapshot value;
        public object Value
        {
            get => value;
            set => this.value = (RectTransformSnapshot)value;
        }
        public bool IsOverridden
        {
            get => doOverride;
            set => doOverride = value;
        }

        public Type ValueType => typeof(RectTransformSnapshot);

        [Serializable]
        public struct Array : IOverridable
        {
            public bool doOverride;
            public RectTransformSnapshot[] value;
            public object Value
            {
                get => value;
                set => this.value = (RectTransformSnapshot[])value;
            }
            public bool IsOverridden
            {
                get => doOverride;
                set => doOverride = value;
            }

            public Type ValueType => typeof(RectTransformSnapshot[]);
        }
    }
    [Serializable]
    public struct OverridableRectTransformSnapshotEuler : IOverridable
    {
        public bool doOverride;
        public RectTransformSnapshotEuler value;
        public object Value
        {
            get => value;
            set => this.value = (RectTransformSnapshotEuler)value;
        }
        public bool IsOverridden
        {
            get => doOverride;
            set => doOverride = value;
        }

        public Type ValueType => typeof(RectTransformSnapshotEuler);

        [Serializable]
        public struct Array : IOverridable
        {
            public bool doOverride;
            public RectTransformSnapshotEuler[] value;
            public object Value
            {
                get => value;
                set => this.value = (RectTransformSnapshotEuler[])value;
            }
            public bool IsOverridden
            {
                get => doOverride;
                set => doOverride = value;
            }

            public Type ValueType => typeof(RectTransformSnapshotEuler[]);
        }
    }
}
