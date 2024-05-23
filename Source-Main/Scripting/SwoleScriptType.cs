using System;
using System.Collections.Generic;

#if SWOLE_ENV
using Miniscript;
#endif

namespace Swole.Script
{
#if SWOLE_ENV
    public class SwoleScriptType : ValMap
    {

        public static readonly Dictionary<string, SwoleScriptType> namedTypes = new Dictionary<string, SwoleScriptType>();
        public static readonly List<SwoleScriptType> allTypes = new List<SwoleScriptType>();

        protected string name;
        public string Name => name;

        protected Type realTypeEquivalent;
        public Type RealType => realTypeEquivalent;

        protected SwoleScriptType parentType;
        public SwoleScriptType ParentType => parentType;

        private SwoleScriptType globalType;
        public SwoleScriptType GlobalType => globalType == null ? this : globalType;

        public SwoleScriptType(string name, Type realTypeEquivalent = null, SwoleScriptType parentType = null, bool isCopy = false)
        {
            this.name = name;
            this.realTypeEquivalent = realTypeEquivalent;
            this.parentType = parentType;

            if (!isCopy)
            {
                if (!string.IsNullOrWhiteSpace(name)) namedTypes[name] = this;
                allTypes.Add(this);
            }
        }

        /// <summary>
        /// Fetches a local context version of the type. (To avoid changes to the type outside of the context's environment)
        /// </summary>
        public SwoleScriptType GetType(TAC.Context context) 
        {
            if (globalType != null) return globalType.GetType(context);

            var key = $"~contextualType_{Name}"; 
            var globalContext = context.vm.globalContext;

            if (globalContext.variables != null && globalContext.variables.TryGetValue(key, out var value)) // Try to find an existing version of the type in the context
            {
                if (value is SwoleScriptType sst) return sst;
            }
             
            SwoleScriptType ct = EvalCopy(globalContext);
            ct.globalType = this;
            if (parentType != null)
            {
                ct.parentType = parentType.GetType(context); // Get a local context version of the parent type as well
                ct.SetElem(ValString.magicIsA, ct.parentType);
            }
            globalContext.SetVar(key, ct); // Store the local type in the context
            return ct;
        }

        public delegate void CreateNewObjectDelegate(TAC.Context context, ValMap instance);
        public CreateNewObjectDelegate onCreateNewObject;
        /// <summary>
        /// Create a new instance from the type. Uses the type that is local to the context (to ensure any changes made to the type stay only in that context)
        /// </summary>
        public ValMap NewObject(TAC.Context context)
        {
            var instance = new ValMap();
            var type = GetType(context);
            instance.SetElem(ValString.magicIsA, type);
            instance.assignOverride = type.assignOverride;
            onCreateNewObject?.Invoke(context, instance);
            return instance;
        }
         
        new public SwoleScriptType EvalCopy(TAC.Context context)
        {
            var result = new SwoleScriptType(Name, null, null, true);

            result.name = name;
            result.realTypeEquivalent = realTypeEquivalent;
            result.globalType = globalType;
            result.parentType = parentType;

            foreach (Value k in map.Keys)
            {
                Value key = k;
                Value value = map[key];
                if (key is ValTemp || key is ValVar || value is ValSeqElem) key = key.Val(context);
                if (value is ValTemp || value is ValVar || value is ValSeqElem) value = value.Val(context);
                result.map[key] = value;
            }

            result.assignOverride = assignOverride;  

            return result;
        }

        /// <summary>
        /// Checks if two swole script types are the same
        /// </summary>
        public override double Equality(Value rhs)
        {
            if (globalType != null && !ReferenceEquals(this, globalType) && rhs is SwoleScriptType sst && globalType.Equality(sst.GlobalType) >= 1) return 1; // Compare global types to see if they're the same 
            return base.Equality(rhs);
        }

        public override int Hash()
        {
            if (globalType != null && !ReferenceEquals(this, globalType)) return globalType.Hash();
            return base.Hash();
        }

    }
#else
    public class SwoleScriptType
    {        
    
        public static readonly Dictionary<string, SwoleScriptType> namedTypes = new Dictionary<string, SwoleScriptType>();
        public static readonly List<SwoleScriptType> allTypes = new List<SwoleScriptType>();

        protected string name;
        public string Name => name;
        
        protected Type realTypeEquivalent;
        public Type RealType => realTypeEquivalent;

        protected SwoleScriptType parentType;
        public SwoleScriptType ParentType => parentType;

        private SwoleScriptType globalType;
        public SwoleScriptType GlobalType => globalType == null ? this : globalType;

        public SwoleScriptType(string name, Type realTypeEquivalent = null, SwoleScriptType parentType = null, bool isCopy = false)
        {
            this.name = name;
            this.realTypeEquivalent = realTypeEquivalent;
            this.parentType = parentType;
            
            if (!isCopy)
            {
                if (!string.IsNullOrWhiteSpace(name)) namedTypes[name] = this;
                allTypes.Add(this);
            }
        }

        public int Hash() => GetHashCode();

    }
#endif
}
