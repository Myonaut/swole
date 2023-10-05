using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using Miniscript;

using static Swolescript.SwoleScriptSemantics;

namespace Swolescript
{

    public class SwoleScript
    {

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

        private readonly EngineHook engine;

        private DirectoryInfo assetDirectory;
        public DirectoryInfo AssetDirectoryLocal
        {
            get
            {

                if (assetDirectory == null && engine != null && !string.IsNullOrEmpty(engine.WorkingDirectory)) assetDirectory = Directory.CreateDirectory($"{engine.WorkingDirectory}/swole");
                return assetDirectory;

            }
        }

        public SwoleScript(EngineHook engine)
        {
            this.engine = engine;
        }

        public static SwoleScript SetEngine(EngineHook engine) => instance = new SwoleScript(engine);

        private static SwoleScript instance = new SwoleScript(new EngineHook());
        public static SwoleScript Instance => instance;
        public static EngineHook Engine => instance.engine;
        public static RuntimeEnvironment DefaultEnvironment => Engine.RuntimeEnvironment;
        public static DirectoryInfo AssetDirectory => instance.AssetDirectoryLocal;

        private List<SourcePackage> packages = new List<SourcePackage>();

        [Serializable]
        public enum PackageActionResult
        {
            Success, PackageWasNull, PackageWasEmpty, PackageInvalidName, PackageInvalidVersion, PackageAlreadyLoaded, PackageNotFound, PackageNotLoaded, PackageDependencyNotFound, PackageDependencyNotLoaded, VersionOfPackageNotFound
        }

        public static PackageActionResult LoadPackage(SourcePackage package, out string resultInfo) => Instance.LoadPackageLocal(package, out resultInfo);
        public PackageActionResult LoadPackageLocal(SourcePackage package, out string resultInfo)
        {
            resultInfo = "";
            if (package == null) return PackageActionResult.PackageWasNull;
            if (!package.NameIsValid) return PackageActionResult.PackageInvalidName;
            if (package.ScriptCount <= 0) return PackageActionResult.PackageWasEmpty;
            foreach (var loadedPackage in packages) if (loadedPackage.Name == package.Name) return PackageActionResult.PackageAlreadyLoaded;

            for (int a = 0; a < package.DependencyCount; a++)
            {
                var dep = package.GetDependency(a);
                if (FindPackageLocal(dep, out _, out resultInfo) != PackageActionResult.Success)
                {
                    resultInfo = $"Package dependency {a} '{(string.IsNullOrEmpty(dep) ? "" : dep)}' was not found.{(string.IsNullOrEmpty(resultInfo) ? "" : (" Reason: " + resultInfo))}";
                    return PackageActionResult.PackageDependencyNotFound;
                }
            }

            packages.Add(package);
            return PackageActionResult.Success;
        }

        public static PackageActionResult UnloadPackage(SourcePackage package, out string resultInfo) => Instance.UnloadPackageLocal(package, out resultInfo);
        public PackageActionResult UnloadPackageLocal(SourcePackage package, out string resultInfo)
        {
            resultInfo = "";
            if (package == null) return PackageActionResult.PackageWasNull;
            return UnloadPackageLocal(package.Name, out resultInfo);
        }
        public static PackageActionResult UnloadPackage(string packageName, out string resultInfo) => Instance.UnloadPackageLocal(packageName, out resultInfo);
        public PackageActionResult UnloadPackageLocal(string packageName, out string resultInfo)
        {
            resultInfo = "";
            if (!ValidatePackageName(packageName)) return PackageActionResult.PackageInvalidName;
            return packages.RemoveAll(i => i.Name == packageName) > 0 ? PackageActionResult.Success : PackageActionResult.PackageNotLoaded;
        }

        public static PackageActionResult FindPackage(string packageStringOrName, out SourcePackage packageOut, out string resultInfo) => Instance.FindPackageLocal(packageStringOrName, out packageOut, out resultInfo);
        public PackageActionResult FindPackageLocal(string packageStringOrName, out SourcePackage packageOut, out string resultInfo)
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

            return FindPackageLocal(packageStringOrName, packageVersion, out packageOut, out resultInfo);
        }
        public static PackageActionResult FindPackage(string packageName, string packageVersion, out SourcePackage packageOut, out string resultInfo) => Instance.FindPackageLocal(packageName, packageVersion, out packageOut, out resultInfo);
        public PackageActionResult FindPackageLocal(string packageName, string packageVersion, out SourcePackage packageOut, out string resultInfo)
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

        public static bool TryFindScript(string packageName, string scriptName, out SourceScript scriptOut, out string resultInfo) => Instance.TryFindScriptLocal(packageName, scriptName, out scriptOut, out resultInfo);
        public bool TryFindScriptLocal(string packageName, string scriptName, out SourceScript scriptOut, out string resultInfo)
        {

            scriptOut = default;

            if (FindPackageLocal(packageName, out var package, out resultInfo) != PackageActionResult.Success) 
            {

                resultInfo = $"Could not find package '{packageName}'.{(string.IsNullOrEmpty(resultInfo) ? "" : (" Reason: " + resultInfo))}";
                return false;
            }

            for(int a = 0; a < package.ScriptCount; a++)
            {
                var script = package[a];
                if (script.name == scriptName) 
                {
                    scriptOut = script;
                    return true; 
                }
            }

            scriptName = scriptName.ToLower().Trim();

            for (int a = 0; a < package.ScriptCount; a++)
            {
                var script = package[a];
                if (script.name.ToLower().Trim() == scriptName)
                {
                    scriptOut = script;
                    return true;
                }
            }

            return false;

        }

        /// <summary>
        /// Converts SwoleScript code to MiniScript code. 'workingPackage' is the package currently being edited, if applicable. 'topAuthor' is the author of the source that has been passed to this function. 'localScripts' is other scripts that are included in the workingPackage, if applicable.
        /// </summary>
        public string ParseSourceLocal(string source, ref List<PackageIdentifier> dependencyList, string topAuthor = null, PackageManifest workingPackage = default, int autoIndentation = ssDefaultAutoIndentation, ICollection<SourceScript> localScripts = null) 
        {

            List<PackageIdentifier> deps = dependencyList;

            void AddDependency(PackageIdentifier dep)
            {
                if (deps == null) deps = new List<PackageIdentifier>();
                foreach (var existingDep in deps) if (existingDep == dep || (!ValidateVersionString(dep.version) && existingDep.name == dep.name)) return;
                deps.Add(dep);
            }

            string parsedSource = source;

            string ReadSwoleCodeLine(Lexer msLexer, ref int endPos, out bool appendLineBreak)
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
                    foreach (var localScript in localScripts) if (localScript.name == scriptName)
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
                        if (localScript.name == scriptName)
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
            int indentation = 0;
            bool canImport = true;
            Token prevToken = null;
            while (!msLexer.AtEnd)
            {

                int startPos = msLexer.position;

                var token = msLexer.Dequeue();
                UnityEngine.Debug.Log(token.ToString()); // Temporary for development purposes

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

                            string invalidLine = ReadSwoleCodeLine(msLexer, ref endPos, out bool appendLineBreak);

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

                        string importLine = ReadSwoleCodeLine(msLexer, ref endPos, out bool appendLineBreak);

                        if (startPos >= endPos) return;
                        startPos = startPos + lengthOffset;
                        endPos = endPos + lengthOffset;

                        string replacementString = "";

                        if (FindPackageLocal(importLine, out var package, out string resultInfo) == PackageActionResult.Success && package != null)
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

                        string insertLine = ReadSwoleCodeLine(msLexer, ref endPos, out bool appendLineBreak);

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

                                embedString = embedScript.GetSourceEmbed(topAuthor, workingPackage, indentation, startPos > 0, appendLineBreak);
                                if (!isLocalDependency && embedScript.PackageInfo.NameIsValid) AddDependency(embedScript.PackageInfo.GetIdentity());

                            }
                            else if (TryFindScriptLocal(packageName, scriptName, out embedScript, out string resultInfo))
                            {
                                embedString = embedScript.GetSourceEmbed(topAuthor, workingPackage, indentation, startPos > 0, appendLineBreak);
                                SplitFullPackageString(packageName, out string pckName, out string pckVer);
                                AddDependency(new PackageIdentifier(pckName, pckVer));

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
        public static string ParseSource(string source, ref List<PackageIdentifier> dependencyList, string topAuthor = null, PackageManifest workingPackage = default, int autoIndentation = ssDefaultAutoIndentation, ICollection<SourceScript> localScripts = null) => Instance.ParseSourceLocal(source, ref dependencyList, topAuthor, workingPackage, autoIndentation, localScripts);

    }

}