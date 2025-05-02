#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections.Generic;

using UnityEngine;

using Swole.API.Unity.Animation;

using static Swole.API.Unity.Animation.AnimationCurveEditor;

namespace Swole.API.Unity
{

    public interface IAnimationCurveProxy : ICurve
    {

        public void ClearAllListeners();
        public void NotifyStateChange();

        public WrapMode preWrapMode { get; set; }
        public WrapMode postWrapMode { get; set; }
        public Keyframe[] keys { get; set; }
        public AnimationCurveEditor.KeyframeStateRaw[] Keys { get; set; }
        public int length { get; }

        public AnimationCurveEditor.KeyframeStateRaw this[int keyIndex] { get; set; }
        public AnimationCurveEditor.KeyframeStateRaw GetKey(int keyIndex);
        public void SetKey(int keyIndex, AnimationCurveEditor.KeyframeStateRaw value, bool notifyListeners = true);

        public int AddKey(AnimationCurveEditor.KeyframeState keyState, bool notifyListeners = true);
        public int AddKey(float time, float value, bool notifyListeners = true);

        public void FixNaN();

        public void ClearKeys();
    }

    public class EditableAnimationCurve : SwoleObject<EditableAnimationCurve, SerializedAnimationCurve>, ICloneable, IAnimationCurveProxy
    {

        public event VoidParameterlessDelegate OnStateChange;
        public void ClearAllListeners() => OnStateChange = null;
        public void NotifyStateChange() => OnStateChange?.Invoke();
        public void SyncWithUnityCurve()
        {
            /*if (instance != null)*/ state.ApplyToAnimationCurve(instance); 
        }

        public static EditableAnimationCurve Linear(float timeStart, float valueStart, float timeEnd, float valueEnd)
        {
            var state = new AnimationCurveEditor.State();

            state.preWrapMode = (int)WrapMode.Clamp;
            state.postWrapMode = (int)WrapMode.Clamp;

            state.keyframes = new AnimationCurveEditor.KeyframeStateRaw[]
            {
                new AnimationCurveEditor.KeyframeState() { data = new Keyframe() { time = timeStart, value = valueStart, weightedMode = WeightedMode.Both }, tangentSettings = new AnimationCurveEditor.KeyframeTangentSettings() { inTangentMode = AnimationCurveEditor.BrokenTangentMode.Linear, outTangentMode = AnimationCurveEditor.BrokenTangentMode.Linear, tangentMode = AnimationCurveEditor.TangentMode.Broken } },
                new AnimationCurveEditor.KeyframeState() { data = new Keyframe() { time = timeEnd, value = valueEnd, weightedMode = WeightedMode.Both }, tangentSettings = new AnimationCurveEditor.KeyframeTangentSettings() { inTangentMode = AnimationCurveEditor.BrokenTangentMode.Linear, outTangentMode = AnimationCurveEditor.BrokenTangentMode.Linear, tangentMode = AnimationCurveEditor.TangentMode.Broken } }
            };
             
            return new EditableAnimationCurve(state); 
        }

        protected AnimationCurve instance;
        public AnimationCurve Instance 
        {
            set => instance = value;
            get
            {
                if (instance == null) instance = state.AsNewAnimationCurve();     

                return instance;
            }
        }

        public string name;
        public string Name 
        {
            get => name;
            set => name = value;
        }
        public override string SerializedName => Name;

        public CurveType Type => CurveType.Animation;

        protected bool isDirty;
        protected AnimationCurveEditor.State state;
        public AnimationCurveEditor.State State
        {
            set => SetState(value);
            get => GetState();
        }

        public AnimationCurveEditor.State GetState() => state;
        public AnimationCurveEditor.State CloneState() => state.Duplicate();

        protected const float clampedAutoFalloff = 1 / 3f;
        public void SetState(AnimationCurveEditor.State state, bool notifyListeners = true, bool reevaluateBasedOnTangentSettings = true)
        { 
            if (reevaluateBasedOnTangentSettings)
            {
                if (state.keyframes != null)
                {
                    for (int a = 0; a < state.keyframes.Length; a++) 
                    {
                        var key = (AnimationCurveEditor.KeyframeState)state.keyframes[a];

                        bool hasLeftNeighbor = a > 0;
                        var leftNeighbor = hasLeftNeighbor ? (AnimationCurveEditor.KeyframeState)state.keyframes[a - 1] : key;

                        bool hasRightNeighbor = a < state.keyframes.Length - 1;
                        var rightNeighbor = hasRightNeighbor ? (AnimationCurveEditor.KeyframeState)state.keyframes[a + 1] : key; 

                        if (hasLeftNeighbor)
                        {
                            if (key.tangentSettings.tangentMode != AnimationCurveEditor.TangentMode.Broken || key.tangentSettings.inTangentMode == AnimationCurveEditor.BrokenTangentMode.Linear || key.tangentSettings.inTangentMode == AnimationCurveEditor.BrokenTangentMode.Constant)
                            {
                                switch (key.tangentSettings.tangentMode)
                                {

                                    default:
                                        if (key.tangentSettings.inTangentMode == BrokenTangentMode.Linear)
                                        {
                                            key.data = CalculateLinearInTangent(key, leftNeighbor);
                                        }
                                        else if (key.tangentSettings.inTangentMode == BrokenTangentMode.Constant)
                                        {
                                            key.data.inTangent = float.PositiveInfinity;
                                        }
                                        else if (key.tangentSettings.inTangentMode == BrokenTangentMode.Free)
                                        {
                                        }
                                        break;

                                    case TangentMode.Auto:
                                        if (!hasRightNeighbor)
                                        {
                                            key.data.inTangent = 0;
                                        }
                                        else
                                        {
                                            float mul = Mathf.Clamp01(Mathf.Abs((GetValueInRange(key.data.value, leftNeighbor.data.value, rightNeighbor.data.value) - 0.5f) / 0.5f));
                                            mul = 1 - (Mathf.Max(0, mul - (1 - clampedAutoFalloff)) / clampedAutoFalloff);
                                            key.data.inTangent = ((rightNeighbor.data.value - leftNeighbor.data.value) / (rightNeighbor.data.time - leftNeighbor.data.time)) * mul;
                                        }
                                        break;

                                    case TangentMode.Smooth:
                                        break;

                                    case TangentMode.Flat:
                                        if (key.data.weightedMode != WeightedMode.In && key.data.weightedMode != WeightedMode.Both)
                                        {
                                            key.data.inWeight = defaultTangentWeight;
                                        }
                                        else
                                        {
                                        }
                                        key.data.inTangent = 0;
                                        break;
                                }
                            }
                        }
                        if (hasRightNeighbor)
                        {
                            if (key.tangentSettings.tangentMode != AnimationCurveEditor.TangentMode.Broken || key.tangentSettings.outTangentMode == AnimationCurveEditor.BrokenTangentMode.Linear || key.tangentSettings.outTangentMode == AnimationCurveEditor.BrokenTangentMode.Constant)
                            {
                                switch (key.tangentSettings.tangentMode)
                                {

                                    default:
                                        if (key.tangentSettings.outTangentMode == BrokenTangentMode.Linear)
                                        {
                                            key.data = CalculateLinearOutTangent(key, rightNeighbor);
                                        }
                                        else if (key.tangentSettings.outTangentMode == BrokenTangentMode.Constant)
                                        {
                                            key.data.outTangent = float.PositiveInfinity; 
                                        }
                                        else if (key.tangentSettings.outTangentMode == BrokenTangentMode.Free)
                                        {
                                        }
                                        break;

                                    case TangentMode.Auto:
                                        if (!hasLeftNeighbor)
                                        {
                                            key.data.outTangent = 0;
                                        }
                                        else
                                        {
                                            key.data.outTangent = key.data.inTangent;
                                        }
                                        break;

                                    case TangentMode.Smooth:
                                        break;

                                    case TangentMode.Flat:
                                        if (key.data.weightedMode != WeightedMode.Out && key.data.weightedMode != WeightedMode.Both)
                                        {
                                            key.data.outWeight = defaultTangentWeight;
                                        }
                                        else
                                        {
                                        }
                                        key.data.outTangent = 0;
                                        break;

                                }
                            }
                        }

                        state.keyframes[a] = key;
                    }
                }
            }

            this.state = state;
            SyncWithUnityCurve();
            if (notifyListeners) NotifyStateChange();
        }

        public static implicit operator AnimationCurve(EditableAnimationCurve inCurve) => inCurve == null ? null : inCurve.Instance;
        public static implicit operator EditableAnimationCurve(AnimationCurveEditor.State inState) => new EditableAnimationCurve() { State = inState };
        public static implicit operator AnimationCurveEditor.State(EditableAnimationCurve inCurve) => inCurve == null ? default : inCurve.State; 
        public static implicit operator EditableAnimationCurve(SerializedAnimationCurve serializedCurve) => serializedCurve.AsEditableAnimationCurve();//new EditableAnimationCurve(serializedCurve);  

        public override SerializedAnimationCurve AsSerializableStruct() => this;
        public override string AsJSON(bool prettyPrint = false) => AsSerializableStruct().AsJSON(prettyPrint);

        public ISwoleSerializable Serialize() => AsSerializableStruct();  

        public EditableAnimationCurve Duplicate() => new EditableAnimationCurve(AsSerializableStruct());    
        public object Clone() => Duplicate();

        public EditableAnimationCurve() : base(default) 
        {
            state.preWrapMode = (int)WrapMode.Clamp; 
            state.postWrapMode = (int)WrapMode.Clamp; 
        }
        public EditableAnimationCurve(SerializedAnimationCurve data) : base(data) 
        {
            name = data.name;

            State = data.keyframeStates;

            if (state.preWrapMode == 0) state.preWrapMode = (int)WrapMode.Clamp; 
            if (state.postWrapMode == 0) state.postWrapMode = (int)WrapMode.Clamp;  

            if ((data.keyframeStates.keyframes == null || data.keyframeStates.keyframes.Length == 0) && (data.keyframes != null && data.keyframes.Length > 0)) // Serialized data was likely a unity animation curve, now convert it 
            { 
                state.keyframes = new AnimationCurveEditor.KeyframeStateRaw[data.keyframes.Length];
                for (int a = 0; a < data.keyframes.Length; a++) state.keyframes[a] = new AnimationCurveEditor.KeyframeState() { data = data.keyframes[a], tangentSettings = new AnimationCurveEditor.KeyframeTangentSettings() { inTangentMode = AnimationCurveEditor.BrokenTangentMode.Free, outTangentMode = AnimationCurveEditor.BrokenTangentMode.Free, tangentMode = AnimationCurveEditor.TangentMode.Broken } };
            }
        } 
        public EditableAnimationCurve(AnimationCurveEditor.State state, AnimationCurve instance, string name = null) : base(default)
        {
            this.name = name;

            this.state = state;
            if (this.state.preWrapMode == 0) this.state.preWrapMode = (int)WrapMode.Clamp;
            if (this.state.postWrapMode == 0) this.state.postWrapMode = (int)WrapMode.Clamp;

            this.instance = instance;  
        }
        public EditableAnimationCurve(AnimationCurve curve, string name = null) : base(default)
        {
            this.name = name;

            instance = curve;  
            if (instance != null)
            {
                state.preWrapMode = (int)curve.preWrapMode;
                state.postWrapMode = (int)curve.postWrapMode;
                if (state.preWrapMode == 0) state.preWrapMode = (int)WrapMode.Clamp;
                if (state.postWrapMode == 0) state.postWrapMode = (int)WrapMode.Clamp;

                state.keyframes = new AnimationCurveEditor.KeyframeStateRaw[instance.length]; 
                for (int a = 0; a < instance.length; a++) state.keyframes[a] = new AnimationCurveEditor.KeyframeState() { data = instance[a], tangentSettings = new AnimationCurveEditor.KeyframeTangentSettings() { inTangentMode = AnimationCurveEditor.BrokenTangentMode.Free, outTangentMode = AnimationCurveEditor.BrokenTangentMode.Free, tangentMode = AnimationCurveEditor.TangentMode.Broken } };
            }
        }

        public WrapMode preWrapMode 
        { 
            get => (WrapMode)state.preWrapMode; 
            set
            {
                state.preWrapMode = (int)value;
                NotifyStateChange();
            }
        }
        public WrapMode postWrapMode 
        { 
            get => (WrapMode)state.postWrapMode; 
            set
            {
                state.postWrapMode = (int)value;
                NotifyStateChange();
            }
        }
        public Keyframe[] keys
        {
            get => Instance.keys;
            set 
            {
                if (value == null)
                {
                    state.keyframes = null;
                    NotifyStateChange();
                    return;
                }

                if (state.keyframes == null || state.keyframes.Length != value.Length)
                {
                    var temp = new AnimationCurveEditor.KeyframeStateRaw[value.Length];
                    for (int a = 0; a < temp.Length; a++) temp[a] = new AnimationCurveEditor.KeyframeState() 
                    { 
                        data = new Keyframe() { weightedMode = WeightedMode.None }, 
                        tangentSettings = new AnimationCurveEditor.KeyframeTangentSettings() { inTangentMode = AnimationCurveEditor.BrokenTangentMode.Free, outTangentMode = AnimationCurveEditor.BrokenTangentMode.Free, tangentMode = AnimationCurveEditor.TangentMode.Broken } 
                    };
                    if (state.keyframes != null) for (int a = 0; a < Mathf.Min(value.Length, state.keyframes.Length); a++) temp[a] = state.keyframes[a]; 
                    state.keyframes = temp;
                }

                for (int a = 0; a < value.Length; a++) 
                { 
                    var key = state.keyframes[a];
                    var inputKey = value[a];

                    key.time = inputKey.time;
                    key.value = inputKey.value;
                    key.inTangent = inputKey.inTangent;
                    key.outTangent = inputKey.outTangent;
                    key.inWeight = inputKey.inWeight;
                    key.outWeight = inputKey.outWeight;

                    key.weightedMode = (int)inputKey.weightedMode; 

                    state.keyframes[a] = key;
                }

                NotifyStateChange();
            }
        }
        public AnimationCurveEditor.KeyframeStateRaw[] Keys
        {
            get => state.keyframes == null ? new AnimationCurveEditor.KeyframeStateRaw[0] : state.keyframes;
            set
            {
                var s = State;
                s.keyframes = value;
                State = s;
            }
        }
        public AnimationCurveEditor.KeyframeStateRaw GetKey(int keyIndex) => state.keyframes[keyIndex];
        public void SetKey(int keyIndex, AnimationCurveEditor.KeyframeStateRaw value) => SetKey(keyIndex, value, true, true);
        public void SetKey(int keyIndex, AnimationCurveEditor.KeyframeStateRaw value, bool notifyListeners) => SetKey(keyIndex, value, notifyListeners, true);
        public void SetKey(int keyIndex, AnimationCurveEditor.KeyframeStateRaw value, bool notifyListeners, bool syncWithUnityCurve) 
        { 
            state.keyframes[keyIndex] = value;
            if (syncWithUnityCurve) SyncWithUnityCurve();
            if (notifyListeners) NotifyStateChange();
        }

        public int length => state.keyframes == null ? 0 : state.keyframes.Length;

        public AnimationCurveEditor.KeyframeStateRaw this[int keyIndex]
        {
            get => GetKey(keyIndex);
            set => SetKey(keyIndex, value);        
        }

        public float Evaluate(float t) => Instance.Evaluate(t); 
        public EngineInternal.Vector2 Evaluate2(float t)
        {
            var val = Evaluate(t);
            return new EngineInternal.Vector2(val, val); 
        }
        public EngineInternal.Vector3 Evaluate3(float t)
        {
            var val = Evaluate(t);
            return new EngineInternal.Vector3(val, val, val); 
        }

        public int AddKey(AnimationCurveEditor.KeyframeState keyState) => AddKey(keyState, true, true);
        public int AddKey(AnimationCurveEditor.KeyframeState keyState, bool notifyListeners) => AddKey(keyState, notifyListeners, true);
        public int AddKey(AnimationCurveEditor.KeyframeState keyState, bool notifyListeners, bool syncWithUnityCurve)
        {
            if (state.keyframes == null || state.keyframes.Length <= 0)
            {
                state.keyframes = new AnimationCurveEditor.KeyframeStateRaw[1];
                state.keyframes[0] = keyState;
                if (syncWithUnityCurve) SyncWithUnityCurve();
                if (notifyListeners) NotifyStateChange();
                return 0;
            } 
            else
            {
                int pos = -1;
                for(int a = 0; a < state.keyframes.Length; a++)
                {
                    var key = state.keyframes[a];
                    if (keyState.data.time == key.time) return -1; 
                    pos = a + 1;
                    if (keyState.data.time < key.time)  
                    { 
                        pos = a;
                        break;
                    }
                    
                }
                if (pos >= 0)
                {
                    var temp = new AnimationCurveEditor.KeyframeStateRaw[state.keyframes.Length + 1];
                    for(int a = 0; a < pos; a++) temp[a] = state.keyframes[a];
                    temp[pos] = keyState;
                    for (int a = pos; a < state.keyframes.Length; a++) temp[a + 1] = state.keyframes[a];
                    state.keyframes = temp;
                }

                if (syncWithUnityCurve) SyncWithUnityCurve();
                if (notifyListeners) NotifyStateChange();
                return pos; 
            }
        }
        public int AddKey(float time, float value) => AddKey(time, value, true, true);
        public int AddKey(float time, float value, bool notifyListeners = true) => AddKey(time, value, notifyListeners, true);
        public int AddKey(float time, float value, bool notifyListeners = true, bool syncWithUnityCurve = true) => AddKey(new AnimationCurveEditor.KeyframeState() { data = new Keyframe() { time = time, value = value, inTangent = 0, outTangent = 0, inWeight = 1, outWeight = 1, weightedMode = WeightedMode.None }, tangentSettings = new AnimationCurveEditor.KeyframeTangentSettings() { inTangentMode = AnimationCurveEditor.BrokenTangentMode.Free, outTangentMode = AnimationCurveEditor.BrokenTangentMode.Free, tangentMode = AnimationCurveEditor.TangentMode.Broken } }, notifyListeners, syncWithUnityCurve);

        public void FixNaN(bool notifyListeners, bool syncWithUnityCurve)
        {
            if (state.keyframes == null) return;

            bool changed = false;
            for(int a = 0; a < state.keyframes.Length; a++)
            {
                var kf = state.keyframes[a];

                if (!float.IsFinite(kf.inTangent)) 
                {
                    changed = true;
                    kf.inTangent = 0; 
                }
                if (!float.IsFinite(kf.outTangent))
                {
                    changed = true;
                    kf.outTangent = 0; 
                }
                if (!float.IsFinite(kf.inWeight))
                {
                    changed = true;
                    kf.inWeight = 1; 
                }
                if (!float.IsFinite(kf.outWeight))
                {
                    changed = true;
                    kf.outWeight = 1;  
                }

                state.keyframes[a] = kf;
            }

            if (changed && syncWithUnityCurve) SyncWithUnityCurve();
            if (changed && notifyListeners) NotifyStateChange();
        }
        public void FixNaN(bool notifyListeners) => FixNaN(notifyListeners, true);
        public void FixNaN() => FixNaN(true, true);

        public void ClearKeys() => ClearKeys(true, true);
        public void ClearKeys(bool notifyListeners) => ClearKeys(notifyListeners, true);
        public void ClearKeys(bool notifyListeners, bool syncWithUnityCurve)
        {
            state.keyframes = null;

            if (syncWithUnityCurve) SyncWithUnityCurve();
            if (notifyListeners) NotifyStateChange();
        }

    }

    public struct AnimationCurveProxy : IAnimationCurveProxy
    {

        public object Clone() => new AnimationCurveProxy(unityCurve == null ? null : (AnimationCurve)new EditableAnimationCurve(unityCurve).Duplicate(), name); 

        public string name;

        public string Name
        {
            get => name;
            set => name = value;
        }

        public void ClearAllListeners() { }
        public void NotifyStateChange() { }

        public CurveType Type => CurveType.Animation;

        public ISwoleSerializable Serialize() => new EditableAnimationCurve(unityCurve).AsSerializableStruct();  

        public AnimationCurve unityCurve; 
        public AnimationCurveProxy(AnimationCurve unityCurve, string name = null)
        {
            this.unityCurve = unityCurve; 
            this.name = name;
        }
        public static explicit operator AnimationCurveProxy(AnimationCurve curve) => new AnimationCurveProxy(curve);
        public static explicit operator AnimationCurve(AnimationCurveProxy proxy) => proxy.unityCurve; 

        public AnimationCurveEditor.KeyframeStateRaw this[int keyIndex]
        {
            get => GetKey(keyIndex);
            set => SetKey(keyIndex, value);
        }
        public AnimationCurveEditor.KeyframeStateRaw GetKey(int keyIndex) => unityCurve == null ? default : new AnimationCurveEditor.KeyframeState() { data = unityCurve[keyIndex], tangentSettings = new AnimationCurveEditor.KeyframeTangentSettings() { inTangentMode = AnimationCurveEditor.BrokenTangentMode.Free, outTangentMode = AnimationCurveEditor.BrokenTangentMode.Free, tangentMode = AnimationCurveEditor.TangentMode.Broken } };
        public void SetKey(int keyIndex, AnimationCurveEditor.KeyframeStateRaw value, bool notifyListeners = true)
        {
            var keys_ = keys;
            keys_[keyIndex] = value;
            keys = keys_; 
        }

        public WrapMode preWrapMode 
        { 
            get => unityCurve == null ? default : unityCurve.preWrapMode;
            set
            {
                if (unityCurve == null) return;
                unityCurve.preWrapMode = value;
            }
        }
        public WrapMode postWrapMode
        {
            get => unityCurve == null ? default : unityCurve.postWrapMode;
            set
            {
                if (unityCurve == null) return;
                unityCurve.postWrapMode = value;
            }
        }
        public Keyframe[] keys
        {
            get => unityCurve == null ? default : unityCurve.keys;
            set
            {
                if (unityCurve == null) return;
                unityCurve.keys = value;
            }
        }
        public AnimationCurveEditor.KeyframeStateRaw[] Keys
        {
            get 
            {
                if (unityCurve == null) return null; 
                AnimationCurveEditor.KeyframeStateRaw[] keyframes = new AnimationCurveEditor.KeyframeStateRaw[unityCurve.length];
                for (int a = 0; a < keyframes.Length; a++) keyframes[a] = new AnimationCurveEditor.KeyframeState() { data = unityCurve[a], tangentSettings = new AnimationCurveEditor.KeyframeTangentSettings() { inTangentMode = AnimationCurveEditor.BrokenTangentMode.Free, outTangentMode = AnimationCurveEditor.BrokenTangentMode.Free, tangentMode = AnimationCurveEditor.TangentMode.Broken } };
                return keyframes; 
            }
            set
            {
                if (unityCurve == null) return;
                if (value == null)
                {
                    unityCurve.keys = null;
                    return;
                }
                Keyframe[] keyframes = new Keyframe[value.Length];
                for (int a = 0; a < keyframes.Length; a++) keyframes[a] = value[a];
                unityCurve.keys = keyframes;
            }
        }

        public int length => unityCurve == null ? default : unityCurve.length;

        public int AddKey(AnimationCurveEditor.KeyframeState keyState, bool notifyListeners = true)
        {
            if (unityCurve == null) return -1;
            return unityCurve.AddKey(keyState); 
        }
        public int AddKey(float time, float value, bool notifyListeners = true)
        {
            if (unityCurve == null) return -1;
            return unityCurve.AddKey(time, value);
        }

        public float Evaluate(float t)
        {
            if (unityCurve == null) return 0;
            return unityCurve.Evaluate(t);
        }
        public EngineInternal.Vector2 Evaluate2(float t)
        {
            var val = Evaluate(t);
            return new EngineInternal.Vector2(val, val); 
        }
        public EngineInternal.Vector3 Evaluate3(float t)
        {
            var val = Evaluate(t);
            return new EngineInternal.Vector3(val, val, val);
        }

        public void FixNaN(bool notifyListeners)
        {
            if (unityCurve == null) return;

            var keys = unityCurve.keys;
            bool changed = false;
            for (int a = 0; a < keys.Length; a++)
            {
                var kf = keys[a];

                if (!float.IsFinite(kf.inTangent))
                {
                    changed = true;
                    kf.inTangent = 0; 
                }
                if (!float.IsFinite(kf.outTangent))
                {
                    changed = true;
                    kf.outTangent = 0;
                }
                if (!float.IsFinite(kf.inWeight))
                {
                    changed = true;
                    kf.inWeight = 1; 
                }
                if (!float.IsFinite(kf.outWeight))
                {
                    changed = true;
                    kf.outWeight = 1; 
                }  

                keys[a] = kf;
            }
            if (changed) unityCurve.keys = keys;
        }
        public void FixNaN() => FixNaN(true);

        public void ClearKeys()
        {
            if (unityCurve == null) return;

            unityCurve.ClearKeys();
        }

    }

}

#endif