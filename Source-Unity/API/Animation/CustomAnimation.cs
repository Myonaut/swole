#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Reflection;

using UnityEngine;
using UnityEngine.Jobs;

using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

using static Swole.API.Unity.Animation.CustomAnimator;
using Swole.Script;
using Swole.Animation;

namespace Swole.API.Unity.Animation
{

    [Serializable]
    public class CustomAnimation : SwoleObject<CustomAnimation, CustomAnimation.Serialized>, IAnimationAsset
    {

        public System.Type AssetType => GetType();
        public object Asset => this;

        private bool disposed;
        public bool isNotInternalAsset;
        public bool IsInternalAsset 
        {
            get => !isNotInternalAsset;
            set => isNotInternalAsset = !value;
        }

        public string CollectionID
        {
            get => string.Empty;
            set { }
        }
        public bool HasCollectionID => false;

        public bool IsValid => !disposed;
        public void Dispose()
        {
            if (!IsInternalAsset && !disposed)
            {
                FlushJobData();
            }
            disposed = true;
        }
        public void DisposeSelf() => Dispose();
        public void Delete() => Dispose();

        public bool IsIdenticalAsset(ISwoleAsset asset) => ReferenceEquals(this, asset); 

        public const int DefaultFrameRate = 30;
        //public const int DefaultJobCurveSampleRate = 4;
        public const int DefaultJobCurveSampleRate = 16;

        public string ID
        {
            get
            {
                string package = PackageInfo.GetIdentityString();
                return (string.IsNullOrWhiteSpace(package) ? string.Empty : (package + "/")) + Name;
            }
        }
         
        public bool HasKeyframes 
        { 
            get 
            { 
                if (transformAnimationCurves != null)
                {
                    bool HasKeys(ITransformCurve curve)
                    {
                        if (curve != null && curve.HasKeyframes) return true;
                        return false;
                    }
                    foreach(var info in transformAnimationCurves)
                    {
                        if (info.infoMain.curveIndex >= 0)
                        {
                            if (info.infoMain.isLinear)
                            {
                                if (HasKeys(transformLinearCurves[info.infoMain.curveIndex])) return true;
                            }
                            else
                            {
                                if (HasKeys(transformCurves[info.infoMain.curveIndex])) return true;
                            }
                        }
                        if (info.infoBase.curveIndex >= 0)
                        {
                            if (info.infoBase.isLinear)
                            {
                                if (HasKeys(transformLinearCurves[info.infoBase.curveIndex])) return true;
                            }
                            else
                            {
                                if (HasKeys(transformCurves[info.infoBase.curveIndex])) return true;
                            }
                        }
                    }
                }
                if (propertyAnimationCurves != null)
                {
                    bool HasKeys(IPropertyCurve curve)
                    {
                        if (curve != null && curve.HasKeyframes) return true; 
                        return false;
                    }
                    foreach (var info in propertyAnimationCurves)
                    {
                        if (info.infoMain.curveIndex >= 0)
                        {
                            if (info.infoMain.isLinear)
                            {
                                if (HasKeys(propertyLinearCurves[info.infoMain.curveIndex])) return true;
                            }
                            else
                            {
                                if (HasKeys(propertyCurves[info.infoMain.curveIndex])) return true;
                            }
                        }
                        if (info.infoBase.curveIndex >= 0)
                        {
                            if (info.infoBase.isLinear)
                            {
                                if (HasKeys(propertyLinearCurves[info.infoBase.curveIndex])) return true;
                            }
                            else
                            {
                                if (HasKeys(propertyCurves[info.infoBase.curveIndex])) return true;
                            }
                        }
                    }
                }

                return false;
            } 
        }
        public float GetClosestKeyframeTime(float referenceTime, bool includeReferenceTime = true, IntFromDecimalDelegate getFrameIndex = null)
        {
            float closestTime = 0;
            float minDiff = float.MaxValue;
             
            if (transformAnimationCurves != null)
            {
                void GetClosestTime(ITransformCurve curve) 
                {
                    float closest = curve.GetClosestKeyframeTime(referenceTime, framesPerSecond, includeReferenceTime, getFrameIndex); 
                    float diff = referenceTime - closest;
                    if (diff > minDiff) return; 
                    closestTime = closest;
                    minDiff = diff;
                }
                
                foreach (var info in transformAnimationCurves)
                {
                    if (info.infoMain.curveIndex >= 0)
                    {
                        if (info.infoMain.isLinear)
                        {
                            GetClosestTime(transformLinearCurves[info.infoMain.curveIndex]);
                        }
                        else
                        {
                            GetClosestTime(transformCurves[info.infoMain.curveIndex]);
                        }
                    }
                    if (info.infoBase.curveIndex >= 0)
                    {
                        if (info.infoBase.isLinear)
                        {
                            GetClosestTime(transformLinearCurves[info.infoBase.curveIndex]);
                        }
                        else
                        {
                            GetClosestTime(transformCurves[info.infoBase.curveIndex]);
                        }
                    }
                }
            }
            if (propertyAnimationCurves != null)
            {
                void GetClosestTime(IPropertyCurve curve)
                {
                    closestTime = Mathf.Min(closestTime, curve.GetClosestKeyframeTime(referenceTime, framesPerSecond, includeReferenceTime, getFrameIndex));
                }
                foreach (var info in propertyAnimationCurves)
                {
                    if (info.infoMain.curveIndex >= 0)
                    {
                        if (info.infoMain.isLinear)
                        {
                            GetClosestTime(propertyLinearCurves[info.infoMain.curveIndex]);
                        }
                        else
                        {
                            GetClosestTime(propertyCurves[info.infoMain.curveIndex]);
                        }
                    }
                    if (info.infoBase.curveIndex >= 0)
                    {
                        if (info.infoBase.isLinear)
                        {
                            GetClosestTime(propertyLinearCurves[info.infoBase.curveIndex]);
                        }
                        else
                        {
                            GetClosestTime(propertyCurves[info.infoBase.curveIndex]);
                        }
                    }
                }
            }

            return closestTime;
        }

        #region Serialization

        public override CustomAnimation.Serialized AsSerializableStruct() => this;
        public override string AsJSON(bool prettyPrint = false) => AsSerializableStruct().AsJSON(prettyPrint);

        [Serializable]
        public struct Serialized : ISerializableContainer<CustomAnimation, CustomAnimation.Serialized>
        {

            public ContentInfo contentInfo;
            public string SerializedName => contentInfo.name;

            public int framesPerSecond;

            public SerializedAnimationCurve timeCurve;

            public TransformLinearCurve.Serialized[] transformLinearCurves;
            public TransformCurve.Serialized[] transformCurves;

            public int jobCurveSampleRate;

            public PropertyLinearCurve.Serialized[] propertyLinearCurves;
            public PropertyCurve.Serialized[] propertyCurves;

            public CurveInfoPair[] transformAnimationCurves;
            public CurveInfoPair[] propertyAnimationCurves;

            public CustomAnimation.Event.Serialized[] events;

            public CustomAnimation AsOriginalType(PackageInfo packageInfo = default) => new CustomAnimation(this, packageInfo);

            public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);

            public string AsJSON(bool prettyPrint = false) => swole.Engine.ToJson(this, prettyPrint);
            public static Serialized FromJSON(string json) => swole.FromJson<Serialized>(json);
        }

        public static implicit operator Serialized(CustomAnimation animation)
        {
            Serialized s = new Serialized();

            s.contentInfo = animation.contentInfo;
            s.framesPerSecond = animation.framesPerSecond;
            s.timeCurve = animation.timeCurve;
            s.jobCurveSampleRate = animation.jobCurveSampleRate;

            if (animation.transformLinearCurves != null)
            {
                s.transformLinearCurves = new TransformLinearCurve.Serialized[animation.transformLinearCurves.Length];
                for (int a = 0; a < s.transformLinearCurves.Length; a++) s.transformLinearCurves[a] = animation.transformLinearCurves[a];
            }
            if (animation.transformCurves != null)
            {
                s.transformCurves = new TransformCurve.Serialized[animation.transformCurves.Length];
                for (int a = 0; a < s.transformCurves.Length; a++) s.transformCurves[a] = animation.transformCurves[a];
            }

            if (animation.propertyLinearCurves != null)
            {
                s.propertyLinearCurves = new PropertyLinearCurve.Serialized[animation.propertyLinearCurves.Length];
                for (int a = 0; a < s.propertyLinearCurves.Length; a++) s.propertyLinearCurves[a] = animation.propertyLinearCurves[a];
            }
            if (animation.propertyCurves != null)
            {
                s.propertyCurves = new PropertyCurve.Serialized[animation.propertyCurves.Length];
                for (int a = 0; a < s.propertyCurves.Length; a++) s.propertyCurves[a] = animation.propertyCurves[a];
            }

            s.transformAnimationCurves = animation.transformAnimationCurves;
            s.propertyAnimationCurves = animation.propertyAnimationCurves;

            if (animation.events != null)
            {
                s.events = new CustomAnimation.Event.Serialized[animation.events.Length];
                for (int a = 0; a < animation.events.Length; a++) s.events[a] = animation.events[a];
            }
             
            return s;
        }

        public CustomAnimation(CustomAnimation.Serialized serializable, PackageInfo packageInfo = default) : base(serializable)
        {
            isNotInternalAsset = true;

            this.packageInfo = packageInfo;

            this.contentInfo = serializable.contentInfo;
            this.framesPerSecond = serializable.framesPerSecond;
            this.timeCurve = serializable.timeCurve.AsEditableAnimationCurve();  
            this.jobCurveSampleRate = serializable.jobCurveSampleRate;

            if (serializable.transformLinearCurves != null)
            {
                this.transformLinearCurves = new TransformLinearCurve[serializable.transformLinearCurves.Length];
                for (int a = 0; a < this.transformLinearCurves.Length; a++) this.transformLinearCurves[a] = serializable.transformLinearCurves[a].AsOriginalType(packageInfo);
            }
            if (serializable.transformCurves != null)
            {
                this.transformCurves = new TransformCurve[serializable.transformCurves.Length];
                for (int a = 0; a < this.transformCurves.Length; a++) this.transformCurves[a] = serializable.transformCurves[a].AsOriginalType(packageInfo);
            }

            if (serializable.propertyLinearCurves != null)
            {
                this.propertyLinearCurves = new PropertyLinearCurve[serializable.propertyLinearCurves.Length];
                for (int a = 0; a < this.propertyLinearCurves.Length; a++) this.propertyLinearCurves[a] = serializable.propertyLinearCurves[a].AsOriginalType(packageInfo);
            }
            if (serializable.propertyCurves != null)
            {
                this.propertyCurves = new PropertyCurve[serializable.propertyCurves.Length];
                for (int a = 0; a < this.propertyCurves.Length; a++) this.propertyCurves[a] = serializable.propertyCurves[a].AsOriginalType(packageInfo);
            }

            this.transformAnimationCurves = serializable.transformAnimationCurves;
            this.propertyAnimationCurves = serializable.propertyAnimationCurves;

            if (serializable.events != null)
            { 
                this.events = new CustomAnimation.Event[serializable.events.Length];
                for (int a = 0; a < serializable.events.Length; a++) this.events[a] = serializable.events[a].AsOriginalType(packageInfo); 
            }

        }

        #endregion

        public object Clone() => Duplicate();
        public CustomAnimation Duplicate()
        {

            var clone = new CustomAnimation(contentInfo, framesPerSecond, jobCurveSampleRate, timeCurve == null ? null : timeCurve.Duplicate(), null, null, null, null, 
                transformAnimationCurves == null ? null : (CurveInfoPair[])transformAnimationCurves.Clone(), propertyAnimationCurves == null ? null : (CurveInfoPair[])propertyAnimationCurves.Clone(), null, packageInfo);

            clone.isNotInternalAsset = true;

            if (transformLinearCurves != null)
            {
                TransformLinearCurve[] transformLinearCurves_clone = new TransformLinearCurve[transformLinearCurves.Length];
                for (int a = 0; a < transformLinearCurves.Length; a++) transformLinearCurves_clone[a] = transformLinearCurves[a].Duplicate();

                clone.transformLinearCurves = transformLinearCurves_clone;
            }

            if (transformCurves != null)
            {
                TransformCurve[] transformCurves_clone = new TransformCurve[transformCurves.Length];
                for (int a = 0; a < transformCurves.Length; a++) transformCurves_clone[a] = transformCurves[a].Duplicate();

                clone.transformCurves = transformCurves_clone;
            }

            if (propertyLinearCurves != null)
            {
                PropertyLinearCurve[] propertyLinearCurves_clone = new PropertyLinearCurve[propertyLinearCurves.Length];
                for (int a = 0; a < propertyLinearCurves.Length; a++) propertyLinearCurves_clone[a] = propertyLinearCurves[a].Duplicate();

                clone.propertyLinearCurves = propertyLinearCurves_clone;
            }

            if (propertyCurves != null)
            {
                PropertyCurve[] propertyCurves_clone = new PropertyCurve[propertyCurves.Length];
                for (int a = 0; a < propertyCurves.Length; a++) propertyCurves_clone[a] = propertyCurves[a].Duplicate();

                clone.propertyCurves = propertyCurves_clone;
            }

            if (events != null)
            {
                Event[] events_clone = new Event[events.Length];
                for(int a = 0; a < events.Length; a++) events_clone[a] = events[a].Duplicate();
                clone.events = events_clone;
            }

            return clone;
        }

        private string originPath;
        public string OriginPath => originPath;
        public IContent SetOriginPath(string path)
        {
            originPath = path;
            return this;
        }
        private string relativePath;
        public string RelativePath => relativePath;
        public IContent SetRelativePath(string path)
        {
            relativePath = path;
            return this;
        }

        public const float _defaultEventCompletionTimeout = 0.008f;
        [Serializable]
        public class Event : SwoleObject<Event, Event.Serialized>, ICloneable
        {

            [NonSerialized]
            private int? cachedId;
            public int CachedId
            {
                get
                {
                    if (cachedId.HasValue) return cachedId.Value;

                    cachedId = RuntimeHelpers.GetHashCode(this);
                    return cachedId.Value;
                }
                set
                {
                    cachedId = value;
                }
            }

            [NonSerialized]
            private Event original;

            #region Serialization

            public override Event.Serialized AsSerializableStruct() => this;
            public override string AsJSON(bool prettyPrint = false) => AsSerializableStruct().AsJSON(prettyPrint);

            [Serializable]
            public struct Serialized : ISerializableContainer<Event, Event.Serialized>
            {

                public string name;
                public string SerializedName => name;
                public float timelinePosition;
                public int priority;
                public string source;

                public Event AsOriginalType(PackageInfo packageInfo = default) => new Event(this, packageInfo);

                public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);

                public string AsJSON(bool prettyPrint = false) => swole.Engine.ToJson(this, prettyPrint);
                public static Serialized FromJSON(string json) => swole.FromJson<Serialized>(json);
            }

            public static implicit operator Serialized(Event _event)
            {
                Serialized s = new Serialized();

                s.name = _event.name;
                s.timelinePosition = _event.timelinePosition;
                s.priority = _event.priority;
                s.source = _event.source;

                return s;
            }

            public Event(Event.Serialized serializable, PackageInfo packageInfo = default) : base(serializable)
            {

                this.name = serializable.name;
                this.timelinePosition = serializable.timelinePosition;
                this.priority = serializable.priority;
                this.source = serializable.source;

            }

            #endregion

            public Event() : base(default) { }

            public Event(string name, float timelinePosition, int priority, string source) : base(default)
            {
                this.name = name;
                this.timelinePosition = timelinePosition;
                this.priority = priority;
                this.source = source; 
            }

            public Serialized State
            {
                get => this;
                set
                {
                    this.name = value.name;
                    this.timelinePosition = value.timelinePosition;
                    this.priority = value.priority;
                    this.source = value.source;
                }
            }
            public void SetStateForThisAndOriginals(Serialized state)
            {
                State = state;
                if (original != null) original.SetStateForThisAndOriginals(state);
            }

            private string name;
            public string Name 
            {
                get => name;
                set => name = value;
            }
            public override string SerializedName => Name; 

            private float timelinePosition;
            public float TimelinePosition
            {
                get => timelinePosition;
                set => timelinePosition = value;
            }

            private int priority;
            /// <summary>
            /// If two events try to execute at the same timeline position, this value is used to determine which is executed first. Lower values execute sooner. The opposite is true if the animation is playing in reverse.
            /// </summary>
            public int Priority
            {
                get => priority;
                set => priority = value;
            }

            private string source;
            public string Source
            {
                get => source;
                set
                {
                    source = value;
                    cachedDependencies = null;
                    if (executable != null)
                    {
                        executable.Dispose();
                        executable = null;
                    }
                }
            }

            [NonSerialized]
            private PackageIdentifier[] cachedDependencies;

            public object Clone() => Duplicate(true);
            public Event Duplicate() => Duplicate(true);
            public Event Duplicate(bool isCopy)
            {
                var clone = new Event();

                if (isCopy)
                {
                    clone.cachedId = CachedId;
                    clone.original = this;
                }

                clone.name = name;
                clone.timelinePosition = timelinePosition;
                clone.priority = priority;
                clone.source = source;

                clone.cachedDependencies = cachedDependencies;

                return clone;
            }

            public void GetDependencies(List<PackageIdentifier> dependencies)
            {
                if (dependencies == null) return;

                if (cachedDependencies == null) 
                {
#if SWOLE_ENV
                    int startIndex = dependencies.Count;
                    swole.ExtractPackageDependencies(source, dependencies);
                    int count = dependencies.Count - startIndex;
                    cachedDependencies = new PackageIdentifier[count];
                    for (int a = startIndex; a < dependencies.Count; a++) cachedDependencies[a] = dependencies[a];
#endif
                } 
                else
                {
                    dependencies.AddRange(cachedDependencies);
                }
            }

            [NonSerialized]
            private ExecutableScript executable;
            public ExecutableScript Executable
            {
                get
                {
                    if (executable == null) executable = new ExecutableScript($"anim_event[{name}]", source, priority);
                    return executable;
                }
            }

            public void Execute(IRuntimeEnvironment environment, float timeout = _defaultEventCompletionTimeout, SwoleLogger logger = null) => Executable.ExecuteToCompletion(environment, timeout, logger);   

        }

        public Event[] events;
        public List<Event> GetEventsAtTime(float timelinePosition, List<Event> list = null)
        {
            if (list == null) list = new List<Event>();

            if (events != null)
            {
                foreach (var _event in events) if (_event.TimelinePosition == timelinePosition) list.Add(_event);
                list.Sort(SortEventsAscending);
            }

            return list;
        }
        public List<Event> GetEventsAtFrame(decimal timelinePosition, List<Event> list = null)
        {
            int frame = AnimationTimeline.CalculateFrameAtTimelinePosition(timelinePosition, framesPerSecond);
            return GetEventsAtFrame(frame, list);
        }
        public List<Event> GetEventsAtFrame(int frameIndex, List<Event> list = null)
        {
            if (list == null) list = new List<Event>();

            if (events != null)
            {
                foreach (var _event in events) if (_event != null && AnimationTimeline.CalculateFrameAtTimelinePosition((decimal)_event.TimelinePosition, framesPerSecond) == frameIndex) list.Add(_event); 
                list.Sort(SortEventsAscending);
            }

            return list;
        }

        public List<PackageIdentifier> ExtractPackageDependencies(List<PackageIdentifier> dependencies = null)
        {

            if (dependencies == null) dependencies = new List<PackageIdentifier>();

            if (events != null)
            {
                foreach (var _event in events) if (_event != null) _event.GetDependencies(dependencies);
            }
            
            return dependencies;

        }

        public IContent CreateCopyAndReplaceContentInfo(ContentInfo info) 
        { 
            var copy = new CustomAnimation(info, framesPerSecond, jobCurveSampleRate, timeCurve, transformLinearCurves, transformCurves, propertyLinearCurves, propertyCurves, transformAnimationCurves, propertyAnimationCurves, events, packageInfo);
            copy.jobData = jobData;
            return copy;
        }
        public IContent CreateShallowCopyAndReplaceContentInfo(ContentInfo info) => CreateCopyAndReplaceContentInfo(info);

        public PackageInfo PackageInfo => packageInfo;
        public ContentInfo ContentInfo => contentInfo;
        public string Name => contentInfo.name;
        public override string SerializedName => Name; 
        public string Author => contentInfo.author;
        public string CreationDate => contentInfo.creationDate;
        public string LastEditDate => contentInfo.lastEditDate;
        public string Description => contentInfo.description;

        protected readonly PackageInfo packageInfo;
        protected readonly ContentInfo contentInfo;

        public CustomAnimation() : base(default) { }

        public CustomAnimation(string name, string author, DateTime creationDate, DateTime lastEditDate, string description, int framesPerSecond, int jobCurveSampleRate, EditableAnimationCurve timeCurve, TransformLinearCurve[] transformLinearCurves, TransformCurve[] transformCurves, PropertyLinearCurve[] propertyLinearCurves, PropertyCurve[] propertyCurves, CurveInfoPair[] transformAnimationCurves, CurveInfoPair[] propertyAnimationCurves, CustomAnimation.Event[] events, PackageInfo packageInfo = default) : this(new ContentInfo() { name = name, author = author, creationDate = creationDate.ToString(IContent.dateFormat), lastEditDate = lastEditDate.ToString(IContent.dateFormat), description = description }, framesPerSecond, jobCurveSampleRate, timeCurve, transformLinearCurves, transformCurves, propertyLinearCurves, propertyCurves, transformAnimationCurves, propertyAnimationCurves, events, packageInfo) { }

        public CustomAnimation(string name, string author, string creationDate, string lastEditDate, string description, int framesPerSecond, int jobCurveSampleRate, EditableAnimationCurve timeCurve, TransformLinearCurve[] transformLinearCurves, TransformCurve[] transformCurves, PropertyLinearCurve[] propertyLinearCurves, PropertyCurve[] propertyCurves, CurveInfoPair[] transformAnimationCurves, CurveInfoPair[] propertyAnimationCurves, CustomAnimation.Event[] events, PackageInfo packageInfo = default) : this(new ContentInfo() { name = name, author = author, creationDate = creationDate, lastEditDate = lastEditDate, description = description }, framesPerSecond, jobCurveSampleRate, timeCurve, transformLinearCurves, transformCurves, propertyLinearCurves, propertyCurves, transformAnimationCurves, propertyAnimationCurves, events, packageInfo) { }

        public CustomAnimation(ContentInfo contentInfo, int framesPerSecond, int jobCurveSampleRate, EditableAnimationCurve timeCurve, TransformLinearCurve[] transformLinearCurves, TransformCurve[] transformCurves, PropertyLinearCurve[] propertyLinearCurves, PropertyCurve[] propertyCurves, CurveInfoPair[] transformAnimationCurves, CurveInfoPair[] propertyAnimationCurves, CustomAnimation.Event[] events, PackageInfo packageInfo = default) : base(default)
        {
            this.packageInfo = packageInfo;
            this.contentInfo = contentInfo;

            this.framesPerSecond = framesPerSecond;
            this.jobCurveSampleRate = jobCurveSampleRate;

            this.timeCurve = timeCurve;

            this.transformLinearCurves = transformLinearCurves;
            this.transformCurves = transformCurves;

            this.propertyLinearCurves = propertyLinearCurves;
            this.propertyCurves = propertyCurves;

            this.transformAnimationCurves = transformAnimationCurves;
            this.propertyAnimationCurves = propertyAnimationCurves;

            this.events = events;

        }

        public static float WrapNormalizedTime(float t, WrapMode preWrapMode, WrapMode postWrapMode)
        {

            if (t < 0f)
            {
                /*switch (preWrapMode)
                {
                    default:
                        return 0;
                    case WrapMode.Loop:
                        t = 1f - math.abs(t) % 1f;
                        break;
                    case WrapMode.PingPong:
                        t = Maths.pingpong(t, 1f);
                        break;
                }*/
                return AnimationUtils.WrapNormalizedTimeBackward(t, preWrapMode);
            }
            else if (t > 1f)
            {
                /*switch (postWrapMode)
                {
                    default:
                        return 1;
                    case WrapMode.Loop:
                        t %= 1f;
                        break;
                    case WrapMode.PingPong:
                        t = Maths.pingpong(t, 1f);
                        break;
                }*/
                return AnimationUtils.WrapNormalizedTimeForward(t, preWrapMode); 
            }

            return t;

        }

        public static MemberInfo GetFieldOrProperty(Component component, string memberName)
        {
            MemberInfo info = component.GetType().GetField(memberName, BindingFlags.Instance | BindingFlags.Public);
            if (info == null) info = component.GetType().GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public);
            return info;
        }

        public static string GetPropertyId(Component component, string propName)
        {
            return component.gameObject.name + "." + component.GetType().Name + "." + propName;
        }

        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct CurveInfo
        {

            public bool isLinear;

            public int curveIndex;

            public static CurveInfo Empty => new CurveInfo() { curveIndex = -1 };

        }

        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct CurveInfoPair
        {

            public CurveInfo infoBase;
            public CurveInfo infoMain;

            public static CurveInfoPair Empty
            {
                get
                {
                    CurveInfoPair pair = default;

                    pair.infoBase = CurveInfo.Empty;
                    pair.infoMain = CurveInfo.Empty;

                    return pair;
                }
            }

        }

        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct CurveHeader
        {
            public int index;
            public int sampleCount;

            public int frameHeaderIndex;
            public int frameCount;

            public WrapMode preWrapMode;
            public WrapMode postWrapMode;

            public float timeRatio;
            public float normalizedTimeStart;
            public float normalizedTimeLength;

            public bool3 validityPosition;
            public bool3 validityScale;
            public bool4 validityRotation;

            public CurveHeader(int index, int sampleCount, int frameHeaderIndex, int frameCount, WrapMode preWrapMode, WrapMode postWrapMode, float timeRatio, float normalizedTimeStart, float normalizedTimeLength, bool3 validityPosition, bool4 validityRotation, bool3 validityScale)
            {
                this.index = index;
                this.sampleCount = sampleCount;

                this.frameHeaderIndex = frameHeaderIndex;
                this.frameCount = frameCount;

                this.preWrapMode = preWrapMode;
                this.postWrapMode = postWrapMode;

                this.timeRatio = timeRatio;
                this.normalizedTimeStart = normalizedTimeStart;
                this.normalizedTimeLength = normalizedTimeLength;

                this.validityPosition = validityPosition;
                this.validityRotation = validityRotation;
                this.validityScale = validityScale;
            }
        }

        public int framesPerSecond = DefaultFrameRate;

        public EditableAnimationCurve timeCurve;

        public float ScaleTime(float time) => ScaleTime(time, LengthInSeconds, timeCurve);
        public static float ScaleTime(float time, float lengthInSeconds, AnimationCurve timeCurve)
        {
            float normalizedTime = time / lengthInSeconds;
            return time * (ScaleNormalizedTime(normalizedTime, timeCurve) / normalizedTime);
        }
        public float ScaleNormalizedTime(float normalizedTime) => ScaleNormalizedTime(normalizedTime, timeCurve);
        public static float ScaleNormalizedTime(float normalizedTime, AnimationCurve timeCurve)
        {
            return timeCurve == null || timeCurve.length <= 0 ? normalizedTime : timeCurve.Evaluate(normalizedTime);
        }

        public TransformLinearCurve[] transformLinearCurves;
        public TransformCurve[] transformCurves;

        public bool TryGetTransformCurve(string transformName, out TransformCurve curve)
        {
            curve = null;
            if (transformAnimationCurves == null) return false;

            for (int a = 0; a < transformAnimationCurves.Length; a++)
            {
                var info = transformAnimationCurves[a];
                if (info.infoMain.curveIndex >= 0 && !info.infoMain.isLinear)
                {
                    var curve_ = transformCurves[info.infoMain.curveIndex];
                    if (curve_.TransformName == transformName)
                    {
                        curve = curve_;
                        return true;
                    }
                }
            }

            return false;
        }

        public bool TryGetTransformLinearCurve(string transformName, out TransformLinearCurve curve)
        {
            curve = null;
            if (transformAnimationCurves == null) return false;

            for (int a = 0; a < transformAnimationCurves.Length; a++)
            {
                var info = transformAnimationCurves[a];
                if (info.infoMain.curveIndex >= 0 && info.infoMain.isLinear)
                {
                    var curve_ = transformLinearCurves[info.infoMain.curveIndex];
                    if (curve_.TransformName == transformName)
                    {
                        curve = curve_;
                        return true;
                    }
                }
            }

            return false;
        }

        public bool TryGetBaseTransformCurve(string transformName, out TransformCurve curve)
        {
            curve = null;
            if (transformAnimationCurves == null) return false;

            for (int a = 0; a < transformAnimationCurves.Length; a++)
            {
                var info = transformAnimationCurves[a];
                if (info.infoBase.curveIndex >= 0 && !info.infoBase.isLinear)
                {
                    var curve_ = transformCurves[info.infoBase.curveIndex];
                    if (curve_.TransformName == transformName)
                    {
                        curve = curve_;
                        return true;
                    }
                }
            }

            return false;
        }

        public bool TryGetBaseTransformLinearCurve(string transformName, out TransformLinearCurve curve)
        {
            curve = null;
            if (transformAnimationCurves == null) return false;

            for (int a = 0; a < transformAnimationCurves.Length; a++)
            {
                var info = transformAnimationCurves[a];
                if (info.infoBase.curveIndex >= 0 && info.infoBase.isLinear)
                {
                    var curve_ = transformLinearCurves[info.infoBase.curveIndex];
                    if (curve_.TransformName == transformName)
                    {
                        curve = curve_;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// The number of samples per frame in one second of a curve's timeline (will pick the time slice with the most frames). 
        /// </summary>
        //public int jobCurveSampleRate = DefaultJobCurveSampleRate;

        /// <summary>
        /// The number of samples per second between two frames of animation in a job. 
        /// </summary>
        public int jobCurveSampleRate = DefaultJobCurveSampleRate;

        public class JobData : IDisposable
        {
            public int linearTransformCurvesIndexOffset;

            [NonSerialized]
            public NativeArray<ITransformCurve.Data> transformCurvesSampled;
            [NonSerialized]
            public NativeArray<CurveHeader> transformCurvesSampledHeaders;
            [NonSerialized]
            public NativeArray<FrameHeader> transformCurvesSampledFrameHeaders;

            [NonSerialized]
            public NativeArray<CurveInfoPair> transformAnimationCurvesForJobs;

            public void Dispose()
            {
                linearTransformCurvesIndexOffset = 0;

                if (transformCurvesSampled.IsCreated)
                {
                    transformCurvesSampled.Dispose();
                    transformCurvesSampled = default;
                }
                if (transformCurvesSampledHeaders.IsCreated)
                {
                    transformCurvesSampledHeaders.Dispose();
                    transformCurvesSampledHeaders = default;
                }
                if (transformCurvesSampledFrameHeaders.IsCreated)
                {
                    transformCurvesSampledFrameHeaders.Dispose();
                    transformCurvesSampledFrameHeaders = default;
                }

                if (transformAnimationCurvesForJobs.IsCreated)
                {
                    transformAnimationCurvesForJobs.Dispose();
                    transformAnimationCurvesForJobs = default;
                }
            }
        }

        [NonSerialized]
        protected JobData jobData;
        protected void CreateJobData()
        {
            if (jobData != null) FlushJobData();

            jobData = new JobData();
            PersistentJobDataTracker.Track(jobData); 
        }
        /// <summary>
        /// Resets all job data arrays. Should only be done if the animation data has changed somehow.
        /// </summary>
        public void FlushJobData()
        {
            if (jobData != null)
            {
                PersistentJobDataTracker.Untrack(jobData);
                jobData.Dispose();
                jobData = null;
            }
        }

        [Obsolete]
        private int SampleTransformCurve(List<ITransformCurve.Data> sampleData, ITransformCurve curve, float animationLength, out float timeRatio)
        {
            float length = curve.GetLengthInSeconds(framesPerSecond);
            timeRatio = length <= 0 ? 0 : (animationLength / length);
            //Debug.Log(Name + ": " + animationLength + "/" + length + "  =  " + timeRatio);

            int maxFrameCount = curve.GetMaxFrameCountInTimeSlice(framesPerSecond);

            int sampleCount = Mathf.Max(2, Mathf.CeilToInt(length * jobCurveSampleRate * maxFrameCount)); // Calculate sample count from the maximum number of frames found in a 1 second time slice on the curve's timeline

            for (int a = 0; a < sampleCount; a++)
            {
                float t = a / (sampleCount - 1f);
                sampleData.Add(curve.Evaluate(t));
            }

            return sampleCount;
        }

        [Serializable]
        public struct FrameHeader
        {
            /// <summary>
            /// The frame's position in the animation timeline
            /// </summary>
            public float time;
            /// <summary>
            /// The frame's normalized position in reference to the first and last frame positions
            /// </summary>
            public float normalizedLocalTime;

            public int sampleIndex;
            public int sampleCount;
        }
        private int SampleTransformCurve(List<ITransformCurve.Data> sampleData, List<FrameHeader> frameHeaders, ITransformCurve curve, float animationLength, out float timeRatio, out int frameHeaderIndex, out int frameCount, bool NaNsafe = true)
        {
            //float length = curve.GetLengthInSeconds(framesPerSecond);
            //timeRatio = length <= 0 ? 0 : animationLength / length;

            timeRatio = 0;
            int sampleCount = 0;

            frameHeaderIndex = frameHeaders.Count;
            curve.GetFrameTimes(framesPerSecond, frameHeaders);
            frameCount = frameHeaders.Count - frameHeaderIndex;

            if (frameCount > 0)
            {
                float finalTime = frameHeaders[frameHeaderIndex + frameCount - 1].time;
                float length = finalTime - frameHeaders[frameHeaderIndex].time;
                timeRatio = length <= 0 ? 0 : (animationLength / length);

                float startFrameTime = frameHeaders[frameHeaderIndex].time;
                if (frameCount == 1)
                {
                    // Add single sample for single frame curve
                    var header = frameHeaders[frameHeaderIndex];
                    header.sampleIndex = sampleData.Count; 

                    var sample = curve.Evaluate(0);
                    int tempLocalSampleCount = 0;
                    if (NaNsafe && (float.IsNaN(sample.localPosition.x) || float.IsNaN(sample.localPosition.y) || float.IsNaN(sample.localPosition.z) ||
                        float.IsNaN(sample.localRotation.value.x) || float.IsNaN(sample.localRotation.value.y) || float.IsNaN(sample.localRotation.value.z) || float.IsNaN(sample.localRotation.value.w) ||
                        float.IsNaN(sample.localScale.x) || float.IsNaN(sample.localScale.y) || float.IsNaN(sample.localScale.z))) 
                    { 
                        swole.LogWarning($"Animation {Name} contains NaN animation data in transform curve '{curve.TransformName}' and will cause errors. Likely has corrupt keyframe tangents somewhere. localPos({sample.localPosition}) localRot({sample.localRotation}) localScale({sample.localScale})");
                    }
                    else
                    {
                        sampleData.Add(sample);
                        tempLocalSampleCount++;
                    }

                    sampleCount += tempLocalSampleCount;
                    header.sampleCount = tempLocalSampleCount;
                    frameHeaders[frameHeaderIndex] = header; 
                }
                else
                {
                    for (int a = 0; a < frameCount; a++)
                    {
                        int headerIndex = frameHeaderIndex + a;
                        var header = frameHeaders[headerIndex];
                        header.normalizedLocalTime = length <= 0 ? 0 : ((header.time - startFrameTime) / length); 
                        header.sampleIndex = sampleData.Count;

                        if (a >= frameCount - 1) // Final frame has no samples
                        {
                            frameHeaders[headerIndex] = header;
                            break;
                        }

                        var nextHeader = frameHeaders[headerIndex + 1]; 

                        int localSampleCount = Mathf.Max(2, Mathf.CeilToInt((nextHeader.time - header.time) * jobCurveSampleRate));

                        int tempLocalSampleCount = 0;
                        for (int b = 0; b < localSampleCount; b++)
                        {
                            float t = b / (localSampleCount - 1f); 

                            var sample = curve.Evaluate(((t * (nextHeader.time - header.time)) + header.time) / finalTime);
                            if (NaNsafe && (float.IsNaN(sample.localPosition.x) || float.IsNaN(sample.localPosition.y) || float.IsNaN(sample.localPosition.z) ||
                                float.IsNaN(sample.localRotation.value.x) || float.IsNaN(sample.localRotation.value.y) || float.IsNaN(sample.localRotation.value.z) || float.IsNaN(sample.localRotation.value.w) ||
                                float.IsNaN(sample.localScale.x) || float.IsNaN(sample.localScale.y) || float.IsNaN(sample.localScale.z))) 
                            { 
                                swole.LogWarning($"Animation {Name} contains NaN animation data in transform curve '{curve.TransformName}' and will cause errors. Likely has corrupt keyframe tangents somewhere. localPos({sample.localPosition}) localRot({sample.localRotation}) localScale({sample.localScale})"); 
                            } 
                            else
                            {
                                sampleData.Add(sample);
                                tempLocalSampleCount++;   
                            }
                        }

                        //sampleCount += localSampleCount;
                        //header.sampleCount = localSampleCount;
                        sampleCount += tempLocalSampleCount;
                        header.sampleCount = tempLocalSampleCount;
                        frameHeaders[headerIndex] = header;
                    }
                }
            }

            return sampleCount;
        }

        public int LinearTransformCurvesSampledIndexOffset => jobData == null ? 0 : jobData.linearTransformCurvesIndexOffset;

        /// <summary>
        /// Once this data has been initialized, it will persist until the app is closed. Any changes to the underlying curve data of this animation will not be reflected in this array.
        /// </summary>
        public NativeArray<ITransformCurve.Data> TransformCurvesSampled
        {

            get
            {

                if (jobData == null) CreateJobData();
                if (!jobData.transformCurvesSampled.IsCreated)
                {

                    float animationLength = LengthInSeconds; 

                    jobData.linearTransformCurvesIndexOffset = transformLinearCurves == null ? 0 : transformLinearCurves.Length;
                    jobData.transformCurvesSampledHeaders = new NativeArray<CurveHeader>(jobData.linearTransformCurvesIndexOffset + (transformCurves == null ? 0 : transformCurves.Length), Allocator.Persistent);

                    List<ITransformCurve.Data> sampleData = new List<ITransformCurve.Data>();
                    List<FrameHeader> frameHeaders = new List<FrameHeader>();

                    int index = 0;
                    void AddSamples(int i, ITransformCurve curve)
                    { 
                        int sampleCount = SampleTransformCurve(sampleData, frameHeaders, curve, animationLength, out float timeRatio, out int frameHeaderIndex, out int frameCount);

                        if (sampleCount > 0)
                        {
                            jobData.transformCurvesSampledHeaders[i] = new CurveHeader(index, sampleCount, frameHeaderIndex, frameCount, curve.PreWrapMode, curve.PostWrapMode,
                                timeRatio, frameHeaders[frameHeaderIndex].time / (animationLength <= 0 ? 1 : animationLength), frameCount <= 0 ? 0 : ((frameHeaders[frameHeaderIndex + frameCount - 1].time - frameHeaders[frameHeaderIndex].time) / (animationLength <= 0 ? 1 : animationLength)),
                                curve.ValidityPosition, curve.ValidityRotation, curve.ValidityScale);
                            index += sampleCount;
                        } 
                        else
                        {
                            jobData.transformCurvesSampledHeaders[i] = new CurveHeader(index, sampleCount, frameHeaderIndex, frameCount, curve.PreWrapMode, curve.PostWrapMode, 0, 0, 0, false, false, false); 
                        }
                    }

                    if (transformLinearCurves != null)
                    {
                        for (int a = 0; a < transformLinearCurves.Length; a++) AddSamples(a, transformLinearCurves[a]);
                    }
                    if (transformCurves != null)
                    {
                        for (int a = 0; a < transformCurves.Length; a++) AddSamples(a + jobData.linearTransformCurvesIndexOffset, transformCurves[a]);
                    }

                    jobData.transformCurvesSampled = new NativeArray<ITransformCurve.Data>(sampleData.ToArray(), Allocator.Persistent);
                    jobData.transformCurvesSampledFrameHeaders = new NativeArray<FrameHeader>(frameHeaders.ToArray(), Allocator.Persistent); 

                }

                return jobData.transformCurvesSampled;

            }

        }

        public NativeArray<CurveHeader> TransformCurvesSampledHeaders => jobData == null ? default : jobData.transformCurvesSampledHeaders;
        public NativeArray<FrameHeader> TransformCurvesSampledFrameHeaders => jobData == null ? default : jobData.transformCurvesSampledFrameHeaders;

        public NativeArray<CurveInfoPair> TransformAnimationCurvesForJobs
        {

            get
            {

                if (jobData == null) CreateJobData();
                if (!jobData.transformAnimationCurvesForJobs.IsCreated)
                {
                    jobData.transformAnimationCurvesForJobs = transformAnimationCurves == null ? new NativeArray<CurveInfoPair>(0, Allocator.Persistent) : new NativeArray<CurveInfoPair>(transformAnimationCurves, Allocator.Persistent);
                }

                return jobData.transformAnimationCurvesForJobs;

            }

        }

        public PropertyLinearCurve[] propertyLinearCurves;
        public PropertyCurve[] propertyCurves;

        public float LengthInSeconds
        {

            get
            {

                float length = 0;

                if (transformLinearCurves != null) for (int a = 0; a < transformLinearCurves.Length; a++) length = math.max(length, transformLinearCurves[a].GetLengthInSeconds(framesPerSecond));
                if (transformCurves != null) for (int a = 0; a < transformCurves.Length; a++) length = math.max(length, transformCurves[a].GetLengthInSeconds(framesPerSecond));

                if (propertyLinearCurves != null) for (int a = 0; a < propertyLinearCurves.Length; a++) length = math.max(length, propertyLinearCurves[a].GetLengthInSeconds(framesPerSecond));
                if (propertyCurves != null) for (int a = 0; a < propertyCurves.Length; a++) length = math.max(length, propertyCurves[a].GetLengthInSeconds(framesPerSecond));

                return length;

            }

        }

        public float LengthInFrames
        {

            get
            {

                int length = 0;

                if (transformLinearCurves != null) for (int a = 0; a < transformLinearCurves.Length; a++) length = math.max(length, transformLinearCurves[a].FrameLength);
                if (transformCurves != null) for (int a = 0; a < transformCurves.Length; a++) length = math.max(length, Mathf.CeilToInt(transformCurves[a].GetLengthInSeconds(framesPerSecond) * framesPerSecond));

                if (propertyLinearCurves != null) for (int a = 0; a < propertyLinearCurves.Length; a++) length = math.max(length, propertyLinearCurves[a].FrameLength);
                if (propertyCurves != null) for (int a = 0; a < propertyCurves.Length; a++) length = math.max(length, Mathf.CeilToInt(propertyCurves[a].GetLengthInSeconds(framesPerSecond) * framesPerSecond));

                return length;

            }

        }


        public CurveInfoPair[] transformAnimationCurves;
        public CurveInfoPair[] propertyAnimationCurves;

        public bool TryGetTransformCurves(string transform, out int index, out CurveInfoPair info, out ITransformCurve mainCurve, out ITransformCurve baseCurve)
        {
            if (transformAnimationCurves != null)
            {
                transform = transform.AsID();
                for(int a = 0; a < transformAnimationCurves.Length; a++)
                {
                    var pair = transformAnimationCurves[a];
                    index = a;
                    info = pair;
                    mainCurve = baseCurve = null;
                    if (pair.infoMain.curveIndex >= 0)
                    {
                        mainCurve = pair.infoMain.isLinear ? transformLinearCurves[pair.infoMain.curveIndex] : transformCurves[pair.infoMain.curveIndex];
                    }
                    if (pair.infoBase.curveIndex >= 0)
                    {
                        baseCurve = pair.infoBase.isLinear ? transformLinearCurves[pair.infoBase.curveIndex] : transformCurves[pair.infoBase.curveIndex];
                    }
                    string transformName = null;
                    if (mainCurve != null)
                    {
                        transformName = mainCurve.TransformName;
                    }
                    else if (baseCurve != null)
                    {
                        transformName = baseCurve.TransformName;
                    }
                    if (transformName.AsID() == transform) return true;
                }
            }
            index = -1;
            info = CurveInfoPair.Empty;
            mainCurve = baseCurve = null;
            return false;
        }

        public bool TryGetPropertyCurves(string property, out int index, out CurveInfoPair info, out IPropertyCurve mainCurve, out IPropertyCurve baseCurve)
        {
            if (propertyAnimationCurves != null)
            {
                for (int a = 0; a < propertyAnimationCurves.Length; a++)
                {
                    var pair = propertyAnimationCurves[a];
                    index = a;
                    info = pair;
                    mainCurve = baseCurve = null;
                    if (pair.infoMain.curveIndex >= 0)
                    {
                        mainCurve = pair.infoMain.isLinear ? propertyLinearCurves[pair.infoMain.curveIndex] : propertyCurves[pair.infoMain.curveIndex];
                    }
                    if (pair.infoBase.curveIndex >= 0)
                    {
                        baseCurve = pair.infoBase.isLinear ? propertyLinearCurves[pair.infoBase.curveIndex] : propertyCurves[pair.infoBase.curveIndex];
                    }
                    string propertyStr = null;
                    if (mainCurve != null)
                    {
                        propertyStr = mainCurve.PropertyString;
                    }
                    else if (baseCurve != null)
                    {
                        propertyStr = baseCurve.PropertyString;
                    }
                    if (propertyStr == property) return true;
                }
            }
            index = -1;
            info = CurveInfoPair.Empty;
            mainCurve = baseCurve = null;
            return false;
        }

        private static readonly List<Event> _eventQueue = new List<Event>(); 
        public static int SortEventsDescending(Event eventA, Event eventB) => (int)Mathf.Sign((eventA.TimelinePosition == eventB.TimelinePosition) ? (eventB.Priority - eventA.Priority) : (eventB.TimelinePosition - eventA.TimelinePosition));
        public static int SortEventsAscending(Event eventA, Event eventB) => (int)Mathf.Sign((eventA.TimelinePosition == eventB.TimelinePosition) ? (eventA.Priority - eventB.Priority) : (eventA.TimelinePosition - eventB.TimelinePosition));
        [Serializable]
        public class Player : IAnimationPlayer
        {

            #region Animation Events
            public event RuntimeEventListenerDelegate onPreEventCalled;
            public event RuntimeEventListenerDelegate onPostEventCalled;

            public void SubscribePreEvent(RuntimeEventListenerDelegate listener)
            {
                if (listener == null) return;
                onPreEventCalled += listener;
            }
            public void UnsubscribePreEvent(RuntimeEventListenerDelegate listener)
            {
                if (listener == null || onPreEventCalled == null) return;
                onPreEventCalled -= listener;
            }
            public void SubscribePostEvent(RuntimeEventListenerDelegate listener)
            {
                if (listener == null) return;
                onPostEventCalled += listener;
            }
            public void UnsubscribePostEvent(RuntimeEventListenerDelegate listener)
            {
                if (listener == null || onPreEventCalled == null) return;
                onPostEventCalled -= listener;
            }

            public IRuntimeEnvironment eventRuntimeEnvironment;
            public IRuntimeEnvironment EventRuntimeEnvironment
            {
                get => eventRuntimeEnvironment;
                set => eventRuntimeEnvironment = value;
            }
            public SwoleLogger eventLogger;
            public SwoleLogger EventLogger
            {
                get => eventLogger;
                set => eventLogger = value;
            }
            public bool HasAnimationEvents => m_animation != null && m_animation.events != null && m_animation.events.Length > 0;
            public void CallAnimationEvents(float startTime, float endTime) => CallAnimationEvents(startTime, endTime, InternalSpeed, null);
            public void CallAnimationEvents(float startTime, float endTime, float currentSpeed) => CallAnimationEvents(startTime, endTime, currentSpeed, null);
            public void CallAnimationEvents(float startTime, float endTime, float currentSpeed, object sender)
            {
                if (!HasAnimationEvents) return;

                if (currentSpeed > 0)
                {
                    if (startTime > endTime) startTime = 0; 
                }
                else if (currentSpeed < 0)
                {
                    if (endTime > startTime) endTime = LengthInSeconds;
                }
                else return;
                //if ((currentSpeed < 0 && endTime - startTime >= 0) || (currentSpeed > 0 && endTime - startTime <= 0)) return;

                var environment = eventRuntimeEnvironment == null ? swole.DefaultEnvironment : eventRuntimeEnvironment;

                float minTime = Mathf.Min(startTime, endTime);
                float maxTime = Mathf.Max(startTime, endTime);

                _eventQueue.Clear();
                for(int a = 0; a < m_animation.events.Length; a++)
                {
                    var _event = m_animation.events[a];
                    if (_event.TimelinePosition < minTime || _event.TimelinePosition > maxTime) continue;
                    _eventQueue.Add(_event);
                }
                if (currentSpeed >= 0) _eventQueue.Sort(SortEventsAscending); else _eventQueue.Sort(SortEventsDescending);

                if (sender == null) sender = this;
                for (int a = 0; a < _eventQueue.Count; a++)
                {
                    try
                    {
                        var _event = _eventQueue[a];
                        try
                        {
                            onPreEventCalled?.Invoke(_event.Name, _event.TimelinePosition, sender);  
                        }
                        catch (Exception ex)
                        {
                            onPreEventCalled = null;
                            swole.LogError(ex);
                        }
                        _event.Execute(environment, _defaultEventCompletionTimeout, eventLogger);
                        try
                        {
                            onPostEventCalled?.Invoke(_event.Name, _event.TimelinePosition, sender);
                        }
                        catch (Exception ex)
                        {
                            onPostEventCalled = null;
                            swole.LogError(ex);
                        }
                    }
                    catch (Exception ex)
                    {
                        swole.LogError(ex);
                    }
                }
                _eventQueue.Clear();
            }
            #endregion

            public int index;
            public int Index
            {
                get => index;
                set => index = value;
            }

            private CustomAnimator m_animator;
            private CustomAnimation m_animation;
            private CustomAnimationAsset m_asset;

            public IAnimator Animator => m_animator;
            public IAnimationAsset Animation => IsUsingAsset ? Asset : m_animation;
            public CustomAnimationAsset Asset => m_asset;
            public CustomAnimation TypedAnimation => IsUsingAsset ? Asset.Animation : m_animation;

            public bool IsUsingAsset => Asset != null; 

            private float m_length;
            public float LengthInSeconds => m_length;

            public AnimationLoopMode loopMode = AnimationLoopMode.PlayOnce;
            public AnimationLoopMode LoopMode
            {
                get => loopMode;
                set => loopMode = value;
            }

            public bool isAdditive;
            public bool IsAdditive
            {
                get => isAdditive;
                set => isAdditive = value;
            }
            public bool isBlend;
            public bool IsBlend
            {
                get => isBlend;
                set => isBlend = value;
            }

            protected float prevTime;
            protected float time;
            public float Time
            {
                get => time;
                set
                {
                    time = prevTime = value;
                }
            }
            public float GetLoopedTime(float time, bool canLoop = true)
            {
                if (canLoop && loopMode != AnimationLoopMode.PlayOnce)
                {

                    if (LengthInSeconds > 0)
                    {

                        switch (loopMode)
                        {

                            default:
                                break;

                            case AnimationLoopMode.Loop:
                                while (time > LengthInSeconds) time -= LengthInSeconds;
                                while (time < 0) time += LengthInSeconds;
                                break;

                            case AnimationLoopMode.PingPong:
                                if (time >= LengthInSeconds && internalSpeedMultiplier > -1)
                                    internalSpeedMultiplier = -1;
                                else if (time <= 0 && internalSpeedMultiplier < 1)
                                    internalSpeedMultiplier = 1;
                                break;

                        }

                    }

                }
                else
                {
                    if (time < 0) time = 0;
                    if (time > LengthInSeconds - 0.0001f) time = LengthInSeconds - 0.0001f;// Mathf.Epsilon; // Avoid time wrap with epsilon?
                }

                return time;
            }

            public float speed = 1;
            public float Speed
            {
                get => speed;
                set => speed = value; 
                
            }
            protected float internalSpeedMultiplier = 1; 
            public float InternalSpeed => speed * internalSpeedMultiplier; 

            public float mix = 1;
            public float Mix
            {
                get => mix;
                set => mix = value;
            }
            protected float dynamicMix;
            /// <summary>
            /// Last mix used during the animation step
            /// </summary>
            public float DynamicMix => dynamicMix;

            public bool paused;
            public bool Paused
            {
                get => paused;
                set => paused = value;
            }


            public void ResetLoop()
            {
                internalSpeedMultiplier = 1;
            }

            private PropertyState[] propertyStates;
            private TransformStateReference[] transformStates;

            private TransformAccessArray m_affectedTransforms;
            private NativeArray<int2> m_transformCurveBindings;

            private TransformHierarchy m_hierarchy;
            public TransformHierarchy Hierarchy => m_hierarchy;

            public bool HasDerivativeHierarchyOf(IAnimationPlayer other) => Hierarchy == other.Hierarchy || (Hierarchy != null && Hierarchy.IsDerivative(other.Hierarchy));

            private void SwapAnimation(CustomAnimation animation)
            {
                Initialize(m_animator, animation);
            }
            public Player(CustomAnimator animator, CustomAnimationAsset asset, bool isAdditive = false, bool isBlend = false) : this(animator, (asset == null ? null : asset.Animation), isAdditive, isBlend)
            {
                m_asset = asset;
                
                if (asset != null)
                {
                    asset.OnSetAnimation += SwapAnimation;
                }
            }
            public Player(CustomAnimator animator, CustomAnimation animation, bool isAdditive = false, bool isBlend = false)
            {
                this.isAdditive = isAdditive;
                this.isBlend = isBlend;

                paused = true;

                Initialize(animator, animation);
            }

            protected void Initialize(CustomAnimator animator, CustomAnimation animation)
            {
                DisposeJobData();

                m_animator = animator;
                m_animation = animation;

                if (animation == null)
                {
                    m_length = 0;
                    return;
                }
                
                m_length = m_animation.LengthInSeconds;

                PropertyState GetPropertyState(CurveInfoPair curveInfo)
                { 

                    IPropertyCurve curve = curveInfo.infoMain.isLinear ? animation.propertyLinearCurves == null ? null : animation.propertyLinearCurves[curveInfo.infoMain.curveIndex] : animation.propertyCurves == null ? null : animation.propertyCurves[curveInfo.infoMain.curveIndex];
                    if (curve == null) return null;
                    curve.RefreshCachedLength(m_animation.framesPerSecond);  // <- Important to do when working in the animation editor, as the animation data can change.

                    Component component = animator.FindAndBindComponent(curve.PropertyString);
                    if (component == null) return null;
                    int finalPeriod = curve.PropertyString.LastIndexOf('.');
                    string memberName = finalPeriod >= 0 ? curve.PropertyString.Substring(finalPeriod + 1, curve.PropertyString.Length - (finalPeriod + 1)) : curve.PropertyString;
                    return animator.AddOrGetState(component, memberName);
                    
                }

                if (animation.propertyAnimationCurves != null)
                {

                    propertyStates = new PropertyState[animation.propertyAnimationCurves.Length];

                    for (int a = 0; a < propertyStates.Length; a++) 
                    {
                        var curveInfo = animation.propertyAnimationCurves[a];
                        propertyStates[a] = GetPropertyState(curveInfo);
                    }

                }

                if (animation.transformAnimationCurves != null)
                {

                    transformStates = new TransformStateReference[animation.transformAnimationCurves.Length];

                    List<Transform> transforms = new List<Transform>();
                    List<int2> bindings = new List<int2>();

                    for (int a = 0; a < animation.transformAnimationCurves.Length; a++)
                    {

                        var header = animation.transformAnimationCurves[a];

                        ITransformCurve curve = header.infoMain.isLinear ? animation.transformLinearCurves[header.infoMain.curveIndex] : animation.transformCurves[header.infoMain.curveIndex];

                        curve.RefreshCachedLength(m_animation.framesPerSecond); // <- Important to do when working in the animation editor, as the animation data can change.

                        var transform = animator.FindTransformInHierarchy(curve.TransformName/*, curve.IsBone*/);

                        if (transform == null) continue;

                        transformStates[a] = animator.AddOrGetState(transform);

                        transforms.Add(transform);
                        bindings.Add(new int2(a, transformStates[a].index));

                    }

                    m_affectedTransforms = new TransformAccessArray(transforms.ToArray());
                    m_transformCurveBindings = new NativeArray<int2>(bindings.ToArray(), Allocator.Persistent);

                    m_hierarchy = animator.GetTransformHierarchy(m_affectedTransforms);

                }

            }

            private JobHandle lastJobHandle;
            public JobHandle LastJobHandle => lastJobHandle;
            public JobHandle Progress(float deltaTime, float mixMultiplier = 1, JobHandle jobDeps = default, bool useMultithreading = true, bool isFinal = false, bool canLoop = true)
            {
                if (m_animation == null) return jobDeps; 

                // Looping
                if (!paused)
                {
                    float speed = InternalSpeed;
                    time += deltaTime * speed;

                    time = GetLoopedTime(time, canLoop);

                    #region Animation Events
                    try
                    {
                        CallAnimationEvents(prevTime, time, speed); 
                    }
                    catch(Exception ex)
                    {
                        swole.LogError(ex);
                    }
                    prevTime = time;
                    #endregion

                }

                float normalizedTime = LengthInSeconds <= 0 ? 0 : (time / LengthInSeconds);
                // Apply time curve
                float loopTime = Mathf.Floor(normalizedTime);
                normalizedTime = m_animation.ScaleNormalizedTime(normalizedTime - loopTime);
                //

                //
                dynamicMix = mix * mixMultiplier;
                if (!isFinal && isBlend && math.abs(dynamicMix) < 0.00001f) return jobDeps; // Don't bother updating when the changes to be made are insignificant

                var animator = m_animator;
                var anim = TypedAnimation;
                if (anim == null) return jobDeps;

                if (propertyStates != null)
                {

                    if (isBlend && isAdditive)
                    {

                        for (int a = 0; a < propertyStates.Length; a++)
                        {

                            var state = propertyStates[a];
                            if (state == null) continue;

                            var curveInfo = anim.propertyAnimationCurves[a];

                            IPropertyCurve mainCurve = null;
                            IPropertyCurve baseCurve = null;

                            if (curveInfo.infoMain.curveIndex >= 0) mainCurve = curveInfo.infoMain.isLinear ? anim.propertyLinearCurves[curveInfo.infoMain.curveIndex] : anim.propertyCurves[curveInfo.infoMain.curveIndex];
                            if (curveInfo.infoBase.curveIndex >= 0) baseCurve = curveInfo.infoBase.isLinear ? anim.propertyLinearCurves[curveInfo.infoBase.curveIndex] : anim.propertyCurves[curveInfo.infoBase.curveIndex];

                            if (mainCurve == null && baseCurve == null) continue;

                            bool testA = baseCurve == null;
                            bool testB = mainCurve == null;

                            if (mainCurve == null) mainCurve = baseCurve;
                            if (baseCurve == null) baseCurve = mainCurve;

                            //float cachedLength = mainCurve.CachedLengthInSeconds;
                            float cachedLengthMain = mainCurve.CachedLengthInSeconds;
                            float cachedLengthBase = baseCurve.CachedLengthInSeconds;
                            //float t = cachedLength <= 0 ? 0 : normalizedTime * (LengthInSeconds / cachedLength);
                            float tM = cachedLengthMain <= 0 ? 0 : normalizedTime * (LengthInSeconds / cachedLengthMain);
                            float tB = cachedLengthBase <= 0 ? 0 : normalizedTime * (LengthInSeconds / cachedLengthBase);

                            //state.ApplyAdditiveMix(mainCurve.Evaluate(t) - baseCurve.Evaluate(t), mix);
                            state.ApplyAdditiveMix(mainCurve.Evaluate(tM) - baseCurve.Evaluate(tB), dynamicMix);

                        }

                    }
                    else if (isAdditive)
                    {

                        for (int a = 0; a < propertyStates.Length; a++)
                        {

                            var state = propertyStates[a];
                            if (state == null) continue;

                            var curveInfo = anim.propertyAnimationCurves[a];

                            IPropertyCurve mainCurve = null;
                            IPropertyCurve baseCurve = null;

                            if (curveInfo.infoMain.curveIndex >= 0) mainCurve = curveInfo.infoMain.isLinear ? anim.propertyLinearCurves[curveInfo.infoMain.curveIndex] : anim.propertyCurves[curveInfo.infoMain.curveIndex];
                            if (curveInfo.infoBase.curveIndex >= 0) baseCurve = curveInfo.infoBase.isLinear ? anim.propertyLinearCurves[curveInfo.infoBase.curveIndex] : anim.propertyCurves[curveInfo.infoBase.curveIndex];

                            if (mainCurve == null && baseCurve == null) continue;

                            if (mainCurve == null) mainCurve = baseCurve;
                            if (baseCurve == null) baseCurve = mainCurve;

                            //float cachedLength = mainCurve.CachedLengthInSeconds;
                            float cachedLengthMain = mainCurve.CachedLengthInSeconds;
                            float cachedLengthBase = baseCurve.CachedLengthInSeconds;
                            //float t = cachedLength <= 0 ? 0 : normalizedTime * (LengthInSeconds / cachedLength);
                            float tM = cachedLengthMain <= 0 ? 0 : normalizedTime * (LengthInSeconds / cachedLengthMain);
                            float tB = cachedLengthBase <= 0 ? 0 : normalizedTime * (LengthInSeconds / cachedLengthBase);

                            //state.ApplyAdditive(mainCurve.Evaluate(t) - baseCurve.Evaluate(t), mix);
                            state.ApplyAdditive(mainCurve.Evaluate(tM) - baseCurve.Evaluate(tB));

                        }

                    }
                    else if (isBlend)
                    {

                        for (int a = 0; a < propertyStates.Length; a++)
                        {

                            var state = propertyStates[a];
                            if (state == null) continue;

                            var curveInfo = anim.propertyAnimationCurves[a];

                            IPropertyCurve mainCurve = null;

                            if (curveInfo.infoMain.curveIndex >= 0) mainCurve = curveInfo.infoMain.isLinear ? anim.propertyLinearCurves[curveInfo.infoMain.curveIndex] : anim.propertyCurves[curveInfo.infoMain.curveIndex];

                            if (mainCurve == null) continue;

                            float cachedLength = mainCurve.CachedLengthInSeconds;
                            float t = cachedLength <= 0 ? 0 : normalizedTime * (LengthInSeconds / cachedLength);

                            state.ApplyMix(mainCurve.Evaluate(t), dynamicMix);

                        }

                    }
                    else
                    {

                        for (int a = 0; a < propertyStates.Length; a++)
                        {

                            var state = propertyStates[a];
                            if (state == null) continue;

                            var curveInfo = anim.propertyAnimationCurves[a];

                            IPropertyCurve mainCurve = null;

                            if (curveInfo.infoMain.curveIndex >= 0) mainCurve = curveInfo.infoMain.isLinear ? anim.propertyLinearCurves[curveInfo.infoMain.curveIndex] : anim.propertyCurves[curveInfo.infoMain.curveIndex];

                            if (mainCurve == null) continue;

                            float cachedLength = mainCurve.CachedLengthInSeconds;
                            float t = cachedLength <= 0 ? 0 : normalizedTime * (LengthInSeconds / cachedLength);

                            state.Apply(mainCurve.Evaluate(t));

                        }

                    }

                }

                if (m_affectedTransforms.isCreated)
                {

                    if (useMultithreading)
                    {

                        normalizedTime = math.saturate(normalizedTime); 

                        if (isBlend && isAdditive)
                        {

                            jobDeps = isFinal ?

                                new ApplyAdditiveMixTransformCurvesJobFinal()
                                {
                                    linearTransformCurvesIndexOffset = anim.LinearTransformCurvesSampledIndexOffset,
                                    normalizedTime = normalizedTime,
                                    mix = dynamicMix,
                                    transformStates = animator.TransformStates,
                                    curveSamples = anim.TransformCurvesSampled,
                                    curveHeaders = anim.TransformCurvesSampledHeaders,
                                    frameHeaders = anim.TransformCurvesSampledFrameHeaders,
                                    transformCurveBindings = m_transformCurveBindings,
                                    transformAnimationCurves = anim.TransformAnimationCurvesForJobs

                                }.Schedule(m_affectedTransforms, jobDeps) :

                                new ApplyAdditiveMixTransformCurvesJob()
                                {
                                    linearTransformCurvesIndexOffset = anim.LinearTransformCurvesSampledIndexOffset,
                                    normalizedTime = normalizedTime,
                                    mix = dynamicMix,
                                    transformStates = animator.TransformStates,
                                    curveSamples = anim.TransformCurvesSampled,
                                    curveHeaders = anim.TransformCurvesSampledHeaders,
                                    frameHeaders = anim.TransformCurvesSampledFrameHeaders,
                                    transformCurveBindings = m_transformCurveBindings,
                                    transformAnimationCurves = anim.TransformAnimationCurvesForJobs

                                }.Schedule(m_transformCurveBindings.Length, 1, jobDeps);

                        }
                        else if (isAdditive)
                        {

                            jobDeps = isFinal ?

                                new ApplyAdditiveTransformCurvesJobFinal()
                                {
                                    linearTransformCurvesIndexOffset = anim.LinearTransformCurvesSampledIndexOffset,
                                    normalizedTime = normalizedTime,
                                    transformStates = animator.TransformStates,
                                    curveSamples = anim.TransformCurvesSampled,
                                    curveHeaders = anim.TransformCurvesSampledHeaders,
                                    frameHeaders = anim.TransformCurvesSampledFrameHeaders,
                                    transformCurveBindings = m_transformCurveBindings,
                                    transformAnimationCurves = anim.TransformAnimationCurvesForJobs

                                }.Schedule(m_affectedTransforms, jobDeps) :

                                new ApplyAdditiveTransformCurvesJob()
                                {
                                    linearTransformCurvesIndexOffset = anim.LinearTransformCurvesSampledIndexOffset,
                                    normalizedTime = normalizedTime,
                                    transformStates = animator.TransformStates,
                                    curveSamples = anim.TransformCurvesSampled,
                                    curveHeaders = anim.TransformCurvesSampledHeaders,
                                    frameHeaders = anim.TransformCurvesSampledFrameHeaders,
                                    transformCurveBindings = m_transformCurveBindings,
                                    transformAnimationCurves = anim.TransformAnimationCurvesForJobs

                                }.Schedule(m_transformCurveBindings.Length, 1, jobDeps);

                        }
                        else if (isBlend)
                        {

                            jobDeps = isFinal ?

                            new ApplyMixTransformCurvesJobFinal()
                            {
                                linearTransformCurvesIndexOffset = anim.LinearTransformCurvesSampledIndexOffset,
                                normalizedTime = normalizedTime,
                                mix = dynamicMix,
                                transformStates = animator.TransformStates,
                                curveSamples = anim.TransformCurvesSampled,
                                curveHeaders = anim.TransformCurvesSampledHeaders,
                                frameHeaders = anim.TransformCurvesSampledFrameHeaders,
                                transformCurveBindings = m_transformCurveBindings,
                                transformAnimationCurves = anim.TransformAnimationCurvesForJobs

                            }.Schedule(m_affectedTransforms, jobDeps) :

                            new ApplyMixTransformCurvesJob()
                            {
                                linearTransformCurvesIndexOffset = anim.LinearTransformCurvesSampledIndexOffset,
                                normalizedTime = normalizedTime,
                                mix = dynamicMix,
                                transformStates = animator.TransformStates,
                                curveSamples = anim.TransformCurvesSampled,
                                curveHeaders = anim.TransformCurvesSampledHeaders,
                                frameHeaders = anim.TransformCurvesSampledFrameHeaders,
                                transformCurveBindings = m_transformCurveBindings,
                                transformAnimationCurves = anim.TransformAnimationCurvesForJobs

                            }.Schedule(m_transformCurveBindings.Length, 1, jobDeps);

                        }
                        else
                        {

                            jobDeps = isFinal ?

                                new ApplyTransformCurvesJobFinal()
                                {
                                    linearTransformCurvesIndexOffset = anim.LinearTransformCurvesSampledIndexOffset,
                                    normalizedTime = normalizedTime,
                                    transformStates = animator.TransformStates,
                                    curveSamples = anim.TransformCurvesSampled,
                                    curveHeaders = anim.TransformCurvesSampledHeaders,
                                    frameHeaders = anim.TransformCurvesSampledFrameHeaders,
                                    transformCurveBindings = m_transformCurveBindings,
                                    transformAnimationCurves = anim.TransformAnimationCurvesForJobs

                                }.Schedule(m_affectedTransforms, jobDeps) :

                                new ApplyTransformCurvesJob()
                                {
                                    linearTransformCurvesIndexOffset = anim.LinearTransformCurvesSampledIndexOffset,
                                    normalizedTime = normalizedTime,
                                    transformStates = animator.TransformStates,
                                    curveSamples = anim.TransformCurvesSampled,
                                    curveHeaders = anim.TransformCurvesSampledHeaders,
                                    frameHeaders = anim.TransformCurvesSampledFrameHeaders,
                                    transformCurveBindings = m_transformCurveBindings,
                                    transformAnimationCurves = anim.TransformAnimationCurvesForJobs

                                }.Schedule(m_transformCurveBindings.Length, 1, jobDeps);

                        }

                    }
                    else
                    {

                        if (isBlend && isAdditive)
                        {
                            for (int a = 0; a < transformStates.Length; a++)
                            {

                                var state = transformStates[a];
                                if (state == null) continue;

                                var curveInfo = anim.transformAnimationCurves[a];

                                ITransformCurve mainCurve = null;
                                ITransformCurve baseCurve = null;

                                if (curveInfo.infoMain.curveIndex >= 0) mainCurve = curveInfo.infoMain.isLinear ? anim.transformLinearCurves[curveInfo.infoMain.curveIndex] : anim.transformCurves[curveInfo.infoMain.curveIndex];
                                if (curveInfo.infoBase.curveIndex >= 0) baseCurve = curveInfo.infoBase.isLinear ? anim.transformLinearCurves[curveInfo.infoBase.curveIndex] : anim.transformCurves[curveInfo.infoBase.curveIndex];

                                if (mainCurve == null && baseCurve == null) continue;

                                if (mainCurve == null) mainCurve = baseCurve;
                                if (baseCurve == null) baseCurve = mainCurve;

                                //float cachedLength = mainCurve.CachedLengthInSeconds;
                                float cachedLengthMain = mainCurve.CachedLengthInSeconds;
                                float cachedLengthBase = baseCurve.CachedLengthInSeconds;
                                //float t = cachedLength <= 0 ? 0 : normalizedTime * (LengthInSeconds / cachedLength);
                                float tM = cachedLengthMain <= 0 ? 0 : normalizedTime * (LengthInSeconds / cachedLengthMain);
                                float tB = cachedLengthBase <= 0 ? 0 : normalizedTime * (LengthInSeconds / cachedLengthBase);

                                var stateData = animator.GetTransformState(state.index);
                                //stateData = stateData.Swizzle(stateData.ApplyAdditiveMix(mainCurve.Evaluate(t) - baseCurve.Evaluate(t), mix), mainCurve.ValidityPosition, mainCurve.ValidityRotation, mainCurve.ValidityScale);
                                stateData = stateData.Swizzle(stateData.ApplyAdditiveMix(mainCurve.Evaluate(tM) - baseCurve.Evaluate(tB), dynamicMix), mainCurve.ValidityPosition, mainCurve.ValidityRotation, mainCurve.ValidityScale);
                                animator.SetTransformState(state.index, stateData);

                            }

                        }
                        else if (isAdditive)
                        {
                            for (int a = 0; a < transformStates.Length; a++)
                            {

                                var state = transformStates[a];
                                if (state == null) continue;

                                var curveInfo = anim.transformAnimationCurves[a];

                                ITransformCurve mainCurve = null;
                                ITransformCurve baseCurve = null;

                                if (curveInfo.infoMain.curveIndex >= 0) mainCurve = curveInfo.infoMain.isLinear ? anim.transformLinearCurves[curveInfo.infoMain.curveIndex] : anim.transformCurves[curveInfo.infoMain.curveIndex];
                                if (curveInfo.infoBase.curveIndex >= 0) baseCurve = curveInfo.infoBase.isLinear ? anim.transformLinearCurves[curveInfo.infoBase.curveIndex] : anim.transformCurves[curveInfo.infoBase.curveIndex];

                                if (mainCurve == null && baseCurve == null) continue;

                                if (mainCurve == null) mainCurve = baseCurve;
                                if (baseCurve == null) baseCurve = mainCurve;

                                //float cachedLength = mainCurve.CachedLengthInSeconds;
                                float cachedLengthMain = mainCurve.CachedLengthInSeconds;
                                float cachedLengthBase = baseCurve.CachedLengthInSeconds;
                                //float t = cachedLength <= 0 ? 0 : normalizedTime * (LengthInSeconds / cachedLength);
                                float tM = cachedLengthMain <= 0 ? 0 : normalizedTime * (LengthInSeconds / cachedLengthMain);
                                float tB = cachedLengthBase <= 0 ? 0 : normalizedTime * (LengthInSeconds / cachedLengthBase);

                                var stateData = animator.GetTransformState(state.index);
                                //stateData = stateData.Swizzle(stateData.ApplyAdditive(mainCurve.Evaluate(t) - baseCurve.Evaluate(t)), mainCurve.ValidityPosition, mainCurve.ValidityRotation, mainCurve.ValidityScale);
                                stateData = stateData.Swizzle(stateData.ApplyAdditive(mainCurve.Evaluate(tM) - baseCurve.Evaluate(tB)), mainCurve.ValidityPosition, mainCurve.ValidityRotation, mainCurve.ValidityScale);
                                animator.SetTransformState(state.index, stateData);

                            }

                        }
                        else if (isBlend)
                        {
                            for (int a = 0; a < transformStates.Length; a++)
                            {

                                var state = transformStates[a];
                                if (state == null) continue;

                                var curveInfo = anim.transformAnimationCurves[a];

                                ITransformCurve mainCurve = null;

                                if (curveInfo.infoMain.curveIndex >= 0) mainCurve = curveInfo.infoMain.isLinear ? anim.transformLinearCurves[curveInfo.infoMain.curveIndex] : anim.transformCurves[curveInfo.infoMain.curveIndex];

                                if (mainCurve == null) continue;

                                float cachedLength = mainCurve.CachedLengthInSeconds;
                                float t = cachedLength <= 0 ? 0 : normalizedTime * (LengthInSeconds / cachedLength);

                                var stateData = animator.GetTransformState(state.index);
                                stateData = stateData.Swizzle(stateData.ApplyMix(mainCurve.Evaluate(t), dynamicMix), mainCurve.ValidityPosition, mainCurve.ValidityRotation, mainCurve.ValidityScale);
                                animator.SetTransformState(state.index, stateData);

                            }

                        }
                        else
                        {
                            for (int a = 0; a < transformStates.Length; a++)
                            {


                                var state = transformStates[a];
                                if (state == null) continue;

                                var curveInfo = anim.transformAnimationCurves[a];

                                ITransformCurve mainCurve = null;

                                if (curveInfo.infoMain.curveIndex >= 0) mainCurve = curveInfo.infoMain.isLinear ? anim.transformLinearCurves[curveInfo.infoMain.curveIndex] : anim.transformCurves[curveInfo.infoMain.curveIndex];

                                if (mainCurve == null) continue;

                                float cachedLength = mainCurve.CachedLengthInSeconds;
                                float t = cachedLength <= 0 ? 0 : normalizedTime * (LengthInSeconds / cachedLength);

                                var stateData = animator.GetTransformState(state.index);
                                stateData = stateData.Swizzle(stateData.Apply(mainCurve.Evaluate(t)), mainCurve.ValidityPosition, mainCurve.ValidityRotation, mainCurve.ValidityScale);
                                animator.SetTransformState(state.index, stateData);

                            }

                        }

                    }

                }

                return lastJobHandle = jobDeps;

            }

            protected void DisposeJobData()
            {
                lastJobHandle.Complete();

                if (m_affectedTransforms.isCreated) m_affectedTransforms.Dispose();

                m_affectedTransforms = default;

                if (m_transformCurveBindings.IsCreated) m_transformCurveBindings.Dispose();

                m_transformCurveBindings = default;
            }
            public void Dispose()
            {
                DisposeJobData();

                if (m_asset != null) 
                {
                    try
                    {
                        m_asset.OnSetAnimation -= SwapAnimation;
                    }
                    catch {}
                    m_asset = null;
                }

                m_animator = null;
                m_animation = null;

                onPreEventCalled = null;
                onPostEventCalled = null;
            }

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ITransformCurve.Data EvaluateSampledCurveData(NativeArray<ITransformCurve.Data> curveSamples, int sampleStartIndex, int sampleCount, float t)
        {
            float it = (sampleCount - 1) * t;

            int i0 = (int)it;
            int i1 = i0 + 1;

            i1 = math.select(i1, i0, i1 >= sampleCount);

            return curveSamples[sampleStartIndex + i0].Slerp(curveSamples[sampleStartIndex + i1], it - i0); 
        }

        #region Deprecated
        [MethodImpl(MethodImplOptions.AggressiveInlining), Obsolete]
        public static ITransformCurve.Data EvaluateSampledCurveData(bool isLinear, int curveIndex, float normalizedTime, NativeArray<ITransformCurve.Data> curveSamples, NativeArray<CurveHeader> curveHeaders, NativeArray<ITransformCurve.Data> curveSamplesLinear, NativeArray<CurveHeader> curveHeadersLinear, out bool3 validityPosition, out bool4 validityRotation, out bool3 validityScale)
        {

            ITransformCurve.Data data;

            CurveHeader header;
            if (isLinear)
            {

                header = curveHeadersLinear[curveIndex];
                data = EvaluateSampledCurveData(curveSamplesLinear, header.index, header.sampleCount, math.saturate(normalizedTime * header.timeRatio)); // TODO: Use wrap modes instead of clamping

            }
            else
            {

                header = curveHeaders[curveIndex];
                data = EvaluateSampledCurveData(curveSamples, header.index, header.sampleCount, math.saturate(normalizedTime * header.timeRatio)); // TODO: Use wrap modes instead of clamping

            }

            validityPosition = header.validityPosition;
            validityRotation = header.validityRotation;
            validityScale = header.validityScale;

            return data;

        }
        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ITransformCurve.Data EvaluateSampledCurveData(bool isLinear, int linearTransformCurvesIndexOffset, int curveIndex, float normalizedTime, NativeArray<ITransformCurve.Data> curveSamples, NativeArray<CurveHeader> curveHeaders, NativeArray<FrameHeader> frameHeaders, out bool3 validityPosition, out bool4 validityRotation, out bool3 validityScale)
        {

            ITransformCurve.Data data;

            curveIndex = math.select(curveIndex, curveIndex + linearTransformCurvesIndexOffset, isLinear);

            var curveHeader = curveHeaders[curveIndex];

            normalizedTime = (normalizedTime - curveHeader.normalizedTimeStart) / math.select(curveHeader.normalizedTimeLength, 1, curveHeader.normalizedTimeLength <= 0);  
            normalizedTime = math.select(AnimationUtils.WrapNormalizedTimeBackward(normalizedTime, curveHeader.preWrapMode), AnimationUtils.WrapNormalizedTimeForward(normalizedTime, curveHeader.postWrapMode), normalizedTime >= 0);

            FrameHeader frameHeaderA = frameHeaders[curveHeader.frameHeaderIndex];  
            FrameHeader frameHeaderB = frameHeaderA; 
            //int i = 0; // Debug
            for (int a = 1; a < curveHeader.frameCount; a++)
            {
                frameHeaderA = frameHeaderB;
                frameHeaderB = frameHeaders[curveHeader.frameHeaderIndex + a];
                //i = a; // Debug
                if (frameHeaderB.normalizedLocalTime >= normalizedTime) break;  
            }
            //if (curveHeader.timeRatio != 0) Debug.Log(i + "/" + curveHeader.frameCount + " : " + curveHeader.normalizedTimeStart + "/" + curveHeader.normalizedTimeLength + " : " + normalizedTime); // Debug

            float frameLength = (frameHeaderB.normalizedLocalTime - frameHeaderA.normalizedLocalTime);  
            normalizedTime = (normalizedTime - frameHeaderA.normalizedLocalTime) / math.select(frameLength, 1, frameLength <= 0); 
            data = EvaluateSampledCurveData(curveSamples, frameHeaderA.sampleIndex, frameHeaderA.sampleCount, normalizedTime);

            validityPosition = curveHeader.validityPosition;
            validityRotation = curveHeader.validityRotation;
            validityScale = curveHeader.validityScale;

            return data;

        }

        [BurstCompile]
        public struct ApplyTransformCurvesJob : IJobParallelFor
        {

            public int linearTransformCurvesIndexOffset;

            public float normalizedTime;

            [NativeDisableParallelForRestriction]
            public NativeList<TransformAnimationState> transformStates;

            [ReadOnly]
            public NativeArray<ITransformCurve.Data> curveSamples;
            [ReadOnly]
            public NativeArray<CurveHeader> curveHeaders;
            [ReadOnly]
            public NativeArray<FrameHeader> frameHeaders;

            [ReadOnly]
            public NativeArray<int2> transformCurveBindings;
            [ReadOnly]
            public NativeArray<CurveInfoPair> transformAnimationCurves;

            public void Execute(int index)
            {

                var binding = transformCurveBindings[index];
                var curveInfo = transformAnimationCurves[binding.x];

                var state = transformStates[binding.y];

                state = state.Swizzle(state.Apply(EvaluateSampledCurveData(curveInfo.infoMain.isLinear, linearTransformCurvesIndexOffset, curveInfo.infoMain.curveIndex, normalizedTime, curveSamples, curveHeaders, frameHeaders, out bool3 validityPosition, out bool4 validityRotation, out bool3 validityScale)), validityPosition, validityRotation, validityScale);

                transformStates[binding.y] = state;

            }

        }

        [BurstCompile]
        public struct ApplyMixTransformCurvesJob : IJobParallelFor
        {

            public int linearTransformCurvesIndexOffset;

            public float normalizedTime;

            public float mix;

            [NativeDisableParallelForRestriction]
            public NativeList<TransformAnimationState> transformStates;

            [ReadOnly]
            public NativeArray<ITransformCurve.Data> curveSamples;
            [ReadOnly]
            public NativeArray<CurveHeader> curveHeaders;
            [ReadOnly]
            public NativeArray<FrameHeader> frameHeaders;

            [ReadOnly]
            public NativeArray<int2> transformCurveBindings;
            [ReadOnly]
            public NativeArray<CurveInfoPair> transformAnimationCurves;

            public void Execute(int index)
            {

                var binding = transformCurveBindings[index];
                var curveInfo = transformAnimationCurves[binding.x];

                var state = transformStates[binding.y];

                state = state.Swizzle(state.ApplyMix(EvaluateSampledCurveData(curveInfo.infoMain.isLinear, linearTransformCurvesIndexOffset, curveInfo.infoMain.curveIndex, normalizedTime, curveSamples, curveHeaders, frameHeaders, out bool3 validityPosition, out bool4 validityRotation, out bool3 validityScale), mix), validityPosition, validityRotation, validityScale);

                transformStates[binding.y] = state;

            }

        }

        [BurstCompile]
        public struct ApplyAdditiveTransformCurvesJob : IJobParallelFor
        {

            public int linearTransformCurvesIndexOffset;

            public float normalizedTime;

            [NativeDisableParallelForRestriction]
            public NativeList<TransformAnimationState> transformStates;

            [ReadOnly]
            public NativeArray<ITransformCurve.Data> curveSamples;
            [ReadOnly]
            public NativeArray<CurveHeader> curveHeaders;
            [ReadOnly]
            public NativeArray<FrameHeader> frameHeaders;

            [ReadOnly]
            public NativeArray<int2> transformCurveBindings;
            [ReadOnly]
            public NativeArray<CurveInfoPair> transformAnimationCurves;

            public void Execute(int index)
            {

                var binding = transformCurveBindings[index];
                var curveInfo = transformAnimationCurves[binding.x];

                var state = transformStates[binding.y];

                ITransformCurve.Data mainData = EvaluateSampledCurveData(curveInfo.infoMain.isLinear, linearTransformCurvesIndexOffset, curveInfo.infoMain.curveIndex, normalizedTime, curveSamples, curveHeaders, frameHeaders, out bool3 validityPosition, out bool4 validityRotation, out bool3 validityScale);
                ITransformCurve.Data baseData = EvaluateSampledCurveData(curveInfo.infoBase.isLinear, linearTransformCurvesIndexOffset, curveInfo.infoBase.curveIndex, normalizedTime, curveSamples, curveHeaders, frameHeaders, out _, out _, out _);

                state = state.Swizzle(state.ApplyAdditive(mainData - baseData), validityPosition, validityRotation, validityScale);
                //state = state.Swizzle(state.ApplyAdditive(ITransformCurve.Data.Subtract(mainData, baseData, state.modifiedLocalRotation)), validityPosition, validityRotation, validityScale); 

                transformStates[binding.y] = state;

            }

        }

        [BurstCompile]
        public struct ApplyAdditiveMixTransformCurvesJob : IJobParallelFor
        {

            public int linearTransformCurvesIndexOffset;

            public float normalizedTime;
            public float mix;

            [NativeDisableParallelForRestriction]
            public NativeList<TransformAnimationState> transformStates;

            [ReadOnly]
            public NativeArray<ITransformCurve.Data> curveSamples;
            [ReadOnly]
            public NativeArray<CurveHeader> curveHeaders;
            [ReadOnly]
            public NativeArray<FrameHeader> frameHeaders;

            [ReadOnly]
            public NativeArray<int2> transformCurveBindings;
            [ReadOnly]
            public NativeArray<CurveInfoPair> transformAnimationCurves;

            public void Execute(int index)
            {

                var binding = transformCurveBindings[index];
                var curveInfo = transformAnimationCurves[binding.x];

                var state = transformStates[binding.y];

                ITransformCurve.Data mainData = EvaluateSampledCurveData(curveInfo.infoMain.isLinear, linearTransformCurvesIndexOffset, curveInfo.infoMain.curveIndex, normalizedTime, curveSamples, curveHeaders, frameHeaders, out bool3 validityPosition, out bool4 validityRotation, out bool3 validityScale);
                ITransformCurve.Data baseData = EvaluateSampledCurveData(curveInfo.infoBase.isLinear, linearTransformCurvesIndexOffset, curveInfo.infoBase.curveIndex, normalizedTime, curveSamples, curveHeaders, frameHeaders, out _, out _, out _);

                state = state.Swizzle(state.ApplyAdditiveMix(mainData - baseData, mix), validityPosition, validityRotation, validityScale);
                //state = state.Swizzle(state.ApplyAdditiveMix(ITransformCurve.Data.Subtract(mainData, baseData, state.modifiedLocalRotation), mix), validityPosition, validityRotation, validityScale);

                transformStates[binding.y] = state;

            }

        }

        [BurstCompile]
        public struct ApplyTransformCurvesJobFinal : IJobParallelForTransform
        {

            public int linearTransformCurvesIndexOffset;

            public float normalizedTime;

            [NativeDisableParallelForRestriction]
            public NativeList<TransformAnimationState> transformStates;

            [ReadOnly]
            public NativeArray<ITransformCurve.Data> curveSamples;
            [ReadOnly]
            public NativeArray<CurveHeader> curveHeaders;
            [ReadOnly]
            public NativeArray<FrameHeader> frameHeaders;

            [ReadOnly]
            public NativeArray<int2> transformCurveBindings;
            [ReadOnly]
            public NativeArray<CurveInfoPair> transformAnimationCurves;

            public void Execute(int index, TransformAccess transform)
            {

                var binding = transformCurveBindings[index];
                var curveInfo = transformAnimationCurves[binding.x];

                var state = transformStates[binding.y];

                state = state.Swizzle(state.Apply(EvaluateSampledCurveData(curveInfo.infoMain.isLinear, linearTransformCurvesIndexOffset, curveInfo.infoMain.curveIndex, normalizedTime, curveSamples, curveHeaders, frameHeaders, out bool3 validityPosition, out bool4 validityRotation, out bool3 validityScale)), validityPosition, validityRotation, validityScale);

                transformStates[binding.y] = state;

                state.Modify(transform);

            }

        }

        [BurstCompile]
        public struct ApplyMixTransformCurvesJobFinal : IJobParallelForTransform
        {

            public int linearTransformCurvesIndexOffset;

            public float normalizedTime;

            public float mix;

            [NativeDisableParallelForRestriction]
            public NativeList<TransformAnimationState> transformStates;

            [ReadOnly]
            public NativeArray<ITransformCurve.Data> curveSamples;
            [ReadOnly]
            public NativeArray<CurveHeader> curveHeaders;
            [ReadOnly]
            public NativeArray<FrameHeader> frameHeaders;

            [ReadOnly]
            public NativeArray<int2> transformCurveBindings;
            [ReadOnly]
            public NativeArray<CurveInfoPair> transformAnimationCurves;

            public void Execute(int index, TransformAccess transform)
            {

                var binding = transformCurveBindings[index];
                var curveInfo = transformAnimationCurves[binding.x];

                var state = transformStates[binding.y];

                state = state.Swizzle(state.ApplyMix(EvaluateSampledCurveData(curveInfo.infoMain.isLinear, linearTransformCurvesIndexOffset, curveInfo.infoMain.curveIndex, normalizedTime, curveSamples, curveHeaders, frameHeaders, out bool3 validityPosition, out bool4 validityRotation, out bool3 validityScale), mix), validityPosition, validityRotation, validityScale);

                transformStates[binding.y] = state;

                state.Modify(transform);

            }

        }

        [BurstCompile]
        public struct ApplyAdditiveTransformCurvesJobFinal : IJobParallelForTransform
        {

            public int linearTransformCurvesIndexOffset;

            public float normalizedTime;

            [NativeDisableParallelForRestriction]
            public NativeList<TransformAnimationState> transformStates;

            [ReadOnly]
            public NativeArray<ITransformCurve.Data> curveSamples;
            [ReadOnly]
            public NativeArray<CurveHeader> curveHeaders;
            [ReadOnly]
            public NativeArray<FrameHeader> frameHeaders;

            [ReadOnly]
            public NativeArray<int2> transformCurveBindings;
            [ReadOnly]
            public NativeArray<CurveInfoPair> transformAnimationCurves;

            public void Execute(int index, TransformAccess transform)
            {

                var binding = transformCurveBindings[index];
                var curveInfo = transformAnimationCurves[binding.x];

                var state = transformStates[binding.y];

                ITransformCurve.Data mainData = EvaluateSampledCurveData(curveInfo.infoMain.isLinear, linearTransformCurvesIndexOffset, curveInfo.infoMain.curveIndex, normalizedTime, curveSamples, curveHeaders, frameHeaders, out bool3 validityPosition, out bool4 validityRotation, out bool3 validityScale);
                ITransformCurve.Data baseData = EvaluateSampledCurveData(curveInfo.infoBase.isLinear, linearTransformCurvesIndexOffset, curveInfo.infoBase.curveIndex, normalizedTime, curveSamples, curveHeaders, frameHeaders, out _, out _, out _);

                state = state.Swizzle(state.ApplyAdditive(mainData - baseData), validityPosition, validityRotation, validityScale);
                //state = state.Swizzle(state.ApplyAdditive(ITransformCurve.Data.Subtract(mainData, baseData, state.modifiedLocalRotation)), validityPosition, validityRotation, validityScale);
                
                transformStates[binding.y] = state;

                state.Modify(transform);

            }

        }

        [BurstCompile]
        public struct ApplyAdditiveMixTransformCurvesJobFinal : IJobParallelForTransform
        {

            public int linearTransformCurvesIndexOffset;

            public float normalizedTime;
            public float mix;

            [NativeDisableParallelForRestriction]
            public NativeList<TransformAnimationState> transformStates;

            [ReadOnly]
            public NativeArray<ITransformCurve.Data> curveSamples;
            [ReadOnly]
            public NativeArray<CurveHeader> curveHeaders;
            [ReadOnly]
            public NativeArray<FrameHeader> frameHeaders;

            [ReadOnly]
            public NativeArray<int2> transformCurveBindings;
            [ReadOnly]
            public NativeArray<CurveInfoPair> transformAnimationCurves;

            public void Execute(int index, TransformAccess transform)
            {

                var binding = transformCurveBindings[index];
                var curveInfo = transformAnimationCurves[binding.x];

                var state = transformStates[binding.y];

                ITransformCurve.Data mainData = EvaluateSampledCurveData(curveInfo.infoMain.isLinear, linearTransformCurvesIndexOffset, curveInfo.infoMain.curveIndex, normalizedTime, curveSamples, curveHeaders, frameHeaders, out bool3 validityPosition, out bool4 validityRotation, out bool3 validityScale);
                ITransformCurve.Data baseData = EvaluateSampledCurveData(curveInfo.infoBase.isLinear, linearTransformCurvesIndexOffset, curveInfo.infoBase.curveIndex, normalizedTime, curveSamples, curveHeaders, frameHeaders, out _, out _, out _);

                state = state.Swizzle(state.ApplyAdditiveMix(mainData - baseData, mix), validityPosition, validityRotation, validityScale);
                //state = state.Swizzle(state.ApplyAdditiveMix(ITransformCurve.Data.Subtract(mainData, baseData, state.modifiedLocalRotation), mix), validityPosition, validityRotation, validityScale); 
                
                transformStates[binding.y] = state;

                state.Modify(transform);

            }

        }

    }
}

#endif