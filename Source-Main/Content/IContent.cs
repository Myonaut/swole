using System;
using System.Globalization;

namespace Swole
{

    public interface IContent
    {

        public PackageManifest PackageInfo { get; }

        public string Name { get; }

        public string Author { get; }

        public string CreationDateString { get; }

        public string LastEditDateString { get; }

        public string Description { get; }

        public const string dateFormat = "MM/dd/yyyy";

    }

}
