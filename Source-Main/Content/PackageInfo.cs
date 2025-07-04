using System;

using static Swole.swole;
using static Swole.Script.SwoleScriptSemantics;

namespace Swole
{

    [Serializable]
    public struct PackageInfo : IEquatable<PackageInfo>
    {

        public static explicit operator string(PackageInfo info) => info.GetIdentityString();

        public static implicit operator PackageIdentifier(PackageInfo info) => info.GetIdentity();

        public PackageInfo(string name, Version version, string curator, string description,string url = null, string[] tags = null) : this(name, version == null ? "" : version.ToString(), curator, description, url, tags) { }
        public PackageInfo(string name, string version, string curator, string description, string url = null, string[] tags = null)
        {
            this.url = url;
            this.name = name;
            this.version = version;
            this.curator = curator;
            this.description = description;
            this.tags = tags;
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

        /// <summary>
        /// Optional search tags
        /// </summary>
        public string[] tags;

        public bool Equals(PackageInfo other)
        {

            if (name != other.name) return false;
            if (version != other.version) return false;
            if (curator != other.curator) return false;

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

        public string ConvertToAssetPath(string assetName) => ((PackageIdentifier)this).ConvertToAssetPath(assetName);
        public AssetIdentifier ConvertToAssetIdentifier(string assetName) => ((PackageIdentifier)this).ConvertToAssetIdentifier(assetName);

    }

}
