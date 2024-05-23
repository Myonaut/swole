#if (UNITY_EDITOR || UNITY_STANDALONE)

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

using TMPro;

using Swole.UI;
using Swole.Script;

using static Swole.API.Unity.CustomEditorUtils;
using Swole.API.Unity.Animation;

namespace Swole.API.Unity
{

    // >> SEARCH FOR KEYWORD "ContentTypes" TO FIND SECTIONS OF CODE THAT MUST BE REFACTORED WHEN NEW CONTENT TYPES ARE INTRODUCED
    // >> DO THE SAME IN ContentManager.cs

    public class PackageViewer : MonoBehaviour
    {

        [SerializeField, Header("Main")]
        protected ProjectManager manager;
        [SerializeField]
        protected ActivationController optionsController;
        [SerializeField]
        protected int showOptionsIndex = 0;
        [SerializeField]
        protected int hideOptionsIndex = 1;

        protected ContentPackage activePackage;
        public ContentPackage ActivePackage => activePackage;

        protected bool isLocal;
        public bool IsLocal => isLocal;

        protected bool hideWarnings;

        [SerializeField, Header("Pools")]
        protected PrefabPool packageContentPool;

        [SerializeField, Header("Windows")]
        protected GameObject textFieldEditWindow;
        [SerializeField]
        protected GameObject textAreaEditWindow;
        [SerializeField]
        protected GameObject contentInspectionWindow;
        [SerializeField]
        protected GameObject codeEditorWindow;
        [SerializeField]
        protected GameObject warningWindow;

        [Header("Icons")]
        public Sprite ico_Text;
        public Sprite ico_Image;
        public Sprite ico_Script;
        public Sprite ico_Animation;
        public Sprite ico_Creation;
        public Sprite ico_GameplayExperience;

        [SerializeField, Header("Fields")]
        protected Text urlField;
        [SerializeField]
        protected TMP_Text urlFieldTMP;

        [SerializeField]
        protected Text projectNameField;
        [SerializeField]
        protected TMP_Text projectNameFieldTMP;

        [SerializeField]
        protected Text packageNameField;
        [SerializeField]
        protected TMP_Text packageNameFieldTMP;

        [SerializeField]
        protected Text versionField;
        [SerializeField]
        protected TMP_Text versionFieldTMP;

        [SerializeField]
        protected Text creatorField;
        [SerializeField]
        protected TMP_Text creatorFieldTMP;

        [SerializeField]
        protected Text tagsField;
        [SerializeField]
        protected TMP_Text tagsFieldTMP;

        [SerializeField]
        protected Text descriptionField;
        [SerializeField]
        protected TMP_Text descriptionFieldTMP;

        [SerializeField, Header("Other")]
        private UIDynamicDropdown packageSelectionDropdown;

        [SerializeField]
        protected LayoutGroup contentLayoutGroup;
        [NonSerialized]
        protected RectTransform contentLayoutGroupTransform;
        [SerializeField]
        protected UIPopupMessageFadable defaultErrorMessage;

        protected void Awake()
        {
            if (contentLayoutGroup != null) contentLayoutGroupTransform = contentLayoutGroup.GetComponent<RectTransform>();
        }

        protected void Start()
        {
            Refresh();
        }

        public void SetActiveLocalPackage(string packageName, string packageVersion)
        {
            SetActivePackage(ContentManager.FindLocalPackage(packageName, packageVersion), true);
        }
        public void SetActiveExternalPackage(string packageName, string packageVersion)
        {
            SetActivePackage(ContentManager.FindExternalPackage(packageName, packageVersion));
        }
        public UnityEvent OnSetLocalPackage = new UnityEvent();
        public UnityEvent OnSetExternalPackage = new UnityEvent();
        public void SetActivePackage(ContentPackage package, bool isLocal = false)
        {
            activePackage = package;
            this.isLocal = isLocal;
            if (isLocal) OnSetLocalPackage?.Invoke(); else OnSetExternalPackage?.Invoke();
            Refresh();
        }

        protected bool fullRefreshNext;

        private static readonly List<ContentPackage> _tempPackageList = new List<ContentPackage>();
        public void Refresh()
        {

            inspectedContentIndex = -1;

            if (fullRefreshNext)
            {
                fullRefreshNext = false;

                ContentManager.ReloadLocalPackages();
                manager?.RefreshLocalProjects(false);
                if (activePackage != null) activePackage = isLocal ? ContentManager.FindLocalPackage(activePackage.GetIdentity()) : ContentManager.FindExternalPackage(activePackage.GetIdentity());
            }

            if (activePackage != null)
            {

                if (optionsController != null) optionsController.SetActiveGroup(IsLocal ? showOptionsIndex : hideOptionsIndex);

                if (packageSelectionDropdown != null) 
                {
                    bool isLocal_ = IsLocal;
                    packageSelectionDropdown.ClearMenuItems();
                    if (activePackage != null) packageSelectionDropdown.SetSelectionText(isLocal_ ? activePackage.GetIdentityString() : $"v{activePackage.VersionString}"); else packageSelectionDropdown.SetSelectionText("versions");
                    _tempPackageList.Clear();
                    List<ContentPackage> list;
                    if (isLocal_)
                    {
                        list = ContentManager.GetPackagesInProject(ContentManager.GetProjectIdentifier(activePackage), _tempPackageList);
                    } 
                    else
                    {
                        list = ContentManager.FindExternalPackagesOrdered(activePackage.Name, _tempPackageList); 
                    }
                    
                    for(int a = 0; a < list.Count; a++)
                    {
                        var pkg = list[a];
                        if (pkg == null) continue; 
                        var obj = packageSelectionDropdown.CreateNewMenuItem(isLocal_ ? pkg.GetIdentityString() : $"v{pkg.VersionString}");
                        if (obj != null)
                        {
                            Button button = obj.GetComponentInChildren<Button>(true);
                            if (button != null) 
                            { 
                                if (button.onClick == null) button.onClick = new Button.ButtonClickedEvent();
                                button.onClick.AddListener(new UnityAction(() => SetActivePackage(pkg, isLocal_)));
                            }
                            UITabButton tabButton = obj.GetComponentInChildren<UITabButton>(true);
                            if (tabButton != null)
                            {
                                if (tabButton.OnClick == null) tabButton.OnClick = new UnityEvent();
                                tabButton.OnClick.AddListener(new UnityAction(() => SetActivePackage(pkg, isLocal_)));
                            }
                        }
                    }
                }

                if (urlField != null) urlField.text = activePackage.URL;
                if (urlFieldTMP != null) urlFieldTMP.SetText(activePackage.URL);

                if (projectNameField != null) projectNameField.text = ContentManager.GetProjectIdentifier(activePackage);
                if (projectNameFieldTMP != null) projectNameFieldTMP.SetText(ContentManager.GetProjectIdentifier(activePackage));

                if (packageNameField != null) packageNameField.text = activePackage.Name;
                if (packageNameFieldTMP != null) packageNameFieldTMP.SetText(activePackage.Name);

                if (versionField != null) versionField.text = activePackage.VersionString;
                if (versionFieldTMP != null) versionFieldTMP.SetText(activePackage.VersionString);

                if (creatorField != null) creatorField.text = activePackage.Curator;
                if (creatorFieldTMP != null) creatorFieldTMP.SetText(activePackage.Curator);
                
                if (tagsField != null) tagsField.text = activePackage.Tags == null ? string.Empty : string.Join(", ", activePackage.Tags);
                if (tagsFieldTMP != null) tagsFieldTMP.SetText(activePackage.Tags == null ? string.Empty : string.Join(", ", activePackage.Tags));        

                if (descriptionField != null) descriptionField.text = activePackage.Description;
                if (descriptionFieldTMP != null) descriptionFieldTMP.SetText(activePackage.Description);

                RefreshContentList();
            }  
            else
            {

                if (optionsController != null) optionsController.SetActiveGroup(hideOptionsIndex);

                if (packageSelectionDropdown != null) 
                { 
                    packageSelectionDropdown.ClearMenuItems();
                    packageSelectionDropdown.SetSelectionText(string.Empty);
                }

                if (urlField != null) urlField.text = string.Empty;
                if (urlFieldTMP != null) urlFieldTMP.SetText(string.Empty);

                if (projectNameField != null) projectNameField.text = string.Empty;
                if (projectNameFieldTMP != null) projectNameFieldTMP.SetText(string.Empty);

                if (packageNameField != null) packageNameField.text = string.Empty;
                if (packageNameFieldTMP != null) packageNameFieldTMP.SetText(string.Empty);

                if (versionField != null) versionField.text = string.Empty;
                if (versionFieldTMP != null) versionFieldTMP.SetText(string.Empty);

                if (creatorField != null) creatorField.text = string.Empty;
                if (creatorFieldTMP != null) creatorFieldTMP.SetText(string.Empty);

                if (tagsField != null) tagsField.text = string.Empty;
                if (tagsFieldTMP != null) tagsFieldTMP.SetText(string.Empty);

                if (descriptionField != null) descriptionField.text = string.Empty;
                if (descriptionFieldTMP != null) descriptionFieldTMP.SetText(string.Empty);

                ClearContentList();
            }
        }

        public void ClearContentList()
        {

            if (contentLayoutGroupTransform != null)
            {
                //foreach (Transform child in contentLayoutGroupTransform) Destroy(child.gameObject);
                foreach (Transform child in contentLayoutGroupTransform) packageContentPool.Release(child.gameObject);
            }

        }
        public virtual void ClearFilterTagsContent()
        {
        
        }
        public virtual void FilterContent(Swole.UI.FilterMode filterMode)
        {
            // TODO: Add filtering
        }

        public void RefreshContentList()
        {

            if (contentLayoutGroupTransform != null)
            {
                ClearContentList();

                if (packageContentPool != null)
                {
                    for (int a = 0; a < activePackage.ContentCount; a++)
                    {
                        var content = activePackage[a];
                        if (content == null) continue;
                        //var listObject = Instantiate(packageContentPrototype);
                        if (!packageContentPool.TryGetNewInstance(out GameObject listObject)) continue;
                        string contentName = content.Name;
                        if (string.IsNullOrEmpty(contentName) && !string.IsNullOrEmpty(content.OriginPath)) contentName = Path.GetFileName(content.OriginPath);
                        //contentName = Path.Combine(content.RelativePath, contentName);
                        listObject.name = contentName;
                        listObject.SetActive(true);
                        var listObjectTransform = listObject.AddOrGetComponent<RectTransform>();
                        listObjectTransform.SetParent(contentLayoutGroupTransform, false);
                        var name = listObjectTransform.FindFirstComponentUnderChild<Text>("name");
                        var nameTMP = listObjectTransform.FindFirstComponentUnderChild<TMP_Text>("name");
                        if (name != null) name.text = contentName;
                        if (nameTMP != null) nameTMP.SetText(contentName);
                        var icon = listObjectTransform.FindFirstComponentUnderChild<Image>("icon");
                        void ActivateActionButton(string buttonName, UnityAction OnPress)
                        {
                            var editButton = listObjectTransform.FindFirstComponentUnderChild<Button>(buttonName);
                            if (editButton != null)
                            {
                                editButton.gameObject.SetActive(true);
                                if (OnPress != null)
                                {
                                    if (editButton.onClick == null) editButton.onClick = new Button.ButtonClickedEvent(); else editButton.onClick.RemoveAllListeners();
                                    editButton.onClick.AddListener(OnPress);
                                }
                            }
                            var editTabButton = listObjectTransform.FindFirstComponentUnderChild<UITabButton>(buttonName);
                            if (editTabButton != null)
                            {
                                editTabButton.gameObject.SetActive(true);
                                if (OnPress != null)
                                {
                                    if (editTabButton.OnClick == null) editTabButton.OnClick = new UnityEvent(); else editTabButton.OnClick.RemoveAllListeners();
                                    editTabButton.OnClick.AddListener(OnPress);
                                }
                            }
                        }

                        Transform actionsRoot = listObjectTransform.FindDeepChildLiberal("Actions");
                        if (actionsRoot != null) // Disable all action buttons initially.
                        {
                            for (int c = 0; c < actionsRoot.childCount; c++) actionsRoot.GetChild(c).gameObject.SetActive(false);
                        }

                        int i = a;
                        ActivateActionButton("inspect", () => StartInspectingContent(i));
                        #region ContentTypes
                        if (content is Creation creation) 
                        {
                            if (icon != null) icon.sprite = ico_Creation;
                            if (IsLocal)
                            {
                                ActivateActionButton("edit", null);  // TODO: Open creation editor and start editing the creation
                                ActivateActionButton("delete", () => DeleteContent(i));
                            }
                        } 
                        else if (content is SourceScript script)
                        {
                            if (icon != null) icon.sprite = ico_Script;
                            if (IsLocal)
                            {
                                ActivateActionButton("edit", () => {
                                    inspectedContentIndex = i;
                                    StartEditingContent(EditableTypes.ScriptSource);
                                });
                                ActivateActionButton("delete", () => DeleteContent(i));
                            }
                        }
                        else if (content is GameplayExperience exp)
                        {
                            if (icon != null) icon.sprite = ico_GameplayExperience;
                            if (IsLocal)
                            {
                                ActivateActionButton("edit", () => {
                                    inspectedContentIndex = i;
                                    StartEditingContent(EditableTypes.ScriptSource); // TODO: Start editing the gameplay experience
                                });
                                ActivateActionButton("delete", () => DeleteContent(i));
                            }
                        }
                        else if (content is CustomAnimation animation)
                        {
                            if (icon != null) icon.sprite = ico_Animation;
                            if (IsLocal)
                            {
                                ActivateActionButton("edit", null); // TODO: Open animation editor and start editing the animation
                                ActivateActionButton("delete", () => DeleteContent(i));
                            }
                        }
                        else if (content is ImageAsset img)
                        {
                            if (icon != null) icon.sprite = ico_Image;
                            if (IsLocal)
                            {
                                ActivateActionButton("edit", null); // TODO: Start editing the image source
                                ActivateActionButton("delete", () => DeleteContent(i));
                            }
                        }
                        #endregion
                    }
                }
            }

        }

        [Serializable]
        public enum EditableTypes
        {
            ProjectName, Creator, PackageName, Version, Tags, Description, URL, ContentName, ScriptSource
        }
        public delegate bool FinalizeEditDelegate();
        private FinalizeEditDelegate currentEdit;
        private GameObject activeEditWindow;
        public void StartEditing(string editable) => StartEditing(Enum.Parse<EditableTypes>(editable));
        public void StartEditing(int editableId) => StartEditing((EditableTypes)editableId);
        public void StartEditing(EditableTypes toEdit)
        {
            if (activeEditWindow != null) activeEditWindow.SetActive(false);

            Toggle GetAndDisableToggle(Transform windowTransform)
            {
                Toggle toggle = windowTransform.GetComponentInChildren<Toggle>(true);
                if (toggle != null) toggle.gameObject.SetActive(false);
                return toggle;
            }

            activeEditWindow = null;
            currentEdit = null;
            switch (toEdit)
            {

                case EditableTypes.ProjectName:
                    activeEditWindow = textFieldEditWindow == null ? textAreaEditWindow : textFieldEditWindow;
                    if (activeEditWindow != null)
                    {
                        ContentManager.CleanProjectInfo();

                        var windowTransform = activeEditWindow.transform;
                        GetAndDisableToggle(windowTransform);
                        var nameTransform = windowTransform.FindDeepChildLiberal("name"); // Find the window name transform
                        if (nameTransform != null) SetComponentText(nameTransform, "PROJECT NAME");
                        var valueTransform = windowTransform.FindDeepChildLiberal("value");
                        InputField valueText = null;
                        TMP_InputField valueTextTMP = null;
                        if (valueTransform != null)
                        {
                            valueText = valueTransform.GetComponentInChildren<InputField>();
                            valueTextTMP = valueTransform.GetComponentInChildren<TMP_InputField>();

                            string currentValue = projectNameFieldTMP == null ? projectNameField == null ? string.Empty : projectNameField.text : projectNameFieldTMP.text;
                            if (valueText != null)
                            {
                                valueText.text = currentValue;
                                if (valueText.textComponent != null) valueText.MoveTextStart(false);
                                valueText.ForceLabelUpdate();
                            }
                            if (valueTextTMP != null)
                            {
                                valueTextTMP.text = currentValue;
                                if (valueTextTMP.textComponent != null && valueTextTMP.textComponent.textInfo != null) valueTextTMP.MoveTextStart(false);
                                valueTextTMP.ForceLabelUpdate();
                            }
                        }
                        if (valueText == null && valueTextTMP == null)
                        {
                            activeEditWindow = null;
                            return;
                        }
                        var errorMessage = windowTransform.GetComponentInChildren<UIPopupMessageFadable>(true);
                        currentEdit = () => SetProjectName(valueTextTMP == null ? valueText.text : valueTextTMP.text, errorMessage);
                    }
                    break;

                case EditableTypes.PackageName:
                    activeEditWindow = textFieldEditWindow == null ? textAreaEditWindow : textFieldEditWindow;
                    if (activeEditWindow != null)
                    {
                        var windowTransform = activeEditWindow.transform;
                        var nameTransform = windowTransform.FindDeepChildLiberal("name"); // Find the window name transform
                        if (nameTransform != null) SetComponentText(nameTransform, "PACKAGE NAME");
                        var valueTransform = windowTransform.FindDeepChildLiberal("value");
                        InputField valueText = null;
                        TMP_InputField valueTextTMP = null;
                        if (valueTransform != null)
                        {
                            valueText = valueTransform.GetComponentInChildren<InputField>();
                            valueTextTMP = valueTransform.GetComponentInChildren<TMP_InputField>();

                            string currentValue = packageNameFieldTMP == null ? packageNameField == null ? string.Empty : packageNameField.text : packageNameFieldTMP.text;
                            if (valueText != null)
                            {
                                valueText.text = currentValue;
                                if (valueText.textComponent != null) valueText.MoveTextStart(false);
                                valueText.ForceLabelUpdate();
                            }
                            if (valueTextTMP != null)
                            {
                                valueTextTMP.text = currentValue;
                                if (valueTextTMP.textComponent != null && valueTextTMP.textComponent.textInfo != null) valueTextTMP.MoveTextStart(false);
                                valueTextTMP.ForceLabelUpdate();
                            }
                        }
                        if (valueText == null && valueTextTMP == null)
                        {
                            activeEditWindow = null;
                            return;
                        }
                        Toggle toggle = GetAndDisableToggle(windowTransform);
                        if (toggle != null)
                        {
                            toggle.gameObject.SetActive(true);
                            toggle.isOn = true;

                            Text toggleText = toggle.GetComponentInChildren<Text>();
                            TMP_Text toggleTextTMP = toggle.GetComponentInChildren<TMP_Text>();

                            if (toggleText != null) toggleText.text = "RENAME ALL VERSIONS";
                            if (toggleTextTMP != null) toggleTextTMP.text = "RENAME ALL VERSIONS";
                        }
                        var errorMessage = windowTransform.GetComponentInChildren<UIPopupMessageFadable>(true);
                        currentEdit = () =>
                        {

                            if (SetPackageName(valueTextTMP == null ? valueText.text : valueTextTMP.text, toggle == null ? true : toggle.isOn, errorMessage))
                            {
                                if (toggle != null) toggle.gameObject.SetActive(false);
                                return true;
                            }

                            return false;
                        };
                    }
                    break;

                case EditableTypes.Version:
                    activeEditWindow = textFieldEditWindow == null ? textAreaEditWindow : textFieldEditWindow;
                    if (activeEditWindow != null)
                    {
                        var windowTransform = activeEditWindow.transform;
                        GetAndDisableToggle(windowTransform);
                        var nameTransform = windowTransform.FindDeepChildLiberal("name"); // Find the window name transform
                        if (nameTransform != null) SetComponentText(nameTransform, "VERSION");
                        var valueTransform = windowTransform.FindDeepChildLiberal("value");
                        InputField valueText = null;
                        TMP_InputField valueTextTMP = null;
                        if (valueTransform != null)
                        {
                            valueText = valueTransform.GetComponentInChildren<InputField>();
                            valueTextTMP = valueTransform.GetComponentInChildren<TMP_InputField>();

                            string currentValue = versionFieldTMP == null ? versionField == null ? string.Empty : versionField.text : versionFieldTMP.text;
                            if (valueText != null)
                            {
                                valueText.text = currentValue;
                                if (valueText.textComponent != null) valueText.MoveTextStart(false);
                                valueText.ForceLabelUpdate();
                            }
                            if (valueTextTMP != null)
                            {
                                valueTextTMP.text = currentValue;
                                if (valueTextTMP.textComponent != null && valueTextTMP.textComponent.textInfo != null) valueTextTMP.MoveTextStart(false);
                                valueTextTMP.ForceLabelUpdate();
                            }
                        }
                        if (valueText == null && valueTextTMP == null)
                        {
                            activeEditWindow = null;
                            return;
                        }
                        var errorMessage = windowTransform.GetComponentInChildren<UIPopupMessageFadable>(true);
                        currentEdit = () => SetVersion(valueTextTMP == null ? valueText.text : valueTextTMP.text, errorMessage);

                    }
                    break;

                case EditableTypes.URL:
                    activeEditWindow = textFieldEditWindow == null ? textAreaEditWindow : textFieldEditWindow;
                    if (activeEditWindow != null)
                    {
                        var windowTransform = activeEditWindow.transform;
                        var nameTransform = windowTransform.FindDeepChildLiberal("name"); // Find the window name transform
                        if (nameTransform != null) SetComponentText(nameTransform, "URL");
                        var valueTransform = windowTransform.FindDeepChildLiberal("value");
                        InputField valueText = null;
                        TMP_InputField valueTextTMP = null;
                        if (valueTransform != null)
                        {
                            valueText = valueTransform.GetComponentInChildren<InputField>();
                            valueTextTMP = valueTransform.GetComponentInChildren<TMP_InputField>();

                            string currentValue = urlFieldTMP == null ? urlField == null ? string.Empty : urlField.text : urlFieldTMP.text;
                            if (valueText != null)
                            {
                                valueText.text = currentValue;
                                if (valueText.textComponent != null) valueText.MoveTextStart(false);
                                valueText.ForceLabelUpdate();
                            }
                            if (valueTextTMP != null)
                            {
                                valueTextTMP.text = currentValue;
                                if (valueTextTMP.textComponent != null && valueTextTMP.textComponent.textInfo != null) valueTextTMP.MoveTextStart(false);
                                valueTextTMP.ForceLabelUpdate();
                            }
                        }
                        if (valueText == null && valueTextTMP == null)
                        {
                            activeEditWindow = null;
                            return;
                        }
                        Toggle toggle = GetAndDisableToggle(windowTransform);
                        if (toggle != null)
                        {
                            toggle.gameObject.SetActive(true);
                            toggle.isOn = false;

                            Text toggleText = toggle.GetComponentInChildren<Text>();
                            TMP_Text toggleTextTMP = toggle.GetComponentInChildren<TMP_Text>();

                            if (toggleText != null) toggleText.text = "UPDATE ALL VERSIONS";
                            if (toggleTextTMP != null) toggleTextTMP.text = "UPDATE ALL VERSIONS";
                        }
                        var errorMessage = windowTransform.GetComponentInChildren<UIPopupMessageFadable>(true);
                        currentEdit = () => SetURL(valueTextTMP == null ? valueText.text : valueTextTMP.text, toggle == null ? false : toggle.isOn, errorMessage);
                    }
                    break;

                case EditableTypes.Creator:
                    activeEditWindow = textFieldEditWindow == null ? textAreaEditWindow : textFieldEditWindow;
                    if (activeEditWindow != null)
                    {
                        var windowTransform = activeEditWindow.transform;
                        var nameTransform = windowTransform.FindDeepChildLiberal("name"); // Find the window name transform
                        if (nameTransform != null) SetComponentText(nameTransform, "CREATOR");
                        var valueTransform = windowTransform.FindDeepChildLiberal("value");
                        InputField valueText = null;
                        TMP_InputField valueTextTMP = null;
                        if (valueTransform != null)
                        {
                            valueText = valueTransform.GetComponentInChildren<InputField>();
                            valueTextTMP = valueTransform.GetComponentInChildren<TMP_InputField>();

                            string currentValue = creatorFieldTMP == null ? creatorField == null ? string.Empty : creatorField.text : creatorFieldTMP.text;
                            if (valueText != null)
                            {
                                valueText.text = currentValue;
                                if (valueText.textComponent != null) valueText.MoveTextStart(false);
                                valueText.ForceLabelUpdate();
                            }
                            if (valueTextTMP != null)
                            {
                                valueTextTMP.text = currentValue;
                                if (valueTextTMP.textComponent != null && valueTextTMP.textComponent.textInfo != null) valueTextTMP.MoveTextStart(false);
                                valueTextTMP.ForceLabelUpdate();
                            }
                        }
                        if (valueText == null && valueTextTMP == null)
                        {
                            activeEditWindow = null;
                            return;
                        }
                        Toggle toggle = GetAndDisableToggle(windowTransform);
                        if (toggle != null)
                        {
                            toggle.gameObject.SetActive(true);
                            toggle.isOn = false;

                            Text toggleText = toggle.GetComponentInChildren<Text>();
                            TMP_Text toggleTextTMP = toggle.GetComponentInChildren<TMP_Text>();

                            if (toggleText != null) toggleText.text = "UPDATE ALL VERSIONS";
                            if (toggleTextTMP != null) toggleTextTMP.text = "UPDATE ALL VERSIONS";
                        }
                        var errorMessage = windowTransform.GetComponentInChildren<UIPopupMessageFadable>(true);
                        currentEdit = () => SetCreator(valueTextTMP == null ? valueText.text : valueTextTMP.text, toggle == null ? false : toggle.isOn, errorMessage);
                    }
                    break;

                case EditableTypes.Tags:
                    activeEditWindow = textFieldEditWindow == null ? textAreaEditWindow : textFieldEditWindow;
                    if (activeEditWindow != null) 
                    {
                        var windowTransform = activeEditWindow.transform;
                        var nameTransform = windowTransform.FindDeepChildLiberal("name"); // Find the window name transform
                        if (nameTransform != null) SetComponentText(nameTransform, "TAGS");
                        var valueTransform = windowTransform.FindDeepChildLiberal("value");
                        InputField valueText = null;
                        TMP_InputField valueTextTMP = null;
                        if (valueTransform != null)
                        {
                            valueText = valueTransform.GetComponentInChildren<InputField>();
                            valueTextTMP = valueTransform.GetComponentInChildren<TMP_InputField>();

                            string currentValue = tagsFieldTMP == null ? tagsField == null ? string.Empty : tagsField.text : tagsFieldTMP.text;
                            if (valueText != null)
                            {
                                valueText.text = currentValue;
                                if (valueText.textComponent != null) valueText.MoveTextStart(false);
                                valueText.ForceLabelUpdate();
                            }
                            if (valueTextTMP != null)
                            {
                                valueTextTMP.text = currentValue;
                                if (valueTextTMP.textComponent != null && valueTextTMP.textComponent.textInfo != null) valueTextTMP.MoveTextStart(false);
                                valueTextTMP.ForceLabelUpdate();
                            }
                        }
                        if (valueText == null && valueTextTMP == null)
                        {
                            activeEditWindow = null;
                            return;
                        }
                        Toggle toggle = GetAndDisableToggle(windowTransform);
                        if (toggle != null)
                        {
                            toggle.gameObject.SetActive(true);
                            toggle.isOn = false;

                            Text toggleText = toggle.GetComponentInChildren<Text>();
                            TMP_Text toggleTextTMP = toggle.GetComponentInChildren<TMP_Text>();

                            if (toggleText != null) toggleText.text = "UPDATE ALL VERSIONS";
                            if (toggleTextTMP != null) toggleTextTMP.text = "UPDATE ALL VERSIONS";
                        }
                        var errorMessage = windowTransform.GetComponentInChildren<UIPopupMessageFadable>(true);
                        currentEdit = () => SetTags(valueTextTMP == null ? valueText.text : valueTextTMP.text, toggle == null ? false : toggle.isOn, errorMessage);
                    }
                    break;

                case EditableTypes.Description:
                    activeEditWindow = textAreaEditWindow == null ? textFieldEditWindow : textAreaEditWindow;
                    if (activeEditWindow != null)
                    {
                        var windowTransform = activeEditWindow.transform;
                        var nameTransform = windowTransform.FindDeepChildLiberal("name"); // Find the window name transform
                        if (nameTransform != null) SetComponentText(nameTransform, "DESCRIPTION");
                        var valueTransform = windowTransform.FindDeepChildLiberal("value");
                        InputField valueText = null;
                        TMP_InputField valueTextTMP = null;
                        if (valueTransform != null)
                        {
                            valueText = valueTransform.GetComponentInChildren<InputField>();
                            valueTextTMP = valueTransform.GetComponentInChildren<TMP_InputField>();

                            string currentValue = descriptionFieldTMP == null ? descriptionField == null ? string.Empty : descriptionField.text : descriptionFieldTMP.text;
                            if (valueText != null) 
                            { 
                                valueText.text = currentValue;
                                if (valueText.textComponent != null) valueText.MoveTextStart(false);
                                valueText.ForceLabelUpdate();
                            }
                            if (valueTextTMP != null) 
                            { 
                                valueTextTMP.text = currentValue;
                                if (valueTextTMP.textComponent != null && valueTextTMP.textComponent.textInfo != null) valueTextTMP.MoveTextStart(false);
                                valueTextTMP.ForceLabelUpdate();
                            }
                        }
                        if (valueText == null && valueTextTMP == null)
                        {
                            activeEditWindow = null;
                            return;
                        }
                        Toggle toggle = GetAndDisableToggle(windowTransform);
                        if (toggle != null)
                        {
                            toggle.gameObject.SetActive(true);
                            toggle.isOn = false;

                            Text toggleText = toggle.GetComponentInChildren<Text>();
                            TMP_Text toggleTextTMP = toggle.GetComponentInChildren<TMP_Text>();

                            if (toggleText != null) toggleText.text = "UPDATE ALL VERSIONS";
                            if (toggleTextTMP != null) toggleTextTMP.text = "UPDATE ALL VERSIONS";
                        }
                        var errorMessage = windowTransform.GetComponentInChildren<UIPopupMessageFadable>(true);
                        currentEdit = () => SetDescription(valueTextTMP == null ? valueText.text : valueTextTMP.text, toggle == null ? false : toggle.isOn, errorMessage);
                    }
                    break;

            }

            if (activeEditWindow != null)
            {
                activeEditWindow.SetActive(true);
                SetButtonOnClickAction(activeEditWindow.transform.FindDeepChildLiberal("finalize"), TryCompleteEdit);
            }
        }
        public void TryCompleteEdit()
        {
            if (currentEdit == null) return;
            if (activeEditWindow != null && !activeEditWindow.activeInHierarchy)
            {
                currentEdit = null;
                activeEditWindow = null;
                return;
            }
            if (currentEdit.Invoke())
            {
                activeEditWindow?.SetActive(false);
                currentEdit = null;
                activeEditWindow = null;
            }
        }

        protected int inspectedContentIndex;

        public void StartInspectingContent(int contentIndex)
        {
            if (contentInspectionWindow == null || contentIndex < 0 || activePackage == null || contentIndex >= activePackage.ContentCount) return;

            inspectedContentIndex = contentIndex;
            IContent content = activePackage.GetContent(contentIndex);

            Transform rootTransform = contentInspectionWindow.transform;
            Transform contentInspectorTransform = rootTransform.FindDeepChildLiberal("Default");

            void FindAndSetTextFieldValueAndEditAction(string fieldName, string value, UnityAction editAction)
            {
                Transform contentNameTransform = contentInspectorTransform.FindDeepChildLiberal(fieldName);
                if (contentNameTransform != null)
                {
                    Transform valueTransform = contentNameTransform.FindDeepChildLiberal("Value");
                    if (valueTransform != null)
                    {
                        SetComponentText(valueTransform, value);
                    }

                    Transform optionsTransform = contentNameTransform.FindDeepChildLiberal("Options");
                    if (optionsTransform != null)
                    {
                        if (isLocal)
                        {
                            optionsTransform.gameObject.SetActive(true);

                            Transform editTransform = optionsTransform.FindDeepChildLiberal("Edit");
                            if (editTransform != null)
                            {
                                SetButtonOnClickAction(editTransform, editAction);
                            }
                        }
                        else
                        {
                            optionsTransform.gameObject.SetActive(false);
                        }
                    }
                }
            }

            #region ContentTypes
            if (content is SourceScript script)
            {
                Transform temp = rootTransform.FindDeepChildLiberal("Script");
                if (temp != null) contentInspectorTransform = temp;

                FindAndSetTextFieldValueAndEditAction("Source", script.source, () => StartEditingContent(EditableTypes.ScriptSource));

            }
            else if (content is Creation)
            {
                Transform temp = rootTransform.FindDeepChildLiberal("Creation");
                if (temp != null) contentInspectorTransform = temp;
            }
            else if (content is GameplayExperience)
            {
                Transform temp = rootTransform.FindDeepChildLiberal("GameplayExperience");
                if (temp != null) contentInspectorTransform = temp;
            }
            else if (content is CustomAnimation)
            {
                Transform temp = rootTransform.FindDeepChildLiberal("Animation");
                if (temp != null) contentInspectorTransform = temp;
            }
            else if (content is ImageAsset)
            {
                Transform temp = rootTransform.FindDeepChildLiberal("Image");
                if (temp != null) contentInspectorTransform = temp;

                // TODO: Set preview image to the image
            }
            #endregion

            if (contentInspectorTransform == null)
            {
                swole.LogError($"Content Inspector has no category for type '{content.GetType().Name}'");
                return;
            }
            else 
            {
                void DisableContentInspector(string inspectorName)
                {
                    Transform temp = rootTransform.FindDeepChildLiberal(inspectorName);
                    if (temp != null && temp != contentInspectorTransform) temp.gameObject.SetActive(false);           
                }

                DisableContentInspector("Default");
                DisableContentInspector("Script");
                DisableContentInspector("Creation");

                contentInspectorTransform.gameObject.SetActive(true);
            }

            FindAndSetTextFieldValueAndEditAction("ContentName", content.Name, () => StartEditingContent(EditableTypes.ContentName));
            FindAndSetTextFieldValueAndEditAction("Author", content.Author, () => StartEditingContent(EditableTypes.Creator));
            FindAndSetTextFieldValueAndEditAction("Info", $"Content Type: {content.GetType().Name}{Environment.NewLine}{(string.IsNullOrEmpty(content.LastEditDate) ? "" : $"Created On: {content.CreationDate}")}{(string.IsNullOrEmpty(content.LastEditDate) ? "" :  $" - Last Edited On: {content.LastEditDate}")}{Environment.NewLine}{Environment.NewLine}{content.Description}", () => StartEditingContent(EditableTypes.Description));

            contentInspectionWindow.SetActive(true); 
        }

        public void StartEditingContent(string editable) => StartEditingContent(Enum.Parse<EditableTypes>(editable));
        public void StartEditingContent(int editableId) => StartEditingContent((EditableTypes)editableId);
        public void StartEditingContent(EditableTypes toEdit)
        {
            if (inspectedContentIndex < 0) return;
            IContent content = activePackage.GetContent(inspectedContentIndex);
            if (content == null) return;
            string contentType = "CONTENT";
            #region ContentTypes
            if (content is SourceScript) contentType = "SCRIPT";
            else if (content is Creation) contentType = "CREATION";
            else if (content is GameplayExperience) contentType = "GAMEPLAY EXPERIENCE";
            else if (content is CustomAnimation) contentType = "ANIMATION";
            else if (content is ImageAsset) contentType = "IMAGE";
            #endregion

            if (activeEditWindow != null) activeEditWindow.SetActive(false);

            Toggle GetAndDisableToggle(Transform windowTransform)
            {
                Toggle toggle = windowTransform.GetComponentInChildren<Toggle>(true);
                if (toggle != null) toggle.gameObject.SetActive(false);
                return toggle;
            }

            activeEditWindow = null;
            currentEdit = null;
            switch (toEdit)
            {

                case EditableTypes.ContentName:
                    activeEditWindow = textFieldEditWindow == null ? textAreaEditWindow : textFieldEditWindow;
                    if (activeEditWindow != null)
                    {
                        var windowTransform = activeEditWindow.transform;
                        GetAndDisableToggle(windowTransform);
                        var nameTransform = windowTransform.FindDeepChildLiberal("name"); // Find the window name transform
                        if (nameTransform != null) SetComponentText(nameTransform, $"{contentType} NAME");
                        var valueTransform = windowTransform.FindDeepChildLiberal("value");
                        InputField valueText = null;
                        TMP_InputField valueTextTMP = null;
                        if (valueTransform != null)
                        {
                            valueText = valueTransform.GetComponentInChildren<InputField>();
                            valueTextTMP = valueTransform.GetComponentInChildren<TMP_InputField>();

                            string currentValue = content.Name;
                            if (valueText != null)
                            {
                                valueText.text = currentValue;
                                if (valueText.textComponent != null) valueText.MoveTextStart(false);
                                valueText.ForceLabelUpdate();
                            }
                            if (valueTextTMP != null)
                            {
                                valueTextTMP.text = currentValue;
                                if (valueTextTMP.textComponent != null && valueTextTMP.textComponent.textInfo != null) valueTextTMP.MoveTextStart(false);
                                valueTextTMP.ForceLabelUpdate();
                            }
                        }
                        if (valueText == null && valueTextTMP == null)
                        {
                            activeEditWindow = null;
                            return;
                        }

                        var errorMessage = windowTransform.GetComponentInChildren<UIPopupMessageFadable>(true);
                        currentEdit = () => SetContentName(valueTextTMP == null ? valueText.text : valueTextTMP.text, errorMessage);
                    }
                    break;

                case EditableTypes.Creator:
                    activeEditWindow = textFieldEditWindow == null ? textAreaEditWindow : textFieldEditWindow;
                    if (activeEditWindow != null)
                    {
                        var windowTransform = activeEditWindow.transform;
                        GetAndDisableToggle(windowTransform);
                        var nameTransform = windowTransform.FindDeepChildLiberal("name"); // Find the window name transform
                        if (nameTransform != null) SetComponentText(nameTransform, $"{contentType} AUTHOR");
                        var valueTransform = windowTransform.FindDeepChildLiberal("value");
                        InputField valueText = null;
                        TMP_InputField valueTextTMP = null;
                        if (valueTransform != null)
                        {
                            valueText = valueTransform.GetComponentInChildren<InputField>();
                            valueTextTMP = valueTransform.GetComponentInChildren<TMP_InputField>();

                            string currentValue = content.Author;
                            if (valueText != null)
                            {
                                valueText.text = currentValue;
                                if (valueText.textComponent != null) valueText.MoveTextStart(false);
                                valueText.ForceLabelUpdate();
                            }
                            if (valueTextTMP != null)
                            {
                                valueTextTMP.text = currentValue;
                                if (valueTextTMP.textComponent != null && valueTextTMP.textComponent.textInfo != null) valueTextTMP.MoveTextStart(false);
                                valueTextTMP.ForceLabelUpdate();
                            }
                        }
                        if (valueText == null && valueTextTMP == null)
                        {
                            activeEditWindow = null;
                            return;
                        }

                        var errorMessage = windowTransform.GetComponentInChildren<UIPopupMessageFadable>(true);
                        currentEdit = () => SetContentAuthor(valueTextTMP == null ? valueText.text : valueTextTMP.text, errorMessage);
                    }
                    break;

                case EditableTypes.Description:
                    activeEditWindow = textAreaEditWindow == null ? textFieldEditWindow : textAreaEditWindow;
                    if (activeEditWindow != null)
                    {
                        var windowTransform = activeEditWindow.transform;
                        GetAndDisableToggle(windowTransform);
                        var nameTransform = windowTransform.FindDeepChildLiberal("name"); // Find the window name transform
                        if (nameTransform != null) SetComponentText(nameTransform, $"{contentType} DESCRIPTION");
                        var valueTransform = windowTransform.FindDeepChildLiberal("value");
                        InputField valueText = null;
                        TMP_InputField valueTextTMP = null;
                        if (valueTransform != null)
                        {
                            valueText = valueTransform.GetComponentInChildren<InputField>();
                            valueTextTMP = valueTransform.GetComponentInChildren<TMP_InputField>();

                            string currentValue = content.Description;
                            if (valueText != null)
                            {
                                valueText.text = currentValue;
                                if (valueText.textComponent != null) valueText.MoveTextStart(false);
                                valueText.ForceLabelUpdate();
                            }
                            if (valueTextTMP != null)
                            {
                                valueTextTMP.text = currentValue;
                                if (valueTextTMP.textComponent != null && valueTextTMP.textComponent.textInfo != null) valueTextTMP.MoveTextStart(false);
                                valueTextTMP.ForceLabelUpdate();
                            }
                        }
                        if (valueText == null && valueTextTMP == null)
                        {
                            activeEditWindow = null;
                            return;
                        }

                        var errorMessage = windowTransform.GetComponentInChildren<UIPopupMessageFadable>(true);
                        currentEdit = () => SetContentDescription(valueTextTMP == null ? valueText.text : valueTextTMP.text, errorMessage);
                    }
                    break;

                case EditableTypes.ScriptSource:
                    if (codeEditorWindow != null && content is SourceScript script)
                    {
                        activeEditWindow = codeEditorWindow;
                        activeEditWindow.SetActive(true);

                        IEnumerator WaitForInit()
                        {
                            yield return null;
                            yield return null;

                            var comp = codeEditorWindow.GetComponentInChildren(typeof(ICodeEditor), true);
                            ICodeEditor editor = null;
                            if (comp != null) editor = (ICodeEditor)comp;

                            if (editor != null)
                            {
                                editor.Code = script.source;
                                SetButtonOnClickAction(activeEditWindow.transform.FindDeepChildLiberal("finalize"), TryCompleteContentEdit);
                                currentEdit = () => SetScriptSource(editor.Code);
                            }
                            else activeEditWindow = null;
                        }

                        StartCoroutine(WaitForInit());
                        return;
                    }
                    break;

            }

            if (activeEditWindow != null)
            {
                activeEditWindow.SetActive(true);
                SetButtonOnClickAction(activeEditWindow.transform.FindDeepChildLiberal("finalize"), TryCompleteContentEdit);
            }
        }
        public void TryCompleteContentEdit()
        {
            if (currentEdit == null) return;
            if (activeEditWindow != null && !activeEditWindow.activeInHierarchy)
            {
                currentEdit = null;
                activeEditWindow = null;
                return;
            }
            if (currentEdit.Invoke())
            {
                activeEditWindow?.SetActive(false);
                currentEdit = null;
                activeEditWindow = null;
            }
        }

        public bool SetProjectName(string name, UIPopupMessageFadable errorMessage = null)
        {
            if (activePackage == null || !IsLocal) return false;

            bool valid = false;
            if (!string.IsNullOrEmpty(name) && projectNameField != null && projectNameField.text.ToLower() == name.ToLower()) valid = true;
            if (!string.IsNullOrEmpty(name) && projectNameFieldTMP != null && projectNameFieldTMP.text.ToLower() == name.ToLower()) valid = true;

            if (!valid && ProjectManager.ValidateProjectName(name, errorMessage)) valid = true;

            if (valid)
            {
                if (projectNameField != null) projectNameField.text = name;
                if (projectNameFieldTMP != null) projectNameFieldTMP.SetText(name);

                ContentManager.SetProjectIdentifier(activePackage, name, ContentManager.SaveMethod.Immediate);
                manager?.RefreshLocalProjects(true);

                return true;
            } 

            return false;
        }

        private readonly List<ContentManager.LocalPackage> localPackageSampler = new List<ContentManager.LocalPackage>();
        public bool SetPackageName(string name, bool renameAllPackages = true, UIPopupMessageFadable errorMessage = null)
        {
            if (activePackage == null || !IsLocal) return false;

            bool valid = false;
            if (!string.IsNullOrEmpty(name) && packageNameField != null && packageNameField.text.ToLower() == name.ToLower()) valid = true;
            if (!string.IsNullOrEmpty(name) && packageNameFieldTMP != null && packageNameFieldTMP.text.ToLower() == name.ToLower()) valid = true;

            if (!valid && ProjectManager.ValidateNewPackageName(name, activePackage.VersionString, errorMessage)) valid = true;

            if (valid)
            {
                if (packageNameField != null) packageNameField.text = name;
                if (packageNameFieldTMP != null) packageNameFieldTMP.SetText(name);

                var localPkg = ContentManager.FindLocalPackage(activePackage.GetIdentity());
                if (localPkg == null || localPkg.Content == null)
                {
                    errorMessage?.SetMessageAndShow("The local package does not exist!");
                    return false;
                }

                if (!ContentManager.TryGetProjectIdentifier(activePackage, out string projectName)) projectName = string.Empty;
                var existingPkg = ContentManager.FindLocalPackage(name);
                if (existingPkg != null && existingPkg.Content != null && ContentManager.TryGetProjectIdentifier(existingPkg, out string existingProjectName))
                {
                    if (projectName != existingProjectName)
                    {
                        errorMessage?.SetMessageAndShow($"A local package with the same name already exists in '{existingProjectName}'!");
                        return false;
                    }
                }

                SwolePackage pkg = SwolePackage.Create(activePackage);
                var manifest = pkg.Manifest;
                var info = manifest.info;
                info.name = name;
                manifest.info = info;
                pkg.UpdateManifest(manifest);
                localPkg.Content = pkg;
                if (!ContentManager.SavePackage(localPkg, swole.DefaultLogger))
                {
                    errorMessage?.SetMessageAndShow("Failed to save package!");
                    return false;
                }
                if (!string.IsNullOrEmpty(projectName)) ContentManager.SetProjectIdentifier(pkg, projectName, ContentManager.SaveMethod.Immediate);
                if (renameAllPackages)
                {
                    ContentManager.FindLocalPackages(activePackage.Name, localPackageSampler);
                    foreach(var otherLocalPkg in localPackageSampler)
                    {
                        if (otherLocalPkg == null || otherLocalPkg.Content == null || otherLocalPkg.Content.VersionString == activePackage.VersionString) continue;

                        SwolePackage otherPkg = SwolePackage.Create(otherLocalPkg.Content);
                        manifest = otherPkg.Manifest;
                        info = manifest.info;
                        info.name = name;
                        manifest.info = info;
                        otherPkg.UpdateManifest(manifest);
                        var otherLocalPkg_ = otherLocalPkg;
                        otherLocalPkg_.Content = otherPkg;
                        ContentManager.SavePackage(otherLocalPkg_, swole.DefaultLogger);
                    }
                }
                 
                activePackage = pkg;

                fullRefreshNext = true;
                Refresh();
                return true;
            }

            return false;
        }
        public bool SetVersion(string version, UIPopupMessageFadable errorMessage = null)
        {
            if (activePackage == null || !IsLocal) return false;

            bool valid = false;
            if (!string.IsNullOrEmpty(version) && versionField != null && versionField.text.ToLower() == version.ToLower()) valid = true;
            if (!string.IsNullOrEmpty(version) && versionFieldTMP != null && versionFieldTMP.text.ToLower() == version.ToLower()) valid = true;

            if (!valid && ProjectManager.ValidateNewPackageName(activePackage.Name, version, errorMessage)) valid = true;

            if (valid)
            {
                if (versionField != null) versionField.text = version;
                if (versionFieldTMP != null) versionFieldTMP.SetText(version);

                var localPkg = ContentManager.FindLocalPackage(activePackage.GetIdentity());
                if (localPkg == null || localPkg.Content == null)
                {
                    errorMessage?.SetMessageAndShow("The local package does not exist!");
                    return false;
                }

                SwolePackage pkg = SwolePackage.Create(activePackage);
                var manifest = pkg.Manifest;
                var info = manifest.info;
                info.version = version;
                manifest.info = info;
                pkg.UpdateManifest(manifest);
                localPkg.Content = pkg;
                if (!ContentManager.SavePackage(localPkg, swole.DefaultLogger))
                {
                    errorMessage?.SetMessageAndShow("Failed to save package!");
                    return false;
                }

                activePackage = pkg;

                fullRefreshNext = true;
                Refresh();
                return true;
            }

            return false;
        }

        public bool SetURL(string url, bool updateAllPackages = true, UIPopupMessageFadable errorMessage = null)
        {
            if (activePackage == null || !IsLocal) return false;

            bool valid = false;
            if (!string.IsNullOrEmpty(url) && urlField != null && urlField.text.ToLower() == url.ToLower()) valid = true;
            if (!string.IsNullOrEmpty(url) && urlFieldTMP != null && urlFieldTMP.text.ToLower() == url.ToLower()) valid = true;

            if (!valid && ProjectManager.ValidateProjectURL(url, errorMessage)) valid = true;

            if (valid)
            {
                if (urlField != null) urlField.text = url;
                if (urlFieldTMP != null) urlFieldTMP.SetText(url);

                var localPkg = ContentManager.FindLocalPackage(activePackage.GetIdentity());
                if (localPkg == null || localPkg.Content == null)
                {
                    errorMessage?.SetMessageAndShow("The local package does not exist!");
                    return false;
                }

                bool SetValueForPackage(ContentPackage contentPkg, ContentManager.LocalPackage localPkg, out SwolePackage swolePkg)
                {
                    swolePkg = SwolePackage.Create(contentPkg);
                    var manifest = swolePkg.Manifest;
                    var info = manifest.info;
                    info.url = url;
                    manifest.info = info;
                    swolePkg.UpdateManifest(manifest);
                    localPkg.Content = swolePkg;
                    return ContentManager.SavePackage(localPkg, swole.DefaultLogger);
                }

                if (!SetValueForPackage(activePackage, localPkg, out SwolePackage pkg))
                {
                    errorMessage?.SetMessageAndShow("Failed to save package!");
                    return false;
                }
                if (updateAllPackages)
                {
                    ContentManager.FindLocalPackages(activePackage.Name, localPackageSampler);
                    foreach (var otherLocalPkg in localPackageSampler)
                    {
                        if (otherLocalPkg == null || otherLocalPkg.Content == null || otherLocalPkg.Content.VersionString == activePackage.VersionString) continue;
                        SetValueForPackage(otherLocalPkg.Content, otherLocalPkg, out _);
                    }
                }
                activePackage = pkg;

                Refresh();
                fullRefreshNext = true;
                return true;
            }

            return false;
        }

        public bool SetCreator(string creator, bool updateAllPackages = true, UIPopupMessageFadable errorMessage = null)
        {
            if (activePackage == null || !IsLocal) return false;

            bool valid = false;
            if (!string.IsNullOrEmpty(creator) && creatorField != null && creatorField.text.ToLower() == creator.ToLower()) valid = true;
            if (!string.IsNullOrEmpty(creator) && creatorFieldTMP != null && creatorFieldTMP.text.ToLower() == creator.ToLower()) valid = true;

            if (!valid && ProjectManager.ValidateCreatorName(creator, errorMessage)) valid = true;

            if (valid)
            {
                if (creatorField != null) creatorField.text = creator;
                if (creatorFieldTMP != null) creatorFieldTMP.SetText(creator);

                var localPkg = ContentManager.FindLocalPackage(activePackage.GetIdentity());
                if (localPkg == null || localPkg.Content == null)
                {
                    errorMessage?.SetMessageAndShow("The local package does not exist!");
                    return false;
                }

                bool SetValueForPackage(ContentPackage contentPkg, ContentManager.LocalPackage localPkg, out SwolePackage swolePkg)
                {
                    swolePkg = SwolePackage.Create(contentPkg);
                    var manifest = swolePkg.Manifest;
                    var info = manifest.info;
                    info.curator = creator;
                    manifest.info = info;
                    swolePkg.UpdateManifest(manifest);
                    localPkg.Content = swolePkg;
                    return ContentManager.SavePackage(localPkg, swole.DefaultLogger);
                }

                if (!SetValueForPackage(activePackage, localPkg, out SwolePackage pkg))
                {
                    errorMessage?.SetMessageAndShow("Failed to save package!");
                    return false;
                }
                if (updateAllPackages)
                {
                    ContentManager.FindLocalPackages(activePackage.Name, localPackageSampler);
                    foreach (var otherLocalPkg in localPackageSampler)
                    {
                        if (otherLocalPkg == null || otherLocalPkg.Content == null || otherLocalPkg.Content.VersionString == activePackage.VersionString) continue;
                        SetValueForPackage(otherLocalPkg.Content, otherLocalPkg, out _);
                    }
                }
                activePackage = pkg;

                Refresh();
                fullRefreshNext = true;
                return true;
            }

            return false;
        }

        public bool SetTags(string tags, bool updateAllPackages = true, UIPopupMessageFadable errorMessage = null)
        {
            if (activePackage == null || !IsLocal) return false;

            bool valid = false;
            if (!string.IsNullOrEmpty(tags) && tagsField != null && tagsField.text.ToLower() == tags.ToLower()) valid = true;
            if (!string.IsNullOrEmpty(tags) && tagsFieldTMP != null && tagsFieldTMP.text.ToLower() == tags.ToLower()) valid = true;

            if (!valid && ProjectManager.ValidateTags(tags, errorMessage)) valid = true;

            if (valid)
            {
                if (tagsField != null) tagsField.text = tags;
                if (tagsFieldTMP != null) tagsFieldTMP.SetText(tags);

                var localPkg = ContentManager.FindLocalPackage(activePackage.GetIdentity());
                if (localPkg == null || localPkg.Content == null)
                {
                    errorMessage?.SetMessageAndShow("The local package does not exist!");
                    return false;
                }

                string[] tagsArray = ProjectManager.SeparateTags(tags).ToArray();

                bool SetValueForPackage(ContentPackage contentPkg, ContentManager.LocalPackage localPkg, out SwolePackage swolePkg)
                {
                    swolePkg = SwolePackage.Create(contentPkg);
                    var manifest = swolePkg.Manifest;
                    var info = manifest.info;
                    info.tags = tagsArray;
                    manifest.info = info;
                    swolePkg.UpdateManifest(manifest);
                    localPkg.Content = swolePkg;
                    return ContentManager.SavePackage(localPkg, swole.DefaultLogger);
                }

                if (!SetValueForPackage(activePackage, localPkg, out SwolePackage pkg))
                {
                    errorMessage?.SetMessageAndShow("Failed to save package!");
                    return false;
                }
                if (updateAllPackages)
                {
                    ContentManager.FindLocalPackages(activePackage.Name, localPackageSampler);
                    foreach (var otherLocalPkg in localPackageSampler)
                    {
                        if (otherLocalPkg == null || otherLocalPkg.Content == null || otherLocalPkg.Content.VersionString == activePackage.VersionString) continue;
                        SetValueForPackage(otherLocalPkg.Content, otherLocalPkg, out _);
                    }
                }
                activePackage = pkg;

                Refresh();
                fullRefreshNext = true;
                return true;
            }

            return false;
        }

        public bool SetDescription(string desc, bool updateAllPackages = true, UIPopupMessageFadable errorMessage = null)
        {
            if (activePackage == null || !IsLocal) return false;

            bool valid = false;
            if (!string.IsNullOrEmpty(desc) && descriptionField != null && descriptionField.text.ToLower() == desc.ToLower()) valid = true;
            if (!string.IsNullOrEmpty(desc) && descriptionFieldTMP != null && descriptionFieldTMP.text.ToLower() == desc.ToLower()) valid = true;

            if (!valid && ProjectManager.ValidateDescription(desc, errorMessage)) valid = true;

            if (valid)
            {
                if (descriptionField != null) descriptionField.text = desc;
                if (descriptionFieldTMP != null) descriptionFieldTMP.SetText(desc);

                var localPkg = ContentManager.FindLocalPackage(activePackage.GetIdentity());
                if (localPkg == null || localPkg.Content == null)
                {
                    errorMessage?.SetMessageAndShow("The local package does not exist!");
                    return false;
                }

                bool SetValueForPackage(ContentPackage contentPkg, ContentManager.LocalPackage localPkg, out SwolePackage swolePkg)
                {
                    swolePkg = SwolePackage.Create(contentPkg);
                    var manifest = swolePkg.Manifest;
                    var info = manifest.info;
                    info.description = desc;
                    manifest.info = info;
                    swolePkg.UpdateManifest(manifest);
                    localPkg.Content = swolePkg;
                    return ContentManager.SavePackage(localPkg, swole.DefaultLogger);
                }

                if (!SetValueForPackage(activePackage, localPkg, out SwolePackage pkg))
                {
                    errorMessage?.SetMessageAndShow("Failed to save package!");
                    return false;
                }
                if (updateAllPackages)
                {
                    ContentManager.FindLocalPackages(activePackage.Name, localPackageSampler);
                    foreach (var otherLocalPkg in localPackageSampler)
                    {
                        if (otherLocalPkg == null || otherLocalPkg.Content == null || otherLocalPkg.Content.VersionString == activePackage.VersionString) continue;
                        SetValueForPackage(otherLocalPkg.Content, otherLocalPkg, out _);
                    }
                }
                activePackage = pkg;

                Refresh();
                fullRefreshNext = true;
                return true;
            }

            return false;
        }

        public bool SetContentName(string contentName, UIPopupMessageFadable errorMessage = null)
        {
            if (activePackage == null || !IsLocal || inspectedContentIndex < 0) return false;
            IContent content = activePackage.GetContent(inspectedContentIndex);
            if (content == null) return false;
            Type contentType = content.GetType();

            bool valid = false;
            if (!string.IsNullOrEmpty(contentName) && content.Name.ToLower() == contentName.ToLower()) valid = true;

            if (!valid && ProjectManager.ValidateContentName(contentName, errorMessage)) valid = true;
            if (valid && activePackage.Contains(contentName, contentType))
            {
                errorMessage?
                    .SetMessage($"A {contentType.Name} with the name '{contentName}' already exists!")
                    .SetDisplayTime(errorMessage.DefaultDisplayTime)
                    .Show();
                return false;
            }

            if (valid)
            {

                var localPkg = ContentManager.FindLocalPackage(activePackage.GetIdentity());
                if (localPkg == null || localPkg.Content == null)
                {
                    errorMessage?.SetMessageAndShow("The local package does not exist!");
                    return false;
                }

                ContentInfo newInfo = content.ContentInfo;
                newInfo.name = contentName;

                bool SetValueForPackage(ContentPackage contentPkg, ContentManager.LocalPackage localPkg, out SwolePackage swolePkg)
                {
                    swolePkg = SwolePackage.Create(contentPkg);
                    swolePkg.Replace(content, content.CreateCopyAndReplaceContentInfo(newInfo));
                    localPkg.Content = swolePkg;
                    return ContentManager.SavePackage(localPkg, swole.DefaultLogger);
                }

                if (!SetValueForPackage(activePackage, localPkg, out SwolePackage pkg))
                {
                    errorMessage?.SetMessageAndShow("Failed to save package!");
                    return false;
                }
                activePackage = pkg;

                IEnumerator Reload()
                {

                    yield return null;

                    fullRefreshNext = true;
                    Refresh();

                    yield return null;

                    StartInspectingContent(activePackage.IndexOf(contentName, contentType));

                }

                StartCoroutine(Reload());

                return true;
            }

            return false;
        }

        public bool SetContentAuthor(string author, UIPopupMessageFadable errorMessage = null)
        {
            if (activePackage == null || !IsLocal || inspectedContentIndex < 0) return false;
            IContent content = activePackage.GetContent(inspectedContentIndex);
            if (content == null) return false;
            string contentName = content.Name;
            Type contentType = content.GetType();

            bool valid = false;
            if (!string.IsNullOrEmpty(author) && content.Author.ToLower() == author.ToLower()) valid = true;

            if (!valid && ProjectManager.ValidateCreatorName(author, errorMessage)) valid = true;

            if (valid)
            {

                var localPkg = ContentManager.FindLocalPackage(activePackage.GetIdentity());
                if (localPkg == null || localPkg.Content == null)
                {
                    errorMessage?.SetMessageAndShow("The local package does not exist!");
                    return false;
                }

                ContentInfo newInfo = content.ContentInfo;
                newInfo.author = author;

                bool SetValueForPackage(ContentPackage contentPkg, ContentManager.LocalPackage localPkg, out SwolePackage swolePkg)
                {
                    swolePkg = SwolePackage.Create(contentPkg);
                    swolePkg.Replace(content, content.CreateCopyAndReplaceContentInfo(newInfo));
                    localPkg.Content = swolePkg;
                    return ContentManager.SavePackage(localPkg, swole.DefaultLogger);
                }

                if (!SetValueForPackage(activePackage, localPkg, out SwolePackage pkg))
                {
                    errorMessage?.SetMessageAndShow("Failed to save package!");
                    return false;
                }
                activePackage = pkg;

                IEnumerator Reload()
                {

                    yield return null;

                    fullRefreshNext = true;
                    Refresh();

                    yield return null;

                    StartInspectingContent(activePackage.IndexOf(contentName, contentType));

                }

                StartCoroutine(Reload());

                return true;
            }

            return false;
        }

        public bool SetContentDescription(string desc, UIPopupMessageFadable errorMessage = null)
        {
            if (activePackage == null || !IsLocal || inspectedContentIndex < 0) return false;
            IContent content = activePackage.GetContent(inspectedContentIndex);
            if (content == null) return false;
            string contentName = content.Name;
            Type contentType = content.GetType();
            bool valid = false;
            if (!string.IsNullOrEmpty(desc) && content.Description.ToLower() == desc.ToLower()) valid = true;

            if (!valid && ProjectManager.ValidateDescription(desc, errorMessage)) valid = true;

            if (valid)
            {

                var localPkg = ContentManager.FindLocalPackage(activePackage.GetIdentity());
                if (localPkg == null || localPkg.Content == null)
                {
                    errorMessage?.SetMessageAndShow("The local package does not exist!");
                    return false;
                }

                ContentInfo newInfo = content.ContentInfo;
                newInfo.description = desc;

                bool SetValueForPackage(ContentPackage contentPkg, ContentManager.LocalPackage localPkg, out SwolePackage swolePkg)
                {
                    swolePkg = SwolePackage.Create(contentPkg);
                    swolePkg.Replace(content, content.CreateCopyAndReplaceContentInfo(newInfo));
                    localPkg.Content = swolePkg;
                    return ContentManager.SavePackage(localPkg, swole.DefaultLogger);
                }

                if (!SetValueForPackage(activePackage, localPkg, out SwolePackage pkg))
                {
                    errorMessage?.SetMessageAndShow("Failed to save package!");
                    return false;
                }
                activePackage = pkg;

                IEnumerator Reload()
                {

                    yield return null;

                    fullRefreshNext = true;
                    Refresh();

                    yield return null;

                    StartInspectingContent(activePackage.IndexOf(contentName, contentType));

                }

                StartCoroutine(Reload());

                return true;
            }

            return false;
        }

        public bool SetScriptSource(string source, UIPopupMessageFadable errorMessage = null)
        {
            if (activePackage == null || !IsLocal || inspectedContentIndex < 0) return false;
            IContent content = activePackage.GetContent(inspectedContentIndex);
            if (content == null || !(content is SourceScript script)) return false;
            string contentName = content.Name;
            Type contentType = content.GetType();

            var localPkg = ContentManager.FindLocalPackage(activePackage.GetIdentity());
            if (localPkg == null || localPkg.Content == null)
            {
                errorMessage?.SetMessageAndShow("The local package does not exist!");
                return false;
            }

            script.source = source;

            bool SetValueForPackage(ContentPackage contentPkg, ContentManager.LocalPackage localPkg, out SwolePackage swolePkg)
            {
                swolePkg = SwolePackage.Create(contentPkg);
                swolePkg.Replace(content, script);
                localPkg.Content = swolePkg;
                return ContentManager.SavePackage(localPkg, swole.DefaultLogger);
            }

            if (!SetValueForPackage(activePackage, localPkg, out SwolePackage pkg))
            {
                errorMessage?.SetMessageAndShow("Failed to save package!");
                return false;
            }
            activePackage = pkg;

            IEnumerator Reload()
            {

                yield return null;

                fullRefreshNext = true;
                Refresh();

                yield return null;

                StartInspectingContent(activePackage.IndexOf(contentName, contentType));

            }

            StartCoroutine(Reload());

            return true;

        }

        public const string _id_Type = "Type";
        public const string _id_Author = "Author";

        #region ContentTypes
        public const string _id_ScriptName = "Script Name";
        public const string _id_CreationName = "Creation Name";
        public const string _id_GameplayExperienceName = "Experience Name";
        public const string _id_AnimationName = "Animation Name";
        public const string _id_ImageName = "Image Name";
        #endregion

        public const string _id_Input = "Input";

        #region ContentTypes
        public const string _id_Content_Script = "script";
        public const string _id_Content_Creation = "creation";
        public const string _id_Content_GameplayExperience = "experience";
        public const string _id_Content_Animation = "animation";
        public const string _id_Content_Image = "image";
        #endregion

        public const string _defaultProjectName = "New Project";
        public const string _defaultPackageName = "my.package.name";
        public const string _defaultVersionString = "0.1.0";
        public void OpenNewContentWindow(GameObject creatorWindow)
        {
            if (creatorWindow == null || activePackage == null || !isLocal) return;

            #region ContentTypes
            Transform scriptName = creatorWindow.transform.FindDeepChildLiberal(_id_ScriptName);
            Transform creationName = creatorWindow.transform.FindDeepChildLiberal(_id_CreationName);
            Transform experienceName = creatorWindow.transform.FindDeepChildLiberal(_id_GameplayExperienceName);
            Transform animationName = creatorWindow.transform.FindDeepChildLiberal(_id_AnimationName);
            Transform imageName = creatorWindow.transform.FindDeepChildLiberal(_id_ImageName);

            SetInputFieldText(scriptName, string.Empty);
            SetInputFieldText(creationName, string.Empty);
            SetInputFieldText(experienceName, string.Empty);
            SetInputFieldText(animationName, string.Empty);
            SetInputFieldText(imageName, string.Empty);
            #endregion

            Transform auth = creatorWindow.transform.FindDeepChildLiberal(_id_Author);
            Transform desc = creatorWindow.transform.FindDeepChildLiberal(ProjectManager._id_description);

            SetInputFieldText(auth, string.Empty);
            SetInputFieldText(desc, string.Empty);

            creatorWindow.SetActive(true); 
        }

        public static bool ValidateContentTypeName(string typeName)
        {

            if (string.IsNullOrEmpty(typeName)) return false;

            typeName = typeName.ToLower();

            #region ContentTypes
            if (typeName == _id_Content_Script) return true;
            if (typeName == _id_Content_Creation) return true;
            if (typeName == _id_Content_GameplayExperience) return true;
            if (typeName == _id_Content_Animation) return true;
            if (typeName == _id_Content_Image) return true;
            #endregion

            return false;

        }

        public void CreateNewContent(GameObject creatorWindow)
        { 
            if (creatorWindow == null || activePackage == null || !isLocal) return;

            UIPopupMessageFadable errorMessage = creatorWindow.GetComponentInChildren<UIPopupMessageFadable>(true);

            Transform typ = creatorWindow.transform.FindDeepChildLiberal(_id_Type);

            string typeName = GetComponentText(typ).ToLower();
            if (!ValidateContentTypeName(typeName)) return;

            Transform auth = creatorWindow.transform.FindDeepChildLiberal(_id_Author);
            Transform desc = creatorWindow.transform.FindDeepChildLiberal(ProjectManager._id_description);

            string author = GetInputFieldText(auth).Trim();
            string description = GetInputFieldText(desc);

            if (!ProjectManager.ValidateCreatorName(author, errorMessage)) return;
            if (!ProjectManager.ValidateDescription(description, errorMessage)) return;

            // > Create the content asset

            SwolePackage package = SwolePackage.Create(activePackage);
            ContentManager.LocalPackage localPackage = ContentManager.FindLocalPackage(activePackage.GetIdentity());
            if (localPackage == null || localPackage.Content == null)
            {
                swole.LogError($"Tried to create a new {typeName} for {activePackage}, but it wasn't found under the registered packages.");
                return;
            }

            IContent content = null;
            #region ContentTypes
            if (typeName == _id_Content_Script)
            {
                Transform nme = creatorWindow.transform.FindDeepChildLiberal(_id_ScriptName);
                string name = GetInputFieldText(nme).Trim();
                if (!ProjectManager.ValidateContentName(name, errorMessage)) return;
                if (activePackage.Contains(name, typeof(SourceScript)))
                {
                    errorMessage?
                        .SetMessage($"A script with the name '{name}' already exists!")
                        .SetDisplayTime(errorMessage.DefaultDisplayTime)
                        .Show();
                    return;
                }
                DateTime date = DateTime.Now;
                content = new SourceScript(name, author, date, date, description, string.Empty, activePackage.Manifest);
            } 
            else if (typeName == _id_Content_Creation)
            {
                Transform nme = creatorWindow.transform.FindDeepChildLiberal(_id_CreationName);
                string name = GetInputFieldText(nme).Trim();
                if (!ProjectManager.ValidateContentName(name, errorMessage)) return;
                if (activePackage.Contains(name, typeof(Creation)))
                {
                    errorMessage?
                        .SetMessage($"A creation with the name '{name}' already exists!")
                        .SetDisplayTime(errorMessage.DefaultDisplayTime)
                        .Show();
                    return;
                }
                DateTime date = DateTime.Now;
                content = new Creation(name, author, date, date, description, new CreationScript(), null, activePackage.Manifest); 
            }
            else if (typeName == _id_Content_Animation)
            {
                Transform nme = creatorWindow.transform.FindDeepChildLiberal(_id_AnimationName);
                string name = GetInputFieldText(nme).Trim();
                if (!ProjectManager.ValidateContentName(name, errorMessage)) return;
                if (activePackage.Contains(name, typeof(CustomAnimation)))
                {
                    errorMessage?
                        .SetMessage($"An animation with the name '{name}' already exists!")
                        .SetDisplayTime(errorMessage.DefaultDisplayTime)
                        .Show();
                    return;
                }
                DateTime date = DateTime.Now;
                content = new CustomAnimation(name, author, date, date, description, CustomAnimation.DefaultFrameRate, CustomAnimation.DefaultJobCurveSampleRate, null, null, null, null, null, null, null, null, activePackage.Manifest); 
            }
            #endregion
            if (content == null)
            {
                swole.LogError($"Tried to create a new {typeName} for {activePackage}, but the returned content was null.");
                return;
            }

            package.Add(content);
            activePackage = package;
            ContentManager.SavePackage(localPackage.workingDirectory, activePackage, swole.DefaultLogger);

            // <

            creatorWindow.SetActive(false);

            IEnumerator Reload()
            {

                yield return null;

                RefreshContentList();
                fullRefreshNext = true;

                yield return null;

                FilterContent(UI.FilterMode.Newest);

            }

            StartCoroutine(Reload());

        }

        public void OpenNewPackageWindow(GameObject creatorWindow)
        {
            if (creatorWindow == null) return;

            Transform projUrl = creatorWindow.transform.FindDeepChildLiberal(ProjectManager._id_URL);
            Transform pkgName = creatorWindow.transform.FindDeepChildLiberal(ProjectManager._id_packageName);
            Transform version = creatorWindow.transform.FindDeepChildLiberal(ProjectManager._id_version);
            Transform ctr = creatorWindow.transform.FindDeepChildLiberal(ProjectManager._id_creator);
            Transform tgs = creatorWindow.transform.FindDeepChildLiberal(ProjectManager._id_tags);
            Transform desc = creatorWindow.transform.FindDeepChildLiberal(ProjectManager._id_description);

            SetInputFieldText(projUrl, string.Empty);
            SetInputFieldText(pkgName, _defaultPackageName);
            SetInputFieldText(version, _defaultVersionString); 
            SetInputFieldText(ctr, string.Empty);
            SetInputFieldText(tgs, string.Empty);
            SetInputFieldText(desc, string.Empty);

            creatorWindow.SetActive(true);
        }
        public void CreateNewPackage(GameObject creatorWindow)
        {
            if (creatorWindow == null || activePackage == null || !isLocal) return;

            UIPopupMessageFadable errorMessage = creatorWindow.GetComponentInChildren<UIPopupMessageFadable>(true);

            Transform projUrl = creatorWindow.transform.FindDeepChildLiberal(ProjectManager._id_URL);
            Transform pkgName = creatorWindow.transform.FindDeepChildLiberal(ProjectManager._id_packageName);
            Transform version = creatorWindow.transform.FindDeepChildLiberal(ProjectManager._id_version);
            Transform ctr = creatorWindow.transform.FindDeepChildLiberal(ProjectManager._id_creator);
            Transform tgs = creatorWindow.transform.FindDeepChildLiberal(ProjectManager._id_tags);
            Transform desc = creatorWindow.transform.FindDeepChildLiberal(ProjectManager._id_description);

            string url = GetInputFieldText(projUrl).Trim();
            string packageName = GetInputFieldText(pkgName).Trim();
            string versionString = GetInputFieldText(version).Trim();
            string creator = GetInputFieldText(ctr).Trim();
            string combinedTags = GetInputFieldText(tgs).Trim();
            string description = GetInputFieldText(desc);

            if (!ProjectManager.ValidateProjectURL(url, errorMessage)) return;
            if (!ProjectManager.ValidateNewPackageName(packageName, versionString, errorMessage)) return;
            if (!ProjectManager.ValidateVersionString(versionString, errorMessage)) return;
            if (!ProjectManager.ValidateCreatorName(creator, errorMessage)) return;
            if (!ProjectManager.ValidateTags(combinedTags, errorMessage)) return;
            if (!ProjectManager.ValidateDescription(description, errorMessage)) return;

            // > Create the project

            ContentManager.Project project = default;
            if (!ContentManager.TryFindProject(ContentManager.GetProjectIdentifier(activePackage), out project))
            {
                project.name = ContentManager.GetProjectIdentifier(activePackage);
                project.primaryPath = Path.Combine(ContentManager.LocalPackageDirectoryPath, project.name);       
            } 
            Directory.CreateDirectory(project.primaryPath);

            List<string> validTags = ProjectManager.SeparateTags(combinedTags);

            PackageManifest manifest = new PackageManifest()
            {

                info = new PackageInfo()
                {
                    curator = creator,
                    name = packageName,
                    version = versionString,
                    tags = validTags.ToArray(),
                    description = description,
                    url = url
                }

            };

            ContentManager.SavePackage(Path.Combine(project.primaryPath, manifest.ToString()), new ContentPackage(manifest), swole.DefaultLogger);
            ContentManager.SetProjectIdentifier(packageName, project.name, ContentManager.SaveMethod.Immediate);

            // <

            creatorWindow.SetActive(false);

            IEnumerator Reload()
            {

                yield return null;

                fullRefreshNext = true;
                Refresh();

                yield return null;

                SetActiveLocalPackage(packageName, versionString);

            }
             
            StartCoroutine(Reload());
        }

        public void OpenPackageCloningWindow(GameObject creatorWindow)
        {
            if (creatorWindow == null || activePackage == null || !isLocal) return;

            Transform pkgName = creatorWindow.transform.FindDeepChildLiberal(ProjectManager._id_packageName);
            if (pkgName != null) pkgName = pkgName.FindDeepChildLiberal("value");
            Transform version = creatorWindow.transform.FindDeepChildLiberal(ProjectManager._id_version);

            SetComponentText(pkgName, activePackage.Name);
            Version ver = activePackage.Version;
            int major = Mathf.Max(0, ver.Major);
            int minor = Mathf.Max(0, ver.Minor);
            int build = ver.Build;
            int revision = ver.Revision;
            if (build < 0 && revision < 0)
            {
                ver = new Version(major, minor + 1);
            }
            else if (revision < 0)
            {
                ver = new Version(major, minor, build + 1);
            }
            else
            {
                ver = new Version(major, minor, build, revision + 1);
            }
            SetInputFieldText(version, ver.ToString());

            creatorWindow.SetActive(true);
        }
        public void ClonePackage(GameObject creatorWindow)
        {
            if (creatorWindow == null || activePackage == null || !isLocal) return;

            UIPopupMessageFadable errorMessage = creatorWindow.GetComponentInChildren<UIPopupMessageFadable>(true);

            Transform version = creatorWindow.transform.FindDeepChildLiberal(ProjectManager._id_version);

            string versionString = GetInputFieldText(version).Trim();

            if (!ProjectManager.ValidateVersionString(versionString, errorMessage)) return;

            if (ContentManager.CheckIfLocalPackageExists(activePackage.Name, versionString))
            {
                errorMessage?.SetMessage($"Version '{versionString}' already exists!").SetDisplayTime(errorMessage.DefaultDisplayTime).Show();
                return; 
            }

            // > Create the project

            ContentManager.Project project = default;
            if (!ContentManager.TryFindProject(ContentManager.GetProjectIdentifier(activePackage), out project))
            {
                project.name = ContentManager.GetProjectIdentifier(activePackage);
                project.primaryPath = Path.Combine(ContentManager.LocalPackageDirectoryPath, project.name);
            }
            Directory.CreateDirectory(project.primaryPath);

            PackageManifest manifest = activePackage.Manifest;
            PackageInfo info = manifest.info;
            info.version = versionString;
            manifest.info = info;

            SwolePackage clonedPkg = SwolePackage.Create(activePackage);
            clonedPkg.UpdateManifest(manifest);
            ContentManager.SavePackage(Path.Combine(project.primaryPath, manifest.ToString()), clonedPkg, swole.DefaultLogger);

            // <

            creatorWindow.SetActive(false);

            IEnumerator Reload()
            {

                yield return null;

                fullRefreshNext = true;
                Refresh();

                yield return null;

                SetActiveLocalPackage(clonedPkg.Name, clonedPkg.VersionString);

            }

            StartCoroutine(Reload());
        }

        public void ShowWarningWindow(string message, UnityAction OnAgree = null, UnityAction OnCancel = null)
        {
            if (warningWindow != null)
            {
                warningWindow.SetActive(true);

                var windowTransform = warningWindow.transform;
                var messageTransform = windowTransform.FindDeepChildLiberal("message");
                if (messageTransform != null) SetComponentText(messageTransform, message);

                Toggle toggle = warningWindow.GetComponentInChildren<Toggle>();

                void Agree()
                {
                    if (toggle != null) hideWarnings = toggle.isOn;
                    OnAgree?.Invoke();
                    warningWindow.SetActive(false);
                }

                void Cancel()
                {
                    OnCancel?.Invoke();
                    warningWindow.SetActive(false);
                }

                SetButtonOnClickActionByName(windowTransform, "finalize", Agree);
                SetButtonOnClickActionByName(windowTransform, "cancel", Cancel);
            }
        }

        public void DeleteActivePackage()
        {
            if (activePackage == null || !isLocal) return;

            var localPkg = ContentManager.FindLocalPackage(activePackage.GetIdentity());
            if (localPkg == null || localPkg.Content == null)
            {
                defaultErrorMessage?.SetMessage("The local package does not exist!").SetDisplayTime(defaultErrorMessage.DefaultDisplayTime).Show();
                return;
            }

            if (localPkg.workingDirectory == null || !localPkg.workingDirectory.Exists)
            {
                defaultErrorMessage?.SetMessage("The package directory does not exist in the local file system!").SetDisplayTime(defaultErrorMessage.DefaultDisplayTime).Show();
                return;
            }

            void Delete()
            {
                localPkg.workingDirectory.Delete(true);

                activePackage = null;
                IEnumerator Reload()
                {

                    yield return null;

                    fullRefreshNext = true;
                    Refresh();

                    yield return null;

                    ContentManager.CleanProjectInfo();

                }

                StartCoroutine(Reload());
            }

            if (hideWarnings)
            {
                Delete();
            } 
            else
            {
                ShowWarningWindow($"Are you sure you want to delete the package '{activePackage}'? This action cannot be undone!", Delete);
            }
        }

        public void DeleteContent(int contentIndex)
        {
            if (contentIndex < 0 || activePackage == null || !isLocal || contentIndex >= activePackage.ContentCount) return;

            var localPkg = ContentManager.FindLocalPackage(activePackage.GetIdentity());
            if (localPkg == null || localPkg.Content == null)
            {
                defaultErrorMessage?.SetMessage("The local package does not exist!").SetDisplayTime(defaultErrorMessage.DefaultDisplayTime).Show();
                return;
            }

            if (localPkg.workingDirectory == null || !localPkg.workingDirectory.Exists)
            {
                defaultErrorMessage?.SetMessage("The package directory does not exist in the local file system!").SetDisplayTime(defaultErrorMessage.DefaultDisplayTime).Show();
                return;
            }

            IContent content = activePackage.GetContent(contentIndex);

            void Delete()
            {
                SwolePackage pkg = SwolePackage.Create(activePackage);
                pkg.Remove(content);
                activePackage = pkg;

                ContentManager.SavePackage(localPkg.workingDirectory, activePackage, swole.DefaultLogger);

                IEnumerator Reload()
                {

                    yield return null;

                    RefreshContentList();
                    fullRefreshNext = true;

                }

                StartCoroutine(Reload());
            }

            if (hideWarnings)
            {
                Delete();
            }
            else
            {
                ShowWarningWindow($"Are you sure you want to delete the {content.GetType().Name} '{content.Name}'? This action cannot be undone!", Delete);
            }
        }

    }

}

#endif