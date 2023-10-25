#if UNITY_EDITOR

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEditor.Callbacks;
using UnityEditor.PackageManager.Requests;

namespace Swole.API.Unity
{

    public class SwoleUnityPackageSetup : AssetPostprocessor
    {

        private static bool invalid;
        public static bool Valid => !invalid;
        private static void Invalidate() => invalid = true;

        public const string packageDisplayName = "Swole Dev Suite";
        public const string packageNameSubstring = "myonaut.swole";
        /// <summary>
        /// The symbol used to communicate that all the necessary assets have been imported.
        /// </summary>
        public const string fullyLoadedScriptingDefineSymbol = "SWOLE_ENV";

        #region >>> USE CASE SPECIFIC CODE
        private static bool CheckIfTypeExists(string typeName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach(var assembly in assemblies)
            {
                if (assembly.GetType(typeName, false, true) != null) return true;
            }
            return false;
        }
        private static bool FoundLeanTween() => CheckIfTypeExists($"LeanTween");
        private static bool FoundMiniScript() => CheckIfTypeExists($"Miniscript.Script");
        #endregion

        public static bool CanFullyLoad()
        {
            #region >>> USE CASE SPECIFIC CODE
            return FoundMiniScript() && FoundLeanTween();
            #endregion
        }

        private static void RefreshDatabaseThenWaitToFullyLoad()
        {
            AssetDatabase.Refresh();
            WaitToFullyLoad();
        }
        private static void WaitToFullyLoad()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += WaitToFullyLoad;
                return;
            }

            EditorApplication.delayCall += TryToFullyLoad;
        }

        private static void TryToFullyLoad()
        {
            if (CanFullyLoad()) SetFullyLoaded();
        }

        private static void SetFullyLoaded()
        {
            ResetAssetLoaders();
#if !SWOLE_ENV
            var targetNames = Enum.GetNames(typeof(BuildTargetGroup));
            foreach (var targetName in targetNames) 
            {
                NamedBuildTarget target = default;
                try
                {
                    target = NamedBuildTarget.FromBuildTargetGroup(Enum.Parse<BuildTargetGroup>(targetName));
                }
                catch { }
                SetFullyLoaded(target);  
            }
            //SetFullyLoaded(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup)); // If you wanted to only set scripting define symbols for the currently selected build target
#endif
        }
        private static void SetUnloaded()
        {
#if SWOLE_ENV
            var targetNames = Enum.GetNames(typeof(BuildTargetGroup));
            foreach (var targetName in targetNames)
            {
                NamedBuildTarget target = default;
                try
                {
                    target = NamedBuildTarget.FromBuildTargetGroup(Enum.Parse<BuildTargetGroup>(targetName));
                }
                catch { }
                SetUnloaded(target);
            }
#endif
        }
        private static void SetFullyLoaded(NamedBuildTarget target)
        {
#if !SWOLE_ENV
            if (string.IsNullOrEmpty(target.TargetName)) return;
            PlayerSettings.GetScriptingDefineSymbols(target, out string[] symbols_);
            int symbolCount = symbols_.Length;
            for (int a = 0; a < symbolCount; a++) if (symbols_[a] == fullyLoadedScriptingDefineSymbol) return;

            string[] symbols = new string[symbolCount + 1];
            if (symbolCount > 0) Array.Copy(symbols_, symbols, symbolCount);
            symbols[symbolCount] = fullyLoadedScriptingDefineSymbol;

            foreach (string sym in symbols) Debug.Log(sym);
            PlayerSettings.SetScriptingDefineSymbols(target, symbols);
#endif
        }
        private static void SetUnloaded(NamedBuildTarget target)
        {
#if SWOLE_ENV
            if (string.IsNullOrEmpty(target.TargetName)) return;
            PlayerSettings.GetScriptingDefineSymbols(target, out string[] symbols_);
            List<string> symbols = new List<string>(symbols_);
            symbols.RemoveAll(i => i == fullyLoadedScriptingDefineSymbol);

            PlayerSettings.SetScriptingDefineSymbols(target, symbols.ToArray());
#endif
        }

        [InitializeOnLoadMethod, DidReloadScripts]
        private static void OnInitialize()
        {
            if (!Valid) return;
            SubscribeToRegistered();
            SubscribeToRegistering();
            SubscribeToAssemblyReloadComplete();

            Run();
        }

        private static void OnRemove()
        {
            if (!Valid) return;

            Invalidate();
            try
            {
                UnsubscribeToRegistered();
            }
            catch { }
            try
            {
                UnsubscribeToRegistering();
            }
            catch { }
            try
            {
                UnsubscribeToAssemblyReloadComplete();
            }
            catch { }

            try
            {
                SetUnloaded();
            }
            catch { }

            DeleteMutableAssets();
            Debug.Log($"[{packageDisplayName}] Removal process complete!");
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            foreach (string str in importedAssets)
            {
                Debug.Log("Reimported Asset: " + str);
            }
            foreach (string str in deletedAssets)
            {
                Debug.Log("Deleted Asset: " + str);
            }

            for (int i = 0; i < movedAssets.Length; i++)
            {
                Debug.Log("Moved Asset: " + movedAssets[i] + " from: " + movedFromAssetPaths[i]);
            }

            Run();
        }

        private static bool subscribedRegistered;
        private static bool subscribedRegistering;
        private static bool subscribedAssembly;

        private static void SubscribeToRegistered() { if (!subscribedRegistered) { Events.registeredPackages += RegisteredPackagesEventHandler; subscribedRegistered = true; } }
        private static void UnsubscribeToRegistered() { if (subscribedRegistered) { Events.registeredPackages -= RegisteredPackagesEventHandler; subscribedRegistered = false; } }

        private static void SubscribeToRegistering() { if (!subscribedRegistering) { Events.registeringPackages += RegisteringPackagesEventHandler; subscribedRegistering = true; } }
        private static void UnsubscribeToRegistering() { if (subscribedRegistering) { Events.registeringPackages -= RegisteringPackagesEventHandler; subscribedRegistering = false; } }

        private static void SubscribeToAssemblyReloadComplete() { if (!subscribedAssembly) { AssemblyReloadEvents.afterAssemblyReload += AssemblyReloadComplete; subscribedAssembly = true; } }
        private static void UnsubscribeToAssemblyReloadComplete() { if (subscribedAssembly) { AssemblyReloadEvents.afterAssemblyReload -= AssemblyReloadComplete; subscribedAssembly = false; } }

        private static void RegisteredPackagesEventHandler(PackageRegistrationEventArgs packageRegistrationEventArgs)
        {
            // Code executed here can safely assume that the Editor has finished compiling the new list of packages
            Debug.Log("Swole setup after package registered test");
            Run();
        }

        private static void RegisteringPackagesEventHandler(PackageRegistrationEventArgs packageRegistrationEventArgs)
        {
            foreach (var removedPackage in packageRegistrationEventArgs.removed)
            {
                if (removedPackage.displayName == packageDisplayName)
                {
                    OnRemove();
                    break;
                }
            }
        }

        private static void AssemblyReloadComplete()
        {
            Run();
        }

        private static bool isRunning;
        public static bool IsRunning => isRunning;
        public static void Run()
        {
#if SWOLE_ENV
            if (CanFullyLoad())
            {
                return;
            }
            else
            {
                SetUnloaded();
            }
#endif

            if (!Valid || IsRunning) return;

            isRunning = true;

            string packageDir = null;
            ListRequest Request = null;

            void Step()
            {
                if (packageDir == null && Request == null)
                {
                    Request = Client.List();
                }

                if (Request != null)
                {
                    if (Request.IsCompleted)
                    {
                        if (Request.Status == StatusCode.Success)
                        {
                            foreach (var package in Request.Result)
                            {
                                if (package.displayName == packageDisplayName && package.name.ToLower().Contains(packageNameSubstring.ToLower()))
                                {
                                    packageDir = package.resolvedPath;
                                    break;
                                }
                            }
                        }
                        else if (Request.Status >= StatusCode.Failure)
                        {
                            Debug.Log($"[{packageDisplayName}] Encountered error while performing package lookup: {Request.Error.message}");
                        }

                        Request = null;
                    }
                    else
                    {
                        return;
                    }
                }

                if (!string.IsNullOrEmpty(packageDir))
                {
                    BeginLoadingAssets(packageDir);

                    if (!CanFullyLoad()) return;
                    SetFullyLoaded();
                } 
                else
                {
                    Debug.Log($"[{packageDisplayName}] Package directory path was empty!");
                }

                isRunning = false;
                EditorApplication.update -= Step;
            }

            EditorApplication.update += Step;
        }

        #region >>> USE CASE SPECIFIC CODE
        private static string mutableImportPath = Path.Combine(Application.dataPath, $"{packageDisplayName} (READ-ONLY)");
        private static string miniScriptPath = Path.Combine(Application.dataPath, mutableImportPath, "MiniScript");
        private static string leanTweenPath = Path.Combine(Application.dataPath, "LeanTween");

        private static bool loadingLeanTween = false;
        private static bool loadingMiniScript = false;

        private const string setupFolderName = "_SETUP-UNITY";
        private const string warningFileName = "WARNING.txt";

        private static string warningFileText = "This folder is prone to deletion or change! DO NOT STORE ANYTHING IN HERE!" + Environment.NewLine +
                        $"If you need the contents of this folder to remain even when the package '{packageDisplayName}' is removed or updated, simply delete this file.";

        private static void WaitForLeanTweenPackageImport(string n)
        {
            AssetDatabase.importPackageCompleted -= WaitForLeanTweenPackageImport;
            if (!Directory.Exists(leanTweenPath))
            {
                loadingLeanTween = false;
                Debug.LogError($"[{packageDisplayName}] Failed to import LeanTween from its cached .unitypackage file!");
                return;
            }
            Debug.Log($"[{packageDisplayName}] Successfully installed LeanTween!");
            WaitToFullyLoad();
        }
        #endregion

        private static void ResetAssetLoaders()
        {
            #region >>> USE CASE SPECIFIC CODE
            loadingLeanTween = false;
            loadingMiniScript = false;
            #endregion
        }

        private static void BeginLoadingAssets(string packageDirectoryPath)
        {
            if (!Valid) return;
            #region >>> USE CASE SPECIFIC CODE
            // > Only execute this code if the assets haven't started being loaded, otherwise just keep waiting

            bool refresh = false;

            void CreateMutableImportDir()
            {
                var dir = Directory.CreateDirectory(mutableImportPath);
                if (dir == null)
                {
                    Debug.LogError($"[{packageDisplayName}] Failed to create mutable import directory at path '{mutableImportPath}'");
                    return;
                }
                string warningFilePath = Path.Combine(dir.FullName, warningFileName);
                if (!File.Exists(warningFilePath)) File.WriteAllText(warningFilePath, warningFileText);
            }

            if (!loadingLeanTween)
            {
                loadingLeanTween = true;
                if (!FoundLeanTween())
                {
                    string cachedPath = Path.Combine(packageDirectoryPath, setupFolderName, "LeanTween.unitypackage");
                    if (File.Exists(cachedPath))
                    {
                        Debug.Log($"[{packageDisplayName}] Installing LeanTween unitypackage...");
                        AssetDatabase.importPackageCompleted += WaitForLeanTweenPackageImport;
                        AssetDatabase.ImportPackage(cachedPath, false);
                    } 
                    else
                    {
                        loadingLeanTween = false;
                        Debug.LogWarning($"[{packageDisplayName}] Could not locate/access cached package data for LeanTween! {cachedPath}");
                    }
                }
            }

            if (!loadingMiniScript)
            {
                loadingMiniScript = true;
                if (!FoundMiniScript())
                {
                    Debug.Log($"[{packageDisplayName}] Installing MiniScript assets...");
                    CreateMutableImportDir();
                    var targetDir = Directory.CreateDirectory(miniScriptPath);
                    string cachedPath = Path.Combine(packageDirectoryPath, "MiniScript");
                    if (Directory.Exists(cachedPath))
                    {
                        try
                        {
                            var topDir = new DirectoryInfo(cachedPath);
                            var sourceDir = new DirectoryInfo(Path.Combine(topDir.FullName, "source"));
                            var topFiles = topDir.EnumerateFiles("*", SearchOption.TopDirectoryOnly);
                            foreach (var topFile in topFiles) File.Copy(topFile.FullName, Path.Combine(targetDir.FullName, topFile.Name));
                            var subDirs = topDir.EnumerateDirectories("*", SearchOption.AllDirectories);
                            foreach (var subDir in subDirs)
                            {
                                if (subDir.Name == ".hidden")
                                {
                                    topFiles = subDir.EnumerateFiles("*", SearchOption.TopDirectoryOnly);
                                    foreach (var topFile in topFiles) File.Copy(topFile.FullName, Path.Combine(sourceDir.FullName, topFile.Name));
                                }
                                else if (subDir.Name == ".source")
                                {
                                    topFiles = subDir.EnumerateFiles("*", SearchOption.TopDirectoryOnly);
                                    foreach (var topFile in topFiles) File.Copy(topFile.FullName, Path.Combine(sourceDir.FullName, topFile.Name));
                                }
                                else if (subDir.Name.Contains("-cs")) // Found a c-sharp source code folder
                                {
                                    Copy(subDir.FullName, Path.Combine(sourceDir.FullName, subDir.Name));
                                }
                            }
                            refresh = true;
                            Debug.Log($"[{packageDisplayName}] Successfully installed MiniScript!");
                        } 
                        catch(Exception ex)
                        {
                            Debug.LogError($"[{packageDisplayName}] Error while trying to transfer MiniScript assets: [{ex.GetType().Name}] {ex.Message}");
                        }
                    }
                    else
                    {
                        loadingMiniScript = false;
                        Debug.LogWarning($"[{packageDisplayName}] Could not locate/access cached package data for MiniScript! {cachedPath}");
                    }
                }
            }

            if (refresh) AssetDatabase.Refresh();

            // <
#endregion
        }

        public static void DeleteMutableAssets()
        {
            #region >>> USE CASE SPECIFIC CODE
            bool refresh = false;

            if (Directory.Exists(mutableImportPath))
            {
                string warningFilePath = Path.Combine(mutableImportPath, warningFileName);
                if (File.Exists(warningFilePath))
                {
                    refresh = true;
                    try
                    {
                        Directory.Delete(warningFilePath);
                    } 
                    catch(Exception ex)
                    {
                        Debug.LogError($"[{packageDisplayName}] Error while trying to delete mutable import folder: [{ex.GetType().Name}] {ex.Message}");
                    }
                }
            }

            if (refresh) AssetDatabase.Refresh();
            #endregion
        }

        #region >>> USE CASE SPECIFIC CODE
        /// <summary>
        /// Source: https://stackoverflow.com/questions/58744/copy-the-entire-contents-of-a-directory-in-c-sharp
        /// </summary>
        public static void Copy(string sourceDirectory, string targetDirectory)
        {
            DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
            DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);

            CopyAll(diSource, diTarget);
        }

        /// <summary>
        /// Source: https://stackoverflow.com/questions/58744/copy-the-entire-contents-of-a-directory-in-c-sharp
        /// </summary>
        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }
        #endregion

    }

}

#endif