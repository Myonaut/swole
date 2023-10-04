using System;
using System.Collections.Generic;

using static Swolescript.SwoleScript;
using static Swolescript.SwoleScriptSemantics;

namespace Swolescript
{

    [Serializable]
    public struct PackageManifest
    {

        public PackageManifest(string name, Version version, string curator, string description, ICollection<PackageIdentifier> dependencies = null, string url = null) : this(name, version == null ? "" : version.ToString(), curator, description, dependencies, url) { }
        public PackageManifest(string name, string version, string curator, string description, ICollection<PackageIdentifier> dependencies = null, string url = null)
        {
            this.url = url;
            this.name = name;
            this.version = version;
            this.curator = curator;
            this.description = description;

            if (dependencies == null)
            {
                this.dependencies = null;
                return;
            }

            this.dependencies = new PackageIdentifier[dependencies.Count];

            int i = 0;
            foreach(var dep in dependencies)
            {
                this.dependencies[i] = dep;
                i++;
            }
        }

        /// <summary>
        /// Optional origin url
        /// </summary>
        public string url;
        public bool HasURL => !string.IsNullOrEmpty(url);

        public string name;
        public bool NameIsValid => ValidatePackageName(name);

        public string version;
        public Version Version => version.AsVersion();
        public bool VersionIsValid => ValidateVersionString(version);
        public string GetIdentityString() => GetFullPackageString(name, version);
        public PackageIdentifier GetIdentity() => new PackageIdentifier(name, version);

        public string curator;
        public string description;
        public PackageIdentifier[] dependencies;

        public int DependencyCount => dependencies == null ? 0 : dependencies.Length;
        public PackageIdentifier GetDependency(int index) => index < 0 || index >= DependencyCount ? default : dependencies[index];
        public string GetDependencyString(int index) => index < 0 || index >= DependencyCount ? "" : GetFullPackageString(dependencies[index].name, dependencies[index].version);

    }

}
