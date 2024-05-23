using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Swole.Script;

#if BULKOUT_ENV
using Swole.API.Unity;
using Swole.API.Unity.Animation;
#endif

namespace Swole
{

    [Serializable]
    public enum PackageType
    {
        Both, Local, External
    }

    public interface IPackage
    {
        public ContentPackage Content { get; }
    }

    public class ContentManager : SingletonBehaviour<ContentManager>
    {

        // >> SEARCH FOR KEYWORD "ContentTypes" TO FIND SECTIONS OF CODE THAT MUST BE REFACTORED WHEN NEW CONTENT TYPES ARE INTRODUCED
        // >> DO THE SAME IN PackageViewer.cs

        // disable execution
        public override bool ExecuteInStack => false;
        public override void OnFixedUpdate() { }
        public override void OnLateUpdate() { }
        public override void OnUpdate() { }
        //

        public override bool DestroyOnLoad => false;

        private static readonly List<LocalPackage> _tempLocalPackageList = new List<LocalPackage>();
        private static readonly List<ExternalPackage> _tempExternalPackageList = new List<ExternalPackage>();
        private static readonly List<ContentPackage> _tempPackageList = new List<ContentPackage>();

        public const int maximum_PackageManifestTags = 16;
        public const int minCharCount_PackageName = 3;
        public const int maxCharCount_CuratorName = 16;
        public const int maxCharCount_ContentName = 32;
        public const int maxCharCount_Description = 1024;

        public const string folderNames_Packages = "packages";
        public const string folderNames_LocalPackages = "local";
        public const string folderNames_CachedPackages = "cache";

        public const string folderNames_Editors = "editors";
        public const string folderNames_AnimationEditor = "animation";
        public const string folderNames_Sessions = "sessions";

        public const string tags_Temporary = ".TEMP";

        public const string commonFiles_Manifest = "manifest.json";
        public const string commonFiles_Projects = "projects.json";

        public const string fileExtension_Generic = "swldat";
        public const string fileExtension_ZIP = "zip";
        public const string fileExtension_JSON = "json";
        public const string fileExtension_Default = fileExtension_JSON;
        public const string fileExtension_Package = "swole";
        public const string fileExtension_Script = "swlscr";
        public const string fileExtension_Creation = "swlobj";
        public const string fileExtension_Animation = "swlani";
        public const string fileExtension_GameplayExperience = "swlexp";
        public const string fileExtension_Image = "swlimg"; 

        public static bool HasValidFileExtension(string fileName)
        {

            if (string.IsNullOrEmpty(fileName)) return false;

            if (fileName.EndsWith(fileExtension_ZIP) || fileName.EndsWith(fileExtension_Package)) return true;

            #region ContentTypes
            if (fileName.EndsWith(fileExtension_Script)) return true;
            if (fileName.EndsWith(fileExtension_Creation)) return true;
            if (fileName.EndsWith(fileExtension_Animation)) return true;
            if (fileName.EndsWith(fileExtension_GameplayExperience)) return true;
            if (fileName.EndsWith(fileExtension_Image)) return true;
            #endregion

            return false;

        }

        public static IContent LoadContent(PackageInfo packageInfo, FileInfo file, SwoleLogger logger = null) => LoadContent(packageInfo, file.FullName, logger);
        public static IContent LoadContent(PackageInfo packageInfo, string path, SwoleLogger logger = null) => LoadContentInternal(true, packageInfo, path, logger).GetAwaiter().GetResult();
        public static Task<IContent> LoadContentAsync(PackageInfo packageInfo, FileInfo file, SwoleLogger logger = null) => LoadContentAsync(packageInfo, file.FullName, logger); 
        public static Task<IContent> LoadContentAsync(PackageInfo packageInfo, string path, SwoleLogger logger = null) => LoadContentInternal(false, packageInfo, path, logger);
        async private static Task<IContent> LoadContentInternal(bool sync, PackageInfo packageInfo, string path, SwoleLogger logger = null)
        {
            if (string.IsNullOrEmpty(path)) return default;

            IContent content = default;
            if (path.EndsWith(fileExtension_Package) || path.EndsWith(fileExtension_ZIP)) // File is probably an embedded package, so try to load it recursively.
            {
                ExternalPackage embeddedPackage;
                if (sync)
                {
                    embeddedPackage = LoadPackageFromRaw(string.Empty, path, File.ReadAllBytes(path), null, null, logger, !path.EndsWith(fileExtension_ZIP));
                }
                else
                {
                    embeddedPackage = await LoadPackageFromRawAsync(string.Empty, path, await File.ReadAllBytesAsync(path), null, null, logger, !path.EndsWith(fileExtension_ZIP));
                }
                if (embeddedPackage.content != null)
                {
                    logger?.Log($"Loaded embedded package '{embeddedPackage.content}' from '{embeddedPackage.cachedPath}'");
                }
            }
            #region elseif { ContentTypes }
            else if (path.EndsWith(fileExtension_Script))
            {
                byte[] data = sync ? File.ReadAllBytes(path) : await File.ReadAllBytesAsync(path);
                content = LoadContent<SourceScript>(packageInfo, data, logger);
            }
            else if (path.EndsWith(fileExtension_Creation))
            {
                byte[] data = sync ? File.ReadAllBytes(path) : await File.ReadAllBytesAsync(path);
                content = LoadContent<Creation>(packageInfo, data, logger);
            }
            else if (path.EndsWith(fileExtension_GameplayExperience))
            {
                byte[] data = sync ? File.ReadAllBytes(path) : await File.ReadAllBytesAsync(path);
                content = LoadContent<GameplayExperience>(packageInfo, data, logger);
            }
#if BULKOUT_ENV
            else if (path.EndsWith(fileExtension_Animation))
            {
                byte[] data = sync ? File.ReadAllBytes(path) : await File.ReadAllBytesAsync(path);
                content = LoadContent<CustomAnimation>(packageInfo, data, logger);
            }
            else if (path.EndsWith(fileExtension_Image))
            {
                byte[] data = sync ? File.ReadAllBytes(path) : await File.ReadAllBytesAsync(path);
                content = LoadContent<ImageAsset>(packageInfo, data, logger);
            }
#endif
            #endregion

            if (content != null) ContentExtensions.SetOriginPathAndUpdateRelativePath(ref content, path); 

            return content;
        }
        public static T LoadContent<T>(PackageInfo packageInfo, byte[] rawData, SwoleLogger logger = null) where T : IContent
        {
            TryLoadType<T>(out T output, packageInfo, rawData, logger);
            return output;
        }
        public static bool TryLoadType<T>(out T output, PackageInfo packageInfo, byte[] rawData, SwoleLogger logger = null)
        {
            output = default;
            if (rawData == null) return false;

            Type t = typeof(T);

            try
            {

                string json;
                var implementedInterfaces = t.GetInterfaces();
                foreach (var interfaceType in implementedInterfaces)
                {
                    if (!interfaceType.IsGenericType) continue;
                    var genericType = interfaceType.GetGenericTypeDefinition();
                    if (genericType == typeof(ISwoleSerialization<,>))
                    {
                        json = DefaultJsonSerializer.StringEncoder.GetString(rawData);
                        object obj = swole.Engine.FromJson(json, interfaceType.GetGenericArguments()[1]);
                        if (obj == null) return false;
                        ISerializableContainer container = (ISerializableContainer)obj;
                        obj = container.AsNonserializableObject(packageInfo);
                        if (obj == null) return false;
                        output = (T)obj;
                        return true; 
                    }
                }

                // Fallback attempt
                json = DefaultJsonSerializer.StringEncoder.GetString(rawData);
                if (string.IsNullOrEmpty(json)) return default;

                output = swole.Engine.FromJson<T>(json);
                return true;
                //

            }
            catch (Exception ex)
            {

                logger?.LogError($"Error while attempting to load data{(packageInfo.NameIsValid ? $" from package '{packageInfo.name}'" : "")} with type '{typeof(T).FullName}'");
                logger?.LogError(ex);

            }

            return false;
        }
         
        public static bool SaveContent(DirectoryInfo dir, IContent content, SwoleLogger logger = null) => SaveContent(dir.FullName, content, logger);
        public static bool SaveContent(string directoryPath, IContent content, SwoleLogger logger = null) => SaveContentInternal(true, directoryPath, content, logger).GetAwaiter().GetResult();
        public static Task<bool> SaveContentAsync(DirectoryInfo dir, IContent content, SwoleLogger logger = null) => SaveContentAsync(dir.FullName, content, logger);
        public static Task<bool> SaveContentAsync(string directoryPath, IContent content, SwoleLogger logger = null) => SaveContentInternal(false, directoryPath, content, logger);
        async private static Task<bool> SaveContentInternal(bool sync, string directoryPath, IContent content, SwoleLogger logger = null)
        {

            if (content == null) 
            {
                logger?.LogWarning($"Tried to save null content object to '{directoryPath}'");
                return false; 
            }

            try
            {

                string fullPath = "";
                byte[] bytes = null;
                #region ContentTypes
                if (content is SourceScript script)
                {
                    string json = script.AsJSON(true);
                    bytes = DefaultJsonSerializer.StringEncoder.GetBytes(json);
                    fullPath = Path.Combine(directoryPath, $"{script.Name}.{fileExtension_Script}");
                }
                else if (content is Creation creation)
                {

                    string json = creation.AsJSON(true);
                    bytes = DefaultJsonSerializer.StringEncoder.GetBytes(json);
                    fullPath = Path.Combine(directoryPath, $"{creation.Name}.{fileExtension_Creation}");

                }
                else if (content is GameplayExperience exp)
                {

                    string json = exp.AsJSON(true);
                    bytes = DefaultJsonSerializer.StringEncoder.GetBytes(json);
                    fullPath = Path.Combine(directoryPath, $"{exp.Name}.{fileExtension_GameplayExperience}");

                }
                else if (content is IImageAsset img)
                {

                    string json = img.AsJSON(true);
                    bytes = DefaultJsonSerializer.StringEncoder.GetBytes(json); 
                    fullPath = Path.Combine(directoryPath, $"{img.Name}.{fileExtension_Image}");

                }
#if BULKOUT_ENV
                else if (content is CustomAnimation anim)
                {

                    string json = anim.AsJSON(true);
                    bytes = DefaultJsonSerializer.StringEncoder.GetBytes(json);
                    fullPath = Path.Combine(directoryPath, $"{anim.Name}.{fileExtension_Animation}");

                }
#endif
                #endregion

                if (string.IsNullOrEmpty(fullPath)) fullPath = Path.Combine(directoryPath, $"{content.Name}.{fileExtension_Default}");

                if (bytes == null) 
                { 
                    string json = swole.Engine.ToJson(content, true);
                    bytes = DefaultJsonSerializer.StringEncoder.GetBytes(json);
                }

                if (sync) File.WriteAllBytes(fullPath, bytes); else await File.WriteAllBytesAsync(fullPath, bytes);
                return true;

            } 
            catch (Exception ex)
            {

                logger?.LogError($"Error while attempting to save content '{content.Name}' of type '{content.GetType().FullName}' to '{directoryPath}'");
                logger?.LogError($"[{ex.GetType().Name}]: {ex.Message}");

            }
            return false;
        }
        public delegate void PackageEditContentDelegate(ContentPackage oldContent, ContentPackage newContent);
        [Serializable]
        public class LocalPackage : IPackage
        {
            public DirectoryInfo workingDirectory;
            protected ContentPackage content;

            public ContentPackage Content
            {
                get => content;
                set 
                {
                    try
                    {
                        OnEditContents?.Invoke(content, value);
                    }
                    catch(Exception ex)
                    {
                        swole.LogError($"Encountered exception while notifying listeners about the content in package '{GetIdentityString()}' being edited");
                        swole.LogError(ex);
                    }
                    content = value;
                }
            }

            public event PackageEditContentDelegate OnEditContents;
            public void ClearListeners()
            {
                OnEditContents = null;
            }

            public static implicit operator ContentPackage(LocalPackage pkg) => pkg.content; 

            public PackageIdentifier GetIdentity() => content == null ? default : content.GetIdentity();
            public string GetIdentityString() => content == null ? default : content.GetIdentityString();
        }

        public static bool DirectoryIsPackage(DirectoryInfo directory) => DirectoryIsPackage(directory == null ? string.Empty : directory.FullName);
        public static bool DirectoryIsPackage(string directory)
        {
            if (string.IsNullOrEmpty(directory)) return false;
            return File.Exists(Path.Combine(directory, commonFiles_Manifest));
        }

        public static void LoadContent(DirectoryInfo dir, PackageManifest manifest, List<IContent> content, SwoleLogger logger = null) => LoadContentInternal(true, dir, manifest, content, logger).GetAwaiter().GetResult();
        public static Task LoadContentAsync(DirectoryInfo dir, PackageManifest manifest, List<IContent> content, SwoleLogger logger = null) => LoadContentInternal(false, dir, manifest, content, logger);
        async private static Task LoadContentInternal(bool sync, DirectoryInfo dir, PackageManifest manifest, List<IContent> content, SwoleLogger logger = null)
        {
            var files = dir.EnumerateFiles("*", SearchOption.TopDirectoryOnly).Where(i => HasValidFileExtension(i.Name));
            foreach (var file in files)
            {
                var contentObj = sync ? LoadContent(manifest, file, logger) : await LoadContentAsync(manifest, file, logger);
                if (contentObj == null) continue;
                content.Add(contentObj);
            }
            var dirs = dir.EnumerateDirectories("*", SearchOption.TopDirectoryOnly);
            foreach (var subDir in dirs)
            {
                if (DirectoryIsPackage(subDir)) continue; // Directory is its own package so ignore it
                if (sync) LoadContent(subDir, manifest, content, logger); else await LoadContentAsync(subDir, manifest, content, logger);
            }
        }

        /// <summary>
        /// Loads a package locally by name and version.
        /// </summary>
        public static LocalPackage LoadLocalPackage(PackageIdentifier identity, SwoleLogger logger = null) => LoadLocalPackage(identity.name, identity.version, logger);
        /// <summary>
        /// Asynchronously loads a package locally by name and version.
        /// </summary>
        public static Task<LocalPackage> LoadLocalPackageAsync(PackageIdentifier identity, SwoleLogger logger = null) => LoadLocalPackageAsync(identity.name, identity.version, logger);
        /// <summary>
        /// Loads a package locally by name and version.
        /// </summary>
        public static LocalPackage LoadLocalPackage(string packageName, string version, SwoleLogger logger = null) => LoadPackage(Path.Combine(LocalPackageDirectoryPath, SwoleScriptSemantics.GetFullPackageString(packageName, version)), logger);
        /// <summary>
        /// Asynchronously loads a package locally by name and version.
        /// </summary>
        public static Task<LocalPackage> LoadLocalPackageAsync(string packageName, string version, SwoleLogger logger = null) => LoadPackageAsync(Path.Combine(LocalPackageDirectoryPath, SwoleScriptSemantics.GetFullPackageString(packageName, version)), logger);
        /// <summary>
        /// Loads a package locally from a folder.
        /// </summary>
        public static LocalPackage LoadPackage(string directoryPath, SwoleLogger logger = null) => LoadPackage(new DirectoryInfo(directoryPath), logger);
        /// <summary>
        /// Asynchronously loads a package locally from a folder.
        /// </summary>
        public static Task<LocalPackage> LoadPackageAsync(string directoryPath, SwoleLogger logger = null) => LoadPackageAsync(new DirectoryInfo(directoryPath), logger);
        /// <summary>
        /// Loads a package locally from a folder.
        /// </summary>
        public static LocalPackage LoadPackage(DirectoryInfo packageDirectory, SwoleLogger logger = null) => LoadPackageInternal(true, packageDirectory, logger).GetAwaiter().GetResult();
        /// <summary>
        /// Asynchronously loads a package locally from a folder.
        /// </summary>
        public static Task<LocalPackage> LoadPackageAsync(DirectoryInfo packageDirectory, SwoleLogger logger = null) => LoadPackageInternal(false, packageDirectory, logger);
        async private static Task<LocalPackage> LoadPackageInternal(bool sync, DirectoryInfo packageDirectory, SwoleLogger logger = null)
        {
            if (packageDirectory == null) return default;
            if (!packageDirectory.Exists)
            {
                logger?.LogError($"Tried to load local package from '{packageDirectory.FullName}' — but the directory does not exist!");
                return default;
            }
            var instance = Instance;
            if (instance == null) return default;
            for (int a = 0; a < instance.localPackages.Count; a++) if (instance.localPackages[a].workingDirectory.FullName == packageDirectory.FullName) 
                {
                    logger?.LogWarning($"Tried to load local package from '{packageDirectory.FullName}' — but it was already loaded."); 
                    return default;// instance.localPackages[a]; 
                }

            LocalPackage package = new LocalPackage();
            package.workingDirectory = packageDirectory;

            PackageManifest manifest = default;
            var manifestFile = packageDirectory.EnumerateFiles("*", SearchOption.TopDirectoryOnly).Where(i => i.Name == commonFiles_Manifest).FirstOrDefault();
            if (manifestFile != null)
            {
                byte[] raw = sync ? File.ReadAllBytes(manifestFile.FullName) : await File.ReadAllBytesAsync(manifestFile.FullName);
                manifest = PackageManifest.FromRaw(raw); 
            } 
            else
            {
                logger?.LogError($"Error loading package from '{packageDirectory.FullName}' — Reason: No package manifest was found!");
                return default;
            }

            if (!manifest.NameIsValid)
            {
                logger?.LogError($"Error loading package from '{packageDirectory.FullName}' — Reason: Name in package manifest was invalid!");
                return default;
            }

            // Load valid files and ignore the rest
            List<IContent> content = new List<IContent>();
            if (sync) LoadContent(packageDirectory, manifest, content, logger); else await LoadContentAsync(packageDirectory, manifest, content, logger);
            package.Content = new ContentPackage(manifest, content);
            //

            AddLocalPackage(package);

            if (!HasProjectIdentifier(manifest))
            {
                if (packageDirectory.IsSubDirectoryOf(LocalPackageDirectoryPath) && !packageDirectory.Parent.FullName.IsIdenticalPath(LocalPackageDirectoryPath)) // If the package is in a project folder, set the project identifier to the name of that folder.
                {
                    SetProjectIdentifier(manifest, packageDirectory.Parent.Name, SaveMethod.Immediate);
                }
                else if (packageDirectory.Name != manifest.GetIdentityString())
                {
                    int end = packageDirectory.Name.IndexOf(SwoleScriptSemantics.ssVersionPrefix); // If the name somehow ends up being a package identifier, at least remove the version from it.
                    if (end < 0) end = packageDirectory.Name.Length;
                    SetProjectIdentifier(manifest, packageDirectory.Name.Substring(0, end), SaveMethod.Immediate);
                }
            }
            return package;
        } 

        /// <summary>
        /// Loads a package externally from a zip archive.
        /// </summary>
        public static ExternalPackage LoadPackage(string sourcePath, string cachedPath = null, SwoleLogger logger = null) => LoadPackageInternal(true, sourcePath, cachedPath, logger).GetAwaiter().GetResult();
        /// <summary>
        /// Asynchronously loads a package externally from a zip archive.
        /// </summary>
        public static Task<ExternalPackage> LoadPackageAsync(string sourcePath, string cachedPath = null, SwoleLogger logger = null) => LoadPackageInternal(false, sourcePath, cachedPath, logger);
        async private static Task<ExternalPackage> LoadPackageInternal(bool sync, string sourcePath, string cachedPath = null, SwoleLogger logger = null)
        {
            if (string.IsNullOrEmpty(sourcePath) && string.IsNullOrEmpty(cachedPath)) return default;
            var instance = Instance;
            if (instance == null) return default;

            List<EngineHook.FileDescr> fileDescs = null;
            List<byte[]> fileData = null;
            if (string.IsNullOrEmpty(cachedPath))
            {
                cachedPath = Path.Combine(swole.AssetDirectory.FullName, folderNames_Packages, folderNames_CachedPackages, Path.GetFileName(sourcePath));
                if (File.Exists(cachedPath))
                { 
                    if (File.Exists(sourcePath))
                    {
                        File.Delete(cachedPath);
                        if (sync) File.Copy(sourcePath, cachedPath); else await SwoleUtil.CopyFileAsync(sourcePath, cachedPath);
                    } 
                } 
                else if (File.Exists(sourcePath))
                {
                    if (sync) File.Copy(sourcePath, cachedPath); else await SwoleUtil.CopyFileAsync(sourcePath, cachedPath);
                } 
                else
                {
                    logger?.LogError($"Failed to copy external package file from '{sourcePath}' into cache — Reason: The file does not exist!");
                    return default;
                }
            }
            if (sync) 
            { 
                swole.Engine.DecompressZIP(cachedPath, ref fileDescs, ref fileData); 
            }
            else
            {
                var result = await swole.Engine.DecompressZIPAsync(cachedPath);
                fileDescs = result.fileDescs;
                fileData = result.fileData;
            }

            return sync ? LoadPackage(sourcePath, cachedPath, fileDescs, fileData, logger) : await LoadPackageAsync(sourcePath, cachedPath, fileDescs, fileData, logger);
        }
        /// <summary>
        /// Loads a package externally from data in memory.
        /// </summary>
        public static ExternalPackage LoadPackage(string sourcePath, string cachedPath, ICollection<EngineHook.FileDescr> fileDescs, ICollection<byte[]> fileData, SwoleLogger logger = null, bool packageExpected = true) => LoadPackageInternal(true, sourcePath, cachedPath, fileDescs, fileData, logger, packageExpected).GetAwaiter().GetResult();
        /// <summary>
        /// Asynchronously loads a package externally from data in memory.
        /// </summary>
        public static Task<ExternalPackage> LoadPackageAsync(string sourcePath, string cachedPath, ICollection<EngineHook.FileDescr> fileDescs, ICollection<byte[]> fileData, SwoleLogger logger = null, bool packageExpected = true) => LoadPackageInternal(false, sourcePath, cachedPath, fileDescs, fileData, logger, packageExpected);
        async private static Task<ExternalPackage> LoadPackageInternal(bool sync, string sourcePath, string cachedPath, ICollection<EngineHook.FileDescr> fileDescs, ICollection<byte[]> fileData, SwoleLogger logger = null, bool packageExpected = true)
        {
            if (fileDescs == null || fileData == null) return default;
            Type[] fileTypes = new Type[fileDescs.Count];
            string[] filePaths = new string[fileDescs.Count];
            byte[] manifestFile = null;

            int i = 0;
            using (var enuDescs = fileDescs.GetEnumerator())
            using (var enuData = fileData.GetEnumerator())
            {
                while (enuDescs.MoveNext() && enuData.MoveNext())
                {
                    i++;
                    var fileDesc = enuDescs.Current;
                    
                    if (!string.IsNullOrEmpty(fileDesc.fileName))
                    {
                        int index = i - 1;
                        filePaths[index] = fileDesc.fileName;

                        if (Path.GetFileName(fileDesc.fileName).AsID() == commonFiles_Manifest.AsID())
                        {
                            manifestFile = enuData.Current;
                        } 
                        else if (fileDesc.fileName.EndsWith(fileExtension_ZIP) || fileDesc.fileName.EndsWith(fileExtension_Package))  // File is likely an embedded package
                        {
                            fileTypes[index] = typeof(ExternalPackage);
                        }
                        #region elseif { ContentTypes }
                        else if (fileDesc.fileName.EndsWith(fileExtension_Script))
                        {
                            fileTypes[index] = typeof(SourceScript);
                        }
                        else if (fileDesc.fileName.EndsWith(fileExtension_Creation))
                        {
                            fileTypes[index] = typeof(Creation);
                        }

                        else if (fileDesc.fileName.EndsWith(fileExtension_GameplayExperience))
                        {
                            fileTypes[index] = typeof(GameplayExperience);
                        }
#if BULKOUT_ENV
                        else if (fileDesc.fileName.EndsWith(fileExtension_Animation))
                        {
                            fileTypes[index] = typeof(CustomAnimation);
                        }
                        else if (fileDesc.fileName.EndsWith(fileExtension_Image))
                        {
                            fileTypes[index] = typeof(ImageAsset);
                        }
#endif
#endregion
                    }
                }
            }

            return sync ? LoadPackage(sourcePath, cachedPath, manifestFile, fileData, fileTypes, logger, packageExpected, string.Empty, filePaths) : await LoadPackageAsync(sourcePath, cachedPath, manifestFile, fileData, fileTypes, logger, packageExpected, string.Empty, filePaths);
        }

        /// <summary>
        /// Loads a package externally from data in memory.
        /// </summary>
        public static ExternalPackage LoadPackageFromRaw(string sourcePath, string cachedPath, byte[] packageFileRaw, List<EngineHook.FileDescr> outFileDescs = null, List<byte[]> outFileData = null, SwoleLogger logger = null, bool packageExpected = true) => LoadPackageFromRawInternal(true, sourcePath, cachedPath, packageFileRaw, outFileDescs, outFileData, logger, packageExpected).GetAwaiter().GetResult();
        /// <summary>
        /// Asynchronously loads a package externally from data in memory.
        /// </summary>
        public static Task<ExternalPackage> LoadPackageFromRawAsync(string sourcePath, string cachedPath, byte[] packageFileRaw, List<EngineHook.FileDescr> outFileDescs = null, List<byte[]> outFileData = null, SwoleLogger logger = null, bool packageExpected = true) => LoadPackageFromRawInternal(false, sourcePath, cachedPath, packageFileRaw, outFileDescs, outFileData, logger, packageExpected);
        async private static Task<ExternalPackage> LoadPackageFromRawInternal(bool sync, string sourcePath, string cachedPath, byte[] packageFileRaw, List<EngineHook.FileDescr> outFileDescs = null, List<byte[]> outFileData = null, SwoleLogger logger = null, bool packageExpected = true)
        {
            if (packageFileRaw == null) return default;

            if (outFileDescs == null) outFileDescs = new List<EngineHook.FileDescr>();
            if (outFileData == null) outFileData = new List<byte[]>();
            outFileDescs.Clear();
            outFileData.Clear();

            var tempFileDescs = outFileDescs;
            var tempFileData = outFileData;
            if (sync)
            {
                swole.Engine.DecompressZIP(packageFileRaw, ref tempFileDescs, ref tempFileData);
                return LoadPackage(sourcePath, cachedPath, tempFileDescs, tempFileData, logger, packageExpected);
            }

            void Decompress() => swole.Engine.DecompressZIP(packageFileRaw, ref tempFileDescs, ref tempFileData);
            await Task.Run(Decompress);
            return await LoadPackageAsync(sourcePath, cachedPath, tempFileDescs, tempFileData, logger, packageExpected);
        }

        /// <summary>
        /// Loads a package externally from data in memory.
        /// </summary>
        public static ExternalPackage LoadPackage(string sourcePath, string cachedPath, byte[] manifestFile, ICollection<byte[]> files, ICollection<Type> fileTypes, SwoleLogger logger = null, bool packageExpected = true, string rootFolderName = null, ICollection<string> filePaths = null) => LoadPackageInternal(true, sourcePath, cachedPath, manifestFile, files, fileTypes, logger, packageExpected, rootFolderName, filePaths).GetAwaiter().GetResult();
        /// <summary>
        /// Asynchronously loads a package externally from data in memory.
        /// </summary>
        public static Task<ExternalPackage> LoadPackageAsync(string sourcePath, string cachedPath, byte[] manifestFile, ICollection<byte[]> files, ICollection<Type> fileTypes, SwoleLogger logger = null, bool packageExpected = true, string rootFolderName = null, ICollection<string> filePaths = null) => LoadPackageInternal(false, sourcePath, cachedPath, manifestFile, files, fileTypes, logger, packageExpected, rootFolderName, filePaths);
        async private static Task<ExternalPackage> LoadPackageInternal(bool sync, string sourcePath, string cachedPath, byte[] manifestFile, ICollection<byte[]> files, ICollection<Type> fileTypes, SwoleLogger logger = null, bool packageExpected = true, string rootFolderName = null, ICollection<string> filePaths = null)
        {
            if (files == null || fileTypes == null) return default;
            var instance = Instance;
            if (instance == null) return default;

            ExternalPackage package = new ExternalPackage();
            package.cachedPath = cachedPath;

            PackageManifest manifest = default;
            if (manifestFile != null)
            {
                manifest = PackageManifest.FromRaw(manifestFile);
            }
            else
            {
                if (packageExpected) logger?.LogError($"Error loading external package from '{(string.IsNullOrEmpty(cachedPath) ? sourcePath : cachedPath)}' — Reason: No package manifest was provided!");
                return default;
            }
             
            if (!manifest.NameIsValid)
            {
                logger?.LogError($"Error loading external package from '{(string.IsNullOrEmpty(cachedPath) ? sourcePath : cachedPath)}' — Reason: Name in package manifest was invalid!");
                return default;
            }

            for (int a = 0; a < instance.externalPackages.Count; a++) if (instance.externalPackages[a].content != null && instance.externalPackages[a].content.GetIdentityString() == manifest.GetIdentityString())
                {
                    logger?.LogWarning($"Tried to load external package '{manifest}' from '{(string.IsNullOrEmpty(cachedPath) ? sourcePath : cachedPath)}' — but it was already loaded.");
                    return default;// instance.externalPackages[a];
                }

            if (string.IsNullOrEmpty(sourcePath))
            {
                sourcePath = manifest.URL;
            }
            else
            {
                var info = manifest.info;
                info.url = sourcePath;
                manifest.info = info;
            }
            package.sourcePath = sourcePath;

            List<EngineHook.FileDescr> embeddedPackageFileDescs = null;
            List<byte[]> embeddedPackageFileData = null;
            // > Load valid files and ignore the rest
            List<IContent> content = new List<IContent>();
            using (var enuFiles = files.GetEnumerator())
            using (var enuTypes = fileTypes.GetEnumerator())
            using (var enuPaths = (filePaths == null ? Enumerable.Empty<string>().GetEnumerator() : filePaths.GetEnumerator()))
            {
                while (enuFiles.MoveNext() && enuTypes.MoveNext())
                {
                    enuPaths.MoveNext();
                    var filePath = enuPaths.Current;

                    var fileType = enuTypes.Current;
                    var fileData = enuFiles.Current;
                    if (fileType == null || fileData == null) continue;

                    IContent contentObj = null;
                    if (fileType == typeof(ExternalPackage)) // File is likely an embedded package, so try to load it recursively.
                    {
                        ExternalPackage embeddedPackage;
                        if (sync)
                        {
                            embeddedPackage = LoadPackageFromRaw(string.Empty, $"{cachedPath}@embedded", fileData, embeddedPackageFileDescs, embeddedPackageFileData, logger, false);
                        }
                        else
                        {
                            embeddedPackage = await LoadPackageFromRawAsync(string.Empty, $"{cachedPath}@embedded", fileData, embeddedPackageFileDescs, embeddedPackageFileData, logger, false);
                        }
                        if (embeddedPackage.content != null)
                        {
                            logger?.Log($"Loaded embedded package '{embeddedPackage.content}' from '{embeddedPackage.cachedPath}'");
                        }
                    }
                    #region elseif { ContentTypes }
                    else if (fileType == typeof(SourceScript))
                    {
                        contentObj = LoadContent<SourceScript>(manifest, fileData, logger);
                    }
                    else if (fileType == typeof(Creation))
                    {
                        contentObj = LoadContent<Creation>(manifest, fileData, logger);
                    }
                    else if (fileType == typeof(GameplayExperience))
                    {
                        contentObj = LoadContent<GameplayExperience>(manifest, fileData, logger);
                    }
#if BULKOUT_ENV
                    else if (fileType == typeof(CustomAnimation))
                    {
                        contentObj = LoadContent<CustomAnimation>(manifest, fileData, logger);
                    }
                    else if (fileType == typeof(ImageAsset))
                    {
                        contentObj = LoadContent<ImageAsset>(manifest, fileData, logger); 
                    }
#endif
                    #endregion

                    if (contentObj == null) continue;

                    if (!string.IsNullOrEmpty(filePath)) ContentExtensions.SetRelativePath(ref contentObj, ContentExtensions.GetRelativePathFromOriginPath(rootFolderName, filePath).NormalizeDirectorySeparators());
                    contentObj = contentObj.SetOriginPath($"{cachedPath}@{Path.DirectorySeparatorChar}{filePath}".NormalizeDirectorySeparators());

                    content.Add(contentObj);
                }
            }

            package.content = new ContentPackage(manifest, content);
            // <

            AddExternalPackage(package);

            return package;
        }

        public static bool SavePackage(LocalPackage package, SwoleLogger logger = null) => SavePackage(package.workingDirectory, package.Content, logger);
        public static bool SavePackage(DirectoryInfo dir, ContentPackage package, SwoleLogger logger = null) => SavePackage(dir == null ? "" : dir.FullName, package, logger);
        public static bool SavePackage(ContentPackage package, SwoleLogger logger = null) => SavePackage(Path.Combine(LocalPackageDirectoryPath, package == null ? "null" : package.GetIdentityString()), package, logger);
        public static bool SavePackage(string path, ContentPackage package, SwoleLogger logger = null) => SavePackageInternal(true, path, package, logger).GetAwaiter().GetResult();

        public static Task<bool> SavePackageAsync(LocalPackage package, SwoleLogger logger = null) => SavePackageAsync(package.workingDirectory, package.Content, logger);
        public static Task<bool> SavePackageAsync(DirectoryInfo dir, ContentPackage package, SwoleLogger logger = null) => SavePackageAsync(dir == null ? "" : dir.FullName, package, logger);
        public static Task<bool> SavePackageAsync(ContentPackage package, SwoleLogger logger = null) => SavePackageAsync(Path.Combine(LocalPackageDirectoryPath, package == null ? "null" : package.GetIdentityString()), package, logger);
        public static Task<bool> SavePackageAsync(string path, ContentPackage package, SwoleLogger logger = null) => SavePackageInternal(false, path, package, logger);

        async private static Task<bool> SavePackageInternal(bool sync, string path, ContentPackage package, SwoleLogger logger = null)
        {
            if (package == null || string.IsNullOrEmpty(path)) return false;

            bool exists = Directory.Exists(path);
            string tempPath = path;
            if (exists)
            {
                tempPath = Path.Combine(Directory.GetParent(path).FullName, $"{tags_Temporary}.{new DirectoryInfo(path).Name}"); // Use a temporary directory so we don't overwrite anything if something goes wrong.
                if (Directory.Exists(tempPath)) Directory.Delete(tempPath, true);
            }

            int contentCount = package.ContentCount;
            int filesSaved = 0;
            int expectedFilesSaved = contentCount + 1;
            bool completedTransfer = !exists;

            try
            {
                var dir = Directory.CreateDirectory(tempPath);

                // Save package manifest
                string manifestPath = Path.Combine(dir.FullName, commonFiles_Manifest);
                byte[] manifestBytes = DefaultJsonSerializer.StringEncoder.GetBytes(swole.Engine.ToJson(package.Manifest, true));
                if (sync)
                { 
                    File.WriteAllBytes(manifestPath, manifestBytes);
                    filesSaved++;
                }  
                else
                {
                    await File.WriteAllBytesAsync(manifestPath, manifestBytes);
                    filesSaved++;
                }
                //

                for (int a = 0; a < contentCount; a++)
                {

                    var content = package[a];

                    try
                    {

                        bool result = sync ? SaveContent(dir, content, logger) : await SaveContentAsync(dir, content, logger);
                        if (result) filesSaved++; else if (content != null) logger?.LogWarning($"Failed to save content object '{content.Name}' of type '{content.GetType().FullName}' in package '{package.GetIdentityString()}'.");

                    }
                    catch (Exception ex)
                    {
                        if (content != null) logger?.LogError($"Error while attempting to save content '{content.Name}' in package '{package.GetIdentityString()}'");
                        logger?.LogError($"[{ex.GetType().Name}]: {ex.Message}");
                    }

                }
                 
                if (exists) // Nothing went wrong so delete the old files and make the temp directory permanent.
                {
                    Directory.Delete(path, true);
                    Directory.Move(tempPath, path);
                    completedTransfer = true;
                }

                if (filesSaved >= expectedFilesSaved)
                {
                    logger?.Log($"Successfully saved package '{package.GetIdentityString()}'!");
                }
                else
                {
                    logger?.LogWarning($"Completed a partial save of '{package.GetIdentityString()}'. Only {filesSaved} out of {expectedFilesSaved} files were saved.");
                }

                return true;
            }
            catch (Exception ex)
            {
                logger?.LogError($"[{ex.GetType().Name}]: {ex.Message}");
            }

            if (completedTransfer) 
            {
                logger?.LogWarning($"Encountered error while saving package '{package.GetIdentityString()}'. {filesSaved} out of {expectedFilesSaved} files were saved."); 
            } 
            else
            {
                logger?.LogError($"Encountered catastrophic error while saving package '{package.GetIdentityString()}'. No files were saved.");
            }

            return false;
        }

        [Serializable]
        public struct ExternalPackage : IPackage
        {

            public string sourcePath;
            public string cachedPath;

            public ContentPackage content;
            public ContentPackage Content => content;

            public static implicit operator ContentPackage(ExternalPackage pkg) => pkg.content; 

            public PackageIdentifier GetIdentity() => content == null ? default : content.GetIdentity();
            public string GetIdentityString() => content == null ? default : content.GetIdentityString();
        }

        protected readonly List<LocalPackage> localPackages = new List<LocalPackage>();
        protected readonly List<ExternalPackage> externalPackages = new List<ExternalPackage>();

        public static int LocalPackageCount
        {
            get
            {
                var instance = Instance;
                if (instance == null) return 0;
                return instance.localPackages.Count;
            }
        }
        public static int ExternalPackageCount
        {
            get
            {
                var instance = Instance;
                if (instance == null) return 0;
                return instance.externalPackages.Count;
            }
        }

        protected static bool AddLocalPackage(LocalPackage package)
        {
            var instance = Instance;
            if (instance == null) return false;
            if (package == null || package.Content == null) return false;
            var existing = FindLocalPackage(package.GetIdentity());
            if (existing != null && existing.Content != null) return false;
            if (existing == null) 
            { 
                instance.localPackages.Add(package); 
            } 
            else
            {
                existing.Content = package.Content;
            }
            LoadSource(package.Content); 
            return true;
        }
        protected static bool AddExternalPackage(ExternalPackage package)
        {
            var instance = Instance;
            if (instance == null) return false;
            if (package.content == null) return false;
            if (FindExternalPackage(package.content.GetIdentity()).content != null) return false;
            instance.externalPackages.Add(package);
            LoadSource(package.content);
            return true;
        }

        public static LocalPackage GetLocalPackage(int i)
        {
            var instance = Instance;
            if (instance == null || i < 0 || i >= instance.localPackages.Count) return default;
            return instance.localPackages[i];
        }
        public static ExternalPackage GetExternalPackage(int i)
        {
            var instance = Instance;
            if (instance == null || i < 0 || i >= instance.externalPackages.Count) return default;
            return instance.externalPackages[i];
        }

        public static LocalPackage FindLocalPackage(PackageIdentifier id, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false) => FindLocalPackage(id.name, id.version, tryLiberalApproachOnFail, useLiberalNames);
        public static LocalPackage FindLocalPackage(string packageName, string version = null, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false)
        {
            var instance = Instance;
            if (instance == null || string.IsNullOrEmpty(packageName)) return default;

            bool fetchLatest = string.IsNullOrEmpty(version);
            Version latestVersion = new Version("0.0.0.0");
            LocalPackage latestPackage = default;
            for (int a = 0; a < instance.localPackages.Count; a++)
            {
                var pkg = instance.localPackages[a];
                if (pkg == null || pkg.Content == null) continue;
                if ((useLiberalNames ? pkg.Content.Name.AsID() : pkg.Content.Name) != packageName || (!fetchLatest && pkg.Content.VersionString != version)) continue;
                if (!fetchLatest) 
                { 
                    return pkg; 
                }
                else
                {
                    var ver = pkg.Content.Version;
                    if (ver.CompareTo(latestVersion) > 0)
                    {
                        latestVersion = ver;
                        latestPackage = pkg;
                    }
                }
            }
            if (latestPackage != null && latestPackage.Content != null) return latestPackage;

            if (tryLiberalApproachOnFail)
            {
                packageName = packageName.AsID();
                return FindLocalPackage(packageName, version, false, true);
            }

            return default;
        }
        public static bool TryFindLocalPackage(PackageIdentifier id, out LocalPackage package, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false) => TryFindLocalPackage(out package, id.name, id.version, tryLiberalApproachOnFail, useLiberalNames);
        public static bool TryFindLocalPackage(out LocalPackage package, string packageName, string version = null, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false)
        {
            package = FindLocalPackage(packageName, version, tryLiberalApproachOnFail, useLiberalNames);
            return package != null && package.Content != null;
        }
        public static bool CheckIfLocalPackageExists(PackageIdentifier id, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false) => TryFindLocalPackage(id, out _, tryLiberalApproachOnFail, useLiberalNames);
        public static bool CheckIfLocalPackageExists(string packageName, string version = null, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false) => TryFindLocalPackage(out _, packageName, version, tryLiberalApproachOnFail, useLiberalNames);
        public static List<LocalPackage> FindLocalPackages(string packageName, List<LocalPackage> list = null, bool useLiberalNames = true)
        {
            if (list == null) list = new List<LocalPackage>();

            var instance = Instance;
            if (instance == null || string.IsNullOrEmpty(packageName)) return list;

            if (useLiberalNames) packageName = packageName.AsID();
            for (int a = 0; a < instance.localPackages.Count; a++)
            {
                var pkg = instance.localPackages[a];
                if (pkg == null || pkg.Content == null) continue;
                if ((useLiberalNames ? pkg.Content.Name.AsID() : pkg.Content.Name) != packageName) continue;
                list.Add(pkg);
            }

            return list;
        }
        public static List<ContentPackage> FindLocalPackages(string packageName, List<ContentPackage> list = null, bool useLiberalNames = true)
        {
            if (list == null) list = new List<ContentPackage>();

            var instance = Instance;
            if (instance == null || string.IsNullOrEmpty(packageName)) return list;

            if (useLiberalNames) packageName = packageName.AsID();
            for (int a = 0; a < instance.localPackages.Count; a++)
            {
                var pkg = instance.localPackages[a];
                if (pkg == null || pkg.Content == null) continue;
                if ((useLiberalNames ? pkg.Content.Name.AsID() : pkg.Content.Name) != packageName) continue;
                list.Add(pkg);
            }

            return list;
        }
        /// <summary>
        /// Get all local packages with specified name. List will be ordered alphabetically then by version in descending order.
        /// </summary>
        public static List<ContentPackage> FindLocalPackagesOrdered(string packageName, List<ContentPackage> list = null, bool useLiberalNames = true) => FindLocalPackages(packageName, list, useLiberalNames).OrderByDescending(i => i.GetIdentityString()).ToList();

        public static ExternalPackage FindExternalPackage(PackageIdentifier id, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false) => FindExternalPackage(id.name, id.version, tryLiberalApproachOnFail, useLiberalNames);
        public static ExternalPackage FindExternalPackage(string packageName, string version = null, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false)
        {
            var instance = Instance;
            if (instance == null || string.IsNullOrEmpty(packageName)) return default;

            bool fetchLatest = string.IsNullOrEmpty(version);
            Version latestVersion = new Version("0.0.0.0");
            ExternalPackage latestPackage = default;
            for (int a = 0; a < instance.externalPackages.Count; a++)
            {
                var pkg = instance.externalPackages[a];
                if (pkg.content == null) continue;
                if ((useLiberalNames ? pkg.content.Name.AsID() : pkg.content.Name) != packageName || (!fetchLatest && pkg.content.VersionString != version)) continue;
                if (!fetchLatest)
                {
                    return pkg;
                }
                else
                {
                    var ver = pkg.content.Version;
                    if (ver.CompareTo(latestVersion) > 0)
                    {
                        latestVersion = ver;
                        latestPackage = pkg;
                    }
                }
            }
            if (latestPackage.content != null) return latestPackage;

            if (tryLiberalApproachOnFail)
            {
                packageName = packageName.AsID();
                return FindExternalPackage(packageName, version, false, true);
            }

            return default;

        }
        public static bool TryFindExternalPackage(PackageIdentifier id, out ExternalPackage package, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false) => TryFindExternalPackage(out package, id.name, id.version, tryLiberalApproachOnFail, useLiberalNames);
        public static bool TryFindExternalPackage(out ExternalPackage package, string packageName, string version = null, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false)
        {
            package = FindExternalPackage(packageName, version, tryLiberalApproachOnFail, useLiberalNames);
            return package.content != null;
        }
        public static bool CheckIfExternalPackageExists(PackageIdentifier id, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false) => TryFindExternalPackage(id, out _, tryLiberalApproachOnFail, useLiberalNames);
        public static bool CheckIfExternalPackageExists(string packageName, string version = null, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false) => TryFindExternalPackage(out _, packageName, version, tryLiberalApproachOnFail, useLiberalNames);
        public static List<ExternalPackage> FindExternalPackages(string packageName, List<ExternalPackage> list = null, bool useLiberalNames = true)
        {
            if (list == null) list = new List<ExternalPackage>();

            var instance = Instance;
            if (instance == null || string.IsNullOrEmpty(packageName)) return list;

            if (useLiberalNames) packageName = packageName.AsID();
            for (int a = 0; a < instance.externalPackages.Count; a++)
            {
                var pkg = instance.externalPackages[a];
                if (pkg.content == null) continue;
                if ((useLiberalNames ? pkg.content.Name.AsID() : pkg.content.Name) != packageName) continue;
                list.Add(pkg);
            }

            return list;
        }
        public static List<ContentPackage> FindExternalPackages(string packageName, List<ContentPackage> list = null, bool useLiberalNames = true)
        {
            if (list == null) list = new List<ContentPackage>();

            var instance = Instance;
            if (instance == null || string.IsNullOrEmpty(packageName)) return list;

            if (useLiberalNames) packageName = packageName.AsID();
            for (int a = 0; a < instance.externalPackages.Count; a++)
            {
                var pkg = instance.externalPackages[a];
                if (pkg.content == null) continue;
                if ((useLiberalNames ? pkg.content.Name.AsID() : pkg.content.Name) != packageName) continue;
                list.Add(pkg);
            }

            return list;
        }
        /// <summary>
        /// Get all external packages with specified name. List will be ordered alphabetically then by version in descending order.
        /// </summary>
        public static List<ContentPackage> FindExternalPackagesOrdered(string packageName, List<ContentPackage> list = null, bool useLiberalNames = true) => FindExternalPackages(packageName, list, useLiberalNames).OrderByDescending(i => i.GetIdentityString()).ToList();

        public static ContentPackage FindPackage(PackageIdentifier id, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false) => FindPackage(id.name, id.version, tryLiberalApproachOnFail, useLiberalNames);
        public static ContentPackage FindPackage(string packageName, string version = null, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false)
        {
            var instance = Instance;
            if (instance == null || string.IsNullOrEmpty(packageName)) return null;

            bool fetchLatest = string.IsNullOrEmpty(version);
            Version latestVersion = new Version("0.0.0.0");
            ContentPackage latestPackage = null;
            for (int a = 0; a < instance.localPackages.Count; a++)
            {
                var pkg = instance.localPackages[a];
                if (pkg == null || pkg.Content == null) continue;
                if ((useLiberalNames ? pkg.Content.Name.AsID() : pkg.Content.Name) != packageName || (!fetchLatest && pkg.Content.VersionString != version)) continue;
                if (!fetchLatest)
                {
                    return pkg;
                }
                else
                {
                    var ver = pkg.Content.Version;
                    if (ver.CompareTo(latestVersion) > 0)
                    {
                        latestVersion = ver;
                        latestPackage = pkg;
                    }
                }
            }
            for (int a = 0; a < instance.externalPackages.Count; a++)
            {
                var pkg = instance.externalPackages[a];
                if (pkg.content == null) continue;
                if ((useLiberalNames ? pkg.content.Name.AsID() : pkg.content.Name) != packageName || (!fetchLatest && pkg.content.VersionString != version)) continue;
                if (!fetchLatest)
                {
                    return pkg;
                }
                else
                {
                    var ver = pkg.content.Version;
                    if (ver.CompareTo(latestVersion) > 0)
                    {
                        latestVersion = ver;
                        latestPackage = pkg;
                    }
                }
            }
            if (latestPackage != null) return latestPackage;

            if (tryLiberalApproachOnFail)
            {
                packageName = packageName.AsID();
                return FindPackage(packageName, version, false, true);
            }

            return null;
        }
        public static bool TryFindPackage(PackageIdentifier id, out ContentPackage package, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false) => TryFindPackage(out package, id.name, id.version, tryLiberalApproachOnFail, useLiberalNames);
        public static bool TryFindPackage(out ContentPackage package, string packageName, string version = null, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false)
        {
            package = FindPackage(packageName, version, tryLiberalApproachOnFail, useLiberalNames);
            return package != null;
        }
        public static bool CheckIfPackageExists(PackageIdentifier id, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false) => TryFindPackage(id, out _, tryLiberalApproachOnFail, useLiberalNames);
        public static bool CheckIfPackageExists(string packageName, string version = null, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false) => TryFindPackage(out _, packageName, version, tryLiberalApproachOnFail, useLiberalNames);
        public static List<ContentPackage> FindPackages(string packageName, List<ContentPackage> list = null, bool useLiberalNames = true)
        {
            if (list == null) list = new List<ContentPackage>();

            var instance = Instance;
            if (instance == null || string.IsNullOrEmpty(packageName)) return list;

            if (useLiberalNames) packageName = packageName.AsID();
            for (int a = 0; a < instance.localPackages.Count; a++)
            {
                var pkg = instance.localPackages[a];
                if (pkg == null || pkg.Content == null) continue;
                if ((useLiberalNames ? pkg.Content.Name.AsID() : pkg.Content.Name) != packageName) continue;
                list.Add(pkg);
            }
            for (int a = 0; a < instance.externalPackages.Count; a++)
            {
                var pkg = instance.externalPackages[a];
                if (pkg.content == null) continue;
                if ((useLiberalNames ? pkg.content.Name.AsID() : pkg.content.Name) != packageName) continue;
                list.Add(pkg);
            }

            return list;
        }
        /// <summary>
        /// Get all packages with specified name. List will be ordered alphabetically then by version in descending order.
        /// </summary>
        public static List<ContentPackage> FindPackagesOrdered(string packageName, List<ContentPackage> list = null, bool useLiberalNames = true) => FindPackages(packageName, list, useLiberalNames).OrderByDescending(i => i.GetIdentityString()).ToList();
        
        #region Query Content

        public static T FindContent<T>(string assetName, PackageIdentifier package, PackageType packageType = PackageType.Both, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false) where T : IContent
        { 
            return FindContent<T>(assetName, package.name, package.version, packageType, tryLiberalApproachOnFail, useLiberalNames); 
        }
        public static T FindContent<T>(string assetName, string packageName, string packageVersion = null, PackageType packageType = PackageType.Both, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false) where T : IContent
        {
            if (TryFindContent<T>(out T content, assetName, packageName, packageVersion, packageType, tryLiberalApproachOnFail, useLiberalNames)) return content;
            return default;
        }
        public static bool TryFindContent<T>(string assetName, PackageIdentifier package, out T content, PackageType packageType = PackageType.Both, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false) where T : IContent
        {
            return TryFindContent<T>(out content, assetName, package.name, package.version, packageType, tryLiberalApproachOnFail, useLiberalNames);
        }
        public static bool TryFindContent<T>(out T content, string assetName, string packageName, string packageVersion = null, PackageType packageType = PackageType.Both, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false) where T : IContent
        {
            content = default;
            if (string.IsNullOrEmpty(assetName) || string.IsNullOrEmpty(packageName)) return false;

            switch (packageType)
            {
                case PackageType.Both:
                    if (TryFindPackage(out ContentPackage package, packageName, packageVersion, tryLiberalApproachOnFail, useLiberalNames))
                    {
                        if (package.TryFind<T>(out content, assetName, !useLiberalNames)) return true;
                        if (tryLiberalApproachOnFail && package.TryFind<T>(out content, assetName, false)) return true;
                    }
                    break;

                case PackageType.Local:
                    if (TryFindLocalPackage(out LocalPackage localPackage, packageName, packageVersion, tryLiberalApproachOnFail, useLiberalNames))
                    {
                        if (localPackage.Content.TryFind<T>(out content, assetName, !useLiberalNames)) return true;
                        if (tryLiberalApproachOnFail && localPackage.Content.TryFind<T>(out content, assetName, false)) return true;
                    }
                    break;

                case PackageType.External:
                    if (TryFindExternalPackage(out ExternalPackage externalPackage, packageName, packageVersion, tryLiberalApproachOnFail, useLiberalNames))
                    {
                        if (externalPackage.content.TryFind<T>(out content, assetName, !useLiberalNames)) return true;
                        if (tryLiberalApproachOnFail && externalPackage.Content.TryFind<T>(out content, assetName, false)) return true; 
                    }
                    break;
            }

            return false;
        }
        public static bool CheckIfContentExists<T>(string assetName, PackageIdentifier id, PackageType packageType = PackageType.Both, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false) where T : IContent
        { 
            return TryFindContent<T>(assetName, id, out _, packageType, tryLiberalApproachOnFail, useLiberalNames);
        }
        public static bool CheckIfContentExists<T>(string assetName, string packageName, string version = null, PackageType packageType = PackageType.Both, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false) where T : IContent
        { 
            return TryFindContent<T>(out _, assetName, packageName, version, packageType, tryLiberalApproachOnFail, useLiberalNames); 
        }


        public static T FindLocalContent<T>(string assetName, PackageIdentifier package, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false) where T : IContent
        {
            return FindLocalContent<T>(assetName, package.name, package.version,  tryLiberalApproachOnFail, useLiberalNames);
        }
        public static T FindLocalContent<T>(string assetName, string packageName, string packageVersion = null, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false) where T : IContent
        {
            if (TryFindLocalContent<T>(out T content, assetName, packageName, packageVersion, tryLiberalApproachOnFail, useLiberalNames)) return content;
            return default;
        }
        public static bool TryFindLocalContent<T>(string assetName, PackageIdentifier package, out T content, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false) where T : IContent
        {
            return TryFindLocalContent<T>(out content, assetName, package.name, package.version, tryLiberalApproachOnFail, useLiberalNames);
        }
        public static bool TryFindLocalContent<T>(out T content, string assetName, string packageName, string packageVersion = null, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false) where T : IContent
        {
            return TryFindContent<T>(out content, assetName, packageName, packageVersion, PackageType.Local, tryLiberalApproachOnFail, useLiberalNames);
        }
        public static bool CheckIfLocalContentExists<T>(string assetName, PackageIdentifier id, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false) where T : IContent
        {
            return TryFindLocalContent<T>(assetName, id, out _, tryLiberalApproachOnFail, useLiberalNames);
        }
        public static bool CheckIfLocalContentExists<T>(string assetName, string packageName, string version = null, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false) where T : IContent
        {
            return TryFindLocalContent<T>(out _, assetName, packageName, version, tryLiberalApproachOnFail, useLiberalNames);
        }


        public static T FindExternalContent<T>(string assetName, PackageIdentifier package, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false) where T : IContent
        {
            return FindExternalContent<T>(assetName, package.name, package.version, tryLiberalApproachOnFail, useLiberalNames);
        }
        public static T FindExternalContent<T>(string assetName, string packageName, string packageVersion = null, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false) where T : IContent
        {
            if (TryFindExternalContent<T>(out T content, assetName, packageName, packageVersion, tryLiberalApproachOnFail, useLiberalNames)) return content;
            return default;
        }
        public static bool TryFindExternalContent<T>(string assetName, PackageIdentifier package, out T content, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false) where T : IContent
        {
            return TryFindExternalContent<T>(out content, assetName, package.name, package.version, tryLiberalApproachOnFail, useLiberalNames);
        }
        public static bool TryFindExternalContent<T>(out T content, string assetName, string packageName, string packageVersion = null, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false) where T : IContent
        {
            return TryFindContent<T>(out content, assetName, packageName, packageVersion, PackageType.External, tryLiberalApproachOnFail, useLiberalNames);
        }
        public static bool CheckIfExternalContentExists<T>(string assetName, PackageIdentifier id, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false) where T : IContent
        {
            return TryFindExternalContent<T>(assetName, id, out _, tryLiberalApproachOnFail, useLiberalNames);
        }
        public static bool CheckIfExternalContentExists<T>(string assetName, string packageName, string version = null, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false) where T : IContent
        {
            return TryFindExternalContent<T>(out _, assetName, packageName, version, tryLiberalApproachOnFail, useLiberalNames);
        }

        #endregion

        public static string PackageDirectoryPath => Path.Combine(swole.AssetDirectory.FullName, folderNames_Packages);
        public static string LocalPackageDirectoryPath => Path.Combine(PackageDirectoryPath, folderNames_LocalPackages);
        public static string CachedPackageDirectoryPath => Path.Combine(PackageDirectoryPath, folderNames_CachedPackages);

        protected DirectoryInfo localPackageDirectory;
        public DirectoryInfo LocalPackageDirectory => localPackageDirectory;

        protected DirectoryInfo cachedPackageDirectory;
        public DirectoryInfo CachedPackageDirectory => cachedPackageDirectory;

        private static void UnloadSource(ContentPackage package)
        {
            if (package == null) return;
            var sourcePackage = package.AsSourcePackage();
            if (sourcePackage != null)
            {
                try
                {
                    swole.UnloadSourcePackage(sourcePackage, out _);
                }
                catch (Exception ex)
                {
                    swole.DefaultLogger?.LogError($"Encountered an error while unloading code source from package '{package}'");
                    swole.DefaultLogger?.LogError($"[{ex.GetType().FullName}]: {ex.Message}");
                }
            }
        }
        private static void LoadSource(ContentPackage package)
        {
            if (package == null) return;
            var sourcePackage = package.AsSourcePackage();
            if (sourcePackage != null && !sourcePackage.IsEmpty)
            {
                try
                {
                    var result = swole.LoadSourcePackage(sourcePackage, out string resultInfo);
                    if (result != swole.PackageActionResult.Success)
                    {
                        if (result == swole.PackageActionResult.PackageAlreadyLoaded)
                        {
                            swole.DefaultLogger?.LogWarning($"Tried to load code source from package '{package}' but it was already present.");
                        }
                        else
                        {
                            swole.DefaultLogger?.LogError($"Failed to load code source from package '{package}'!");
                            swole.DefaultLogger?.LogError($"[{result}]{(!string.IsNullOrEmpty(resultInfo) ? $": {resultInfo}" : "")}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    swole.DefaultLogger?.LogError($"Encountered an error while loading code source from package '{package}'");
                    swole.DefaultLogger?.LogError($"[{ex.GetType().FullName}]: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Load packages in the form of directories from the local file system.
        /// </summary>
        public static void LoadPackagesLocally(string directory) => LoadPackagesLocally(new DirectoryInfo(directory));
        /// <summary>
        /// Load packages in the form of directories from the local file system.
        /// </summary>
        public static void LoadPackagesLocally(DirectoryInfo directory)
        {
            if (directory == null) return;

            var localPackageDirectories = directory.GetDirectories("*", SearchOption.TopDirectoryOnly);
            foreach (var dir_ in localPackageDirectories)
            {         
                if (!DirectoryIsPackage(dir_)) // Directory is not a package so check for packages stored inside of it instead
                {
                    LoadPackagesLocally(dir_);
                    continue;
                }

                DirectoryInfo dir = dir_;
                if (dir.Name.StartsWith(tags_Temporary)) // This could possibly happen if the app crashed while trying to save a local package.
                {
                    try
                    {
                        string truePath = Path.Combine(dir.Parent.FullName, dir.Name.Replace(tags_Temporary + ".", "").Replace(tags_Temporary, ""));
                        if (Directory.Exists(truePath))
                        {
                            Directory.Delete(dir.FullName, true); // The original directory exists so just delete the temporary one.
                            continue;
                        }
                        else
                        {
                            Directory.Move(dir.FullName, truePath); // If the temporary directory exists but the original doesn't, then our best course of action is to make the temporary directory permanent.
                            dir = new DirectoryInfo(truePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        swole.DefaultLogger?.LogError($"Encountered an error while evaluating a temporary package folder: '{dir.Name}'");
                        swole.DefaultLogger?.LogError($"[{ex.GetType().FullName}]: {ex.Message}");
                    }
                }

                LoadPackage(dir, swole.DefaultLogger);
            }
        }

        /// <summary>
        /// Load packages in the form of zip archives from the local file system.
        /// </summary>
        public static void LoadPackagesExternally(string directory) => LoadPackagesExternally(new DirectoryInfo(directory));
        /// <summary>
        /// Load packages in the form of zip archives from the local file system.
        /// </summary>
        public static void LoadPackagesExternally(DirectoryInfo directory)
        {
            if (directory == null) return;

            var files = directory.EnumerateFiles("*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                string nameLower = file.Name.AsID();
                if (!nameLower.EndsWith(fileExtension_ZIP) && !nameLower.EndsWith(fileExtension_Package)) continue;
                LoadPackage(string.Empty, file.FullName, swole.DefaultLogger);
            }
        }

        private void ReloadLocalPackagesLocal()
        {
            foreach (var package in localPackages) if (package != null) 
                {
                    package.ClearListeners();
                    UnloadSource(package.Content); 
                }
            localPackages.Clear();

            localPackageDirectory = Directory.CreateDirectory(LocalPackageDirectoryPath);
            LoadPackagesLocally(localPackageDirectory);

            ReloadProjectInfo();
            if (projects != null)
            {
                void LoadDirectoryIfNotLocal(string projectName, string path)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(path) || !SwoleUtil.IsValidPath(path) || SwoleUtil.IsSubPathOf(path, localPackageDirectory.FullName)) return;
                        LoadPackagesLocally(path);
                    }
                    catch(Exception ex)
                    {
                        swole.DefaultLogger?.LogError($"Encountered an error while attempting to load local packages from directory '{path}'.{(string.IsNullOrEmpty(projectName) ? string.Empty : $" (Project: {projectName})")}");
                        swole.DefaultLogger?.LogError($"[{ex.GetType().Name}]: {ex.Message}");
                    } 
                }
                foreach(Project proj in projects)
                {
                    LoadDirectoryIfNotLocal(proj.name, proj.primaryPath);
                    if (proj.externalPaths != null)
                    {
                        foreach (string externalPath in proj.externalPaths) LoadDirectoryIfNotLocal(proj.name, externalPath);
                    }
                }
            }

            ReloadProjectInfo();
        }
        private void ReloadExternalPackagesLocal()
        {
            foreach (var package in externalPackages) UnloadSource(package.content);
            externalPackages.Clear();

            localPackageDirectory = Directory.CreateDirectory(LocalPackageDirectoryPath);
            LoadPackagesExternally(localPackageDirectory); // Load zip archive packages from local directory

            cachedPackageDirectory = Directory.CreateDirectory(CachedPackageDirectoryPath);
            LoadPackagesExternally(cachedPackageDirectory); // Load zip archive packages from cache directory
        }
        public void ReloadAllPackagesLocal()
        {

            foreach (var package in localPackages) if (package != null) 
                {
                    package.ClearListeners();
                    UnloadSource(package.Content); 
                }
            localPackages.Clear();
            foreach (var package in externalPackages) UnloadSource(package.content);
            externalPackages.Clear();

            ReloadLocalPackagesLocal();
            ReloadExternalPackagesLocal();
        }
        public static void ReloadLocalPackages()
        {
            var instance = Instance;
            if (instance == null) return;
            instance.ReloadLocalPackagesLocal();
        }
        public static void ReloadExternalPackages()
        {
            var instance = Instance;
            if (instance == null) return;
            instance.ReloadExternalPackagesLocal();
        }
        public static void ReloadAllPackages()
        {
            var instance = Instance;
            if (instance == null) return;
            instance.ReloadAllPackagesLocal();
        }

        [Serializable]
        public enum SaveMethod
        {
            None, Immediate, InBackground
        }

        public static string ProjectIdentifiersFilePath => Path.Combine(swole.AssetDirectory.FullName, commonFiles_Projects);

        [Serializable]
        public struct Project
        {
            public string name;
            public string primaryPath;
            public string[] externalPaths;
        }

        [Serializable]
        public struct ProjectIdentifier 
        {
            public string packageName;
            public string projectName;
        }
        [Serializable]
        public struct SerializableProjectInfo
        {
            public Project[] projects;
            //public Dictionary<string, string> identifiers; // Not compatible with Unity JSON serialization
            public ProjectIdentifier[] identifiers;
        }

        private List<Project> projects;
        private Dictionary<string, string> projectIdentifiers = null; 
        private static SerializableProjectInfo ProjectIdentifiersSerializable
        {
            get
            {
                /*var instance = Instance;
                if (instance == null) return default;
                return new SerializableProjectInfo() { projects = instance.projects, identifiers = instance.projectIdentifiers };*/ // Not compatible with Unity JSON serialization

                var instance = Instance;
                if (instance == null || instance.projectIdentifiers == null) return new SerializableProjectInfo();
                ProjectIdentifier[] array = new ProjectIdentifier[instance.projectIdentifiers.Count];
                int i = 0;
                foreach(var set in instance.projectIdentifiers)
                {
                    array[i] = new ProjectIdentifier() { packageName = set.Key, projectName = set.Value };
                    i++;
                }
                return new SerializableProjectInfo() { projects = instance.projects.ToArray(), identifiers = array };
            }
        }
        private static void Deserialize(SerializableProjectInfo serializedProjectInfo)
        {
            /*var instance = Instance;
            if (instance == null) return;
            instance.projectIdentifiers = serializedProjectIdentifiers.identifiers;*/ // Not compatible with Unity JSON serialization

            if (serializedProjectInfo.identifiers == null && serializedProjectInfo.projects == null) return;
            var instance = Instance;
            if (instance == null) return;
            if (instance.projectIdentifiers == null) instance.projectIdentifiers = new Dictionary<string, string>();
            instance.projectIdentifiers.Clear();
            foreach (var identifier in serializedProjectInfo.identifiers) instance.projectIdentifiers[identifier.packageName] = identifier.projectName;

            if (instance.projects == null) instance.projects = new List<Project>();
            instance.projects.Clear();
            if (serializedProjectInfo.projects != null) instance.projects.AddRange(serializedProjectInfo.projects);
        }

        public static int IndexOfProject(string projectName)
        {
            var instance = Instance;
            if (instance == null || string.IsNullOrEmpty(projectName)) return -1;
            if (instance.projectIdentifiers == null || instance.projects == null)
            {
                ContentManager.ReloadProjectInfo();
                if (instance.projectIdentifiers == null || instance.projects == null) return -1;
            }
            for (int a = 0; a < instance.projects.Count; a++) if (instance.projects[a].name == projectName) return a;
            projectName = projectName.AsID();
            for (int a = 0; a < instance.projects.Count; a++) if (instance.projects[a].name.AsID() == projectName) return a;
            return -1;
        }
        public static bool CheckIfProjectExists(string projectName) => IndexOfProject(projectName) >= 0;
        public static bool TryFindProject(string projectName, out Project project) => TryFindProject(projectName, out project, out _);
        public static bool TryFindProject(string projectName, out Project project, out int index)
        {
            index = -1;
            project = default;
            var instance = Instance;
            if (instance == null) return false;

            index = IndexOfProject(projectName);
            if (index < 0) return false;

            project = instance.projects[index];
            return true;
        }
        public static void SetProjectPrimaryPath(string projectName, string path)
        {
            var instance = Instance;
            if (instance == null) return;

            if (!TryFindProject(projectName, out Project project, out int index)) return;

            project.primaryPath = path;
            instance.projects[index] = project;
        }

        public static void SetProjectIdentifier(SourcePackage package, string projectName, SaveMethod saveMethod = SaveMethod.InBackground) => SetProjectIdentifier(package == null ? string.Empty : package.Name, projectName, saveMethod);
        public static void SetProjectIdentifier(ContentPackage package, string projectName, SaveMethod saveMethod = SaveMethod.InBackground) => SetProjectIdentifier(package == null ? string.Empty : package.Name, projectName, saveMethod);
        public static void SetProjectIdentifier(PackageInfo info, string projectName, SaveMethod saveMethod = SaveMethod.InBackground) => SetProjectIdentifier(info.name, projectName, saveMethod);
        public static void SetProjectIdentifier(string packageName, string projectName, SaveMethod saveMethod = SaveMethod.InBackground)
        {
            if (string.IsNullOrEmpty(packageName) || string.IsNullOrEmpty(projectName)) return;
            var instance = Instance;
            if (instance == null) return;
            if (instance.projectIdentifiers == null)
            {
                ReloadProjectInfo();
                if (instance.projectIdentifiers == null) instance.projectIdentifiers = new Dictionary<string, string>();
            }
            instance.projectIdentifiers[packageName] = projectName;
            if (!CheckIfProjectExists(projectName))
            {
                string projectNameID = projectName.AsID();
                string primaryPath = Path.Combine(LocalPackageDirectoryPath, projectName);
                FindLocalPackages(packageName, _tempLocalPackageList);
                foreach(var localPkg in _tempLocalPackageList)
                {
                    if (localPkg == null || localPkg.Content == null || localPkg.workingDirectory == null || !localPkg.workingDirectory.Exists) continue;
                    if (localPkg.workingDirectory.Name.AsID() == projectNameID)
                    {
                        primaryPath = localPkg.workingDirectory.FullName;
                        break;
                    }
                    DirectoryInfo parent = localPkg.workingDirectory.Parent;
                    if (parent != null && parent.Name.AsID() == projectNameID)
                    {
                        primaryPath = parent.FullName;
                        break;
                    }
                }
                 
                Project project = new Project() { name = projectName, primaryPath = primaryPath };
                instance.projects.Add(project);
            }
            switch (saveMethod)
            {
                default:
                    break;

                case SaveMethod.Immediate:
                    SaveProjectInfo();
                    break;

                case SaveMethod.InBackground:
                    SaveProjectInfoAsync();
                    break;
            }
        }
        public static string GetProjectIdentifier(SourcePackage package) => GetProjectIdentifier(package == null ? string.Empty : package.Name);
        public static string GetProjectIdentifier(ContentPackage package) => GetProjectIdentifier(package == null ? string.Empty : package.Name);
        public static string GetProjectIdentifier(PackageInfo info) => GetProjectIdentifier(info.name);
        public static string GetProjectIdentifier(string packageName)
        {
            if (TryGetProjectIdentifier(packageName, out string projectName)) return projectName;
            return packageName;
        }

        public static bool TryGetProjectIdentifier(SourcePackage package, out string projectName) => TryGetProjectIdentifier(package == null ? string.Empty : package.Name, out projectName);
        public static bool TryGetProjectIdentifier(ContentPackage package, out string projectName) => TryGetProjectIdentifier(package == null ? string.Empty : package.Name, out projectName);
        public static bool TryGetProjectIdentifier(PackageInfo info, out string projectName) => TryGetProjectIdentifier(info.name, out projectName);
        public static bool TryGetProjectIdentifier(string packageName, out string projectName)
        {
            projectName = string.Empty;
            if (string.IsNullOrEmpty(packageName)) return false;
            var instance = Instance;
            if (instance == null) return false;
            if (instance.projectIdentifiers == null)
            {
                ReloadProjectInfo();
                if (instance.projectIdentifiers == null) return false;
            }

            return instance.projectIdentifiers.TryGetValue(packageName, out projectName);
        }

        public static bool HasProjectIdentifier(SourcePackage package) => HasProjectIdentifier(package == null ? string.Empty : package.Name);
        public static bool HasProjectIdentifier(ContentPackage package) => HasProjectIdentifier(package == null ? string.Empty : package.Name);
        public static bool HasProjectIdentifier(PackageInfo info) => HasProjectIdentifier(info.name);
        public static bool HasProjectIdentifier(string packageName)
        {
            if (string.IsNullOrEmpty(packageName)) return false;
            var instance = Instance;
            if (instance == null) return false;
            if (instance.projectIdentifiers == null)
            {
                ReloadProjectInfo();
                if (instance.projectIdentifiers == null) return false;
            }
            return instance.projectIdentifiers.ContainsKey(packageName);
        }

        private static bool savingProjectInfo; 
        public static void SaveProjectInfo() => SaveProjectInfo(true).GetAwaiter().GetResult();
        public static Task SaveProjectInfoAsync() => SaveProjectInfo(false);
        async private static Task SaveProjectInfo(bool sync)
        {
            if (savingProjectInfo) return;
            savingProjectInfo = true;
            try
            {
                if (Instance == null) return;
                string filePath = ProjectIdentifiersFilePath;
                string json = swole.Engine.ToJson(ProjectIdentifiersSerializable, true);
                if (sync) File.WriteAllBytes(filePath, DefaultJsonSerializer.StringEncoder.GetBytes(json)); else await File.WriteAllBytesAsync(filePath, DefaultJsonSerializer.StringEncoder.GetBytes(json));
            }
            catch(Exception ex)
            {
                swole.LogError("Encountered an error while saving project info.");
                swole.LogError(ex);
            }
            savingProjectInfo = false;
        }

        public static void ReloadProjectInfo() => ReloadProjectInfo(true).GetAwaiter().GetResult();
        public static Task ReloadProjectInfoAsync() => ReloadProjectInfo(false);
        async private static Task ReloadProjectInfo(bool sync)
        {
            if (savingProjectInfo) return;
            try
            {
                if (Instance == null) return;
                string filePath = ProjectIdentifiersFilePath;
                if (!File.Exists(filePath)) return;
                Deserialize(swole.Engine.FromJson<SerializableProjectInfo>(DefaultJsonSerializer.StringEncoder.GetString(sync ? File.ReadAllBytes(filePath) : await File.ReadAllBytesAsync(filePath))));
            }
            catch (Exception ex)
            {
                swole.LogError("Encountered an error while loading project info.");
                swole.LogError(ex);
            }
        }

        /// <summary>
        /// Remove empty projects.
        /// </summary>
        public static void CleanProjectInfo() => CleanProjectInfo(true).GetAwaiter().GetResult();
        /// <summary>
        /// Remove empty projects.
        /// </summary>
        public static Task CleanProjectInfoAsync() => CleanProjectInfo(false);
        private static readonly List<string> cleanerList1 = new List<string>();
        private static readonly List<string> cleanerList2 = new List<string>();
        private static readonly List<string> cleanerList3 = new List<string>();
        async private static Task CleanProjectInfo(bool sync)
        {
            if (savingProjectInfo) return;
            try
            {
                var instance = Instance;
                if (instance == null) return;
                if (instance.projectIdentifiers == null || instance.projects == null)
                {
                    if (sync) ReloadProjectInfo(); else await ReloadProjectInfoAsync();
                    if (instance.projectIdentifiers == null || instance.projects == null) return;
                }
                cleanerList1.Clear();
                cleanerList2.Clear();
                cleanerList3.Clear();
                foreach (var set in instance.projectIdentifiers)
                {
                    var pkg = FindLocalPackage(set.Key);
                    if ((pkg == null || pkg.Content == null) && !cleanerList2.Contains(set.Value))
                    {
                        cleanerList1.Add(set.Key);
                    } 
                    else if (pkg != null && pkg.Content != null)
                    {
                        cleanerList2.Add(set.Value);
                        cleanerList1.RemoveAll(i => (instance.projectIdentifiers.ContainsKey(i) && instance.projectIdentifiers[i] == set.Value));
                    }
                }
                foreach (var key in cleanerList1)
                {
                    instance.projectIdentifiers.Remove(key);
                }
                for(int a = 0; a < instance.projects.Count; a++)
                {
                    var proj = instance.projects[a];
                    if (!cleanerList2.Contains(proj.name))
                    {
                        cleanerList3.Add(proj.name);
                        if (!string.IsNullOrEmpty(proj.primaryPath))
                        {
                            DirectoryInfo dir = new DirectoryInfo(proj.primaryPath);
                            if (dir.IsEmpty())
                            {
                                if (dir.Exists) dir.Delete();
                            }
                        }
                    }
                }
                foreach (string projName in cleanerList3) instance.projects.RemoveAll(i => i.name == projName);
                cleanerList1.Clear();
                cleanerList2.Clear();
                cleanerList3.Clear();

                if (sync) SaveProjectInfo(); else await SaveProjectInfoAsync();
            }
            catch (Exception ex)
            {
                swole.LogError("Encountered an error while cleaning project identifiers.");
                swole.LogError(ex);
            }
        }

        public static int GetNumberOfExternalPackageVersions(string packageName)
        {
            var instance = Instance;

            int count = 0;
            if (!string.IsNullOrEmpty(packageName) && instance != null)
            {
                packageName = packageName.AsID();

                foreach (var pkg in instance.externalPackages)
                {
                    if (pkg.content == null) continue;
                    if (pkg.content.Name.AsID() == packageName) count++;
                }
            }
            return count;
        }

        public static int GetNumberOfPackagesInProject(string projectName)
        {
            var instance = Instance;

            int count = 0;
            if (!string.IsNullOrEmpty(projectName) && instance != null)
            {
                projectName = projectName.AsID();

                if (instance.projectIdentifiers == null) ReloadProjectInfo();

                foreach (var pkg in instance.localPackages)
                {
                    if (pkg == null || pkg.Content == null) continue;
                    if (GetProjectIdentifier(pkg.Content).AsID() == projectName) count++;
                }
            }
            return count;
        } 

        /// <summary>
        /// Get a list of packages in a project ordered alphabetically and then by version in descending order.
        /// </summary>
        public static List<ContentPackage> GetPackagesInProject(string projectName, List<ContentPackage> list = null)
        {
            if (list == null) list = new List<ContentPackage>();

            var instance = Instance;

            if (!string.IsNullOrEmpty(projectName) && instance != null)
            {
                projectName = projectName.AsID();
                _tempLocalPackageList.Clear();

                if (instance.projectIdentifiers == null) ReloadProjectInfo();

                if (instance.projectIdentifiers != null)
                {
                    foreach (var set in instance.projectIdentifiers)
                    {
                        if (set.Value.AsID() == projectName)
                        {
                            ContentManager.FindLocalPackages(set.Key, _tempLocalPackageList);
                        }
                    }
                }

                foreach (var pkg in instance.localPackages)
                {
                    if (pkg == null || pkg.Content == null) continue;
                    bool contains = false;
                    foreach(var mem in _tempLocalPackageList) if (mem.Content != null && mem.Content.GetIdentityString() == pkg.Content.GetIdentityString())
                        {
                            contains = true;
                            break;
                        }
                    if (!contains && pkg.Content.Name.AsID() == projectName) _tempLocalPackageList.Add(pkg);
                }

                for (int a = 0; a < _tempLocalPackageList.Count; a++) list.Add(_tempLocalPackageList[a]);
            }

            _tempLocalPackageList.Clear();

            list = list.OrderByDescending(i => i.GetIdentityString()).ToList();
            return list;
        }

        private bool initialized;
        public static bool IsInitialized
        {
            get
            {
                var instance = Instance;
                if (instance == null) return false;
                return instance.initialized;
            }
        }

        public static bool Initialize(bool force = false)
        {
            var instance = Instance;
            if (instance == null) return false;

            return instance.InitializeLocal(force);
        }

        public bool InitializeLocal(bool force = false)
        {

            if (initialized && !force) return false;

            ReloadAllPackagesLocal();
            CleanProjectInfo();

            initialized = true;
            return true;

        }

        protected override void OnInit()
        {

            base.OnInit(); 

            if (Instance != this) return;

            InitializeLocal();

        }

    }

}
