using System;
using System.Collections;
using System.Collections.Generic;

namespace Swolescript
{

    [Serializable]
    public class SourcePackage
    {

        public static bool ValidatePackageName(SourcePackage package) => ValidatePackageName(package == null ? "" : package.Name);
        public static bool ValidatePackageName(string name)
        {

            return !string.IsNullOrEmpty(name);

        }

        public bool NameIsValid => ValidatePackageName(name);

        public SourcePackage(string name, string curator, string description, ICollection<SourceScript> scripts, ICollection<string> dependencies = null)
        {

            this.name = name;
            this.curator = curator;
            this.description = description;

            this.scripts = new SourceScript[scripts.Count];
            int i = 0;
            foreach (var script in scripts) 
            { 
                this.scripts[i] = script;
                i++;
            }

            if (dependencies != null)
            {

                this.dependencies = new string[dependencies.Count];

                i = 0;
                foreach (var dependency in dependencies)
                {
                    this.dependencies[i] = dependency;
                    i++;
                }

            }

        }

        protected readonly string name;
        public string Name => string.IsNullOrEmpty(name) ? "" : name;

        protected readonly string curator;
        public string Curator => string.IsNullOrEmpty(curator) ? "" : curator;

        protected readonly string description;
        public string Description => string.IsNullOrEmpty(description) ? "" : description;

        protected readonly SourceScript[] scripts;

        public int ScriptCount => scripts == null ? 0 : scripts.Length;

        public SourceScript GetScript(int index) => index < 0 || index >= ScriptCount ? default : scripts[index];

        public SourceScript this[int index] => GetScript(index);

        protected readonly string[] dependencies;
        public int DependencyCount => dependencies == null ? 0 : dependencies.Length;

        public string GetDependency(int index) => index < 0 || index >= DependencyCount ? "" : dependencies[index];

    }

}
