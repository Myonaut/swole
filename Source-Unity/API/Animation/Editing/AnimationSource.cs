#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using Unity.Mathematics;

using Swole.UI;

namespace Swole.API.Unity.Animation
{
    [Serializable]
    public class AnimationSource : SwoleObject<AnimationSource, AnimationSource.Serialized>
    {

        [SerializeField]
        protected bool invalid;
        public bool IsValid => !invalid;
        public void Destroy()
        {
            invalid = true;
            if (previewAsset != null)
            {
                GameObject.Destroy(previewAsset);
                previewAsset = null;
            }
            if (rawAnimation != null) 
            { 
                rawAnimation.FlushJobData();
                rawAnimation = null;
            }
            if (compiledAnimation != null)
            {
                compiledAnimation.FlushJobData();
                compiledAnimation = null;
            }

            ClearListeners();
        }
        public void ClearListeners()
        {
            OnBecomeDirty = null;
            OnMarkDirty = null;
        }

        #region Serialization

        public override AnimationSource.Serialized AsSerializableStruct() => this;
        public override string AsJSON(bool prettyPrint = false) => AsSerializableStruct().AsJSON(prettyPrint);

        [Serializable]
        public struct Serialized : ISerializableContainer<AnimationSource, AnimationSource.Serialized>
        {
            public float timelineLength;

            public float lastPlaybackPosition;
            public float lastPlaybackSpeed;

            public PackageIdentifier package;
            public string syncedName;
            public string animationName;

            public bool visible;

            public bool isDirty;

            public List<AnimationUtils.LoopPoint> loopPoints;

            public CustomAnimation.Serialized sessionLocalAnimation;

            public AnimationSource AsOriginalType(PackageInfo packageInfo = default) => new AnimationSource(this, packageInfo);

            public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);

            public string AsJSON(bool prettyPrint = false) => swole.ToJson(this, prettyPrint);
            public static Serialized FromJSON(string json) => swole.FromJson<Serialized>(json);
        }

        public static implicit operator Serialized(AnimationSource source)
        {
            Serialized s = new Serialized();

            s.timelineLength = source.timelineLength;

            s.lastPlaybackPosition = source.playbackPosition;
            s.lastPlaybackSpeed = source.playbackSpeed;

            s.package = source.package;
            s.syncedName = source.syncedName;
            s.animationName = source.displayName;

            s.visible = source.visible;

            s.isDirty = source.isDirty;

            s.loopPoints = source.loopPoints;
            if (source.rawAnimation != null) s.sessionLocalAnimation = source.rawAnimation.AsSerializableStruct();

            return s;
        }

        public AnimationSource(AnimationSource.Serialized serializable, PackageInfo packageInfo = default) : base(serializable)
        {
            timelineLength = serializable.timelineLength;

            playbackPosition = serializable.lastPlaybackPosition;
            playbackSpeed = serializable.lastPlaybackSpeed;

            package = serializable.package;
            syncedName = serializable.syncedName;
            displayName = serializable.animationName;

            visible = serializable.visible;
            isDirty = serializable.isDirty;

            loopPoints = serializable.loopPoints;

            rawAnimation = serializable.sessionLocalAnimation.AsOriginalType(packageInfo);
        }

        #endregion

        #region Package Linking

        public bool PushToPackage(SwoleLogger logger = null)
        {
            if (ContentManager.TryFindLocalPackage(package, out ContentManager.LocalPackage pkg)) // Checks for null
            {
                if (string.IsNullOrWhiteSpace(syncedName)) syncedName = DisplayName;

                SwolePackage editPkg = SwolePackage.Create(pkg);
                if (pkg.Content.TryFind<CustomAnimation>(out var toReplace, syncedName))
                {
                    editPkg.Replace(toReplace, CompiledAnimation);
                }
                else
                {
                    editPkg.Add(CompiledAnimation);
                }

                pkg.Content = editPkg; 
                if (ContentManager.SavePackage(pkg, logger))  
                {
                    syncedName = DisplayName;
                    isDirty = false;
                    return true;
                }
            }

            return false;
        }
        public bool PullFromPackage()
        {
            if (ContentManager.TryFindLocalPackage(package, out ContentManager.LocalPackage pkg)) // Checks for null
            {
                if (string.IsNullOrWhiteSpace(syncedName)) syncedName = DisplayName;

                if (pkg.Content.TryFind<CustomAnimation>(out var newContent, syncedName))
                {
                    rawAnimation = newContent.Duplicate();
                    displayName = rawAnimation.Name;
                    isCompiled = false;
                    isDirty = false;
                    return true;
                }
            }

            return false;
        }
        public bool BindToPackage(string packageName, string packageVersion) => BindToPackage(new PackageIdentifier(packageName, packageVersion), out _);
        public bool BindToPackage(string packageName, string packageVersion, out ContentManager.LocalPackage localPackage) => BindToPackage(new PackageIdentifier(packageName, packageVersion), out localPackage);
        public bool BindToPackage(PackageIdentifier package) => BindToPackage(package, out _);
        public bool BindToPackage(PackageIdentifier package, out ContentManager.LocalPackage localPackage)
        {
            if (!ContentManager.TryFindLocalPackage(package, out localPackage)) return false;

            this.package = package;
            return true;
        }

        #endregion

        public AnimationSource() : base(default) { }

        /// <summary>
        /// This source's index in the owning animatable's animation bank.
        /// </summary>
        [NonSerialized]
        public int index;

        /// <summary>
        /// An animator that could be currently playing the animation. Used to make sure animation jobs are done before flushing data.
        /// </summary>
        public CustomAnimator previewAnimator;

        /// <summary>
        /// A layer in the owning animatable's animator. It is used to control the preview playback of this animation source.
        /// </summary>
        [NonSerialized]
        public CustomAnimationLayer previewLayer;
        /// <summary>
        /// The state machine that controls playback in the preview layer.
        /// </summary>
        public CustomStateMachine PreviewState
        {
            get
            {
                if (previewLayer == null) return null;
                return previewLayer.ActiveStateTyped; 
            }
        }
        public float GetMixFromSlider()
        {
            if (listMember != null && listMember.rectTransform != null)
            {
                var slider = listMember.rectTransform.GetComponentInChildren<Slider>();
                if (slider != null) return slider.value;
            }

            return 1;
        }
        public void SetMix(float value)
        {
            if (listMember != null && listMember.rectTransform != null)
            {
                var slider = listMember.rectTransform.GetComponentInChildren<Slider>();
                if (slider != null) slider.value = value;
            }

            if (previewLayer != null) previewLayer.mix = value;
        }

        /// <summary>
        /// The length to set the animation editor's timeline to.
        /// </summary>
        public float timelineLength;

        /// <summary>
        /// The current preview position in the animation editor timeline.
        /// </summary>
        public float playbackPosition;
        public float playbackSpeed = 1;

        /// <summary>
        /// The bound package where the compiled animation will be exported to.
        /// </summary>
        public PackageIdentifier package;
        public string syncedName;
        [SerializeField]
        protected string displayName;
        public string DisplayName
        {
            get => displayName;
            set
            {
                displayName = value;
                if (rawAnimation != null) 
                {
                    var info = rawAnimation.ContentInfo;
                    info.name = displayName;
                    rawAnimation = (CustomAnimation)rawAnimation.CreateCopyAndReplaceContentInfo(info);
                }
                if (compiledAnimation != null)
                {
                    var info = compiledAnimation.ContentInfo;
                    info.name = displayName;
                    compiledAnimation = (CustomAnimation)compiledAnimation.CreateCopyAndReplaceContentInfo(info);
                }
            }
        }

        public bool visible;

        public void SetVisibility(AnimationEditor editor, bool visible)
        {
            if (editor.IsPlayingPreview) editor.Pause();

            this.visible = visible;
            if (previewLayer != null) previewLayer.SetActive(visible);
        }

        [SerializeField]
        private bool isDirty;
        /// <summary>
        /// Has the source data changed since it was last saved to a package?
        /// </summary>
        public bool IsDirty => isDirty;

        public event VoidParameterlessDelegate OnBecomeDirty;
        public event VoidParameterlessDelegate OnMarkDirty;
        public bool MarkAsDirty()
        {
            bool wasDirty = isDirty;

            isDirty = true;
            isCompiled = false;

            if (!wasDirty) OnBecomeDirty?.Invoke();
            OnMarkDirty?.Invoke();
            return wasDirty;
        }

        [NonSerialized]
        public UICategorizedList.Member listMember;

        protected const string _packageLinkButtonTag = "link";
        public void CreateListMember(AnimationEditor editor, ImportedAnimatable animatable, bool force = false)
        {
            if (editor == null || editor.importedAnimatablesList == null || animatable == null || animatable.listCategory == null || (listMember != null && !force)) return;

            var session = editor.CurrentSession; 
            listMember = editor.importedAnimatablesList.AddNewListMember(displayName, animatable.listCategory, () => {

                if (session == null || editor.CurrentSession != session) return;
                if (animatable != null) animatable.SetCurrentlyEditedSource(editor, index);
                session.SetActiveObject(editor, index, true, false);
                editor.RefreshAnimatableListUI();

            });

            if (listMember != null && listMember.rectTransform != null)
            {
                var slider = listMember.rectTransform.GetComponentInChildren<Slider>(); 
                if (slider != null)
                {
                    slider.minValue = 0;
                    slider.maxValue = 1;
                    if (slider.onValueChanged == null) slider.onValueChanged = new Slider.SliderEvent();
                    slider.onValueChanged.RemoveAllListeners();
                    slider.onValueChanged.AddListener((float value) =>
                    {
                        if (previewLayer == null) return;
                        previewLayer.mix = value;
                    });
                }

                CustomEditorUtils.SetButtonOnClickActionByName(listMember.rectTransform, _packageLinkButtonTag, () => editor.OpenPackageLinkWindow(this)); 
            }
        }

        [NonSerialized]
        private CustomAnimationAsset previewAsset;
        public CustomAnimationAsset PreviewAsset
        {
            get
            {
                if (previewAsset == null) 
                { 
                    previewAsset = ScriptableObject.CreateInstance<CustomAnimationAsset>();
                    previewAsset.name = $"previewAsset:{previewAsset.GetInstanceID()}"; 
                }
                return previewAsset;
            }
        }

        [NonSerialized]
        public CustomAnimation rawAnimation;
        public CustomAnimation GetOrCreateRawData()
        {
            if (rawAnimation == null) 
            { 
                rawAnimation = new CustomAnimation(displayName, string.Empty, DateTime.Now, DateTime.Now, string.Empty, CustomAnimation.DefaultFrameRate, CustomAnimation.DefaultJobCurveSampleRate, null, null, null, null, null, null, null, null);
                MarkAsDirty();
            }
            return rawAnimation;
        }
        [NonSerialized]
        private CustomAnimation compiledAnimation;
        public CustomAnimation CompiledAnimation
        {
            get
            {
                if (!IsCompiled) Compile();
                return compiledAnimation;
            }
        }
        [NonSerialized]
        private bool isCompiled;
        /// <summary>
        /// Have the latest changes to the source data been compiled?
        /// </summary>
        public bool IsCompiled => isCompiled && compiledAnimation != null;

        public List<AnimationUtils.LoopPoint> loopPoints;

        public List<AnimationUtils.LoopPoint> GetLoopPoints(string id, List<AnimationUtils.LoopPoint> outputList = null)
        {
            if (outputList == null) outputList = new List<AnimationUtils.LoopPoint>();
            if (loopPoints == null) return outputList;

            id = id.AsID();

            for (int a = 0; a < loopPoints.Count; a++)
            {
                var lp = loopPoints[a];
                if (lp == null) continue;
                if (lp.id.AsID() == id) outputList.Add(lp);
            }

            outputList.Sort((x, y) => (int)Mathf.Sign(x.position - y.position));

            return outputList;
        }

        private static readonly List<AnimationUtils.LoopPoint> tempLPs = new List<AnimationUtils.LoopPoint>();
        private static readonly List<TransformLinearCurve> transformLinearCurves = new List<TransformLinearCurve>();
        private static readonly List<TransformCurve> transformCurves = new List<TransformCurve>();
        private static readonly List<PropertyLinearCurve> propertyLinearCurves = new List<PropertyLinearCurve>();
        private static readonly List<PropertyCurve> propertyCurves = new List<PropertyCurve>();
        private static readonly List<CustomAnimation.CurveInfoPair> transformCurveInfo = new List<CustomAnimation.CurveInfoPair>();
        private static readonly List<CustomAnimation.CurveInfoPair> propertyCurveInfo = new List<CustomAnimation.CurveInfoPair>();
        private static readonly List<Keyframe> tempKeyframes = new List<Keyframe>();
        private static readonly List<ITransformCurve.Frame> tempTransformFrames = new List<ITransformCurve.Frame>();
        private static readonly List<IPropertyCurve.Frame> tempPropertyFrames = new List<IPropertyCurve.Frame>();
        public CustomAnimation Compile(bool removeEmptyCurves=true)
        {
            if (rawAnimation != null)
            {
                isCompiled = false;

                bool isNew = false;
                if (compiledAnimation == null)
                {
                    isNew = true;
                    compiledAnimation = new CustomAnimation(rawAnimation.ContentInfo, rawAnimation.framesPerSecond, rawAnimation.jobCurveSampleRate, rawAnimation.timeCurve, null, null, null, null, rawAnimation.transformAnimationCurves, rawAnimation.propertyAnimationCurves, rawAnimation.events, rawAnimation.PackageInfo);
                }
                compiledAnimation.framesPerSecond = rawAnimation.framesPerSecond;
                compiledAnimation.jobCurveSampleRate = rawAnimation.jobCurveSampleRate;
                compiledAnimation.timeCurve = rawAnimation.timeCurve;
                compiledAnimation.transformAnimationCurves = rawAnimation.transformAnimationCurves;
                compiledAnimation.propertyAnimationCurves = rawAnimation.propertyAnimationCurves;
                compiledAnimation.events = rawAnimation.events; 

                //TransformLinearCurve[] transformLinearCurves = rawAnimation.transformLinearCurves == null ? new TransformLinearCurve[0] : (TransformLinearCurve[])rawAnimation.transformLinearCurves.Clone();
                //TransformCurve[] transformCurves = rawAnimation.transformCurves == null ? new TransformCurve[0] : (TransformCurve[])rawAnimation.transformCurves.Clone();
                //PropertyLinearCurve[] propertyLinearCurves = rawAnimation.propertyLinearCurves == null ? new PropertyLinearCurve[0] : (PropertyLinearCurve[])rawAnimation.propertyLinearCurves.Clone();
                //PropertyCurve[] propertyCurves = rawAnimation.propertyCurves == null ? new PropertyCurve[0] : (PropertyCurve[])rawAnimation.propertyCurves.Clone();

                transformLinearCurves.Clear();
                if (rawAnimation.transformLinearCurves != null) transformLinearCurves.AddRange(rawAnimation.transformLinearCurves);
                transformCurves.Clear();
                if (rawAnimation.transformCurves != null) transformCurves.AddRange(rawAnimation.transformCurves);
                propertyLinearCurves.Clear();
                if (rawAnimation.propertyLinearCurves != null) propertyLinearCurves.AddRange(rawAnimation.propertyLinearCurves);
                propertyCurves.Clear();
                if (rawAnimation.propertyCurves != null) propertyCurves.AddRange(rawAnimation.propertyCurves); 

                transformCurveInfo.Clear();
                if (rawAnimation.transformAnimationCurves != null) transformCurveInfo.AddRange(rawAnimation.transformAnimationCurves);
                propertyCurveInfo.Clear();
                if (rawAnimation.propertyAnimationCurves != null) propertyCurveInfo.AddRange(rawAnimation.propertyAnimationCurves);

                if (removeEmptyCurves)
                {
                    #region Remove Empty & Unused Curves
                    bool IsEmptyTransformCurve(CustomAnimation.CurveInfo info)
                    {
                        if (info.isLinear && info.curveIndex >= 0 && info.curveIndex < transformLinearCurves.Count)
                        {
                            var curve = transformLinearCurves[info.curveIndex];
                            if (curve == null || curve.frames == null || curve.frames.Length <= 0) return true; else return false;
                        }
                        else if (!info.isLinear && info.curveIndex >= 0 && info.curveIndex < transformCurves.Count)
                        {
                            var curve = transformCurves[info.curveIndex];
                            if (curve == null || (!math.any(curve.ValidityPosition) && !math.any(curve.ValidityRotation) && !math.any(curve.ValidityScale))) return true; else return false;
                        }

                        return true;
                    }
                    bool IsEmptyPropertyCurve(CustomAnimation.CurveInfo info)
                    {
                        if (info.isLinear && info.curveIndex >= 0 && info.curveIndex < propertyLinearCurves.Count)
                        {
                            var curve = propertyLinearCurves[info.curveIndex];
                            if (curve == null || curve.frames == null || curve.frames.Length <= 0) return true; else return false;
                        }
                        else if (!info.isLinear && info.curveIndex >= 0 && info.curveIndex < propertyCurves.Count)
                        {
                            var curve = propertyCurves[info.curveIndex];
                            if (curve == null || (curve.propertyValueCurve == null || curve.propertyValueCurve.length <= 0)) return true; else return false;
                        }

                        return true;
                    }

                    bool DeleteTransformLinearCurve(int index)
                    {
                        foreach (var info in transformCurveInfo)
                        {
                            if (info.infoBase.isLinear && info.infoBase.curveIndex == index)
                            {
                                if (!IsEmptyTransformCurve(info.infoMain)) return false; // Main part of pair is not empty, so cancel deletion
                            }
                            //else if (info.infoMain.isLinear && info.infoMain.curveIndex == index) Empty main curves paired with non-empty base curves should still be deleted
                            //{
                            //if (!IsEmptyTransformCurve(info.infoBase)) return false;  // Base part of pair is not empty, so cancel deletion
                            //}
                        }
                        transformCurveInfo.RemoveAll(i => (i.infoBase.isLinear && i.infoBase.curveIndex == index) || (i.infoMain.isLinear && i.infoMain.curveIndex == index));
                        for (int a = 0; a < transformCurveInfo.Count; a++)
                        {
                            var info = transformCurveInfo[a];
                            var infoBase = info.infoBase;
                            var infoMain = info.infoMain;

                            if (infoBase.isLinear && infoBase.curveIndex >= index) infoBase.curveIndex = infoBase.curveIndex - 1;
                            if (infoMain.isLinear && infoMain.curveIndex >= index) infoMain.curveIndex = infoMain.curveIndex - 1;

                            info.infoBase = infoBase;
                            info.infoMain = infoMain;
                            transformCurveInfo[a] = info;
                        }
                        transformLinearCurves.RemoveAt(index);
                        return true;
                    }
                    bool DeleteTransformCurve(int index)
                    {
                        foreach (var info in transformCurveInfo)
                        {
                            if (!info.infoBase.isLinear && info.infoBase.curveIndex == index)
                            {
                                if (!IsEmptyTransformCurve(info.infoMain)) return false; // Main part of pair is not empty, so cancel deletion
                            }
                            //else if (!info.infoMain.isLinear && info.infoMain.curveIndex == index) Empty main curves paired with non-empty base curves should still be deleted
                            //{
                            //if (!IsEmptyTransformCurve(info.infoBase)) return false;  // Base part of pair is not empty, so cancel deletion
                            //}
                        }
                        transformCurveInfo.RemoveAll(i => (!i.infoBase.isLinear && i.infoBase.curveIndex == index) || (!i.infoMain.isLinear && i.infoMain.curveIndex == index));
                        for (int a = 0; a < transformCurveInfo.Count; a++)
                        {
                            var info = transformCurveInfo[a];
                            var infoBase = info.infoBase;
                            var infoMain = info.infoMain;

                            if (!infoBase.isLinear && infoBase.curveIndex >= index) infoBase.curveIndex = infoBase.curveIndex - 1;
                            if (!infoMain.isLinear && infoMain.curveIndex >= index) infoMain.curveIndex = infoMain.curveIndex - 1;

                            info.infoBase = infoBase;
                            info.infoMain = infoMain;
                            transformCurveInfo[a] = info;
                        }
                        transformCurves.RemoveAt(index);
                        return true;
                    }

                    bool DeletePropertyLinearCurve(int index)
                    {
                        foreach (var info in propertyCurveInfo)
                        {
                            if (info.infoBase.isLinear && info.infoBase.curveIndex == index)
                            {
                                if (!IsEmptyPropertyCurve(info.infoMain)) return false; // Main part of pair is not empty, so cancel deletion
                            }
                            //else if (info.infoMain.isLinear && info.infoMain.curveIndex == index) Empty main curves paired with non-empty base curves should still be deleted
                            //{
                            //if (!IsEmptyPropertyCurve(info.infoBase)) return false;  // Base part of pair is not empty, so cancel deletion
                            //}
                        }
                        propertyCurveInfo.RemoveAll(i => (i.infoBase.isLinear && i.infoBase.curveIndex == index) || (i.infoMain.isLinear && i.infoMain.curveIndex == index));
                        for (int a = 0; a < propertyCurveInfo.Count; a++)
                        {
                            var info = propertyCurveInfo[a];
                            var infoBase = info.infoBase;
                            var infoMain = info.infoMain;

                            if (infoBase.isLinear && infoBase.curveIndex >= index) infoBase.curveIndex = infoBase.curveIndex - 1;
                            if (infoMain.isLinear && infoMain.curveIndex >= index) infoMain.curveIndex = infoMain.curveIndex - 1;

                            info.infoBase = infoBase;
                            info.infoMain = infoMain;
                            propertyCurveInfo[a] = info;
                        }
                        propertyLinearCurves.RemoveAt(index);
                        return true;
                    }
                    bool DeletePropertyCurve(int index)
                    {
                        foreach (var info in propertyCurveInfo)
                        {
                            if (!info.infoBase.isLinear && info.infoBase.curveIndex == index)
                            {
                                if (!IsEmptyPropertyCurve(info.infoMain)) return false; // Main part of pair is not empty, so cancel deletion
                            }
                            //else if (!info.infoMain.isLinear && info.infoMain.curveIndex == index) Empty main curves paired with non-empty base curves should still be deleted
                            //{
                            //if (!IsEmptyPropertyCurve(info.infoBase)) return false;  // Base part of pair is not empty, so cancel deletion
                            //}
                        }
                        propertyCurveInfo.RemoveAll(i => (!i.infoBase.isLinear && i.infoBase.curveIndex == index) || (!i.infoMain.isLinear && i.infoMain.curveIndex == index));
                        for (int a = 0; a < propertyCurveInfo.Count; a++)
                        {
                            var info = propertyCurveInfo[a];
                            var infoBase = info.infoBase;
                            var infoMain = info.infoMain;

                            if (!infoBase.isLinear && infoBase.curveIndex >= index) infoBase.curveIndex = infoBase.curveIndex - 1;
                            if (!infoMain.isLinear && infoMain.curveIndex >= index) infoMain.curveIndex = infoMain.curveIndex - 1;

                            info.infoBase = infoBase;
                            info.infoMain = infoMain;
                            propertyCurveInfo[a] = info;
                        }
                        propertyCurves.RemoveAt(index);
                        return true;
                    }

                    int i;

                    i = 0;
                    while (i < transformLinearCurves.Count)
                    {
                        bool flag = true;
                        var curve = transformLinearCurves[i];
                        if (curve.frames == null || curve.frames.Length <= 0) // Check if curve is empty
                        {
                            flag = false;
                        }
                        else
                        {
                            flag = false; // Remains false if not referenced
                            foreach (var info in transformCurveInfo) // Check for references
                            {
                                if (info.infoBase.isLinear && info.infoBase.curveIndex == i)
                                {
                                    flag = true;
                                    break;
                                }
                                else if (info.infoMain.isLinear && info.infoMain.curveIndex == i)
                                {
                                    flag = true;
                                    break;
                                }
                            }
                        }
                        if (!flag) flag = !DeleteTransformLinearCurve(i);
                        if (flag) i++;
                    }
                    i = 0;
                    while (i < transformCurves.Count)
                    {
                        bool flag = true;
                        var curve = transformCurves[i];
                        if (!math.any(curve.ValidityPosition) && !math.any(curve.ValidityRotation) && !math.any(curve.ValidityScale)) // Check if curve is empty
                        {
                            flag = false;
                        }
                        else
                        {
                            flag = false; // Remains false if not referenced
                            foreach (var info in transformCurveInfo) // Check for references
                            {
                                if (!info.infoBase.isLinear && info.infoBase.curveIndex == i)
                                {
                                    flag = true;
                                    break;
                                }
                                else if (!info.infoMain.isLinear && info.infoMain.curveIndex == i)
                                {
                                    flag = true;
                                    break;
                                }
                            }
                        }
                        if (!flag) flag = !DeleteTransformCurve(i);
                        if (flag) i++;
                    }

                    i = 0;
                    while (i < propertyLinearCurves.Count)
                    {
                        bool flag = true;
                        var curve = propertyLinearCurves[i];
                        if (curve.frames == null || curve.frames.Length <= 0) // Check if curve is empty
                        {
                            flag = false;
                        }
                        else
                        {
                            flag = false; // Remains false if not referenced
                            foreach (var info in propertyCurveInfo) // Check for references
                            {
                                if (info.infoBase.isLinear && info.infoBase.curveIndex == i)
                                {
                                    flag = true;
                                    break;
                                }
                                else if (info.infoMain.isLinear && info.infoMain.curveIndex == i)
                                {
                                    flag = true;
                                    break;
                                }
                            }
                        }
                        if (!flag) flag = !DeletePropertyLinearCurve(i);
                        if (flag) i++;
                    }
                    i = 0;
                    while (i < propertyCurves.Count)
                    {
                        bool flag = true;
                        var curve = propertyCurves[i];
                        if (curve.propertyValueCurve == null || curve.propertyValueCurve.length <= 0) // Check if curve is empty
                        {
                            flag = false;
                        }
                        else
                        {
                            flag = false; // Remains false if not referenced
                            foreach (var info in propertyCurveInfo) // Check for references
                            {
                                if (!info.infoBase.isLinear && info.infoBase.curveIndex == i)
                                {
                                    flag = true;
                                    break;
                                }
                                else if (!info.infoMain.isLinear && info.infoMain.curveIndex == i)
                                {
                                    flag = true;
                                    break;
                                }
                            }
                        }
                        if (!flag) flag = !DeletePropertyCurve(i);
                        if (flag) i++;
                    }
                    #endregion
                }

                #region Bake Loop Points

                Keyframe[] LoopCurve(List<AnimationUtils.LoopPoint> loopPoints, Keyframe[] oldKeyframes)
                {
                    if (oldKeyframes.Length <= 1 || loopPoints.Count <= 0) return null;

                    List<Keyframe> keyframes = new List<Keyframe>(oldKeyframes);
                    var firstFrame = keyframes[0];

                    for (int a = 0; a < loopPoints.Count; a++)
                    {
                        var lp = loopPoints[a];
                        if (lp == null) continue;

                        var key = firstFrame;
                        key.time = lp.position / (float)rawAnimation.framesPerSecond;
                        // Prevent overlapping keyframes
                        bool overlapping = false;
                        foreach (var kf in keyframes)
                        {
                            if (kf.time == key.time)
                            {
                                overlapping = true;
                                break;
                            }
                        }
                        //
                        if (!overlapping) keyframes.Add(key);
                    }

                    keyframes.Sort((x, y) => (int)Mathf.Sign(x.time - y.time));
                    return keyframes.ToArray();
                }

                AnimationCurve LoopAnimationCurve(List<AnimationUtils.LoopPoint> loopPoints, AnimationCurve curve)
                {
                    if (curve == null) return null;

                    Keyframe[] newKeys = LoopCurve(loopPoints, curve.keys);
                    if (newKeys != null)
                    {
                        curve = curve.AsSerializableStruct();
                        curve.keys = newKeys;
                    }
                    return curve;
                }

                TransformCurve LoopTransformCurve(TransformCurve curve)
                {
                    if (curve == null) return null;

                    bool cloned = false;
                    AnimationCurve LoopInnerCurve(AnimationCurve innerCurve, string suffix)
                    {
                        tempLPs.Clear();
                        GetLoopPoints(curve.name + suffix, tempLPs);
                        if (tempLPs.Count > 0)
                        {
                            if (!cloned)
                            {
                                curve = (TransformCurve)curve.Clone();
                                cloned = true;
                            }
                            return LoopAnimationCurve(tempLPs, innerCurve);
                        }
                        return innerCurve;
                    }

                    AnimationCurve curveOut;

                    curveOut = LoopInnerCurve(curve.localPositionCurveX, ".localPosition.x");
                    curve.localPositionCurveX = curveOut;
                    curveOut = LoopInnerCurve(curve.localPositionCurveY, ".localPosition.y");
                    curve.localPositionCurveY = curveOut;
                    curveOut = LoopInnerCurve(curve.localPositionCurveZ, ".localPosition.z");
                    curve.localPositionCurveZ = curveOut;

                    curveOut = LoopInnerCurve(curve.localRotationCurveX, ".localRotation.x");
                    curve.localRotationCurveX = curveOut;
                    curveOut = LoopInnerCurve(curve.localRotationCurveY, ".localRotation.y");
                    curve.localRotationCurveY = curveOut;
                    curveOut = LoopInnerCurve(curve.localRotationCurveZ, ".localRotation.z");
                    curve.localRotationCurveZ = curveOut;
                    curveOut = LoopInnerCurve(curve.localRotationCurveW, ".localRotation.w");
                    curve.localRotationCurveW = curveOut;

                    curveOut = LoopInnerCurve(curve.localScaleCurveX, ".localScale.x");
                    curve.localScaleCurveX = curveOut;
                    curveOut = LoopInnerCurve(curve.localScaleCurveY, ".localScale.y");
                    curve.localScaleCurveY = curveOut;
                    curveOut = LoopInnerCurve(curve.localScaleCurveZ, ".localScale.z");
                    curve.localScaleCurveZ = curveOut;

                    return curve;
                }

                for (int a = 0; a < transformLinearCurves.Count; a++)
                {
                    var curve = transformLinearCurves[a];
                    if (curve == null || curve.frames == null || curve.frames.Length <= 1) continue;

                    string id = curve.name + ".linear";

                    tempLPs.Clear();
                    GetLoopPoints(id, tempLPs);
                    if (tempLPs.Count > 0)
                    {
                        curve = (TransformLinearCurve)curve.Clone();
                        List<ITransformCurve.Frame> keyframes = new List<ITransformCurve.Frame>(curve.frames);
                        if (keyframes.Count > 1)
                        {
                            var firstFrame = keyframes[0];

                            for (int b = 0; b < tempLPs.Count; b++)
                            {
                                var lp = tempLPs[b];
                                if (lp == null) continue;

                                // Prevent overlapping keyframes
                                bool overlapping = false;
                                foreach (var kf in keyframes)
                                {
                                    if (kf.timelinePosition == lp.position)
                                    {
                                        overlapping = true;
                                        break;
                                    }
                                }
                                //
                                if (!overlapping) keyframes.Add(new ITransformCurve.Frame() { timelinePosition = lp.position, data = firstFrame.data, interpolationCurve = firstFrame.interpolationCurve });
                            }

                            keyframes.Sort((x, y) => (int)Mathf.Sign(x.timelinePosition - y.timelinePosition));
                            curve.frames = keyframes.ToArray();

                            transformLinearCurves[a] = curve;
                        }
                    }

                }

                for (int a = 0; a < transformCurves.Count; a++) transformCurves[a] = LoopTransformCurve(transformCurves[a]);

                for (int a = 0; a < propertyLinearCurves.Count; a++)
                {
                    var curve = propertyLinearCurves[a];
                    if (curve == null || curve.frames == null || curve.frames.Length <= 1) continue;

                    string id = curve.name + ".linear";

                    tempLPs.Clear();
                    GetLoopPoints(id, tempLPs);
                    if (tempLPs.Count > 0)
                    {
                        curve = (PropertyLinearCurve)curve.Clone();
                        List<IPropertyCurve.Frame> keyframes = new List<IPropertyCurve.Frame>(curve.frames);
                        if (keyframes.Count > 1)
                        {
                            var firstFrame = keyframes[0];

                            for (int b = 0; b < tempLPs.Count; b++)
                            {
                                var lp = tempLPs[b];
                                if (lp == null) continue;

                                // Prevent overlapping keyframes
                                bool overlapping = false;
                                foreach (var kf in keyframes)
                                {
                                    if (kf.timelinePosition == lp.position)
                                    {
                                        overlapping = true;
                                        break;
                                    }
                                }
                                //
                                if (!overlapping) keyframes.Add(new IPropertyCurve.Frame() { timelinePosition = lp.position, value = firstFrame.value, interpolationCurve = firstFrame.interpolationCurve });
                            }

                            keyframes.Sort((x, y) => (int)Mathf.Sign(x.timelinePosition - y.timelinePosition));
                            curve.frames = keyframes.ToArray();

                            propertyLinearCurves[a] = curve;
                        }
                    }

                }

                for (int a = 0; a < propertyCurves.Count; a++)
                {
                    var curve = propertyCurves[a];
                    if (curve == null || curve.propertyValueCurve == null || curve.propertyValueCurve.length <= 1) continue;

                    string id = curve.name + ".value";

                    tempLPs.Clear();
                    GetLoopPoints(id, tempLPs);
                    if (tempLPs.Count > 0)
                    {
                        curve = (PropertyCurve)curve.Clone();
                        List<Keyframe> keyframes = new List<Keyframe>(curve.propertyValueCurve.keys);
                        if (keyframes.Count > 1)
                        {
                            var firstFrame = keyframes[0];

                            for (int b = 0; b < tempLPs.Count; b++)
                            {
                                var lp = tempLPs[b];
                                if (lp == null) continue;

                                var key = firstFrame;
                                key.time = lp.position / (float)rawAnimation.framesPerSecond;
                                // Prevent overlapping keyframes
                                bool overlapping = false;
                                foreach (var kf in keyframes)
                                {
                                    if (kf.time == key.time)
                                    {
                                        overlapping = true;
                                        break;
                                    }
                                }
                                //
                                if (!overlapping) keyframes.Add(key);
                            }

                            keyframes.Sort((x, y) => (int)Mathf.Sign(x.time - y.time));
                            curve.propertyValueCurve.keys = keyframes.ToArray();

                            propertyCurves[a] = curve;
                        }
                    }
                }

                #endregion

                #region Ensure Animation Length

                float currentLength = 0;

                foreach (var curve in transformLinearCurves) if (curve != null) currentLength = Mathf.Max(currentLength, curve.GetLengthInSeconds(rawAnimation.framesPerSecond));
                foreach (var curve in transformCurves) if (curve != null) currentLength = Mathf.Max(currentLength, curve.GetLengthInSeconds(rawAnimation.framesPerSecond));
                foreach (var curve in propertyLinearCurves) if (curve != null) currentLength = Mathf.Max(currentLength, curve.GetLengthInSeconds(rawAnimation.framesPerSecond));
                foreach (var curve in propertyCurves) if (curve != null) currentLength = Mathf.Max(currentLength, curve.GetLengthInSeconds(rawAnimation.framesPerSecond));

                if (currentLength < timelineLength)
                {
                    bool LengthenCurve(AnimationCurve curve, out AnimationCurve outCurve)
                    {
                        outCurve = curve;
                        if (curve == null || curve.length <= 0 || !(curve.postWrapMode == WrapMode.Clamp || curve.postWrapMode == WrapMode.ClampForever || curve.postWrapMode == WrapMode.Once)) return false;

                        outCurve = new AnimationCurve();
                        outCurve.preWrapMode = curve.preWrapMode;
                        outCurve.postWrapMode = curve.postWrapMode;

                        tempKeyframes.Clear();
                        for (int a = 0; a < curve.length; a++) tempKeyframes.Add(curve[a]);

                        var finalKey = tempKeyframes[tempKeyframes.Count - 1];
                        finalKey.time = timelineLength;
                        finalKey = AnimationCurveEditor.CalculateLinearInTangent(finalKey, tempKeyframes[tempKeyframes.Count - 1]);
                        tempKeyframes[tempKeyframes.Count - 1] = AnimationCurveEditor.CalculateLinearOutTangent(tempKeyframes[tempKeyframes.Count - 1], finalKey);
                        tempKeyframes.Add(finalKey);
                        outCurve.keys = tempKeyframes.ToArray();

                        return true;
                    }

                    bool flag = true;
                    for(int a = 0; a < transformCurves.Count; a++)
                    {
                        var curve = transformCurves[a];
                        if (curve != null)
                        {
                            AnimationCurve editedCurve;
                            if (LengthenCurve(curve.localPositionCurveX, out editedCurve))
                            {
                                flag = false;
                                curve = curve.ShallowCopy();
                                curve.localPositionCurveX = editedCurve;
                            }
                            else if (LengthenCurve(curve.localPositionCurveY, out editedCurve))
                            {
                                flag = false;
                                curve = curve.ShallowCopy();
                                curve.localPositionCurveY = editedCurve;
                            }
                            else if (LengthenCurve(curve.localPositionCurveZ, out editedCurve))
                            {
                                flag = false;
                                curve = curve.ShallowCopy();
                                curve.localPositionCurveZ = editedCurve;
                            }
                            else if (LengthenCurve(curve.localRotationCurveX, out editedCurve))
                            {
                                flag = false;
                                curve = curve.ShallowCopy();
                                curve.localRotationCurveX = editedCurve;
                            }
                            else if (LengthenCurve(curve.localRotationCurveY, out editedCurve))
                            {
                                flag = false;
                                curve = curve.ShallowCopy();
                                curve.localRotationCurveY = editedCurve;
                            }
                            else if (LengthenCurve(curve.localRotationCurveZ, out editedCurve))
                            {
                                flag = false;
                                curve = curve.ShallowCopy();
                                curve.localRotationCurveZ = editedCurve;
                            }
                            else if (LengthenCurve(curve.localRotationCurveW, out editedCurve))
                            {
                                flag = false;
                                curve = curve.ShallowCopy();
                                curve.localRotationCurveW = editedCurve;
                            }
                            else if (LengthenCurve(curve.localScaleCurveX, out editedCurve))
                            {
                                flag = false;
                                curve = curve.ShallowCopy();
                                curve.localScaleCurveX = editedCurve;
                            }
                            else if (LengthenCurve(curve.localScaleCurveY, out editedCurve))
                            {
                                flag = false;
                                curve = curve.ShallowCopy();
                                curve.localScaleCurveY = editedCurve;
                            }
                            else if (LengthenCurve(curve.localScaleCurveZ, out editedCurve))
                            {
                                flag = false;
                                curve = curve.ShallowCopy();
                                curve.localScaleCurveZ = editedCurve;
                            }

                            if (!flag)
                            {
                                transformCurves[a] = curve;
                                break;
                            }
                        }
                    }
                    if (flag)
                    {
                        for (int a = 0; a < propertyCurves.Count; a++)
                        {
                            var curve = propertyCurves[a];
                            if (curve != null && LengthenCurve(curve.propertyValueCurve, out AnimationCurve editedCurve))
                            {
                                flag = false;
                                curve = curve.ShallowCopy();
                                curve.propertyValueCurve = editedCurve;
                                propertyCurves[a] = curve;
                                break;
                            }
                        }
                    }
                    if (flag)
                    {
                        for (int a = 0; a < transformLinearCurves.Count; a++)
                        {
                            var curve = transformLinearCurves[a];
                            if (curve != null && curve.frames != null && curve.frames.Length > 0 && (curve.postWrapMode == WrapMode.Clamp || curve.postWrapMode == WrapMode.ClampForever || curve.postWrapMode == WrapMode.Once))
                            {
                                flag = false;
                                curve = curve.ShallowCopy();

                                tempTransformFrames.Clear();
                                tempTransformFrames.AddRange(curve.frames);
                                tempTransformFrames.Sort((ITransformCurve.Frame x, ITransformCurve.Frame y) => (int)Mathf.Sign(y.timelinePosition - x.timelinePosition));
                                var lastFrame = tempTransformFrames[tempTransformFrames.Count - 1];
                                lastFrame.timelinePosition = Mathf.FloorToInt(timelineLength * rawAnimation.framesPerSecond);
                                tempTransformFrames.Add(lastFrame);
                                curve.frames = tempTransformFrames.ToArray(); 

                                transformLinearCurves[a] = curve;
                                break;
                            }
                        }
                    }
                    if (flag)
                    {
                        for (int a = 0; a < propertyLinearCurves.Count; a++)
                        {
                            var curve = propertyLinearCurves[a];
                            if (curve != null && curve.frames != null && curve.frames.Length > 0 && (curve.postWrapMode == WrapMode.Clamp || curve.postWrapMode == WrapMode.ClampForever || curve.postWrapMode == WrapMode.Once))
                            {
                                curve = curve.ShallowCopy();

                                tempPropertyFrames.Clear();
                                tempPropertyFrames.AddRange(curve.frames);
                                tempPropertyFrames.Sort((IPropertyCurve.Frame x, IPropertyCurve.Frame y) => (int)Mathf.Sign(y.timelinePosition - x.timelinePosition));
                                var lastFrame = tempPropertyFrames[tempPropertyFrames.Count - 1];
                                lastFrame.timelinePosition = Mathf.FloorToInt(timelineLength * rawAnimation.framesPerSecond);
                                tempPropertyFrames.Add(lastFrame);
                                curve.frames = tempPropertyFrames.ToArray();

                                propertyLinearCurves[a] = curve;
                                flag = false;
                                break;
                            }
                        }
                    }
                }
                else if (currentLength > timelineLength)
                {

                    bool ShortenCurve(AnimationCurve curve, out AnimationCurve outCurve)
                    {
                        outCurve = curve;
                        if (curve == null || curve.length <= 0 || curve[curve.length - 1].time <= timelineLength) return false;

                        outCurve = new AnimationCurve();
                        outCurve.preWrapMode = curve.preWrapMode;
                        outCurve.postWrapMode = curve.postWrapMode;

                        tempKeyframes.Clear();
                        for (int a = 0; a < curve.length; a++) tempKeyframes.Add(curve[a]);

                        while (tempKeyframes.Count > 1 && tempKeyframes[tempKeyframes.Count - 2].time > timelineLength) tempKeyframes.RemoveAt(tempKeyframes.Count - 1);
                        var finalKey = tempKeyframes[tempKeyframes.Count - 1];
                        finalKey.time = timelineLength;
                        tempKeyframes[tempKeyframes.Count - 1] = finalKey;

                        outCurve.keys = tempKeyframes.ToArray();
                        return true;
                    }

                    for (int a = 0; a < transformCurves.Count; a++)
                    {
                        var curve = transformCurves[a];
                        if (curve != null)
                        {
                            AnimationCurve editedCurve;
                            bool canCopy = true;
                            if (ShortenCurve(curve.localPositionCurveX, out editedCurve))
                            {
                                if (canCopy) 
                                { 
                                    curve = curve.ShallowCopy();
                                    canCopy = false;
                                }
                                curve.localPositionCurveX = editedCurve;
                            }
                            if (ShortenCurve(curve.localPositionCurveY, out editedCurve))
                            {
                                if (canCopy)
                                {
                                    curve = curve.ShallowCopy();
                                    canCopy = false;
                                }
                                curve.localPositionCurveY = editedCurve;
                            }
                            if (ShortenCurve(curve.localPositionCurveZ, out editedCurve))
                            {
                                if (canCopy)
                                {
                                    curve = curve.ShallowCopy();
                                    canCopy = false;
                                }
                                curve.localPositionCurveZ = editedCurve;
                            }

                            if (ShortenCurve(curve.localRotationCurveX, out editedCurve))
                            {
                                if (canCopy)
                                {
                                    curve = curve.ShallowCopy();
                                    canCopy = false;
                                }
                                curve.localRotationCurveX = editedCurve;
                            }
                            if (ShortenCurve(curve.localRotationCurveY, out editedCurve))
                            {
                                if (canCopy)
                                {
                                    curve = curve.ShallowCopy();
                                    canCopy = false;
                                }
                                curve.localRotationCurveY = editedCurve;
                            }
                            if (ShortenCurve(curve.localRotationCurveZ, out editedCurve))
                            {
                                if (canCopy)
                                {
                                    curve = curve.ShallowCopy();
                                    canCopy = false;
                                }
                                curve.localRotationCurveZ = editedCurve;
                            }
                            if (ShortenCurve(curve.localRotationCurveW, out editedCurve))
                            {
                                if (canCopy)
                                {
                                    curve = curve.ShallowCopy();
                                    canCopy = false;
                                }
                                curve.localRotationCurveW = editedCurve;
                            }

                            if (ShortenCurve(curve.localScaleCurveX, out editedCurve))
                            {
                                if (canCopy)
                                {
                                    curve = curve.ShallowCopy();
                                    canCopy = false;
                                }
                                curve.localScaleCurveX = editedCurve;
                            }
                            if (ShortenCurve(curve.localScaleCurveY, out editedCurve))
                            {
                                if (canCopy)
                                {
                                    curve = curve.ShallowCopy();
                                    canCopy = false;
                                }
                                curve.localScaleCurveY = editedCurve;
                            }
                            if (ShortenCurve(curve.localScaleCurveZ, out editedCurve))
                            {
                                if (canCopy)
                                {
                                    curve = curve.ShallowCopy();
                                    canCopy = false;
                                }
                                curve.localScaleCurveZ = editedCurve;
                            }

                            transformCurves[a] = curve;
                        }
                    }

                    for (int a = 0; a < propertyCurves.Count; a++)
                    {
                        var curve = propertyCurves[a];
                        if (curve != null && ShortenCurve(curve.propertyValueCurve, out AnimationCurve editedCurve))
                        {
                            curve = curve.ShallowCopy();
                            curve.propertyValueCurve = editedCurve;
                            propertyCurves[a] = curve; 
                        }
                    }

                    int frameLength = Mathf.FloorToInt(timelineLength * rawAnimation.framesPerSecond);
                    for (int a = 0; a < transformLinearCurves.Count; a++)
                    {
                        var curve = transformLinearCurves[a];
                        if (curve != null && curve.GetLengthInSeconds(rawAnimation.framesPerSecond) > timelineLength)
                        {
                            curve = curve.ShallowCopy();

                            tempTransformFrames.Clear();
                            tempTransformFrames.AddRange(curve.frames);
                            tempTransformFrames.Sort((ITransformCurve.Frame x, ITransformCurve.Frame y) => (int)Mathf.Sign(y.timelinePosition - x.timelinePosition));
                            while (tempTransformFrames.Count > 1 && tempTransformFrames[tempTransformFrames.Count - 2].timelinePosition > frameLength) tempTransformFrames.RemoveAt(tempTransformFrames.Count - 1);
                            tempTransformFrames[tempTransformFrames.Count - 1].timelinePosition = frameLength; 

                            curve.frames = tempTransformFrames.ToArray();
                             
                            transformLinearCurves[a] = curve;
                        }
                    }
                      
                    for (int a = 0; a < propertyLinearCurves.Count; a++)
                    {
                        var curve = propertyLinearCurves[a];
                        if (curve != null && curve.GetLengthInSeconds(rawAnimation.framesPerSecond) > timelineLength)
                        {
                            curve = curve.ShallowCopy();

                            tempPropertyFrames.Clear();
                            tempPropertyFrames.AddRange(curve.frames);
                            tempPropertyFrames.Sort((IPropertyCurve.Frame x, IPropertyCurve.Frame y) => (int)Mathf.Sign(y.timelinePosition - x.timelinePosition));
                            while (tempPropertyFrames.Count > 1 && tempPropertyFrames[tempPropertyFrames.Count - 2].timelinePosition > frameLength) tempPropertyFrames.RemoveAt(tempPropertyFrames.Count - 1);
                            tempPropertyFrames[tempPropertyFrames.Count - 1].timelinePosition = frameLength;

                            curve.frames = tempPropertyFrames.ToArray();

                            propertyLinearCurves[a] = curve;
                        }
                    }
                }

                #endregion

                //compiledAnimation.transformLinearCurves = transformLinearCurves;
                //compiledAnimation.transformCurves = transformCurves;
                //compiledAnimation.propertyLinearCurves = propertyLinearCurves;
                //compiledAnimation.propertyCurves = propertyCurves;

                compiledAnimation.transformLinearCurves = transformLinearCurves.ToArray();
                compiledAnimation.transformCurves = transformCurves.ToArray();
                compiledAnimation.propertyLinearCurves = propertyLinearCurves.ToArray();
                compiledAnimation.propertyCurves = propertyCurves.ToArray();

                compiledAnimation.transformAnimationCurves = transformCurveInfo.ToArray();
                compiledAnimation.propertyAnimationCurves = propertyCurveInfo.ToArray();

                transformLinearCurves.Clear();
                transformCurves.Clear();
                propertyLinearCurves.Clear();
                propertyCurves.Clear();

                transformCurveInfo.Clear();
                propertyCurveInfo.Clear();

                isCompiled = true;

                if (!isNew)
                {
                    try
                    {
                        if (previewAnimator != null) previewAnimator.OutputDependency.Complete();
                        compiledAnimation.FlushJobData();
                    }
                    catch (Exception e)
                    {
                        swole.LogError(e.Message);
                    }
                }

                PreviewAsset.Animation = compiledAnimation;
            }

            return compiledAnimation;
        }

        public bool ContainsCurve(AnimationCurve curve)
        {
            if (curve == null || rawAnimation == null) return false;

            if (ReferenceEquals(rawAnimation.timeCurve, curve)) return true;
            if (rawAnimation.transformCurves != null)
            {
                foreach (var ocurve in rawAnimation.transformCurves) 
                {
                    if (ocurve == null) continue;

                    if (ReferenceEquals(ocurve.localPositionTimeCurve, curve)) return true;
                    if (ReferenceEquals(ocurve.localPositionCurveX, curve)) return true;
                    if (ReferenceEquals(ocurve.localPositionCurveY, curve)) return true;
                    if (ReferenceEquals(ocurve.localPositionCurveZ, curve)) return true;

                    if (ReferenceEquals(ocurve.localRotationTimeCurve, curve)) return true;
                    if (ReferenceEquals(ocurve.localRotationCurveX, curve)) return true;
                    if (ReferenceEquals(ocurve.localRotationCurveY, curve)) return true;
                    if (ReferenceEquals(ocurve.localRotationCurveZ, curve)) return true;
                    if (ReferenceEquals(ocurve.localRotationCurveW, curve)) return true;

                    if (ReferenceEquals(ocurve.localScaleTimeCurve, curve)) return true;
                    if (ReferenceEquals(ocurve.localScaleCurveX, curve)) return true;
                    if (ReferenceEquals(ocurve.localScaleCurveY, curve)) return true;
                    if (ReferenceEquals(ocurve.localScaleCurveZ, curve)) return true;
                }
            }
            if (rawAnimation.propertyCurves != null)
            {
                foreach (var ocurve in rawAnimation.propertyCurves)
                {
                    if (ocurve == null) continue;

                    if (ReferenceEquals(ocurve.propertyValueCurve, curve)) return true;
                }
            }
            if (rawAnimation.transformLinearCurves != null)
            {
                foreach (var ocurve in rawAnimation.transformLinearCurves)
                {
                    if (ocurve == null || ocurve.frames == null) continue;
                    foreach (var frame in ocurve.frames) if (frame != null && ReferenceEquals(frame.interpolationCurve, curve)) return true;
                }
            }
            if (rawAnimation.propertyLinearCurves != null)
            {
                foreach (var ocurve in rawAnimation.propertyLinearCurves)
                {
                    if (ocurve == null || ocurve.frames == null) continue;
                    foreach (var frame in ocurve.frames) if (frame != null && ReferenceEquals(frame.interpolationCurve, curve)) return true;
                }
            }

            return false;
        }
        public bool ContainsCurve(ITransformCurve curve)
        {
            if (curve == null || rawAnimation == null) return false;

            if (rawAnimation.transformCurves != null)
            {
                foreach (var ocurve in rawAnimation.transformCurves) if (ReferenceEquals(curve, ocurve)) return true;
            }
            if (rawAnimation.transformLinearCurves != null)
            {
                foreach (var ocurve in rawAnimation.transformLinearCurves) if (ReferenceEquals(curve, ocurve)) return true;
            }

            return false;
        }
        public bool ContainsCurve(IPropertyCurve curve)
        {
            if (curve == null || rawAnimation == null) return false;

            if (rawAnimation.propertyCurves != null)
            {
                foreach (var ocurve in rawAnimation.propertyCurves) if (ReferenceEquals(curve, ocurve)) return true;
            }
            if (rawAnimation.propertyLinearCurves != null)
            {
                foreach (var ocurve in rawAnimation.propertyLinearCurves) if (ReferenceEquals(curve, ocurve)) return true;
            }

            return false;
        }

        public void InsertKeyframes(bool createFromPreviousKeyIfApplicable, bool useReferencePose, ImportedAnimatable animatable, float time, IntFromDecimalDelegate getFrameIndex = null, bool onlyInsertChangedData = true, List<Transform> transformMask = null, WrapMode preWrapMode = WrapMode.Clamp, WrapMode postWrapMode = WrapMode.Clamp, bool verbose=false)
        {
            if (animatable == null) return;
            if (animatable.RestPose == null) 
            {
                swole.LogWarning($"Tried to insert keyframes into source '{displayName}' using animatable '{animatable.displayName}', but the animatable has no rest pose!");
                return;
            }

            var originalPoseAtTime = animatable.RestPose.Duplicate();  

            if (rawAnimation != null)
            {
                var compAnim = CompiledAnimation;
                if (compAnim != null) originalPoseAtTime.ApplyAnimation(compAnim, time, false, 1, null, preWrapMode, postWrapMode); // Get the current pose at this point in the animation
            }
             
            var newPose = originalPoseAtTime;

            animatable.RefreshLastPose();
            if (animatable.lastPose != null) 
            {
                if (animatable.referencePose != null && useReferencePose)
                {
                    // Applies the pose change in an additive fashion to the current pose at time in the animation. Allows for correct pose changes even if other animations have been played on the rig (in theory)
                    var difference = animatable.referencePose.GetDifference(animatable.lastPose);
                    newPose = originalPoseAtTime.Duplicate().ApplyDeltas(difference); 
                    if (verbose) swole.Log($"Inserting key at [{time}] using difference between current pose and animator reference pose.");
                } 
                else 
                {
                    newPose = animatable.lastPose.Duplicate();
                    if (verbose) swole.Log($"Inserting key at [{time}] using current pose.");
                }
            }

            InsertKeyframes(createFromPreviousKeyIfApplicable, time, getFrameIndex, newPose, animatable.RestPose, onlyInsertChangedData ? originalPoseAtTime : null, transformMask, preWrapMode, postWrapMode, verbose);
        }
        public void InsertKeyframes(bool createFromPreviousKeyIfApplicable, float timelinePosition, IntFromDecimalDelegate getFrameIndex, AnimationUtils.Pose pose, AnimationUtils.Pose restPose, AnimationUtils.Pose originalPose = null, List<Transform> transformMask = null, WrapMode preWrapMode = WrapMode.Clamp, WrapMode postWrapMode = WrapMode.Clamp, bool verbose = false)
        {
            var anim = GetOrCreateRawData();
            if (anim != null)
            {
                if (restPose != null)
                {
                    if (createFromPreviousKeyIfApplicable && anim.HasKeyframes)
                    {
                        float prevTime = anim.GetClosestKeyframeTime(timelinePosition, false, getFrameIndex); // Find the time of the key that comes before timelinePosition
                        //Debug.Log(prevTime + " -> " + timelinePosition); 
                        var prevKeyPose = restPose.Duplicate().ApplyAnimation(anim, prevTime, false, 1, null, preWrapMode, postWrapMode); // Get the pose of the key at prevTime
                        var difference = prevKeyPose.GetDifference(pose); // Get the difference between the previous key and the new key
                        pose = prevKeyPose.ApplyDeltas(difference); // Apply the new pose on top of the previous so that the shortest rotation between the two will be used  
                    }
                }
                pose.Insert(anim, timelinePosition, restPose, originalPose, transformMask, getFrameIndex != null, getFrameIndex, verbose);
            }
            MarkAsDirty();
        }
    }

}

#endif