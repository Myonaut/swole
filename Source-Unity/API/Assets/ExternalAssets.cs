#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Experimental.Rendering;

#if BULKOUT_ENV
using TriLibCore; // Paid Asset Integration https://assetstore.unity.com/packages/tools/modeling/trilib-2-model-loading-package-157548
using TriLibCore.General;
using TriLibCore.SFB;
using TriLibCore.Extensions;
#endif

namespace Swole.API.Unity {

    public static class ExternalAssets
    {

        public interface ILoaderOptions
        {
            public object Options { get; }

            public bool DestroyOnError { get; set; }

            public int Timeout { get; set; }

        }

#if BULKOUT_ENV
        public interface ILoaderContext : TriLibCore.Interfaces.IAssetLoaderContext
#else
    public interface ILoaderContext
#endif
        {
            public ILoaderOptions Options { get; set; }
            public string Filename { get; set; }
            public string BasePath { get; set; }
            public string FileExtension { get; set; }
            public Stream Stream { get; set; }
            public Action<ILoaderContext> OnLoad { get; set; }
            public Action<ILoaderContext, float> OnProgress { get; set; }
            public Action<ILoaderError> HandleError { get; set; }
            public Action<ILoaderError> OnError { get; set; }
            public Action<ILoaderContext> OnPreLoad { get; set; }
            public object CustomData { get; set; }
            public bool HaltTasks { get; set; }
            public GameObject RootGameObject { get; set; }
            public void Setup();
        }

        private static void Cleanup(ILoaderContext assetLoaderContext)
        {
            //if (assetLoaderContext.Stream != null && assetLoaderContext.Options.CloseStreamAutomatically)
            //{
            //    assetLoaderContext.Stream.TryToDispose();
            //}
            //GC.Collect();
            //GC.WaitForPendingFinalizers();
            //GC.Collect();
        }

#if BULKOUT_ENV
        public interface ILoaderError : TriLibCore.Interfaces.IAssetLoaderContext
#else
    public interface ILoaderError
#endif
        {
            object GetContext();
            Exception GetInnerException();
        }

#if BULKOUT_ENV
        private static void Rethrow<T>(ContextualizedError<T> contextualizedError)
        {
            throw contextualizedError;
        }
#endif

#if BULKOUT_ENV
        private static void HandleContextualizedError(IContextualizedError error)
        {
            if (error is ILoaderError loaderError) HandleLoaderError(loaderError);
        }
#endif
        private static void HandleLoaderError(ILoaderError error)
        {
            var exception = error.GetInnerException();
            if (error.GetContext() is ILoaderContext assetLoaderContext)
            {
                Cleanup(assetLoaderContext);
                if (assetLoaderContext.Options.DestroyOnError && assetLoaderContext.RootGameObject != null)
                {
                    if (!Application.isPlaying)
                    {
                        GameObject.DestroyImmediate(assetLoaderContext.RootGameObject);
                    }
                    else
                    {
                        GameObject.Destroy(assetLoaderContext.RootGameObject);
                    }
                    assetLoaderContext.RootGameObject = null;
                }
                if (assetLoaderContext.OnError != null)
                {
#if BULKOUT_ENV
                    TriLibCore.Utils.Dispatcher.InvokeAsync(assetLoaderContext.OnError, error);
#endif
                }
            }
            else
            {
#if BULKOUT_ENV
                var contextualizedError = new ContextualizedError<object>(exception, null);
                TriLibCore.Utils.Dispatcher.InvokeAsync(Rethrow, contextualizedError);
#endif
            }
        }

        public abstract class IOLoader : MonoBehaviour, ILoader
        {
            protected bool AutoDestroy;

            protected Action<ILoaderContext> OnLoad;
            protected Action<ILoaderContext, float> OnProgress;
            protected Action<ILoaderError> OnError;
            protected Action<bool> OnBeginLoad;
            protected GameObject WrapperGameObject;
            protected ILoaderOptions AssetLoaderOptions;
            protected bool HaltTask;

#if BULKOUT_ENV
            private IList<TriLibCore.SFB.ItemWithStream> _items;
#endif
            private string _assetExtension;

            protected virtual void DestroyMe()
            {
                Destroy(gameObject);
            }

            protected virtual void HandleFileLoading()
            {
                StartCoroutine(DoHandleFileLoading());
            }

            protected virtual IEnumerator DoHandleFileLoading()
            {
#if BULKOUT_ENV
                var hasFiles = _items != null && _items.Count > 0;// && _items[0].HasData;
#else
            var hasFiles = false;
#endif
                OnBeginLoad?.Invoke(hasFiles);
                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();
                if (!hasFiles)
                {
                    if (AutoDestroy)
                    {
                        DestroyMe(); 
                    }
                    yield break;
                }

                int i = -1;
                while (true)
                {
#if BULKOUT_ENV
                    var assetFileWithStream = FindNextAssetFile(i + 1, out i);
                    if (assetFileWithStream == null) break;
                    //_items.Remove(assetFileWithStream);

                    var assetFilename = assetFileWithStream.Name;
                    var assetStream = assetFileWithStream.OpenStream();

                    _assetExtension = assetFilename != null ? TriLibCore.Utils.FileUtils.GetFileExtension(assetFilename, false) : null;
#else
            string assetFilename = string.Empty;
            Stream assetStream = null;
#endif

                    if (assetStream != null)
                    {
                        //AssetLoader.LoadModelFromStream(assetStream, assetFilename, _modelExtension, OnLoad, OnMaterialsLoad, OnProgress, OnError, WrapperGameObject, AssetLoaderOptions, CustomDataHelper.CreateCustomDataDictionaryWithData(_items), HaltTask);
                        LoadAssetFromStream(_assetExtension, assetFilename, assetStream);
                    }
                    else
                    {
                        //AssetLoader.LoadModelFromFile(assetFilename, OnLoad, OnMaterialsLoad, OnProgress, OnError, WrapperGameObject, AssetLoaderOptions, CustomDataHelper.CreateCustomDataDictionaryWithData(_items), HaltTask);
                        LoadAssetFromFile(assetFilename);
                    }
                }


                if (AutoDestroy)
                {
                    DestroyMe();
                }
            }
            protected abstract void LoadAssetFromStream(string extension, string assetFilename, Stream assetStream);
            protected abstract void LoadAssetFromFile(string filePath);

#if BULKOUT_ENV
            private TriLibCore.SFB.ItemWithStream FindNextAssetFile(int startIndex, out int index)
            {
                index = -1;

                var extensions = Extensions;
                for (var i = startIndex; i < _items.Count; i++)
                {
                    var item = _items[i];
                    if (item.Name == null)
                    {
                        continue;
                    }

                    var extension = TriLibCore.Utils.FileUtils.GetFileExtension(item.Name, false);
                    if (extensions.Contains(extension))
                    {
                        index = i;
                        return item;
                    }
                }

                return null;
            }
#endif

            public abstract IList<string> Extensions { get; }

#if BULKOUT_ENV
            protected ExtensionFilter[] GetExtensions()
            {
                var extensions = Extensions;
                var extensionFilters = new List<ExtensionFilter>();
                var subExtensions = new List<string>();
                for (var i = 0; i < extensions.Count; i++)
                {
                    var extension = extensions[i];
                    extensionFilters.Add(new ExtensionFilter(null, extension));
                    subExtensions.Add(extension);
                }

                //subExtensions.Add("zip");
                //extensionFilters.Add(new ExtensionFilter(null, new[] { "zip" }));
                extensionFilters.Add(new ExtensionFilter("All Files", new[] { "*" }));
                extensionFilters.Insert(0, new ExtensionFilter("Accepted Files", subExtensions.ToArray()));
                return extensionFilters.ToArray();
            }


            protected virtual void OnItemsWithStreamSelected(IList<TriLibCore.SFB.ItemWithStream> itemsWithStream)
            {
                if (itemsWithStream != null)
                {
                    _items = itemsWithStream;
                    TriLibCore.Utils.Dispatcher.InvokeAsync(HandleFileLoading);
                }
                else
                {
                    if (AutoDestroy)
                    {
                        DestroyMe();
                    }
                }
            }
#endif

            public abstract bool IsValid { get; }
            public abstract void Dispose();
        }

        public interface ILoader : IDisposable
        {
            public bool IsValid { get; }
        }

        public const string fileExtension_JPG = "jpg";
        public const string fileExtension_PNG = "png";
        public static readonly List<string> _imageFileExtensions = new List<string>() { fileExtension_JPG, fileExtension_PNG };
#if BULKOUT_ENV
        public static ReaderBase GetImageFileReader(string fileExtension)
        {
            if (fileExtension.AsID() == fileExtension_JPG.AsID() || fileExtension.AsID() == fileExtension_PNG.AsID())
            {
                return new Unity_JPG_PNG_Reader();
            }

            return null;
        }

        public struct LoadedBytes : TriLibCore.Interfaces.IRootModel
        {
            public List<TriLibCore.Interfaces.IModel> AllModels { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public List<TriLibCore.Interfaces.IGeometryGroup> AllGeometryGroups { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public List<TriLibCore.Interfaces.IAnimation> AllAnimations { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public List<TriLibCore.Interfaces.IMaterial> AllMaterials { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public List<TriLibCore.Interfaces.ITexture> AllTextures { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public List<TriLibCore.Interfaces.ICamera> AllCameras { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public List<TriLibCore.Interfaces.ILight> AllLights { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public Vector3 Pivot { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public Vector3 LocalPosition { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public Quaternion LocalRotation { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public Vector3 LocalScale { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public bool Visibility { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public TriLibCore.Interfaces.IModel Parent { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public List<TriLibCore.Interfaces.IModel> Children { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public List<TriLibCore.Interfaces.IModel> Bones { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public bool IsBone { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public TriLibCore.Interfaces.IGeometryGroup GeometryGroup { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public Matrix4x4[] BindPoses { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public int[] MaterialIndices { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public Dictionary<string, object> UserProperties { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public bool HasCustomPivot { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public Matrix4x4 OriginalGlobalMatrix { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public string Name { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public bool Used { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public byte[] bytes;
        }
        public class Unity_JPG_PNG_Reader : ReaderBase
        {
            internal enum ProcessingSteps
            {
                Parsing
            }

            public override string Name => "Unity_JPG_PNG";

            protected override Type LoadingStepEnumType => typeof(ProcessingSteps);

            protected override TriLibCore.Interfaces.IRootModel CreateRootModel()
            {
                throw new NotImplementedException();
            }

            public override TriLibCore.Interfaces.IRootModel ReadStream(Stream stream, AssetLoaderContext assetLoaderContext, string filename = null, Action<AssetLoaderContext, float> onProgress = null)
            {
                base.ReadStream(stream, assetLoaderContext, filename, onProgress);
                //SetupStream(ref stream);
                byte[] bytes = stream.ReadBytes();      
                stream.Close(); 

                UpdateLoadingPercentage(1f);
                return new LoadedBytes() { bytes = bytes };
            }

        }


#endif
        public class ImageLoader : IOLoader
        {

            public static ImageLoader Create()
            {
                var gameObject = new GameObject("ImageLoader");
                var assetLoaderFilePicker = gameObject.AddComponent<ImageLoader>();
                assetLoaderFilePicker.AutoDestroy = true;
                return assetLoaderFilePicker;
            }

            public override IList<string> Extensions => _imageFileExtensions;

            protected override void LoadAssetFromStream(string extension, string assetFilename, Stream assetStream)
            {
                LoadImageFromStream(assetStream, assetFilename, extension, OnLoad, OnProgress, OnError, AssetLoaderOptions, null, false, null);
            }

            protected override void LoadAssetFromFile(string path)
            {
                LoadImageFromFile(path, OnLoad, OnProgress, OnError, AssetLoaderOptions, null, false, null);
            }

            /// <summary>Loads an Image from the OS file picker asynchronously, or synchronously when the OS doesn't support Threads.</summary>
            /// <param name="title">The dialog title.</param>
            /// <param name="onLoad">The Method to call on the Main Thread when the Image is loaded.</param>
            /// <param name="onProgress">The Method to call when the Image loading progress changes.</param>
            /// <param name="onBeginLoad">The Method to call when the Image begins to load. This event receives a Boolean indicating if any file has been selected on the file-picker dialog.</param>
            /// <param name="onError">The Method to call on the Main Thread when any error occurs.</param>
            /// <param name="assetLoaderOptions">The options to use when loading the Image.</param>
            /// <param name="haltTask">Turn on this field to avoid loading the Image immediately and chain the Tasks.</param>
            public void LoadImageFromFilePickerAsync(bool allowMultipleImages, string startDirectory, string title, Action<ILoaderContext> onLoad, Action<ILoaderContext, float> onProgress, Action<bool> onBeginLoad, Action<ILoaderError> onError, ILoaderOptions assetLoaderOptions = null, bool haltTask = false)
            {
                OnLoad = onLoad;
                OnProgress = onProgress;
                OnError = onError;
                OnBeginLoad = onBeginLoad;
                AssetLoaderOptions = assetLoaderOptions;
                HaltTask = haltTask; 
#if BULKOUT_ENV
                try
                {
                    StandaloneFileBrowser.OpenFilePanelAsync(title, startDirectory, GetExtensions(), allowMultipleImages, OnItemsWithStreamSelected);
                }
                catch (Exception)
                {
                    TriLibCore.Utils.Dispatcher.InvokeAsync(DestroyMe);
                    throw;
                }
#endif
            }

            public override bool IsValid => this != null;

            public override void Dispose() => DestroyMe();

        }

        public abstract class ModelLoader : ILoader
        {
            public abstract bool IsValid { get; }

            public abstract void Dispose();

            /// <summary>Loads a Model from the OS file picker asynchronously, or synchronously when the OS doesn't support Threads.</summary>
            /// <param name="title">The dialog title.</param>
            /// <param name="onLoad">The Method to call on the Main Thread when the Model is loaded but resources may still be pending.</param>
            /// <param name="onMaterialsLoad">The Method to call on the Main Thread when the Model and resources are loaded.</param>
            /// <param name="onProgress">The Method to call when the Model loading progress changes.</param>
            /// <param name="onBeginLoad">The Method to call when the model begins to load. This event receives a Boolean indicating if any file has been selected on the file-picker dialog.</param>
            /// <param name="onError">The Method to call on the Main Thread when any error occurs.</param>
            /// <param name="wrapperGameObject">The Game Object that will be the parent of the loaded Game Object. Can be null.</param>
            /// <param name="assetLoaderOptions">The options to use when loading the Model.</param>
            /// <param name="haltTask">Turn on this field to avoid loading the model immediately and chain the Tasks.</param>
            public abstract void LoadModelFromFilePickerAsync(string title, Action<ILoaderContext> onLoad, Action<ILoaderContext> onMaterialsLoad, Action<ILoaderContext, float> onProgress, Action<bool> onBeginLoad, Action<ILoaderError> onError, GameObject wrapperGameObject = null, ILoaderOptions assetLoaderOptions = null, bool haltTask = false);

        }

        #region Images

        public static ImageLoader GetNewImageLoader()
        {
            ImageLoader loader = null;
#if BULKOUT_ENV
            loader = ImageLoader.Create();
#endif

            return loader;
        }

        public class ImageLoaderOptions : ILoaderOptions
        {
            public object Options => this;

            public bool DestroyOnError { get => false; set { } }

            public int timeout = 180;
            public int Timeout
            {
                get => timeout;
                set => timeout = value;
            }

            public bool linearColorSpace;
            public bool noMipMaps;

        }
        public static readonly ImageLoaderOptions _defaultImageLoaderOptions = new ImageLoaderOptions();

        public class ImageLoaderContext : ILoaderContext
        {
            public ImageLoaderOptions options;
            public ILoaderOptions Options { get => options; set => options = value is ImageLoaderOptions opts ? opts : _defaultImageLoaderOptions; }
            public string filename;
            public string Filename { get => filename; set => filename = value; }
            public string basepath;
            public string BasePath { get => basepath; set => basepath = value; }

            public string fileExtension;
            public string FileExtension { get => fileExtension; set => fileExtension = value; }
            public Stream stream;
            public Stream Stream { get => stream; set => stream = value; }

            public Action<ILoaderContext> onLoad;
            public Action<ILoaderContext> OnLoad { get => onLoad; set => onLoad = value; }

            public Action<ILoaderContext, float> onProgress;
            public Action<ILoaderContext, float> OnProgress { get => onProgress; set => onProgress = value; }

            public Action<ILoaderError> handleError;
            public Action<ILoaderError> HandleError { get => handleError; set => handleError = value; }

            public Action<ILoaderError> onError;
            public Action<ILoaderError> OnError { get => onError; set => onError = value; }

            public Action<ILoaderContext> onPreLoad;
            public Action<ILoaderContext> OnPreLoad { get => onPreLoad; set => onPreLoad = value; }

            public object customData;
            public object CustomData { get => customData; set => customData = value; }

            public bool haltTasks;
            public bool HaltTasks { get => haltTasks; set => haltTasks = value; }
            public GameObject RootGameObject
            {
                get => null;
                set { }
            }

            protected bool async;
            public bool Async { get => async; set => async = value; }

            protected string persistentDataPath;
            public string PersistentDataPath { get => persistentDataPath; set => persistentDataPath = value; }

#if BULKOUT_ENV
            protected AssetLoaderContext context;
            public AssetLoaderContext Context
            {
                get
                {
                    if (context == null)
                    {
                        context = new AssetLoaderContext() { Options = _defaultModelLoaderOptions.options, CustomData = this, Filename = Filename, BasePath = BasePath, FileExtension = FileExtension, Stream = Stream, HaltTasks = HaltTasks, RootGameObject = RootGameObject, Async = Async, PersistentDataPath = PersistentDataPath };
                    }
                    return context;
                }
            }

            public LoadedBytes output;
#endif

            public void Setup()
            {
#if BULKOUT_ENV
                Context.Setup();
#endif
            }

        }

        /// <summary>Loads a Image from the given Stream asynchronously.</summary>
        /// <param name="stream">The Stream containing the Image data.</param>
        /// <param name="filename">The Image filename.</param>
        /// <param name="fileExtension">The Image file extension. (Eg.: jpg)</param>
        /// <param name="onLoad">The Method to call on the Main Thread when the Image is loaded but resources may still pending.</param>
        /// <param name="onProgress">The Method to call when the Image loading progress changes.</param>
        /// <param name="onError">The Method to call on the Main Thread when any error occurs.</param>
        /// <param name="assetLoaderOptions">The options to use when loading the Image.</param>
        /// <param name="customContextData">The Custom Data that will be passed along the Context.</param>
        /// <param name="haltTask">Turn on this field to avoid loading the Image immediately and chain the Tasks.</param>
        /// <param name="onPreLoad">The method to call on the parallel Thread before the Unity objects are created.</param>
        /// <returns>The Asset Loader Context, containing Image loading information and the output Object.</returns>
        public static ImageLoaderContext LoadImageFromStream(Stream stream,
            string filename = null,
            string fileExtension = null,
            Action<ILoaderContext> onLoad = null,
            Action<ILoaderContext, float> onProgress = null,
            Action<ILoaderError> onError = null,
            ILoaderOptions assetLoaderOptions = null,
            object customContextData = null,
            bool haltTask = false,
            Action<ILoaderContext> onPreLoad = null)
        {
            var assetLoaderContext = new ImageLoaderContext
            {
                Options = assetLoaderOptions == null ? assetLoaderOptions : _defaultImageLoaderOptions,
                Stream = stream,
                Filename = filename,
#if BULKOUT_ENV
                FileExtension = fileExtension ?? TriLibCore.Utils.FileUtils.GetFileExtension(filename, false),
                BasePath = TriLibCore.Utils.FileUtils.GetFileDirectory(filename),
#else
            FileExtension = fileExtension,
            BasePath = string.Empty,
#endif
                OnLoad = onLoad,
                OnProgress = onProgress,
                HandleError = HandleLoaderError,
                OnError = onError,
                OnPreLoad = onPreLoad,
                CustomData = customContextData,
                HaltTasks = haltTask,
#if (UNITY_WEBGL && !TRILIB_ENABLE_WEBGL_THREADS) || (UNITY_WSA && !TRILIB_ENABLE_UWP_THREADS) || TRILIB_FORCE_SYNC
                Async = false,
#else
                Async = true,
#endif
                PersistentDataPath = Application.persistentDataPath
            };
            assetLoaderContext.Setup();
            LoadImageInternal(assetLoaderContext);
            return assetLoaderContext;
        }
        /// <summary>Loads an Image from the given path asynchronously.</summary>
        /// <param name="path">The Image file path.</param>
        /// <param name="onLoad">The Method to call on the Main Thread when the Image is loaded but resources may still pending.</param>
        /// <param name="onProgress">The Method to call when the Image loading progress changes.</param>
        /// <param name="onError">The Method to call on the Main Thread when any error occurs.</param>
        /// <param name="assetLoaderOptions">The options to use when loading the Image.</param>
        /// <param name="customContextData">The Custom Data that will be passed along the Context.</param>
        /// <param name="haltTask">Turn on this field to avoid loading the Image immediately and chain the Tasks.</param>
        /// <param name="onPreLoad">The method to call on the parallel Thread before the Unity objects are created.</param>
        /// <returns>The Asset Loader Context, containing Image loading information and the output Object.</returns>
        public static ImageLoaderContext LoadImageFromFile(string path,
            Action<ILoaderContext> onLoad = null,
            Action<ILoaderContext, float> onProgress = null,
            Action<ILoaderError> onError = null,
            ILoaderOptions assetLoaderOptions = null,
            object customContextData = null,
            bool haltTask = false,
            Action<ILoaderContext> onPreLoad = null)
        {
            var assetLoaderContext = new ImageLoaderContext
            {
                Options = assetLoaderOptions == null ? assetLoaderOptions : _defaultImageLoaderOptions,
                Filename = path,
                BasePath = TriLibCore.Utils.FileUtils.GetFileDirectory(path),
                OnLoad = onLoad,
                OnProgress = onProgress,
                HandleError = HandleLoaderError,
                OnError = onError,
                OnPreLoad = onPreLoad,
                CustomData = customContextData,
                HaltTasks = haltTask,
#if (UNITY_WEBGL && !TRILIB_ENABLE_WEBGL_THREADS) || (UNITY_WSA && !TRILIB_ENABLE_UWP_THREADS) || TRILIB_FORCE_SYNC
                Async = false,
#else
                Async = true,
#endif
                PersistentDataPath = Application.persistentDataPath
            };
            assetLoaderContext.Setup();
            LoadImageInternal(assetLoaderContext);
            return assetLoaderContext;
        }
        private static void LoadImageInternal(ImageLoaderContext assetLoaderContext)
        {

            var fileExtension = assetLoaderContext.FileExtension;
            if (fileExtension == null && assetLoaderContext.Filename != null)
            {
#if BULKOUT_ENV
                fileExtension = TriLibCore.Utils.FileUtils.GetFileExtension(assetLoaderContext.Filename, false);
#endif
            }
            if (fileExtension == "zip")
            {
#if BULKOUT_ENV
                /*AssetLoaderZip.LoadModelFromZipFile(assetLoaderContext.Filename,
                    assetLoaderContext.OnLoad,
                    assetLoaderContext.OnMaterialsLoad,
                    assetLoaderContext.OnProgress,
                    assetLoaderContext.OnError,
                    assetLoaderContext.WrapperGameObject,
                    assetLoaderContext.Options,
                    assetLoaderContext.CustomData,
                    assetLoaderContext.FileExtension,
                    assetLoaderContext.HaltTasks,
                    assetLoaderContext.OnPreLoad);*/
#endif
            }
            else
            {
#if BULKOUT_ENV
                TriLibCore.Utils.ThreadUtils.RequestNewThreadFor(
                        assetLoaderContext,
                        LoadImage,
                        FinalizeImageLoad, // On complete
                        HandleContextualizedError,
                        assetLoaderContext.Options.Timeout,
                        null,
                        !assetLoaderContext.HaltTasks,
                        assetLoaderContext.OnPreLoad
                    );
#endif
            }
        }

        #region Loading

        private static void LoadImage(ILoaderContext assetLoaderContext)
        {
            if (assetLoaderContext is ImageLoaderContext context) SetupImageLoading(context);
        }

        private static void SetupImageLoading(ImageLoaderContext assetLoaderContext)
        {
            if (assetLoaderContext.Stream == null && string.IsNullOrWhiteSpace(assetLoaderContext.Filename))
            {
                throw new Exception("Unable to load the given file.");
            }

            var fileExtension = assetLoaderContext.FileExtension;
            if (string.IsNullOrWhiteSpace(fileExtension))
            {
#if BULKOUT_ENV
                fileExtension = TriLibCore.Utils.FileUtils.GetFileExtension(assetLoaderContext.Filename, false);
#endif
            }
            else if (fileExtension[0] == '.' && fileExtension.Length > 1)
            {
                fileExtension = fileExtension.Substring(1);
            }

#if BULKOUT_ENV
            void OnProgress(AssetLoaderContext context, float progress)
            {
                assetLoaderContext.OnProgress?.Invoke(assetLoaderContext, progress);
            }

            if (assetLoaderContext.Stream == null)
            {
                var fileStream = new FileStream(assetLoaderContext.Filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                assetLoaderContext.Stream = fileStream;
                var reader = GetImageFileReader(fileExtension);
                if (reader != null)
                {
                    var output = reader.ReadStream(fileStream, assetLoaderContext.Context, assetLoaderContext.Filename, OnProgress);
                    if (output is LoadedBytes outputTex)
                    {
                        assetLoaderContext.output = outputTex;
                    }
                }
            }
            else
            {
                var reader = GetImageFileReader(fileExtension);
                if (reader != null)
                {
                    var output = reader.ReadStream(assetLoaderContext.Stream, assetLoaderContext.Context, assetLoaderContext.Filename, OnProgress);
                    if (output is LoadedBytes outputTex)
                    {
                        assetLoaderContext.output = outputTex;
                    }
                }
                else
                {
                    throw new Exception("Could not find a suitable reader for the given image. Please fill the 'fileExtension' parameter when calling any image loading method.");
                }
                if (assetLoaderContext.output.bytes == null)
                {
                    throw new Exception("Could not load the given image.");
                }
            }
#endif
        }


        private static void FinalizeImageLoad(ILoaderContext assetLoaderContext)
        {
            if (assetLoaderContext is ImageLoaderContext imageLoaderContext)
            {
            }
            assetLoaderContext.OnLoad?.Invoke(assetLoaderContext);
        }

        public static bool CanTextureBeCompressed(int textureWidth, int textureHeight) => textureWidth % 4 == 0 && textureHeight % 4 == 0;
        public struct TextureAssetCreationResult
        {
            public bool isCompressed;
            public bool isLinear;
            public bool isPNG;
            public Texture2D texture;
        }
#if BULKOUT_ENV
        public static TextureAssetCreationResult CreateNewTextureAsset(ILoaderContext assetLoaderContext, LoadedBytes bytes, bool attemptToCompress = true) => CreateNewTextureAsset(assetLoaderContext, bytes.bytes, attemptToCompress);
#endif
        public static TextureAssetCreationResult CreateNewTextureAsset(ILoaderContext assetLoaderContext, byte[] bytes = null, bool attemptToCompress = true)
        {
            // disable compression for now as it's leading to larger file sizes
            attemptToCompress = false; 

            bool isLinear = false;
            bool useMipMaps = true;
            bool isPNG = assetLoaderContext.FileExtension.AsID().Contains(fileExtension_PNG.AsID());

            ImageLoaderContext imageLoaderContext = null;
            if (assetLoaderContext is ImageLoaderContext) 
            { 
                imageLoaderContext = (ImageLoaderContext)assetLoaderContext;
            }
#if BULKOUT_ENV
            else if (assetLoaderContext is AssetLoaderContext alc && alc.CustomData is ImageLoaderContext)
            {
                imageLoaderContext = (ImageLoaderContext)alc.CustomData;
            }
#endif

            if (imageLoaderContext != null)
            {
                isLinear = imageLoaderContext.options.linearColorSpace;
                useMipMaps = !imageLoaderContext.options.noMipMaps;

                if (bytes == null) bytes = imageLoaderContext.output.bytes;
            }

            Texture2D tex = null;
            if (attemptToCompress)
            {
                if (isLinear)
                {
                    if (isPNG && SystemInfo.IsFormatSupported(GraphicsFormat.RGBA_DXT5_UNorm, UnityEngine.Experimental.Rendering.FormatUsage.Sample))
                    {
                        tex = new Texture2D(4, 4, UnityEngine.TextureFormat.DXT5, useMipMaps, true); 
                    }
                    else if (!isPNG && SystemInfo.IsFormatSupported(GraphicsFormat.RGBA_DXT1_UNorm, UnityEngine.Experimental.Rendering.FormatUsage.Sample))
                    {
                        tex = new Texture2D(4, 4, UnityEngine.TextureFormat.DXT1, useMipMaps, true);
                    }
                }
                else
                {
                    if (isPNG && SystemInfo.IsFormatSupported(GraphicsFormat.RGBA_DXT5_SRGB, UnityEngine.Experimental.Rendering.FormatUsage.Sample))
                    {
                        tex = new Texture2D(4, 4, UnityEngine.TextureFormat.DXT5, useMipMaps, false);
                    }
                    else if (!isPNG && SystemInfo.IsFormatSupported(GraphicsFormat.RGBA_DXT1_SRGB, UnityEngine.Experimental.Rendering.FormatUsage.Sample))
                    {
                        tex = new Texture2D(4, 4, UnityEngine.TextureFormat.DXT1, useMipMaps, false);
                    }
                }
            }

            if (tex == null)
            {
                if (isPNG)
                { 
                    tex = new Texture2D(2, 2, UnityEngine.TextureFormat.ARGB32, useMipMaps, isLinear);  
                }
                else
                {
                    tex = new Texture2D(2, 2, UnityEngine.TextureFormat.RGB24, useMipMaps, isLinear);  
                } 
            }  
            //Debug.Log($"Loading Image '{assetLoaderContext.Filename}' [{(isPNG ? "PNG" : "JPG")}] [{tex.format.ToString()}] [{(isLinear ? "Linear" : "sRGB")}] [useMipMaps={(useMipMaps ? "true" : "false")}]");
            try
            {
                if (!ImageConversion.LoadImage(tex, bytes)) throw new Exception($"Image '{assetLoaderContext.Filename}' could not be loaded as [{(isPNG ? "PNG" : "JPG")}] [{tex.format.ToString()}] [{(isLinear ? "Linear" : "sRGB")}] [useMipMaps={(useMipMaps ? "true" : "false")}]");            
            } 
            catch//(Exception)
            {
                if (attemptToCompress)
                {
                    if (tex != null) GameObject.DestroyImmediate(tex);
                    return CreateNewTextureAsset(assetLoaderContext, bytes, false);
                } 
                else
                {
                    if (tex != null) GameObject.DestroyImmediate(tex);
                    throw;
                }
            }

            return new TextureAssetCreationResult() { texture = tex, isCompressed = attemptToCompress, isLinear = isLinear, isPNG = isPNG }; 
        }

        #endregion

        #endregion

        #region Models

        public static ModelLoader GetNewModelLoader()
        {
            ModelLoader loader = null;
#if BULKOUT_ENV
            loader = new TriLibLoader();
#endif

            return loader;
        }

#if BULKOUT_ENV

        public static readonly TriLibLoaderOptions _defaultModelLoaderOptions = new TriLibLoaderOptions(AssetLoader.CreateDefaultLoaderOptions(false, true)); 

        public struct TriLibLoaderOptions : ILoaderOptions
        {
            public AssetLoaderOptions options;
            public object Options => options;

            public bool DestroyOnError { get => options == null ? false : options.DestroyOnError; set { if (options != null) options.DestroyOnError = value; } }

            public int Timeout { get => options == null ? 0 : options.Timeout; set { if (options != null) options.Timeout = value; } }

            public TriLibLoaderOptions(AssetLoaderOptions options)
            {
                this.options = options;
            }
        }

        public struct TriLibContextualizedError : ILoaderError
        {
            public IContextualizedError error;
            public TriLibContextualizedError(IContextualizedError error)
            {
                this.error = error;
            }

            public AssetLoaderContext Context => error.Context;

            public object GetContext() => error.GetContext();
            public Exception GetInnerException() => error.GetInnerException();

            public void Setup() { }
        }
        public struct TriLibLoaderContext : ILoaderContext
        {
            public TriLibCore.AssetLoaderContext context;
            public TriLibLoaderContext(TriLibCore.AssetLoaderContext context)
            {
                this.context = context;
            }

            public AssetLoaderContext Context => context;

            public ILoaderOptions Options { get => new TriLibLoaderOptions(context.Options); set { if (value is AssetLoaderOptions alo) context.Options = alo; } }
            public string Filename { get => context.Filename; set { context.Filename = value; } }
            public string BasePath { get => context.BasePath; set { context.BasePath = value; } }
            public string FileExtension { get => context.FileExtension; set { context.FileExtension = value; } } 
            public Stream Stream { get => context.Stream; set { context.Stream = value; } }
            public Action<ILoaderContext> OnLoad { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public Action<ILoaderContext, float> OnProgress { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public Action<ILoaderError> HandleError { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public Action<ILoaderError> OnError { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public Action<ILoaderContext> OnPreLoad { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public object CustomData { get => context.CustomData; set => context.CustomData = value; }
            public bool HaltTasks { get => context.HaltTasks; set => context.HaltTasks = value; }
            public GameObject RootGameObject { get => context.RootGameObject; set => context.RootGameObject = value; }

            public void Setup() => context.Setup();
        }
        public class TriLibLoader : ModelLoader
        {
            protected AssetLoaderFilePicker filePicker;
            protected AssetLoaderOptions loaderOptions;
            public TriLibLoader(AssetLoaderOptions loaderOptions = null)
            {
                if (loaderOptions == null) loaderOptions = _defaultModelLoaderOptions.options;
                this.loaderOptions = loaderOptions;

                filePicker = AssetLoaderFilePicker.Create();
            }

            public override bool IsValid => filePicker != null;
            public override void Dispose()
            {
                if (filePicker != null)
                {
                    GameObject.DestroyImmediate(filePicker.gameObject);
                }
                filePicker = null;
            }

            public override void LoadModelFromFilePickerAsync(string title, Action<ILoaderContext> onLoad, Action<ILoaderContext> onMaterialsLoad, Action<ILoaderContext, float> onProgress, Action<bool> onBeginLoad, Action<ILoaderError> onError, GameObject wrapperGameObject = null, ILoaderOptions assetLoaderOptions = null, bool haltTask = false)
            {
#if BULKOUT_ENV
                if (filePicker == null)
                {
                    Dispose();
                    return;
                }

                void OnLoad(AssetLoaderContext context)
                {
                    onLoad?.Invoke(new TriLibLoaderContext(context));
                }
                void OnMaterialsLoad(AssetLoaderContext context)
                {
                    onMaterialsLoad?.Invoke(new TriLibLoaderContext(context));
                }
                void OnProgress(AssetLoaderContext context, float progress)
                {
                    onProgress?.Invoke(new TriLibLoaderContext(context), progress);
                }
                void OnError(IContextualizedError error)
                {
                    onError?.Invoke(new TriLibContextualizedError(error));
                }

                // Shows the model selection file-picker.
                filePicker.LoadModelFromFilePickerAsync(title, OnLoad, OnMaterialsLoad, OnProgress, onBeginLoad, OnError, wrapperGameObject, assetLoaderOptions is TriLibLoaderOptions opt1 ? opt1.options : loaderOptions, haltTask);
#endif
            }

        }
#endif

        #endregion



    }

}

#endif