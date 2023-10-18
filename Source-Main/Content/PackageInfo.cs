using System;
using System.Collections.Generic;

using static Swole.Swole;
using static Swole.Script.SwoleScriptSemantics;

namespace Swole
{

    [Serializable]
    public struct PackageInfo : IEquatable<PackageInfo>
    {

        public static explicit operator string(PackageInfo info) => info.GetIdentityString();

        public PackageInfo(string name, Version version, string curator, string description,string url = null) : this(name, version == null ? "" : version.ToString(), curator, description, url) { }
        public PackageInfo(string name, string version, string curator, string description, string url = null)
        {
            this.url = url;
            this.name = name;
            this.version = version;
            this.curator = curator;
            this.description = description;
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
        public override string ToString() => GetIdentityString();

        public string curator;
        public string description;

        public bool Equals(PackageInfo other)
        {

            if (name != other.name) return false;
            if (url != other.url) return false;
            if (version != other.version) return false;
            if (curator != other.curator) return false;
            if (description != other.description) return false;

            return true;

        }

        public static bool operator ==(PackageInfo A, PackageInfo B) => A.Equals(B);
        public static bool operator !=(PackageInfo A, PackageInfo B) => !A.Equals(B);

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is PackageInfo info) return this == info;
            if (obj is PackageManifest manifest) return this == manifest;
            return base.Equals(obj);
        }

    }

}
