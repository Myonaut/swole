using System;

namespace Swolescript
{

    public class EngineHook
    {

        public virtual string Name => "No Engine";

        public virtual string ParseSource(string source) => source;

        public virtual EngineInternal.Vector3 GetLocalPosition(object engineObject) => EngineInternal.Vector3.zero;
        public virtual EngineInternal.Vector3 GetLocalScale(object engineObject) => EngineInternal.Vector3.one;
        public virtual EngineInternal.Quaternion GetLocalRotation(object engineObject) => EngineInternal.Quaternion.identity;

        public virtual EngineInternal.Vector3 GetWorldPosition(object engineObject) => EngineInternal.Vector3.zero;
        public virtual EngineInternal.Vector3 GetLossyScale(object engineObject) => EngineInternal.Vector3.one;
        public virtual EngineInternal.Quaternion GetWorldRotation(object engineObject) => EngineInternal.Quaternion.identity;

    }

}
