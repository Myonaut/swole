using Swole.Script;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Swole
{

    [Serializable]
    public class ContentPackage
    {

        private SourcePackage cachedSourcePackage;

        /// <summary>
        /// Returns a SourcePackage containing all of the scripts in the ContentPackage.
        /// </summary>
        public SourcePackage AsSourcePackage()
        {

            if (cachedSourcePackage != null) return cachedSourcePackage;

            List<SourceScript> scripts = new List<SourceScript>();

            if (content != null)
            {



            }

            cachedSourcePackage = new SourcePackage(manifest, scripts);

            return cachedSourcePackage;

        }
        public SourcePackage SourcePackage => AsSourcePackage();

        public ContentPackage(PackageManifest manifest, ICollection<IContent> content)
        {

            this.manifest = manifest;

            if (content != null)
            {
                this.content = new IContent[content.Count];
                int i = 0;
                foreach (var script in this.content)
                {
                    this.content[i] = script;
                    i++;
                }
            }
            else this.content = null;

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

        protected readonly IContent[] content;

        public int ContentCount => content == null ? 0 : content.Length;
        public IContent GetContent(int index) => index < 0 || index >= ContentCount ? default : content[index];
        public IContent this[int index] => GetContent(index);

        public int DependencyCount => Manifest.DependencyCount;
        public PackageIdentifier GetDependency(int index) => Manifest.GetDependency(index);
        public string GetDependencyString(int index) => Manifest.GetDependencyString(index);

    }

}
