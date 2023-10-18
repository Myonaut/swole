using System;
using System.Collections.Generic;

using static Swole.Script.SwoleScriptSemantics;

namespace Swole
{

    [Serializable]
    public struct PackageManifest : IEquatable<PackageManifest>
    {

        public static PackageManifest FromRaw(byte[] buffer)
        {
            if (buffer == null) return default;
            string json = DefaultJsonSerializer.StringEncoder.GetString(buffer);
            return Swole.Engine.FromJson<PackageManifest>(json);
        }

        public static implicit operator PackageInfo(PackageManifest manifest) => manifest.info;
        public static explicit operator string(PackageManifest manifest) => manifest.GetIdentityString();

        public PackageManifest(string name, Version version, string curator, string description, ICollection<PackageIdentifier> dependencies = null, string url = null) : this(name, version == null ? "" : version.ToString(), curator, description, dependencies, url) { }
        public PackageManifest(string name, string version, string curator, string description, ICollection<PackageIdentifier> dependencies = null, string url = null) : this(new PackageInfo() 
        { 
        
            url = url,
            name = name,
            version = version, 
            curator = curator,
            description = description

        }, dependencies) { }
        public PackageManifest(PackageInfo info, ICollection<PackageIdentifier> dependencies = null)
        {
            this.info = info;

            if (dependencies == null)
            {
                this.dependencies = null;
                return;
            }

            this.dependencies = new PackageIdentifier[dependencies.Count];

            int i = 0;
            foreach (var dep in dependencies)
            {
                this.dependencies[i] = dep;
                i++;
            }
        }

        public PackageInfo info;

        public string URL => info.url;
        public string Name => info.name;
        public string VersionString => info.version;
        public string Curator => info.curator;
        public string Description => info.description;

        public bool HasURL => info.HasURL;
        public bool NameIsValid => info.NameIsValid;
        public Version Version => info.Version;
        public bool VersionIsValid => info.VersionIsValid;
        public string GetIdentityString() => info.GetIdentityString();
        public PackageIdentifier GetIdentity() => info.GetIdentity();
        public override string ToString() => GetIdentityString();

        public PackageIdentifier[] dependencies;

        public int DependencyCount => dependencies == null ? 0 : dependencies.Length;
        public PackageIdentifier GetDependency(int index) => index < 0 || index >= DependencyCount ? default : dependencies[index];
        public string GetDependencyString(int index) => index < 0 || index >= DependencyCount ? "" : GetFullPackageString(dependencies[index].name, dependencies[index].version);

        public bool Equals(PackageManifest other)
        {

            if (info != other.info) return false;

            if (DependencyCount != other.DependencyCount) return false;

            for(int a = 0; a < DependencyCount; a++)
            {
                var depA = dependencies[a];
                bool foundDep = false;
                for (int b = 0; b < other.DependencyCount; b++)
                {
                    var depB = dependencies[b];
                    if (depA.Equals(depB)) 
                    {
                        foundDep = true;
                        break;
                    }
                }
                if (!foundDep) return false;
            }

            return true;

        }

        public static bool operator ==(PackageManifest A, PackageManifest B) => A.Equals(B);
        public static bool operator !=(PackageManifest A, PackageManifest B) => !A.Equals(B);

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is PackageManifest manifest) return this == manifest;
            return base.Equals(obj);
        }

    }

}
