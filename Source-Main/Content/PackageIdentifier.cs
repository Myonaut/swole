using System;

using static Swole.swole;
using static Swole.Script.SwoleScriptSemantics;

namespace Swole
{

    [Serializable]
    public struct PackageIdentifier : IEquatable<PackageIdentifier>
    {

        public PackageIdentifier(string name, string version)
        {
            this.name = name;
            this.version = version;
        }

        public PackageIdentifier(string packageString)
        {
            SplitFullPackageString(packageString, out name, out version);
        }

        public string name;

        public string version;
        public Version Version => version.AsVersion();
        public bool VersionIsValid => ValidateVersionString(version);

        public static implicit operator string(PackageIdentifier dep) => GetFullPackageString(dep.name, dep.version);
        public static implicit operator PackageIdentifier(string str) => new PackageIdentifier(str);
        public override string ToString() => this;

        public bool Equals(PackageIdentifier other)
        {
            if (name != other.name) return false;
            if (version != other.version) return false;

            return true;
        }

        public static bool operator ==(PackageIdentifier A, PackageIdentifier B) => A.Equals(B);
        public static bool operator !=(PackageIdentifier A, PackageIdentifier B) => !A.Equals(B);

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is PackageIdentifier other) return this == other;
            if (obj is PackageInfo info) return this == info.GetIdentity();
            if (obj is PackageManifest manifest) return this == manifest.GetIdentity();
            return base.Equals(obj);
        }

        public string ConvertToAssetPath(string assetName) => $"{ToString()}{System.IO.Path.DirectorySeparatorChar}{assetName}";
        public AssetIdentifier ConvertToAssetIdentifier(string assetName) => new AssetIdentifier(ToString(), assetName);

    }

    [Serializable]
    public struct AssetIdentifier : IEquatable<AssetIdentifier>
    {

        public AssetIdentifier(string package, string name)
        {
            this.package = package;
            this.name = name;
        }

        public string package;

        public string name;

        public static implicit operator PackageIdentifier(AssetIdentifier asset) => new PackageIdentifier(asset.package);
        public override string ToString() => $"{package}/{name}";

        public bool Equals(AssetIdentifier other)
        {
            if (name != other.name) return false;
            if (package != other.package) return false;

            return true;
        }

        public static bool operator ==(AssetIdentifier A, AssetIdentifier B) => A.Equals(B);
        public static bool operator !=(AssetIdentifier A, AssetIdentifier B) => !A.Equals(B);

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is AssetIdentifier other) return this == other;
            return base.Equals(obj);
        }

    }

}
