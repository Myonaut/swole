#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

using TMPro;

using Swole.UI;
using Swole.Animation;

#if BULKOUT_ENV
using RLD; // Paid Asset Integration https://assetstore.unity.com/packages/tools/modeling/runtime-level-design-52325
#endif

using static Swole.API.Unity.SwoleCurveEditor;
using static Swole.API.Unity.CustomEditorUtils;
using Swole.Script;

namespace Swole.API.Unity.Animation
{
    public class AnimationEditor : MonoBehaviour, IExecutableBehaviour
    {

        public virtual int Priority => CustomAnimatorUpdater.ExecutionPriority; // Same as animators
        public int CompareTo(IExecutableBehaviour other) => other == null ? 1 : Priority.CompareTo(other.Priority);

        #region Input

        public bool CursorIsInTimeline
        {
            get
            {
                return timelineWindow.ContainerTransform.ContainsWorldPosition(timelineWindow.ScreenPositionToWorldPosition(CursorProxy.ScreenPosition));
            }
        }
        public bool CursorIsOnTimeline
        {
            get
            {
                GameObject obj = CursorProxy.FirstObjectUnderCursor;
                return (obj == timelineWindow.gameObject || obj == timelineWindow.ContainerTransform.gameObject); 
            }
        }
        public bool CursorIsOverTimelineObject
        {
            get
            {
                GameObject obj = CursorProxy.FirstObjectUnderCursor;
                if (obj != null)
                {
                    Transform tr = obj.transform;
                    return tr.IsChildOf(timelineWindow.transform) || tr.IsChildOf(timelineWindow.ContainerTransform);
                }

                return false;
            }
        }

        protected void InputStep()
        {

            if (InputProxy.Modding_PrimeActionKey)
            {
                if (InputProxy.Modding_UndoKeyDown)
                {
                    if (InputProxy.Modding_ModifyActionKey)
                    {
                        Redo();
                    } 
                    else
                    {
                        Undo();
                    }
                } 
            }

            if (InputProxy.Modding_SelectAllKeyDown)
            {
                if (CursorIsInTimeline) 
                {
                    if (SelectedKeyframeCount > 0) DeselectAllKeyframes(); else SelectAllKeyframes();
                }
            }
            if (InputProxy.CursorPrimaryButtonDown)
            {
                if (CursorIsOnTimeline) DeselectAllKeyframes();
            }
            if (InputProxy.Modding_DeleteKeyDown)
            {
                if (CursorIsOverTimelineObject) DeleteSelectedKeyframes();    
            }

            if (InputProxy.Modding_PrimeActionKey)
            {
                if (InputProxy.Modding_CopyKeyDown)
                {
                    if (CursorIsOverTimelineObject) CopyKeyframes();
                } 
                else if (InputProxy.Modding_PasteKeyDown && timelineWindow != null) 
                {
                    if (CursorIsOverTimelineObject) PasteKeyframes(timelineWindow.ScrubFrame); 
                }
            }
        }

        #endregion

        #region Undo System

        protected readonly ActionBasedUndoSystem undoSystem = new ActionBasedUndoSystem();

        public void Undo() => undoSystem.Undo(); 
        public void Redo() => undoSystem.Redo();

        public struct UndoableImportAnimatable : IRevertableAction
        {
            public AnimationEditor editor;
            public ImportedAnimatable animatable;
            public int importIndex;

            public UndoableImportAnimatable(AnimationEditor editor, int importIndex, ImportedAnimatable animatable)
            {
                this.editor = editor;
                this.importIndex = importIndex;
                this.animatable = animatable;
                undoState = false;
            }

            public void Reapply()
            {
                if (editor == null) return;
                editor.AddAnimatable(animatable, importIndex);
            }

            public void Revert()
            {
                if (editor == null) return;
                editor.RemoveAnimatable(animatable, false); 
            }

            public void Perpetuate() { }
            public void PerpetuateUndo() 
            {
                if (editor == null || animatable == null) return;
                animatable.index = -1;
                if (!editor.RemoveAnimatable(animatable, true)) animatable.Destroy(); 
            }

            public bool undoState;
            public bool GetUndoState() => undoState;
            public IRevertableAction SetUndoState(bool undone)
            {
                var newState = this;
                newState.undoState = undone;
                return newState;
            }
        }
        public struct UndoableRemoveAnimatable : IRevertableAction
        {
            public AnimationEditor editor;
            public ImportedAnimatable animatable;
            public int index;

            public UndoableRemoveAnimatable(AnimationEditor editor, int index, ImportedAnimatable animatable)
            {
                this.editor = editor;
                this.index = index;
                this.animatable = animatable;
                undoState = new VarRef<bool>(false);
            }

            public void Perpetuate()
            {
                if (editor == null || animatable == null) return;
                animatable.index = -1;
                if (!editor.RemoveAnimatable(animatable, true)) animatable.Destroy();  
            }
            public void PerpetuateUndo()
            {
            }

            public void Reapply()
            {
                if (editor == null || animatable == null) return;
                animatable.index = -1;
                editor.RemoveAnimatable(animatable, false);
            }

            public void Revert()
            {
                if (editor == null || animatable == null) return;
                editor.AddAnimatable(animatable, index);
            }

            public bool undoState;
            public bool GetUndoState() => undoState;

            public IRevertableAction SetUndoState(bool undone)
            {
                var newState = this;
                newState.undoState = undone;
                return newState;
            }
        }

        public struct UndoableCreateNewAnimationSource : IRevertableAction
        {

            public AnimationEditor editor;
            public int animatableIndex, sourceindex;
            public AnimationSource source;

            public UndoableCreateNewAnimationSource(AnimationEditor editor, int animatableIndex, int sourceindex, AnimationSource source)
            {
                this.editor = editor;
                this.animatableIndex = animatableIndex;
                this.sourceindex = sourceindex;
                this.source = source;

                undoState = false;
            }

            public void Reapply()
            {

            }

            public void Revert()
            {

            }

            public void Perpetuate() { }
            public void PerpetuateUndo() { }

            public bool undoState;
            public bool GetUndoState() => undoState;
            public IRevertableAction SetUndoState(bool undone)
            {
                var newState = this;
                newState.undoState = undone;
                return newState;
            }
        }

        public struct UndoableStartEditingCurve : IRevertableAction
        {
            public AnimationEditor editor;
            public AnimationCurve curve;

            public UndoableStartEditingCurve(AnimationEditor editor, AnimationCurve curve)
            {
                this.editor = editor;
                this.curve = curve;

                undoState = false;
            }

            public void Reapply()
            {
                if (editor == null) return;
                editor.StartEditingCurve(curve, false);
            }

            public void Revert()
            {
                if (editor == null) return;
                editor.StopEditingCurve();
            }

            public void Perpetuate() { }
            public void PerpetuateUndo() { }

            public bool undoState;
            public bool GetUndoState() => undoState;
            public IRevertableAction SetUndoState(bool undone)
            {
                var newState = this;
                newState.undoState = undone;
                return newState;
            }
        }
        public struct UndoableStopEditingCurve : IRevertableAction
        {
            public AnimationEditor editor;
            public AnimationCurve curve;

            public UndoableStopEditingCurve(AnimationEditor editor, AnimationCurve curve)
            {
                this.editor = editor;
                this.curve = curve;

                undoState = false;
            }

            public void Reapply()
            {
                if (editor == null) return;
                editor.StopEditingCurve();
            }

            public void Revert()
            {
                if (editor == null) return;
                editor.StartEditingCurve(curve, false);
            }

            public void Perpetuate() { }
            public void PerpetuateUndo() { }

            public bool undoState;
            public bool GetUndoState() => undoState;
            public IRevertableAction SetUndoState(bool undone)
            {
                var newState = this;
                newState.undoState = undone;
                return newState;
            }
        }

        #endregion

    [Serializable]
        public enum EditMode
        {
            GLOBAL_TIME, GLOBAL_KEYS, BONE_KEYS, BONE_CURVES, PROPERTY_CURVES, ANIMATION_EVENTS
        }

        [Header("Runtime"), SerializeField]
        protected EditMode editMode;
        public void SetEditMode(EditMode mode)
        {
            editMode = mode;
            switch (editMode)
            {
                case EditMode.GLOBAL_TIME:
                    if (timelineWindow != null) timelineWindow.SetRenderFrameMarkers(true);
                    if (boneDropdownListRoot != null) boneDropdownListRoot.gameObject.SetActive(false);
                    if (boneGroupDropdownListRoot != null) boneGroupDropdownListRoot.gameObject.SetActive(false);
                    if (boneCurveDropdownListRoot != null) boneCurveDropdownListRoot.gameObject.SetActive(false);
                    if (propertyDropdownListRoot != null) propertyDropdownListRoot.gameObject.SetActive(false);
                    break;

                case EditMode.GLOBAL_KEYS:
                    if (timelineWindow != null) timelineWindow.SetRenderFrameMarkers(true);
                    if (boneDropdownListRoot != null) boneDropdownListRoot.gameObject.SetActive(false);
                    if (boneGroupDropdownListRoot != null) boneGroupDropdownListRoot.gameObject.SetActive(false);
                    if (boneCurveDropdownListRoot != null) boneCurveDropdownListRoot.gameObject.SetActive(false);
                    if (propertyDropdownListRoot != null) propertyDropdownListRoot.gameObject.SetActive(false);
                    break;

                case EditMode.BONE_KEYS:
                    if (timelineWindow != null) timelineWindow.SetRenderFrameMarkers(true);
                    if (boneDropdownListRoot != null) boneDropdownListRoot.gameObject.SetActive(true);
                    if (boneGroupDropdownListRoot != null) boneGroupDropdownListRoot.gameObject.SetActive(true);
                    if (boneCurveDropdownListRoot != null) boneCurveDropdownListRoot.gameObject.SetActive(false);
                    if (propertyDropdownListRoot != null) propertyDropdownListRoot.gameObject.SetActive(false);
                    break;

                case EditMode.BONE_CURVES:
                    if (timelineWindow != null) timelineWindow.SetRenderFrameMarkers(true);
                    if (boneDropdownListRoot != null) boneDropdownListRoot.gameObject.SetActive(true);
                    if (boneGroupDropdownListRoot != null) boneGroupDropdownListRoot.gameObject.SetActive(false);
                    if (boneCurveDropdownListRoot != null) boneCurveDropdownListRoot.gameObject.SetActive(true);
                    if (propertyDropdownListRoot != null) propertyDropdownListRoot.gameObject.SetActive(false);
                    break;

                case EditMode.PROPERTY_CURVES:
                    if (timelineWindow != null) timelineWindow.SetRenderFrameMarkers(true);
                    if (boneDropdownListRoot != null) boneDropdownListRoot.gameObject.SetActive(false);
                    if (boneGroupDropdownListRoot != null) boneGroupDropdownListRoot.gameObject.SetActive(false);
                    if (boneCurveDropdownListRoot != null) boneCurveDropdownListRoot.gameObject.SetActive(false);
                    if (propertyDropdownListRoot != null) propertyDropdownListRoot.gameObject.SetActive(true);
                    break;

                case EditMode.ANIMATION_EVENTS:
                    if (timelineWindow != null) timelineWindow.SetRenderFrameMarkers(true);
                    if (boneDropdownListRoot != null) boneDropdownListRoot.gameObject.SetActive(false);
                    if (boneGroupDropdownListRoot != null) boneGroupDropdownListRoot.gameObject.SetActive(false);
                    if (boneCurveDropdownListRoot != null) boneCurveDropdownListRoot.gameObject.SetActive(false);
                    if (propertyDropdownListRoot != null) propertyDropdownListRoot.gameObject.SetActive(false);
                    break;
            }

            RefreshTimeline();
        }
        public void SetEditMode(int mode) => SetEditMode((EditMode)mode);

#if BULKOUT_ENV
        [Header("Editor Setup")]
        public string additiveEditorSetupScene = "sc_RLD-Add"; // Paid Asset Integration https://assetstore.unity.com/packages/tools/modeling/runtime-level-design-52325
#else
        [Header("Editor Setup")]
        public string additiveEditorSetupScene = "";
#endif
        [Tooltip("Primary object used for manipulating the scene.")] 
        public RuntimeEditor runtimeEditor;

        [Tooltip("The UI element used to display the list of imported animatables and their animations.")]
        public UICategorizedList importedAnimatablesList;

        [Header("Windows")]
        public AnimationTimeline timelineWindow;
        public RectTransform animatableImporterWindow;
        public RectTransform newAnimationWindow;
        public RectTransform compilationWindow;
        public RectTransform curveEditorWindow;
        protected AnimationCurveEditor curveEditor;
        public AnimationCurveEditor CurveEditor
        {
            get
            {
                if (curveEditor == null && curveEditorWindow != null) curveEditor = curveEditorWindow.GetComponentInChildren<AnimationCurveEditor>(true);
                return curveEditor;
            }
        }
        public RectTransform physiqueEditorWindow;
        protected PhysiqueEditorWindow physiqueEditor;
        public PhysiqueEditorWindow PhysiqueEditor
        {
            get
            {
                if (physiqueEditor == null && physiqueEditorWindow != null) physiqueEditor = physiqueEditorWindow.GetComponentInChildren<PhysiqueEditorWindow>(true);
                return physiqueEditor;
            }
        }
        public void OpenPhysiqueEditor()
        {
            if (physiqueEditorWindow == null) return;
            var activeObj = ActiveAnimatable;
            if (activeObj == null) return;

            physiqueEditorWindow.gameObject.SetActive(true);
            physiqueEditorWindow.SetAsLastSibling();

            var character = activeObj.instance.GetComponentInChildren<MuscularRenderedCharacter>();
            if (character == null)
            {
                physiqueEditorWindow.gameObject.SetActive(false);
            }
            else
            {
                var editor = PhysiqueEditor;
                if (editor != null) editor.Character = character;              
            }

        }

        public RectTransform confirmationMessageWindow;
        protected const string _messageTag = "Message";
        protected const string _confirmButtonTag = "Confirm";
        protected const string _cancelButtonTag = "Cancel";
        protected const string _closeButtonTag = "Close";
        public void ShowConfirmationMessage(string message, VoidParameterlessDelegate onConfirm, VoidParameterlessDelegate onCancel = null)
        {
            if (confirmationMessageWindow == null) return;

            void Confirm()
            {
                confirmationMessageWindow.gameObject.SetActive(false);
                onConfirm?.Invoke();
            }

            confirmationMessageWindow.gameObject.SetActive(true);

            CustomEditorUtils.SetComponentTextByName(confirmationMessageWindow, _messageTag, message);

            CustomEditorUtils.SetButtonOnClickActionByName(confirmationMessageWindow, _confirmButtonTag, Confirm);
            CustomEditorUtils.SetButtonOnClickActionByName(confirmationMessageWindow, _cancelButtonTag, () => onCancel?.Invoke());
            CustomEditorUtils.SetButtonOnClickActionByName(confirmationMessageWindow, _closeButtonTag, () => onCancel?.Invoke(), false, true, false); 
        }

        #region Package Linking

        public RectTransform packageLinkWindow;
        public Color colorUnbound = Color.red;
        public Color colorNotSynced = Color.yellow;
        public Color colorOutdated = Color.cyan;
        public Color colorSynced = Color.green;

        protected const string _sourceNameTag = "Name";
        protected const string _linkIconTag = "LinkIcon";
        protected const string _infoTag = "Info";
        protected const string _bindButtonTag = "BindButton";
        protected const string _pushButtonTag = "PushButton";
        protected const string _pullButtonTag = "PullButton";

        protected const string _unboundText = "UNBOUND";
        protected const string _localIsNewerText = "UNSYNCED CHANGES";
        protected const string _externalIsNewerText = "LINKED ASSET IS NEWER";
        protected const string _syncedText = "UP TO DATE";
        public void EvaluateAnimationAssetLink(AnimationSource target, CustomAnimation linkedAnim, VoidParameterlessDelegate onSynced, VoidParameterlessDelegate onLocalNewer, VoidParameterlessDelegate onLinkedNewer)
        {
            if (target.rawAnimation == null)
            {
                onLinkedNewer?.Invoke(); 
            }
            else
            {
                int dateComparison = linkedAnim.LastEditDate().CompareTo(target.rawAnimation.LastEditDate());
                if (target.IsDirty)
                {
                    if (dateComparison < 0)
                    {
                        onLocalNewer?.Invoke();
                    }
                    else if (dateComparison > 0)
                    {
                        onLinkedNewer?.Invoke();
                    }
                    else
                    {
                        onLocalNewer?.Invoke();
                    }
                }
                else
                {
                    if (dateComparison > 0)
                    {
                        onLinkedNewer?.Invoke();
                    }
                    else
                    {
                        onSynced?.Invoke();
                    }
                }
            }
        }

        protected VoidParameterlessDelegate syncPackageLinkWindow;
        public void SyncPackageLinkWindow()
        {
            if (syncPackageLinkWindow != null)
            {
                if (packageLinkWindow == null || !packageLinkWindow.gameObject.activeInHierarchy)
                {
                    syncPackageLinkWindow = null;
                } 
                else
                {
                    syncPackageLinkWindow();
                }
            }
        }
        public void RefreshPackageLinksUI()
        {
            SyncPackageLinkWindow();
            RefreshAnimatableListUI(); 
        }
        public void OpenPackageLinkWindow(AnimationSource target)
        {
            if (target == null || packageLinkWindow == null) return; 

            packageLinkWindow.gameObject.SetActive(true);
            packageLinkWindow.SetAsLastSibling();
            CustomEditorUtils.SetInputFieldOnValueChangeAction(packageLinkWindow, null);

            var rT_name = packageLinkWindow.FindDeepChildLiberal(_sourceNameTag);
            var rT_icon = packageLinkWindow.FindDeepChildLiberal(_linkIconTag);

            var rT_info = packageLinkWindow.FindDeepChildLiberal(_infoTag);

            var rT_bindButton = packageLinkWindow.FindDeepChildLiberal(_bindButtonTag);
            var rT_pushButton = packageLinkWindow.FindDeepChildLiberal(_pushButtonTag);
            var rT_pullButton = packageLinkWindow.FindDeepChildLiberal(_pullButtonTag);

            var errorMessage = packageLinkWindow.GetComponentInChildren<UIPopupMessageFadable>(true);
            void ShowError(string message)
            {
                if (errorMessage == null) return;
                errorMessage.SetMessage(message).SetDisplayTime(errorMessage.DefaultDisplayTime).Show(); 
            }

            exportTarget = target;

            VarRef<bool> tempFlag = new VarRef<bool>(true);
            void SetPushPullMode(ContentManager.LocalPackage localPackage)
            {
                tempFlag.value = true;
                CustomEditorUtils.SetInputFieldText(packageLinkWindow, localPackage.GetIdentityString());
                CustomEditorUtils.SetInputFieldOnValueChangeAction(packageLinkWindow, (string value) =>
                {
                    if (ContentManager.TryFindLocalPackage(value, out ContentManager.LocalPackage localPackage) && localPackage.GetIdentity().Equals(target.package))
                    {
                        if (!tempFlag) 
                        {
                            target.package = localPackage.GetIdentity(); 
                            SetPushPullMode(localPackage); 
                        }
                    } 
                    else if (tempFlag)
                    {
                        tempFlag.value = false;
                        SetBindMode();
                    }
                });

                void IsSynced()
                {
                    if (rT_info != null) CustomEditorUtils.SetComponentTextAndColor(rT_info, _syncedText, colorSynced);
                    if (rT_name != null) CustomEditorUtils.SetComponentTextAndColor(rT_name, target.DisplayName, colorSynced);
                    if (rT_icon != null)
                    {
                        var img = rT_icon.GetComponentInChildren<Image>();
                        if (img != null) img.color = colorSynced;
                    }
                }
                void IsNotSynced()
                {
                    if (rT_info != null) CustomEditorUtils.SetComponentTextAndColor(rT_info, _localIsNewerText, colorNotSynced);
                    if (rT_name != null) CustomEditorUtils.SetComponentTextAndColor(rT_name, target.DisplayName, colorNotSynced);
                    if (rT_icon != null)
                    {
                        var img = rT_icon.GetComponentInChildren<Image>();
                        if (img != null) img.color = colorNotSynced;
                    }
                }
                void IsOutdated()
                {
                    if (rT_info != null) CustomEditorUtils.SetComponentTextAndColor(rT_info, _externalIsNewerText, colorOutdated);
                    if (rT_name != null) CustomEditorUtils.SetComponentTextAndColor(rT_name, target.DisplayName, colorOutdated);
                    if (rT_icon != null)
                    {
                        var img = rT_icon.GetComponentInChildren<Image>();
                        if (img != null) img.color = colorOutdated;
                    }
                }
                void CheckSync()
                {
                    if (target == null || !target.IsValid)
                    {
                        if (packageLinkWindow != null) packageLinkWindow.gameObject.SetActive(false);
                        return;
                    }

                    if (!ContentManager.TryFindLocalPackage(target.package, out ContentManager.LocalPackage localPackage)) // Checks for null
                    {
                        CustomEditorUtils.SetInputFieldText(packageLinkWindow, string.Empty); 
                        SetBindMode(); 
                        return; 
                    }

                    if (localPackage.Content.TryFind<CustomAnimation>(out var linkedAnim, target.syncedName))
                    {
                        EvaluateAnimationAssetLink(target, linkedAnim, IsSynced, IsNotSynced, IsOutdated);
                        if (rT_pullButton != null) CustomEditorUtils.SetButtonInteractable(rT_pullButton, true);
                    }
                    else
                    {
                        IsNotSynced();
                        if (rT_pullButton != null) CustomEditorUtils.SetButtonInteractable(rT_pullButton, false);
                    }
                }
                syncPackageLinkWindow = CheckSync;
                CheckSync();

                if (rT_bindButton != null) rT_bindButton.gameObject.SetActive(false);
                if (rT_pushButton != null) 
                {
                    rT_pushButton.gameObject.SetActive(true);
                    CustomEditorUtils.SetButtonOnClickAction(rT_pushButton, () =>
                    {
                        PushTargetToPackage(target);
                    });
                }
                if (rT_pullButton != null) 
                {
                    rT_pullButton.gameObject.SetActive(true);
                    CustomEditorUtils.SetButtonOnClickAction(rT_pullButton, () =>
                    {
                        void DoIt()
                        {
                            if (PullTargetFromPackage(target))
                            {
                                IsSynced();
                                RefreshTimeline();
                            }
                        }
                        if (target.IsDirty)
                        {
                            ShowConfirmationMessage($"This pull will overwrite your current changes to '{target.DisplayName}' — are you sure?", DoIt);
                        }
                        else 
                        { 
                            DoIt(); 
                        }
                    });
                    CustomEditorUtils.SetButtonInteractable(rT_pullButton, localPackage.Content.Contains(target.syncedName, typeof(CustomAnimation)));
                }
            }
            void SetBindMode()
            {
                if (rT_info != null) CustomEditorUtils.SetComponentTextAndColor(rT_info, _unboundText, colorUnbound);
                if (rT_name != null) CustomEditorUtils.SetComponentTextAndColor(rT_name, target.DisplayName, colorUnbound);
                if (rT_icon != null)
                {
                    var img = rT_icon.GetComponentInChildren<Image>();
                    if (img != null) img.color = colorUnbound;
                }

                if (rT_bindButton != null)
                {
                    rT_bindButton.gameObject.SetActive(true);
                    CustomEditorUtils.SetButtonOnClickAction(rT_bindButton, () =>
                    {
                        string targetPackageID = CustomEditorUtils.GetInputFieldText(packageLinkWindow);
                        var temp = new PackageIdentifier(targetPackageID);
                        Debug.Log(temp.name + " : " + temp.version);
                        var result = BindTargetToPackage(target, new PackageIdentifier(targetPackageID), out ContentManager.LocalPackage package);
                        RefreshAnimatableListUI(); 
                        if (result)
                        {
                            SetPushPullMode(package);
                        } 
                        else
                        {
                            ShowError($"Failed to bind to package '{targetPackageID}'");        
                        }
                    });
                }

                if (rT_pushButton != null) rT_pushButton.gameObject.SetActive(false);
                if (rT_pullButton != null) rT_pullButton.gameObject.SetActive(false);
            }

            if (ContentManager.TryFindLocalPackage(target.package, out ContentManager.LocalPackage localPackage))
            {
                SetPushPullMode(localPackage);
            } 
            else
            {
                CustomEditorUtils.SetInputFieldText(packageLinkWindow, string.Empty);
                SetBindMode(); 
            }
        }

        public void CreateNewProject(GameObject newProjectWindow)
        {

        }

        protected AnimationSource exportTarget;
        protected PackageIdentifier packageTarget;

        public void BindTargetToPackage() => BindTargetToPackage(exportTarget, packageTarget);
        public bool BindTargetToPackage(AnimationSource exportTarget, PackageIdentifier packageTarget) => BindTargetToPackage(exportTarget, packageTarget, out _);
        public bool BindTargetToPackage(AnimationSource exportTarget, PackageIdentifier packageTarget, out ContentManager.LocalPackage localPackage)
        {
            localPackage = default;
            if (exportTarget == null) return false;

            this.exportTarget = exportTarget;
            this.packageTarget = packageTarget;

            return exportTarget.BindToPackage(packageTarget, out localPackage);
        }

        public void PushTargetToPackage() => PushTargetToPackage(exportTarget);
        public bool PushTargetToPackage(AnimationSource target)
        {
            try
            {
                this.exportTarget = target;
                if (target.PushToPackage())
                {
                    RefreshPackageLinksUI(); 
                    return true;
                }
            }
            catch (Exception ex)
            {
                swole.LogError($"Encountered exception while attempting to push content to package '{target.package}'");
                swole.LogError(ex);
            }

            return false;
        }
        public void PullTargetFromPackage() => PullTargetFromPackage(exportTarget);
        public bool PullTargetFromPackage(AnimationSource target)
        {
            try
            {
                this.exportTarget = target;
                if (target.PullFromPackage())
                {
                    RefreshPackageLinksUI();
                    return true;
                }
            }
            catch (Exception ex)
            {
                swole.LogError($"Encountered exception while attempting to pull content from package '{target.package}'");
                swole.LogError(ex);
            }

            return false;
        }

        #endregion

        #region Save/Load Session Window

        public static string AnimationEditorSessionDirectoryPath => Path.Combine(swole.AssetDirectory.FullName, ContentManager.folderNames_Editors, ContentManager.folderNames_AnimationEditor, ContentManager.folderNames_Sessions);

        protected const string _saveTag = "Save";

        public RectTransform saveSessionWindow;
        public static bool CheckIfSessionFileExists(string sessionName)
        {
            string path = AnimationEditorSessionDirectoryPath;
            if (Directory.Exists(path))
            {
                if (File.Exists(Path.Combine(path, $"{sessionName}.{ContentManager.fileExtension_Generic}"))) return true;  
            }
            return false;
        }
        public static bool SaveSessionInDefaultDirectory(Session session)
        {
            if (session == null) return false;

            try
            {
                var dir = Directory.CreateDirectory(AnimationEditorSessionDirectoryPath);
                if (SaveSession(dir, session, swole.DefaultLogger))
                {
                    session.isDirty = false;
                    return true;
                }
            } 
            catch(Exception ex)
            {
                swole.LogError($"Encountered exception while trying to save session '{session.name}' to default directory");
                swole.LogError(ex);
            }
            return false;
        }
        public void SaveCurrentSessionInDefaultDirectory() => SaveSessionInDefaultDirectory(currentSession);
        public void OpenSessionSaveWindow() => OpenSessionSaveWindow(saveSessionWindow);
        public void OpenSessionSaveWindow(RectTransform window)
        {
            if (window == null) return;

            window.gameObject.SetActive(true);
            window.SetAsLastSibling();

            SyncSessionSaveWindow(window);

            CustomEditorUtils.SetButtonOnClickActionByName(window, _saveTag, () =>
            {
                var session = currentSession;
                if (session != null)
                {
                    string sessionName = CustomEditorUtils.GetInputFieldText(window);
                    if (string.IsNullOrWhiteSpace(sessionName)) sessionName = session.name;  

                    void Save()
                    {
                        session.name = sessionName;
                        SaveSessionInDefaultDirectory(session);
                        SyncSessionSaveWindow();
                    }

                    if (session.name != sessionName && CheckIfSessionFileExists(sessionName))
                    {
                        ShowConfirmationMessage($"A session with the name '{sessionName}' already exists. Are you sure you want to overwrite it?", Save);
                    }
                    else Save();
                }
            });
        }
        protected void SyncSessionSaveWindow() => SyncSessionSaveWindow(saveSessionWindow);
        protected void SyncSessionSaveWindow(RectTransform window)
        {
            CustomEditorUtils.SetInputFieldText(window, currentSession == null ? "null session" : currentSession.name);
            CustomEditorUtils.SetComponentTextAndColorByName(window, _infoTag, currentSession == null ? "NULL" : (currentSession.isDirty ? "UNSAVED CHANGES" : "SAVED"), currentSession == null ? colorUnbound : (currentSession.isDirty ? colorNotSynced : colorSynced));

            CustomEditorUtils.SetButtonInteractableByName(window, _saveTag, currentSession != null); 
        }

        public RectTransform loadSessionWindow;
        public void OpenSessionLoadWindow() => OpenSessionLoadWindow(loadSessionWindow);
        protected static readonly List<FileInfo> _tempFiles = new List<FileInfo>(); 
        public void OpenSessionLoadWindow(RectTransform window)
        {
            if (window == null) return;

            window.gameObject.SetActive(true);
            window.SetAsLastSibling();

            try
            {
                var list = window.GetComponentInChildren<UIRecyclingList>();
                if (list == null) return;

                list.Clear();

                var dir = Directory.CreateDirectory(AnimationEditorSessionDirectoryPath);
                if (dir != null)
                {
                    var files = dir.GetFiles($"*.{ContentManager.fileExtension_Generic}", SearchOption.TopDirectoryOnly);
                    _tempFiles.Clear();
                    _tempFiles.AddRange(files);
                    _tempFiles.Sort((FileInfo x, FileInfo y) => y.LastWriteTime.CompareTo(x.LastWriteTime));
                     
                    foreach(var file in _tempFiles)
                    {
                        var temp = list.AddNewMember(Path.GetFileNameWithoutExtension(file.FullName), () =>
                        {
                            void Load()
                            {
                                var session = LoadSessionFromFile(file, swole.DefaultLogger);
                                if (session != null) 
                                { 
                                    LoadSession(session);
                                    window.gameObject.SetActive(false);
                                } 
                                else
                                {
                                    var errorMsg = window.GetComponentInChildren<UIPopupMessageFadable>();
                                    if (errorMsg != null) errorMsg.SetMessage("Failed to load session.").SetDisplayTime(errorMsg.DefaultDisplayTime).Show();
                                }
                            }

                            if (currentSession != null && currentSession.isDirty)
                            {
                                ShowConfirmationMessage("Current session has unsaved changes that will be lost. Are you sure?", Load);
                            }
                            else Load();
                        });
                    }
                    _tempFiles.Clear();
                }

                list.Refresh();
            }
            catch (Exception ex)
            {
                swole.LogError($"Encountered exception while trying to open load session window");
                swole.LogError(ex);
            }
        }

        #endregion

        #region Animation Events

        protected const string str_name = "name";
        protected const string str_frame = "frame";
        protected const string str_edit = "edit";
        protected const string str_up = "up";
        protected const string str_down = "down";
        protected const string str_delete = "delete";
        protected const string str_addEvent = "addEvent";
        protected const string str_addEventToFrame = "addEventToFrame";
        private static string GetFrameHeader(int frameIndex) => $"{str_frame} {frameIndex}".ToUpper();
        public RectTransform animationEventsWindow;
        private static int FrameListSort(UIRecyclingList.MemberData memA, UIRecyclingList.MemberData memB)
        {
            int frameA = -1;
            int frameB = -1;
            if (memA.storage is int) frameA = (int)memA.storage;
            if (memB.storage is int) frameB = (int)memB.storage; 

            return (int)Mathf.Sign(frameA - frameB);
        }
        protected void RefreshAnimationEventKeyframes()
        {
            if (editMode != EditMode.ANIMATION_EVENTS) return; 
            RefreshKeyframes();
        }
        public void OpenAnimationEventsWindow() => OpenAnimationEventsWindow(animationEventsWindow);
        public void OpenAnimationEventsWindow(RectTransform window)
        {
            if (window == null) return;
            window.gameObject.SetActive(true);
            window.SetAsLastSibling(); 

            RefreshAnimationEventsWindow(window, CurrentSource);
        }

        public RectTransform animationEventEditWindow;
        public void OpenAnimationEventEditWindow(AnimationSource source, CustomAnimation.Event _event, UIRecyclingList list = null, UIRecyclingList.MemberData listMember = default, UIRecyclingList parentList = null) => OpenAnimationEventEditWindow(animationEventEditWindow, source, _event, list, listMember, parentList);
        public void OpenAnimationEventEditWindow(RectTransform window, AnimationSource source, CustomAnimation.Event _event, UIRecyclingList list = null, UIRecyclingList.MemberData listMember = default, UIRecyclingList parentList = null)
        {
            if (window == null) return;
            window.gameObject.SetActive(true);
            window.SetAsLastSibling();

            RefreshAnimationEventEditWindow(window, source, _event, list, listMember, parentList);
        }

        public void RefreshAnimationEventsWindow()
        {
            RefreshAnimationEventsWindow(animationEventsWindow, CurrentSource); 
        }
        private readonly List<CustomAnimation.Event> _orderedEvents = new List<CustomAnimation.Event>();
        private static readonly List<CustomAnimation.Event> _tempEvents = new List<CustomAnimation.Event>();
        private static UIRecyclingList.MemberData GetFrameEventList(UIRecyclingList list, int frameIndex, out bool isNew)
        {
            isNew = false;

            string frameHeader = GetFrameHeader(frameIndex);
            var mem = list.FindMember(frameHeader);
            if (mem.id == null)
            {
                mem.name = frameHeader;
                mem.storage = frameIndex;
                mem.id = list.AddOrUpdateMember(mem, false);
                list.Sort(FrameListSort);
                isNew = true;
            }

            return mem;
        }
        public void RefreshAnimationEventsWindow(RectTransform window, AnimationSource source)
        {
            if (window == null || !window.gameObject.activeInHierarchy) return;  
            var mainList = window.gameObject.GetComponentInChildren<UIRecyclingList>(); 
            if (mainList == null) return;
            mainList.autoRefreshChildLists = false;
            mainList.membersAreNotButtons = true; 

            CustomAnimation.Event[] eventArray = source == null ? null : source.GetOrCreateRawData().events; // for checking if events have been altered
            CustomAnimation.Event NewEventAtIndex(int frameIndex)
            {
                var anim = source.GetOrCreateRawData();
                var _events = new CustomAnimation.Event[anim.events == null ? 1 : anim.events.Length + 1];
                if (anim.events != null) anim.events.CopyTo(_events, 0);
                var _event = new CustomAnimation.Event("new_event_name", (float)AnimationTimeline.FrameToTimelinePosition(frameIndex, source.rawAnimation.framesPerSecond), 0, null);
                _event.Priority = 999999; // Forces the event to the end of the frame event list it gets added to (value will get replaced after by its index in the list)
                _events[_events.Length - 1] = _event; 
                anim.events = _events;

                source.MarkAsDirty();

                RefreshAnimationEventKeyframes();

                return _event; 
            }

            int prevStartIndex = -3;
            int prevEndIndex = -3;
            bool validRefresh = false;
            void Refreshed()
            {
                if ((mainList.VisibleRangeStart == prevStartIndex && mainList.VisibleRangeEnd == prevEndIndex) && !mainList.IsDirty && (source == null || source.rawAnimation == null || ReferenceEquals(eventArray, source.rawAnimation.events))) return;

                validRefresh = true;
                prevStartIndex = mainList.VisibleRangeStart;
                prevEndIndex = mainList.VisibleRangeEnd; 

                mainList.Clear();
                if (source != null && source.rawAnimation != null && source.rawAnimation.events != null)
                {
                    _orderedEvents.Clear();
                    eventArray = source.rawAnimation.events; 
                    _orderedEvents.AddRange(eventArray);
                    _orderedEvents.Sort(CustomAnimation.SortEventsAscending);   
                    foreach (var _event in _orderedEvents)
                    {
                        var frameEventListData = GetFrameEventList(mainList, AnimationTimeline.CalculateFrameAtTimelinePosition((decimal)_event.TimelinePosition, source.rawAnimation.framesPerSecond), out bool isNewList);

                        if (isNewList)
                        {
                            void OnRefreshEventList(UIRecyclingList.MemberData data, GameObject inst) // Called by the parent list per member when the parent list is refreshed
                            {
                                if (!validRefresh) return;
                                if (data.storage is not int frameIndex) 
                                {
                                    swole.LogError($"[{nameof(AnimationEditor)}] Something went wrong with frameIndex storage"); 
                                    return; 
                                }

                                var frameEventList = inst.GetComponentInChildren<UIRecyclingList>(true);
                                if (frameEventList == null) return;

                                frameEventList.Clear();

                                void OnRefreshEvent(UIRecyclingList.MemberData data, GameObject inst) // Called by the frame list per member when that frame list is refreshed
                                {
                                    if (source.rawAnimation == null || data.storage is not CustomAnimation.Event _event) return;

                                    void RefreshEventName(string name)
                                    {
                                        _event.Name = data.name = name;
                                        data.id = frameEventList.AddOrUpdateMemberWithStorageComparison(data, false);

                                        source?.MarkAsDirty();
                                    }

                                    CustomEditorUtils.SetInputFieldTextByName(inst, str_name, _event.Name);
                                    CustomEditorUtils.SetInputFieldOnValueChangeActionByName(inst, str_name, RefreshEventName);

                                    void StartEditing() => OpenAnimationEventEditWindow(animationEventEditWindow, source, _event, frameEventList, data, mainList);
                                    CustomEditorUtils.SetButtonOnClickActionByName(inst, str_edit, StartEditing);

                                    void MoveUp()
                                    {
                                        int pos = data.id.index;
                                        int swapPos = pos + 1;

                                        var swapData = frameEventList.GetMember(swapPos);
                                        if (swapData.id != null)
                                        {
                                            data.id.index = swapPos;
                                            data.id = frameEventList.AddOrUpdateMemberWithStorageComparison(data, false);
                                            data.id.index = swapPos; // do it again incase id reference changed
                                            data.id = frameEventList.AddOrUpdateMemberWithStorageComparison(data, false);

                                            swapData.id.index = pos;
                                            frameEventList.AddOrUpdateMember(swapData, true);
                                            _event.Priority = swapPos; 
                                        }

                                        source?.MarkAsDirty();
                                    }
                                    void MoveDown()
                                    {
                                        int pos = data.id.index;
                                        int swapPos = pos - 1;

                                        var swapData = frameEventList.GetMember(swapPos);
                                        if (swapData.id != null)
                                        {
                                            data.id.index = swapPos;
                                            data.id = frameEventList.AddOrUpdateMemberWithStorageComparison(data, false);
                                            data.id.index = swapPos; // do it again incase id reference changed
                                            data.id = frameEventList.AddOrUpdateMemberWithStorageComparison(data, false);

                                            swapData.id.index = pos;
                                            frameEventList.AddOrUpdateMember(swapData, true); 
                                            _event.Priority = swapPos;
                                        }

                                        source?.MarkAsDirty();
                                    }

                                    if (_event.Priority > 0) CustomEditorUtils.SetButtonOnClickActionByName(inst, str_up, MoveUp); else CustomEditorUtils.SetButtonInteractableByName(inst, str_up, false);
                                    _tempEvents.Clear();
                                    if (_event.Priority < source.rawAnimation.GetEventsAtFrame((decimal)_event.TimelinePosition, _tempEvents).Count - 1) CustomEditorUtils.SetButtonOnClickActionByName(inst, str_down, MoveDown); else CustomEditorUtils.SetButtonInteractableByName(inst, str_down, false);
                                    _tempEvents.Clear(); 
                                }

                                foreach(var __event in _orderedEvents) // Add the events to the visible list
                                {
                                    int frameIndex_ = AnimationTimeline.CalculateFrameAtTimelinePosition((decimal)__event.TimelinePosition, source.rawAnimation.framesPerSecond);
                                    if (frameIndex_ != frameIndex) continue;

                                    UIRecyclingList.MemberID memId = null; 
                                    void StartEditing() => OpenAnimationEventEditWindow(animationEventEditWindow, source, __event, frameEventList, frameEventList.GetMember(memId), mainList);
                                    memId = frameEventList.AddNewMember(__event.Name, StartEditing, false, OnRefreshEvent, __event); 
                                    __event.Priority = memId.index; 
                                }

                                if (source != null)
                                {
                                    void CreateNewEvent()
                                    {
                                        var _event = NewEventAtIndex(frameIndex);
                                        mainList.MarkAsDirty(); // Make sure list refreshes fully
                                        mainList.Refresh();
                                        OpenAnimationEventEditWindow(animationEventEditWindow, source, _event, null, new UIRecyclingList.MemberData() { name = _event.Name, storage = _event }, mainList);
                                    }
                                    CustomEditorUtils.SetButtonOnClickActionByName(inst, str_addEventToFrame, CreateNewEvent);
                                }

                            }

                            frameEventListData.onRefresh = OnRefreshEventList;
                            frameEventListData.id = mainList.AddOrUpdateMember(frameEventListData, false); 
                        }
                    }
                }
            } 
            void PostRefresh()
            {
                if (!validRefresh) return;
                var subLists = mainList.Container.GetComponentsInChildren<UIRecyclingList>();
                foreach (var subList in subLists) if (subList != mainList) subList.Refresh();
                validRefresh = false;
            }

            mainList.ClearAllListeners();
            mainList.OnRefresh += Refreshed; 
            mainList.AfterRefresh += PostRefresh;
            mainList.Refresh(); 

            if (source != null)
            {
                void CreateNewEvent()
                {
                    var _event = NewEventAtIndex(timelineWindow == null ? 0 : AnimationTimeline.CalculateFrameAtTimelinePosition((decimal)timelineWindow.ScrubPosition, source.GetOrCreateRawData().framesPerSecond));
                    mainList.MarkAsDirty(); // Make sure list refreshes fully
                    mainList.Refresh(); 
                    OpenAnimationEventEditWindow(animationEventEditWindow, source, _event, null, new UIRecyclingList.MemberData() { name = _event.Name, storage = _event }, mainList); 
                }
                CustomEditorUtils.SetButtonOnClickActionByName(window, str_addEvent, CreateNewEvent);  
            }
        }

        public void RefreshAnimationEventEditWindow(AnimationSource source, CustomAnimation.Event _event, UIRecyclingList list = null, UIRecyclingList.MemberData listMember = default, UIRecyclingList eventList = null) => RefreshAnimationEventEditWindow(animationEventEditWindow, source, _event, list, listMember, eventList); 
        public void RefreshAnimationEventEditWindow(RectTransform window, AnimationSource source, CustomAnimation.Event _event, UIRecyclingList list = null, UIRecyclingList.MemberData listMember = default, UIRecyclingList parentList = null)
        {
            if (window == null || !window.gameObject.activeInHierarchy || source == null || _event == null) return; 

            int frameRate = source.rawAnimation == null ? CustomAnimation.DefaultFrameRate : source.rawAnimation.framesPerSecond;

            var codeEditor = window.GetComponentInChildren<ICodeEditor>();
            if (codeEditor != null)
            {
                codeEditor.ClearAllListeners(); 

                var popup = window.GetComponentInChildren<UIPopup>();
                if (popup == null) popup = window.GetComponentInParent<UIPopup>();
                if (popup != null)
                {
                    if (popup.OnClose == null) popup.OnClose = new UnityEvent(); else popup.OnClose.RemoveListener(codeEditor.SpoofClose);
                    popup.OnClose.AddListener(codeEditor.SpoofClose);
                }

                codeEditor.Code = string.IsNullOrWhiteSpace(_event.Source) ? string.Empty : _event.Source;

                void SetEventCode(string code) => _event.Source = code;
                codeEditor.ListenForChanges(SetEventCode);
                codeEditor.ListenForClosure(SetEventCode);
            }

            void SetEventName(string name) 
            { 
                _event.Name = name; 
                if (list != null)
                {
                    if (list.TryGetMember(listMember.id, out var tempMem) || list.TryGetMemberByStorageComparison(listMember, out tempMem)) listMember = tempMem;         
                    listMember.name = name;
                    listMember.id = list.AddOrUpdateMemberWithStorageComparison(listMember);     
                }

                source?.MarkAsDirty();
            }
            CustomEditorUtils.SetInputFieldOnValueChangeActionByName(window, str_name, SetEventName);

            void SetEventFrame(int frame, bool force)
            {
                bool changed = force || AnimationTimeline.CalculateFrameAtTimelinePosition((decimal)_event.TimelinePosition, frameRate) != frame;
                if (changed)
                {
                    if (force) CustomEditorUtils.SetInputFieldTextByName(window, str_frame, frame.ToString()); // Wasn't changed by input field so update it

                    bool refreshParent = false;
                    _event.TimelinePosition = (float)AnimationTimeline.FrameToTimelinePosition(frame, frameRate);
                    if (list != null) 
                    { 
                        list.RemoveMember(listMember);
                        list.Refresh();
                        if (list.Count <= 0) refreshParent = true;                     
                    }
                    if (listMember.id != null) listMember.id.index = -1;

                    if (parentList != null)
                    {
                        parentList.MarkAsDirty();  
                        string header = GetFrameHeader(frame);
                        var tempData = parentList.FindMember(header);
                        tempData.storage = frame;

                        if (tempData.id == null)
                        {
                            tempData.name = header;
                            tempData.id = parentList.AddOrUpdateMemberWithStorageComparison(tempData, true);
                            refreshParent = false;

                            if (tempData.id == null || tempData.id.index < 0)
                            {
                                tempData = parentList.FindMember(header); 
                            }
                        }
                        if (parentList.TryGetVisibleMemberInstance(tempData, out GameObject inst)) 
                        {
                            list = inst.GetComponentInChildren<UIRecyclingList>(true);

                            if (list != null)
                            {
                                listMember = list.AddOrGetMemberWithStorageComparison(listMember, true);
                                _event.Priority = listMember.id.index; 
                            }
                        }
                        if (refreshParent) parentList.Refresh();
                    }

                    RefreshAnimationEventKeyframes();
                    source?.MarkAsDirty();
                }
            }

            void SetEventFrameFromString(string frameStr)
            {
                if (int.TryParse(frameStr, out int frame)) SetEventFrame(frame, false);
            }
            CustomEditorUtils.SetInputFieldOnValueChangeActionByName(window, str_frame, SetEventFrameFromString);

            void PromptDelete()
            {
                void Delete()
                {
                    if (_event != null && source != null && source.rawAnimation != null && source.rawAnimation.events != null)
                    {
                        _tempEvents.Clear();
                        _tempEvents.AddRange(source.rawAnimation.events);
                        _tempEvents.RemoveAll(i => ReferenceEquals(_event, i));  
                        source.rawAnimation.events = _tempEvents.ToArray();
                        _tempEvents.Clear();

                        if (parentList != null)
                        {
                            int frame = AnimationTimeline.CalculateFrameAtTimelinePosition((decimal)_event.TimelinePosition, source.rawAnimation.framesPerSecond);
                            string header = GetFrameHeader(frame);
                            var tempData = parentList.FindMember(header);
                            tempData.storage = frame;
                            if (parentList.TryGetVisibleMemberInstance(tempData, out GameObject inst))
                            {
                                list = inst.GetComponentInChildren<UIRecyclingList>(true);    
                            }
                        }
                    }

                    window.gameObject.SetActive(false); 
                    if (list != null) 
                    { 
                        list.RemoveMember(listMember);
                        list.Refresh();

                        if (list.Count <= 0 && parentList != null)
                        {
                            parentList.Refresh(); 
                        }
                    }

                    RefreshAnimationEventKeyframes();
                    source?.MarkAsDirty();  
                }
                ShowConfirmationMessage($"Are you sure you want to delete '{(_event == null ? "null" : _event.Name)}'? This cannot be undone!", Delete);
            }

            CustomEditorUtils.SetInputFieldTextByName(window, str_name, _event.Name);
            CustomEditorUtils.SetButtonOnClickActionByName(window, str_delete, PromptDelete);

            int frameIndex = AnimationTimeline.CalculateFrameAtTimelinePosition((decimal)_event.TimelinePosition, frameRate);
            if (list == null)
            { 
                SetEventFrame(frameIndex, true);  
                SetEventName(_event.Name);
            } 
            else
            {
                CustomEditorUtils.SetInputFieldTextByName(window, str_frame, frameIndex.ToString()); 
            }
        }

        #endregion

        [Header("Elements")]
        public GameObject playModeDropdownRoot;
        public GameObject keyframePrototype;
        protected PrefabPool keyframePool;
        [Range(0, 1)]
        public float keyframeAnchorY = 0.5f; 

        public RectTransform contextMenuMain;
        public void OpenContextMenuMain()
        {
            if (contextMenuMain == null) return;

            contextMenuMain.gameObject.SetActive(true);
            var menuTransform = contextMenuMain.transform;
            for(int a = 0; a < menuTransform.childCount; a++)
            {
                var child = menuTransform.GetChild(a);
                child.gameObject.SetActive(false);
            }
        }

        public GameObject boneDropdownListRoot;
        protected UIRecyclingList boneDropdownList;
        public UIRecyclingList BoneDropdownList
        {
            get
            {
                if (boneDropdownList == null) boneDropdownList = boneDropdownListRoot == null ? null : boneDropdownListRoot.GetComponentInChildren<UIRecyclingList>(true);
                return boneDropdownList;
            }
        }
        public void RefreshBoneDropdown()
        {
            if (BoneDropdownList == null) return;
            boneDropdownList.Clear();

            for(int a = 0; a < poseableBones.Count; a++)
            {
                int i = a;
                var bone = poseableBones[a];
                if (bone.transform == null) continue;
                boneDropdownList.AddNewMember(bone.transform.name, () => SetSelectedBoneInUI(i)); 
            }
            boneDropdownList.Refresh();

            string selectedBoneName = "null";
            if (selectedBoneUI >= 0 && selectedBoneUI < poseableBones.Count) 
            {
                var bone = poseableBones[selectedBoneUI];
                if (bone != null && bone.transform != null) selectedBoneName = bone.transform.name; 
            }
            SetComponentTextByName(boneDropdownListRoot, _currentTextObjName, selectedBoneName);
        }

        public GameObject boneCurveDropdownListRoot;
        protected UIRecyclingList boneCurveDropdownList;
        public UIRecyclingList BoneCurveDropdownList
        {
            get
            {
                if (boneCurveDropdownList == null) boneCurveDropdownList = boneCurveDropdownListRoot == null ? null : boneCurveDropdownListRoot.GetComponentInChildren<UIRecyclingList>(true);
                return boneCurveDropdownList;
            }
        }
        [Serializable]
        public enum TransformProperty
        {
            localPosition_x, localPosition_y, localPosition_z, localRotation_x, localRotation_y, localRotation_z, localRotation_w, localScale_x, localScale_y, localScale_z
        }
        protected TransformProperty selectedTransformPropertyUI;
        public void SetSelectedTransformPropertyUI(TransformProperty property)
        {
            selectedTransformPropertyUI = property;
            SetComponentTextByName(boneCurveDropdownListRoot, _currentTextObjName, property.ToString());
            RefreshTimeline();
        }
        public void RefreshBoneCurveDropdown()
        {
            if (BoneCurveDropdownList == null) return;
            boneCurveDropdownList.Clear();
            
            boneCurveDropdownList.AddNewMember(TransformProperty.localPosition_x.ToString(), () => SetSelectedTransformPropertyUI(TransformProperty.localPosition_x));
            boneCurveDropdownList.AddNewMember(TransformProperty.localPosition_y.ToString(), () => SetSelectedTransformPropertyUI(TransformProperty.localPosition_y));
            boneCurveDropdownList.AddNewMember(TransformProperty.localPosition_z.ToString(), () => SetSelectedTransformPropertyUI(TransformProperty.localPosition_z));

            boneCurveDropdownList.AddNewMember(TransformProperty.localRotation_x.ToString(), () => SetSelectedTransformPropertyUI(TransformProperty.localRotation_x));
            boneCurveDropdownList.AddNewMember(TransformProperty.localRotation_y.ToString(), () => SetSelectedTransformPropertyUI(TransformProperty.localRotation_y));
            boneCurveDropdownList.AddNewMember(TransformProperty.localRotation_z.ToString(), () => SetSelectedTransformPropertyUI(TransformProperty.localRotation_z));
            boneCurveDropdownList.AddNewMember(TransformProperty.localRotation_w.ToString(), () => SetSelectedTransformPropertyUI(TransformProperty.localRotation_w));

            boneCurveDropdownList.AddNewMember(TransformProperty.localScale_x.ToString(), () => SetSelectedTransformPropertyUI(TransformProperty.localScale_x));
            boneCurveDropdownList.AddNewMember(TransformProperty.localScale_y.ToString(), () => SetSelectedTransformPropertyUI(TransformProperty.localScale_y));
            boneCurveDropdownList.AddNewMember(TransformProperty.localScale_z.ToString(), () => SetSelectedTransformPropertyUI(TransformProperty.localScale_z));

            boneCurveDropdownList.Refresh();

            SetComponentTextByName(boneCurveDropdownListRoot, _currentTextObjName, selectedTransformPropertyUI.ToString());
        }
        public void RefreshBoneCurveDropdowns()
        {
            RefreshBoneDropdown();
            RefreshBoneCurveDropdown();
        }

        protected int selectedBoneUI;
        public int SelectedBoneUI
        {
            get => selectedBoneUI;
            set => SetSelectedBoneInUI(value);
        }
        protected const string _currentTextObjName = "Current-Text"; 
        public void SetSelectedBoneInUI(int index)
        {
            selectedBoneUI = index;

            if (boneDropdownListRoot != null)
            {
                string boneName = "null";
                if (poseableBones != null && index >= 0 && index < poseableBones.Count)
                {
                    var bone = poseableBones[index];
                    if (bone != null) boneName = bone.transform.name;
                }
                SetComponentTextByName(boneDropdownListRoot, _currentTextObjName, boneName); 
            }

            RefreshTimeline(); 
        }

        public GameObject boneGroupDropdownListRoot;
        protected UIRecyclingList boneGroupDropdownList;
        public UIRecyclingList BoneGroupDropdownList
        {
            get
            {
                if (boneGroupDropdownList == null) boneGroupDropdownList = boneGroupDropdownListRoot == null ? null : boneGroupDropdownListRoot.GetComponentInChildren<UIRecyclingList>(true);
                return boneGroupDropdownList;
            }
        }
        [Serializable]
        public enum BoneKeyGroup
        {
            All, LocalPosition, LocalRotation, LocalScale
        }
        protected BoneKeyGroup selectedBoneKeyGroupUI;
        public void SetSelectedBoneKeyGroupUI(BoneKeyGroup group)
        {
            selectedBoneKeyGroupUI = group;
            SetComponentTextByName(boneGroupDropdownListRoot, _currentTextObjName, group.ToString());
            RefreshTimeline();
        }
        public void RefreshBoneKeyGroupDropdown()
        {
            if (BoneGroupDropdownList == null) return;
            boneGroupDropdownList.Clear();

            boneGroupDropdownList.AddNewMember(BoneKeyGroup.All.ToString(), () => SetSelectedBoneKeyGroupUI(BoneKeyGroup.All));
            boneGroupDropdownList.AddNewMember(BoneKeyGroup.LocalPosition.ToString(), () => SetSelectedBoneKeyGroupUI(BoneKeyGroup.LocalPosition));
            boneGroupDropdownList.AddNewMember(BoneKeyGroup.LocalRotation.ToString(), () => SetSelectedBoneKeyGroupUI(BoneKeyGroup.LocalRotation));
            boneGroupDropdownList.AddNewMember(BoneKeyGroup.LocalScale.ToString(), () => SetSelectedBoneKeyGroupUI(BoneKeyGroup.LocalScale));

            boneGroupDropdownList.Refresh();

            SetComponentTextByName(boneGroupDropdownListRoot, _currentTextObjName, selectedBoneKeyGroupUI.ToString());
        }
        public void RefreshBoneKeyDropdowns()
        {
            RefreshBoneDropdown();
            RefreshBoneKeyGroupDropdown();
        }

        public GameObject propertyDropdownListRoot;
        protected UIRecyclingList propertyDropdownList;
        public UIRecyclingList PropertyDropdownList
        {
            get
            {
                if (propertyDropdownList == null) propertyDropdownList = propertyDropdownListRoot == null ? null : propertyDropdownListRoot.GetComponentInChildren<UIRecyclingList>(true);
                return propertyDropdownList;
            }
        }
        public string flexPropertyPrefix = "Flex_";
        public void RefreshPropertyDropdown()
        {
            if (PropertyDropdownList == null) return;
            propertyDropdownList.Clear();

            var animatable = ActiveAnimatable;

            if (animatable != null)
            {
                if (animatable.animator != null)
                {
                    var flexProxy = animatable.animator.GetComponent<MuscleFlexProxy>();
                    if (flexProxy != null)
                    {
                        var props = flexProxy.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                        string prefix = flexPropertyPrefix.ToLower();
                        foreach(var prop in props)
                        {
                            if (prop == null) continue;
                            int prefixStart = prop.Name.ToLower().IndexOf(prefix); 
                            if (prefixStart < 0) continue;
                            string displayName = prop.Name;//.Substring(prefixStart + prefix.Length);
                            string id = $"{IAnimator._animatorTransformPropertyStringPrefix}.{nameof(MuscleFlexProxy)}.{prop.Name}";
                            propertyDropdownList.AddNewMember(displayName, () => SetSelectedComponentPropertyUI(displayName, id));
                        }
                    }
                }
            }

            propertyDropdownList.Refresh();

            SetComponentTextByName(propertyDropdownListRoot, _currentTextObjName, string.IsNullOrWhiteSpace(selectedPropertyUI) ? "null" : selectedPropertyUI);
        }
        protected string selectedPropertyUI;
        protected string selectedPropertyId;
        public void SetSelectedComponentPropertyUI(string displayName, string id)
        {
            selectedPropertyUI = displayName;
            selectedPropertyId = id;
            SetComponentTextByName(propertyDropdownListRoot, _currentTextObjName, string.IsNullOrWhiteSpace(selectedPropertyUI) ? "null" : selectedPropertyUI);
            RefreshTimeline();
        }

        public static AnimationCurve DefaultLinearTimeCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public UIAnimationCurveRenderer curveRenderer;
        public void RefreshCurveRenderer(bool rebuild=false)
        {
            if (curveRenderer == null) return;

            if (timelineWindow != null)
            {
                float start = 0;
                float end = timelineWindow.Length;

                if (editMode != EditMode.GLOBAL_TIME && curveRenderer.curve != null && curveRenderer.curve.length > 1)
                {
                    var keyStart = curveRenderer.curve[0];
                    var keyEnd = curveRenderer.curve[curveRenderer.curve.length - 1];

                    start = keyStart.time;
                    end = keyEnd.time;
                }

                float p1 = timelineWindow.GetVisibleTimelineNormalizedPosition(start);
                float p2 = timelineWindow.GetVisibleTimelineNormalizedPosition(end);

                var rT = curveRenderer.RectTransform;
                rT.anchorMin = new Vector2(p1, 0);
                rT.anchorMax = new Vector2(p2, 1);
                var sizeDelta = rT.sizeDelta;
                sizeDelta.x = 0;
                rT.sizeDelta = sizeDelta;
                var pos = rT.anchoredPosition3D;
                pos.x = 0;
                rT.anchoredPosition3D = pos;
            }

            if (rebuild) curveRenderer.Rebuild();
        }
        public void RefreshTimeline()
        {
            if (timelineWindow == null) return;

            var animatable = ActiveAnimatable;
            bool flag = animatable == null || animatable.editIndex < 0 || animatable.animationBank == null || animatable.editIndex >= animatable.animationBank.Count;
            var source = flag ? null : animatable.animationBank[animatable.editIndex];
            flag = flag || source == null;

            if (!flag) timelineWindow.SetLength(source.timelineLength, false);

            var anim = flag ? null : source.rawAnimation;
            flag = flag || anim == null;

            if (flag || editMode == EditMode.GLOBAL_TIME || editMode == EditMode.ANIMATION_EVENTS)
            {
                timelineWindow.SetTimeCurve(null);
            }
            else
            {
                timelineWindow.SetTimeCurve(anim.timeCurve == null ? DefaultLinearTimeCurve : anim.timeCurve);
            }

            switch (editMode)
            {
                case EditMode.GLOBAL_TIME:
                    if (curveRenderer != null)
                    {
                        curveRenderer.gameObject.SetActive(true);
                        curveRenderer.SetLineColor(globalTimeCurveColor);
                        curveRenderer.curve = timelineWindow.TimeCurve;
                        RefreshCurveRenderer(true);

                        if (anim != null)
                        {
                            SetButtonOnClickAction(curveRenderer.gameObject, () =>
                            {
                                if (anim.timeCurve == null) 
                                { 
                                    curveRenderer.curve = timelineWindow.TimeCurve = anim.timeCurve = AnimationCurve.Linear(0, 0, 1, 1);
                                    source.MarkAsDirty();
                                }
                                StartEditingCurve(anim.timeCurve, true, true);
                            });
                        } 
                        else
                        {
                            SetButtonOnClickAction(curveRenderer.gameObject, null, true, true, false);
                        }
                    }
                    break;
                case EditMode.GLOBAL_KEYS:
                    if (curveRenderer != null)
                    {
                        curveRenderer.gameObject.SetActive(false);
                        SetButtonOnClickAction(curveRenderer.gameObject, null, true, true, false); 
                    }
                    break;
                case EditMode.BONE_KEYS:
                    RefreshBoneKeyDropdowns();
                    if (curveRenderer != null)
                    {
                        curveRenderer.gameObject.SetActive(false);
                        SetButtonOnClickAction(curveRenderer.gameObject, null, true, true, false);
                    }
                    break;
                case EditMode.BONE_CURVES:
                    RefreshBoneCurveDropdowns();
                    if (curveRenderer != null)
                    {
                        flag = selectedBoneUI >= 0 && selectedBoneUI < poseableBones.Count && animatable != null && source != null;
                        var bone = flag ? poseableBones[selectedBoneUI] : null;
                        flag = flag && bone != null && bone.transform != null; 
                        if (flag)
                        {
                            AnimationUtils.GetOrCreateTransformCurve(out var transformCurve, bone.transform.name, source.GetOrCreateRawData(), animatable.RestPose);

                            curveRenderer.gameObject.SetActive(true);
                            curveRenderer.SetLineColor(rawCurveColor);

                            AnimationCurve animCurve = null;
                            if (transformCurve != null && typeof(TransformCurve).IsAssignableFrom(transformCurve.GetType()))
                            {
                                TransformCurve transformCurve_ = (TransformCurve)transformCurve;
                                switch (selectedTransformPropertyUI)
                                {
                                    case TransformProperty.localPosition_x:
                                        animCurve = transformCurve_.localPositionCurveX;
                                        break;
                                    case TransformProperty.localPosition_y:
                                        animCurve = transformCurve_.localPositionCurveY;
                                        break;
                                    case TransformProperty.localPosition_z:
                                        animCurve = transformCurve_.localPositionCurveZ;
                                        break;

                                    case TransformProperty.localRotation_x:
                                        animCurve = transformCurve_.localRotationCurveX;
                                        break;
                                    case TransformProperty.localRotation_y:
                                        animCurve = transformCurve_.localRotationCurveY;
                                        break;
                                    case TransformProperty.localRotation_z:
                                        animCurve = transformCurve_.localRotationCurveZ;
                                        break;
                                    case TransformProperty.localRotation_w:
                                        animCurve = transformCurve_.localRotationCurveW;
                                        break;

                                    case TransformProperty.localScale_x:
                                        animCurve = transformCurve_.localScaleCurveX;
                                        break;
                                    case TransformProperty.localScale_y:
                                        animCurve = transformCurve_.localScaleCurveY;
                                        break;
                                    case TransformProperty.localScale_z:
                                        animCurve = transformCurve_.localScaleCurveZ;
                                        break;
                                }
                            }

                            curveRenderer.curve = animCurve;
                            RefreshCurveRenderer(true);

                            if (animCurve != null)
                            {
                                SetButtonOnClickAction(curveRenderer.gameObject, () =>
                                {
                                    StartEditingCurve(animCurve, true, true);
                                });
                            }
                            else
                            {
                                SetButtonOnClickAction(curveRenderer.gameObject, null, true, true, false);
                            }
                        } 
                        else
                        {
                            curveRenderer.curve = null;
                            RefreshCurveRenderer(true);
                            curveRenderer.gameObject.SetActive(false);
                        }
                    }
                    break;
                case EditMode.PROPERTY_CURVES:
                    RefreshPropertyDropdown();
                    if (curveRenderer != null)
                    {
                        flag = !string.IsNullOrWhiteSpace(selectedPropertyId) && animatable != null && source != null;
                        if (flag)
                        {
                            AnimationUtils.GetOrCreatePropertyCurve(out var propertyCurve, selectedPropertyId, source.GetOrCreateRawData(), animatable.RestPose);

                            curveRenderer.gameObject.SetActive(true);
                            curveRenderer.SetLineColor(rawCurveColor);

                            AnimationCurve animCurve = null;
                            if (propertyCurve != null && typeof(PropertyCurve).IsAssignableFrom(propertyCurve.GetType()))
                            {
                                PropertyCurve propertyCurve_ = (PropertyCurve)propertyCurve;
                                animCurve = propertyCurve_.propertyValueCurve;
                            }

                            curveRenderer.curve = animCurve;
                            RefreshCurveRenderer(true);

                            if (animCurve != null)
                            {
                                SetButtonOnClickAction(curveRenderer.gameObject, () =>
                                {
                                    StartEditingCurve(animCurve, true, true); 
                                });
                            }
                            else
                            {
                                SetButtonOnClickAction(curveRenderer.gameObject, null);
                            }
                        }
                        else
                        {
                            curveRenderer.curve = null;
                            RefreshCurveRenderer(true);
                            curveRenderer.gameObject.SetActive(false);
                        }
                    }
                    break;
                case EditMode.ANIMATION_EVENTS:
                    if (curveRenderer != null)
                    {
                        SetButtonOnClickAction(curveRenderer.gameObject, OpenAnimationEventsWindow); // Use curve renderer object as a button to open animation event window, even though we're not rendering a curve.

                        curveRenderer.curve = null;
                        RefreshCurveRenderer(true);
                        curveRenderer.gameObject.SetActive(true);
                    }
                    break;
            }

            RefreshKeyframes();
        }
        protected void OnSetTimelineSize()
        {
            RefreshCurveRenderer(false);
            RefreshKeyframePositions();
        }
        protected void OnSetTimelineLength(float length)
        {
            RefreshKeyframes();

            var source = CurrentSource;
            if (source != null) source.timelineLength = length;
        }

        protected void StartEditingCurve(AnimationCurve curve, bool notifyListeners = true, bool undoable=false)
        {
            if (curve == null) return;
            var editor = CurveEditor;
            if (editor == null) return;

            if (undoable) undoSystem.AddHistory(new UndoableStartEditingCurve() { editor = this, curve = curve });
            if (curveEditorWindow != null) 
            { 
                curveEditorWindow.gameObject.SetActive(true);
                if (undoable)
                {
                    var popup = curveEditorWindow.GetComponent<UIPopup>();
                    if (popup != null)
                    {
                        if (popup.OnClose == null) popup.OnClose = new UnityEvent(); else popup.OnClose.RemoveAllListeners();
                        popup.OnClose.AddListener(() => StopEditingCurve(true));
                    }
                }
            }
            if (editor.Curve == curve) return;
            if (editor is SwoleCurveEditor swoleCurveEditor) swoleCurveEditor.SetCurve(curve, notifyListeners); else editor.SetCurve(curve);
        }
        protected void StopEditingCurve(bool undoable = false)
        {
            if (curveEditorWindow != null) 
            {
                if (undoable && CurveEditor != null) undoSystem.AddHistory(new UndoableStopEditingCurve() { editor = this, curve = curveEditor.Curve });
                curveEditorWindow.gameObject.SetActive(false); 
            }
        }

        [SerializeField]
        protected RectTransform keyContainerTransform;
        public RectTransform KeyContainerTransform
        {
            get
            {
                if (keyContainerTransform == null) 
                {
                    if (timelineWindow != null) keyContainerTransform = timelineWindow.ContainerTransform;
                    if (keyContainerTransform == null) keyContainerTransform = gameObject.AddOrGetComponent<RectTransform>();
                }
                return keyContainerTransform;
            }
        }

        public GameObject loopButton;
        public GameObject recordButton;
        public GameObject playButton;
        public GameObject pauseButton;
        public GameObject stopButton;
        public GameObject firstFrameButton;
        public GameObject prevFrameButton;
        public GameObject nextFrameButton;
        public GameObject lastFrameButton;

        [Header("Bone Rendering")]
        public int boneOverlayLayer = 29; 

        public GameObject boneRootPrototype;
        public GameObject boneLeafPrototype;

        private PrefabPool boneRootPool;
        private PrefabPool boneLeafPool;

        [Header("Events"), SerializeField]
        private UnityEvent OnSetupSuccess = new UnityEvent();
        [SerializeField]
        private UnityEvent OnSetupFail = new UnityEvent();

        [Header("Misc")]
        public Transform floorTransform;

        public Color globalTimeCurveColor = Color.white;
        public Color rawCurveColor = Color.white;

        public Color keyColor = Color.white;
        public Color selectedKeyColor = Color.cyan;

        public Text setupErrorTextOutput;
        public TMP_Text setupErrorTextOutputTMP;

        public Sprite icon_humanoidRig;
        public Sprite icon_mechanicalRig;

        protected Sprite GetRigIcon(AnimatableAsset.ObjectType type)
        {
            switch (type)
            {
                case AnimatableAsset.ObjectType.Humanoid:
                    return icon_humanoidRig;

                case AnimatableAsset.ObjectType.Mechanical:
                    return icon_mechanicalRig;
            }

            return null;
        }

        protected void ShowSetupError(string msg)
        {

            if (setupErrorTextOutput != null)
            {
                setupErrorTextOutput.gameObject.SetActive(true);
                setupErrorTextOutput.enabled = true;
                setupErrorTextOutput.text = msg;
            }

            if (setupErrorTextOutputTMP != null)
            {
                setupErrorTextOutputTMP.gameObject.SetActive(true);
                setupErrorTextOutputTMP.enabled = true;
                setupErrorTextOutputTMP.text = msg;
            }

        }

        protected AnimationCurve lastEditedCurve;
        protected virtual void Awake()
        {

            SingletonCallStack.Insert(this);

            void FailSetup(string msg)
            {
                swole.LogError(msg);
                OnSetupFail?.Invoke();
                ShowSetupError(msg);
                Destroy(this);
            }

            if (animatableImporterWindow == null)
            {
                FailSetup($"Animatable Importer Window not set for Animation Editor '{name}'");
                return;
            }

            if (importedAnimatablesList == null)
            {
                FailSetup($"Imported Animatables List not set for Animation Editor '{name}'");
                return;
            }

            if (boneRootPrototype == null)
            {
                FailSetup($"Bone Root Prototype not set for Animation Editor '{name}'");
                return;
            }
            if (boneLeafPrototype == null)
            {
                FailSetup($"Bone Leaf Prototype not set for Animation Editor '{name}'");
                return;
            }
            if (keyframePrototype == null)
            {
                FailSetup($"Keyframe Prototype not set for Animation Editor '{name}'");
                return;
            }
            if (CurveEditor == null)
            {
                FailSetup($"Curve Editor not set for Animation Editor '{name}'");
                return;
            }
            
            if (curveEditor.OnStateChange == null) curveEditor.OnStateChange = new UnityEvent<AnimationCurveEditor.State, AnimationCurveEditor.State>();
            curveEditor.OnStateChange.AddListener((AnimationCurveEditor.State oldState, AnimationCurveEditor.State newState) => 
            {
                AnimationCurve editedCurve = curveEditor.Curve;
                undoSystem.AddHistory(new ChangeStateAction() 
                { 
                    
                    curveEditor = curveEditor, oldState = oldState, newState = newState,

                    onChange = (bool undo) =>
                    {
                        if (editedCurve != null) StartEditingCurve(editedCurve, false);
                    }
                
                });

                if (ReferenceEquals(lastEditedCurve, editedCurve))
                {
                    var activeSource = CurrentSource;
                    if (activeSource != null && !activeSource.IsDirty && activeSource.ContainsCurve(editedCurve))
                    {
                        activeSource.MarkAsDirty();
                    }
                }
                lastEditedCurve = editedCurve;

                IEnumerator Delayed()
                {
                    yield return null;
                    RefreshCurveRenderer(true);
                    if (editMode == EditMode.BONE_CURVES || editMode == EditMode.PROPERTY_CURVES)
                    {
                        RefreshKeyframes();
                    }
                }

                StartCoroutine(Delayed());

            }); 

            if (timelineWindow != null)
            {
                if (timelineWindow.OnSetScrubPosition == null) timelineWindow.OnSetScrubPosition = new UnityEvent<float>();
                timelineWindow.OnSetScrubPosition.AddListener(PlaySnapshotUnclamped);

                if (timelineWindow.OnResize == null) timelineWindow.OnResize = new UnityEvent();
                timelineWindow.OnResize.AddListener(OnSetTimelineSize);

                if (timelineWindow.OnSetLength == null) timelineWindow.OnSetLength = new UnityEvent<float>();
                timelineWindow.OnSetLength.AddListener(OnSetTimelineLength);
            }

            if (boneRootPool == null) boneRootPool = new GameObject("boneRootPool").AddComponent<PrefabPool>();
            boneRootPool.transform.SetParent(transform, false);
            boneRootPool.Reinitialize(boneRootPrototype, PoolGrowthMethod.Incremental, 1, 1, 2048);

            if (boneLeafPool == null) boneLeafPool = new GameObject("boneLeafPool").AddComponent<PrefabPool>();
            boneLeafPool.transform.SetParent(transform, false);
            boneLeafPool.Reinitialize(boneLeafPrototype, PoolGrowthMethod.Incremental, 1, 1, 2048);

            if (keyframePool == null) keyframePool = new GameObject("keyframePool").AddComponent<PrefabPool>();
            keyframePool.transform.SetParent(transform, false);
            keyframePool.Reinitialize(keyframePrototype, PoolGrowthMethod.Incremental, 1, 1, 1024);
            keyframePool.SetContainerTransform(KeyContainerTransform);

            if (loopButton != null)
            {
                CustomEditorUtils.SetButtonOnClickAction(loopButton, ToggleLooping, true, true, false); 
                SetLooping(false);
            }
            if (recordButton != null)
            {
                CustomEditorUtils.SetButtonOnClickAction(recordButton, ToggleRecording, true, true, false);
                SetRecording(false);
            }
            if (playButton != null)
            {
                CustomEditorUtils.SetButtonOnClickAction(playButton, PlayDefault, true, true, false);
                pauseButton.SetActive(true);
            }
            if (pauseButton != null)
            {
                CustomEditorUtils.SetButtonOnClickAction(pauseButton, Pause, true, true, false); 
                pauseButton.SetActive(false);
            }
            if (stopButton != null)
            {
                CustomEditorUtils.SetButtonOnClickAction(stopButton, StopAndReset, true, true, false);
                stopButton.SetActive(false);
            }
            if (firstFrameButton != null)
            {
                CustomEditorUtils.SetButtonOnClickAction(firstFrameButton, SkipToFirstFrame, true, true, false);
            }
            if (prevFrameButton != null)
            {
                CustomEditorUtils.SetButtonOnClickAction(prevFrameButton, SkipToPreviousFrame, true, true, false);
            }
            if (nextFrameButton != null)
            {
                CustomEditorUtils.SetButtonOnClickAction(nextFrameButton, SkipToNextFrame, true, true, false);
            }
            if (lastFrameButton != null)
            {
                CustomEditorUtils.SetButtonOnClickAction(lastFrameButton, SkipToFinalFrame, true, true, false);
            }

            if (string.IsNullOrEmpty(additiveEditorSetupScene))
            {
                string msg = "No additive editor setup scene was set. There will be no way to control the scene camera or to load and manipulate prefabs without it!";
                swole.LogWarning(msg);
                OnSetupFail?.Invoke();
                ShowSetupError(msg); 
            }
            else
            {

                try
                {

                    Camera mainCam = Camera.main;
                    EventSystem[] eventSystems = GameObject.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);

                    SceneManager.LoadScene(additiveEditorSetupScene, LoadSceneMode.Additive);
                    var scn = SceneManager.GetSceneByName(additiveEditorSetupScene);
                    if (scn.IsValid())
                    {

                        bool hadExistingCamera = false;
                        Vector3 existingCameraPos = Vector3.zero;
                        Quaternion existingCameraRot = Quaternion.identity;
                        if (mainCam != null)
                        {
                            hadExistingCamera = true;
                            var camT = mainCam.transform;
                            existingCameraPos = camT.position;
                            existingCameraRot = camT.rotation;
                            Destroy(mainCam.gameObject); // Make room for new main camera in setup scene
                        }
                        foreach (var system in eventSystems) if (system != null) Destroy(system.gameObject); // Remove any existing event systems and use the one in the setup scene

                        IEnumerator FindRuntimeEditor()
                        {
                            while (!scn.isLoaded)
                            {
                                yield return null;
                                scn = SceneManager.GetSceneByBuildIndex(scn.buildIndex);
                            }

                            if (runtimeEditor == null) runtimeEditor = GameObject.FindFirstObjectByType<RuntimeEditor>();
                            if (runtimeEditor == null)
                            {
                                FailSetup("No RuntimeEditor object was found! Objects cannot be edited without it. The additive editor setup scene should contain one.");
                            }
                            else
                            {
                                runtimeEditor.OnPreSelect += OnPreSelectCustomize;
                                runtimeEditor.OnSelectionChanged += OnSelectionChange;
                                runtimeEditor.OnManipulateTransforms += RecordManipulationAction;

                                runtimeEditor.DisableGrid = true;
                                runtimeEditor.DisableUndoRedo = true;
                                runtimeEditor.DisableGroupSelect = true;

#if BULKOUT_ENV
                                var rtSelect = RTObjectSelection.Get;
                                if (rtSelect != null)
                                {
                                    rtSelect.Settings.SetObjectLayerSelectable(boneOverlayLayer, true); // Make bone overlay layer selectable 
                                }
#endif
                                 
                                if (hadExistingCamera)
                                {
                                    mainCam = Camera.main;
                                    if (mainCam != null)
                                    {
                                        var camT = mainCam.transform;
                                        camT.SetPositionAndRotation(existingCameraPos, existingCameraRot);
                                    }
                                }

                                OnSetupSuccess.Invoke();
                            }

                        }

                        StartCoroutine(FindRuntimeEditor()); // Wait for scene to fully load, then find the runtime editor.

                    }
                    else
                    {
                        ShowSetupError($"Invalid scene name '{additiveEditorSetupScene}'");
                    }

                }
                catch (Exception ex)
                {
                    ShowSetupError(ex.Message);
                    throw ex;
                } 

            }

            SetEditMode(editMode);
            SetPlaybackMode(playbackMode);

            RenderingControl.SetRenderAnimationBones(true);
        }
        protected virtual void Start()
        {
            PlaybackPosition = 0;
        }

        protected void OnDestroy()
        {

            SingletonCallStack.Remove(this); 

            if (currentSession != null) currentSession.Destroy();

            if (runtimeEditor != null)
            {
                runtimeEditor.OnPreSelect -= OnPreSelectCustomize;
                runtimeEditor = null;
            }
        }

        #region Keyframe Logic

        //public float KeyframeChunkSize => timelineWindow == null ? 0 : ((1f / timelineWindow.LengthInFrames) * timelineWindow.Length);

        protected static readonly List<ITransformCurve.Frame> tempTransformFrames = new List<ITransformCurve.Frame>();
        protected static readonly List<IPropertyCurve.Frame> tempPropertyFrames = new List<IPropertyCurve.Frame>();
        protected static readonly List<Keyframe> tempKeyframes = new List<Keyframe>();
        protected static readonly List<CustomAnimation.Event> tempEvents = new List<CustomAnimation.Event>();

        protected delegate void EvaluateTransformLinearCurveKey(TransformLinearCurve curve, List<ITransformCurve.Frame> keyframesEdited);
        protected delegate void EvaluatePropertyLinearCurveKey(PropertyLinearCurve curve, List<IPropertyCurve.Frame> keyframesEdited);
        protected delegate void EvaluateAnimationCurveKey(AnimationCurve curve, TimelinePositionToFrameIndex getFrameIndex, List<Keyframe> keyframesEdited);
        protected delegate void EvaluateAnimationEvent(CustomAnimation.Event _event, TimelinePositionToFrameIndex getFrameIndex, List<CustomAnimation.Event> eventsEdited);

        protected delegate int TimelinePositionToFrameIndex(decimal timelinePos);
        protected delegate decimal FrameIndexToTimelinePosition(int frameIndex);

        protected class TimelineKeyframe
        {
            public AnimationEditor editor;

            public int timelinePosition;

            protected bool selected;
            public bool IsSelected
            {
                get => selected;
                set
                {
                    if (value) Select(); else Deselect();
                }
            }
            public void Select() 
            { 
                selected = true;
                if (editor != null) Image.color = editor.selectedKeyColor;
            }
            public void Deselect()
            { 
                selected = false;
                if (editor != null) Image.color = editor.keyColor;
            }

            public GameObject instance;
            protected RectTransform rectTransform;
            public RectTransform RectTransform
            {
                get
                {
                    if (rectTransform == null) rectTransform = instance == null ? null : instance.GetComponent<RectTransform>();
                    return rectTransform;
                }
            }
            protected Image image;
            public Image Image
            {
                get
                {
                    if (image == null) image = instance == null ? null : instance.GetComponent<Image>();
                    return image;
                }
            }

            public List<TransformLinearCurve> transformLinearCurves;
            public List<PropertyLinearCurve> propertyLinearCurves;
            public List<AnimationCurve> curves;
            public List<CustomAnimation> animationEventSources;

            public void IterateTransformLinearCurves(EvaluateTransformLinearCurveKey evaluateKey)
            {
                if (transformLinearCurves != null && evaluateKey != null)
                {
                    foreach (var curve in transformLinearCurves)
                    {
                        if (curve == null || curve.frames == null) continue;
                        tempTransformFrames.Clear();
                        tempTransformFrames.AddRange(curve.frames);

                        evaluateKey.Invoke(curve, tempTransformFrames);

                        tempTransformFrames.Sort((ITransformCurve.Frame x, ITransformCurve.Frame y) => (int)Mathf.Sign(x.timelinePosition - y.timelinePosition));
                        curve.frames = tempTransformFrames.ToArray();
                    }
                    tempTransformFrames.Clear();
                }
            }
            public void IteratePropertyLinearCurves(EvaluatePropertyLinearCurveKey evaluateKey)
            {
                if (propertyLinearCurves != null && evaluateKey != null)
                {
                    foreach (var curve in propertyLinearCurves)
                    {
                        if (curve == null || curve.frames == null) continue;
                        tempPropertyFrames.Clear();
                        tempPropertyFrames.AddRange(curve.frames);

                        evaluateKey.Invoke(curve, tempPropertyFrames);

                        tempPropertyFrames.Sort((IPropertyCurve.Frame x, IPropertyCurve.Frame y) => (int)Mathf.Sign(x.timelinePosition - y.timelinePosition));
                        curve.frames = tempPropertyFrames.ToArray();
                    }
                    tempPropertyFrames.Clear();
                }
            }
            public void IterateCurves(EvaluateAnimationCurveKey evaluateKey, TimelinePositionToFrameIndex getFrameIndex)
            {
                if (curves != null && evaluateKey != null)
                {
                    //float startTime = chunkSize * timelinePosition;
                    //float endTime = chunkSize * (timelinePosition + 1);
                    foreach (var curve in curves)
                    {
                        if (curve == null || curve.length <= 0) continue;
                        tempKeyframes.Clear();
                        tempKeyframes.AddRange(curve.keys);

                        evaluateKey.Invoke(curve, getFrameIndex, tempKeyframes);

                        tempKeyframes.Sort((Keyframe x, Keyframe y) => (int)Mathf.Sign(x.time - y.time));
                        curve.keys = tempKeyframes.ToArray();
                    }
                    tempKeyframes.Clear();
                }
            }
            public void IterateEvents(EvaluateAnimationEvent evaluateEvent, TimelinePositionToFrameIndex getFrameIndex)
            {
                if (animationEventSources != null && evaluateEvent != null)
                {
                    foreach (var anim in animationEventSources)
                    {
                        if (anim == null) continue;

                        tempEvents.Clear();
                        tempEvents.AddRange(anim.events);
                        foreach (var _event in anim.events)
                        {
                            if (_event == null) continue;

                            evaluateEvent.Invoke(_event, getFrameIndex, tempEvents);
                        }
                        anim.events = tempEvents.ToArray();
                        tempEvents.Clear();
                    }
                }
            }
            public void Relocate(int newTimelinePosition, TimelinePositionToFrameIndex getFrameIndex, FrameIndexToTimelinePosition getTimelinePos)
            {
                if (transformLinearCurves != null)
                {
                    void Evaluate(TransformLinearCurve curve, List<ITransformCurve.Frame> keyframesEdited)
                    {
                        foreach (var frame in keyframesEdited) if (frame.timelinePosition == timelinePosition) frame.timelinePosition = newTimelinePosition;
                    }
                    IterateTransformLinearCurves(Evaluate);
                }
                if (propertyLinearCurves != null)
                {
                    void Evaluate(PropertyLinearCurve curve, List<IPropertyCurve.Frame> keyframesEdited)
                    {
                        foreach (var frame in keyframesEdited) if (frame.timelinePosition == timelinePosition) frame.timelinePosition = newTimelinePosition;
                    }
                    IteratePropertyLinearCurves(Evaluate);
                }

                float offset = (float)(getTimelinePos(newTimelinePosition) - getTimelinePos(timelinePosition));
                if (curves != null)
                {
                    void Evaluate(AnimationCurve curve, TimelinePositionToFrameIndex getFrameIndex, List<Keyframe> keyframesEdited)
                    {
                        for (int a = 0; a < keyframesEdited.Count; a++)
                        {
                            var kf = keyframesEdited[a];
                            if (getFrameIndex((decimal)kf.time) == timelinePosition)
                            {
                                kf.time = kf.time + offset;
                                keyframesEdited[a] = kf;
                            }
                        }
                    }
                    IterateCurves(Evaluate, getFrameIndex);
                }
                if (animationEventSources != null)
                {
                    void Evaluate(CustomAnimation.Event _event, TimelinePositionToFrameIndex getFrameIndex, List<CustomAnimation.Event> eventsEdited)
                    {
                        if (getFrameIndex((decimal)_event.TimelinePosition) == timelinePosition)
                        {
                            _event.TimelinePosition = _event.TimelinePosition + offset; 
                        }
                    }
                    IterateEvents(Evaluate, getFrameIndex);
                }

                timelinePosition = newTimelinePosition;
            }
            public void Delete(TimelinePositionToFrameIndex getFrameIndex)
            {
                if (transformLinearCurves != null)
                {
                    void Evaluate(TransformLinearCurve curve, List<ITransformCurve.Frame> keyframesEdited)
                    {
                        keyframesEdited.RemoveAll(i => i.timelinePosition == timelinePosition);
                    }
                    IterateTransformLinearCurves(Evaluate);
                }
                if (propertyLinearCurves != null)
                {
                    void Evaluate(PropertyLinearCurve curve, List<IPropertyCurve.Frame> keyframesEdited)
                    {
                        keyframesEdited.RemoveAll(i => i.timelinePosition == timelinePosition); 
                    }
                    IteratePropertyLinearCurves(Evaluate);
                }
                if (curves != null)
                {
                    void Evaluate(AnimationCurve curve, TimelinePositionToFrameIndex getFrameIndex, List<Keyframe> keyframesEdited)
                    {
                        keyframesEdited.RemoveAll(i => getFrameIndex((decimal)i.time) == timelinePosition);
                    }
                    IterateCurves(Evaluate, getFrameIndex);
                }
                if (animationEventSources != null)
                {
                    void Evaluate(CustomAnimation.Event _event, TimelinePositionToFrameIndex getFrameIndex, List<CustomAnimation.Event> eventsEdited)
                    {
                        if (_event == null) return;
                        eventsEdited.RemoveAll(i => getFrameIndex((decimal)_event.TimelinePosition) == timelinePosition);
                    }
                    IterateEvents(Evaluate, getFrameIndex);
                }
            }
            public void CopyPaste(TimelineKeyframe copy, float offset, TimelinePositionToFrameIndex getFrameIndex)
            {
                int frameOffset = copy.timelinePosition - timelinePosition;

                if (transformLinearCurves != null)
                {
                    if (copy.transformLinearCurves == null) copy.transformLinearCurves = new List<TransformLinearCurve>();
                    copy.transformLinearCurves.AddRange(transformLinearCurves);

                    void Evaluate(TransformLinearCurve curve, List<ITransformCurve.Frame> keyframesEdited)
                    {
                        foreach(var frame in curve.frames)
                        {
                            if (frame.timelinePosition != timelinePosition) continue;

                            var frameCopy = frame.Duplicate();
                            frameCopy.timelinePosition = frameCopy.timelinePosition + frameOffset;
                            keyframesEdited.Add(frameCopy); // Ordering doesn't matter because the list will be sorted
                        }
                    }
                    IterateTransformLinearCurves(Evaluate);
                }
                if (propertyLinearCurves != null)
                {
                    if (copy.propertyLinearCurves == null) copy.propertyLinearCurves = new List<PropertyLinearCurve>();
                    copy.propertyLinearCurves.AddRange(propertyLinearCurves);

                    void Evaluate(PropertyLinearCurve curve, List<IPropertyCurve.Frame> keyframesEdited)
                    {
                        foreach (var frame in curve.frames)
                        {
                            if (frame.timelinePosition != timelinePosition) continue;

                            var frameCopy = frame.Duplicate();
                            frameCopy.timelinePosition = frameCopy.timelinePosition + frameOffset;
                            keyframesEdited.Add(frameCopy); // Ordering doesn't matter because the list will be sorted
                        }
                    }
                    IteratePropertyLinearCurves(Evaluate);
                }
                if (curves != null)
                {
                    if (copy.curves == null) copy.curves = new List<AnimationCurve>();
                    copy.curves.AddRange(curves);

                    void Evaluate(AnimationCurve curve, TimelinePositionToFrameIndex getFrameIndex, List<Keyframe> keyframesEdited)
                    {
                        for(int a = 0; a < curve.length; a++)
                        {
                            var key = curve[a];
                            if (getFrameIndex((decimal)key.time) != timelinePosition) continue;

                            key.time = key.time + offset;
                            keyframesEdited.Add(key); // Ordering doesn't matter because the list will be sorted
                        }
                    }
                    IterateCurves(Evaluate, getFrameIndex);
                }
                if (animationEventSources != null)
                {
                    if (copy.animationEventSources == null) copy.animationEventSources = new List<CustomAnimation>(); 
                    copy.animationEventSources.AddRange(animationEventSources);

                    void Evaluate(CustomAnimation.Event _event, TimelinePositionToFrameIndex getFrameIndex, List<CustomAnimation.Event> eventsEdited)
                    {
                        if (_event == null || getFrameIndex((decimal)_event.TimelinePosition) != timelinePosition) return;

                        var eventCopy = _event.Duplicate();
                        eventCopy.TimelinePosition = _event.TimelinePosition + offset; 
                        eventsEdited.Add(eventCopy);
                    }
                    IterateEvents(Evaluate, getFrameIndex); 
                }
            }
        }
        protected readonly Dictionary<int, TimelineKeyframe> keyframeInstances = new Dictionary<int, TimelineKeyframe>();
        protected void ClearKeyframes()
        {
            ClearClipboard();
            foreach (var pair in keyframeInstances)
            {
                var key = pair.Value;
                if (key == null) continue;
                if (key.instance != null)
                {
                    keyframePool.Release(key.instance);
                    key.instance.SetActive(false);
                }

                key.transformLinearCurves?.Clear();
                key.propertyLinearCurves?.Clear();
                key.curves?.Clear();
                key.animationEventSources?.Clear();

                key.editor = null;
                key.instance = null;
                key.transformLinearCurves = null;
                key.propertyLinearCurves = null;
                key.curves = null;
                key.animationEventSources = null;  
            }
            keyframeInstances.Clear();
        }
        protected void RefreshKeyframePosition(TimelineKeyframe key)
        {
            if (timelineWindow == null) return;

            var timelineTransform = timelineWindow.ContainerTransform;
            RefreshKeyframePosition(key, timelineTransform); 
        }
        protected void RefreshKeyframePosition(TimelineKeyframe key, RectTransform timelineTransform)
        {
            if (key.instance == null) return;
            var rT = key.RectTransform;
            rT.SetParent(timelineTransform);
            rT.anchorMin = rT.anchorMax = new Vector2(timelineWindow.GetVisibleTimelineNormalizedFramePosition(key.timelinePosition), keyframeAnchorY);
            rT.anchoredPosition3D = Vector3.zero;
        }
        public void RefreshKeyframePositions()
        {
            if (timelineWindow == null)
            {
                ClearKeyframes();
                return;
            }

            var timelineTransform = KeyContainerTransform;
            foreach (var element in keyframeInstances) RefreshKeyframePosition(element.Value, timelineTransform);
        }
        private static readonly List<TimelineKeyframe> tempKeys = new List<TimelineKeyframe>();
        public bool RelocateSelectedkeyframes(int offset)
        {
            if (offset == 0) return true;
            tempKeys.Clear();
            foreach (var pair in keyframeInstances) 
            {
                if (pair.Value.IsSelected) tempKeys.Add(pair.Value);       
            }

            foreach (var key in tempKeys) 
            {
                int newFrame = key.timelinePosition + offset; 
                if (newFrame < 0 || newFrame >= timelineWindow.LengthInFrames) return false; // Cancel due to key out of range
                if (keyframeInstances.TryGetValue(newFrame, out TimelineKeyframe existingKey) && !existingKey.IsSelected) return false; // Cancel due to overlapping key
            } 

            if (tempKeys.Count > 0)
            {
                var activeSource = CurrentSource;
                if (activeSource == null) return false;

                activeSource.MarkAsDirty();
            }

            var timelineTransform = KeyContainerTransform;
            foreach (var key in tempKeys) keyframeInstances.Remove(key.timelinePosition);

            if (offset < 0)
            {
                for (int i = 0; i < tempKeys.Count; i++)
                {
                    var key = tempKeys[i]; 
                    int newTimelinePosition = key.timelinePosition + offset;
                    keyframeInstances[newTimelinePosition] = key;
                    key.Relocate(newTimelinePosition, timelineWindow.CalculateFrameAtTimelinePosition, timelineWindow.FrameToTimelinePosition);
                    RefreshKeyframePosition(key, timelineTransform);
                }
            } 
            else
            {
                for (int i = (tempKeys.Count - 1); i >= 0; i--) 
                {
                    var key = tempKeys[i];
                    int newTimelinePosition = key.timelinePosition + offset;
                    keyframeInstances[newTimelinePosition] = key;
                    key.Relocate(newTimelinePosition, timelineWindow.CalculateFrameAtTimelinePosition, timelineWindow.FrameToTimelinePosition);
                    RefreshKeyframePosition(key, timelineTransform);
                }
            }
            
            tempKeys.Clear();

            if (curveRenderer != null && curveRenderer.gameObject.activeSelf) RefreshCurveRenderer(true);

            return true;
        }
        public int SelectedKeyframeCount
        {
            get
            {
                int count = 0;
                foreach (var pair in keyframeInstances) if (pair.Value.IsSelected) count++;
                return count;
            }
        }
        public void DeselectAllKeyframes()
        {
            foreach (var pair in keyframeInstances) pair.Value.Deselect();
        }
        public void SelectAllKeyframes()
        {
            foreach (var pair in keyframeInstances) pair.Value.Select(); 
        }
        public void DeleteSelectedKeyframes()
        {
            bool flag = false;

            foreach (var pair in keyframeInstances) if (pair.Value.IsSelected) 
                { 
                    pair.Value.Delete(timelineWindow.CalculateFrameAtTimelinePosition);
                    flag = true;
                }

            if (flag)
            {
                RefreshKeyframes();
                if (curveRenderer != null && curveRenderer.gameObject.activeSelf) RefreshCurveRenderer(true);

                var source = CurrentSource;
                if (source != null) source.MarkAsDirty(); 
            }
        }
        protected int copyFromFrameIndex = -1;
        protected readonly List<int> keysToCopy = new List<int>();
        public void ClearClipboard()
        {
            copyFromFrameIndex = -1;
            keysToCopy.Clear();
        }
        public void CopyKeyframes()
        {
            ClearClipboard();

            foreach (var pair in keyframeInstances)
            {
                var key = pair.Value;
                if (key == null || !key.IsSelected) continue;

                keysToCopy.Add(pair.Key);
            }

            if (keysToCopy.Count > 0) copyFromFrameIndex = timelineWindow == null ? keyframeInstances[keysToCopy[0]].timelinePosition : timelineWindow.ScrubFrame; 
        }
        public void PasteKeyframes(int pasteToFrameIndex) 
        {
            if (timelineWindow == null || copyFromFrameIndex < 0 || pasteToFrameIndex == copyFromFrameIndex || pasteToFrameIndex < 0) return;

            int frameOffset = pasteToFrameIndex - copyFromFrameIndex;
            float offset = (float)(timelineWindow.FrameToTimelinePosition(pasteToFrameIndex) - timelineWindow.FrameToTimelinePosition(copyFromFrameIndex));

            tempKeys.Clear();
            foreach(var frameIndex in keysToCopy) 
            {
                if (!keyframeInstances.TryGetValue(frameIndex, out var key)) continue;
                tempKeys.Add(key); 
            }

            if (tempKeys.Count > 0)
            {
                var activeSource = CurrentSource;
                if (activeSource == null) return;

                activeSource.MarkAsDirty(); 
            }

            foreach (var toCopy in tempKeys)
            {
                var copy = GetOrCreateKeyframe(toCopy.timelinePosition + frameOffset);
                if (copy == null) continue;

                toCopy.CopyPaste(copy, offset, timelineWindow.CalculateFrameAtTimelinePosition);  
            }
            tempKeys.Clear();

            RefreshKeyframePositions();
        }
        protected TimelineKeyframe GetOrCreateKeyframe(int timelinePosition)
        {
            if (keyframeInstances.TryGetValue(timelinePosition, out TimelineKeyframe key)) return key;

            if (timelinePosition > timelineWindow.LengthInFrames || !keyframePool.TryGetNewInstance(out GameObject keyObj)) return null;

            keyObj.SetActive(true);

            key = new TimelineKeyframe();
            key.editor = this;
            keyframeInstances[timelinePosition] = key;
            key.timelinePosition = timelinePosition;
            key.instance = keyObj;
            key.Deselect();

            var draggable = keyObj.AddOrGetComponent<UIDraggable>();
            draggable.freeze = true;
            draggable.navigation = default;
            if (draggable.OnClick == null) draggable.OnClick = new UnityEvent(); else draggable.OnClick.RemoveAllListeners();
            draggable.OnClick.AddListener(() =>
            {
                if (InputProxy.Modding_ModifyActionKey)
                {
                    key.IsSelected = !key.IsSelected;
                }
                else
                {
                    foreach (var pair in keyframeInstances) pair.Value.Deselect(); // Deselect all other keys
                    key.Select();
                }
            });
            VarRef<bool> newlySelectedFlag = new VarRef<bool>(false);
            if (draggable.OnDragStart == null) draggable.OnDragStart = new UnityEvent(); else draggable.OnDragStart.RemoveAllListeners();
            draggable.OnDragStart.AddListener(() =>
            {
                draggable.cancelNextClick = true;
                newlySelectedFlag.value = !key.IsSelected;
                key.Select();
            });
            if (draggable.OnDragStop == null) draggable.OnDragStop = new UnityEvent(); else draggable.OnDragStop.RemoveAllListeners();
            draggable.OnDragStop.AddListener(() =>
            {
                if (newlySelectedFlag) key.Deselect();
            });
            if (draggable.OnDragStep == null) draggable.OnDragStep = new UnityEvent(); else draggable.OnDragStep.RemoveAllListeners();
            if (timelineWindow != null)
            {
                draggable.OnDragStep.AddListener(() =>
                {
                    var canvas = timelineWindow.Canvas;
                    Vector3 cursorPos = timelineWindow.ContainerTransform.InverseTransformPoint(canvas.transform.TransformPoint(AnimationCurveEditorUtils.ScreenToCanvasPosition(canvas, CursorProxy.ScreenPosition)));
                    float posInContainer = timelineWindow.GetNormalizedPositionFromLocalPosition(cursorPos);

                    int newFrame = timelineWindow.CalculateFrameAtTimelinePosition((decimal)timelineWindow.GetTimeFromNormalizedPosition(posInContainer, true));
                    if (newFrame != key.timelinePosition) RelocateSelectedkeyframes(newFrame - key.timelinePosition);
                });
            }

            return key;
        }
        public void RefreshKeyframes()
        {
            ClearKeyframes();

            var animatable = ActiveAnimatable;
            if (animatable == null) return;
            var activeSource = animatable.CurrentSource;
            if (activeSource == null || activeSource.rawAnimation == null) return;

            void TryAddCurve(AnimationCurve curve)
            {
                if (curve == null || curve.length <= 0) return;

                for(int a = 0; a < curve.length; a++)
                {
                    var keyframe = curve[a];
                    //var key = GetOrCreateKeyframe(Mathf.FloorToInt(keyframe.time * activeSource.rawAnimation.framesPerSecond));
                    var key = GetOrCreateKeyframe(timelineWindow.CalculateFrameAtTimelinePosition((decimal)keyframe.time));
                    if (key != null) 
                    {
                        if (key.curves == null) key.curves = new List<AnimationCurve>();
                        key.curves.Add(curve); 
                    }
                }
            }
            void TryAddTransformLinearCurve(TransformLinearCurve curve)
            {
                if (curve == null || curve.FrameLength <= 0) return;

                foreach(var frame in curve.frames)
                {
                    var key = GetOrCreateKeyframe(frame.timelinePosition);
                    if (key != null)
                    {
                        if (key.transformLinearCurves == null) key.transformLinearCurves = new List<TransformLinearCurve>();
                        key.transformLinearCurves.Add(curve);
                    }
                }
            }
            void TryAddPropertyLinearCurve(PropertyLinearCurve curve)
            {
                if (curve == null || curve.FrameLength <= 0) return;

                foreach (var frame in curve.frames)
                {
                    var key = GetOrCreateKeyframe(frame.timelinePosition);
                    if (key != null)
                    {
                        if (key.propertyLinearCurves == null) key.propertyLinearCurves = new List<PropertyLinearCurve>();
                        key.propertyLinearCurves.Add(curve);
                    }
                }
            }
            void TryAddAnimationEventSource(CustomAnimation eventSource)
            {
                if (eventSource == null || eventSource.events == null) return;

                foreach(var _event in eventSource.events)
                {
                    if (_event == null) continue;

                    var key = GetOrCreateKeyframe(timelineWindow.CalculateFrameAtTimelinePosition((decimal)_event.TimelinePosition));
                    if (key != null)
                    {
                        if (key.animationEventSources == null) key.animationEventSources = new List<CustomAnimation>();
                        key.animationEventSources.Add(eventSource);
                    }
                }
            }

            switch (editMode)
            {
                case EditMode.GLOBAL_KEYS:
                    if (activeSource.rawAnimation.transformAnimationCurves != null)
                    {
                        foreach (var curveInfo in activeSource.rawAnimation.transformAnimationCurves)
                        {
                            if (curveInfo.infoMain.isLinear)
                            {
                                if (activeSource.rawAnimation.transformLinearCurves == null || curveInfo.infoMain.curveIndex < 0 || curveInfo.infoMain.curveIndex >= activeSource.rawAnimation.transformLinearCurves.Length) continue;
                                var curve = activeSource.rawAnimation.transformLinearCurves[curveInfo.infoMain.curveIndex];
                                if (curve == null) continue;

                                TryAddTransformLinearCurve(curve);
                            } 
                            else
                            {
                                if (activeSource.rawAnimation.transformCurves == null || curveInfo.infoMain.curveIndex < 0 || curveInfo.infoMain.curveIndex >= activeSource.rawAnimation.transformCurves.Length) continue;
                                var curve = activeSource.rawAnimation.transformCurves[curveInfo.infoMain.curveIndex];
                                if (curve == null) continue;

                                TryAddCurve(curve.localPositionCurveX);
                                TryAddCurve(curve.localPositionCurveY);
                                TryAddCurve(curve.localPositionCurveZ);

                                TryAddCurve(curve.localRotationCurveX);
                                TryAddCurve(curve.localRotationCurveY);
                                TryAddCurve(curve.localRotationCurveZ);
                                TryAddCurve(curve.localRotationCurveW);

                                TryAddCurve(curve.localScaleCurveX);
                                TryAddCurve(curve.localScaleCurveY);
                                TryAddCurve(curve.localScaleCurveZ);
                            }
                        }
                    }

                    if (activeSource.rawAnimation.propertyAnimationCurves != null)
                    {
                        foreach (var curveInfo in activeSource.rawAnimation.propertyAnimationCurves)
                        {
                            if (curveInfo.infoMain.isLinear)
                            {
                                if (activeSource.rawAnimation.propertyLinearCurves == null || curveInfo.infoMain.curveIndex < 0 || curveInfo.infoMain.curveIndex >= activeSource.rawAnimation.propertyLinearCurves.Length) continue;
                                var curve = activeSource.rawAnimation.propertyLinearCurves[curveInfo.infoMain.curveIndex];
                                if (curve == null) continue;

                                TryAddPropertyLinearCurve(curve);
                            }
                            else
                            {
                                if (activeSource.rawAnimation.propertyCurves == null || curveInfo.infoMain.curveIndex < 0 || curveInfo.infoMain.curveIndex >= activeSource.rawAnimation.propertyCurves.Length) continue;
                                var curve = activeSource.rawAnimation.propertyCurves[curveInfo.infoMain.curveIndex];
                                if (curve == null) continue;

                                TryAddCurve(curve.propertyValueCurve);
                            }
                        }
                    }
                    break;

                case EditMode.BONE_KEYS:
                    if (activeSource.rawAnimation.transformAnimationCurves != null && selectedBoneUI >= 0 && selectedBoneUI < poseableBones.Count)
                    {
                        var bone = poseableBones[selectedBoneUI];
                        if (bone != null && bone.transform != null) 
                        {
                            string boneName = bone.transform.name.AsID();
                            foreach (var curveInfo in activeSource.rawAnimation.transformAnimationCurves)
                            {
                                if (curveInfo.infoMain.isLinear)
                                {
                                    if (activeSource.rawAnimation.transformLinearCurves == null || curveInfo.infoMain.curveIndex < 0 || curveInfo.infoMain.curveIndex >= activeSource.rawAnimation.transformLinearCurves.Length) continue;
                                    var curve = activeSource.rawAnimation.transformLinearCurves[curveInfo.infoMain.curveIndex];
                                    if (curve == null || curve.frames == null || curve.TransformName.AsID() != boneName) continue;

                                    TryAddTransformLinearCurve(curve);
                                }
                                else
                                {
                                    if (activeSource.rawAnimation.transformCurves == null || curveInfo.infoMain.curveIndex < 0 || curveInfo.infoMain.curveIndex >= activeSource.rawAnimation.transformCurves.Length) continue;
                                    var curve = activeSource.rawAnimation.transformCurves[curveInfo.infoMain.curveIndex];
                                    if (curve == null || curve.TransformName.AsID() != boneName) continue;

                                    if (selectedBoneKeyGroupUI == BoneKeyGroup.All || selectedBoneKeyGroupUI == BoneKeyGroup.LocalPosition)
                                    {
                                        TryAddCurve(curve.localPositionCurveX);
                                        TryAddCurve(curve.localPositionCurveY);
                                        TryAddCurve(curve.localPositionCurveZ); 
                                    }
                                    if (selectedBoneKeyGroupUI == BoneKeyGroup.All || selectedBoneKeyGroupUI == BoneKeyGroup.LocalRotation)
                                    {
                                        TryAddCurve(curve.localRotationCurveX);
                                        TryAddCurve(curve.localRotationCurveY);
                                        TryAddCurve(curve.localRotationCurveZ);
                                        TryAddCurve(curve.localRotationCurveW);
                                    }
                                    if (selectedBoneKeyGroupUI == BoneKeyGroup.All || selectedBoneKeyGroupUI == BoneKeyGroup.LocalScale)
                                    {
                                        TryAddCurve(curve.localScaleCurveX);
                                        TryAddCurve(curve.localScaleCurveY);
                                        TryAddCurve(curve.localScaleCurveZ);
                                    }
                                }
                            }
                        }
                    }
                    break;

                case EditMode.BONE_CURVES:
                    if (selectedBoneUI >= 0 && selectedBoneUI < poseableBones.Count)
                    {
                        var bone = poseableBones[selectedBoneUI];
                        if (bone != null && bone.transform != null)
                        {
                            activeSource.rawAnimation.TryGetTransformCurves(bone.transform.name, out _, out _, out ITransformCurve mainCurve, out _);
                            if (mainCurve != null)
                            {
                                if (mainCurve is TransformLinearCurve linearCurve)
                                {
                                    TryAddTransformLinearCurve(linearCurve);
                                }
                                else if (mainCurve is TransformCurve curve)
                                {
                                    switch(selectedTransformPropertyUI)
                                    {
                                        case TransformProperty.localPosition_x:
                                            TryAddCurve(curve.localPositionCurveX);
                                            break;
                                        case TransformProperty.localPosition_y:
                                            TryAddCurve(curve.localPositionCurveY);
                                            break;
                                        case TransformProperty.localPosition_z:
                                            TryAddCurve(curve.localPositionCurveZ);
                                            break;

                                        case TransformProperty.localRotation_x:
                                            TryAddCurve(curve.localRotationCurveX);
                                            break;
                                        case TransformProperty.localRotation_y:
                                            TryAddCurve(curve.localRotationCurveY);
                                            break;
                                        case TransformProperty.localRotation_z:
                                            TryAddCurve(curve.localRotationCurveZ);
                                            break;
                                        case TransformProperty.localRotation_w:
                                            TryAddCurve(curve.localRotationCurveW);
                                            break;

                                        case TransformProperty.localScale_x:
                                            TryAddCurve(curve.localScaleCurveX);
                                            break;
                                        case TransformProperty.localScale_y:
                                            TryAddCurve(curve.localScaleCurveY);
                                            break;
                                        case TransformProperty.localScale_z:
                                            TryAddCurve(curve.localScaleCurveZ);
                                            break;
                                    }
                                }
                            }
                        }
                    }
                    break;

                case EditMode.PROPERTY_CURVES:
                    if (!string.IsNullOrWhiteSpace(selectedPropertyId))
                    {
                        activeSource.rawAnimation.TryGetPropertyCurves(selectedPropertyId, out _, out _, out IPropertyCurve mainCurve, out _);
                        if (mainCurve != null)
                        {
                            if (mainCurve is PropertyLinearCurve linearCurve)
                            {
                                TryAddPropertyLinearCurve(linearCurve);
                            }
                            else if (mainCurve is PropertyCurve curve)
                            {
                                TryAddCurve(curve.propertyValueCurve);
                            }
                        }
                    }
                    break;

                case EditMode.ANIMATION_EVENTS:
                    TryAddAnimationEventSource(activeSource.rawAnimation);
                    break;
            }

            RefreshKeyframePositions();

            if (animationEventsWindow != null && animationEventsWindow.gameObject.activeInHierarchy)
            {
                RefreshAnimationEventsWindow(); 
            }
        }

        #endregion

        private List<AnimatableAsset> animatablesCache = new List<AnimatableAsset>();
        public void OpenAnimatableImporter()
        {
            if (animatableImporterWindow == null) return;

            UIPopup popup = animatableImporterWindow.GetComponentInChildren<UIPopup>(true);

            UICategorizedList list = animatableImporterWindow.GetComponentInChildren<UICategorizedList>(true);
            if (list != null) 
            {
                list.Clear(false); 

                animatablesCache.Clear(); 
                animatablesCache =AnimationLibrary.GetAllAnimatables(animatablesCache);

                foreach(var animatable in animatablesCache)
                {
                    list.AddNewListMember(animatable.name, animatable.type.ToString().ToUpper(), () => {

                        if (popup != null) popup.Close();
                        ImportNewAnimatable(animatable, true, true);
                    
                    }, GetRigIcon(animatable.type)); 
                }
            } 

            animatableImporterWindow.gameObject.SetActive(true);
            animatableImporterWindow.SetAsLastSibling();
            if (popup != null) popup.Elevate();
        }

        public static void AddAnimatableToScene(AnimatableAsset animatable, out GameObject instance, out CustomAnimator animator)
        {
            instance = null;
            animator = null;

            if (animatable == null) return;

            instance = GameObject.Instantiate(animatable.prefab);

            Transform transform = instance.transform;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            animator = instance.GetComponentInChildren<CustomAnimator>();
            if (animator != null)
            {
                animator.OverrideUpdateCalls = true;
                animator.disableMultithreading = false;
                animator.enabled = false;
            }
        }

        public static ImportedAnimatable AddAnimatableToScene(AnimatableAsset animatable, int copy=0)
        {
            if (animatable == null) return null;

            ImportedAnimatable obj = new ImportedAnimatable() { id = animatable.name, displayName = animatable.name + (copy <= 0 ? "" : $" ({copy})") };

            AddAnimatableToScene(animatable, out obj.instance, out obj.animator);

            if (obj.animator != null) 
            {
                obj.RestPose = new AnimationUtils.Pose(obj.animator);
            } 
            else if (obj.instance != null)
            {
                obj.RestPose = new AnimationUtils.Pose(obj.instance.transform);
            }

            return obj;
        }

        public const string previewPrefix = "preview";
        public const string previewLayer_Main = "main";

        public const string restPosePrefix = "restPose";
        public const string restPoseLayer_Main = "main";

        [Serializable]
        public class Session : SwoleObject<Session, Session.Serialized>
        {

            [NonSerialized]
            public bool isDirty;

            #region Serialization

            public override Serialized AsSerializableStruct() => this;
            public override string AsJSON(bool prettyPrint = false) => AsSerializableStruct().AsJSON(prettyPrint);

            [Serializable]
            public struct Serialized : ISerializableContainer<Session, Session.Serialized>
            {

                public string name;

                public int activeObjectIndex;
                public ImportedAnimatable.Serialized[] importedObjects;

                public Session AsOriginalType(PackageInfo packageInfo = default) => new Session(this, packageInfo);

                public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);

                public string AsJSON(bool prettyPrint = false) => swole.Engine.ToJson(this, prettyPrint);
                public static Serialized FromJSON(string json) => swole.FromJson<Serialized>(json);
            }

            public static implicit operator Serialized(Session source)
            {
                Serialized s = new Serialized();

                s.name = source.name;
                s.activeObjectIndex = source.activeObjectIndex;

                if (source.importedObjects != null)
                {
                    s.importedObjects = new ImportedAnimatable.Serialized[source.importedObjects.Count];
                    for (int a = 0; a < source.importedObjects.Count; a++) s.importedObjects[a] = source.importedObjects[a];
                }
                return s;
            }

            public Session(Session.Serialized serializable, PackageInfo packageInfo = default) : base(serializable)
            {
                this.name = serializable.name;
                this.activeObjectIndex = serializable.activeObjectIndex;

                if (serializable.importedObjects != null)
                {
                    this.importedObjects = new List<ImportedAnimatable>();
                    for (int a = 0; a < serializable.importedObjects.Length; a++) this.importedObjects.Add(serializable.importedObjects[a].AsOriginalType(packageInfo)); 
                }
            }

            #endregion

            public string name;

            public Session(string name) : base(default)
            {
                this.name = name;
            }

            public void Destroy()
            {
                if (importedObjects != null)
                {
                    foreach (var obj in importedObjects) if (obj != null) obj.Destroy();
                    importedObjects = null;
                }
            }

            public int activeObjectIndex=-1;
            public List<ImportedAnimatable> importedObjects;

            public ImportedAnimatable ActiveObject
            {
                get
                {
                    if (importedObjects == null || activeObjectIndex < 0 || activeObjectIndex >= importedObjects.Count) return null;
                    return importedObjects[activeObjectIndex];
                }
            }

            public void SetVisibilityOfObject(AnimationEditor editor, int index, bool visible)
            {
                if (importedObjects == null || index < 0 || index >= importedObjects.Count) return;
                SetVisibilityOfObject(editor, importedObjects[index], visible);
            }
            public void SetVisibilityOfObject(AnimationEditor editor, ImportedAnimatable obj, bool visible)
            {
                if (obj == null) return;
                obj.SetVisibility(editor, visible);
            }
            public void SetVisibilityOfSource(AnimationEditor editor, int animatableIndex, int sourceIndex, bool visible)
            {
                if (importedObjects == null || animatableIndex < 0 || animatableIndex >= importedObjects.Count) return;

                var obj = importedObjects[animatableIndex];
                if (obj == null) return;

                SetVisibilityOfSource(editor, obj, sourceIndex, visible);
            }
            public void SetVisibilityOfSource(AnimationEditor editor, ImportedAnimatable obj, int sourceIndex, bool visible)
            {
                if (obj == null) return;
                obj.SetVisibilityOfSource(editor, sourceIndex, visible);
            }
            public void SetVisibilityOfSource(AnimationEditor editor, ImportedAnimatable obj, AnimationSource source, bool visible)
            {
                if (obj == null) return;
                obj.SetVisibilityOfSource(editor, source, visible);
            }
            
            public void SetActiveObject(AnimationEditor editor, int index, bool skipIfAlreadyActive = true, bool updateUI = true)
            {
                if (importedObjects == null) return;
                index = Mathf.Min(index, importedObjects.Count - 1);

                ImportedAnimatable importedObj = null;

                if (index >= 0) importedObj = importedObjects[index];
                if (importedObj != null && importedObj.listCategory != null) importedObj.listCategory.Expand(); 

                if ((skipIfAlreadyActive && activeObjectIndex != index) || !skipIfAlreadyActive)
                {
                    activeObjectIndex = index;

                    if (importedObj != null)
                    {
                        importedObj.SetVisibility(editor, true);
                        editor.SetActiveAnimator(importedObj.animator);
                    }

                    editor.OnSetActiveObject(importedObj);
                }

                if (updateUI) editor.RefreshAnimatableListUI();
            }

            [Obsolete]
            public void Reload(AnimationEditor editor)
            {
                if (importedObjects == null) return;

                foreach (var obj in importedObjects)
                {
                    if (obj == null) continue;
                    if (obj.instance != null)
                    {
                        GameObject.Destroy(obj.instance);
                    }
                    if (editor.importedAnimatablesList != null && obj.listCategory != null)
                    {
                        editor.importedAnimatablesList.DeleteCategory(obj.listCategory);
                    }
                }

                foreach (var obj in importedObjects)
                {
                    if (obj == null) continue;
                    var asset = AnimationLibrary.FindAnimatable(obj.id);
                    if (asset == null) continue;
                    obj.instance = GameObject.Instantiate(asset.prefab);
                    Transform transform = obj.instance.transform;
                    transform.localPosition = Vector3.zero;
                    transform.localRotation = Quaternion.identity;
                    transform.localScale = Vector3.one;
                }
                
                SetActiveObject(editor, activeObjectIndex);
            }

            public const string _editPhysiqueContextMenuOptionName = "EditPhysique";

            public void ImportNewAnimatable(AnimationEditor editor, string id, bool setAsActiveObject = true, bool undoable=false) => ImportNewAnimatable(editor, AnimationLibrary.FindAnimatable(id), setAsActiveObject, undoable);
            public void ImportNewAnimatable(AnimationEditor editor, AnimatableAsset animatable, bool setAsActiveObject=true, bool undoable=false)
            {
                if (animatable != null)
                {
                    if (importedObjects == null) importedObjects = new List<ImportedAnimatable>(); 

                    int i = 0;
                    foreach (var iobj in importedObjects) if (iobj != null && iobj.displayName.AsID().StartsWith(animatable.name.AsID())) i++;

                    var instance = AnimationEditor.AddAnimatableToScene(animatable, i);
                    AddAnimatable(editor, instance, -1, setAsActiveObject);

                    if (undoable && instance != null)
                    {
                        editor.undoSystem.AddHistory(new UndoableImportAnimatable(editor, instance.index, instance));
                    }
                }
            }

            public void AddAnimatable(AnimationEditor editor, ImportedAnimatable animatable, int index = -1, bool setAsActiveObject = false, bool addToList=true)
            {
                if (animatable == null) return;
                if (importedObjects == null) importedObjects = new List<ImportedAnimatable>(); 

                if (animatable != null && animatable.instance != null)
                {
                    animatable.instance.SetActive(true);

                    var category = animatable.listCategory == null ? editor.importedAnimatablesList.AddNewCategory(animatable.displayName) : animatable.listCategory;
                    if (category == null)
                    {
                        GameObject.Destroy(animatable.instance);
                    }
                    else
                    {
                        if (addToList)
                        {
                            if (index < 0 || index >= importedObjects.Count)
                            {
                                animatable.index = importedObjects.Count;
                                importedObjects.Add(animatable);
                            }
                            else
                            {
                                animatable.index = index;
                                importedObjects.Insert(index, animatable);
                                RefreshImportedIndices(index);
                            }
                        }

                        animatable.listCategory = category;
                        int sourceCount = animatable.SourceCount;
                        if (sourceCount > 0)
                        {
                            for(int a = 0; a < sourceCount; a++)
                            {
                                var source = animatable.animationBank[a];
                                if (source == null) continue; 

                                source.index = a;
                                //source.CreateListMember(editor, animatable);
                                animatable.AddNewAnimationSource(editor, source, a, false, false); 
                            }
                        }
                        category.Expand();

                        var pointerProxy = category.gameObject.AddOrGetComponent<PointerEventsProxy>();
                        if (pointerProxy.OnRightClick == null) pointerProxy.OnRightClick = new UnityEvent(); else pointerProxy.OnRightClick.RemoveAllListeners();
                        pointerProxy.OnRightClick.AddListener(() =>
                        {
                            // Open context menu when animatable list object is right clicked
                            if (editor.contextMenuMain != null)
                            {
                                editor.OpenContextMenuMain();
                                var canvas = editor.contextMenuMain.GetComponentInParent<Canvas>(true);
                                editor.contextMenuMain.position = canvas.transform.TransformPoint(AnimationCurveEditorUtils.ScreenToCanvasPosition(canvas, CursorProxy.ScreenPosition));

                                var editPhysique = editor.contextMenuMain.FindDeepChildLiberal(_editPhysiqueContextMenuOptionName);
                                if (editPhysique != null)
                                {
                                    editPhysique.gameObject.SetActive(true);
                                    SetButtonOnClickAction(editPhysique, () =>
                                    {
                                        SetActiveObject(editor, animatable.index);
                                        editor.OpenPhysiqueEditor();
                                    });
                                }
                            }

                        });

                        var button = category.gameObject.GetComponent<Button>();
                        var tabButton = category.gameObject.GetComponent<UITabButton>();
                        if (button != null)
                        {
                            if (button.onClick == null) button.onClick = new Button.ButtonClickedEvent();
                            button.onClick.RemoveAllListeners();
                            button.onClick.AddListener(() =>
                            {
                                SetActiveObject(editor, animatable.index);
                            });
                        }
                        if (tabButton != null)
                        {
                            if (tabButton.OnClick == null) tabButton.OnClick = new UnityEvent();
                            tabButton.OnClick.RemoveAllListeners();
                            tabButton.OnClick.AddListener(() =>
                            {
                                SetActiveObject(editor, animatable.index);
                            });
                        }

                        var expand = category.rectTransform.FindDeepChildLiberal("Expand");
                        if (expand != null)
                        {
                            SetButtonOnClickAction(expand, () =>
                            {
                                expand.gameObject.SetActive(false);
                                category.Expand();
                            });
                            expand.gameObject.SetActive(false);
                        }
                        var retract = category.rectTransform.FindDeepChildLiberal("Retract");
                        if (retract != null)
                        {
                            SetButtonOnClickAction(retract, () =>
                            {
                                retract.gameObject.SetActive(false);
                                category.Retract();
                            });
                            expand.gameObject.SetActive(true);
                        }

                        var options = category.rectTransform.FindDeepChildLiberal("Options");
                        if (options != null)
                        {
                            SetButtonOnClickActionByName(options, "Add", () =>
                            {
                                SetActiveObject(editor, animatable.index);
                                editor.OpenNewAnimationWindow();
                            });
                        }

                        if (setAsActiveObject) SetActiveObject(editor, animatable.index, true, false);
                        editor.RefreshAnimatableListUI();
                    }
                }
            }
            public void RefreshImportedIndices(int startIndex = 0)
            {
                if (importedObjects == null) return;

                for (int i = startIndex; i < importedObjects.Count; i++)
                {
                    importedObjects[i].index = i;
                }
            }
            public bool RemoveAnimatable(AnimationEditor editor, ImportedAnimatable animatable, bool destroy, bool undoable = false)
            {
                if (animatable == null || importedObjects == null) return false;

                if (animatable.index < 0 || animatable.index >= importedObjects.Count) animatable.index = importedObjects.IndexOf(animatable);         

                return RemoveAnimatable(editor, animatable.index, destroy, undoable);
            }
            public bool RemoveAnimatable(AnimationEditor editor, int index, bool destroy, bool undoable = false)
            {
                if (importedObjects == null || index < 0 || index >= importedObjects.Count) return false;

                if (editor.IsPlayingPreview) editor.Pause();

                var obj = importedObjects[index];
                if (obj != null)
                {
                    if (obj.listCategory != null) 
                    { 
                        editor.importedAnimatablesList.DeleteCategory(obj.listCategory);
                        obj.listCategory = null;
                    }
                    int sourceCount = obj.SourceCount;
                    if (obj.SourceCount > 0)
                    {
                        for(int a = 0; a < sourceCount; a++)
                        {
                            var source = obj.animationBank[a];
                            if (source == null) continue;

                            source.listMember = null;
                        }
                    }

                    obj.index = -1;
                    if (destroy) 
                    {
                        obj.Destroy();   
                    } 
                    else
                    {
                        if (obj.instance != null) obj.instance.SetActive(false);
                        if (undoable) editor.undoSystem.AddHistory(new UndoableRemoveAnimatable(editor, index, obj));
                    }
                    
                }
                importedObjects.RemoveAt(index);

                RefreshImportedIndices(index);
                if (activeObjectIndex == index) SetActiveObject(editor, index);

                return true;
            }

            public AnimationSource CreateNewAnimationSource(AnimationEditor editor, string animationName, float length) => CreateNewAnimationSource(editor, activeObjectIndex, animationName, length);
            public AnimationSource CreateNewAnimationSource(AnimationEditor editor, int animatableIndex, string animationName, float length)
            {
                if (animatableIndex < 0 || importedObjects == null || animatableIndex >= importedObjects.Count) return null;
                var animatable = importedObjects[animatableIndex];
                if (animatable == null) return null;

                return animatable.CreateNewAnimationSource(editor, animationName, length);
            }

            public AnimationSource LoadNewAnimationSource(AnimationEditor editor, int animatableIndex, string displayName, CustomAnimation animationToLoad)
            {
                if (animatableIndex < 0 || importedObjects == null || animatableIndex >= importedObjects.Count) return null;
                var animatable = importedObjects[animatableIndex];
                if (animatable == null) return null;

                return animatable.LoadNewAnimationSource(editor, displayName, animationToLoad);
            }
            public AnimationSource LoadNewAnimationSource(AnimationEditor editor, int animatableIndex, string animationName) 
            {
                if (animatableIndex < 0 || importedObjects == null || animatableIndex >= importedObjects.Count) return null;
                var animatable = importedObjects[animatableIndex];
                if (animatable == null) return null;

                return animatable.LoadNewAnimationSource(editor, animationName);
            }
            public AnimationSource LoadNewAnimationSource(AnimationEditor editor, int animatableIndex, PackageIdentifier package, string animationName) 
            {
                if (animatableIndex < 0 || importedObjects == null || animatableIndex >= importedObjects.Count) return null;
                var animatable = importedObjects[animatableIndex];
                if (animatable == null) return null;

                return animatable.LoadNewAnimationSource(editor, package, animationName);
            }
            public void AddNewAnimationSource(AnimationEditor editor, int animatableIndex, AnimationSource source)
            {
                if (animatableIndex < 0 || importedObjects == null || animatableIndex >= importedObjects.Count) return;
                var animatable = importedObjects[animatableIndex];
                if (animatable == null) return;

                animatable.AddNewAnimationSource(editor, source);
            }

            public void RemoveAnimationSource(AnimationEditor editor, int animatableIndex, AnimationSource source)
            {
                if (animatableIndex < 0 || importedObjects == null || animatableIndex >= importedObjects.Count) return;
                var animatable = importedObjects[animatableIndex];
                if (animatable == null) return;

                animatable.RemoveAnimationSource(editor, source);
            }
            public void RemoveAnimationSource(AnimationEditor editor, int animatableIndex, int index)
            {
                if (animatableIndex < 0 || importedObjects == null || animatableIndex >= importedObjects.Count) return;
                var animatable = importedObjects[animatableIndex];
                if (animatable == null) return;

                animatable.RemoveAnimationSource(editor, index);
            }

            public void End(AnimationEditor editor)
            {
                if (editor == null) return;

                if (editor.runtimeEditor != null) editor.runtimeEditor.DeselectAll();
                if (editor.importedAnimatablesList != null) editor.importedAnimatablesList.Clear();

                if (importedObjects != null)
                {
                    foreach(var obj in importedObjects)
                    {
                        if (obj == null) continue;

                        if (obj.animationBank != null)
                        {
                            foreach (var source in obj.animationBank) source.listMember = null;
                        }
                        obj.listCategory = null;
                        obj.Destroy();
                    }
                }
            }

        }

        public const string _sessionNameDateFormat = "MM-dd-yyyy_HH-mm";
        public Session StartNewSession(string name = null)
        {
            if (currentSession != null) EndCurrentSession();

            if (string.IsNullOrWhiteSpace(name)) name = DateTime.Now.ToString(_sessionNameDateFormat);
            return currentSession = new Session(name);
        }
        public Session LoadSession(Session.Serialized session) => LoadSession(session.AsOriginalType());
        public Session LoadSession(Session session)
        {
            if (currentSession != null) EndCurrentSession();

            currentSession = session;
            currentSession.RefreshImportedIndices();
            if (currentSession.importedObjects != null)
            {
                for(int a = 0; a < currentSession.importedObjects.Count; a++)
                {
                    var animatable = currentSession.importedObjects[a];
                    if (animatable == null) continue;
                    currentSession.AddAnimatable(this, animatable, a, false, false); // creates list members and listeners
                    //animatable.RebuildController();                 
                }
            }
            SetActiveObject(currentSession.activeObjectIndex, false);
            return currentSession;
        }
        public void EndCurrentSession()
        {
            if (currentSession != null) currentSession.End(this);
            currentSession = null;
        }
        public void MarkSessionDirty()
        {
            if (currentSession != null) currentSession.isDirty = true;
        }

        public static Session LoadSessionFromFile(FileInfo file, SwoleLogger logger = null) => LoadSessionFromFile(file.FullName, logger);
        public static Session LoadSessionFromFile(string path, SwoleLogger logger = null) => LoadSessionFromFileInternal(true, path, logger).GetAwaiter().GetResult();
        public static Task<Session> LoadSessionFromFileAsync(FileInfo file, SwoleLogger logger = null) => LoadSessionFromFileAsync(file.FullName, logger);
        public static Task<Session> LoadSessionFromFileAsync(string path, SwoleLogger logger = null) => LoadSessionFromFileInternal(false, path, logger);
        async static private Task<Session> LoadSessionFromFileInternal(bool sync, string path, SwoleLogger logger = null)
        {
            try
            {
                byte[] data = sync ? File.ReadAllBytes(path) : await File.ReadAllBytesAsync(path);
                ContentManager.TryLoadType<Session>(out Session session, default, data, logger);

                return session;
            } 
            catch(Exception ex)
            {
                logger?.LogError($"Error while attempting to load session from '{path}'");
                logger?.LogError(ex);
            }

            return null;
        }

        public static bool SaveSession(DirectoryInfo dir, Session session, SwoleLogger logger = null) => SaveSession(dir.FullName, session, logger);
        public static bool SaveSession(string directoryPath, Session session, SwoleLogger logger = null) => SaveSessionInternal(true, directoryPath, session, logger).GetAwaiter().GetResult();
        public static Task<bool> SaveSessionAsync(DirectoryInfo dir, Session session, SwoleLogger logger = null) => SaveSessionAsync(dir.FullName, session, logger);
        public static Task<bool> SaveSessionAsync(string directoryPath, Session session, SwoleLogger logger = null) => SaveSessionInternal(false, directoryPath, session, logger);
        async private static Task<bool> SaveSessionInternal(bool sync, string directoryPath, Session session, SwoleLogger logger = null)
        {

            if (session == null)
            {
                logger?.LogWarning($"Tried to save null session to '{directoryPath}'");
                return false; 
            }

            try
            {

                string fullPath = Path.Combine(directoryPath, $"{session.name}.{ContentManager.fileExtension_Generic}");

                string json = swole.Engine.ToJson(session, true);
                byte[] bytes = DefaultJsonSerializer.StringEncoder.GetBytes(json);

                if (sync) File.WriteAllBytes(fullPath, bytes); else await File.WriteAllBytesAsync(fullPath, bytes);
                return true;

            }
            catch (Exception ex)
            {

                logger?.LogError($"Error while attempting to save session '{session.name}' to '{directoryPath}'");
                logger?.LogError(ex);

            }
            return false;
        }

        public const float maxAnimationLength = 600;
        public void OpenNewAnimationWindow()
        {
            if (newAnimationWindow == null) return;

            InputField[] fields = newAnimationWindow.gameObject.GetComponentsInChildren<InputField>(true);
            foreach (var field in fields) if (field != null && field.contentType != InputField.ContentType.DecimalNumber && field.contentType != InputField.ContentType.IntegerNumber) field.text = string.Empty;

            TMP_InputField[] fieldsTMP = newAnimationWindow.gameObject.GetComponentsInChildren<TMP_InputField>(true);
            foreach (var field in fieldsTMP) if (field != null && field.contentType != TMP_InputField.ContentType.DecimalNumber && field.contentType != TMP_InputField.ContentType.IntegerNumber) field.text = string.Empty;

            UIPopupMessageFadable errorMessage = newAnimationWindow.GetComponentInChildren<UIPopupMessageFadable>(true);
            UIPopup popup = newAnimationWindow.GetComponentInChildren<UIPopup>(true);

            var create = newAnimationWindow.FindDeepChildLiberal("Create");
            void Create()
            {
                var dropdown = newAnimationWindow.FindDeepChildLiberal("SourceDropDown");
                if (dropdown != null)
                {
                    var selection = dropdown.FindDeepChildLiberal("Current-Text");
                    if (selection != null)
                    {
                        var selectionText = GetComponentText(selection);
                        if (selectionText == "new")
                        {
                            var selectionRoot = newAnimationWindow.FindDeepChildLiberal("NewSource");
                            if (selectionRoot != null)
                            {
                                string animName = "null";
                                float animLength = 1f;

                                var name = selectionRoot.FindDeepChildLiberal("Name");
                                if (name != null) animName = GetInputFieldText(name);
                                var length = selectionRoot.FindDeepChildLiberal("LengthInput");
                                if (length != null)
                                {
                                    string animLengthText = GetInputFieldText(length);
                                    if (!float.TryParse(animLengthText, out animLength)) animLength = 1f;
                                }

                                if (string.IsNullOrWhiteSpace(animName))
                                {
                                    if (errorMessage != null) errorMessage.SetMessage("Animation name cannot be empty!").SetDisplayTime(errorMessage.DefaultDisplayTime).Show();
                                    return;
                                }
                                else if (animLength <= 0)
                                {
                                    if (errorMessage != null) errorMessage.SetMessage("Animation length must be greater than zero!").SetDisplayTime(errorMessage.DefaultDisplayTime).Show();
                                    return;
                                }
                                else if (animLength > maxAnimationLength)
                                {
                                    if (errorMessage != null) errorMessage.SetMessage("Animation length cannot be greater than 600 (10 minutes)!").SetDisplayTime(errorMessage.DefaultDisplayTime).Show();
                                    return;
                                }

                                var source = CreateNewAnimationSource(animName, animLength);
                                if (source == null)
                                {
                                    if (errorMessage != null) errorMessage.SetMessage("An unkown error occurred").SetDisplayTime(errorMessage.DefaultDisplayTime).Show();
                                    return;
                                }
                            }
                        }
                        else if (selectionText == "import")
                        {
                            string packageString = string.Empty;
                            string animName = string.Empty;

                            var selectionRoot = newAnimationWindow.FindDeepChildLiberal("ImportSource");
                            if (selectionRoot != null)
                            {
                                var name = selectionRoot.FindDeepChildLiberal("Name");
                                if (name != null) animName = GetInputFieldText(name);
                                var package = selectionRoot.FindDeepChildLiberal("Package");
                                if (package != null) packageString = GetInputFieldText(package);

                                if (string.IsNullOrWhiteSpace(animName))
                                {
                                    if (errorMessage != null) errorMessage.SetMessage("Animation name cannot be empty!").SetDisplayTime(errorMessage.DefaultDisplayTime).Show();
                                    return;
                                }

                                if (!LoadNewAnimationSource(animName, out AnimationSource source, new PackageIdentifier(packageString)) || source == null)
                                {
                                    if (errorMessage != null) errorMessage.SetMessage($"Failed to load animation '{animName}'{(string.IsNullOrEmpty(packageString) ? "" : $" from package '{packageString}'")}!").SetDisplayTime(errorMessage.DefaultDisplayTime).Show();
                                    return;
                                }
                            }
                        }
                    }
                }

                popup.Close();
            }
            SetButtonOnClickAction(create, Create);

            newAnimationWindow.gameObject.SetActive(true);
            newAnimationWindow.SetAsLastSibling();
            popup?.Elevate();
        }

        public void ImportNewAnimatable(string id, bool setAsActiveObject = true, bool undoable=false)
        {
            if (currentSession == null) currentSession = StartNewSession();
            currentSession.ImportNewAnimatable(this, id, setAsActiveObject, undoable);
        }
        public void ImportNewAnimatable(AnimatableAsset animatable, bool setAsActiveObject = true, bool undoable=false)
        {
            if (currentSession == null) currentSession = StartNewSession();
            currentSession.ImportNewAnimatable(this, animatable, setAsActiveObject, undoable);
        }
        public void AddAnimatable(ImportedAnimatable animatable, int index = -1, bool setAsActiveObject = false, bool addToList=true)
        {
            if (currentSession == null) currentSession = StartNewSession();
            currentSession.AddAnimatable(this, animatable, index, setAsActiveObject, addToList);
        }
        public void RefreshImportedIndices(int startIndex = 0)
        {
            if (currentSession == null) currentSession = StartNewSession();
            currentSession.RefreshImportedIndices(startIndex);
        }
        public bool RemoveAnimatable(ImportedAnimatable animatable, bool destroy, bool undoable=false)
        {
            if (currentSession == null) return false;
            return currentSession.RemoveAnimatable(this, animatable, destroy, undoable);
        }
        public bool RemoveAnimatable(int index, bool destroy, bool undoable = false)
        {
            if (currentSession == null) return false;
            return currentSession.RemoveAnimatable(this, index, destroy, undoable);
        }

        protected Session currentSession;
        public Session CurrentSession => currentSession;

        public void SetActiveObject(int index, bool skipIfAlreadyActive = true, bool updateUI = true)
        {
            if (currentSession == null) return;
            currentSession.SetActiveObject(this, index, skipIfAlreadyActive, updateUI);
        }

        protected readonly List<GameObject> tempGameObjects = new List<GameObject>();
        /// <summary>
        /// Called by the current session whenever the active object is changed
        /// </summary>
        protected void OnSetActiveObject(ImportedAnimatable activeObj) 
        {
            if (physiqueEditorWindow != null)
            {
                if (activeObj == null || activeObj.instance == null)
                {
                    physiqueEditorWindow.gameObject.SetActive(false);
                } 
                else if (physiqueEditorWindow.gameObject.activeSelf)
                {
                    var character = activeObj.instance.GetComponentInChildren<MuscularRenderedCharacter>(true);
                    if (character == null)
                    {
                        physiqueEditorWindow.gameObject.SetActive(false);
                    } 
                    else 
                    {
                        var editor = PhysiqueEditor;
                        if (editor != null) editor.Character = character;
                    }
                }
            }

            if (activeObj != null)
            {
                if (activeObj.selectedBoneIndices != null)
                {
                    tempGameObjects.Clear(); 
                    foreach (var selectedBoneIndex in activeObj.selectedBoneIndices) if (selectedBoneIndex >= 0 && selectedBoneIndex < poseableBones.Count && poseableBones[selectedBoneIndex].transform != null) tempGameObjects.Add(poseableBones[selectedBoneIndex].transform.gameObject);

                    runtimeEditor.DeselectAll();
                    runtimeEditor.Select(tempGameObjects);

                }
            }

            RefreshTimeline();
        }

        protected const string _activeTag = "active";
        protected const string _activeMainTag = "activemain";
        protected const string _syncedTag = "synced";
        protected const string _outofsyncTag = "outofsync";
        protected const string _outdatedTag = "outdated";
        protected const string _unboundTag = "unbound";
        public void RefreshAnimatableListUI()
        {
            if (importedAnimatablesList == null) return;

            for (int a = 0; a < importedAnimatablesList.CategoryCount; a++)
            {
                var category = importedAnimatablesList[a];
                if (category == null || category.rectTransform == null) continue;

                var childT = category.rectTransform.FindDeepChildLiberal(_activeTag);
                if (childT != null) childT.gameObject.SetActive(false); 

                if (category.members == null) continue;
                for (int b = 0; b < category.members.Count; b++)
                {
                    var member = category.members[b];
                    if (member == null || member.rectTransform == null) continue;

                    childT = member.rectTransform.FindDeepChildLiberal(_activeTag);
                    if (childT != null) childT.gameObject.SetActive(false);

                    childT = member.rectTransform.FindDeepChildLiberal(_activeMainTag);
                    if (childT != null) childT.gameObject.SetActive(false);

                    childT = member.rectTransform.FindDeepChildLiberal(_syncedTag);
                    if (childT != null) childT.gameObject.SetActive(false);
                    childT = member.rectTransform.FindDeepChildLiberal(_outofsyncTag);
                    if (childT != null) childT.gameObject.SetActive(false);
                    childT = member.rectTransform.FindDeepChildLiberal(_outdatedTag);
                    if (childT != null) childT.gameObject.SetActive(false);
                    childT = member.rectTransform.FindDeepChildLiberal(_unboundTag);
                    if (childT != null) childT.gameObject.SetActive(true); 
                }
            }

            if (currentSession != null && currentSession.importedObjects != null)
            {
                for (int a = 0; a < currentSession.importedObjects.Count; a++)
                {
                    var obj = currentSession.importedObjects[a];
                    if (obj == null) continue;

                    if (obj.animationBank != null)
                    {
                        for (int b = 0; b < obj.animationBank.Count; b++)
                        {
                            var source = obj.animationBank[b];
                            if (source == null) continue;

                            source.SetMix(source.previewLayer == null ? 1 : source.previewLayer.mix);

                            if (source.listMember == null || source.listMember.rectTransform == null) continue;
                            var rT = source.listMember.rectTransform;
                            Transform childT;

                            if (swole.FindContentPackage(source.package, out ContentPackage pkg, out _) == swole.PackageActionResult.Success) // Change icon of save button based on the source's export state
                            {
                                void IsSynced()
                                {
                                    childT = rT.FindDeepChildLiberal(_syncedTag); // Assets are synced
                                    if (childT != null) childT.gameObject.SetActive(true);
                                }
                                void IsNotSynced()
                                {
                                    childT = rT.FindDeepChildLiberal(_outofsyncTag); // Changes need to be synced
                                    if (childT != null) childT.gameObject.SetActive(true);
                                }
                                void IsOutdated()
                                {
                                    childT = rT.FindDeepChildLiberal(_outdatedTag); // Linked content is newer
                                    if (childT != null) childT.gameObject.SetActive(true);
                                }

                                if (string.IsNullOrWhiteSpace(source.syncedName)) source.syncedName = source.DisplayName;

                                if (pkg.TryFind<CustomAnimation>(out var linkedAnim, source.syncedName))
                                {
                                    EvaluateAnimationAssetLink(source, linkedAnim, IsSynced, IsNotSynced, IsOutdated);
                                }
                                else
                                {
                                    IsNotSynced();
                                }

                                childT = rT.FindDeepChildLiberal(_unboundTag);
                                if (childT != null) childT.gameObject.SetActive(false);
                            }

                        }
                    }

                    var activeSource = obj.CurrentSource;
                    if (activeSource == null || activeSource.listMember == null || activeSource.listMember.rectTransform == null) continue; 

                    var active = activeSource.listMember.rectTransform.FindDeepChildLiberal(_activeTag);
                    if (active != null) active.gameObject.SetActive(true);
                }

                var activeObj = currentSession.ActiveObject;
                if (activeObj != null && activeObj.listCategory != null)
                {
                    var active = activeObj.listCategory.rectTransform == null ? null : activeObj.listCategory.rectTransform.FindDeepChildLiberal(_activeTag);
                    if (active != null) active.gameObject.SetActive(true);

                    var activeAnim = activeObj.CurrentSource;
                    if (activeAnim != null && activeAnim.listMember != null)
                    {
                        active = activeAnim.listMember.rectTransform == null ? null : activeAnim.listMember.rectTransform.FindDeepChildLiberal(_activeMainTag);
                        if (active != null) active.gameObject.SetActive(true);
                    }

                }
            }
        }

        protected void DeselectAll()
        {
            runtimeEditor.DeselectAll();
        }

        protected void OnSelectionChange(List<GameObject> fullSelection, List<GameObject> newlySelected, List<GameObject> deselected)
        {
            if (newlySelected != null && newlySelected.Count > 0)
            {
                GameObject lastSelected = null;
                int i = newlySelected.Count - 1;
                while(lastSelected == null && i >= 0)
                {
                    lastSelected = newlySelected[i];
                    i--;
                }
                if (lastSelected != null)
                {
                    int boneIndex = FindPoseableBoneIndex(lastSelected); 
                    if (boneIndex >= 0)
                    {
                        selectedBoneUI = boneIndex;
                        RefreshTimeline(); 
                    }
                }
            }

            var activeObj = ActiveAnimatable;
            if (activeObj != null && fullSelection != null)
            {
                if (activeObj.selectedBoneIndices == null) activeObj.selectedBoneIndices = new List<int>();
                activeObj.selectedBoneIndices.Clear();

                foreach (var selectedObj in fullSelection)
                {
                    int boneIndex = FindPoseableBoneIndex(selectedObj);
                    if (boneIndex >= 0)
                    {
                        activeObj.selectedBoneIndices.Add(boneIndex);
                    }
                }
            }
        }

        private readonly HashSet<GameObject> objectSetA = new HashSet<GameObject>();
        protected void OnPreSelectCustomize(int selectReason, List<GameObject> toSelect, List<GameObject> toIgnore)
        {

            objectSetA.Clear();
            
            foreach(var obj in toSelect)
            {
                if (obj == null) continue;

                var proxy = obj.GetComponent<SelectionProxy>();
                if (proxy == null && obj.transform.parent != null) proxy = obj.transform.parent.GetComponent<SelectionProxy>();
                if (proxy != null)
                {
                    if (proxy.includeSelf) objectSetA.Add(obj);
                    foreach (var selected in proxy.toSelect) objectSetA.Add(selected);
                } 
                else
                {
                    objectSetA.Add(obj); 
                }
            }

            toSelect.Clear();
            toSelect.AddRange(objectSetA);
        }

        public class PoseableBone
        {
            public int parent;
            public Transform transform;
            public Transform rootSelectable;
            public ChildBone[] children;
        }

        public struct ChildBone
        {
            public Transform transform;
            public Transform leafSelectable;
        }

        protected void AlignLeafTransform(Transform leaf, Transform childBone)
        {
            leaf.localPosition = Vector3.zero;
            leaf.localRotation = Quaternion.FromToRotation(Vector3.up, childBone.localPosition.normalized);
            leaf.localScale = new Vector3(1, childBone.localPosition.magnitude, 1);
        }

        private readonly List<PoseableBone> poseableBones = new List<PoseableBone>();
        public int FindPoseableBoneIndex(string boneName)
        {
            for (int i = 0; i < poseableBones.Count; i++) if (poseableBones[i].transform.name == boneName) return i;
            boneName = boneName.AsID();
            for (int i = 0; i < poseableBones.Count; i++) if (poseableBones[i].transform.name.AsID() == boneName) return i;
            return -1;
        }
        public int FindPoseableBoneIndex(GameObject obj) => FindPoseableBoneIndex(obj.transform);
        public int FindPoseableBoneIndex(Transform boneTransform)
        {
            for(int i = 0; i < poseableBones.Count; i++) if (poseableBones[i].transform == boneTransform || poseableBones[i].rootSelectable == boneTransform) return i;
            return -1;
        }
        public PoseableBone FindPoseableBone(Transform boneTransform)
        {
            int i = FindPoseableBoneIndex(boneTransform);
            return i < 0 ? null : poseableBones[i];
        }
        private static readonly List<GameObject> _boneQuery = new List<GameObject>();
        public int SelectedBoneCount
        {
            get
            {
                if (runtimeEditor == null) return 0;
                _boneQuery.Clear();
                runtimeEditor.QueryActiveSelection(_boneQuery);

                int count = 0; 
                foreach (var bone in _boneQuery) if (bone != null && FindPoseableBoneIndex(bone) >= 0) count++;
                _boneQuery.Clear();

                return count;
            }
        }
        private readonly List<GameObject> visibleRootBones = new List<GameObject>();
        private readonly List<GameObject> visibleLeafBones = new List<GameObject>();

        public void IgnorePreviewObjects()
        {
            if (currentSession == null) return;
#if BULKOUT_ENV
            var rtScene = RTScene.Get;
            if (rtScene != null)
            {
                if (currentSession.importedObjects != null)
                {
                    foreach (var obj in currentSession.importedObjects)
                    {
                        if (obj.animator == null) continue;
                        rtScene.SetRootObjectIgnored(obj.animator.gameObject, true); // Make RLD ignore the animation bones      
                    }
                }
            }
#endif
        }
        public void StopIgnoringPreviewObjects()
        {
            if (currentSession == null) return;
#if BULKOUT_ENV
            var rtScene = RTScene.Get;
            if (rtScene != null)
            {
                if (currentSession.importedObjects != null)
                {
                    foreach (var obj in currentSession.importedObjects)
                    {
                        if (obj.animator == null) continue;
                        rtScene.SetRootObjectIgnored(obj.animator.gameObject, false); // Make RLD no longer ignore the animation bones      
                    }
                }
            }
#endif
        }

        protected CustomAnimator activeAnimator;
        public CustomAnimator ActiveAnimator => activeAnimator;

        public void SetActiveAnimator(CustomAnimator animator)
        {
            if (activeAnimator != animator)
            {
                foreach (GameObject obj in visibleRootBones) if (obj != null)
                    {
                        boneRootPool.Release(obj);
                        obj.transform.SetParent(null);
                        obj.SetActive(false);
                    }
                foreach (GameObject obj in visibleLeafBones) if (obj != null)
                    {
                        boneLeafPool.Release(obj);
                        obj.transform.SetParent(null);
                        obj.SetActive(false);
                    }

                visibleRootBones.Clear();
                visibleLeafBones.Clear();
            }
            else return;

            activeAnimator = animator;
            poseableBones.Clear();

            if (animator != null && animator.Bones != null)
            {

                var bones = animator.Bones.bones;
                if (bones != null)
                {

                    for(int i = 0; i < bones.Length; i++)
                    {
                        var bone = bones[i];
                        if (bone == null || !animator.avatar.TryGetBoneInfo(bone.name, out var boneInfo)) continue;

                        if (boneRootPool.TryGetNewInstance(out GameObject rootInst))
                        {
                            var parentBoneInfo = PoseableRig.BoneInfo.GetDefault(string.Empty);
                            var parentBone = bone.parent;
                            while (parentBone != null && parentBoneInfo.isDefault)
                            {
                                animator.avatar.TryGetBoneInfo(parentBone.name, out parentBoneInfo);
                                parentBone = parentBone.parent;
                            }

                            var proxy = rootInst.AddOrGetComponent<SelectionProxy>();
                            proxy.includeSelf = false;
                            if (proxy.toSelect == null) proxy.toSelect = new List<GameObject>();
                            proxy.toSelect.Clear();
                            proxy.toSelect.Add(bone.gameObject);

                            visibleRootBones.Add(rootInst);
                            rootInst.SetLayerAllChildren(boneOverlayLayer); 

                            var rootT = rootInst.transform;
                            rootT.SetParent(bone, false);
                            rootT.localPosition = Vector3.zero;
                            rootT.localRotation = Quaternion.identity;
                            float scale = parentBoneInfo.childScale * (boneInfo.isDefault ? parentBoneInfo.scale : boneInfo.scale);
                            rootT.localScale = new Vector3(scale, scale, scale);

                            boneInfo.dontDrawConnection = boneInfo.isDefault ? parentBoneInfo.dontDrawConnection : boneInfo.dontDrawConnection;

                            ChildBone[] children = null;
                            if (!boneInfo.dontDrawConnection)
                            { 
                                children = new ChildBone[bone.childCount]; 
                                for (int j = 0; j < bone.childCount; j++)
                                {
                                    var childBone = bone.GetChild(j);
                                    if (childBone == null || !animator.avatar.IsPoseableBone(childBone.name)) continue;

                                    if (boneLeafPool.TryGetNewInstance(out GameObject leafInst))
                                    {
                                        var proxyLeaf = leafInst.AddOrGetComponent<SelectionProxy>();
                                        proxyLeaf.includeSelf = false;
                                        if (proxyLeaf.toSelect == null) proxyLeaf.toSelect = new List<GameObject>();
                                        proxyLeaf.toSelect.Clear();
                                        proxyLeaf.toSelect.Add(bone.gameObject);

                                        visibleLeafBones.Add(leafInst);
                                        leafInst.SetLayerAllChildren(boneOverlayLayer);

                                        var leafT = leafInst.transform;
                                        leafT.SetParent(bone, false);
                                        AlignLeafTransform(leafT, childBone);

                                        children[j] = new ChildBone() { transform = childBone, leafSelectable = leafT };
                                    }
                                }
                            }
                            poseableBones.Add(new PoseableBone() { rootSelectable = rootT, transform = bone, children = children }); 
                        }
                    }
                }

            }

            RefreshBoneKeyDropdowns();
            RefreshBoneCurveDropdowns();
            RefreshTimeline();
        }

        public AnimationSource CreateNewAnimationSource(string animationName, float length)
        {
            if (currentSession == null) return null;
            return currentSession.CreateNewAnimationSource(this, animationName, length);
        }
        public bool LoadNewAnimationSource(string animationName, out AnimationSource source, PackageIdentifier package=default)
        {
            source = null;
            if (currentSession == null) return false;

            source = currentSession.LoadNewAnimationSource(this, currentSession.activeObjectIndex, package, animationName);
            return source != null;
        }

        public void RemoveAnimationSource(int animatableIndex, AnimationSource source)
        {
            if (currentSession == null) return;
            currentSession.RemoveAnimationSource(this, animatableIndex, source);
        }
        public void RemoveAnimationSource(int animatableIndex, int index)
        {
            if (currentSession == null) return;
            currentSession.RemoveAnimationSource(this, animatableIndex, index);
        }

        public void SetVisibilityOfObject(int index, bool visible)
        {
            if (currentSession == null) return;
            currentSession.SetVisibilityOfObject(this, index, visible);
        }
        public void SetVisibilityOfObject(ImportedAnimatable obj, bool visible)
        {
            if (currentSession == null) return;
            currentSession.SetVisibilityOfObject(this, obj, visible);
        }
        public void SetVisibilityOfSource(int animatableIndex, int sourceIndex, bool visible)
        {
            if (currentSession == null) return;
            currentSession.SetVisibilityOfSource(this, animatableIndex, sourceIndex, visible);
        }
        public void SetVisibilityOfSource(ImportedAnimatable obj, int sourceIndex, bool visible)
        {
            if (currentSession == null) return;
            currentSession.SetVisibilityOfSource(this, obj, sourceIndex, visible);
        }
        public void SetVisibilityOfSource(ImportedAnimatable obj, AnimationSource source, bool visible)
        {
            if (currentSession == null) return;
            currentSession.SetVisibilityOfSource(this, obj, source, visible);  
        }

        #region Behaviour Loop

        public bool testSave;
        public bool testLoad;
        public string testPath;

        public bool testPlayback;
        public bool testPause;
        public bool testStop;
        public bool testScrub;
        public bool testInsert;
        public PlayMode testMode;

        public bool test;
        public bool testPlay;
        public bool testRunAnimator;

        public float testTime;

        public CustomAnimation testAnimation;

        private AnimationUtils.Pose restPose;
        private AnimationUtils.Pose lastPose;

        private CustomAnimationController testController;
        private bool playFlag;
        public void OnUpdate()
        {
            if (string.IsNullOrWhiteSpace(testPath)) testPath = Application.persistentDataPath;
            if (currentSession != null && testSave)
            {
                testSave = false;
                if (!string.IsNullOrEmpty(testPath))
                {
                    SaveSession(testPath, currentSession, swole.DefaultLogger);
                }
            }
            if (testLoad)
            {
                testLoad = false;
                if (!string.IsNullOrEmpty(testPath))
                {
                    var sesh = LoadSessionFromFile(testPath, swole.DefaultLogger);
                    if (sesh != null)
                    {
                        LoadSession(sesh); 
                    }
                }
            }

            if (testPlayback)
            {
                testPlayback = false;
                SetPlaybackMode(testMode); 
                Play(); 
            }
            if (testPause)
            {
                testPause = false;
                Pause();
            }
            if (testStop)
            {
                testStop = false;
                Stop();
            }
            if (testScrub)
            {
                testScrub = false;
                SetPlaybackMode(testMode);
                PlaySnapshot(testTime);
            }
            if (testInsert)
            {
                testInsert = false;
                var activeSource = CurrentSource;
                if (activeSource != null) activeSource.InsertKeyframes(true, true, ActiveAnimatable, testTime, (timelineWindow == null ? null : timelineWindow.CalculateFrameAtTimelinePosition));
            }
            Step();
            //
            if (test)
            {
                test = false;

                OpenAnimationEventsWindow(animationEventsWindow); 
                 
                /*var pose = new AnimationUtils.Pose(activeAnimator);
                if (restPose == null) restPose = pose;
                if (testAnimation == null) testAnimation = new CustomAnimation();

                pose.Insert(testAnimation, testTime, restPose, lastPose);
                lastPose = pose;
                testTime += 1;*/
            }
            if (testPlay)
            {
                testPlay = false;
                if (testAnimation != null && activeAnimator != null)
                {

                    if (testController != null) activeAnimator.RemoveControllerData(testController);
                    testAnimation.FlushJobData();

                    testController = ScriptableObject.CreateInstance<CustomAnimationController>();
                    testController.name = "test";
                    testController.animationReferences = new CustomMotionController.AnimationReference[] { new CustomMotionController.AnimationReference("testAnim", testAnimation, AnimationLoopMode.Loop) };

                    CustomAnimationLayer testLayer = new CustomAnimationLayer();
                    testLayer.name = "layer1";
                    testLayer.blendParameterIndex = -1;
                    testLayer.entryStateIndex = 0; 
                    testLayer.mix = 1;
                    testLayer.motionControllerIdentifiers = new MotionControllerIdentifier[] { new MotionControllerIdentifier() { index=0,type= MotionControllerType.AnimationReference } };
                    testLayer.IsAdditive = false;

                    CustomStateMachine testStateMachine = new CustomStateMachine() { name="testMachine", motionControllerIndex = 0 };

                    testLayer.stateMachines = new CustomStateMachine[] { testStateMachine };

                    testController.layers = new CustomAnimationLayer[] { testLayer };

                    activeAnimator.ApplyController(testController); 

                }
            }
            if (testRunAnimator)
            {
                if (!playFlag)
                {
                    playFlag = true;
                    RTScene.Get.SetRootObjectIgnored(activeAnimator.gameObject, true);
                }
                activeAnimator.UpdateStep(Time.deltaTime);
            } else if (playFlag)
            {
                playFlag = false;
                RTScene.Get.SetRootObjectIgnored(activeAnimator.gameObject, false);
            }
        }

        public virtual void OnLateUpdate()
        {
            LateStep();
            //
            if (testRunAnimator)
            {
                activeAnimator.LateUpdateStep(Time.deltaTime);
            }
            if (floorTransform != null)
            {
                Vector3 floorPos = floorTransform.position;

                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    Vector3 camPos = mainCam.transform.position;
                    floorPos.x = camPos.x;
                    floorPos.z = camPos.z;
                }

                floorTransform.position = floorPos;
            }

            if (clickableOptions != null) foreach (var option in clickableOptions) if (option != null) option.Refresh(this);
        }

        public virtual void OnFixedUpdate(){}

        #endregion

        public ImportedAnimatable ActiveAnimatable
        {
            get
            {
                if (currentSession == null) return null;
                return currentSession.ActiveObject;
            }
        }
        public AnimationSource CurrentSource
        {
            get
            {
                var activeObj = ActiveAnimatable;
                if (activeObj == null) return null;

                return activeObj.CurrentSource;
            }
        }
        public CustomAnimation CurrentCompiledAnimation
        {
            get
            {
                var activeObj = ActiveAnimatable;
                if (activeObj == null) return null;

                return activeObj.CurrentCompiledAnimation;
            }
        }

        public CustomStateMachine ActivePreviewState
        {
            get
            {
                var source = CurrentSource;
                if (source == null) return null;

                return source.PreviewState;  
            }
        }

        [Serializable]
        public enum PlayMode
        {
            Active_Only, Last_Edited, All, Active_All
        }

        protected PlayMode playbackMode;
        public PlayMode PlaybackMode => playbackMode;

        public void SetPlaybackMode(int mode) => SetPlaybackMode((PlayMode)mode);
        public void SetPlaybackMode(PlayMode mode)
        {
            if (isPlaying) Pause(); 

            playbackMode = mode;
 
            if (playModeDropdownRoot != null)
            {
                SetComponentTextByName(playModeDropdownRoot, _currentTextObjName, playbackMode.ToString().ToLower());
            }
        }
        public void SetPlaybackActiveOnly() => SetPlaybackMode(PlayMode.Active_Only);
        public void SetPlaybackLastEdited() => SetPlaybackMode(PlayMode.Last_Edited); 
        public void SetPlaybackAll() => SetPlaybackMode(PlayMode.All);
        public void SetPlaybackActiveAll() => SetPlaybackMode(PlayMode.Active_All);

        [SerializeField]
        private float playbackPosition;
        public void SetPlaybackPosition(float pos)
        {
            playbackPosition = pos;
            if (timelineWindow != null) // Update timeline UI
            { 
                timelineWindow.SetScrubPosition(playbackPosition, false);
                timelineWindow.SetNonlinearScrubPosition(timelineWindow.GetNonlinearTimeFromTime(playbackPosition), false);
            } 
        }
        public float PlaybackPosition
        {
            get => playbackPosition;
            set => SetPlaybackPosition(value);
        }

        public float playbackSpeed = 1;

        private bool isPlaying;
        public bool IsPlayingPreview => isPlaying;

        private bool loop;
        public bool IsLooping 
        {
            get => loop;
            set => SetLooping(value);
        }
        public void ToggleLooping() => SetLooping(!loop);
        public void SetLooping(bool looping)
        {
            loop = looping;
            if (loopButton != null)
            {
                var inactive = loopButton.transform.FindDeepChildLiberal("inactive");
                var active = loopButton.transform.FindDeepChildLiberal("active");

                if (loop)
                {
                    inactive.gameObject.SetActive(false);
                    active.gameObject.SetActive(true);
                }
                else
                {
                    inactive.gameObject.SetActive(true);
                    active.gameObject.SetActive(false);
                }
            }
        }
        private bool record;
        public bool IsRecording 
        {
            get => record;
            set => SetRecording(value); 
        }
        public void ToggleRecording() => SetRecording(!record);
        public void SetRecording(bool recording)
        {
            record = recording;
            if (recordButton != null)
            {
                var inactive = recordButton.transform.FindDeepChildLiberal("inactive");
                var active = recordButton.transform.FindDeepChildLiberal("active");

                if (record)
                {
                    inactive.gameObject.SetActive(false);
                    active.gameObject.SetActive(true);
                }
                else
                {
                    inactive.gameObject.SetActive(true);
                    active.gameObject.SetActive(false);
                }
            }
        }

        public void PlayOnce()
        {
            var activeAnim = CurrentSource;
            if (activeAnim != null) PlaybackPosition = activeAnim.playbackPosition;

            Play(false, PlaybackPosition);
        }
        public void PlayLooped() 
        {
            var activeAnim = CurrentSource;
            if (activeAnim != null) PlaybackPosition = activeAnim.playbackPosition;  

            Play(true, PlaybackPosition); 
        }
        private readonly List<SourceCollection> toCompile = new List<SourceCollection>();
        protected void CompileSourcesForPlayback(CompilationDelegate OnComplete = null, CompilationDelegate OnFail = null, CompilationDelegate OnBusy = null)
        {
            if (IsCompiling || IsPlayingPreview) 
            {
                OnBusy?.Invoke();
                return;
            }

            toCompile.Clear();
            if (currentSession.importedObjects != null)
            {
                var activeObj = currentSession.ActiveObject;
                if (activeObj != null) activeObj.SetVisibility(this, true);
                switch (playbackMode)
                {
                    case PlayMode.All:
                        foreach (var obj in currentSession.importedObjects)
                        {
                            if (obj == null || !obj.visible || obj.animationBank == null) continue;
                            if (obj.compilationList == null) obj.compilationList = new List<AnimationSource>();
                            obj.compilationList.Clear();
                            SourceCollection collection = new SourceCollection() { name = obj.displayName, sources = obj.compilationList };
                            foreach (var anim in obj.animationBank)
                            {
                                if (anim == null || !anim.visible) continue;
                                anim.previewAnimator = obj.animator;
                                obj.compilationList.Add(anim);
                            }
                            if (obj.compilationList.Count > 0) toCompile.Add(collection);
                        }
                        break;
                    case PlayMode.Last_Edited:
                        foreach (var obj in currentSession.importedObjects)
                        {
                            if (obj == null || !obj.visible || obj.animationBank == null) continue;
                            if (obj.compilationList == null) obj.compilationList = new List<AnimationSource>();
                            obj.compilationList.Clear();
                            SourceCollection collection = new SourceCollection() { name = obj.displayName, sources = obj.compilationList };
                            if (obj.editIndex >= 0 && obj.editIndex < obj.animationBank.Count) 
                            {
                                var anim = obj.animationBank[obj.editIndex];
                                anim.previewAnimator = obj.animator;
                                obj.compilationList.Add(anim);
                            }
                            if (obj.compilationList.Count > 0) toCompile.Add(collection);
                        }
                        break;
                    case PlayMode.Active_All:
                        if (activeObj != null && activeObj.animationBank != null)
                        {
                            if (activeObj.compilationList == null) activeObj.compilationList = new List<AnimationSource>();
                            activeObj.compilationList.Clear();
                            SourceCollection collection = new SourceCollection() { name = activeObj.displayName, sources = activeObj.compilationList };
                            foreach (var anim in activeObj.animationBank)
                            {
                                if (anim == null || !anim.visible) continue;
                                anim.previewAnimator = activeObj.animator;
                                activeObj.compilationList.Add(anim);
                            }
                            if (activeObj.compilationList.Count > 0) toCompile.Add(collection);
                        }
                        break;
                    case PlayMode.Active_Only:
                        if (activeObj != null && activeObj.animationBank != null)
                        {
                            if (activeObj.compilationList == null) activeObj.compilationList = new List<AnimationSource>();
                            activeObj.compilationList.Clear();
                            SourceCollection collection = new SourceCollection() { name = activeObj.displayName, sources = activeObj.compilationList };
                            if (activeObj.editIndex >= 0 && activeObj.editIndex < activeObj.animationBank.Count) 
                            {
                                var anim = activeObj.animationBank[activeObj.editIndex];
                                anim.previewAnimator = activeObj.animator;
                                activeObj.compilationList.Add(anim); 
                            }
                            if (activeObj.compilationList.Count > 0) toCompile.Add(collection);
                        }
                        break;
                }
            }

            if (!CompileAll(toCompile, null, false, OnComplete, OnFail)) OnBusy?.Invoke(); 
        }
        private void ArrangePreviewLayers()
        {
            if (currentSession != null && currentSession.importedObjects != null)
            {
                void ArrangeActiveSourceForObject(ImportedAnimatable obj, bool useRestPoseAsBase = true)
                {
                    if (obj == null || !obj.visible || obj.animator == null) return;

                    obj.RebuildControllerIfNull();

                    var source = obj.CurrentSource;
                    if (source != null && source.previewLayer != null) 
                    { 
                        source.previewLayer.IsAdditive = useRestPoseAsBase;
                        //if (useRestPoseAsBase) source.previewLayer.mix = source.GetMixFromSlider(); else source.previewLayer.mix = 1;
                        source.previewLayer.mix = source.GetMixFromSlider(); 
                    }

                    obj.RemoveRestPoseAnimationFromAnimator();
                    if (useRestPoseAsBase)
                    {
                        obj.AddRestPoseAnimationToAnimator();
                        var rpLayer = obj.GetRestPoseAnimationLayer(); 
                        if (rpLayer != null)
                        {
                            rpLayer.IsAdditive = false;
                            rpLayer.mix = 1;
                            rpLayer.RearrangeNoRemap(0); // Make the rest pose base layer the first layer to get evaluated in the animation player.
                        }
                        else
                        {
                            obj.animator.RecalculateLayerIndicesNoRemap();
                        }
                    } 
                }
                void ArrangeAllSourcesForObject(ImportedAnimatable obj)
                {
                    if (obj == null || !obj.visible || obj.animator == null) return;

                    obj.RebuildControllerIfNull();

                    if (obj.animationBank != null)
                    {
                        for (int a = 0; a < obj.animationBank.Count; a++)
                        {
                            var source = obj.animationBank[a];
                            if (source == null || source.previewLayer == null) continue;
                            source.previewLayer.IsAdditive = true;
                            source.previewLayer.mix = source.GetMixFromSlider();
                            source.previewLayer.RearrangeNoRemap(a, false); // Make the preview layer arrangement match the arrangement of the ui list in the editor. Animations placed lower in the list are evaluated sooner.
                        }
                    }

                    obj.RemoveRestPoseAnimationFromAnimator();
                    obj.AddRestPoseAnimationToAnimator();
                    var rpLayer = obj.GetRestPoseAnimationLayer();
                    if (rpLayer != null)
                    {
                        rpLayer.IsAdditive = false;
                        rpLayer.mix = 1;
                        rpLayer.RearrangeNoRemap(0); // Make the rest pose base layer the first layer to get evaluated in the animation player.
                    }
                    else
                    {
                        obj.animator.RecalculateLayerIndicesNoRemap();
                    }
                }

                switch (playbackMode)
                {
                    case PlayMode.Last_Edited:
                        foreach (var obj in currentSession.importedObjects) ArrangeActiveSourceForObject(obj);
                        break;
                    case PlayMode.All:
                        foreach (var obj in currentSession.importedObjects) ArrangeAllSourcesForObject(obj);             
                        break;
                    case PlayMode.Active_Only:
                        //ArrangeActiveSourceForObject(currentSession.ActiveObject, false);
                        ArrangeActiveSourceForObject(currentSession.ActiveObject);
                        break;
                    case PlayMode.Active_All:
                        ArrangeAllSourcesForObject(currentSession.ActiveObject);
                        break;
                }
            }
        }
        public void PlayDefault() => Play(IsLooping, -1);
        public virtual void Play(bool loop=false, float startPos=-1)
        {
            if (currentSession == null || IsPlayingPreview) return;

            void Begin()
            {
                StopEditingCurve(); 

                var activeObj = ActiveAnimatable;
                if (activeObj != null && !activeObj.visible) activeObj.SetVisibility(this, true);

                RenderingControl.SetRenderAnimationBones(false);
                IgnorePreviewObjects();

                ArrangePreviewLayers();
                if (CurrentSource != null) this.testAnimation = CurrentSource.CompiledAnimation; // TODO: REMOVE
                if (startPos >= 0) PlaybackPosition = startPos;
                isPlaying = true;
                this.loop = loop;

                if (playButton != null) playButton.SetActive(false);
                if (pauseButton != null) pauseButton.SetActive(true);
                if (stopButton != null) stopButton.SetActive(true);
            }
            void Fail()
            {
                swole.LogWarning("Unable to initiate playback due to failed compilation."); 
            }

            CompileSourcesForPlayback(Begin, Fail, () => { swole.LogWarning("Cannot start a new compilation job: compiler is busy or the editor is in playback mode."); });
        }
        public virtual void PlaySnapshotUnclamped(float time) => PlaySnapshot(time, false);
        public virtual void PlaySnapshot(float time, bool clampTime = true)
        {
            if (currentSession == null || IsPlayingPreview) return;

            void Begin()
            {
                var activeObj = ActiveAnimatable;
                if (activeObj != null && !activeObj.visible) activeObj.SetVisibility(this, true);

                var activeState = ActivePreviewState;
                if (activeState != null)
                {
                    if (clampTime) time = Mathf.Clamp(time, 0, activeState.GetEstimatedDuration() - Mathf.Epsilon); // Try to keep time within the active animation's timeline.
                } 

                ArrangePreviewLayers();

                PlaybackPosition = time;

                PlayStep();
                PlayStepLate();
                IEnumerator RefreshReferencePoses()
                {
                    yield return null;
                    yield return new WaitForEndOfFrame();
                    if (PlaybackPosition == time)
                    {
                        SetReferencePoseFlags();
                        RefreshFlaggedReferencePoses();
                    }
                }
                StartCoroutine(RefreshReferencePoses()); 
            }
            void Fail()
            {
                swole.LogWarning("Unable to initiate playback due to failed compilation.");
            }

            CompileSourcesForPlayback(Begin, Fail, () => { swole.LogWarning("Cannot start a new compilation job: compiler is busy or the editor is in playback mode."); });
        }

        protected float CurrentFrameStep
        {
            get
            {
                var source = CurrentSource;
                if (source == null || source.rawAnimation == null) return 0;

                return 1f / source.rawAnimation.framesPerSecond;
            }
        }
        public float CurrentLength
        {
            get
            {
                if (timelineWindow != null) return timelineWindow.Length;

                var source = CurrentSource;
                if (source == null || source.rawAnimation == null) return 0;

                return source.timelineLength;
            }
        }
        public void SkipToFirstFrame()
        {
            float targetTime = 0;

            if (keyframeInstances.Count > 0)
            {
                TimelineKeyframe firstFrame = null;
                int minPos = int.MaxValue;
                foreach(var pair in keyframeInstances) if (pair.Value != null && pair.Key < minPos)
                    {
                        minPos = pair.Key;
                        firstFrame = pair.Value;
                    }
                if (firstFrame != null) 
                { 
                    targetTime = minPos * CurrentFrameStep;
                    if (timelineWindow != null) targetTime = timelineWindow.GetTimeFromNormalizedPosition(timelineWindow.GetVisibleTimelineNormalizedFramePosition(targetTime));
                }
            }

            if (isPlaying)
            {
                PlaybackPosition = targetTime;
            } 
            else
            {
                PlaySnapshot(targetTime);
            }
        }
        public void SkipToFinalFrame()
        {
            float targetTime = CurrentLength;
            float frameStep = CurrentFrameStep;
            if (frameStep > 0) targetTime = Mathf.Floor(CurrentLength / frameStep) * frameStep;

            if (keyframeInstances.Count > 0)
            {
                TimelineKeyframe firstFrame = null;
                int maxPos = int.MinValue;
                foreach (var pair in keyframeInstances) if (pair.Value != null && pair.Key > maxPos)
                    {
                        maxPos = pair.Key;
                        firstFrame = pair.Value;
                    }
                if (firstFrame != null)
                {
                    targetTime = maxPos * CurrentFrameStep;
                    if (timelineWindow != null) targetTime = timelineWindow.GetTimeFromNormalizedPosition(timelineWindow.GetVisibleTimelineNormalizedFramePosition(targetTime));
                }
            }

            if (isPlaying)
            {
                PlaybackPosition = targetTime;
            }
            else
            {
                PlaySnapshot(targetTime);
            }
        }
        public void SkipToNextFrame()
        {
            float frameStep = CurrentFrameStep;
            if (frameStep <= 0) return;

            float targetTime = PlaybackPosition;
            if (keyframeInstances.Count > 0)
            {
                int currentTimelinePosition = Mathf.FloorToInt(PlaybackPosition / frameStep);
                int nextTimelinePosition = int.MaxValue;

                bool flag = false;
                foreach (var pair in keyframeInstances) if (pair.Value != null && pair.Key > currentTimelinePosition && pair.Key < nextTimelinePosition)
                    {
                        nextTimelinePosition = pair.Key;
                        flag = true;
                    }
                if (flag)
                {
                    targetTime = nextTimelinePosition * frameStep;
                    if (timelineWindow != null) targetTime = timelineWindow.GetTimeFromNormalizedPosition(timelineWindow.GetVisibleTimelineNormalizedFramePosition(targetTime));
                }
            }

            if (isPlaying)
            {
                PlaybackPosition = targetTime;
            }
            else
            {
                PlaySnapshot(targetTime);
            }
        }
        public void SkipToPreviousFrame()
        {
            float frameStep = CurrentFrameStep;
            if (frameStep <= 0) return;

            float targetTime = PlaybackPosition;
            if (keyframeInstances.Count > 0)
            {
                int currentTimelinePosition = Mathf.FloorToInt(PlaybackPosition / frameStep); 
                int prevTimelinePosition = int.MinValue;

                bool flag = false;
                foreach (var pair in keyframeInstances) if (pair.Value != null && pair.Key < currentTimelinePosition && pair.Key > prevTimelinePosition)
                    {
                        prevTimelinePosition = pair.Key;
                        flag = true;
                    }
                if (flag)
                {
                    targetTime = prevTimelinePosition * frameStep;
                    if (timelineWindow != null) targetTime = timelineWindow.GetTimeFromNormalizedPosition(timelineWindow.GetVisibleTimelineNormalizedFramePosition(targetTime));
                }
            }

            if (isPlaying)
            {
                PlaybackPosition = targetTime;
            }
            else
            {
                PlaySnapshot(targetTime);
            }
        }

        public virtual void SetReferencePoseFlags()
        {
            if (currentSession != null && currentSession.importedObjects != null)
            {
                switch (playbackMode)
                {
                    case PlayMode.Last_Edited:
                    case PlayMode.All:
                        foreach (var obj in currentSession.importedObjects)
                        {
                            if (obj == null || !obj.visible) continue;
                            obj.setNextPoseAsReference = true;
                        }
                        break;
                    case PlayMode.Active_Only:
                    case PlayMode.Active_All:
                        var activeObj = currentSession.ActiveObject;
                        if (activeObj != null) activeObj.setNextPoseAsReference = true;
                        break;

                }
            }
        }

        public virtual void Pause()
        {
            if (!isPlaying) return;
            RenderingControl.SetRenderAnimationBones(true);
            StopIgnoringPreviewObjects();

            isPlaying = false;
            if (playButton != null) playButton.SetActive(true);
            if (pauseButton != null) pauseButton.SetActive(false);
            if (stopButton != null) stopButton.SetActive(false);

            SetReferencePoseFlags();
        }
        public virtual void StopAndReset()
        {
            PlaybackPosition = 0;
            Stop();
        }
        public virtual void Stop()
        {
            Pause();

            var activeObj = currentSession.ActiveObject;
            switch (playbackMode)
            {
                case PlayMode.Last_Edited:
                    foreach (var obj in currentSession.importedObjects)
                    {
                        if (obj == null || !obj.visible) continue;

                        var source = obj.CurrentSource;
                        if (source == null) continue;
                        var state = source.PreviewState;
                        if (state == null) continue;
                        state.SetNormalizedTime(0);

                        // reset playback
                        if (obj.animator != null) obj.animator.UpdateStep(0);
                        obj.setNextPoseAsReference = true;
                    }
                    break;
                case PlayMode.All:
                    foreach (var obj in currentSession.importedObjects)
                    {
                        if (obj == null || !obj.visible || obj.animationBank == null) continue;
                        foreach (var source in obj.animationBank)
                        {
                            if (source == null) continue;
                            var state = source.PreviewState;
                            if (state == null) continue;
                            state.SetNormalizedTime(0);
                        }

                        // reset playback
                        if (obj.animator != null) obj.animator.UpdateStep(0);
                        obj.setNextPoseAsReference = true;
                    }
                    break;
                case PlayMode.Active_Only:
                    if (activeObj != null)
                    {
                        var source = activeObj.CurrentSource;
                        if (source != null)
                        {
                            var state = source.PreviewState;
                            if (state == null)
                            {
                                state.SetNormalizedTime(0);

                                // reset playback
                                if (activeObj.animator != null) activeObj.animator.UpdateStep(0);
                                activeObj.setNextPoseAsReference = true;
                            }
                        }
                    }
                    break;
                case PlayMode.Active_All:
                    if (activeObj != null && activeObj.animationBank != null)
                    {
                        foreach (var source in activeObj.animationBank)
                        {
                            if (source == null) continue;
                            var state = source.PreviewState;
                            if (state == null) continue;
                            state.SetNormalizedTime(0);
                        }

                        // reset playback
                        if (activeObj.animator != null) activeObj.animator.UpdateStep(0);
                        activeObj.setNextPoseAsReference = true;
                    }
                    break;

            }
        }

        private bool isCompiling;
        public bool IsCompiling => isCompiling;
        private bool cancelCompilation;
        public void CancelCompilation()
        {
            if (!isCompiling) return;
            cancelCompilation = true;
            if (compilationWindow != null) compilationWindow.gameObject.SetActive(false);
        }
        public struct SourceCollection
        {
            public string name;
            public ICollection<AnimationSource> sources;
        }
        public delegate void CompilationDelegate();
        public bool CompileCollection(SourceCollection collection, CompilationDelegate callback = null, bool force = false, bool recompile = false, CompilationDelegate onComplete = null, CompilationDelegate onCancel = null, bool verbose = false) => CompileCollection(collection.name, collection.sources, callback, force, recompile, onComplete, onCancel, verbose);
        public bool CompileCollection(string collectionName, ICollection<AnimationSource> sources, CompilationDelegate callback = null, bool force = false, bool recompile = false, CompilationDelegate onComplete = null, CompilationDelegate onCancel = null, bool verbose = false) => Compile(sources, $"COMPILING ANIMATIONS FOR {collectionName}", callback, force, recompile, onComplete, onCancel, verbose);
        public bool Compile(ICollection<AnimationSource> sources, string message = null, CompilationDelegate callback = null, bool force = false, bool recompile = false, CompilationDelegate onComplete = null, CompilationDelegate onCancel = null, bool verbose = false)
        {
            if (IsPlayingPreview || (isCompiling && !force) || sources == null) return false;

            if (sources.Count <= 0)
            {
                onComplete?.Invoke();
                callback?.Invoke();
                return true;
            }

            isCompiling = true;
            cancelCompilation = false;

            UIProgressBar progressBar = null;
            if (compilationWindow != null) 
            {
                compilationWindow.gameObject.SetActive(true);

                progressBar = compilationWindow.GetComponentInChildren<UIProgressBar>(true);
                if (progressBar != null) progressBar.SetProgress(0);

                var info = compilationWindow.FindDeepChildLiberal("Info");
                if (info != null) CustomEditorUtils.SetComponentText(info, message == null ? "COMPILING ANIMATIONS" : message);

                var cancel = compilationWindow.FindDeepChildLiberal("Cancel");
                if (cancel != null) CustomEditorUtils.SetButtonOnClickAction(cancel, CancelCompilation);
            }
            
            IEnumerator Compilation()
            {
                int i = 0;
                foreach (var source in sources)
                {
                    if (cancelCompilation) 
                    { 
                        try
                        {
                            onCancel?.Invoke();
                        } 
                        catch (Exception e)
                        {
                            swole.LogError(e);
                        }
                        break; 
                    }
                    i++;
                    try
                    {
                        try
                        {
                            if (progressBar != null) progressBar.SetProgress(i / (float)sources.Count);
                        }
                        catch (Exception e)
                        {
                            swole.LogError(e);
                        }

                        if (source == null || (source.IsCompiled && !recompile)) continue;
                        source.Compile();
                    }
                    catch (Exception e)
                    {
                        swole.LogError($"Encountered error while compiling '{source.DisplayName}'");
                        swole.LogError(e);
                    }
                    yield return null;
                }

                isCompiling = false;
                if (compilationWindow != null) compilationWindow.gameObject.SetActive(false);

                try
                {
                    if (i >= sources.Count) onComplete?.Invoke();
                }
                catch (Exception e)
                {
                    swole.LogError(e);
                }
                try
                {
                    callback?.Invoke();
                }
                catch (Exception e)
                {
                    swole.LogError(e);
                }

                if (verbose) swole.Log($"Finished compilation task [{i}/{sources.Count} SOURCES COMPILED]");
            } 

            StartCoroutine(Compilation());
            return true;
        }
        public bool CompileAll(ICollection<SourceCollection> collections, CompilationDelegate callback = null, bool recompile = false, CompilationDelegate onComplete = null, CompilationDelegate onCancel = null, bool verbose = false)
        {
            if (IsPlayingPreview || isCompiling || collections == null) return false;

            if (collections.Count <= 0)
            {
                onComplete?.Invoke();
                callback?.Invoke();
                return true;
            }

            isCompiling = true;
            cancelCompilation = false;
            bool pauseToken = false;

            IEnumerator Compilation()
            {
                int i = 0;
                foreach(var collection in collections)
                {
                    if (cancelCompilation)
                    {
                        try
                        {
                            onCancel?.Invoke();
                        }
                        catch (Exception e)
                        {
                            swole.LogError(e.Message);
                        }
                        break;
                    }
                    i++;
                    if (collection.sources == null) continue;

                    pauseToken = true; 
                    if (!Compile(collection.sources, $"COMPILING {collection.name} [{i}/{collections.Count} TASKS]", () =>
                    {
                        pauseToken = false;
                        isCompiling = true;
                    }, true, recompile)) pauseToken = false;
                    while (pauseToken) 
                    {
                        if (cancelCompilation)
                        {
                            try
                            {
                                onCancel?.Invoke();
                            }
                            catch (Exception e)
                            {
                                swole.LogError(e);
                            }
                            break;
                        }
                        yield return null; 
                    }
                } 

                isCompiling = false;
                if (compilationWindow != null) compilationWindow.gameObject.SetActive(false);

                try
                {
                    if (verbose) swole.Log($"Finished multi-compile [{i}/{collections.Count} TASKS COMPLETED]");
                    if (i >= collections.Count) onComplete?.Invoke();
                    callback?.Invoke();
                } 
                catch(Exception e)
                {
                    //swole.LogError(e);
                }
            }
             
            StartCoroutine(Compilation());
            return true;
        }

        private readonly List<int> visibleSources = new List<int>();
        protected virtual void PlayStep()
        {
            if (IsCompiling || currentSession == null) return;

            var activeAnimatable = ActiveAnimatable;
            switch (playbackMode)
            {
                case PlayMode.All: // Playback all visible animatables and their visible animation sources
                    if (currentSession.importedObjects != null)
                    {
                        foreach (var obj in currentSession.importedObjects)
                        {
                            if (obj == null || !obj.visible || obj.animator == null || obj.animationBank == null) continue;
                            AnimationSource startSource = obj.CurrentSource;
                            foreach (var source in obj.animationBank)
                            {
                                if (source == null || !source.visible) continue;
                                var sourceState = source.PreviewState;
                                if (sourceState == null) continue;
                                sourceState.SetTime(PlaybackPosition);
                                if (obj == activeAnimatable) source.playbackPosition = PlaybackPosition;
                                if (startSource == null) startSource = source; 
                            }

                            obj.animator.UpdateStep(0);
                        }
                    }
                    break;
                case PlayMode.Last_Edited:  // Playback all visible animatables and their last edited animation source
                    if (currentSession.importedObjects != null)
                    {
                        foreach (var obj in currentSession.importedObjects)
                        {
                            if (obj == null || !obj.visible || obj.animator == null || obj.animationBank == null) continue;

                            visibleSources.Clear();

                            for (int i = 0; i < obj.animationBank.Count; i++)
                            {
                                var source = obj.animationBank[i];
                                if (source == null || !source.visible) continue;
                                if (i != obj.editIndex)
                                {
                                    if (source.previewLayer != null)
                                    {
                                        source.previewLayer.SetActive(false); // Temporarily disable the layer since it's not the one being edited
                                        visibleSources.Add(i);
                                    }
                                    continue;
                                }
                                var sourceState = source.PreviewState;
                                if (sourceState == null) continue;
                                sourceState.SetTime(PlaybackPosition);
                                if (obj == activeAnimatable) source.playbackPosition = PlaybackPosition;
                            }

                            obj.animator.UpdateStep(0);

                            foreach (int index in visibleSources) obj.animationBank[index].previewLayer.SetActive(true); // Re-enable the layers that were temporarily disabled                            
                        }
                    }
                    break;
                case PlayMode.Active_All:  // Playback the active animatable and its visible animation sources
                    if (activeAnimatable != null && activeAnimatable.animator != null && activeAnimatable.animationBank != null)
                    {
                        AnimationSource startSource = activeAnimatable.CurrentSource;
                        foreach (var source in activeAnimatable.animationBank)
                        {
                            if (source == null || !source.visible) continue;
                            var sourceState = source.PreviewState;
                            if (sourceState == null) continue;
                            sourceState.SetTime(PlaybackPosition);
                            source.playbackPosition = PlaybackPosition;
                            if (startSource == null) startSource = source;
                        }

                        activeAnimatable.animator.UpdateStep(0);
                    }
                    break;
                case PlayMode.Active_Only:  // Playback the active animatable and its last edited animation source
                    if (activeAnimatable != null && activeAnimatable.animator != null && activeAnimatable.animationBank != null)
                    {
                        visibleSources.Clear();

                        for (int i = 0; i < activeAnimatable.animationBank.Count; i++)
                        {
                            var source = activeAnimatable.animationBank[i];
                            if (source == null || !source.visible) continue;
                            if (i != activeAnimatable.editIndex)
                            {
                                if (source.previewLayer != null)
                                {
                                    source.previewLayer.SetActive(false); // Temporarily disable the layer since it's not the one being edited
                                    visibleSources.Add(i);
                                }
                                continue;
                            }
                            var sourceState = source.PreviewState;
                            if (sourceState == null) continue;
                            sourceState.SetTime(PlaybackPosition);
                            source.playbackPosition = PlaybackPosition;
                        }

                        activeAnimatable.animator.UpdateStep(0);

                        foreach (int index in visibleSources) activeAnimatable.animationBank[index].previewLayer.SetActive(true); // Re-enable the layers that were temporarily disabled
                    }
                    break;
            }
        }
        public virtual void Step()
        {
            if (IsCompiling) return;

            InputStep();

            if (IsPlayingPreview)
            {
                if (currentSession == null)
                {
                    Pause();
                }
                else
                {
                    var state = ActivePreviewState;
                    if (loop && state != null)
                    {
                        var length = state.GetEstimatedDuration();
                        if (length > 0)
                        {
                            if (playbackSpeed >= 0)
                            {
                                while (playbackPosition >= length)
                                {
                                    playbackPosition -= length;
                                }
                            }
                            else
                            {
                                while (playbackPosition <= 0)
                                {
                                    playbackPosition += length;
                                }
                            }
                        }
                    }

                    PlaybackPosition = PlaybackPosition + Time.deltaTime * playbackSpeed;

                    PlayStep();
                }
            } 
            else
            {
                EditStep();
            }
        } 
        protected virtual void EditStep()
        {

        }
        protected readonly List<Transform> tempTransforms = new List<Transform>();
        protected virtual void RecordPose(List<Transform> affectedTransforms, bool useReferencePose)
        {
            var activeSource = CurrentSource;
            if (activeSource == null) return;

            tempTransforms.Clear();
            foreach (var transform in affectedTransforms)
            {
                var bone = FindPoseableBone(transform);
                if (bone == null) continue;

                //tempTransforms.Add(transform);
                tempTransforms.Add(bone.transform);
            }

            float time = PlaybackPosition;
            if (timelineWindow != null)
            {
                decimal epsilon = 1m / AnimationTimeline.frameRate;
                //time = timelineWindow.GetTimeFromNormalizedPosition(timelineWindow.GetVisibleTimelineNormalizedFramePosition(timelineWindow.GetNonlinearTimeFromTime(time)));
                var tempTime = timelineWindow.CalculateFrameTimeAtTimelinePosition((decimal)timelineWindow.GetNonlinearTimeFromTime(time));
                time = (float)(Mathf.FloorToInt((float)(tempTime / epsilon)) * epsilon);
            }

            var animatable = ActiveAnimatable;
            activeSource.InsertKeyframes(true, useReferencePose, animatable, time, (timelineWindow == null ? null : timelineWindow.CalculateFrameAtTimelinePosition), true, tempTransforms);
            animatable.setNextPoseAsReference = true;
            RefreshFlaggedReferencePoses();

            RefreshTimeline();
        }
        protected virtual void RecordManipulationAction(List<Transform> affectedTransforms, Vector3 relativeOffset, Quaternion relativeRotation, Vector3 relativeScale)
        {
            if (!IsRecording) return;

            RecordPose(affectedTransforms, true);
        }
        protected virtual void PlayStepLate()
        {
            if (currentSession != null)
            {
                var activeAnimatable = ActiveAnimatable;

                switch (playbackMode)
                {
                    case PlayMode.All:
                    case PlayMode.Last_Edited:
                        if (currentSession.importedObjects != null)
                        {
                            foreach (var obj in currentSession.importedObjects)
                            {
                                obj.animator.LateUpdateStep(Time.deltaTime);
                            }
                        }
                        break;
                    case PlayMode.Active_All:
                    case PlayMode.Active_Only:
                        if (activeAnimatable != null && activeAnimatable.animator != null)
                        {
                            activeAnimatable.animator.LateUpdateStep(Time.deltaTime);
                        }
                        break;
                }
            }
        }
        public virtual void RefreshFlaggedReferencePoses()
        {
            if (currentSession != null)
            {
                if (currentSession.importedObjects != null)
                {
                    foreach (var obj in currentSession.importedObjects)
                    {
                        try
                        {
                            if (obj == null) continue;
                            if (obj.setNextPoseAsReference)
                            {
                                obj.SetCurrentPoseAsReference();
                                obj.setNextPoseAsReference = false;
                            }
                        }
                        catch (Exception e)
                        {
                            swole.LogError(e.Message);
                        }
                    }
                }
            }
        }
        public virtual void LateStep()
        {
            if (IsCompiling) return;

            if (IsPlayingPreview)
            {

                var state = ActivePreviewState;
                if (playbackSpeed == 0 || state == null)
                {
                    Pause();
                }
                else
                {
                    if (!loop)
                    {
                        if (playbackSpeed >= 0)
                        {
                            var length = state.GetEstimatedDuration();
                            if (PlaybackPosition >= length)
                            {
                                PlaybackPosition = 0;
                                Stop();
                            }
                        }
                        else
                        {
                            if (PlaybackPosition <= 0)
                            {
                                PlaybackPosition = state.GetEstimatedDuration();
                                Stop();
                            }
                        }
                    }
                }

                PlayStepLate();
            } 

            RefreshFlaggedReferencePoses();
        }

        #region Actions

        [Serializable]
        public class ClickableOption
        {
            public GameObject gameObject;
            public bool requireActiveAnimatable = true;
            public bool requireActiveAnimationSource = true;
            public bool requireBoneSelection = false;
            public BoolParameterlessDelegate customRequirement;

            public void Refresh(AnimationEditor editor)
            {
                if (gameObject == null || editor == null)
                {
                    if (gameObject != null) gameObject.SetActive(false); 
                    return;
                }

                var animatable = editor.ActiveAnimatable;
                if (animatable == null && (requireActiveAnimatable || requireActiveAnimationSource))
                {
                    gameObject.SetActive(false);
                    return;
                }
                var source = animatable.CurrentSource;
                if (source == null && requireActiveAnimationSource)
                {
                    gameObject.SetActive(false);
                    return;
                }
                if (requireBoneSelection && editor.SelectedBoneCount <= 0)
                {
                    gameObject.SetActive(false);
                    return;
                }

                if (customRequirement != null && !customRequirement.Invoke())
                {
                    gameObject.SetActive(false);
                    return;
                }

                gameObject.SetActive(true);  
            }
        }

        [Header("Clickables")]
        public List<ClickableOption> clickableOptions = new List<ClickableOption>();

        protected struct TransformState
        {
            public Vector3 pos;
            public Quaternion rot;
            public Vector3 localScale;

            public TransformState(Transform t, bool worldSpace)
            {
                if (worldSpace)
                {
                    t.GetPositionAndRotation(out pos, out rot);
                } 
                else
                {
                    t.GetLocalPositionAndRotation(out pos, out rot);
                }
                localScale = t.localScale; 
            }

            public void Apply(Transform t, bool worldSpace)
            {
                if (worldSpace)
                {
                    t.SetPositionAndRotation(pos, rot);
                }
                else
                {
                    t.SetLocalPositionAndRotation(pos, rot);
                }
                t.localScale = localScale;
            }

            public TransformState WorldToRootSpace(Transform root) => WorldToRootSpace(root.worldToLocalMatrix, root.rotation);
            public TransformState RootToWorldSpace(Transform root) => RootToWorldSpace(root.localToWorldMatrix, root.rotation);

            public TransformState WorldToRootSpace(Matrix4x4 worldToRoot, Quaternion rootRot)
            {
                var ts = this;

                ts.pos = worldToRoot.MultiplyPoint(pos);
                ts.rot = Quaternion.Inverse(rootRot) * rot;

                return ts;
            }
            public TransformState RootToWorldSpace(Matrix4x4 rootToWorld, Quaternion rootRot)
            {
                var ts = this;

                ts.pos = rootToWorld.MultiplyPoint(pos);
                ts.rot = rootRot * rot;

                return ts;
            }

        }

        private static readonly List<GameObject> _tempGameObjects = new List<GameObject>();
        private static readonly List<Transform> _tempTransforms = new List<Transform>();
        private static readonly List<Transform> _tempTransforms2 = new List<Transform>();
        private static readonly Dictionary<Transform, TransformState> _tempTransformStates = new Dictionary<Transform, TransformState>();

        private void GetAllPoseableBones(List<Transform> list)
        {
            if (list == null) return;

            var animatable = ActiveAnimatable;
            if (animatable == null || animatable.animator == null) return;

            foreach (var transform in animatable.animator.Bones.bones)
            {
                if (transform == null) continue;

                var bone = FindPoseableBone(transform);
                if (bone == null) continue;

                list.Add(bone.transform);
            }
        }

        private void GetSelectedPoseableBones(List<Transform> list)
        {
            if (list == null) return;

            var animatable = ActiveAnimatable;
            if (animatable == null || animatable.animator == null) return;

            _tempGameObjects.Clear();
            runtimeEditor.QueryActiveSelection(_tempGameObjects);

            foreach (var go in _tempGameObjects)
            {
                if (go == null) continue;

                var bone = FindPoseableBone(go.transform);
                if (bone == null) continue;

                list.Add(bone.transform);
            }
        }

        #region Flip Pose
        /// <summary>
        /// Will append mirrored transform equivalents to the given list
        /// </summary>
        public static void FlipPose(Transform root, List<Transform> transforms)
        {
            _tempTransforms.Clear();
            _tempTransformStates.Clear();

            Matrix4x4 rootL2W = Matrix4x4.identity;
            Matrix4x4 rootW2L = Matrix4x4.identity;
            Quaternion rootRot = Quaternion.identity;

            if (root != null)
            {
                rootL2W = root.localToWorldMatrix;
                rootW2L = root.worldToLocalMatrix;
                rootRot = root.rotation;
            }

            void AddTransformState(Transform transform)
            {
                var state = new TransformState();
                transform.GetPositionAndRotation(out state.pos, out state.rot);
                state.localScale = transform.localScale;

                if (root != null) state = state.WorldToRootSpace(rootW2L, rootRot);

                _tempTransformStates[transform] = state;
            }

            foreach (var transform in transforms)
            {
                if (transform == null || transform == root || _tempTransformStates.ContainsKey(transform)) continue;

                AddTransformState(transform);
                string mirroredName = Utils.GetMirroredName(transform.name);
                if (mirroredName != transform.name)
                {
                    var mirrorTransform = root.FindDeepChild(mirroredName);
                    if (mirrorTransform != null) 
                    {
                        AddTransformState(mirrorTransform);
                        if (!transforms.Contains(mirrorTransform)) _tempTransforms.Add(mirrorTransform); 
                    }
                }
            }
            foreach (var append in _tempTransforms) if (!transforms.Contains(append)) transforms.Add(append);

            foreach (var transform in transforms)
            {
                if (transform == null || transform == root) continue;

                string mirroredName = Utils.GetMirroredName(transform.name);

                var state = _tempTransformStates[transform];

                if (mirroredName != transform.name)
                {
                    var mirrorTransform = root.FindDeepChild(mirroredName);
                    if (mirrorTransform != null && _tempTransformStates.TryGetValue(mirrorTransform, out TransformState mirrorState))
                    {
                        state = mirrorState; 
                    }
                }

                Maths.MirrorPositionAndRotationX(state.pos, state.rot, out state.pos, out state.rot);
                if (root != null) state = state.RootToWorldSpace(rootL2W, rootRot);

                transform.SetPositionAndRotation(state.pos, state.rot);
                transform.localScale = state.localScale;
            }

            _tempTransforms.Clear();
            _tempTransformStates.Clear();
        }
        public void FlipPose()
        {
            var animatable = ActiveAnimatable;
            if (animatable == null || animatable.animator == null) return;

            _tempTransforms2.Clear();
            GetAllPoseableBones(_tempTransforms2);

            Transform root = animatable.instance.transform;
            if (animatable.animator.Bones.rigContainer != null) root = animatable.animator.Bones.rigContainer;

            FlipPose(root, _tempTransforms2);

            if (IsRecording) RecordPose(_tempTransforms2, true);  
        }
        public void FlipPoseSelected()
        {
            if (runtimeEditor == null) return;

            var animatable = ActiveAnimatable;
            if (animatable == null || animatable.animator == null) return;

            _tempTransforms2.Clear();
            GetSelectedPoseableBones(_tempTransforms2);

            Transform root = animatable.instance.transform;
            if (animatable.animator.Bones.rigContainer != null) root = animatable.animator.Bones.rigContainer;
            
            FlipPose(root, _tempTransforms2);

            if (IsRecording) RecordPose(_tempTransforms2, true);
        }
        #endregion

        #region Copy/Paste Pose
        protected readonly Dictionary<Transform, TransformState> clipboard_pose = new Dictionary<Transform, TransformState>();
        private static void CopyPose(List<Transform> transforms, Dictionary<Transform, TransformState> clipboard)
        {
            clipboard.Clear();
            if (transforms == null) return;
            foreach (var t in transforms) if (t != null) clipboard[t] = new TransformState(t, false);
        }
        public void CopyPose()
        {
            _tempTransforms2.Clear();
            GetAllPoseableBones(_tempTransforms2); 

            CopyPose(_tempTransforms2, clipboard_pose);
        }
        public void CopyPoseSelected()
        {
            _tempTransforms2.Clear();
            GetSelectedPoseableBones(_tempTransforms2);

            CopyPose(_tempTransforms2, clipboard_pose);
        }
        private static void PastePose(List<Transform> transforms, Dictionary<Transform, TransformState> clipboard)
        {
            if (transforms == null) return; 
            foreach (var t in transforms) if (t != null && clipboard.TryGetValue(t, out var state)) state.Apply(t, false);   
        }
        public void PasteGlobalPose()
        {
            _tempTransforms2.Clear();
            GetAllPoseableBones(_tempTransforms2);

            PastePose(_tempTransforms2, clipboard_pose);

            if (IsRecording) RecordPose(_tempTransforms2, false);
        }
        public void PasteGlobalPoseSelected()
        {
            _tempTransforms2.Clear();
            GetSelectedPoseableBones(_tempTransforms2);

            PastePose(_tempTransforms2, clipboard_pose);

            if (IsRecording) RecordPose(_tempTransforms2, false); 
        }
        public void PasteLocalPose()
        {
            _tempTransforms2.Clear();
            GetAllPoseableBones(_tempTransforms2);

            PastePose(_tempTransforms2, clipboard_pose);

            if (IsRecording) RecordPose(_tempTransforms2, true);
        }
        public void PasteLocalPoseSelected()
        {
            _tempTransforms2.Clear();
            GetSelectedPoseableBones(_tempTransforms2);

            PastePose(_tempTransforms2, clipboard_pose);

            if (IsRecording) RecordPose(_tempTransforms2, true);
        }
        #endregion

        #endregion

    }
}

#endif