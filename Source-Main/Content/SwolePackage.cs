using System;
using System.Collections;
using System.Collections.Generic;

using Swole.Script;

namespace Swole
{

    /// <summary>
    /// A wrapper used to edit a content package. Use SwolePackage.Create to create one.
    /// </summary>
    public class SwolePackage
    {

        public static SwolePackage Create(PackageManifest manifest, ICollection<IContent> content = null, bool ensureDependencies = true)
        {

            SwolePackage package = new SwolePackage();
            package.immutable = new ContentPackage(manifest, content, ensureDependencies);
            return package;

        }
        public static SwolePackage Create(ContentPackage contentPackage)
        {

            SwolePackage package = new SwolePackage();
            package.immutable = contentPackage;
            return package;

        }

        protected readonly List<IContent> contentViewer = new List<IContent>();

        protected readonly List<IContent> orphanedContent = new List<IContent>();
        public void DisposeOrphanedContent()
        {
            if (orphanedContent != null)
            {
                if (immutable != null)
                {
                    contentViewer.Clear();
                    immutable.CopyIntoList(contentViewer);
                    for(int a = 0; a < contentViewer.Count; a++)
                    {
                        var content = contentViewer[a];
                        if (content == null) continue;
                        orphanedContent.RemoveAll(i => i == content);
                    }
                    contentViewer.Clear();
                }
                for (int a = 0; a < orphanedContent.Count; a++)
                {
                    var content = orphanedContent[a];
                    if (content == null) continue;
                    try
                    {
                        content.Dispose();
                    }
                    catch (Exception ex)
                    {
                        swole.LogError(ex);
                    }
                }
                orphanedContent.Clear();
            }
        }

        public void Add(IContent content, bool ensureDependencies = true)
        {

            if (immutable != null)
            {

                if (immutable.ContentCount > 0)
                {

                    contentViewer.Clear();
                    immutable.AsList(contentViewer);
                    contentViewer.Add(content);

                    immutable = new ContentPackage(immutable.Manifest, contentViewer, ensureDependencies, orphanedContent);

                } 
                else
                {

                    immutable = new ContentPackage(immutable.Manifest, content, ensureDependencies, orphanedContent);

                }

            }
            else
            {

                immutable = new ContentPackage(default, content, ensureDependencies, orphanedContent);

            }

        }

        public void Add(ICollection<IContent> content, bool ensureDependencies = true)
        {

            if (immutable != null)
            {

                contentViewer.Clear();
                immutable.AsList(contentViewer);
                contentViewer.AddRange(content);

                immutable = new ContentPackage(immutable.Manifest, contentViewer, ensureDependencies, orphanedContent);

            }
            else
            {

                immutable = new ContentPackage(default, content, ensureDependencies, orphanedContent);

            }

        }

        public void Replace(IContent toReplace, IContent newContent, bool ensureDependencies = true, bool orphanReplacedContent = true)
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
                        if (orphanReplacedContent && !ReferenceEquals(newContent, content)) orphanedContent.Add(content);
                        contentViewer[a] = newContent;
                        break;
                    }
                }

                immutable = new ContentPackage(immutable.Manifest, contentViewer, ensureDependencies, orphanedContent);

            }

        }

        public void AddOrReplace(IContent newContent, bool ensureDependencies = true, bool orphanReplacedContent = true)
        {
             
            if (immutable != null)
            {

                contentViewer.Clear();
                immutable.AsList(contentViewer);
                bool replaced = false;
                for (int a = 0; a < contentViewer.Count; a++)
                {
                    var content = contentViewer[a];
                    if (content != null && (ReferenceEquals(newContent, content) || (content.Name == newContent.Name && content.GetType() == newContent.GetType())))
                    {
                        if (orphanReplacedContent && !ReferenceEquals(newContent, content)) orphanedContent.Add(content);
                        contentViewer[a] = newContent;
                        replaced = true;
                        break;
                    }
                }
                if (!replaced) contentViewer.Add(newContent);

                immutable = new ContentPackage(immutable.Manifest, contentViewer, ensureDependencies, orphanedContent);

            }

        }
        public void AddOrReplace(ICollection<IContent> newContent, bool ensureDependencies = true, bool orphanReplacedContent = true)
        {

            if (immutable != null)
            {
                 
                contentViewer.Clear();
                immutable.AsList(contentViewer);
                void AddOrReplaceInternal(IContent newContent)
                {
                    bool replaced = false;
                    for (int a = 0; a < contentViewer.Count; a++)
                    {
                        var content = contentViewer[a];
                        if (content != null && (ReferenceEquals(newContent, content) || (content.Name == newContent.Name && content.GetType() == newContent.GetType())))
                        {
                            if (orphanReplacedContent && !ReferenceEquals(newContent, content)) orphanedContent.Add(content);
                            contentViewer[a] = newContent;
                            replaced = true;
                            break;
                        }
                    }
                    if (!replaced) contentViewer.Add(newContent);
                }
                foreach (var content in newContent) AddOrReplaceInternal(content);

                immutable = new ContentPackage(immutable.Manifest, contentViewer, ensureDependencies, orphanedContent);

            }

        }

        public bool Remove(IContent content, bool updateDependencies = true, bool orphanRemovedContent = true)
        {

            if (immutable != null && immutable.ContentCount > 0 && content != null)
            {

                var manifest = immutable.Manifest;
                if (updateDependencies) manifest.dependencies = null;

                contentViewer.Clear();
                immutable.AsList(contentViewer);

                bool flag = false;
                int i = 0;
                while(i < contentViewer.Count)
                {
                    var c = contentViewer[i];
                    if (c != null && (ReferenceEquals(c, content) || (content.Name == c.Name && content.GetType() == c.GetType())))
                    {
                        contentViewer.RemoveAt(i);
                        if (orphanRemovedContent) orphanedContent.Add(c);
                        flag = true;
                        continue;
                    }
                    i++;
                }
                if (contentViewer.RemoveAll(i => i == null) > 0) flag = true;

                if (flag)
                {
                    immutable = new ContentPackage(manifest, contentViewer, updateDependencies, orphanedContent);
                    return true;
                }

            }

            return false;

        }

        public bool Remove(ICollection<IContent> content, bool updateDependencies = true, bool orphanRemovedContent = true)
        {

            if (immutable != null && immutable.ContentCount > 0 && content != null)
            {

                var manifest = immutable.Manifest;
                if (updateDependencies) manifest.dependencies = null;

                contentViewer.Clear();
                immutable.AsList(contentViewer);
                contentViewer.AddRange(content);

                bool flag = false; 
                foreach(var cont in content)
                {
                    if (cont == null) continue;
                    int i = 0;
                    while (i < contentViewer.Count)
                    {
                        var c = contentViewer[i];
                        if (c != null && (ReferenceEquals(c, cont) || (cont.Name == c.Name && cont.GetType() == c.GetType())))
                        {
                            contentViewer.RemoveAt(i);
                            if (orphanRemovedContent) orphanedContent.Add(c);
                            flag = true;
                            continue;
                        }
                        i++;
                    }
                }
                if (contentViewer.RemoveAll(i => i == null) > 0) flag = true;

                if (flag) 
                {         
                    immutable = new ContentPackage(manifest, contentViewer, updateDependencies, orphanedContent);
                    return true;
                }

            }

            return false;

        }

        public bool RemoveAll(Predicate<IContent> match, bool updateDependencies = true)
        {

            if (immutable != null && immutable.ContentCount > 0)
            {

                var manifest = immutable.Manifest;
                if (updateDependencies) manifest.dependencies = null;

                contentViewer.Clear();
                immutable.AsList(contentViewer);

                if (contentViewer.RemoveAll(match) > 0)
                {
                    immutable = new ContentPackage(manifest, contentViewer, updateDependencies, orphanedContent);
                    return true;
                }

            }

            return false;

        }

        public void Clear(bool clearDependencies = true)
        {

            var manifest = Manifest;
            if (clearDependencies) manifest.dependencies = null;
            if (immutable != null) immutable.CopyIntoList(orphanedContent);
            immutable = new ContentPackage(orphanedContent, manifest);

        }

        public void UpdateManifest(PackageManifest manifest, bool ensureDependencies = true)
        {

            //if (manifest == Manifest) return; // Manifests with different urls and descriptions are still considered "equal" if they have the same name, version, and curator - so don't check for equality here.

            contentViewer.Clear();
            immutable.AsList(contentViewer);

            immutable = new ContentPackage(manifest, contentViewer, ensureDependencies, orphanedContent);

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