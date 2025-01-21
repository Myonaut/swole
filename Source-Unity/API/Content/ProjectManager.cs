#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

using TMPro;

using Swole.UI;

using static Swole.API.Unity.CustomEditorUtils; 

namespace Swole.API.Unity
{

    public class ProjectManager : MonoBehaviour
    {

        private readonly List<ContentPackage> _tempPackages = new List<ContentPackage>();

        [Header("Layouts")]
        public LayoutGroup localProjectsLayout;
        protected RectTransform localProjectsLayoutTransform;
        public LayoutGroup externalPackagesLayout;
        protected RectTransform externalPackagesLayoutTransform;

        public GameObject reloadingLocalProjectsMessage;
        public GameObject reloadingExternalPackagesMessage;

        [Header("Views")]
        public PackageViewer packageViewer;

        [Header("Pools")]
        public PrefabPool listMemberPool;

        [Header("Icons")]
        public Sprite ico_Project;
        public Sprite ico_Package;

        [Header("Action Windows")]
        public GameObject action_NewProject;

        protected readonly List<RectTransform> localListMembers = new List<RectTransform>();
        protected readonly List<RectTransform> externalListMembers = new List<RectTransform>();

        private UITabGroup tabGroup;

        protected virtual void Awake()
        {

            if (localProjectsLayout != null) localProjectsLayoutTransform = localProjectsLayout.GetComponent<RectTransform>();
            else
            {
                swole.DefaultLogger.LogError($"localProjectsLayout field not set on ProjectManager '{name}'");
                enabled = false;
                return;
            }
            if (externalPackagesLayout != null)
            {
                externalPackagesLayoutTransform = externalPackagesLayout.GetComponent<RectTransform>();
            }
            else
            {
                swole.DefaultLogger.LogError($"externalPackagesLayout field not set on ProjectManager '{name}'");
                enabled = false;
                return;
            }

            tabGroup = gameObject.AddOrGetComponent<UITabGroup>();
            tabGroup.allowNullActive = true;

        }

        protected virtual void Start()
        {
            if (!enabled) return;
            bool reload = !ContentManager.Initialize(false); // Reload packages if the content manager has already been initialized.

            RefreshLists(reload);
        }

        protected RectTransform CreateNewListMemberFromPackage(string name, ContentPackage package, RectTransform listTransform, bool isLocal)
        {
            if (package == null || listMemberPool == null) return null;

            package = isLocal ? ContentManager.FindLocalPackage(package.Name) : ContentManager.FindExternalPackage(package.Name); // Find latest version of package

            if (!listMemberPool.TryGetNewInstance(out GameObject member)) return null;
            member.name = name;
            RectTransform transform = member.GetComponent<RectTransform>();
            transform.SetParent(listTransform, false);

            Transform iconTransform = transform.FindDeepChildLiberal("Icon");
            if (iconTransform != null)
            {
                Image image = iconTransform.GetComponent<Image>();
                if (image != null)
                {
                    if (isLocal)
                    {
                        image.sprite = ico_Project;
                    }
                    else
                    {
                        image.sprite = ico_Package;
                    }
                }
            }

            Transform nameTransform = transform.FindDeepChildLiberal("Name");
            if (nameTransform != null)
            {
                TMP_Text tmpText = nameTransform.GetComponent<TMP_Text>();
                if (tmpText != null) tmpText.SetText(name);
                Text text = nameTransform.GetComponent<Text>();
                if (text != null) text.text = name;
            }

            Transform infoTransform = transform.FindDeepChildLiberal("Info");
            if (infoTransform != null)
            {
                TMP_Text tmpText = infoTransform.GetComponent<TMP_Text>();
                Text text = infoTransform.GetComponent<Text>();
                if (isLocal)
                {
                    int count = ContentManager.GetNumberOfPackagesInProject(ContentManager.GetProjectIdentifier(package));
                    if (tmpText != null) tmpText.SetText($"{count} PACKAGE{(count == 1 ? string.Empty : "S")}");
                    if (text != null) text.text = $"{count}  PACKAGE{(count == 1 ? string.Empty : "S")}";
                }
                else
                {
                    int count = ContentManager.GetNumberOfExternalPackageVersions(package.Name);
                    if (tmpText != null) tmpText.SetText($"{count} VERSION{(count == 1 ? string.Empty : "S")}");
                    if (text != null) text.text = $"{count}  VERSION{(count == 1 ? string.Empty : "S")}";
                }
            }

            UITabButton button = member.GetComponentInChildren<UITabButton>();
            if (button != null)
            {
                if (button.OnClick == null) button.OnClick = new UnityEvent();
                button.OnClick.AddListener(new UnityAction(() => { packageViewer?.SetActivePackage(package, isLocal); }));
                tabGroup.Add(button);
            }

            member.SetActive(true);

            return transform;
        }

        protected class Flag
        {
            public bool value;
            public static implicit operator bool(Flag flag) => flag.value;
        }

        protected readonly Flag refreshingLists = new Flag();
        protected readonly Flag refreshingLocalProjects = new Flag();
        protected readonly Flag refreshingExternalpackages = new Flag();

        public Coroutine RefreshLists(bool reload = false)
        {
            if (refreshingLocalProjects || refreshingExternalpackages || refreshingLists) return null;
            return RefreshListsInternal(reload);
        }
        protected virtual Coroutine RefreshListsInternal(bool reload = false)
        {
            if (refreshingLists) return null;
            refreshingLists.value = true;

            IEnumerator Refresh()
            {

                yield return RefreshLocalProjectsInternal(reload);
                yield return RefreshExternalPackagesInternal(reload);

                refreshingLists.value = false;

            }

            return StartCoroutine(Refresh());

        }

        public delegate void RefreshDelegate();

        protected virtual Coroutine RefreshList(RefreshDelegate refreshDelegate, Flag stateFlag, GameObject messageObject, RefreshDelegate reloadContent, List<RectTransform> listMembers, bool reload = false)
        {

            if (stateFlag) return null;
            stateFlag.value = true;

            if (messageObject != null) messageObject.SetActive(true);

            IEnumerator Refresh()
            {

                yield return null;

                if (reload) reloadContent();

                foreach (var member in listMembers) if (member != null)
                    {
                        tabGroup?.Remove(member.gameObject);
                        listMemberPool.Release(member.gameObject);
                    }
                listMembers.Clear();

                yield return null;

                refreshDelegate();

                if (messageObject != null) messageObject.SetActive(false);
                stateFlag.value = false;

            }

            return StartCoroutine(Refresh());
        }

        private readonly List<string> _tempNames = new List<string>();
        public Coroutine RefreshLocalProjects(bool reload = false)
        {
            if (refreshingLocalProjects || refreshingExternalpackages || refreshingLists) return null;
            return RefreshLocalProjectsInternal(reload);
        }
        protected virtual Coroutine RefreshLocalProjectsInternal(bool reload = false)
        {
            void Refresh()
            {

                _tempNames.Clear();
                for (int a = 0; a < ContentManager.LocalPackageCount; a++)
                {
                    var package = ContentManager.GetLocalPackage(a);
                    if (package == null || package.Content == null) continue;
                    string projName = ContentManager.GetProjectIdentifier(package);
                    if (_tempNames.Contains(projName.AsID())) continue;
                    var member = CreateNewListMemberFromPackage(projName, package.Content, localProjectsLayoutTransform, true);
                    if (member != null)
                    {
                        _tempNames.Add(projName.AsID());
                        localListMembers.Add(member);
                    }
                }

            }

            return RefreshList(Refresh, refreshingLocalProjects, reloadingLocalProjectsMessage, ContentManager.ReloadLocalPackages, localListMembers, reload);
        }

        public Coroutine RefreshExternalPackages(bool reload = false)
        {
            if (refreshingLocalProjects || refreshingExternalpackages || refreshingLists) return null;
            return RefreshExternalPackagesInternal(reload);
        }
        protected virtual Coroutine RefreshExternalPackagesInternal(bool reload = false)
        {
            void Refresh()
            {

                _tempNames.Clear();
                for (int a = 0; a < ContentManager.ExternalPackageCount; a++)
                {
                    var package = ContentManager.GetExternalPackage(a);
                    if (package.content == null || _tempNames.Contains(package.content.Name.AsID())) continue;
                    var member = CreateNewListMemberFromPackage(package.content.Name, package.content, externalPackagesLayoutTransform, false);
                    if (member != null)
                    {
                        _tempNames.Add(package.content.Name.AsID());
                        externalListMembers.Add(member);
                    }
                }

            }
            return RefreshList(Refresh, refreshingExternalpackages, reloadingExternalPackagesMessage, ContentManager.ReloadExternalPackages, externalListMembers, reload);
        }

        public virtual void ClearFilterTagsLocalProjects()
        {

        }
        public virtual void FilterLocalProjects(Swole.UI.FilterMode filterMode)
        {
            // TODO: Add filtering
        }

        public virtual void ClearFilterTagsExternalPackages()
        {

        }
        public virtual void FilterExternalPackages(Swole.UI.FilterMode filterMode)
        {
            // TODO: Add filtering
        }

        public const string _id_URL = "URL";
        public const string _id_projectName = "projectName";
        public const string _id_packageName = "packageName";
        public const string _id_version = "version";
        public const string _id_creator = "creator";
        public const string _id_tags = "tags";
        public const string _id_description = "description";

        public const string _defaultProjectName = "New Project";
        public const string _defaultPackageName = "my.package.name";
        public const string _defaultVersionString = "0.1.0";
        public void OpenNewProjectWindow(GameObject creatorWindow)
        {
            if (creatorWindow == null) return;

            Transform projUrl = creatorWindow.transform.FindDeepChildLiberal(_id_URL);
            Transform projName = creatorWindow.transform.FindDeepChildLiberal(_id_projectName);
            Transform pkgName = creatorWindow.transform.FindDeepChildLiberal(_id_packageName);
            Transform version = creatorWindow.transform.FindDeepChildLiberal(_id_version);
            Transform ctr = creatorWindow.transform.FindDeepChildLiberal(_id_creator);
            Transform tgs = creatorWindow.transform.FindDeepChildLiberal(_id_tags);
            Transform desc = creatorWindow.transform.FindDeepChildLiberal(_id_description);

            SetInputFieldText(projUrl, string.Empty);
            SetInputFieldText(projName, _defaultProjectName);
            SetInputFieldText(pkgName, _defaultPackageName);
            SetInputFieldText(version, _defaultVersionString);
            SetInputFieldText(ctr, string.Empty);
            SetInputFieldText(tgs, string.Empty);
            SetInputFieldText(desc, string.Empty);

            creatorWindow.SetActive(true);
        }

        public static bool ValidateProjectURL(string value, UIPopupMessageFadable errorMessage = null)
        {
            if (!string.IsNullOrEmpty(value) && !value.IsURL() && !value.IsEmailAddress())
            {
                errorMessage?.SetMessageAndShow("Invalid project URL.");
                return false;
            }
            return true;
        }

        public static bool ValidateNewProjectName(string value, UIPopupMessageFadable errorMessage = null)
        {
            if (ContentManager.CheckIfProjectExists(value))
            {
                if (errorMessage != null)
                {
                    errorMessage
                        .SetMessageAndShow($"A project with the name '{value}' already exists!")
                        .SetDisplayTime(errorMessage.DefaultDisplayTime);
                }
                return false;
            }
            return ValidateProjectName(value, errorMessage);
        }
        public static bool ValidateProjectName(string value, UIPopupMessageFadable errorMessage = null)
        {
            if (string.IsNullOrEmpty(value) || value.Length < 1)
            {
                errorMessage?.SetMessageAndShow("Project name cannot be empty.");
                return false;
            }
            if (!value.IsProjectName())
            {
                if (errorMessage != null)
                {
                    errorMessage
                        .SetMessageAndShow("Project name contains invalid characters. It must only contain letters and numbers and be separated with spaces, dashes, periods or underscores.")
                        .SetDisplayTime(errorMessage.DefaultDisplayTime * 2.5f);
                }
                return false;
            }
            return true;
        }

        public static bool ValidatePackageName(string value, UIPopupMessageFadable errorMessage = null)
        {
            if (string.IsNullOrEmpty(value) || value.Length < ContentManager.minCharCount_PackageName)
            {
                errorMessage?.SetMessageAndShow($"Package name must be at least {ContentManager.minCharCount_PackageName} characters in length.");
                return false;
            }
            if (!value.IsPackageName())
            {
                if (errorMessage != null)
                {
                    errorMessage
                        .SetMessageAndShow("Package name is invalid. It can only contain letters and numbers, and use periods as separators. It must start with a letter and end with a letter or number.")
                        .SetDisplayTime(errorMessage.DefaultDisplayTime * 2.5f);
                }
                return false;
            }
            return true;
        }
        public static bool ValidateNewPackageName(string packageName, string versionString, UIPopupMessageFadable errorMessage = null)
        {
            if (!ValidatePackageName(packageName, errorMessage)) return false;
            if (!ValidateVersionString(versionString, errorMessage)) return false;
            if (ContentManager.CheckIfLocalPackageExists(packageName, versionString))
            {
                string existingProject = ContentManager.GetProjectIdentifier(packageName);
                if (errorMessage != null)
                {
                    errorMessage
                        .SetMessageAndShow($"A local package with the name '{packageName}' and version '{versionString}' already exists!{(!string.IsNullOrEmpty(existingProject) && existingProject != packageName ? $" (Under Project '{existingProject}')" : "")}")
                        .SetDisplayTime(errorMessage.DefaultDisplayTime);
                }
                return false;
            }
            return true;
        }

        public static bool ValidateVersionString(string value, UIPopupMessageFadable errorMessage = null)
        {
            if (!value.IsNativeVersionString())
            {
                if (errorMessage != null)
                {
                    errorMessage
                        .SetMessageAndShow("Version field is invalid. It can only contain numbers and periods, and must start and end with a number. It can contain a maximum of 3 periods and a minimum of 1.")
                        .SetDisplayTime(errorMessage.DefaultDisplayTime * 2.5f);
                }
                return false;
            }
            return true;
        }

        public static bool ValidateAuthorName(string creator, UIPopupMessageFadable errorMessage = null) => ValidateCreatorName(creator, errorMessage, "Author"); 
        public static bool ValidateCreatorName(string creator, UIPopupMessageFadable errorMessage = null, string term = "Creator")
        {
            if (string.IsNullOrEmpty(creator))
            {
                errorMessage?.SetMessageAndShow($"{term} name cannot be empty.");
                return false;
            }
            if (creator.Length > ContentManager.maxCharCount_CuratorName)
            {
                errorMessage?.SetMessageAndShow($"{term} name exceeds {ContentManager.maxCharCount_CuratorName} character limit.");
                return false;
            }
            return true;
        }

        public static bool ValidateTags(string value, UIPopupMessageFadable errorMessage = null)
        {
            if (!string.IsNullOrEmpty(value) && !value.IsTagsString())
            {
                if (errorMessage != null)
                {
                    errorMessage
                        .SetMessageAndShow("Tags field contains invalid characters. Tags must only contain letters, numbers, dashes, underscores, and periods. Individual tags are separated with commas.")
                        .SetDisplayTime(errorMessage.DefaultDisplayTime * 2.5f);
                }
                return false;
            }
            return true;
        }

        public static bool ValidateDescription(string desc, UIPopupMessageFadable errorMessage = null)
        {
            if (!string.IsNullOrEmpty(desc) && desc.Length > ContentManager.maxCharCount_Description)
            {
                if (errorMessage != null)
                {
                    errorMessage
                        .SetMessageAndShow($"Description exceeds the {desc} characters limit.")
                        .SetDisplayTime(errorMessage.DefaultDisplayTime);
                }
                return false;
            }
            return true;
        }

        public static bool ValidateContentName(string contentName, UIPopupMessageFadable errorMessage = null)
        {
            if (string.IsNullOrEmpty(contentName))
            {
                errorMessage?.SetMessageAndShow("Content name cannot be empty.");
                return false;
            }
            if (contentName.Length > ContentManager.maxCharCount_ContentName)
            {
                errorMessage?.SetMessageAndShow($"Content name exceeds {ContentManager.maxCharCount_ContentName} character limit.");
                return false;
            }
            if (!SwoleUtil.IsContentName(contentName))
            {
                errorMessage?.SetMessageAndShow($"Content name contains invalid characters.");
                return false; 
            }
            return true;
        }

        public static List<string> SeparateTags(string tagString, List<string> tagsList = null)
        {
            if (tagsList == null) tagsList = new List<string>();

            string[] tags = tagString.IndexOf(',') >= 0 ? tagString.Split(',') : new string[] { tagString };
            if (tags != null)
            {
                for (int a = 0; a < Mathf.Min(tags.Length, ContentManager.maximum_PackageManifestTags); a++)
                {
                    tags[a] = tags[a].Trim().AsTagsString();
                    if (!string.IsNullOrEmpty(tags[a])) tagsList.Add(tags[a]);
                }
            }

            return tagsList;
        }

        public static bool CreateNewProjectFromWindow(GameObject creatorWindow, out PackageManifest manifest) => CreateNewProjectFromWindow(creatorWindow, new Version(0, 1, 0), out manifest);
        public static bool CreateNewProjectFromWindow(GameObject creatorWindow, Version defaultPackageVersion, out PackageManifest manifest)
        {
            manifest = default;
            if (creatorWindow == null) return false;

            UIPopupMessageFadable errorMessage = creatorWindow.GetComponentInChildren<UIPopupMessageFadable>(true);

            Transform projUrl = creatorWindow.transform.FindDeepChildLiberal(_id_URL);
            Transform projName = creatorWindow.transform.FindDeepChildLiberal(_id_projectName);
            Transform pkgName = creatorWindow.transform.FindDeepChildLiberal(_id_packageName);
            Transform version = creatorWindow.transform.FindDeepChildLiberal(_id_version);
            Transform ctr = creatorWindow.transform.FindDeepChildLiberal(_id_creator);
            if (ctr == null) ctr = creatorWindow.transform.FindDeepChildLiberal(PackageViewer._id_Author);
            Transform tgs = creatorWindow.transform.FindDeepChildLiberal(_id_tags);
            Transform desc = creatorWindow.transform.FindDeepChildLiberal(_id_description);
            
            string url = projUrl == null ? string.Empty : GetInputFieldText(projUrl).Trim();
            string packageName = GetInputFieldText(pkgName).Trim();
            string projectName = projName == null ? new PackageIdentifier(packageName).name : GetInputFieldText(projName).Trim(); 
            string versionString = version == null ? (string.IsNullOrWhiteSpace(new PackageIdentifier(packageName).version) ? defaultPackageVersion.ToString() : new PackageIdentifier(packageName).version) : GetInputFieldText(version).Trim();
            string creator = GetInputFieldText(ctr).Trim();
            string combinedTags = tgs == null ? string.Empty : GetInputFieldText(tgs).Trim();
            string description = desc == null ? string.Empty : GetInputFieldText(desc);

            if (!ValidateProjectURL(url, errorMessage)) return false;
            if (!ValidateNewProjectName(projectName, errorMessage)) return false;
            if (!ValidateNewPackageName(packageName, versionString, errorMessage)) return false;
            if (!ValidateVersionString(versionString, errorMessage)) return false;
            if (!ValidateCreatorName(creator, errorMessage)) return false;
            if (!ValidateTags(combinedTags, errorMessage)) return false;
            if (!ValidateDescription(description, errorMessage)) return false;

            // > Create the project

            string path = Path.Combine(ContentManager.LocalPackageDirectoryPath, projectName);
            /*if (Directory.Exists(path))
            {
                if (errorMessage != null)
                {
                    errorMessage
                        .SetMessageAndShow($"A project directory with the name '{projectName}' already exists in '{ContentManager.LocalPackageDirectoryPath}'")
                        .SetDisplayTime(errorMessage.DefaultDisplayTime * 2.5f);
                }
                return;
            }*/ // Just use the existing folder
            Directory.CreateDirectory(path);

            List<string> validTags = SeparateTags(combinedTags);

            manifest = new PackageManifest()
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

            ContentManager.SavePackage(Path.Combine(path, manifest.ToString()), new ContentPackage(manifest), swole.DefaultLogger);

            ContentManager.SetProjectIdentifier(packageName, projectName, ContentManager.SaveMethod.Immediate);

            // <

            creatorWindow.SetActive(false);

            return true;
        }
        public void CreateNewProject(GameObject creatorWindow)
        {
            CreateNewProjectFromWindow(creatorWindow, out PackageManifest manifest);

            IEnumerator Reload()
            {

                yield return null;

                RefreshLocalProjects(true);
                ClearFilterTagsLocalProjects();

                yield return null;

                FilterLocalProjects(UI.FilterMode.Newest);

                yield return null;

                if (tabGroup != null && localListMembers != null)
                {
                    foreach (var member in localListMembers)
                    {
                        if (member != null && member.name == manifest.Name)
                        {
                            var button = member.GetComponentInChildren<UITabButton>(); 
                            if (button != null) tabGroup.ToggleButtons(button);
                            if (packageViewer != null) packageViewer.SetActiveLocalPackage(manifest.Name, manifest.VersionString);
                            break;
                        }
                    }
                }

            }

            StartCoroutine(Reload());
        }
    }
}

#endif