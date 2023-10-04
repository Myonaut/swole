using System;
using System.Collections;
using System.Collections.Generic;

namespace Swolescript
{

    [Serializable]
    public class SourcePackage
    {

        public SourcePackage(PackageManifest manifest, ICollection<SourceScript> scripts)
        {

            this.manifest = manifest;

            this.scripts = new SourceScript[scripts.Count];
            int i = 0;
            foreach (var script in scripts) 
            {
                var script_ = script;
                script_.packageInfo = manifest;
                this.scripts[i] = script_; 
                i++;
            }

        }

        protected readonly PackageManifest manifest;
        public PackageManifest Manifest => manifest;

        public bool HasURL => Manifest.HasURL;
        public bool NameIsValid => Manifest.NameIsValid;
        public bool VersionIsValid => Manifest.VersionIsValid;

        /// <summary>
        /// Optional origin url
        /// </summary>
        public string URL => Manifest.url;
        public string Name => Manifest.name;
        public Version Version => Manifest.Version;
        public string VersionString => Manifest.version;
        public string GetIdentityString() => Manifest.GetIdentityString();
        public PackageIdentifier GetIdentity() => Manifest.GetIdentity();

        public string Curator => Manifest.curator;
        public string Description => Manifest.description;

        protected readonly SourceScript[] scripts;

        public int ScriptCount => scripts == null ? 0 : scripts.Length;
        public SourceScript GetScript(int index) => index < 0 || index >= ScriptCount ? default : scripts[index];
        public SourceScript this[int index] => GetScript(index);

        public int DependencyCount => Manifest.DependencyCount;
        public PackageIdentifier GetDependency(int index) => Manifest.GetDependency(index);
        public string GetDependencyString(int index) => Manifest.GetDependencyString(index);

    }

}
