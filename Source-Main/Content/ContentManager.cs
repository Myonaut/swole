using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Swole.Script;

namespace Swole
{

    public class ContentManager : SingletonBehaviour<ContentManager>
    {

        //
        public override bool ExecuteInStack => false;
        public override void OnFixedUpdate() { }
        public override void OnLateUpdate() { }
        public override void OnUpdate() { }
        //

        public const string folderNames_Packages = "packages";
        public const string folderNames_LocalPackages = "local";
        public const string folderNames_CachedPackages = "cache";

        public const string tags_Temporary = ".TEMP";

        public const string commonFiles_Manifest = "manifest.json";

        public const string fileExtension_Default = "json";
        public const string fileExtension_Package = "swole";
        public const string fileExtension_Script = "swlscr";
        public const string fileExtension_Creation = "swlobj";
        public const string fileExtension_Animation = "swlanim";

        public static bool HasValidFileExtension(string fileName)
        {

            if (string.IsNullOrEmpty(fileName)) return false;

            if (fileName.EndsWith(fileExtension_Package)) return true;

            if (fileName.EndsWith(fileExtension_Script)) return true;
            if (fileName.EndsWith(fileExtension_Creation)) return true;
            if (fileName.EndsWith(fileExtension_Animation)) return true;

            return false;

        }

        public static IContent LoadContent(PackageInfo packageInfo, FileInfo file, SwoleLogger logger = null) => LoadContent(packageInfo, file.FullName, logger);
        public static IContent LoadContent(PackageInfo packageInfo, string path, SwoleLogger logger = null) => LoadContentInternal(true, packageInfo, path, logger).GetAwaiter().GetResult();
        public static Task<IContent> LoadContentAsync(PackageInfo packageInfo, FileInfo file, SwoleLogger logger = null) => LoadContentAsync(packageInfo, file.FullName, logger); 
        public static Task<IContent> LoadContentAsync(PackageInfo packageInfo, string path, SwoleLogger logger = null) => LoadContentInternal(false, packageInfo, path, logger);
        async private static Task<IContent> LoadContentInternal(bool sync, PackageInfo packageInfo, string path, SwoleLogger logger = null)
        {
            if (path.EndsWith(fileExtension_Script))
            {
                byte[] data = sync ? File.ReadAllBytes(path) : await File.ReadAllBytesAsync(path);
                return LoadContent<SourceScript>(packageInfo, data, logger);
            }
            else if (path.EndsWith(fileExtension_Creation))
            {
                byte[] data = sync ? File.ReadAllBytes(path) : await File.ReadAllBytesAsync(path);
                return LoadContent<Creation>(packageInfo, data, logger);
            }
            else if (path.EndsWith(fileExtension_Animation))
            {
                // Not implemented yet
            }

            return default;
        }
        public static T LoadContent<T>(PackageInfo packageInfo, byte[] rawData, SwoleLogger logger = null) where T : IContent
        { 

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
                        ISerializableContainer container = (ISerializableContainer)Swole.Engine.FromJson(json, interfaceType.GetGenericArguments()[1]);
                        return (T)container.AsNonserializableObject(packageInfo);
                    }
                }

                // Fallback attempt
                json = DefaultJsonSerializer.StringEncoder.GetString(rawData);
                if (string.IsNullOrEmpty(json)) return default;

                return Swole.Engine.FromJson<T>(json);
                //

            } 
            catch(Exception ex)
            {

                logger?.LogError($"Error while attempting to load content{(packageInfo.NameIsValid ? $" from package {packageInfo.name}" : "")} with type '{typeof(T).FullName}'");
                logger?.LogError($"[{ex.GetType().Name}]: {ex.Message}");

            }

            return default;
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

                if (content is SourceScript script)
                {

                    string json = script.AsJSON(true);
                    bytes = DefaultJsonSerializer.StringEncoder.GetBytes(json);
                    fullPath = Path.Combine(directoryPath, $"{script.Name}.{fileExtension_Script}");
                    return true;

                }
                else if (content is Creation creation)
                {

                    string json = creation.AsJSON(true);
                    bytes = DefaultJsonSerializer.StringEncoder.GetBytes(json);
                    fullPath = Path.Combine(directoryPath, $"{creation.Name}.{fileExtension_Creation}");

                }

                if (string.IsNullOrEmpty(fullPath)) fullPath = Path.Combine(directoryPath, $"{content.Name}.{fileExtension_Default}");

                if (bytes == null) 
                { 
                    string json = Swole.Engine.ToJson(content, true);
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
        [Serializable]
        public struct LocalPackage
        {
            public DirectoryInfo workingDirectory;
            public ContentPackage instance;

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
                    return instance.localPackages[a]; 
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
            var files = packageDirectory.EnumerateFiles("*", SearchOption.AllDirectories).Where(i => HasValidFileExtension(i.Name));
            foreach(var file in files)
            {
                var contentObj = sync ? LoadContent(manifest, file, logger) : await LoadContentAsync(manifest, file, logger);
                if (contentObj == null) continue;
                content.Add(contentObj);
            }

            package.instance = new ContentPackage(manifest, content);
            //

            instance.localPackages.Add(package);

            return package;
        }

        /// <summary>
        /// Loads a package externally from a zip file.
        /// </summary>
        public static ExternalPackage LoadPackage(string sourcePath, string cachedPath = null, SwoleLogger logger = null) => LoadPackageInternal(true, sourcePath, cachedPath, logger).GetAwaiter().GetResult();
        /// <summary>
        /// Asynchronously loads a package externally from a zip file.
        /// </summary>
        public static Task<ExternalPackage> LoadPackageAsync(string sourcePath, string cachedPath = null, SwoleLogger logger = null) => LoadPackageInternal(false, sourcePath, cachedPath, logger);
        async private static Task<ExternalPackage> LoadPackageInternal(bool sync, string sourcePath, string cachedPath = null, SwoleLogger logger = null)
        {
            if (string.IsNullOrEmpty(sourcePath) && string.IsNullOrEmpty(cachedPath)) return default;
            var instance = Instance;
            if (instance == null) return default;

            List<EngineHook.FileDescr> fileNames = null;
            List<byte[]> fileData = null;
            if (string.IsNullOrEmpty(cachedPath))
            {
                cachedPath = Path.Combine(Swole.AssetDirectory.FullName, folderNames_Packages, folderNames_CachedPackages, Path.GetFileName(sourcePath));
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
                Swole.Engine.DecompressZIP(cachedPath, ref fileNames, ref fileData); 
            }
            else
            {
                var result = await Swole.Engine.DecompressZIPAsync(cachedPath);
                fileNames = result.fileNames;
                fileData = result.fileData;
            }

            if (fileNames == null || fileData == null) return default;
            Type[] fileTypes = new Type[fileNames.Count];
            byte[] manifestFile = null;
            for(int a = 0; a < fileTypes.Length; a++)
            {
                var fileDesc = fileNames[a];
                if (string.IsNullOrEmpty(fileDesc.fileName)) continue;
                if (Path.GetFileName(fileDesc.fileName).AsID() == commonFiles_Manifest.AsID())
                {
                    manifestFile = fileData[a];
                    continue;
                } 
                if (fileDesc.fileName.EndsWith(fileExtension_Script))
                {
                    fileTypes[a] = typeof(SourceScript);
                } 
                else if (fileDesc.fileName.EndsWith(fileExtension_Creation))
                {
                    fileTypes[a] = typeof(Creation);
                }
            }
             
            return LoadPackage(sourcePath, cachedPath, manifestFile, fileData, fileTypes, logger);
        }
        /// <summary>
        /// Loads a package externally from data in memory.
        /// </summary>
        public static ExternalPackage LoadPackage(string sourcePath, string cachedPath, byte[] manifestFile, ICollection<byte[]> files, ICollection<Type> fileTypes, SwoleLogger logger = null)
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
                logger?.LogError($"Error loading external package from '{sourcePath}' — Reason: No package manifest was given!");
                return default;
            }

            if (!manifest.NameIsValid)
            {
                logger?.LogError($"Error loading external package from '{sourcePath}' — Reason: Name in package manifest was invalid!");
                return default;
            }

            for (int a = 0; a < instance.externalPackages.Count; a++) if (instance.externalPackages[a].instance != null && instance.externalPackages[a].instance.GetIdentityString() == manifest.GetIdentityString())
                {
                    logger?.LogWarning($"Tried to load external package from '{sourcePath}' — but it was already loaded.");
                    return instance.externalPackages[a];
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

            // Load valid files and ignore the rest
            List<IContent> content = new List<IContent>();
            using (var enuFiles = files.GetEnumerator())
            using (var enuTypes = fileTypes.GetEnumerator())
            {
                while (enuFiles.MoveNext() && enuTypes.MoveNext())
                {
                    var fileType = enuTypes.Current;
                    var fileData = enuFiles.Current;
                    if (fileType == null || fileData == null) continue;
                    IContent contentObj = null;
                    if (fileType == typeof(SourceScript))
                    {
                        contentObj = LoadContent<SourceScript>(manifest, fileData, logger);
                    }
                    else if (fileType == typeof(Creation))
                    {
                        contentObj = LoadContent<Creation>(manifest, fileData, logger);
                    }
                    if (contentObj == null) continue;
                    content.Add(contentObj);
                }
            }

            package.instance = new ContentPackage(manifest, content);
            //

            instance.externalPackages.Add(package);

            return package;
        }

        public static bool SavePackage(LocalPackage package, SwoleLogger logger = null) => SavePackage(package.workingDirectory, package.instance, logger);
        public static bool SavePackage(DirectoryInfo dir, ContentPackage package, SwoleLogger logger = null) => SavePackage(dir == null ? "" : dir.FullName, package, logger);
        public static bool SavePackage(ContentPackage package, SwoleLogger logger = null) => SavePackage(Path.Combine(LocalPackageDirectoryPath, package == null ? "null" : package.GetIdentityString()), package, logger);
        public static bool SavePackage(string path, ContentPackage package, SwoleLogger logger = null) => SavePackageInternal(true, path, package, logger).GetAwaiter().GetResult();

        public static Task<bool> SavePackageAsync(LocalPackage package, SwoleLogger logger = null) => SavePackageAsync(package.workingDirectory, package.instance, logger);
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
                byte[] manifestBytes = DefaultJsonSerializer.StringEncoder.GetBytes(Swole.Engine.ToJson(package.Manifest, true));
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
        public struct ExternalPackage
        {

            public string sourcePath;
            public string cachedPath;

            public ContentPackage instance;

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

        public static ContentPackage FindPackage(PackageIdentifier id, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false) => FindPackage(id.name, id.version, tryLiberalApproachOnFail, useLiberalNames);
        public static ContentPackage FindPackage(string packageName, string version = null, bool tryLiberalApproachOnFail = true, bool useLiberalNames = false)
        {
            var instance = Instance;
            if (instance == null || string.IsNullOrEmpty(packageName)) return null;

            for (int a = 0; a < instance.localPackages.Count; a++)
            {
                var pkg = instance.localPackages[a];
                if (pkg.instance == null) continue;
                if ((useLiberalNames ? pkg.instance.Name.AsID() : pkg.instance.Name) != packageName || (!string.IsNullOrEmpty(version) && pkg.instance.VersionString != version)) continue;
                return pkg.instance;
            }
            for (int a = 0; a < instance.externalPackages.Count; a++)
            {
                var pkg = instance.externalPackages[a];
                if (pkg.instance == null) continue;
                if ((useLiberalNames ? pkg.instance.Name.AsID() : pkg.instance.Name) != packageName || (!string.IsNullOrEmpty(version) && pkg.instance.VersionString != version)) continue;
                return pkg.instance;
            }

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

        public static string PackageDirectoryPath => Path.Combine(Swole.AssetDirectory.FullName, folderNames_Packages);
        public static string LocalPackageDirectoryPath => Path.Combine(PackageDirectoryPath, folderNames_LocalPackages);
        public static string CachedPackageDirectoryPath => Path.Combine(PackageDirectoryPath, folderNames_CachedPackages);

        protected DirectoryInfo localPackageDirectory;
        public DirectoryInfo LocalPackageDirectory => localPackageDirectory;

        protected DirectoryInfo cachedPackageDirectory;
        public DirectoryInfo CachedPackageDirectory => cachedPackageDirectory;

        private bool initialized;

        public static void Initialize(bool forced = false)
        {

            var instance = Instance;
            if (instance == null) return;

            instance.InitializeLocal(forced);

        }

        public void InitializeLocal(bool forced = false)
        {

            if (initialized && !forced) return;

            void UnloadSource(ContentPackage package)
            {
                if (package == null) return;
                var sourcePackage = package.AsSourcePackage();
                if (sourcePackage != null)
                {
                    try
                    {
                        Swole.UnloadPackage(sourcePackage, out _);
                    }
                    catch (Exception ex)
                    {
                        Swole.Engine.Logger?.LogError($"Encountered an error while unloading code source from package '{package}'");
                        Swole.Engine.Logger?.LogError($"[{ex.GetType().FullName}]: {ex.Message}");
                    }
                }
            }

            foreach (var package in localPackages) UnloadSource(package.instance);
            foreach (var package in externalPackages) UnloadSource(package.instance);

            localPackages.Clear();
            externalPackages.Clear();

            localPackageDirectory = Directory.CreateDirectory(LocalPackageDirectoryPath);
            cachedPackageDirectory = Directory.CreateDirectory(CachedPackageDirectoryPath);

            var localPackageDirectories = localPackageDirectory.GetDirectories();

            foreach (var dir_ in localPackageDirectories)
            {
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
                        Swole.Engine.Logger?.LogError($"Encountered an error while evaluating a temporary package folder: '{dir.Name}'");
                        Swole.Engine.Logger?.LogError($"[{ex.GetType().FullName}]: {ex.Message}");
                    }
                }

                LoadPackage(dir, Swole.Engine.Logger);

            }


            // TODO: Load external packages


            void LoadSource(ContentPackage package)
            {
                if (package == null) return;
                var sourcePackage = package.AsSourcePackage();
                if (sourcePackage != null && !sourcePackage.IsEmpty)
                {
                    try
                    {
                        var result = Swole.LoadPackage(sourcePackage, out string resultInfo);
                        if (result != Swole.PackageActionResult.Success)
                        {
                            if (result == Swole.PackageActionResult.PackageAlreadyLoaded)
                            {
                                Swole.Engine.Logger?.LogWarning($"Tried to load code source from package '{package}' but it was already present.");
                            }
                            else
                            {
                                Swole.Engine.Logger?.LogError($"Failed to load code source from package '{package}'!");
                                Swole.Engine.Logger?.LogError($"[{result}]{(!string.IsNullOrEmpty(resultInfo) ? $": {resultInfo}" : "")}");
                            }
                        }
                    } 
                    catch(Exception ex)
                    {
                        Swole.Engine.Logger?.LogError($"Encountered an error while loading code source from package '{package}'");
                        Swole.Engine.Logger?.LogError($"[{ex.GetType().FullName}]: {ex.Message}");
                    }
                }
            }

            foreach (var package in localPackages) LoadSource(package.instance);
            foreach (var package in externalPackages) LoadSource(package.instance);

            initialized = true;

        }

        protected override void OnAwake()
        {

            base.OnAwake();

            if (Instance != this) return;

            InitializeLocal();

        }

    }

}
