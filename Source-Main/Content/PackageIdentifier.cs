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

    }

}
