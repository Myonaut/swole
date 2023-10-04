using System;

using static Swolescript.SwoleScript;
using static Swolescript.SwoleScriptSemantics;

namespace Swolescript
{

    [Serializable]
    public struct PackageIdentifier
    {

        public PackageIdentifier(string name, string version)
        {
            this.name = name;
            this.version = version;
        }

        public string name;

        public string version;
        public Version Version => version.AsVersion();
        public bool VersionIsValid => ValidateVersionString(version);

        public static implicit operator string(PackageIdentifier dep) => GetFullPackageString(dep.name, dep.version);
        public override string ToString() => this;

    }

}
