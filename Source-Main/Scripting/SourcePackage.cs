using System;
using System.Collections;
using System.Collections.Generic;

using Miniscript;

namespace Swole.Script
{

    [Serializable]
    public class SourcePackage
    {

        public SourcePackage(PackageManifest manifest) 
        { 
            
            this.manifest = manifest; 
        
        }

        private static readonly List<PackageIdentifier> depsViewer = new List<PackageIdentifier>();
        public SourcePackage(PackageManifest manifest, SourceScript script, bool ensureDependencies = true)
        {

            this.scripts = new SourceScript[] { script };

            if (ensureDependencies)
            {

                depsViewer.Clear();
                script.ExtractPackageDependencies(depsViewer);
                bool replace = false;

                int cA = depsViewer.Count;
                for (int a = 0; a < manifest.DependencyCount; a++) depsViewer.Add(manifest.GetDependency(a));
                int cB = depsViewer.Count;
                for(int a = 0; a < cA; a++)
                {
                    var dep = depsViewer[a];
                    bool present = false;
                    for(int b = cA; b < cB; b++)
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

        public SourcePackage(PackageManifest manifest, ICollection<SourceScript> scripts, bool ensureDependencies = true)
        {

            if (scripts != null)
            {

                this.scripts = new SourceScript[scripts.Count];
                int i = 0;
                foreach (var script in scripts)
                {
                    var script_ = script;
                    script_.packageInfo = manifest;
                    this.scripts[i] = script_;
                    i++;
                }

                if (ensureDependencies)
                {

                    depsViewer.Clear();
                    bool replace = false;

                    for (int a = 0; a < manifest.DependencyCount; a++) depsViewer.Add(manifest.GetDependency(a));

                    for (int s = 0; s < this.scripts.Length; s++)
                    {
                        var script = this.scripts[s];

                        int cA = depsViewer.Count;
                        script.ExtractPackageDependencies(depsViewer);
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
            else this.scripts = null;

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

        protected readonly SourceScript[] scripts;

        public List<SourceScript> AsList(List<SourceScript> list = null)
        {
            if (list == null) list = new List<SourceScript>();
            for (int a = 0; a < ScriptCount; a++) list.Add(scripts[a]);

            return list;
        }

        public List<IContent> AsList(List<IContent> list = null)
        {
            if (list == null) list = new List<IContent>();
            for (int a = 0; a < ScriptCount; a++) list.Add(scripts[a]);

            return list;
        }

        public List<T> AsList<T>(List<T> list = null) where T : struct, IContent
        {
            if (list == null) list = new List<T>();
            for (int a = 0; a < ScriptCount; a++) list.Add((T)(object)scripts[a]);

            return list;
        }

        public bool IsEmpty => ScriptCount <= 0;
        public int ScriptCount => scripts == null ? 0 : scripts.Length;
        public SourceScript GetScript(int index) => index < 0 || index >= ScriptCount ? default : scripts[index];
        public SourceScript this[int index] => GetScript(index);

        public int DependencyCount => Manifest.DependencyCount;
        public PackageIdentifier GetDependency(int index) => Manifest.GetDependency(index);
        public string GetDependencyString(int index) => Manifest.GetDependencyString(index);

    }

}
