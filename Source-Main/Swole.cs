using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

#if SWOLE_ENV
using Miniscript;
#endif

using Swole.Script;

using static Swole.Script.SwoleScriptSemantics;

namespace Swole
{

    public class swole
    {

        private readonly EngineHook engine;

        private DirectoryInfo assetDirectory;
        public DirectoryInfo AssetDirectoryLocal
        {
            get
            {

                if (assetDirectory == null && engine != null && !string.IsNullOrEmpty(engine.WorkingDirectory)) assetDirectory = Directory.CreateDirectory(Path.Combine(engine.WorkingDirectory, "swole"));
                return assetDirectory;

            }
        }

        public swole(EngineHook engine)
        {
            this.engine = engine;
        }

        public static swole SetEngine(EngineHook engine) => instance = new swole(engine);

        private static swole instance = new swole(new EngineHook());
        public static swole Instance => instance;
        public static EngineHook Engine => instance.engine;

        public static SwoleLogger DefaultLogger => Engine.Logger;
        public static void Log(string message) => DefaultLogger.Log(message);
        public static void LogWarning(string warning) => DefaultLogger.LogWarning(warning);
        public static void LogError(string error) => DefaultLogger.LogError(error);

        public static RuntimeEnvironment DefaultEnvironment => Engine.RuntimeEnvironment;
        public static DirectoryInfo AssetDirectory => instance.AssetDirectoryLocal;

        public static DirectoryInfo CreateDirectory(string localPath)
        {

            if (string.IsNullOrEmpty(localPath)) return null;

            return Directory.CreateDirectory($"{AssetDirectory}/{localPath}");

        }

        [Serializable]
        public enum RuntimeState
        {

            Default, Editor, EditorPlayTest

        }

        private RuntimeState state;
        public static RuntimeState State
        {
            get => Instance.state;
            set => Instance.state = value;
        }

        public static bool IsEditor => State == RuntimeState.Editor || State == RuntimeState.EditorPlayTest;
        public static bool IsInPlayMode => State == RuntimeState.Default || State == RuntimeState.EditorPlayTest;


        #region Scripts & Packages

        public static bool ValidateScriptName(string name)
        {

            if (string.IsNullOrEmpty(name)) return false;

            return name.IsAlphaNumericNoWhitespace();

        }

        public static bool ValidatePackageName(string name)
        {

            if (string.IsNullOrEmpty(name)) return false;

            return name.IsPackageString();

        }

        public static bool ValidateVersionString(string version)
        {

            if (string.IsNullOrEmpty(version)) return false;

            return version.IsNativeVersionString();

        }

        private List<SourcePackage> packages = new List<SourcePackage>();

        [Serializable]
        public enum PackageActionResult
        {
            Success, PackageWasNull, PackageWasEmpty, PackageInvalidName, PackageInvalidVersion, PackageAlreadyLoaded, PackageNotFound, PackageNotLoaded, PackageDependencyNotFound, PackageDependencyNotLoaded, VersionOfPackageNotFound
        }

        public static PackageActionResult LoadPackage(SourcePackage package, out string resultInfo) => Instance.LoadPackageInternal(package, out resultInfo);
        public PackageActionResult LoadPackageInternal(SourcePackage package, out string resultInfo)
        {
            resultInfo = "";
            if (package == null) return PackageActionResult.PackageWasNull;
            if (!package.NameIsValid) return PackageActionResult.PackageInvalidName;
            if (package.ScriptCount <= 0) return PackageActionResult.PackageWasEmpty;
            foreach (var loadedPackage in packages) if (loadedPackage.Name == package.Name) return PackageActionResult.PackageAlreadyLoaded;

            for (int a = 0; a < package.DependencyCount; a++)
            {
                var dep = package.GetDependency(a);
                if (FindPackageInternal(dep, out _, out resultInfo) != PackageActionResult.Success)
                {
                    resultInfo = $"Package dependency {a} '{(string.IsNullOrEmpty(dep) ? "" : dep)}' was not found.{(string.IsNullOrEmpty(resultInfo) ? "" : (" Reason: " + resultInfo))}";
                    return PackageActionResult.PackageDependencyNotFound;
                }
            }

            packages.Add(package);
            return PackageActionResult.Success;
        }

        public static PackageActionResult UnloadPackage(SourcePackage package, out string resultInfo) => Instance.UnloadPackageInternal(package, out resultInfo);
        public PackageActionResult UnloadPackageInternal(SourcePackage package, out string resultInfo)
        {
            resultInfo = "";
            if (package == null) return PackageActionResult.PackageWasNull;
            return UnloadPackageInternal(package.Name, out resultInfo);
        }
        public static PackageActionResult UnloadPackage(string packageName, out string resultInfo) => Instance.UnloadPackageInternal(packageName, out resultInfo);
        public PackageActionResult UnloadPackageInternal(string packageName, out string resultInfo)
        {
            resultInfo = "";
            if (!ValidatePackageName(packageName)) return PackageActionResult.PackageInvalidName;
            return packages.RemoveAll(i => i.Name == packageName) > 0 ? PackageActionResult.Success : PackageActionResult.PackageNotLoaded;
        }

        public static PackageActionResult FindPackage(string packageStringOrName, out SourcePackage packageOut, out string resultInfo) => Instance.FindPackageInternal(packageStringOrName, out packageOut, out resultInfo);
        public PackageActionResult FindPackageInternal(string packageStringOrName, out SourcePackage packageOut, out string resultInfo)
        {

            resultInfo = "";
            packageOut = null;

            if (string.IsNullOrEmpty(packageStringOrName)) return PackageActionResult.PackageInvalidName;

            string packageVersion = "";

            int versionPrefix = packageStringOrName.IndexOf(ssVersionPrefix);
            if (versionPrefix >= 0) 
            {

                if ((versionPrefix + ssVersionPrefix.Length + 1) < packageStringOrName.Length) packageVersion = packageStringOrName.Substring(versionPrefix + ssVersionPrefix.Length + 1);

                if (string.IsNullOrEmpty(packageVersion)) return PackageActionResult.PackageInvalidVersion;

            }

            return FindPackageInternal(packageStringOrName, packageVersion, out packageOut, out resultInfo);
        }
        public static PackageActionResult FindPackage(string packageName, string packageVersion, out SourcePackage packageOut, out string resultInfo) => Instance.FindPackageInternal(packageName, packageVersion, out packageOut, out resultInfo);
        public PackageActionResult FindPackageInternal(string packageName, string packageVersion, out SourcePackage packageOut, out string resultInfo)
        {

            resultInfo = "";
            packageOut = null;

            if (!string.IsNullOrEmpty(packageVersion) && !ValidateVersionString(packageVersion)) return PackageActionResult.PackageInvalidVersion;

            if (!ValidatePackageName(packageName)) return PackageActionResult.PackageInvalidName;

            if (string.IsNullOrEmpty(packageVersion)) // If a package version is not specified, then find the latest version of the package.
            {

                Version highestVersion = null;

                foreach (var loadedPackage in packages) if (loadedPackage.Name == packageName)
                    {
                        if (packageOut != null && !loadedPackage.VersionIsValid) continue;
                        Version version = loadedPackage.Version;
                        if (highestVersion != null && version.CompareTo(highestVersion) <= 0) continue;
                        highestVersion = version;
                        packageOut = loadedPackage;
                    }

                if (packageOut != null) return PackageActionResult.Success;

            } 
            else // Try to find the specific version of the package.
            {

                var version = new Version(packageVersion);

                bool foundDifferentVersion = false;

                foreach (var loadedPackage in packages) if (loadedPackage.Name == packageName)
                    {
                        foundDifferentVersion = true;
                        if (loadedPackage.Version.CompareTo(version) != 0) continue;
                        packageOut = loadedPackage;
                        return PackageActionResult.Success;
                    }

                if (foundDifferentVersion) 
                {

                    resultInfo = $"Found one or more versions of '{packageName}' - but not version '{packageVersion}'.";
                    return PackageActionResult.VersionOfPackageNotFound; 
                
                }

            }

            return PackageActionResult.PackageNotLoaded;
        }


        public static bool TryFindScript(string packageName, string scriptName, out SourceScript scriptOut, out string resultInfo) => Instance.TryFindScriptInternal(packageName, scriptName, out scriptOut, out resultInfo);
        public bool TryFindScriptInternal(string packageName, string scriptName, out SourceScript scriptOut, out string resultInfo)
        {

            scriptOut = default;

            if (FindPackageInternal(packageName, out var package, out resultInfo) != PackageActionResult.Success) 
            {

                resultInfo = $"Could not find package '{packageName}'.{(string.IsNullOrEmpty(resultInfo) ? "" : (" Reason: " + resultInfo))}";
                return false;
            }

            for(int a = 0; a < package.ScriptCount; a++)
            {
                var script = package[a];
                if (script.Name == scriptName) 
                {
                    scriptOut = script;
                    return true; 
                }
            }

            scriptName = scriptName.ToLower().Trim();

            for (int a = 0; a < package.ScriptCount; a++)
            {
                var script = package[a];
                if (script.Name.ToLower().Trim() == scriptName)
                {
                    scriptOut = script;
                    return true;
                }
            }

            return false;

        }

#if SWOLE_ENV

        public static string ReadSwoleScriptLine(Lexer msLexer, out bool appendLineBreak)
        {
            int discard = 0;
            return ReadSwoleScriptLine(msLexer, ref discard, out appendLineBreak);
        }
        public static string ReadSwoleScriptLine(Lexer msLexer)
        {
            return ReadSwoleScriptLine(msLexer, out _);
        }
        public static string ReadSwoleScriptLine(Lexer msLexer, ref int endPos, out bool appendLineBreak)
        {

            string lineContent = "";
            appendLineBreak = true;
            while (!msLexer.AtEnd)
            {

                var token = msLexer.Dequeue();
                if (token.type == Token.Type.EOL)
                {
                    appendLineBreak = token.text == ";";
                    if (appendLineBreak) endPos = msLexer.position; // If the end of the line is a semi-colon, move endPos to its position so it gets replaced.
                    break;
                }

                endPos = msLexer.position;
                lineContent = lineContent + (token.type == Token.Type.Dot ? "." : token.text);

            }

            return lineContent;

        }

        public static List<PackageIdentifier> ExtractPackageDependencies(string sourceCode, List<PackageIdentifier> dependencies = null) => ExtractPackageDependencies(new Lexer(sourceCode), dependencies);

        public static List<PackageIdentifier> ExtractPackageDependencies(Lexer msLexer, List<PackageIdentifier> dependencies = null)
        {

            if (dependencies == null) dependencies = new List<PackageIdentifier>();

            if (msLexer == null) return dependencies;

            void AddDependency(string importLine)
            {
                int versionChar = importLine.IndexOf(ssVersionPrefix);
                string version = "";
                if (versionChar >= 0 && versionChar + 1 < importLine.Length)
                {
                    version = importLine.Substring(versionChar + 1);
                    if (!version.IsNativeVersionString()) version = "";
                    importLine = versionChar > 0 ? importLine.Substring(0, versionChar) : "";
                }
                importLine = importLine.Trim();
                if (string.IsNullOrEmpty(importLine)) return;
                dependencies.Add(new PackageIdentifier(importLine, version));
            }

            bool canImport = true;
            while (!msLexer.AtEnd)
            {

                var token = msLexer.Dequeue();

                void HandleImports()
                {

                    if (!canImport) return;

                    // Only recognize imports when no other code has been parsed.
                    if (token.type != Token.Type.Identifier)
                    {
                        if (token.type != Token.Type.EOL)
                        {
                            canImport = false;
                            return;
                        }
                        else return;
                    }

                    if (token.text == ssKeyword_Import) AddDependency(ReadSwoleScriptLine(msLexer)); else canImport = false;

                }

                HandleImports();

                void HandleEmbeds()
                {

                    if (token.type == Token.Type.Identifier && token.text == ssKeyword_Insert)
                    {

                        string insertLine = ReadSwoleScriptLine(msLexer);
                        int finalDot = insertLine.LastIndexOf('.');
                        if (finalDot > 0) AddDependency(insertLine.Substring(0, finalDot));

                    }

                }

                HandleEmbeds();

                // TODO: Add support for future intrinsic functions that reference assets in external packages

            }

            return dependencies;

        }

        /// <summary>
        /// Converts SwoleScript code to MiniScript code. 'workingPackage' is the package currently being edited, if applicable. 'topAuthor' is the author of the source that has been passed to this function. 'localScripts' is other scripts that are included in the workingPackage, if applicable.
        /// </summary>
        public string ParseSourceInternal(string source, ref List<PackageIdentifier> dependencyList, string topAuthor = null, PackageManifest workingPackage = default, int autoIndentation = ssDefaultAutoIndentation, int startIndentation = 0, ICollection<SourceScript> localScripts = null) 
        {

            if (localScripts == null && swole.FindPackage(workingPackage.Name, workingPackage.VersionString, out var workingSourcePackage, out _) == PackageActionResult.Success && workingSourcePackage != null)
            {
                var scriptsList = new List<SourceScript>();
                for (int a = 0; a < workingSourcePackage.ScriptCount; a++) scriptsList.Add(workingSourcePackage[a]);
                localScripts = scriptsList;
            }

            List<PackageIdentifier> deps = dependencyList;

            void AddDependency(PackageIdentifier dep)
            {
                if (deps == null) deps = new List<PackageIdentifier>();
                foreach (var existingDep in deps) if (existingDep == dep || (!ValidateVersionString(dep.version) && existingDep.name == dep.name)) return;
                deps.Add(dep);
            }

            string parsedSource = source;

            List<SourcePackage> importedPackages = new List<SourcePackage>();
            bool HasImportedPackage(string packageName)
            {
                foreach (var package in importedPackages) if (package != null && package.GetIdentityString() == packageName) return true;
                return false;
            }
            bool TryFindLocalOrImportedScript(string scriptName, out SourceScript script, out bool isLocal) // Tries to find a script in the local package or in a package that's been imported.
            {
                script = default;
                isLocal = true;
                if (string.IsNullOrEmpty(scriptName)) return false;
                if (localScripts != null)
                {
                    foreach (var localScript in localScripts) if (localScript.Name == scriptName)
                        {
                            script = localScript;
                            return true;
                        }
                }
                isLocal = false;
                foreach (var importedPackage in importedPackages)
                {
                    for (int i = 0; i < importedPackage.ScriptCount; i++)
                    {
                        var localScript = importedPackage[i];
                        if (localScript.Name == scriptName)
                        {
                            script = localScript;
                            return true;
                        }

                    }
                }

                return false;
            }

            var msLexer = new Lexer(source);
            int lengthOffset = 0;
            int indentation = startIndentation;
            bool canImport = true;
            Token prevToken = null;
            while (!msLexer.AtEnd)
            {

                int startPos = msLexer.position;

                var token = msLexer.Dequeue();

                int endPos = msLexer.position;

                if (token.type == Token.Type.Keyword && (token.text == msKeyword_If || token.text == msKeyword_For || token.text == msKeyword_While)) indentation += autoIndentation;
                if (token.type != Token.Type.EOL && ((prevToken != null && prevToken.type == Token.Type.Keyword && prevToken.text == msKeyword_Then) 
                    || (token.type == Token.Type.Keyword && token.text == msKeyword_EndIf)
                    || (token.type == Token.Type.Keyword && token.text == msKeyword_EndFor)
                    || (token.type == Token.Type.Keyword && token.text == msKeyword_EndWhile))) indentation -= autoIndentation;



                void ImportPackages()
                {

                    if (!canImport) 
                    {

                        if (token.type == Token.Type.Identifier && token.text == ssKeyword_Import) // Comment out late import attempts
                        {

                            string invalidLine = ReadSwoleScriptLine(msLexer, ref endPos, out bool appendLineBreak);

                            if (startPos >= endPos) return;
                            startPos = startPos + lengthOffset;
                            endPos = endPos + lengthOffset;

                            string replacementString = $"// {ssMsgPrefix_Error} {ssKeyword_Import} {invalidLine}{(appendLineBreak ? Environment.NewLine : "")}";

                            lengthOffset += (replacementString.Length - ((endPos - startPos) + 1)); // Add 1 because we're removing the char at startPos too.
                            parsedSource = (startPos > 0 ? parsedSource.Substring(0, startPos) : "") + replacementString + ((endPos + 1 < parsedSource.Length) ? parsedSource.Substring(endPos + 1) : "");

                        }

                        return; 
                    
                    }

                    // Only recognize imports when no other code has been parsed.
                    if (token.type != Token.Type.Identifier)
                    {
                        if (token.type != Token.Type.EOL)
                        {
                            canImport = false;
                            return;
                        }
                        else return;
                    }

                    if (token.text == ssKeyword_Import)
                    {

                        string importLine = ReadSwoleScriptLine(msLexer, ref endPos, out bool appendLineBreak);

                        if (startPos >= endPos) return;
                        startPos = startPos + lengthOffset;
                        endPos = endPos + lengthOffset;

                        string replacementString = "";

                        if (FindPackageInternal(importLine, out var package, out string resultInfo) == PackageActionResult.Success && package != null)
                        {

                            if (HasImportedPackage(package.GetIdentityString()))
                            {
                                replacementString = $"// {ssMsgPrefix_Warning} Tried to import '{package.GetIdentityString()}' - but it was already imported!";
                            }
                            else
                            {
                                importedPackages.Add(package);
                                AddDependency(new PackageIdentifier(package.Name, package.VersionString));
                                replacementString = $"// {ssMsgPrefix_Info} Imported '{package.GetIdentityString()}'";
                            }

                        }
                        else
                        {

                            replacementString = $"// {ssMsgPrefix_Error} Failed to import '{importLine}'{(string.IsNullOrEmpty(resultInfo) ? "" : (" Reason: " + resultInfo))}{(appendLineBreak ? Environment.NewLine : "")}";

                        }

                        lengthOffset += (replacementString.Length - ((endPos - startPos) + 1)); // Add 1 because we're removing the char at startPos too.
                        parsedSource = (startPos > 0 ? parsedSource.Substring(0, startPos) : "") + replacementString + ((endPos + 1 < parsedSource.Length) ? parsedSource.Substring(endPos + 1) : "");

                    }
                    else canImport = false;

                }

                ImportPackages();

                void AddEmbeds()
                {

                    if (token.type == Token.Type.Identifier && token.text == ssKeyword_Insert)
                    {

                        string insertLine = ReadSwoleScriptLine(msLexer, ref endPos, out bool appendLineBreak);

                        if (startPos >= endPos) return;
                        startPos = startPos + lengthOffset;
                        endPos = endPos + lengthOffset;

                        string embedString = "";
                        
                        if (insertLine.Length > 0)
                        {

                            int finalDot = insertLine.LastIndexOf('.');

                            string packageName = "";
                            string scriptName = insertLine;

                            if (finalDot >= 0 && finalDot + 1 < insertLine.Length)
                            {
                                scriptName = insertLine.Substring(finalDot + 1);
                                packageName = insertLine.Substring(0, finalDot);
                            }
                            packageName = packageName.Trim();
                            scriptName = scriptName.Trim();

                            if (string.IsNullOrEmpty(packageName) && TryFindLocalOrImportedScript(scriptName, out var embedScript, out bool isLocalDependency)) 
                            {
                                if (embedScript.source != source) // Naive counter against recursive script embeds
                                {
                                    embedString = embedScript.GetSourceEmbed(ref deps, topAuthor, workingPackage, autoIndentation, indentation, localScripts, startPos > 0, appendLineBreak);
                                    if (!isLocalDependency && embedScript.PackageInfo.NameIsValid) AddDependency(embedScript.PackageInfo.GetIdentity()); // Only add packages that aren't the same as the working package
                                }

                            }
                            else if (TryFindScriptInternal(packageName, scriptName, out embedScript, out string resultInfo))
                            {
                                if (embedScript.source != source) // Naive counter against recursive script embeds
                                {
                                    embedString = embedScript.GetSourceEmbed(ref deps, topAuthor, workingPackage, autoIndentation, indentation, localScripts, startPos > 0, appendLineBreak);
                                    SplitFullPackageString(packageName, out string pckName, out string pckVer);
                                    AddDependency(new PackageIdentifier(pckName, pckVer));
                                }
                            }
                            else
                            {

                                embedString = $"// {ssMsgPrefix_Error} Failed to embed '{(packageName + "." + scriptName)}'{(string.IsNullOrEmpty(resultInfo) ? "" : (" Reason: " + resultInfo))}{(appendLineBreak ? Environment.NewLine : "")}";

                            }

                        }

                        lengthOffset += (embedString.Length - ((endPos - startPos) + 1)); // Add 1 because we're removing the char at startPos too.
                        parsedSource = (startPos > 0 ? parsedSource.Substring(0, startPos) : "") + embedString + ((endPos + 1 < parsedSource.Length) ? parsedSource.Substring(endPos + 1) : "");

                    }

                }

                AddEmbeds();

                prevToken = token;

            }

            parsedSource = engine.ParseSource(parsedSource);
            dependencyList = deps; 

            return parsedSource;

        }

        /// <summary>
        /// Converts SwoleScript code to MiniScript code. 'workingPackage' is the package currently being edited, if applicable. 'topAuthor' is the author of the source that has been passed to this function. 'localScripts' is other scripts that are included in the workingPackage, if applicable.
        /// </summary>
        public static string ParseSource(string source, ref List<PackageIdentifier> dependencyList, string topAuthor = null, PackageManifest workingPackage = default, int autoIndentation = ssDefaultAutoIndentation, int startIndentation = 0, ICollection<SourceScript> localScripts = null) => Instance.ParseSourceInternal(source, ref dependencyList, topAuthor, workingPackage, autoIndentation, startIndentation, localScripts);

#endif

#endregion

    }

}