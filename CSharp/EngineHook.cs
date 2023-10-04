using System;

namespace Swolescript
{

    public class EngineHook
    {

        public virtual string Name => "No Engine";

        public virtual string WorkingDirectory => Environment.CurrentDirectory;

        public virtual string ParseSource(string source) => source;

        public virtual string ToJson(object obj, bool prettyPrint = false) => SwoleScriptJSON.ToJson(obj, prettyPrint);
        public virtual object FromJson(string json, Type type) => SwoleScriptJSON.FromJson(json, type);
        public virtual T FromJson<T>(string json) => SwoleScriptJSON.FromJson<T>(json);

        public virtual EngineInternal.Vector3 GetLocalPosition(object engineObject) => EngineInternal.Vector3.zero;
        public virtual EngineInternal.Vector3 GetLocalScale(object engineObject) => EngineInternal.Vector3.one;
        public virtual EngineInternal.Quaternion GetLocalRotation(object engineObject) => EngineInternal.Quaternion.identity;

        public virtual EngineInternal.Vector3 GetWorldPosition(object engineObject) => EngineInternal.Vector3.zero;
        public virtual EngineInternal.Vector3 GetLossyScale(object engineObject) => EngineInternal.Vector3.one;
        public virtual EngineInternal.Quaternion GetWorldRotation(object engineObject) => EngineInternal.Quaternion.identity;

    }

}
