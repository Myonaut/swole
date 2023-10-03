using System;
using System.Collections;
using System.Collections.Generic;
using Miniscript;

using static Swolescript.SwoleScriptSemantics;

namespace Swolescript
{

    public class SwoleScript
    {

        private EngineHook engine;

        public SwoleScript(EngineHook engine)
        {
            this.engine = engine;
        }

        public static SwoleScript SetEngine(EngineHook engine) => instance = new SwoleScript(engine);

        private static SwoleScript instance = new SwoleScript(new EngineHook());
        public static SwoleScript Instance => instance;
        public static EngineHook Engine => instance.engine;

        private List<SourcePackage> packages = new List<SourcePackage>();

        [Serializable]
        public enum PackageActionResult
        {
            Success, PackageWasNull, PackageWasEmpty, PackageInvalidName, PackageAlreadyLoaded, PackageNotFound, PackageNotLoaded, PackageDependencyNotFound, PackageDependencyNotLoaded
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
                if (FindPackageLocal(dep, out var _) != PackageActionResult.Success)
                {
                    resultInfo = $"Package dependency {a} '{(string.IsNullOrEmpty(dep) ? "" : dep)}' has not been loaded or is not a valid package.";
                    return PackageActionResult.PackageDependencyNotFound;
                }
            }

            packages.Add(package);
            return PackageActionResult.Success;
        }

        public static PackageActionResult UnloadPackage(SourcePackage package) => Instance.UnloadPackageLocal(package);
        public PackageActionResult UnloadPackageLocal(SourcePackage package)
        {
            if (package == null) return PackageActionResult.PackageWasNull;
            return UnloadPackageLocal(package.Name);
        }
        public static PackageActionResult UnloadPackage(string packageName) => Instance.UnloadPackageLocal(packageName);
        public PackageActionResult UnloadPackageLocal(string packageName)
        {
            if (!SourcePackage.ValidatePackageName(packageName)) return PackageActionResult.PackageInvalidName;
            return packages.RemoveAll(i => i.Name == packageName) > 0 ? PackageActionResult.Success : PackageActionResult.PackageNotLoaded;
        }

        public static PackageActionResult FindPackage(string packageName, out SourcePackage packageOut) => Instance.FindPackageLocal(packageName, out packageOut);
        public PackageActionResult FindPackageLocal(string packageName, out SourcePackage packageOut)
        {
            packageOut = null;
            if (!SourcePackage.ValidatePackageName(packageName)) return PackageActionResult.PackageInvalidName;

            foreach (var loadedPackage in packages) if (loadedPackage.Name == packageName) 
                {
                    packageOut = loadedPackage;
                    return PackageActionResult.Success;
                }

            return PackageActionResult.PackageNotLoaded;
        }

        public static bool TryFindScript(string packageName, string scriptName, out SourceScript scriptOut) => Instance.TryFindScriptLocal(packageName, scriptName, out scriptOut);
        public bool TryFindScriptLocal(string packageName, string scriptName, out SourceScript scriptOut)
        {
            scriptOut = default;

            if (FindPackageLocal(packageName, out var package) != PackageActionResult.Success) return false;

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
        /// Converts SwoleScript code to MiniScript code.
        /// </summary>
        public string ParseSourceLocal(string source, int autoIndentation = ssDefaultAutoIndentation) 
        {

            string parsedSource = source;

            var msLexer = new Lexer(source);
            int lengthOffset = 0;
            int indentation = 0;
            Token prevToken = null;
            while (!msLexer.AtEnd)
            {

                int startPos = msLexer.position;

                var token = msLexer.Dequeue();
                UnityEngine.Debug.Log(token.ToString()); // For development purposes

                int endPos = msLexer.position;

                if (token.type == Token.Type.Keyword && (token.text == msKeyword_If || token.text == msKeyword_For || token.text == msKeyword_While)) indentation += autoIndentation;
                if (token.type != Token.Type.EOL && ((prevToken != null && prevToken.type == Token.Type.Keyword && prevToken.text == msKeyword_Then) 
                    || (token.type == Token.Type.Keyword && token.text == msKeyword_EndIf)
                    || (token.type == Token.Type.Keyword && token.text == msKeyword_EndFor)
                    || (token.type == Token.Type.Keyword && token.text == msKeyword_EndWhile))) indentation -= autoIndentation;

                void AddEmbeds()
                {

                    if (token.type == Token.Type.Identifier && token.text == ssKeyword_Import)
                    {

                        string importName = "";
                        bool addLineBreak = true;
                        while (!msLexer.AtEnd)
                        {

                            var importToken = msLexer.Dequeue();
                            if (importToken.type == Token.Type.EOL)
                            {
                                addLineBreak = importToken.text == ";";
                                if (addLineBreak) endPos = msLexer.position;
                                break;
                            }

                            endPos = msLexer.position;
                            importName = importName + (importToken.type == Token.Type.Dot ? "." : importToken.text);

                        }

                        if (startPos >= endPos) return;
                        startPos = startPos + lengthOffset;
                        endPos = endPos + lengthOffset;

                        string embedString = "";

                        int finalDot = importName.LastIndexOf('.');
                        if (finalDot >= 0 && finalDot + 1 < importName.Length)
                        {

                            string scriptName = importName.Substring(finalDot + 1);
                            string packageName = importName.Substring(0, finalDot);

                            if (TryFindScriptLocal(packageName, scriptName, out var embedScript))
                            {

                                //embeds.Add(new ScriptEmbed(new CodeSection(startPos, endPos), packageName, scriptName));

                                embedString = embedScript.GetSourceEmbed(packageName, indentation, startPos > 0, addLineBreak);

                            }
                            else
                            {

                                embedString = $"// !!! Failed to import '{(packageName + "." + scriptName)}' {(addLineBreak ? Environment.NewLine : "")}";

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

            return parsedSource;

        }

        /// <summary>
        /// Converts SwoleScript code to MiniScript code.
        /// </summary>
        public static string ParseSource(string source, int autoIndentation = ssDefaultAutoIndentation) => Instance.ParseSourceLocal(source, autoIndentation);

    }

}
