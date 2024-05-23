#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Swole.Script;

namespace Swole.API.Unity
{

    /// <summary>
    /// A wrapper used to edit a content package. Use SwolePackage.Create to create one.
    /// </summary>
    public class SwolePackage// : ScriptableObject
    {

        public static SwolePackage Create(PackageManifest manifest, ICollection<IContent> content = null, bool ensureDependencies = true)
        {

            SwolePackage package = new SwolePackage();//ScriptableObject.CreateInstance<SwolePackage>();
            package.immutable = new ContentPackage(manifest, content, ensureDependencies);
            return package;

        }
        public static SwolePackage Create(ContentPackage contentPackage)
        {

            SwolePackage package = new SwolePackage();//ScriptableObject.CreateInstance<SwolePackage>();
            package.immutable = contentPackage;
            return package;

        }

        protected readonly List<IContent> contentViewer = new List<IContent>();

        public void Add(IContent content, bool ensureDependencies = true)
        {

            if (immutable != null)
            {

                if (immutable.ContentCount > 0)
                {

                    contentViewer.Clear();
                    immutable.AsList(contentViewer);
                    contentViewer.Add(content);

                    immutable = new ContentPackage(immutable.Manifest, contentViewer, ensureDependencies);

                } 
                else
                {

                    immutable = new ContentPackage(immutable.Manifest, content, ensureDependencies);

                }

            }
            else
            {

                immutable = new ContentPackage(default, content, ensureDependencies);

            }

        }

        public void Add(ICollection<IContent> content, bool ensureDependencies = true)
        {

            if (immutable != null)
            {

                contentViewer.Clear();
                immutable.AsList(contentViewer);
                contentViewer.AddRange(content);

                immutable = new ContentPackage(immutable.Manifest, contentViewer, ensureDependencies);

            }
            else
            {

                immutable = new ContentPackage(default, content, ensureDependencies);

            }

        }

        public void Replace(IContent toReplace, IContent newContent, bool ensureDependencies = true)
        {

            if (immutable != null)
            {

                contentViewer.Clear();
                immutable.AsList(contentViewer);
                for (int a = 0; a < contentViewer.Count; a++)
                {
                    var content = contentViewer[a];
                    if (content != null && (ReferenceEquals(toReplace, content) || (content.Name == toReplace.Name && content.GetType() == toReplace.GetType())))
                    {
                        contentViewer[a] = newContent;
                        break;
                    }
                }

                immutable = new ContentPackage(immutable.Manifest, contentViewer, ensureDependencies);

            }

        }

        public bool Remove(IContent content, bool updateDependencies = true)
        {

            if (immutable != null && immutable.ContentCount > 0 && content != null)
            {

                var manifest = immutable.Manifest;
                if (updateDependencies) manifest.dependencies = null;

                contentViewer.Clear();
                immutable.AsList(contentViewer);

                if (contentViewer.RemoveAll(i => i == null || (ReferenceEquals(i, content) || (content.Name == i.Name && content.GetType() == i.GetType()))) > 0)
                {
                    immutable = new ContentPackage(manifest, contentViewer, updateDependencies);
                    return true;
                }

            }

            return false;

        }

        public bool Remove(ICollection<IContent> content, bool updateDependencies = true)
        {

            if (immutable != null && immutable.ContentCount > 0 && content != null)
            {

                var manifest = immutable.Manifest;
                if (updateDependencies) manifest.dependencies = null;

                contentViewer.Clear();
                immutable.AsList(contentViewer);
                contentViewer.AddRange(content);

                bool removed = false;
                foreach(var cont in content)
                {
                    if (cont == null) continue;
                    if (contentViewer.RemoveAll(i => i == null || (ReferenceEquals(i, cont) || (cont.Name == i.Name && cont.GetType() == i.GetType()))) > 0) removed = true;
                }

                if (removed) 
                {         
                    immutable = new ContentPackage(manifest, contentViewer, updateDependencies);
                    return true;
                }

            }

            return false;

        }

        public void Clear(bool clearDependencies = true)
        {

            var manifest = Manifest;
            if (clearDependencies) manifest.dependencies = null;
            immutable = new ContentPackage(manifest);

        }

        public void UpdateManifest(PackageManifest manifest, bool ensureDependencies = true)
        {

            //if (manifest == Manifest) return; // Manifests with different urls and descriptions are still considered "equal" if they have the same name, version, and curator - so don't check for equality here.

            contentViewer.Clear();
            immutable.AsList(contentViewer);

            immutable = new ContentPackage(manifest, contentViewer, ensureDependencies);

        }

        protected ContentPackage immutable;
        public ContentPackage Immutable 
        { 
        
            get
            {

                if (immutable == null) Clear();

                return immutable;

            }

        }

        public static implicit operator ContentPackage(SwolePackage pkg) => pkg.Immutable;

        public SourcePackage SourcePackage => immutable == null ? null : immutable.AsSourcePackage();

        public PackageManifest Manifest => immutable == null ? default : immutable.Manifest;

        public bool HasURL => immutable == null ? false : immutable.HasURL;
        public bool NameIsValid => immutable == null ? false : immutable.NameIsValid;
        public bool VersionIsValid => immutable == null ? false : immutable.VersionIsValid;

        public string URL => immutable == null ? string.Empty : immutable.URL;
        public string Name => immutable == null ? string.Empty : immutable.Name;
        public Version Version => immutable == null ? null : immutable.Version;
        public string VersionString => immutable == null ? string.Empty : immutable.VersionString;
        public string GetIdentityString() => immutable == null ? string.Empty : immutable.GetIdentityString();
        public PackageIdentifier GetIdentity() => immutable == null ? default : immutable.GetIdentity();

        public string Curator => immutable == null ? string.Empty : immutable.Curator;
        public string Description => immutable == null ? string.Empty : immutable.Description;

        public int ContentCount => immutable == null ? 0 : immutable.ContentCount;
        public IContent GetContent(int index) => index < 0 || index >= ContentCount ? default : immutable[index];
        public IContent this[int index] => GetContent(index);

        public int DependencyCount => immutable == null ? 0 : immutable.DependencyCount;
        public PackageIdentifier GetDependency(int index) => immutable.GetDependency(index);
        public string GetDependencyString(int index) => immutable.GetDependencyString(index);

    }

}

#endif