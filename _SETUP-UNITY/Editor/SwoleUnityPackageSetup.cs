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
        public static bool IsPackage(UnityEditor.PackageManager.PackageInfo package) => package.displayName == packageDisplayName && package.name.ToLower().Contains(packageNameSubstring.ToLower());

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

        [InitializeOnLoadMethod]
        private static void OnInitialize()
        {
            if (!Valid) return;
            SubscribeAndRun(true);
        }
         
        [DidReloadScripts]
        private static void SubscribeAndRun() => SubscribeAndRun(false);
        private static void SubscribeAndRun(bool firstRun)
        {
            if (!Valid) return;
            SubscribeToRegistered();
            SubscribeToRegistering();
            SubscribeToAssemblyReloadComplete();

            Run(firstRun);
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
            SubscribeAndRun();
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
            SubscribeAndRun();
        }

        private static void RegisteringPackagesEventHandler(PackageRegistrationEventArgs packageRegistrationEventArgs)
        {
            foreach (var removedPackage in packageRegistrationEventArgs.changedFrom)
            {
                if (IsPackage(removedPackage))
                {
                    OnRemove();
                    return;
                }
            }
            foreach (var removedPackage in packageRegistrationEventArgs.removed)
            {
                if (IsPackage(removedPackage))
                {
                    OnRemove(); 
                    return;
                }
            }
        }

        private static void AssemblyReloadComplete()
        {
            SubscribeAndRun();
        }

        private static bool isRunning;
        public static bool IsRunning => isRunning;
        public static void Run(bool firstRun = false)
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
                                if (IsPackage(package))
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
                    BeginLoadingAssets(packageDir, firstRun);

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
        private const string warningFileName = "_WARNING.txt";

        private static string warningFileText = "This folder is prone to deletion or change! DO NOT STORE ANYTHING IN HERE!";

        private static void WaitForLeanTweenPackageImport(string n)
        {
            try
            {
                AssetDatabase.importPackageCompleted -= WaitForLeanTweenPackageImport;
            }
            catch { }
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

        private static void AddWarningFileTo(string directoryPath)
        {
            Directory.CreateDirectory(directoryPath);
            string warningFilePath = Path.Combine(directoryPath, warningFileName);
            if (!File.Exists(warningFilePath)) File.WriteAllText(warningFilePath, warningFileText);
        }

        private static void BeginLoadingAssets(string packageDirectoryPath, bool firstAttempt = true)
        {
            if (!Valid) return;
            #region >>> USE CASE SPECIFIC CODE
            // > Only execute this code if the assets haven't started being loaded, otherwise just keep waiting

            bool refresh = false;

            void CreateMutableImportDir()
            {
                DirectoryInfo dir = null;
                try
                {
                    dir = Directory.CreateDirectory(mutableImportPath);
                } 
                catch(Exception ex)
                {
                    Debug.LogWarning($"[{packageDisplayName}] Encountered error while creating mutable import directory: [{ex.GetType().FullName}]: {ex.Message}");
                }
                if (dir == null)
                {
                    Debug.LogError($"[{packageDisplayName}] Failed to create mutable import directory at path '{mutableImportPath}'");
                    return;
                }
                AddWarningFileTo(dir.FullName);
            }

            CreateMutableImportDir();
            try
            {
                string sourcePath = Path.Combine(packageDirectoryPath, "bin", "Scenes");
                if (Directory.Exists(sourcePath))
                {
                    string targetPath = Path.Combine(mutableImportPath, "bin", "Scenes");
                    bool skip = false;
                    if (!firstAttempt)
                    {
                        skip = Directory.Exists(targetPath);
                    }
                    if (!skip) Copy(sourcePath, targetPath);
                }
                else
                {
                    Debug.LogWarning($"[{packageDisplayName}] Could not locate/access Scenes folder in cached package path!");
                }
            }
            catch (Exception ex) 
            {
                Debug.LogWarning($"[{packageDisplayName}] Encountered error while copying bin assets: [{ex.GetType().FullName}]: {ex.Message}");
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
                        AddWarningFileTo(leanTweenPath);
                        AssetDatabase.importPackageCompleted += WaitForLeanTweenPackageImport;
                        AssetDatabase.ImportPackage(cachedPath, false);
                    } 
                    else
                    {
                        loadingLeanTween = false;
                        Debug.LogWarning($"[{packageDisplayName}] Could not locate/access cached package data for LeanTween!");
                    }
                }
            }

            if (!loadingMiniScript)
            {
                loadingMiniScript = true;
                if (!FoundMiniScript())
                {
                    Debug.Log($"[{packageDisplayName}] Installing MiniScript assets...");                
                    var targetDir = Directory.CreateDirectory(miniScriptPath);
                    string cachedPath = Path.Combine(packageDirectoryPath, "MiniScript");
                    if (Directory.Exists(cachedPath))
                    {
                        try
                        {
                            var topDir = new DirectoryInfo(cachedPath);
                            var sourceDir = Directory.CreateDirectory(Path.Combine(targetDir.FullName, "source"));
                            var topFiles = topDir.EnumerateFiles("*", SearchOption.TopDirectoryOnly);
                            foreach (var topFile in topFiles) File.Copy(topFile.FullName, Path.Combine(targetDir.FullName, topFile.Name), true);
                            var subDirs = topDir.EnumerateDirectories("*", SearchOption.AllDirectories);
                            var delayDirs = new List<DirectoryInfo>();
                            foreach (var subDir in subDirs)
                            {
                                if (subDir.Name == ".hidden")
                                {
                                    topFiles = subDir.EnumerateFiles("*", SearchOption.TopDirectoryOnly);
                                    foreach (var topFile in topFiles) File.Copy(topFile.FullName, Path.Combine(sourceDir.FullName, topFile.Name), true);
                                }
                                else if (subDir.Name == ".source")
                                {
                                    topFiles = subDir.EnumerateFiles("*", SearchOption.TopDirectoryOnly);
                                    foreach (var topFile in topFiles) File.Copy(topFile.FullName, Path.Combine(sourceDir.FullName, topFile.Name), true);
                                }
                                else if (subDir.Name.Contains("-cs")) // Found a c-sharp source code folder
                                {
                                    Copy(subDir.FullName, Path.Combine(sourceDir.FullName, subDir.Name));
                                } 
                                else if (subDir.Name.Contains(".custom")) // Delay custom source directory enumeration so that it can overwrite files
                                {
                                    delayDirs.Add(subDir);
                                }
                            }
                            foreach(var subDir in delayDirs)
                            {
                                Copy(subDir.FullName, Path.Combine(sourceDir.FullName, subDir.Name));
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
                        Debug.LogWarning($"[{packageDisplayName}] Could not locate/access cached package data for MiniScript!");
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
                    try
                    {
                        Directory.Delete(mutableImportPath, true);
                        refresh = true;
                        string metaFilePath = $"{mutableImportPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)}.meta";
                        if (File.Exists(metaFilePath)) File.Delete(metaFilePath);
                    } 
                    catch(Exception ex)
                    {
                        Debug.LogError($"[{packageDisplayName}] Error while trying to delete mutable import folder: [{ex.GetType().Name}] {ex.Message}");
                    }
                }
            } 

            if (Directory.Exists(leanTweenPath))
            {
                string warningFilePath = Path.Combine(leanTweenPath, warningFileName);
                if (File.Exists(warningFilePath))
                {
                    try
                    {
                        Directory.Delete(leanTweenPath, true);
                        refresh = true;
                        string metaFilePath = $"{leanTweenPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)}.meta";
                        if (File.Exists(metaFilePath)) File.Delete(metaFilePath);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[{packageDisplayName}] Error while trying to delete LeanTween folder: [{ex.GetType().Name}] {ex.Message}");
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