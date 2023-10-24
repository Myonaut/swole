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

namespace Swole.API.Unity
{

    public class PackageViewer : MonoBehaviour
    {

        [SerializeField, Header("Main")]
        protected ProjectManager manager;

        protected ContentPackage activePackage;
        public ContentPackage ActivePackage => activePackage;

        protected bool isLocal;
        public bool IsLocal => isLocal;

        [SerializeField, Header("Prototypes")]
        protected GameObject packageContentPrototype;

        [Header("Icons")]
        public Sprite ico_Script;
        public Sprite ico_Creation;
        public Sprite ico_Text;

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
        protected UIPopupMessageFadable errorMessage;

        protected void Awake()
        {

            if (contentLayoutGroup != null) contentLayoutGroupTransform = contentLayoutGroup.GetComponent<RectTransform>();

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

        private static readonly List<ContentPackage> _tempPackageList = new List<ContentPackage>();
        public void Refresh()
        {
            if (activePackage != null)
            {
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

                if (activePackage.Tags != null)
                {
                    if (tagsField != null) tagsField.text = string.Join(", ", activePackage.Tags);
                    if (tagsFieldTMP != null) tagsFieldTMP.SetText(string.Join(", ", activePackage.Tags)); 
                }

                if (descriptionField != null) descriptionField.text = activePackage.Description;
                if (descriptionFieldTMP != null) descriptionFieldTMP.SetText(activePackage.Description);

                if (contentLayoutGroupTransform != null)
                {
                    foreach (Transform child in contentLayoutGroupTransform) Destroy(child.gameObject);
                    if (packageContentPrototype != null)
                    {
                        for (int a = 0; a < activePackage.ContentCount; a++)
                        {
                            var content = activePackage[a];
                            if (content == null) continue;
                            var listObject = Instantiate(packageContentPrototype);
                            string contentName = content.Name;
                            if (string.IsNullOrEmpty(contentName) && !string.IsNullOrEmpty(content.OriginPath)) contentName = Path.GetFileName(content.OriginPath);
                            //contentName = Path.Combine(content.RelativePath, contentName);
                            listObject.name = contentName;
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
                                        if (editButton.onClick == null) editButton.onClick = new Button.ButtonClickedEvent();
                                        editButton.onClick.AddListener(OnPress);
                                    }
                                }
                                var editTabButton = listObjectTransform.FindFirstComponentUnderChild<UITabButton>(buttonName);
                                if (editTabButton != null)
                                {
                                    editTabButton.gameObject.SetActive(true);
                                    if (OnPress != null)
                                    {
                                        if (editTabButton.OnClick == null) editTabButton.OnClick = new UnityEvent();
                                        editTabButton.OnClick.AddListener(OnPress);
                                    }
                                }

                            }
                            if (content is Creation creation)
                            {
                                if (icon != null) icon.sprite = ico_Creation;
                                ActivateActionButton("inspect", null);
                                if (IsLocal) 
                                { 
                                    ActivateActionButton("edit", null);
                                    ActivateActionButton("delete", null);
                                }
                            }
                            else if (content is SourceScript script)
                            {
                                if (icon != null) icon.sprite = ico_Script;
                                ActivateActionButton("inspect", null);
                                if (IsLocal) 
                                { 
                                    ActivateActionButton("edit", null);
                                    ActivateActionButton("delete", null);
                                }
                            }
                        }
                    }
                }
            }  
            else
            {
                if (packageSelectionDropdown != null) packageSelectionDropdown.ClearMenuItems();

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

                if (contentLayoutGroupTransform != null)
                {
                    foreach (Transform child in contentLayoutGroupTransform) Destroy(child.gameObject);
                }
            }
        }
         
        public void SetProjectName(string name)
        {
            if (!string.IsNullOrEmpty(name) && projectNameField != null && projectNameField.text.ToLower() == name.ToLower()) return;
            if (!string.IsNullOrEmpty(name) && projectNameFieldTMP != null && projectNameFieldTMP.text.ToLower() == name.ToLower()) return;
            if (!ProjectManager.ValidateNewProjectName(name, errorMessage)) return;

            if (projectNameField != null) projectNameField.text = name;
            if (projectNameFieldTMP != null) projectNameFieldTMP.SetText(name);

            ContentManager.SetProjectIdentifier(activePackage, name, ContentManager.SaveMethod.Immediate);
            if (manager != null) manager.RefreshLocalProjects(true);
        }
    }

}

#endif