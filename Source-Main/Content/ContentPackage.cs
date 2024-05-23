using System;
using System.Collections;
using System.Collections.Generic;

using Swole.Script;

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

                for (int a = 0; a < content.Length; a++) if (content[a] is SourceScript script) scripts.Add(script);

            }

            cachedSourcePackage = new SourcePackage(manifest, scripts, false);

            return cachedSourcePackage;

        }
        public SourcePackage SourcePackage => AsSourcePackage();

        public ContentPackage(PackageManifest manifest)
        {

            this.manifest = manifest;
        
        }

        private static readonly List<PackageIdentifier> depsViewer = new List<PackageIdentifier>();
        public ContentPackage(PackageManifest manifest, IContent content, bool ensureDependencies = true)
        {

            this.content = new IContent[] { content };

            if (ensureDependencies)
            {

                depsViewer.Clear();
                content.ExtractPackageDependencies(depsViewer);
                bool replace = false;

                int cA = depsViewer.Count;
                for (int a = 0; a < manifest.DependencyCount; a++) depsViewer.Add(manifest.GetDependency(a));
                int cB = depsViewer.Count;
                for (int a = 0; a < cA; a++)
                {
                    var dep = depsViewer[a];
                    bool present = false;
                    for (int b = cA; b < cB; b++)
                    {
                        if (depsViewer[b].Equals(dep))
                        {
                            present = true;
                            break;
                        }
                    }
                    if (!present)
                    {
                        replace = true;
                        depsViewer.Add(dep);
                    }
                }

                if (replace) // Only allocate new array if necessary
                {
                    depsViewer.RemoveRange(0, cA);
                    manifest.dependencies = depsViewer.ToArray();
                }

            }

            this.manifest = manifest;

        }

        public ContentPackage(PackageManifest manifest, ICollection<IContent> content, bool ensureDependencies = true)
        {

            if (content != null)
            {

                this.content = new IContent[content.Count];
                int i = 0;
                foreach (var c in content)
                {
                    this.content[i] = c;
                    i++;
                }

                if (ensureDependencies)
                {

                    depsViewer.Clear();
                    bool replace = false;

                    for (int a = 0; a < manifest.DependencyCount; a++) depsViewer.Add(manifest.GetDependency(a));

                    for (int s = 0; s < this.content.Length; s++)
                    {
                        var cont = this.content[s];

                        int cA = depsViewer.Count;
                        cont.ExtractPackageDependencies(depsViewer);
                        int cB = depsViewer.Count;

                        for (int a = cA; a < cB; a++)
                        {
                            var dep = depsViewer[a];
                            bool present = false;
                            for (int b = 0; b < cA; b++)
                            {
                                if (depsViewer[b].Equals(dep))
                                {
                                    present = true;
                                    break;
                                }
                            }
                            if (!present)
                            {
                                replace = true;
                                depsViewer.Add(dep);
                            }
                        }
                    }

                    if (replace) // Only allocate new array if necessary
                    {
                        manifest.dependencies = depsViewer.ToArray();
                    }

                }

            }
            else this.content = null;

            this.manifest = manifest;

        }

        protected readonly PackageManifest manifest;
        public PackageManifest Manifest => manifest;

        public bool HasURL => Manifest.HasURL;
        public bool NameIsValid => Manifest.NameIsValid;
        public bool VersionIsValid => Manifest.VersionIsValid;

        /// <summary>
        /// Optional origin url
        /// </summary>
        public string URL => Manifest.URL;
        public string Name => Manifest.Name;
        public Version Version => Manifest.Version;
        public string VersionString => Manifest.VersionString;
        public string GetIdentityString() => Manifest.GetIdentityString();
        public PackageIdentifier GetIdentity() => Manifest.GetIdentity();
        public override string ToString() => GetIdentityString();

        public string Curator => Manifest.Curator;
        public string Description => Manifest.Description;
        public string[] Tags => Manifest.Tags;

        protected readonly IContent[] content;

        public List<IContent> AsList(List<IContent> list = null)
        {
            if (list == null) list = new List<IContent>();
            for (int a = 0; a < ContentCount; a++) if (content[a] != null) list.Add(content[a]);

            return list;
        }

        public int ContentCount => content == null ? 0 : content.Length;
        public IContent GetContent(int index) => index < 0 || index >= ContentCount ? default : content[index];
        public IContent this[int index] => GetContent(index);
        public int IndexOf(string contentName, Type contentType, bool caseSensitive = false)
        {
            if (string.IsNullOrEmpty(contentName) || contentType == null) return -1;
            if (!caseSensitive) contentName = contentName.ToLower();
            for (int a = 0; a < ContentCount; a++)
            {
                var c = GetContent(a);
                if (c == null || !contentType.IsAssignableFrom(c.GetType())) continue;
                string cname = c.Name;
                if (!caseSensitive) cname = cname.ToLower();
                if (cname == contentName) return a;
            }
            return -1;
        }
        public bool Contains(string contentName, Type contentType, bool caseSensitive = false) => IndexOf(contentName, contentType, caseSensitive) >= 0;
        public bool TryFind<T>(out T contentObj, string contentName, bool caseSensitive = false) where T : IContent
        {
            contentObj = default;
            if (TryFind(out var content, contentName, typeof(T), caseSensitive))
            {
                if (content is T)
                {
                    contentObj = (T)content;  
                    return true;
                }
            }

            return false;
        }
        public bool TryFind(out IContent contentObj, string contentName, System.Type type, bool caseSensitive = false)
        {
            contentObj = null;
            if (!typeof(IContent).IsAssignableFrom(type)) return false; 

            contentObj = default;
            int ind = IndexOf(contentName, type, caseSensitive);
            if (ind >= 0)
            {
                contentObj = content[ind];
                return true;
            }

            return false;
        }

        public int DependencyCount => Manifest.DependencyCount;
        public PackageIdentifier GetDependency(int index) => Manifest.GetDependency(index);
        public string GetDependencyString(int index) => Manifest.GetDependencyString(index);

    }

}
