using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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

    public interface IPackage : IDisposable
    {
        /// <summary>
        /// Is the package valid or has it been unloaded/disposed?
        /// </summary>
        public bool IsValid { get; }
        public PackageManifest Manifest { get; }
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
        public const string folderNames_ModelEditor = "model";

        public const string folderNames_Sessions = "sessions";
        public const string folderNames_Presets = "presets";

        public const string folderNames_Remapping = "remapping";

        public const string tags_Temporary = ".TEMP";
        public const string tags_EmbeddedPackage = "@embedded";
        public static string GetRootPathFromEmbeddedPath(string embeddedPath)
        {
            int ind = embeddedPath.IndexOf(ContentManager.tags_EmbeddedPackage);
            if (ind >= 0) embeddedPath = embeddedPath.Substring(0, ind);

            return embeddedPath;
        }

        public const string commonFiles_Manifest = "manifest.json";
        public const string commonFiles_Projects = "projects.json";

        public const string fileExtension_Generic = "swldat";
        public const string fileExtension_ZIP = "zip";
        public const string fileExtension_JSON = "json";
        public const string fileExtension_GenericImage = "image";
        public const string fileExtension_PNG = "png";
        public const string fileExtension_JPG = "jpg";


        private static readonly string[] _swoleFileExtensions = new string[]
        {
            fileExtension_Generic,
            swoleFileExtension_Package,
#region ContentTypes
            swoleFileExtension_Default,
            swoleFileExtension_Script,
            swoleFileExtension_Creation,
            swoleFileExtension_Animation,
            swoleFileExtension_GameplayExperience,
            swoleFileExtension_Image,
            swoleFileExtension_Avatar,
            swoleFileExtension_PoseRig,
            swoleFileExtension_Actor,
            swoleFileExtension_Mesh,
            swoleFileExtension_Material,
            swoleFileExtension_Model
#endregion
        };
        public static bool IsSwoleFileExtension(string ext)
        {
            if (string.IsNullOrWhiteSpace(ext)) return false;

            foreach (var swoleExt in _swoleFileExtensions) if (swoleExt.AsID() == ext.AsID()) return true;
            return false;
        }
        private static readonly string[] _swoleContentExtensions = new string[]
        {
#region ContentTypes
            swoleFileExtension_Default,
            swoleFileExtension_Script,
            swoleFileExtension_Creation,
            swoleFileExtension_Animation,
            swoleFileExtension_GameplayExperience,
            swoleFileExtension_Image,
            swoleFileExtension_Avatar,
            swoleFileExtension_PoseRig,
            swoleFileExtension_Actor,
            swoleFileExtension_Mesh,
            swoleFileExtension_Material,
            swoleFileExtension_Model
#endregion
        };
        public static bool IsSwoleContentExtension(string ext)
        {
            if (string.IsNullOrWhiteSpace(ext)) return false;

            foreach (var swoleExt in _swoleContentExtensions) if (swoleExt.AsID() == ext.AsID()) return true;
            return false;
        }

        public const string swoleFileExtension_Package = "swole";
#region ContentTypes
        public const string swoleFileExtension_Default = "swlson";
        public const string swoleFileExtension_Script = "swlscr";
        public const string swoleFileExtension_Creation = "swlobj";
        public const string swoleFileExtension_Animation = "swlani";
        public const string swoleFileExtension_GameplayExperience = "swlexp";
        public const string swoleFileExtension_Image = "swlimg";
        public const string swoleFileExtension_Avatar = "swlavt";
        public const string swoleFileExtension_PoseRig = "swlrig";
        public const string swoleFileExtension_Actor = "swlact";
        public const string swoleFileExtension_Mesh = "swlmsh";
        public const string swoleFileExtension_Material = "swlmat";
        public const string swoleFileExtension_Model = "swlmdl";
#endregion

        public static bool HasValidFileExtension(string fileName)
        {

            if (string.IsNullOrEmpty(fileName)) return false;

            if (fileName.EndsWith(fileExtension_Generic) || fileName.EndsWith(fileExtension_ZIP) || fileName.EndsWith(swoleFileExtension_Package)) return true;

#region ContentTypes
            if (fileName.EndsWith(swoleFileExtension_Default) || fileName.EndsWith(fileExtension_JSON)) return true;
            if (fileName.EndsWith(swoleFileExtension_Script)) return true;
            if (fileName.EndsWith(swoleFileExtension_Creation)) return true;
            if (fileName.EndsWith(swoleFileExtension_Animation)) return true;
            if (fileName.EndsWith(swoleFileExtension_GameplayExperience)) return true;
            if (fileName.EndsWith(swoleFileExtension_Image)) return true;
            if (fileName.EndsWith(swoleFileExtension_Avatar)) return true;
            if (fileName.EndsWith(swoleFileExtension_PoseRig)) return true;
            if (fileName.EndsWith(swoleFileExtension_Actor)) return true;
            if (fileName.EndsWith(swoleFileExtension_Mesh)) return true;
            if (fileName.EndsWith(swoleFileExtension_Material)) return true;
            if (fileName.EndsWith(swoleFileExtension_Model)) return true;
#endregion

            return false;

        }

        public static IContent LoadContent(bool addPackagesToLoadedPackages, PackageInfo packageInfo, FileInfo file, string localDir, SwoleLogger logger = null) => LoadContent(addPackagesToLoadedPackages, packageInfo, file.FullName, localDir, logger);
        public static IContent LoadContent(bool addPackagesToLoadedPackages, PackageInfo packageInfo, string path, string localDir, SwoleLogger logger = null) => LoadContentInternal(true, addPackagesToLoadedPackages, packageInfo, path, localDir, logger).GetAwaiter().GetResult();
        public static Task<IContent> LoadContentAsync(bool addPackagesToLoadedPackages, PackageInfo packageInfo, FileInfo file, string localDir, SwoleLogger logger = null) => LoadContentAsync(addPackagesToLoadedPackages, packageInfo, file.FullName, localDir, logger); 
        public static Task<IContent> LoadContentAsync(bool addPackagesToLoadedPackages, PackageInfo packageInfo, string path, string localDir, SwoleLogger logger = null) => LoadContentInternal(false, addPackagesToLoadedPackages, packageInfo, path, localDir, logger);
        async private static Task<IContent> LoadContentInternal(bool sync, bool addPackagesToLoadedPackages, PackageInfo packageInfo, string path, string localDir, SwoleLogger logger = null)
        {
            if (string.IsNullOrWhiteSpace(path) || Path.GetFileName(path).AsID() == commonFiles_Manifest.AsID()) return default;

            IContent content = default;
            if (path.EndsWith(swoleFileExtension_Package) || path.EndsWith(fileExtension_ZIP)) // File is probably an embedded package, so try to load it recursively.
            {
                ExternalPackage embeddedPackage;
                if (sync)
                {
                    embeddedPackage = LoadPackageFromRaw(addPackagesToLoadedPackages, string.Empty, path, File.ReadAllBytes(path), null, null, logger, !path.EndsWith(fileExtension_ZIP));
                }
                else
                {
                    embeddedPackage = await LoadPackageFromRawAsync(addPackagesToLoadedPackages, string.Empty, path, await File.ReadAllBytesAsync(path), null, null, logger, !path.EndsWith(fileExtension_ZIP));
                }
                if (embeddedPackage != null && embeddedPackage.IsValid)
                {
                    logger?.Log($"Loaded embedded package '{embeddedPackage.content}' from '{embeddedPackage.cachedPath}'"); 
                }
            }
            #region elseif { ContentTypes }
            else if (path.EndsWith(swoleFileExtension_Default) || path.EndsWith(fileExtension_JSON))
            {
                byte[] data = sync ? File.ReadAllBytes(path) : await File.ReadAllBytesAsync(path);

                bool flag = true;
                if (!path.EndsWith(fileExtension_JSON))
                {
                    try
                    {
                        var jd = LoadContent<JsonData>(packageInfo, data, localDir, null, null, null, logger, false);  
                        content = jd;
                        flag = !jd.hasMetadata; // (flag=True): Is likely raw json. (flag=False): Is a JsonData object
                    }
                    catch { }
                }

                if (flag) content = new JsonData(new ContentInfo() { name = Path.GetFileNameWithoutExtension(path) }, DefaultJsonSerializer.StringEncoder.GetString(data), false, packageInfo); // Treat as raw json file
            }
            else if (path.EndsWith(swoleFileExtension_Script))
            {
                byte[] data = sync ? File.ReadAllBytes(path) : await File.ReadAllBytesAsync(path);
                content = LoadContent<SourceScript>(packageInfo, data, localDir, null, null, null, logger);
            }
            else if (path.EndsWith(swoleFileExtension_Creation))
            {
                byte[] data = sync ? File.ReadAllBytes(path) : await File.ReadAllBytesAsync(path);
                content = LoadContent<Creation>(packageInfo, data, localDir, null, null, null, logger);
            }
            else if (path.EndsWith(swoleFileExtension_GameplayExperience))
            {
                byte[] data = sync ? File.ReadAllBytes(path) : await File.ReadAllBytesAsync(path);
                content = LoadContent<GameplayExperience>(packageInfo, data, localDir, null, null, null, logger);
            }
#if BULKOUT_ENV
            else if (path.EndsWith(swoleFileExtension_Animation))
            {
                byte[] data = sync ? File.ReadAllBytes(path) : await File.ReadAllBytesAsync(path);
                content = LoadContent<CustomAnimation>(packageInfo, data, localDir, null, null, null, logger);
            }
            else if (path.EndsWith(swoleFileExtension_Image))
            {
                byte[] data = sync ? File.ReadAllBytes(path) : await File.ReadAllBytesAsync(path);
                content = LoadContent<ImageAsset>(packageInfo, data, localDir, null, null, null, logger);
            }
            else if (path.EndsWith(swoleFileExtension_Avatar))
            {
                byte[] data = sync ? File.ReadAllBytes(path) : await File.ReadAllBytesAsync(path);
                content = LoadContent<CustomAvatar>(packageInfo, data, localDir, null, null, null, logger);
            }
#endif
            #endregion

            if (content != null) ContentExtensions.SetOriginPathAndUpdateRelativePath(ref content, path); 

            return content;
        }
        public static T LoadContent<T>(PackageInfo packageInfo, byte[] rawData, string localDir = null, ICollection<byte[]> files = null, ICollection<Type> fileTypes = null, ICollection<string> filePaths = null, SwoleLogger logger = null, bool printExceptions = true) where T : IContent => LoadContentInternal<T>(true, packageInfo, rawData, localDir, files, fileTypes, filePaths, logger, printExceptions).GetAwaiter().GetResult();
        public static Task<T> LoadContentAsync<T>(PackageInfo packageInfo, byte[] rawData, string localDir = null, ICollection<byte[]> files = null, ICollection<Type> fileTypes = null, ICollection<string> filePaths = null, SwoleLogger logger = null, bool printExceptions = true) where T : IContent => LoadContentInternal<T>(false, packageInfo, rawData, localDir, files, fileTypes, filePaths, logger, printExceptions);
        async private static Task<T> LoadContentInternal<T>(bool sync, PackageInfo packageInfo, byte[] rawData, string localDir = null, ICollection<byte[]> files = null, ICollection<Type> fileTypes = null, ICollection<string> filePaths = null, SwoleLogger logger = null, bool printExceptions = true) where T : IContent
        {
            if (sync)
            {
                TryLoadType(out T output, packageInfo, rawData, localDir, files, fileTypes, filePaths, logger, printExceptions);
                return output;
            }

            return (await TryLoadTypeInternal<T>(false, packageInfo, rawData, localDir, files, fileTypes, filePaths, logger, printExceptions)).output;
        }

        public struct TryLoadTypeResult<T>
        {
            public bool success;
            public T output;
        }
        public static bool TryLoadType<T>(out T output, PackageInfo packageInfo, byte[] rawData, string localDir = null, ICollection<byte[]> files = null, ICollection<Type> fileTypes = null, ICollection<string> filePaths = null, SwoleLogger logger = null, bool printExceptions = true)
        {
            var res = TryLoadTypeInternal<T>(true, packageInfo, rawData, localDir, files, fileTypes, filePaths, logger, printExceptions).GetAwaiter().GetResult();
            output = res.output;
            return res.success;
        }
        public static Task<TryLoadTypeResult<T>> TryLoadTypeAsync<T>(PackageInfo packageInfo, byte[] rawData, string localDir = null, ICollection<byte[]> files = null, ICollection<Type> fileTypes = null, ICollection<string> filePaths = null, SwoleLogger logger = null, bool printExceptions = true) => TryLoadTypeInternal<T>(false, packageInfo, rawData, localDir, files, fileTypes, filePaths, logger, printExceptions);
        async private static Task<TryLoadTypeResult<T>> TryLoadTypeInternal<T>(bool sync, PackageInfo packageInfo, byte[] rawData, string localDir = null, ICollection<byte[]> files = null, ICollection<Type> fileTypes = null, ICollection<string> filePaths = null, SwoleLogger logger = null, bool printExceptions = true)
        {
            TryLoadTypeResult<T> output = default;
            if (rawData == null) return output;

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
                        if (obj == null) return default;
                        ISerializableContainer container = (ISerializableContainer)obj;

                        #region ContentTypes

                        if (false)
                        {
                        }
#if BULKOUT_ENV
                        else if (container is ImageAsset.Serialized img)
                        {
                            if (!string.IsNullOrWhiteSpace(img.imagePath)) // image has a source uri
                            {
                                if (string.IsNullOrWhiteSpace(img.encodedImageData) && (img.imageData == null || img.imageData.Length <= 0)) // image has no data, so load it from the uri
                                {

                                    if (SwoleUtil.IsWebURL(img.imagePath))
                                    { 
                                        // path is a web url so try to download the file
                                        img.imageData = sync ? LoadAssetDependency(img.imagePath) : await LoadAssetDependencyAsync(img.imagePath); 
                                    }
                                    else
                                    {
                                        if (files != null && fileTypes != null && filePaths != null)
                                        {
                                            // Try to find the file in the current collection of loaded files
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
                                                    if (fileType == null || fileData == null || string.IsNullOrWhiteSpace(filePath)) continue;

                                                    bool isIdentical = false;
                                                    try
                                                    {
                                                        isIdentical = (Path.GetFileName(filePath).Trim() == img.imagePath.Trim()) || (Path.GetFileName(filePath).Trim() == Path.GetFileName(img.imagePath).Trim()) || SwoleUtil.IsIdenticalPath(img.imagePath, filePath); 
                                                    }
                                                    catch { } // Might throw errors when trying to convert to uri, which should be ignored in case the files came from a zip archive

                                                    if (isIdentical || (Path.GetFileName(filePath).AsID() == img.imagePath.AsID()) || (Path.GetFileName(filePath).AsID() == Path.GetFileName(img.imagePath).AsID()))
                                                    {
                                                        img.imageData = fileData;
                                                        if (isIdentical) break; // it's definitely the correct file so break out of loop
                                                    }

                                                }
                                            }
                                        }

                                        if (!string.IsNullOrWhiteSpace(localDir) && (img.imageData == null || img.imageData.Length <= 0)) // Try to find the file in the provided local directory
                                        {
                                            var dir = new DirectoryInfo(localDir);
                                            if (dir.Exists)
                                            {
                                                var path = SwoleUtil.FilePathToFileUrl(Path.Combine(dir.FullName, img.imagePath));
                                                img.imageData = sync ? LoadAssetDependency(path) : await LoadAssetDependencyAsync(path);  
                                            }
                                        }
                                    }
                                }
                            }
                            container = img;
                        }
#endif

#endregion

                        obj = container.AsNonserializableObject(packageInfo);
                        if (obj == null) return output;
                        output = new TryLoadTypeResult<T>() { output = (T)obj, success = true };

                        if (output.output is ISwoleAsset asset) asset.IsInternalAsset = false; // Allows asset to be disposed when necessary

                        return output; 
                    }
                }

                // Fallback attempt
                json = DefaultJsonSerializer.StringEncoder.GetString(rawData);
                if (string.IsNullOrEmpty(json)) return default;

                output = new TryLoadTypeResult<T>() { output = swole.Engine.FromJson<T>(json), success = true };
                return output;
                //

            }
            catch (Exception ex)
            {

                if (printExceptions)
                {
                    logger?.LogError($"Error while attempting to load data{(packageInfo.NameIsValid ? $" from package '{packageInfo.name}'" : "")} with type '{typeof(T).FullName}'");
                    logger?.LogError(ex);
                }

            }

            return default;
        }

        public const long _maxFileAssetDependencySize = 50000000;
        public const long _maxWebAssetDependencySize = 10000000;   
        public static byte[] LoadAssetDependency(string url) => LoadAssetDependencyInternal(true, url).GetAwaiter().GetResult();
        public static Task<byte[]> LoadAssetDependencyAsync(string url) => LoadAssetDependencyInternal(false, url);
        async private static Task<byte[]> LoadAssetDependencyInternal(bool sync, string requestUri)
        {
            var uri = new Uri(requestUri);
            long sizeLimit = uri.IsFile ? _maxFileAssetDependencySize : _maxWebAssetDependencySize;

            swole.Log($"{(uri.IsFile ? "Loading" : "Downloading")} resource from '{uri.AbsoluteUri}'");  
              
            if (sync)
            {

                //using (var client = new HttpClient()) 
                using (var client = new ResourceDownloader())
                {
                    //using (var s = client.GetStreamAsync(uri))
                    //{
                    using (var ms = new MemoryStream())
                    {
                        //s.Result.CopyTo(ms);
                        try
                        {
                            CancellationTokenSource cancellationToken = new CancellationTokenSource();
                            void TrackProgress(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage) 
                            {
                                if (totalFileSize.HasValue && totalFileSize.Value > sizeLimit)
                                {
                                    throw new TaskCanceledException($"Requested download had a size of {totalFileSize.Value} bytes.");
                                }

                                if (totalBytesDownloaded > sizeLimit) cancellationToken.Cancel();  
                            }
                            client.ProgressChanged += TrackProgress;
                            if (uri.IsFile)
                            {
                                client.StartCancellableDownloadURI(cancellationToken, uri, ms);
                            }
                            else
                            {
                                client.StartCancellableDownloadHTTP(cancellationToken, uri.AbsoluteUri, ms);
                            }
                        } 
                        catch(OperationCanceledException ex)
                        {
                            swole.LogError($"File exceeded size limit of {sizeLimit} bytes.");
                            swole.LogError(ex.Message);
                        }
                        return ms.ToArray(); // returns new resized array
                    } 
                    //}
                }
            }
            else
            {
                /*using var client = new HttpClient();
                using var s = await client.GetStreamAsync(uri);
                using var ms = new MemoryStream();
                await s.CopyToAsync(ms, bufferSize);
                return ms.ToArray(); // returns new resized array
                */

                using (var client = new ResourceDownloader())
                {
                    using (var ms = new MemoryStream()) 
                    {
                        try
                        {
                            CancellationTokenSource cancellationToken = new CancellationTokenSource();
                            void TrackProgress(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage)
                            {
                                if (totalFileSize.HasValue && totalFileSize.Value > sizeLimit)
                                {
                                    throw new TaskCanceledException($"Requested download had a size of {totalFileSize.Value} bytes.");
                                }

                                if (totalBytesDownloaded > sizeLimit) cancellationToken.Cancel();
                            }
                            client.ProgressChanged += TrackProgress;
                            if (uri.IsFile)
                            {
                                await client.StartCancellableDownloadURIAsync(cancellationToken, uri, ms); 
                            }
                            else
                            {
                                await client.StartCancellableDownloadHTTPAsync(cancellationToken, uri.AbsoluteUri, ms);
                            }
                        }
                        catch (OperationCanceledException ex)
                        {
                            swole.LogError($"File exceeded size limit of {sizeLimit} bytes.");
                            swole.LogError(ex.Message);
                        }
                        return ms.ToArray(); // returns new resized array
                    }
                }
            }
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
                if (content is JsonData jsonData)
                {
                    if (jsonData.hasMetadata)
                    {
                        string json = jsonData.AsJSON(true);
                        bytes = DefaultJsonSerializer.StringEncoder.GetBytes(json);
                    }
                    else
                    {
                        bytes = DefaultJsonSerializer.StringEncoder.GetBytes(string.IsNullOrWhiteSpace(jsonData.json) ? "{}" : jsonData.json);
                    }

                    fullPath = Path.Combine(directoryPath, $"{jsonData.SerializedName}.{swoleFileExtension_Default}"); 
                }
                else if (content is SourceScript script)
                {
                    string json = script.AsJSON(true);
                    bytes = DefaultJsonSerializer.StringEncoder.GetBytes(json);
                    fullPath = Path.Combine(directoryPath, $"{script.SerializedName}.{swoleFileExtension_Script}");
                }
                else if (content is Creation creation)
                {

                    string json = creation.AsJSON(true);
                    bytes = DefaultJsonSerializer.StringEncoder.GetBytes(json);
                    fullPath = Path.Combine(directoryPath, $"{creation.SerializedName}.{swoleFileExtension_Creation}");

                }
                else if (content is GameplayExperience exp)
                {

                    string json = exp.AsJSON(true);
                    bytes = DefaultJsonSerializer.StringEncoder.GetBytes(json);
                    fullPath = Path.Combine(directoryPath, $"{exp.SerializedName}.{swoleFileExtension_GameplayExperience}");

                }
                else if (content is IImageAsset img)
                {

                    string json = img.AsJSON(true);
                    bytes = DefaultJsonSerializer.StringEncoder.GetBytes(json); 
                    fullPath = Path.Combine(directoryPath, $"{img.SerializedName}.{swoleFileExtension_Image}");

                }
#if BULKOUT_ENV
                else if (content is CustomAnimation anim)
                {

                    string json = anim.AsJSON(true);
                    bytes = DefaultJsonSerializer.StringEncoder.GetBytes(json);
                    fullPath = Path.Combine(directoryPath, $"{anim.SerializedName}.{swoleFileExtension_Animation}");

                }
#endif
                #endregion

                string contentName = content.Name;
                if (bytes == null)
                {
                    string json;
                    if (content is ISwoleSerializable ss)
                    {
                        contentName = ss.SerializedName;
                        json = ss.AsJSON(true);
                    }
                    else
                    {
                        json = swole.Engine.ToJson(content, true);
                    }
                    bytes = DefaultJsonSerializer.StringEncoder.GetBytes(json);
                }

                if (string.IsNullOrEmpty(fullPath)) fullPath = Path.Combine(directoryPath, $"{contentName}.{swoleFileExtension_Default}");

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

            public PackageManifest Manifest => content == null ? default : content.Manifest;

            public PackageIdentifier GetIdentity() => content == null ? default : content.GetIdentity();
            public string GetIdentityString() => content == null ? default : content.GetIdentityString();

            public bool IsValid => content != null;
            public void Dispose()
            {
                ClearListeners();

                if (content != null)
                {
                    UnloadSource(content);
                    content.DisposeContent();
                }

                content = null;
            }
        }

        public static bool DirectoryIsPackage(DirectoryInfo directory) => DirectoryIsPackage(directory == null ? string.Empty : directory.FullName);
        public static bool DirectoryIsPackage(string directory)
        {
            if (string.IsNullOrEmpty(directory)) return false;
            return File.Exists(Path.Combine(directory, commonFiles_Manifest));
        }

        public static void LoadContent(bool addPackagesToLoadedPackages, DirectoryInfo dir, PackageManifest manifest, List<IContent> content, SwoleLogger logger = null) => LoadContentInternal(true, addPackagesToLoadedPackages, dir, manifest, content, logger).GetAwaiter().GetResult();
        public static Task LoadContentAsync(bool addPackagesToLoadedPackages, DirectoryInfo dir, PackageManifest manifest, List<IContent> content, SwoleLogger logger = null) => LoadContentInternal(false, addPackagesToLoadedPackages, dir, manifest, content, logger);
        async private static Task LoadContentInternal(bool sync, bool addPackagesToLoadedPackages, DirectoryInfo dir, PackageManifest manifest, List<IContent> content, SwoleLogger logger = null)
        {
            var files = dir.EnumerateFiles("*", SearchOption.TopDirectoryOnly).Where(i => HasValidFileExtension(i.Name));
            foreach (var file in files)
            {
                if (file.Name.AsID() == commonFiles_Manifest.AsID()) continue;

                var contentObj = sync ? LoadContent(addPackagesToLoadedPackages, manifest, file, dir.FullName, logger) : await LoadContentAsync(addPackagesToLoadedPackages, manifest, file, dir.FullName, logger);
                if (contentObj == null) continue;
                content.Add(contentObj);
            }
            var dirs = dir.EnumerateDirectories("*", SearchOption.TopDirectoryOnly);
            foreach (var subDir in dirs)
            {
                if (DirectoryIsPackage(subDir)) continue; // Directory is its own package so ignore it
                if (sync) LoadContent(addPackagesToLoadedPackages, subDir, manifest, content, logger); else await LoadContentAsync(addPackagesToLoadedPackages, subDir, manifest, content, logger);
            }
        }

        /// <summary>
        /// Loads a package locally by name and version.
        /// </summary>
        public static LocalPackage LoadLocalPackage(bool addToLoadedPackages, PackageIdentifier identity, SwoleLogger logger = null) => LoadLocalPackage(addToLoadedPackages, identity.name, identity.version, logger);
        /// <summary>
        /// Asynchronously loads a package locally by name and version.
        /// </summary>
        public static Task<LocalPackage> LoadLocalPackageAsync(bool addToLoadedPackages, PackageIdentifier identity, SwoleLogger logger = null) => LoadLocalPackageAsync(addToLoadedPackages, identity.name, identity.version, logger);
        /// <summary>
        /// Loads a package locally by name and version.
        /// </summary>
        public static LocalPackage LoadLocalPackage(bool addToLoadedPackages, string packageName, string version, SwoleLogger logger = null) => LoadPackage(addToLoadedPackages, Path.Combine(LocalPackageDirectoryPath, SwoleScriptSemantics.GetFullPackageString(packageName, version)), logger);
        /// <summary>
        /// Asynchronously loads a package locally by name and version.
        /// </summary>
        public static Task<LocalPackage> LoadLocalPackageAsync(bool addToLoadedPackages, string packageName, string version, SwoleLogger logger = null) => LoadPackageAsync(addToLoadedPackages, Path.Combine(LocalPackageDirectoryPath, SwoleScriptSemantics.GetFullPackageString(packageName, version)), logger);
        /// <summary>
        /// Loads a package locally from a folder.
        /// </summary>
        public static LocalPackage LoadPackage(bool addToLoadedPackages, string directoryPath, SwoleLogger logger = null) => LoadPackage(addToLoadedPackages, new DirectoryInfo(directoryPath), logger);
        /// <summary>
        /// Asynchronously loads a package locally from a folder.
        /// </summary>
        public static Task<LocalPackage> LoadPackageAsync(bool addToLoadedPackages, string directoryPath, SwoleLogger logger = null) => LoadPackageAsync(addToLoadedPackages, new DirectoryInfo(directoryPath), logger);
        /// <summary>
        /// Loads a package locally from a folder.
        /// </summary>
        public static LocalPackage LoadPackage(bool addToLoadedPackages, DirectoryInfo packageDirectory, SwoleLogger logger = null) => LoadPackageInternal(true, addToLoadedPackages, packageDirectory, logger).GetAwaiter().GetResult();
        /// <summary>
        /// Asynchronously loads a package locally from a folder.
        /// </summary>
        public static Task<LocalPackage> LoadPackageAsync(bool addToLoadedPackages, DirectoryInfo packageDirectory, SwoleLogger logger = null) => LoadPackageInternal(false, addToLoadedPackages, packageDirectory, logger);
        async private static Task<LocalPackage> LoadPackageInternal(bool sync, bool addToLoadedPackages, DirectoryInfo packageDirectory, SwoleLogger logger = null)
        {
            if (packageDirectory == null) return default;
            if (!packageDirectory.Exists)
            {
                logger?.LogError($"Tried to load local package from '{packageDirectory.FullName}' — but the directory does not exist!");
                return default;
            }
            var instance = Instance;
            if (instance == null) return default;

            if (addToLoadedPackages)
            {
                for (int a = 0; a < instance.localPackages.Count; a++) if (instance.localPackages[a].workingDirectory.FullName == packageDirectory.FullName)
                    {
                        logger?.LogWarning($"Tried to load local package from '{packageDirectory.FullName}' — but it was already loaded.");
                        return default;// instance.localPackages[a]; 
                    }
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
            if (sync) LoadContent(addToLoadedPackages, packageDirectory, manifest, content, logger); else await LoadContentAsync(addToLoadedPackages, packageDirectory, manifest, content, logger);
            package.Content = new ContentPackage(manifest, content, true);
            //

            if (addToLoadedPackages) AddLocalPackage(package);

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
        public static ExternalPackage LoadPackage(bool addToLoadedPackages, string sourcePath, string cachedPath = null, SwoleLogger logger = null, bool packageExpected = true, IList<ExternalPackage> outputList = null) => LoadPackageInternal(true, addToLoadedPackages, sourcePath, cachedPath, logger, packageExpected, outputList).GetAwaiter().GetResult();
        /// <summary>
        /// Asynchronously loads a package externally from a zip archive.
        /// </summary>
        public static Task<ExternalPackage> LoadPackageAsync(bool addToLoadedPackages, string sourcePath, string cachedPath = null, SwoleLogger logger = null, bool packageExpected = true, IList<ExternalPackage> outputList = null) => LoadPackageInternal(false, addToLoadedPackages, sourcePath, cachedPath, logger, packageExpected, outputList);
        async private static Task<ExternalPackage> LoadPackageInternal(bool sync, bool addToLoadedPackages, string sourcePath, string cachedPath = null, SwoleLogger logger = null, bool packageExpected = true, IList<ExternalPackage> outputList = null)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) && string.IsNullOrWhiteSpace(cachedPath)) return default;
            var instance = Instance;
            if (instance == null) return default;

            List<EngineHook.FileDescr> fileDescs = null;
            List<byte[]> fileData = null;
            if (string.IsNullOrWhiteSpace(cachedPath))
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

            return sync ? LoadPackage(addToLoadedPackages, sourcePath, cachedPath, fileDescs, fileData, logger, packageExpected, outputList) : await LoadPackageAsync(addToLoadedPackages, sourcePath, cachedPath, fileDescs, fileData, logger, packageExpected, outputList);
        }
        /// <summary>
        /// Loads a package externally from data in memory.
        /// </summary>
        public static ExternalPackage LoadPackage(bool addToLoadedPackages, string sourcePath, string cachedPath, ICollection<EngineHook.FileDescr> fileDescs, ICollection<byte[]> fileData, SwoleLogger logger = null, bool packageExpected = true, IList<ExternalPackage> outputList = null) => LoadPackageInternal(true, addToLoadedPackages, sourcePath, cachedPath, fileDescs, fileData, logger, packageExpected, outputList).GetAwaiter().GetResult();
        /// <summary>
        /// Asynchronously loads a package externally from data in memory.
        /// </summary>
        public static Task<ExternalPackage> LoadPackageAsync(bool addToLoadedPackages, string sourcePath, string cachedPath, ICollection<EngineHook.FileDescr> fileDescs, ICollection<byte[]> fileData, SwoleLogger logger = null, bool packageExpected = true, IList<ExternalPackage> outputList = null) => LoadPackageInternal(false, addToLoadedPackages, sourcePath, cachedPath, fileDescs, fileData, logger, packageExpected, outputList);
        async private static Task<ExternalPackage> LoadPackageInternal(bool sync, bool addToLoadedPackages, string sourcePath, string cachedPath, ICollection<EngineHook.FileDescr> fileDescs, ICollection<byte[]> fileData, SwoleLogger logger = null, bool packageExpected = true, IList<ExternalPackage> outputList = null)
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
                        else if (fileDesc.fileName.EndsWith(fileExtension_ZIP) || fileDesc.fileName.EndsWith(swoleFileExtension_Package))  // File is likely an embedded package
                        {
                            fileTypes[index] = typeof(ExternalPackage);
                        }
                        #region elseif { ContentTypes }
                        else if (fileDesc.fileName.EndsWith(swoleFileExtension_Default) || fileDesc.fileName.EndsWith(fileExtension_JSON))
                        {
                            fileTypes[index] = typeof(JsonData);
                        }
                        else if (fileDesc.fileName.EndsWith(swoleFileExtension_Script))
                        {
                            fileTypes[index] = typeof(SourceScript);
                        }
                        else if (fileDesc.fileName.EndsWith(swoleFileExtension_Creation))
                        {
                            fileTypes[index] = typeof(Creation);
                        }

                        else if (fileDesc.fileName.EndsWith(swoleFileExtension_GameplayExperience))
                        {
                            fileTypes[index] = typeof(GameplayExperience);
                        }
#if BULKOUT_ENV
                        else if (fileDesc.fileName.EndsWith(swoleFileExtension_Animation))
                        {
                            fileTypes[index] = typeof(CustomAnimation);
                        }
                        else if (fileDesc.fileName.EndsWith(swoleFileExtension_Image))
                        {
                            fileTypes[index] = typeof(ImageAsset);
                        }
#endif
                        #endregion
#if BULKOUT_ENV
                        else if (fileDesc.fileName.EndsWith(fileExtension_PNG) || fileDesc.fileName.EndsWith(fileExtension_JPG) || fileDesc.fileName.EndsWith(fileExtension_GenericImage))  // File is an image
                        {
                            fileTypes[index] = typeof(UnityEngine.Texture2D);
                        }
#endif
                    }
                }
            }

            return sync ? LoadPackage(addToLoadedPackages, sourcePath, cachedPath, manifestFile, fileData, fileTypes, logger, packageExpected, string.Empty, filePaths, outputList) : await LoadPackageAsync(addToLoadedPackages, sourcePath, cachedPath, manifestFile, fileData, fileTypes, logger, packageExpected, string.Empty, filePaths, outputList);
        }

        /// <summary>
        /// Loads a package externally from data in memory.
        /// </summary>
        public static ExternalPackage LoadPackageFromRaw(bool addToLoadedPackages, string sourcePath, string cachedPath, byte[] packageFileRaw, List<EngineHook.FileDescr> outFileDescs = null, List<byte[]> outFileData = null, SwoleLogger logger = null, bool packageExpected = true, IList<ExternalPackage> outputList = null) => LoadPackageFromRawInternal(true, addToLoadedPackages, sourcePath, cachedPath, packageFileRaw, outFileDescs, outFileData, logger, packageExpected, outputList).GetAwaiter().GetResult();
        /// <summary>
        /// Asynchronously loads a package externally from data in memory.
        /// </summary>
        public static Task<ExternalPackage> LoadPackageFromRawAsync(bool addToLoadedPackages, string sourcePath, string cachedPath, byte[] packageFileRaw, List<EngineHook.FileDescr> outFileDescs = null, List<byte[]> outFileData = null, SwoleLogger logger = null, bool packageExpected = true, IList<ExternalPackage> outputList = null) => LoadPackageFromRawInternal(false, addToLoadedPackages, sourcePath, cachedPath, packageFileRaw, outFileDescs, outFileData, logger, packageExpected, outputList);
        async private static Task<ExternalPackage> LoadPackageFromRawInternal(bool sync, bool addToLoadedPackages, string sourcePath, string cachedPath, byte[] packageFileRaw, List<EngineHook.FileDescr> outFileDescs = null, List<byte[]> outFileData = null, SwoleLogger logger = null, bool packageExpected = true, IList<ExternalPackage> outputList = null)
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
                return LoadPackage(addToLoadedPackages, sourcePath, cachedPath, tempFileDescs, tempFileData, logger, packageExpected, outputList);
            }
            
            void Decompress() => swole.Engine.DecompressZIP(packageFileRaw, ref tempFileDescs, ref tempFileData);
            await Task.Run(Decompress);
            return await LoadPackageAsync(addToLoadedPackages, sourcePath, cachedPath, tempFileDescs, tempFileData, logger, packageExpected, outputList);
        }

        /// <summary>
        /// Loads a package externally from data in memory.
        /// </summary>
        public static ExternalPackage LoadPackage(bool addToLoadedPackages, string sourcePath, string cachedPath, byte[] manifestFile, ICollection<byte[]> files, ICollection<Type> fileTypes, SwoleLogger logger = null, bool packageExpected = true, string rootFolderName = null, ICollection<string> filePaths = null, IList<ExternalPackage> outputList = null) => LoadPackageInternal(true, addToLoadedPackages, sourcePath, cachedPath, manifestFile, files, fileTypes, logger, packageExpected, rootFolderName, filePaths, outputList).GetAwaiter().GetResult();
        /// <summary>
        /// Asynchronously loads a package externally from data in memory.
        /// </summary>
        public static Task<ExternalPackage> LoadPackageAsync(bool addToLoadedPackages, string sourcePath, string cachedPath, byte[] manifestFile, ICollection<byte[]> files, ICollection<Type> fileTypes, SwoleLogger logger = null, bool packageExpected = true, string rootFolderName = null, ICollection<string> filePaths = null, IList<ExternalPackage> outputList = null) => LoadPackageInternal(false, addToLoadedPackages, sourcePath, cachedPath, manifestFile, files, fileTypes, logger, packageExpected, rootFolderName, filePaths, outputList);
        async private static Task<ExternalPackage> LoadPackageInternal(bool sync, bool addToLoadedPackages, string sourcePath, string cachedPath, byte[] manifestFile, ICollection<byte[]> files, ICollection<Type> fileTypes, SwoleLogger logger = null, bool packageExpected = true, string rootFolderName = null, ICollection<string> filePaths = null, IList<ExternalPackage> outputList = null)
        {
            if (files == null || fileTypes == null) return default;
            var instance = Instance;
            if (instance == null) return default;

            ExternalPackage package = new ExternalPackage();
            package.cachedPath = cachedPath;

            bool notAPackage = false;
            PackageManifest manifest = default;
            if (manifestFile != null)
            {
                manifest = PackageManifest.FromRaw(manifestFile);
            }
            else
            {
                notAPackage = true;
                if (packageExpected)
                {
                    logger?.LogError($"Error loading external package from '{(string.IsNullOrEmpty(cachedPath) ? sourcePath : cachedPath)}' — Reason: No package manifest was provided!");
                    return default;
                }
            }

            if (!notAPackage)
            {
                if (!manifest.NameIsValid)
                {
                    logger?.LogError($"Error loading external package from '{(string.IsNullOrEmpty(cachedPath) ? sourcePath : cachedPath)}' — Reason: Name in package manifest was invalid!");
                    return default;
                }

                if (addToLoadedPackages)
                {
                    for (int a = 0; a < instance.externalPackages.Count; a++) if (instance.externalPackages[a] != null && instance.externalPackages[a].IsValid && instance.externalPackages[a].content.GetIdentityString() == manifest.GetIdentityString())
                        {
                            logger?.LogWarning($"Tried to load external package '{manifest}' from '{(string.IsNullOrEmpty(cachedPath) ? sourcePath : cachedPath)}' — but it was already loaded.");
                            return default;// instance.externalPackages[a];
                        }
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
            }

            List<EngineHook.FileDescr> embeddedPackageFileDescs = null;
            List<byte[]> embeddedPackageFileData = null;
            // > Load valid files and ignore the rest
            List<IContent> content = notAPackage ? null : new List<IContent>();
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

                    bool hasPath = false;
                    if (!string.IsNullOrWhiteSpace(filePath))
                    {
                        hasPath = true;

                        if (Path.GetFileName(filePath).AsID() == commonFiles_Manifest.AsID()) continue;
                    }

                    IContent contentObj = null;
                    if (fileType == typeof(ExternalPackage)) // File is likely an embedded package, so try to load it recursively.
                    {
                        ExternalPackage embeddedPackage;
                        if (sync)
                        {
                            embeddedPackage = LoadPackageFromRaw(addToLoadedPackages, string.Empty, $"{cachedPath}{tags_EmbeddedPackage}", fileData, embeddedPackageFileDescs, embeddedPackageFileData, logger, false, outputList);
                        }
                        else
                        {
                            embeddedPackage = await LoadPackageFromRawAsync(addToLoadedPackages, string.Empty, $"{cachedPath}{tags_EmbeddedPackage}", fileData, embeddedPackageFileDescs, embeddedPackageFileData, logger, false, outputList);
                        }
                        if (embeddedPackage != null && embeddedPackage.IsValid)
                        { 
                            logger?.Log($"Loaded embedded package '{embeddedPackage.content}' from '{embeddedPackage.cachedPath}'"); 
                        }
                    } 
                    else if (notAPackage)
                    {
                        continue;
                    }
                    #region elseif { ContentTypes }
                    else if (fileType == typeof(JsonData))
                    {
                        bool flag = true;
                        if (!hasPath || !filePath.EndsWith(fileExtension_JSON))
                        {
                            try
                            {
                                var jd = LoadContent<JsonData>(manifest, fileData, null, files, fileTypes, filePaths, logger, false);
                                contentObj = jd;
                                flag = !jd.hasMetadata;
                            }
                            catch { }
                        }

                        if (flag) contentObj = new JsonData(new ContentInfo() { name = hasPath ? Path.GetFileNameWithoutExtension(filePath) : string.Empty }, DefaultJsonSerializer.StringEncoder.GetString(fileData), false, manifest);  
                    }
                    else if (fileType == typeof(SourceScript))
                    {
                        contentObj = LoadContent<SourceScript>(manifest, fileData, null, files, fileTypes, filePaths, logger);
                    }
                    else if (fileType == typeof(Creation))
                    {
                        contentObj = LoadContent<Creation>(manifest, fileData, null, files, fileTypes, filePaths, logger);
                    }
                    else if (fileType == typeof(GameplayExperience))
                    {
                        contentObj = LoadContent<GameplayExperience>(manifest, fileData, null, files, fileTypes, filePaths, logger);
                    }
#if BULKOUT_ENV
                    else if (fileType == typeof(CustomAnimation))
                    {
                        contentObj = LoadContent<CustomAnimation>(manifest, fileData, null, files, fileTypes, filePaths, logger);
                    }
                    else if (fileType == typeof(ImageAsset))
                    {
                        contentObj = LoadContent<ImageAsset>(manifest, fileData, null, files, fileTypes, filePaths, logger); 
                    }
#endif
                    #endregion

                    if (contentObj == null) continue;

                    if (!string.IsNullOrEmpty(filePath)) ContentExtensions.SetRelativePath(ref contentObj, ContentExtensions.GetRelativePathFromOriginPath(rootFolderName, filePath).NormalizeDirectorySeparators());
                    contentObj = contentObj.SetOriginPath($"{cachedPath}@{Path.DirectorySeparatorChar}{filePath}".NormalizeDirectorySeparators());

                    content.Add(contentObj);
                }
            }

            if (notAPackage) return default; 

            package.content = new ContentPackage(manifest, content, true);
            // <

            if (addToLoadedPackages) AddExternalPackage(package);
            if (outputList != null) outputList.Add(package);

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
                var tempDir = Directory.CreateDirectory(tempPath);

                // Save package manifest
                string manifestPath = Path.Combine(tempDir.FullName, commonFiles_Manifest);
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

                        bool result = sync ? SaveContent(tempDir, content, logger) : await SaveContentAsync(tempDir, content, logger);
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
                    // Copy non content data into new dir
                    SwoleUtil.CopyAll(path, tempPath, false, _swoleContentExtensions);   
                    //
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

                package.DisposeOrphanedContent();

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
        public class ExternalPackage : IPackage
        {

            public string sourcePath;
            public string cachedPath;

            public bool isNativeContent;

            public ContentPackage content;
            public ContentPackage Content => content;

            public static implicit operator ContentPackage(ExternalPackage pkg) => pkg.content;

            public PackageManifest Manifest => content == null ? default : content.Manifest;

            public PackageIdentifier GetIdentity() => content == null ? default : content.GetIdentity();
            public string GetIdentityString() => content == null ? default : content.GetIdentityString();

            public bool IsValid => content != null;
            public void Dispose()
            {
                if (content != null)
                {
                    UnloadSource(content);
                    content.DisposeContent();
                }

                content = null;
            }
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
            if (FindExternalPackage(package.content.GetIdentity()) != null) return false;
            instance.externalPackages.RemoveAll(i => i.GetIdentity() == package.GetIdentity()); 
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
            if (latestPackage != null && latestPackage.IsValid) return latestPackage; 

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
            return package != null && package.IsValid;
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

        public static string AssetStreamingPath
        {
            get
            {
#if BULKOUT_ENV
                return UnityEngine.Application.streamingAssetsPath;
#else
                return null;
#endif
            }
        }
        public static string EmbeddedPackageDirectoryPath => Path.Combine(AssetStreamingPath, "EmbeddedPackages");    

        protected DirectoryInfo localPackageDirectory;
        public DirectoryInfo LocalPackageDirectory => localPackageDirectory;

        protected DirectoryInfo cachedPackageDirectory;
        public DirectoryInfo CachedPackageDirectory => cachedPackageDirectory;

        private static void UnloadSource(ContentPackage package)
        {
            if (package == null) return;

            var sourcePackage = package.CachedSourcePackage;
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

        private static void UnloadPackage(IPackage package)
        {
            if (package == null) return;

            package.Dispose();
        }

        /// <summary>
        /// Load packages in the form of directories from the local file system.
        /// </summary>
        public static void LoadPackagesLocally(string directory, bool addToLoadedPackages, IList<LocalPackage> outputList = null) => LoadPackagesLocally(new DirectoryInfo(directory), addToLoadedPackages, outputList);
        /// <summary>
        /// Load packages in the form of directories from the local file system.
        /// </summary>
        public static void LoadPackagesLocally(DirectoryInfo directory, bool addToLoadedPackages, IList<LocalPackage> outputList = null)
        {
            if (directory == null || !directory.Exists) return;

            var localPackageDirectories = directory.GetDirectories("*", SearchOption.TopDirectoryOnly);
            foreach (var dir_ in localPackageDirectories)
            {         
                if (!DirectoryIsPackage(dir_)) // Directory is not a package so check for packages stored inside of it instead
                {
                    LoadPackagesLocally(dir_, addToLoadedPackages, outputList);
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

                var pkg = LoadPackage(addToLoadedPackages, dir, swole.DefaultLogger);
                if (!string.IsNullOrWhiteSpace(pkg.GetIdentityString()) && outputList != null) outputList.Add(pkg);
            }
        }

        /// <summary>
        /// Load packages in the form of zip archives from the local file system.
        /// </summary>
        public static void LoadPackagesExternally(string directory, bool addToLoadedPackages, IList<ExternalPackage> outputList = null) => LoadPackagesExternally(new DirectoryInfo(directory), addToLoadedPackages, outputList);
        /// <summary>
        /// Load packages in the form of zip archives from the local file system.
        /// </summary>
        public static void LoadPackagesExternally(DirectoryInfo directory, bool addToLoadedPackages, IList<ExternalPackage> outputList = null)
        {
            if (directory == null || !directory.Exists) return;

            var files = directory.EnumerateFiles("*", SearchOption.AllDirectories); 
            foreach (var file in files)
            {
                string nameLower = file.Name.AsID();
                if (!nameLower.EndsWith(fileExtension_ZIP) && !nameLower.EndsWith(swoleFileExtension_Package)) continue;
                LoadPackage(addToLoadedPackages, string.Empty, file.FullName, swole.DefaultLogger, false, outputList);  
            }
        }

        private void ReloadLocalPackagesLocal()
        {
            foreach (var package in localPackages) UnloadPackage(package);
            localPackages.Clear();

            localPackageDirectory = Directory.CreateDirectory(LocalPackageDirectoryPath);
            LoadPackagesLocally(localPackageDirectory, true);

            ReloadProjectInfo();
            if (projects != null)
            {
                void LoadDirectoryIfNotLocal(string projectName, string path)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(path) || !SwoleUtil.IsValidPath(path) || SwoleUtil.IsSubPathOf(path, localPackageDirectory.FullName)) return;
                        LoadPackagesLocally(path, true);
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

        private readonly List<ExternalPackage> tempEmbeddedPackages = new List<ExternalPackage>();
        private void ReloadExternalPackagesLocal()
        {
            foreach (var package in externalPackages) UnloadPackage(package);
            externalPackages.Clear();

            localPackageDirectory = Directory.CreateDirectory(LocalPackageDirectoryPath);
            LoadPackagesExternally(localPackageDirectory, true); // Load zip archive packages from local directory

            cachedPackageDirectory = Directory.CreateDirectory(CachedPackageDirectoryPath);
            LoadPackagesExternally(cachedPackageDirectory, true); // Load zip archive packages from cache directory

#if BULKOUT_ENV
            tempEmbeddedPackages.Clear();
            LoadPackagesExternally(EmbeddedPackageDirectoryPath, false, tempEmbeddedPackages);
            for(int a = 0; a < tempEmbeddedPackages.Count; a++)
            {
                var pkg = tempEmbeddedPackages[a];
                pkg.isNativeContent = true;
                AddExternalPackage(pkg); 
            }
            tempEmbeddedPackages.Clear();
#endif
        }
        public void ReloadAllPackagesLocal()
        {

            foreach (var package in localPackages) UnloadPackage(package);
            localPackages.Clear();
            foreach (var package in externalPackages) UnloadPackage(package);
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

        private List<Project> projects = new List<Project>();
        private List<Project> Projects
        {
            get
            {
                if (projects == null) projects = new List<Project>();
                return projects; 
            }
        }
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
                return new SerializableProjectInfo() { projects = instance.Projects.ToArray(), identifiers = array };
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

            instance.Projects.Clear();
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
                instance.Projects.Add(project);
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
                if (instance.projects != null)
                {
                    for (int a = 0; a < instance.projects.Count; a++)
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
                }
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
                    foreach(var mem in _tempLocalPackageList) if (mem != null && mem.Content != null && mem.Content.GetIdentityString() == pkg.Content.GetIdentityString())
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

#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying) return;  
#endif

            InitializeLocal();

        }

    }

}
