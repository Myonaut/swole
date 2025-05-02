#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

using TMPro;

using Swole.UI;
using Swole.Script;
using Swole.API.Unity.Animation;

namespace Swole.API.Unity
{

    public class CreationBuilder : MonoBehaviour
    {

        public const string windowStateID_objectPalette = "object_palette";
        public const string windowStateID_curvesMenu = "curves_menu";
        public const string windowStateID_animationCurveEditor = "anim_curve_editor";
        public const string windowStateID_bezierCurveEditor = "bez_curve_editor";

        public const string str_title = "title";
        public const string str_recompile = "recompile";

        public delegate void SetTextDelegate(string text);

        public class Member : IDisposable
        {

            public Creation.ObjectPreloadState State
            {
                get
                {
                    var state = new Creation.ObjectPreloadState();

                    state.id = swoleGameObject == null ? -1 : swoleGameObject.id;
                    state.isHidden = isHidden;
                    state.showChildren = showChildren;

                    return state;
                }

                set
                {
                    isHidden = value.isHidden;
                    showChildren = value.showChildren;
                }
            }

            public bool showChildren = true;

            public Member parent;
            public int id;
            public Project project;

            public string name;

            public string sourceCollection;

            public UIPopup hierarchyObject;
            public RectTransform rectTransform;

            public SetTextDelegate setDisplayName;
            public RectTransform parentMask;

            public bool isHidden;
            public SwoleGameObject swoleGameObject;

            public float clickStart;
            public float clickLast;

            public UIPopup activeEditorWindow;

            protected Canvas canvas; 
            public Canvas Canvas
            {
                get
                {
                    if (canvas == null) canvas = rectTransform == null ? null : rectTransform.GetComponentInParent<Canvas>(true);
                    return canvas;
                }
            }

            public void SetName(string name)
            {
                this.name = name;
                if (swoleGameObject != null) swoleGameObject.name = name; 
                Refresh(false);
            }

            public bool ScreenPosIsInParentMask(UnityEngine.Vector3 screenPos) => IsInParentMask(Canvas.transform.TransformPoint(Canvas.ScreenToCanvasSpace(screenPos)));
            public bool IsInParentMask(UnityEngine.Vector3 worldPos)
            {
                return parentMask == null && rectTransform.ContainsWorldPosition(worldPos) || parentMask != null && parentMask.ContainsWorldPosition(worldPos);
            }

            public bool IsChildOf(Member parent)
            {
                if (this.parent == null || parent == null) return false;
                if (this.parent == parent) return true;
                return this.parent.IsChildOf(parent);
            }

            public bool HasInHierarchy(Member child)
            {
                if (child == this) return true;
                foreach (var c in children) if (c.HasInHierarchy(child)) return true;
                return false;
            }

            private List<Member> children = new List<Member>();
            public int ChildCount => children.Count;
            public void AddChild(Member child, bool refresh = true)
            {
                if (child == null || IsChildOf(child)) return;
                child.parent = this;
                if (child.swoleGameObject != null && swoleGameObject != null) child.swoleGameObject.transform.SetParent(swoleGameObject.transform, true);
                if (children.Contains(child)) return;
                children.Add(child);
                child.rectTransform.gameObject.SetActive(showChildren);
                if (refresh) Refresh();
            }
            public void RemoveChild(Member child, bool refresh = true)
            {
                if (child == null) return;
                if (child.parent == this) child.parent = null;
                if (refresh && children.RemoveAll(i => i == child) > 0) Refresh();
            }

            public int GetDepth()
            {
                return parent == null ? 0 : parent.GetDepth() + 1;
            }

            public void Refresh(bool refreshChildren = true)
            {
                if (parent == null) setDisplayName(name);
                if (showChildren)
                {
                    int depth = GetDepth() + 1;
                    string indent = new string(' ', depth) + (depth > 0 ? "+ " : "");
                    for (int a = 0; a < children.Count; a++)
                    {
                        var child = children[a];
                        child.rectTransform.gameObject.SetActive(true);
                        child.setDisplayName(indent + child.name);
                        if (refreshChildren) child.Refresh();
                    }
                }
                else
                {
                    for (int a = 0; a < children.Count; a++)
                    {
                        var child = children[a];
                        child.rectTransform.gameObject.SetActive(false);
                    }
                }
            }

            public int GetSiblingIndexStart() => rectTransform.GetSiblingIndex();
            public int GetSiblingIndexEnd()
            {
                int max = GetSiblingIndexStart();
                foreach (var child in children) max = Mathf.Max(max, child.GetSiblingIndexEnd());

                return max;
            }

            public int GetChildSiblingIndexStart(int childIndex)
            {
                if (childIndex < 0 || childIndex >= ChildCount) return GetSiblingIndexEnd();
                return children[childIndex].GetSiblingIndexStart();
            }

            public int GetChildSiblingIndexEnd(int childIndex)
            {
                if (childIndex < 0 || childIndex >= ChildCount) return GetSiblingIndexEnd();
                return children[childIndex].GetSiblingIndexEnd();
            }

            public void SetSiblingIndex(int index, bool refresh = true)
            {

                int startIndex = GetSiblingIndexStart();
                int offsetIndex = index - startIndex;

                rectTransform.SetSiblingIndex(index);
                foreach (var child in children) child.SetSiblingIndex(child.GetSiblingIndexStart() + offsetIndex);

                if (refresh) Refresh(false);

            }

            public void Dispose()
            {
                if (hierarchyObject != null)
                {
                    hierarchyObject.OnClick.RemoveAllListeners();
                    GameObject.Destroy(hierarchyObject.gameObject);
                }
            }
            public void Destroy()
            {
                Dispose();
                if (swoleGameObject != null) GameObject.Destroy(swoleGameObject.gameObject);
            }

        }

        public struct GameObjectState
        {
            public GameObject gameObject;
            public bool activeState;
        }

        protected struct TileToSpawn
        {
            public int id;
            public TilePrototype prototype;
            public Transform root;
        }
        protected struct CreationToSpawn
        {
            public int id;
            public CreationPrototype prototype;
            public Transform root;
        }
        protected static readonly List<ObjectSpawnGroup> _spawnGroups = new List<ObjectSpawnGroup>();

        protected static readonly List<TileToSpawn> _tilePrototypes = new List<TileToSpawn>();
        protected static readonly List<CreationToSpawn> _creationPrototypes = new List<CreationToSpawn>();

        protected static readonly List<ObjectSpawner> _objectSpawners = new List<ObjectSpawner>();

        protected static readonly Dictionary<int, Vector2Int> _objectIds = new Dictionary<int, Vector2Int>();
        public class Project : IDisposable
        {

            private static List<GameObject> _tempGameObjects = new List<GameObject>();
            private static List<SwoleGameObject> _tempSwoleGameObjects = new List<SwoleGameObject>();
            public Creation.ProjectSettings GetSettings()
            {
                Creation.ProjectSettings settings = default;

                var editorCam = builder == null ? Camera.main : builder.MainCamera; // the default main camera in the unity scene
                if (editorCam == null)
                {
                    settings.cameraPosition = new EngineInternal.Vector3(0, 2, -10);
                    settings.cameraRotation = EngineInternal.Quaternion.identity;
                } 
                else
                {
                    var t = editorCam.transform;
                    settings.cameraPosition = UnityEngineHook.AsSwoleVector(t.position);
                    settings.cameraRotation = UnityEngineHook.AsSwoleQuaternion(t.rotation);
                }

                if (builder != null)
                {
                    var prefabHandler = builder.PrefabHandler;
                    if (prefabHandler != null)
                    {
                        settings.selectedCollection = prefabHandler.ActiveCollectionIndex;
                        settings.importedCollections = new PrefabCollectionSource[prefabHandler.CollectionCount];
                        for (int a = 0; a < prefabHandler.CollectionCount; a++) settings.importedCollections[a] = prefabHandler.GetCollectionSource(a);
                    }

                    var runtimeEditor = builder.runtimeEditor;
                    if (runtimeEditor != null)
                    {
                        settings.transformGizmoState = runtimeEditor.CurrentGizmoId;

                        _tempGameObjects.Clear();
                        _tempSwoleGameObjects.Clear();
                        runtimeEditor.QueryActiveSelection(_tempGameObjects);
                        foreach (var obj in _tempGameObjects)
                        {
                            if (obj == null) continue;
                            var sgo = obj.GetComponentInParent<SwoleGameObject>();
                            if (sgo != null && !_tempSwoleGameObjects.Contains(sgo) && IsAssociatedWith(sgo)) _tempSwoleGameObjects.Add(sgo);  
                        }
                        settings.selectedObjects = new int[_tempSwoleGameObjects.Count];
                        for (int a = 0; a < _tempSwoleGameObjects.Count; a++) 
                        {
                            var sgo = _tempSwoleGameObjects[a];
                            if (sgo == null) continue;
                            settings.selectedObjects[a] = sgo.id; 
                        }
                        _tempSwoleGameObjects.Clear();
                        _tempGameObjects.Clear();
                    }
                }

                settings.objectStates = MemberStates;
                settings.windowStates = WindowStates;

                return settings;
            }
            public void ApplySettings(Creation.ProjectSettings? settings)
            {
                var editorCam = builder == null ? Camera.main : builder.MainCamera; // the default main camera in the unity scene
                if (editorCam != null && settings.HasValue)
                {
                    var t = editorCam.transform;
                    t.position = UnityEngineHook.AsUnityVector(settings.Value.cameraPosition);
                    t.rotation = UnityEngineHook.AsUnityQuaternion(settings.Value.cameraRotation);
                }

                if (builder != null)
                {
                    var prefabHandler = builder.PrefabHandler;
                    if (prefabHandler != null)
                    {
                        prefabHandler.ClearCollections();
                        if (settings.HasValue && settings.Value.importedCollections != null)
                        {
                            foreach(var collection in settings.Value.importedCollections)
                            {
                                if (string.IsNullOrWhiteSpace(collection.id)) continue;
                                if (collection.isPackage)
                                {
                                    prefabHandler.AddPackageContent(collection.id, true, false);
                                } 
                                else
                                {
                                    prefabHandler.AddTileCollection(collection.id, false);  
                                }
                            }
                        }
                        if (settings.HasValue) prefabHandler.ActiveCollectionIndex = settings.Value.selectedCollection;
                    }

                    var runtimeEditor = builder.runtimeEditor;
                    if (runtimeEditor != null)
                    {
                        runtimeEditor.DeselectAll(null, false);
                        if (settings.HasValue && settings.Value.selectedObjects != null)
                        {
                            foreach (var selectedId in settings.Value.selectedObjects)
                            {
                                if (members != null)
                                {
                                    foreach(var mem in members)
                                    {
                                        if (mem == null || mem.swoleGameObject == null) continue; 
                                        if (mem.swoleGameObject.id == selectedId) runtimeEditor.Select(mem.swoleGameObject.gameObject, false);
                                    }
                                }
                            }
                        }
                        if (settings.HasValue) runtimeEditor.CurrentGizmoId = settings.Value.transformGizmoState;
                    }
                }
                
                MemberStates = settings.HasValue ? settings.Value.objectStates : null;
                WindowStates = settings.HasValue ? settings.Value.windowStates : null;
            }

            public Creation.ProjectSettings? cachedSettings;

            public PackageInfo? packageInfo;
            public ContentInfo? contentInfo;

            public Sprite previewSprite;
            public ImageAsset previewTextureAsset;
            public ImageAsset prevPreviewTextureAsset;
            public string PreviewTextureName => name + "_preview";
            public bool previewTextureAssetIsNew;
            public void ConvertPreviewSpriteToPreviewTextureAsset()
            {
                ImageAsset asset = new ImageAsset(PreviewTextureName, contentInfo.HasValue ? contentInfo.Value.author : string.Empty, DateTime.Now, DateTime.Now, string.Empty, previewSprite, previewSprite == null ? false : ExternalAssets.CanTextureBeCompressed(previewSprite.texture.width, previewSprite.texture.height), packageInfo.HasValue ? packageInfo.Value : default, false);
                SetPreviewImage(asset);
                previewSprite = null;
                previewTextureAssetIsNew = true;  
            }

            public void SetPreviewImage(Sprite sprite)
            {
                previewSprite = sprite;

                ContentManager.LocalPackage pkg = null;
                if (packageInfo.HasValue) pkg = ContentManager.FindLocalPackage(packageInfo.Value);

                if (pkg != null) ConvertPreviewSpriteToPreviewTextureAsset();
            }
            public void SetPreviewImage(ImageAsset asset)
            {
                if (!previewTextureAssetIsNew)
                {
                    prevPreviewTextureAsset = previewTextureAsset;
                }
                else if (previewTextureAsset != null) previewTextureAsset.Dispose(); 

                previewTextureAsset = asset; 
            }

            public CreationBuilder builder;

            [NonSerialized]
            protected Creation cachedAsset;
            public Creation CachedAsset => cachedAsset;
            public Creation GetCreation(bool refreshSettings = true) => GetCreation(packageInfo.HasValue ? packageInfo.Value : EditPackageState.TargetPackage, contentInfo.HasValue ? contentInfo.Value.author : EditPackageState.CurrentAuthor, refreshSettings);
            public Creation GetCreation(PackageInfo package, string author = null, bool refreshSettings = true)
            {
                if (cachedAsset != null && hasAsset)
                {
                    var info = cachedAsset.ContentInfo;
                    var projectInfo = contentInfo.HasValue ? contentInfo.Value : info;
                    projectInfo.name = name;
                    if (author != null) projectInfo.author = author;

                    if (info != projectInfo)
                    {
                        cachedAsset = (Creation)cachedAsset.CreateShallowCopyAndReplaceContentInfo(projectInfo); 
                    }

                    cachedAsset.previewTexturePath = previewTexturePath;
                    if (refreshSettings) cachedAsset.projectSettings = GetSettings(); else if (cachedSettings.HasValue) cachedAsset.projectSettings = cachedSettings.Value; 

                    return cachedAsset;
                }

                _objectIds.Clear();
                _spawnGroups.Clear();
                _tilePrototypes.Clear();
                _creationPrototypes.Clear();
                foreach(var mem in members) if (mem != null && mem.swoleGameObject != null)
                    {
                        var tilePrototype = mem.swoleGameObject.GetComponentInChildren<TilePrototype>(true);
                        if (tilePrototype != null && tilePrototype.tileSet != null)
                        {
                            _tilePrototypes.Add(new TileToSpawn() { id = mem.swoleGameObject.id + 1, prototype = tilePrototype, root = mem.swoleGameObject.transform });
                        }
                        var creationPrototype = mem.swoleGameObject.GetComponentInChildren<CreationPrototype>(true);
                        if (creationPrototype != null && creationPrototype.asset != null)
                        {
                            _creationPrototypes.Add(new CreationToSpawn() { id = mem.swoleGameObject.id + 1, prototype = creationPrototype, root = mem.swoleGameObject.transform });
                        }
                    }

                while(_tilePrototypes.Count > 0)
                {
                    TileSet tileSet = _tilePrototypes[0].prototype.tileSet;
                    _objectSpawners.Clear();
                    foreach (var prototype in _tilePrototypes) 
                    {
                        if (prototype.prototype.tileSet == tileSet) 
                        {
                            _objectIds[prototype.root.GetInstanceID()] = new Vector2Int(_spawnGroups.Count, _objectSpawners.Count);
                            _objectSpawners.Add(new ObjectSpawner() { name = prototype.root.name, id = prototype.id, positionInRoot = UnityEngineHook.AsSwoleVector(prototype.root.position), rotationInRoot = UnityEngineHook.AsSwoleQuaternion(prototype.root.rotation), localScale = UnityEngineHook.AsSwoleVector(prototype.root.localScale), index = prototype.prototype.tileIndex }); 
                        }
                    }
                    _tilePrototypes.RemoveAll(i => i.prototype.tileSet == tileSet); 

                    TileSpawnGroup group = new TileSpawnGroup(tileSet.ID, _objectSpawners, tileSet.CollectionID);  
                    _spawnGroups.Add(group);  
                }

                while (_creationPrototypes.Count > 0)
                {
                    Creation asset = _creationPrototypes[0].prototype.asset;
                    _objectSpawners.Clear();
                    foreach (var prototype in _creationPrototypes)
                    {
                        if (prototype.prototype.asset == asset) 
                        {
                            _objectIds[prototype.root.GetInstanceID()] = new Vector2Int(_spawnGroups.Count, _objectSpawners.Count);
                            _objectSpawners.Add(new ObjectSpawner() { name = prototype.root.name, id = prototype.id, positionInRoot = UnityEngineHook.AsSwoleVector(prototype.root.position), rotationInRoot = UnityEngineHook.AsSwoleQuaternion(prototype.root.rotation), localScale = UnityEngineHook.AsSwoleVector(prototype.root.localScale) });      
                        }
                    }
                    _creationPrototypes.RemoveAll(i => i.prototype.asset == asset);

                    CreationSpawnGroup group = new CreationSpawnGroup(asset.PackageInfo.GetIdentityString(), asset.Name, _objectSpawners);
                    _spawnGroups.Add(group); 
                }

                foreach (var mem in members) if (mem != null && mem.swoleGameObject != null) // Fill parent indices
                    {
                        var t = mem.swoleGameObject.transform;
                        var parent = t.parent;
                        if (parent == null) continue;

                        if (!_objectIds.TryGetValue(t.GetInstanceID(), out var indices) || !_objectIds.TryGetValue(parent.GetInstanceID(), out var parentIndices)) continue;

                        var spawnGroup = _spawnGroups[indices.x];
                        var spawner = spawnGroup.GetObjectSpawnerUnsafe(indices.y);

                        spawner.parentSpawnerIndex = parentIndices.x + 1; // Add one because zero is undefined
                        spawner.parentInstanceIndex = parentIndices.y + 1; // Add one because zero is undefined
                        spawnGroup.ForceSetObjectSpawner(indices.y, spawner); 
                    }

                cachedAsset = new Creation(name, string.IsNullOrWhiteSpace(author) ? (string.IsNullOrWhiteSpace(info.author) ? package.curator : info.author) : author, string.IsNullOrWhiteSpace(info.creationDate) ? DateTime.Now : ContentExtensions.ConvertDateStringToDateTime(info.creationDate), DateTime.Now, info.description, script, curves, _spawnGroups, previewTexturePath, package);
                if (refreshSettings) cachedAsset.projectSettings = GetSettings(); else if (cachedSettings.HasValue) cachedAsset.projectSettings = cachedSettings.Value;
                hasAsset = true;
                return cachedAsset;
            }

            public string name;
            protected string prevName;

            public string DisplayName => isDirty ? ("*"+name) : name;

            [NonSerialized]
            protected bool isDirty;
            [NonSerialized]
            protected bool hasAsset;

            public bool IsDirty => isDirty;
            public void MarkAsDirty()
            {
                MarkProjectAsDirty();
                hasAsset = false; 
            }
            public void MarkProjectAsDirty()
            {
                isDirty = true;
                if (builder != null) builder.RefreshProjectTabs(); 
            }
            public void Clean() 
            { 
                isDirty = false;
                if (builder != null) builder.RefreshProjectTabs();  
            }

            public GameObject tabObject;

            protected ContentInfo info;
            public ContentInfo Info
            {
                get => info;
                set
                {
                    info = value;
                    MarkAsDirty(); 
                }
            }

            protected string previewTexturePath;
            public string PreviewTexturePath
            {
                get => previewTexturePath;
                set
                {
                    previewTexturePath = value;
                    MarkAsDirty();
                }
            }

            protected List<ICurve> curves = new List<ICurve>();
            public int CurveCount => curves == null ? 0 : curves.Count;
            public ICurve GetCurve(int index) => curves == null || index < 0 || index >= curves.Count ? null : curves[index];
            public void AddCurve(ICurve curve)
            {
                if (curve == null) return;

                if (curves == null) curves = new List<ICurve>();
                curves.Add(curve);

                MarkAsDirty();
            }
            public bool RemoveCurve(ICurve curve)
            {
                if (curve == null || curves == null) return false;

                return curves.RemoveAll(i => ReferenceEquals(i, curve)) > 0; 
            }
            public bool TryGetCurve(string name, out ICurve curve, bool caseSensitive = false)
            {
                curve = null;
                if (curves == null) return false;

                if (!caseSensitive) name = name.AsID();
                for (int a = 0; a < curves.Count; a++)
                {
                    var c = curves[a];
                    if (c == null || (caseSensitive ? c.Name != name : c.Name.AsID() != name)) continue; 

                    curve = c;
                    return true;
                }
                return false; ;
            }

            protected CreationScript script;
            public CreationScript Script
            {
                get => script;
                set
                {
                    if (script != value) MarkAsDirty();
                    script = value;
                }
            }
            protected Dictionary<string, ResizableWindowState> windowStates = new Dictionary<string, ResizableWindowState>();
            public ResizableWindowState GetWindowState(string id)
            {
                if (windowStates.TryGetValue(id, out var state)) return state;
                return default;
            }
            public bool TryGetWindowState(string id, out ResizableWindowState state)
            {
                return windowStates.TryGetValue(id, out state);
            }
            public void SetWindowState(string id, ResizableWindowState state) 
            {
                state.id = id;
                windowStates[id] = state;
                MarkProjectAsDirty(); 
            }
            public ResizableWindowState[] WindowStates 
            { 
                get
                {
                    var temp = windowStates.Values;
                    var array = new ResizableWindowState[temp.Count];
                    temp.CopyTo(array, 0); 
                    return array;
                }

                set
                {
                    if (builder == null) return;

                    var activeProj = builder.ActiveProject;
                    bool isActiveProj = ReferenceEquals(activeProj, this);

                    if (isActiveProj) builder.CloseFloatingWindows();
                    if (windowStates == null) return;

                    windowStates.Clear();
                    if (value != null) 
                        foreach (var state in value) 
                            if (!string.IsNullOrWhiteSpace(state.id))
                            {
                                windowStates[state.id] = state;
                                if (state.id == windowStateID_objectPalette)
                                {
                                    var palette = !isActiveProj || builder.PrefabHandler == null ? null : builder.prefabHandler.tilePickerWindow;
                                    if (palette != null)
                                    {
                                        var paletteRT = palette.GetComponent<RectTransform>();
                                        if (paletteRT != null) state.ApplyWindowState(paletteRT);
                                        if (state.visible) builder.OpenPrefabPalette(this);
                                    }
                                }
                                else if (Enum.TryParse<ExecutionLayer>(state.id, true, out var layer))
                                {
                                    if (!activeCodeEditorWindows.TryGetValue(layer, out var editorWindowRT))
                                    {
                                        if (state.visible)
                                        {
                                            builder.StartEditingCode(layer, this);
                                            activeCodeEditorWindows.TryGetValue(layer, out editorWindowRT);
                                        }
                                    }

                                    if (editorWindowRT != null) state.ApplyWindowState(editorWindowRT);
                                }
                                else if (state.id == windowStateID_curvesMenu)
                                {
                                    var menu = !isActiveProj || builder.curveWindow == null ? null : builder.curveWindow;
                                    if (menu != null)
                                    {
                                        var menuRT = menu.GetComponent<RectTransform>();
                                        if (menuRT != null) state.ApplyWindowState(menuRT);
                                        if (state.visible) builder.OpenCurveMenu(this); 
                                    }
                                }
                                else if (state.id == windowStateID_animationCurveEditor)
                                {
                                    var menu = !isActiveProj || builder.animationCurveEditorWindow == null ? null : builder.animationCurveEditorWindow;
                                    if (menu != null)
                                    {
                                        var menuRT = menu.GetComponent<RectTransform>();
                                        if (menuRT != null) state.ApplyWindowState(menuRT);
                                        if (state.visible && TryGetCurve(state.dataStringA, out var curve))
                                        {
                                            builder.StartEditingCurve(curve, this);
                                        }
                                    }
                                }
                                else if (state.id == windowStateID_bezierCurveEditor)
                                {
                                    var menu = !isActiveProj || builder.bezierCurveEditorWindow == null ? null : builder.bezierCurveEditorWindow;
                                    if (menu != null)
                                    {
                                        var menuRT = menu.GetComponent<RectTransform>();
                                        if (menuRT != null) state.ApplyWindowState(menuRT);
                                        if (state.visible && TryGetCurve(state.dataStringA, out var curve)) 
                                        {
                                            builder.StartEditingCurve(curve, this);
                                        }
                                    }
                                }
                            }
                }
            }

            public bool IsAssociatedWith(GameObject obj) => IsAssociatedWith(obj == null ? null : obj.transform);
            public bool IsAssociatedWith(SwoleGameObject sgo) => IsAssociatedWith(sgo == null ? null : sgo.transform);
            public bool IsAssociatedWith(Transform transform)
            {
                if (transform == null) return false;

                for(int a = 0; a < MemberCount; a++)
                {
                    var mem = members[a];
                    if (mem == null) continue;

                    if (mem.swoleGameObject.gameObject == transform.gameObject) return true;
                    if (mem.swoleGameObject.transform.IsChildOf(transform)) return true;
                    if (transform.IsChildOf(mem.swoleGameObject.transform)) return true;
                }

                return false;
            }

            public List<Member> members = new List<Member>();
            public int MemberCount => members == null ? 0 : members.Count;

            public Creation.ObjectPreloadState[] MemberStates  
            {
                get
                {
                    return null;
                    // TODO: Might need this later
                    /*
                    var array = new Creation.ObjectPreloadState[members == null ? 0 : members.Count];
                    if (members != null)
                    {
                        for(int a = 0; a < members.Count; a++)
                        {
                            var mem = members[a];
                            if (mem == null) continue;
                            array[a] = mem.State;
                        }
                    }
                    return array;*/
                }

                set
                {
                    if (members == null) return;

                    if (value != null) 
                    { 
                        for(int a = 0; a < value.Length; a++)
                        {
                            var state = value[a];
                            if (state.id < 0) continue;

                            for(int b = 0; b < members.Count; b++)
                            {
                                var mem = members[b];
                                if (mem == null || mem.swoleGameObject == null || mem.swoleGameObject.id != state.id) continue;

                                mem.State = state;
                            }
                        }
                    }
                }
            }

            public int GetIdAtIndex(int index)
            {
                if (index < 0 || index >= members.Count) return -1;
                return members[index].id;
            }

            public Member GetMemberById(int id)
            {
                for (int a = 0; a < members.Count; a++) if (members[a].id == id) return members[a];
                return null;
            }
            public Member GetMemberByIndex(int index)
            {
                if (index >= 0 && index < MemberCount) return members[index];
                return null;
            }

            public Member GetMember(SwoleGameObject rootObject)
            {
                for (int a = 0; a < members.Count; a++) if (members[a].swoleGameObject == rootObject) return members[a];
                return null;
            }
            public Member GetMember(GameObject rootObject)
            {
                for (int a = 0; a < members.Count; a++) if (members[a].swoleGameObject != null && members[a].swoleGameObject.gameObject == rootObject) return members[a];
                return null;
            }

            public Member GetMember(RectTransform rectTransform)
            {
                for (int a = 0; a < members.Count; a++) if (members[a].rectTransform == rectTransform) return members[a];
                return null;
            }

            public bool RemoveMemberById(int id) => RemoveMember(GetMemberById(id));
            public bool RemoveMemberByIndex(int index) => RemoveMember(GetMemberByIndex(index));

            public bool RemoveMember(Member member)
            {
                if (member == null) return false;

                members.RemoveAll(i => i == null || i == member);

                member.Dispose();

                return true;
            }

            public List<GameObjectState> associatedInterfaceObjects = new List<GameObjectState>();

            public Transform containerTransform;

            public Dictionary<ExecutionLayer, RectTransform> activeCodeEditorWindows = new Dictionary<ExecutionLayer, RectTransform>();

            public void Activate()
            {
                bool dirtyFlag = isDirty;

                associatedInterfaceObjects.RemoveAll(i => i.gameObject == null);
                foreach (var state in associatedInterfaceObjects) state.gameObject.SetActive(state.activeState);

                foreach (var pair in activeCodeEditorWindows) if (pair.Value != null) pair.Value.gameObject.SetActive(true);

                if (containerTransform != null) containerTransform.gameObject.SetActive(true);
                if (members != null)
                {
                    foreach (var member in members) if (member != null)
                        {
                            if (member.swoleGameObject != null) member.swoleGameObject.gameObject.SetActive(!member.isHidden);
                            if (member.hierarchyObject != null) member.hierarchyObject.gameObject.SetActive(true); 
                        }
                }

                ApplySettings(cachedSettings);
                isDirty = dirtyFlag;
            }

            public void Deactivate()
            {
                bool dirtyFlag = isDirty;

                cachedSettings = GetSettings();

                associatedInterfaceObjects.RemoveAll(i => i.gameObject == null);
                foreach (var state in associatedInterfaceObjects) state.gameObject.SetActive(false);

                foreach (var pair in activeCodeEditorWindows) if (pair.Value != null) pair.Value.gameObject.SetActive(false); 

                if (containerTransform != null) containerTransform.gameObject.SetActive(false);
                if (members != null)
                {
                    foreach (var member in members) if (member != null)
                        {
                            if (member.swoleGameObject != null) member.swoleGameObject.gameObject.SetActive(false);
                            if (member.hierarchyObject != null) member.hierarchyObject.gameObject.SetActive(false);
                        }
                }
                isDirty = dirtyFlag;
            }

            protected bool disposed;
            public bool IsValid => !disposed;
            public void Dispose()
            {
                disposed = true;
                if (activeCodeEditorWindows != null)
                {
                    foreach(var pair in activeCodeEditorWindows)
                    {
                        if (pair.Value == null) continue;

                        pair.Value.gameObject.SetActive(false);
                        if (builder == null || builder.codeEditorPool == null) GameObject.Destroy(pair.Value); else builder.codeEditorPool.Release(pair.Value);
                    }
                    activeCodeEditorWindows.Clear();
                }

                if (members != null)
                {
                    foreach (var member in members) if (member != null) member.Destroy();

                    members.Clear();
                }

                if (previewSprite != null)
                {
                    if (previewSprite.texture != null) GameObject.Destroy(previewSprite.texture);
                    GameObject.Destroy(previewSprite); 
                }
                previewSprite = null;
            }

        }

        protected int activeProjectIndex;
        protected Project ActiveProject
        {
            get
            {
                if (activeProjectIndex < 0 || activeProjectIndex >= openProjects.Count) return null;
                return openProjects[activeProjectIndex];
            }
        }

        protected readonly List<Project> openProjects = new List<Project>();
        public int ProjectCount => openProjects.Count;
        public Project this[int projectIndex] => projectIndex < 0 || projectIndex >= ProjectCount ? null : openProjects[projectIndex];

        public void SetActiveProject(int projectIndex)
        {
            if (IsInTestMode) EndTestMode();

            Project proj;
            if (activeProjectIndex != projectIndex)
            {
                proj = ActiveProject;
                if (proj != null)
                {
                    bool dirtyFlag = proj.IsDirty;
                    proj.Deactivate();

                    var tempProj = proj;
                    IEnumerator ResetDirtyFlag()
                    {
                        yield return null;
                        if (!dirtyFlag) tempProj.Clean();
                    }
                    StartCoroutine(ResetDirtyFlag());
                }
            }

            activeProjectIndex = -1;

            if (runtimeEditor != null) runtimeEditor.DeselectAll(null, false); 

            activeProjectIndex = projectIndex;

            proj = ActiveProject;
            if (proj != null)
            {
                bool dirtyFlag = proj.IsDirty;
                proj.Activate();

                var tempProj = proj; 
                IEnumerator ResetDirtyFlag()
                {
                    yield return null;
                    if (!dirtyFlag) tempProj.Clean();
                }
                StartCoroutine(ResetDirtyFlag());  
            }

            RefreshProjectTabSelection(); 
        }

        public Project CreateNewProject() => CreateNewProject("untitled"); 
        public Project CreateNewProject(string projectName, bool setAsActiveProject = false)
        {
            Project project = new Project() { name = projectName, builder = this };

            openProjects.Add(project);
            if (activeProjectIndex < 0 || setAsActiveProject) 
            {
                SetActiveProject(openProjects.Count - 1);
            }

            //RefreshProjectTabs();
            project.MarkAsDirty(); // refreshes project tabs
            return project;
        }
        public void CreateNewActiveProject() => CreateNewProject("untitled", true);
        public Project CreateNewActiveProject(string projectName) => CreateNewProject(projectName, true); 
        

        private static readonly List<EngineInternal.ITransform> _tempTransforms = new List<EngineInternal.ITransform>();
        public Project OpenProject(Creation creation, bool setAsActiveProject = true)
        {
            if (creation == null) return null;

            if (IsInTestMode) EndTestMode();

            int projectIndex = openProjects.Count;
            var proj = CreateNewProject(creation.Name);
            proj.packageInfo = creation.PackageInfo;
            proj.contentInfo = creation.ContentInfo;

            _tempTransforms.Clear();
            proj.Script = creation.Script;
            if (!string.IsNullOrWhiteSpace(creation.previewTexturePath))
            {
                proj.PreviewTexturePath = creation.previewTexturePath;
                if (creation.LocalPreviewTexture is ImageAsset asset)
                {
                    proj.previewTextureAsset = asset;
                    proj.previewTextureAssetIsNew = false; 
                }
            }
            CreationBehaviour.CreatePreRuntimeCreationPrefab(creation, true, true, null, _tempTransforms, null, false);
            foreach(var obj in _tempTransforms)
            {
                if (obj == null) continue;
                if (obj.baseGameObject.Instance is not GameObject go) continue;
                var sgo = go.GetComponent<SwoleGameObject>();
                if (sgo == null) continue;
                CreateNewMember(sgo, proj, activeProjectIndex == projectIndex, sgo.name);
            }

            if (creation.CurveCount > 0)
            {
                for (int a = 0; a < creation.CurveCount; a++) 
                {
                    var curve = creation.GetCurve(a);
                    if (curve == null) continue;

                    proj.AddCurve((ICurve)curve.Clone());    
                }
            }

            proj.cachedSettings = creation.projectSettings;

            if (setAsActiveProject) SetActiveProject(projectIndex);  
            
            proj.Clean();

            return proj;
        }
        public bool SaveProject(int index, VoidParameterlessDelegate onSave = null, VoidParameterlessDelegate onFail = null, VoidParameterlessDelegate onComplete = null)
        {
            if (index < 0 || index >= openProjects.Count) return false;
            return SaveProject(openProjects[index], onSave, onFail, onComplete);
        }
        public bool SaveProject(int index, VoidParameterlessDelegate onSave, VoidParameterlessDelegate onFail, VoidParameterlessDelegate onComplete, PackageInfo package, string author = null) 
        {
            if (index < 0 || index >= openProjects.Count) return false;
            return SaveProject(openProjects[index], onSave, onFail, onComplete, package, author);
        }
        public bool SaveProject(Project project, VoidParameterlessDelegate onSave = null, VoidParameterlessDelegate onFail = null, VoidParameterlessDelegate onComplete = null) => SaveProject(project, onSave, onFail, onComplete, project.packageInfo.HasValue ? project.packageInfo.Value : EditPackageState.TargetPackage, project.contentInfo.HasValue ? project.contentInfo.Value.author : EditPackageState.CurrentAuthor);
        protected bool SaveProject(Project project, VoidParameterlessDelegate onSave, VoidParameterlessDelegate onFail, VoidParameterlessDelegate onComplete, PackageInfo package, string author = null)
        {
            if (IsInTestMode) EndTestMode();
            if (project == null) 
            {
                swole.LogError("Tried to save null project!");
                onFail?.Invoke();
                onComplete?.Invoke();
                return false;
            }

            var lpkg = ContentManager.FindLocalPackage(package);
            if (lpkg == null || lpkg.Content == null)
            {
                swole.LogError($"Failed to save project '{project.name}' to invalid local package '{package}'!");
                onFail?.Invoke();
                onComplete?.Invoke();
                return false;
            }

            project.packageInfo = lpkg.Content.Manifest;

            var pkg = SwolePackage.Create(lpkg);
            if (project.previewSprite != null) project.ConvertPreviewSpriteToPreviewTextureAsset(); // Convert the waiting sprite to an image asset  

            try
            {
                if (project.prevPreviewTextureAsset is ImageAsset oldPreviewAsset)
                {
                    oldPreviewAsset.Delete(); // delete the old preview image asset if it exists
                    project.prevPreviewTextureAsset = null;
                }
            } 
            catch(Exception ex)
            {
                swole.LogError(ex);
            }

            if (project.previewTextureAssetIsNew) // preview texture was created this session
            {
                if (project.previewTextureAsset != null)
                {
                    if (project.previewTextureAsset.Name != project.PreviewTextureName)
                    {
                        project.previewTextureAsset.DeleteImagePathAsset(lpkg);// delete old preview texture from directory if the name changed
                        pkg.Remove(project.previewTextureAsset, false); // remove old preview texture asset from package if the name changed
                    }

                    var info = project.previewTextureAsset.contentInfo;
                    info.name = project.PreviewTextureName;
                    project.previewTextureAsset.contentInfo = info;
                    project.previewTextureAsset.packageInfo = project.packageInfo.Value;

                    project.PreviewTexturePath = project.previewTextureAsset.Name;
                    pkg.AddOrReplace(project.previewTextureAsset);

                    project.previewTextureAsset.ignoreSpriteOnSerialization = true;
                    project.previewTextureAsset.SaveTextureInPackageDirectory(lpkg, true);  
                }
                project.previewTextureAssetIsNew = false;
            }
            else if (project.previewTextureAsset != null) // preview texture was loaded in or saved previously
            {
                if (project.previewTextureAsset.Name != project.PreviewTextureName)
                {
                    project.previewTextureAsset.RenameImagePathAsset(lpkg, project.PreviewTextureName, true);
                    pkg.Remove(project.previewTextureAsset, false); // remove old preview texture asset from package if the name changed
                }

                var info = project.previewTextureAsset.contentInfo;
                info.name = project.PreviewTextureName;
                project.previewTextureAsset.contentInfo = info;
                project.previewTextureAsset.packageInfo = project.packageInfo.Value;

                project.PreviewTexturePath = project.previewTextureAsset.Name;
                pkg.AddOrReplace(project.previewTextureAsset); 
            }

            if (project.CachedAsset != null && project.name != project.CachedAsset.Name) pkg.Remove(project.CachedAsset); // remove old project asset if the name changed

            project.MarkAsDirty();
            var projectAsset = project.GetCreation(package, author, ReferenceEquals(project, ActiveProject));    
            pkg.AddOrReplace(projectAsset); 
            lpkg.Content = pkg;  
            if (!ContentManager.SavePackage(lpkg))
            {
                swole.LogError($"Failed to save project '{project.name}' to local package '{package}'!");
                onFail?.Invoke();
                onComplete?.Invoke();
                return false;
            }
            project.Clean();

            onSave?.Invoke();
            onComplete?.Invoke();
            return true;
        }

        public void CloseProject(int index) => CloseProject(index, null, null, null);
        public void CloseProject(int index, VoidParameterlessDelegate OnFullyClose, VoidParameterlessDelegate OnCancel, VoidParameterlessDelegate OnComplete) 
        {
            if (index < 0 || index >= openProjects.Count) return;
            CloseProject(openProjects[index], OnFullyClose, OnCancel, OnComplete); 
        }
        protected void CloseProject(Project project) => CloseProject(project, null, null, null);
        protected void CloseProject(Project project, VoidParameterlessDelegate OnFullyClose, VoidParameterlessDelegate OnCancel, VoidParameterlessDelegate OnComplete)
        {
            if (IsInTestMode) EndTestMode();
             
            void FullyClose()
            {
                int index = openProjects.IndexOf(project);
                if (index >= 0)
                {
                    if (index == activeProjectIndex && openProjects.Count > 1)
                    {
                        int nextActiveIndex = index >= openProjects.Count - 1 ? (index - 1) : (index + 1);
                        nextActiveIndex = Mathf.Clamp(nextActiveIndex, 0, openProjects.Count - 1); 

                        SetActiveProject(nextActiveIndex);    
                    }
                     
                    openProjects.RemoveAt(index);
                    if (index < activeProjectIndex)
                    {
                        activeProjectIndex--;
                    }
                    else
                    {
                        activeProjectIndex = Mathf.Clamp(activeProjectIndex, 0, openProjects.Count - 1);
                    }

                    if (openProjects == null || activeProjectIndex < 0 || activeProjectIndex >= openProjects.Count) // No projects are open
                    {
                        CloseFloatingWindows();  
                    }
                }

                RefreshProjectTabs();

                project.Dispose();
                OnFullyClose?.Invoke();
                OnComplete?.Invoke();
            }
            
            if (project.IsDirty)
            {
                IEnumerator WaitForPrompt()
                {
                    while (true)
                    {
                        if (ShowPopupYesNoCancel("SAVE PROJECT", $"Project '{project.name}' has unsaved changes! Do you want to save them before closing?", () => { TryQuickSave(project, true, null, null, FullyClose); }, FullyClose, () =>
                        {
                            OnCancel?.Invoke();
                            OnComplete?.Invoke();
                        })) break;  
                        yield return null;
                    }
                }
                StartCoroutine(WaitForPrompt()); 
            }
            else FullyClose();
        }

        private const string str_name = "Name";
        private const string str_parentMask = "ParentMask"; 

        [Header("Main Setup")]
        public Canvas canvasMain;
        public Canvas canvasElevated;
        protected RectTransform canvasMainTransform;
        protected RectTransform canvasElevatedTransform;

        public SceneSwap sceneSwapper;

        #region Project Tabs
        public HorizontalLayoutGroup creationTabsLayoutGroup;
        protected RectTransform creationTabsTransform;
        public GameObject creationTabPrototype;
        protected PrefabPool creationTabPool;

        protected const string str_active = "active";
        protected const string str_close = "close";

        protected readonly List<GameObject> projectTabs = new List<GameObject>();
        public void RefreshProjectTabs() 
        {
            foreach (var tab in projectTabs) if (tab != null) 
                { 
                    creationTabPool.Release(tab);
                    tab.SetActive(false);
                }
            projectTabs.Clear();

            for(int a = 0; a < openProjects.Count; a++)
            {
                var proj = openProjects[a];
                if (!creationTabPool.TryGetNewInstance(out GameObject inst)) break;

                inst.SetActive(true);

                int index = a;
                CustomEditorUtils.SetComponentText(inst, proj == null ? "null" : proj.DisplayName);   
                CustomEditorUtils.SetButtonOnClickAction(inst, () => SetActiveProject(index));
                CustomEditorUtils.SetButtonOnClickActionByName(inst, str_close, () => CloseProject(proj));

                projectTabs.Add(inst);
            }

            RefreshProjectTabSelection();
        }
        public void RefreshProjectTabSelection()
        {
            for (int a = 0; a < projectTabs.Count; a++)
            {
                var tab = projectTabs[a];
                if (tab == null) continue;

                var active = tab.transform.FindDeepChildLiberal(str_active);
                if (active != null) active.gameObject.SetActive(a == activeProjectIndex);
            }
        }
        #endregion

        public VerticalLayoutGroup hierarchyLayoutGroup;
        protected RectTransform hierarchyTransform;

        [Tooltip("A prefab for a UI object that represents a scene object in the creation hierarchy. Will look for a text object called \"Name\" to display the name of the member. Also looks for a RectTransform called \"ParentMask\" to act as a region for drag and drop parenting.")]
        public UIPopup hierarchyMemberPrototype;

        public Color hierarchyMemberTint = Color.white;
        public Color hierarchyMemberSelectedTint = Color.yellow;

        [Tooltip("A prefab for a UI object that is used as a placeholder object in the ui hierarchy when a member is being dragged around the screen.")]
        public Image hierarchyPlaceholderPrototype;
        [Tooltip("Don't instantiate the placeholder prototype. Check this if the prototype is stored in the scene instead of in the project.")]
        public bool skipInstantiatePlaceholderPrototype;

        [Tooltip("A prefab for a UI graphic that displays where a hovering member will be slotted into the hierarchy.")]
        public Image hierarchySlotInPrototype;
        [Tooltip("Don't instantiate the slotIn prototype. Check this if the prototype is stored in the scene instead of in the project.")]
        public bool skipInstantiateSlotInPrototype;

        [Tooltip("A prefab for a UI graphic that appears when a dragged member will be parented to a member below it.")]
        public Image hierarchyParentToPrototype;
        [Tooltip("Don't instantiate the parentTo prototype. Check this if the prototype is stored in the scene instead of in the project.")]
        public bool skipInstantiateParentToPrototype;

        [Tooltip("Maximum amount of windows that can be open at one time. These windows are those like the code editor or object editor.")]
        public int maxOpenWindowsPerType = 15;

        #region Editor Windows

        public void OpenStatefulEditorWindow(string windowStateID, Project proj, GameObject editorWindow, out RectTransform editorWindowRT, out Canvas canvas, out RectTransform canvasRT)
        {
            editorWindow.SetActive(true);

            editorWindowRT = editorWindow.GetComponent<RectTransform>();
            editorWindowRT.SetAsLastSibling();

            canvas = editorWindow.GetComponentInParent<Canvas>();
            canvasRT = canvas == null ? null : canvas.GetComponent<RectTransform>();

            OpenStatefulEditorWindow(windowStateID, proj, editorWindow, editorWindowRT, canvasRT);
        }
        public void OpenStatefulEditorWindow(string windowStateID, Project proj, GameObject editorWindow) => OpenStatefulEditorWindow(windowStateID, proj, editorWindow, out _, out _, out _);
        public void OpenStatefulEditorWindow(string windowStateID, Project proj, GameObject editorWindow, RectTransform editorWindowRT, RectTransform canvasRT)
        {
            if (proj == null) return;

            UIResizer editorResizer = null;
            UIPopup editorPopup = null;


            if (proj.TryGetWindowState(windowStateID, out var state))
            {
                state.visible = true;
                state.ApplyWindowState(editorWindowRT, canvasRT);
                proj.SetWindowState(windowStateID, state);
            }

            void OnClose()
            {
                if (proj == null || !proj.IsValid)
                {
                    proj = null;
                    return;
                }

                proj.SetWindowState(windowStateID, editorWindowRT.AsResizableWindowState(false, canvasRT));
                if (editorPopup != null) editorPopup.OnDragStep?.RemoveAllListeners();
                if (editorResizer != null) editorResizer.OnDragStep?.RemoveAllListeners();
                editorPopup = null;
                editorResizer = null;
            }
            void OnDrag()
            {
                if (proj == null || !proj.IsValid)
                {
                    proj = null;
                    return;
                }

                proj.SetWindowState(windowStateID, editorWindowRT.AsResizableWindowState(true, canvasRT));
            }
            editorPopup = editorWindow.GetComponentInChildren<UIPopup>(true);
            if (editorPopup != null)
            {
                if (editorPopup.OnDragStep == null) editorPopup.OnDragStep = new UnityEvent(); else editorPopup.OnDragStep.RemoveAllListeners();
                editorPopup.OnDragStep.AddListener(OnDrag);

                if (editorPopup.OnClose == null) editorPopup.OnClose = new UnityEvent(); else editorPopup.OnClose.RemoveAllListeners();
                editorPopup.OnClose.AddListener(OnClose);
            }
            else
            {
                CustomEditorUtils.SetButtonOnClickActionByName(editorWindow, str_close, () =>
                {
                    OnClose();
                    editorWindow.SetActive(false);
                }, true, true, false);
            }

            editorResizer = editorWindow.GetComponentInChildren<UIResizer>(true);
            if (editorResizer != null)
            {
                if (editorResizer.OnDragStep == null) editorResizer.OnDragStep = new UnityEvent(); else editorResizer.OnDragStep.RemoveAllListeners();
                editorResizer.OnDragStep.AddListener(OnDrag);
            }

            proj.SetWindowState(windowStateID, editorWindowRT.AsResizableWindowState(true, canvasRT));
        }

        [Tooltip("A window for editing an object's properties, such as name and transform.")]
        public UIPopup objectEditorWindow;
        protected PrefabPool objectEditorPool;
        [Tooltip("A window for editing SwoleScript code.")]
        public UIPopup codeEditorWindow;
        protected PrefabPool codeEditorPool;

        [Tooltip("A window for setting the preview image for a creation asset.")]
        public GameObject setPreviewImageWindow;

        private const string str_keep = "keep";
        private const string str_discard = "discard";
        private const string str_preview = "preview";
        public void SetPreviewImage() => SetPreviewImage(ActiveProject);
        public void SetPreviewImage(Project project)
        { 
            if (project == null) project = ActiveProject;

            Vector2 startCoords, endCoords;
            if (Screen.width >= Screen.height)
            {
                float aspectRatio = Screen.height / (float)Screen.width;
                startCoords = new Vector2(0.5f - (0.25f * aspectRatio), 0.25f);
                endCoords = new Vector2(0.5f + (0.25f * aspectRatio), 0.75f);
            } 
            else
            {
                float aspectRatio = Screen.width / (float)Screen.height;
                startCoords = new Vector2(0.25f, 0.5f - (0.25f * aspectRatio));
                endCoords = new Vector2(0.75f, 0.5f + (0.25f * aspectRatio));   
            }
            
            var screenshot = Utils.TakeScreenshot(startCoords, endCoords, true, false, Creation._defaultPreviewTextureSize, Creation._defaultPreviewTextureSize);
            Sprite sprite = Sprite.Create(screenshot, new Rect(0, 0, screenshot.width, screenshot.height), new Vector2(0.5f, 0.5f));       
            if (setPreviewImageWindow != null)
            {
                setPreviewImageWindow.SetActive(true); 
                setPreviewImageWindow.transform.SetAsLastSibling(); 

                var previewT = setPreviewImageWindow.transform.FindDeepChildLiberal(str_preview);
                if (previewT != null)
                {
                    var preview = previewT.GetComponentInChildren<Image>();
                    if (preview != null) 
                    {
                        preview.color = Color.white;
                        preview.sprite = sprite;  
                    }
                }

                void Keep()
                {
                    setPreviewImageWindow.gameObject.SetActive(false);
                    SetPreviewImage(sprite, project);
                }
                void Discard()
                {
                    setPreviewImageWindow.gameObject.SetActive(false);
                    GameObject.Destroy(screenshot);
                    GameObject.Destroy(sprite);
                }
                CustomEditorUtils.SetButtonOnClickActionByName(setPreviewImageWindow, str_keep, Keep);
                CustomEditorUtils.SetButtonOnClickActionByName(setPreviewImageWindow, str_discard, Discard);
                CustomEditorUtils.SetButtonOnClickActionByName(setPreviewImageWindow, str_close, Discard);  
            } 
            else
            {
                SetPreviewImage(sprite, project);
            }
        }
        public void SetPreviewImage(Sprite sprite) => SetPreviewImage(sprite, ActiveProject);
        public void SetPreviewImage(Sprite sprite, Project project)
        {
            if (project == null) project = ActiveProject;
            project?.SetPreviewImage(sprite);
        }
        public void SetPreviewImage(ImageAsset asset, Project project = null)
        {
            if (project == null) project = ActiveProject;
            project?.SetPreviewImage(asset);
        }

        [Tooltip("A window for opening a saved project.")]
        public GameObject loadProjectWindow;
        [Tooltip("A window for saving a project.")]
        public GameObject saveProjectWindow;

        private const string str_info = "info";

        private const string str_projectName = "projectName";
        private const string str_packageName = "packageName";

        private const string str_findPackage = "findPackage";

        private const string str_openProject = "openProject";
        private const string str_saveProject = "saveProject";

        private const string str_errorMessage = "errorMessage";

        public void OpenLoadProjectWindow()
        {
            if (loadProjectWindow == null) return;
            
            loadProjectWindow.gameObject.SetActive(true);

            var windowT = loadProjectWindow.transform;
            windowT.SetAsLastSibling();

            var infoT = windowT.FindDeepChildLiberal(str_info);

            Transform previewT = null;
            Image preview = null;
            if (infoT != null)
            {
                previewT = infoT.FindDeepChildLiberal(str_preview);
                if (previewT != null)
                {
                    preview = previewT.GetComponentInChildren<Image>(true);
                    preview.gameObject.SetActive(true); 
                    preview.sprite = null;
                    preview.enabled = false;
                }
                CustomEditorUtils.SetInputOrTextComponentTextByName(infoT, str_projectName, string.Empty); 
            }

            CustomEditorUtils.SetButtonOnClickActionByName(windowT, str_openProject, null, true, true, false);
            CustomEditorUtils.SetButtonInteractableByName(windowT, str_openProject, false, true, true); 

            var projList = loadProjectWindow.GetComponentInChildren<UICategorizedList>(true);
            if (projList != null)
            {
                projList.Clear(true);
                for(int a = 0; a < ContentManager.LocalPackageCount; a++)
                {
                    var lpkg = ContentManager.GetLocalPackage(a);
                    if (lpkg == null) continue;

                    var cat = projList.AddNewCategory(lpkg.GetIdentityString());
                    foreach(var content in lpkg.Content)
                    {
                        if (content == null || !typeof(Creation).IsAssignableFrom(content.AssetType)) continue;

                        bool alreadyOpen = false;
                        foreach(var openProj in this.openProjects)
                        {
                            if (openProj.name != null && content.Name != null && openProj.name.AsID() == content.Name.AsID() && openProj.packageInfo.HasValue && openProj.packageInfo.Value.GetIdentityString().AsID() == lpkg.GetIdentityString().AsID())
                            {
                                alreadyOpen = true;
                                break;
                            }
                        }

                        var contentRef = content;
                        void OnClick()
                        {
                            if (runtimeEditor != null) runtimeEditor.DisableClickSelectForTime(0.4f);  

                            CustomEditorUtils.SetInputOrTextComponentTextByName(infoT, str_projectName, contentRef.Name);
                            if (preview != null)
                            {
                                preview.enabled = false; 
                                if (contentRef.Asset != null) // Fully loads content
                                {
                                    var creation = (Creation)contentRef.Asset;
                                    if (creation.PreviewTexture is ImageAsset img)
                                    {
                                        preview.enabled = true;
                                        preview.sprite = img.Sprite; // get or create sprite
                                    }
                                }
                            }

                            void OpenProj()
                            {
                                loadProjectWindow.SetActive(false); 
                                OpenProject((Creation)contentRef.Asset, true);
                            }

                            CustomEditorUtils.SetButtonOnClickActionByName(windowT, str_openProject, OpenProj, true, true, false);
                            CustomEditorUtils.SetButtonInteractableByName(windowT, str_openProject, !alreadyOpen, true, true);     

                        }
                        projList.AddNewListMember(contentRef.Name, cat, OnClick);
                    }
                }
            }
        }

        public void OpenSaveProjectWindow() => OpenSaveProjectWindow(ActiveProject);
        public void OpenSaveProjectWindow(Project proj, VoidParameterlessDelegate onSave = null, VoidParameterlessDelegate onFail = null, VoidParameterlessDelegate onComplete = null)
        {
            if (proj == null || saveProjectWindow == null) 
            {
                onFail?.Invoke();
                onComplete?.Invoke();
                return; 
            }

            saveProjectWindow.gameObject.SetActive(true);

            var windowT = saveProjectWindow.transform;
            windowT.SetAsLastSibling();

            var infoT = windowT.FindDeepChildLiberal(str_info);

            Transform saveProjectButtonT = windowT.FindDeepChildLiberal(str_saveProject);
            Transform findPackageButtonT = windowT.FindDeepChildLiberal(str_findPackage);

            void OnPackageNameChange(string pkgString)
            {
                if (SwoleUtil.IsPackageStringWithVersion(pkgString))
                {
                    if (saveProjectButtonT != null) saveProjectButtonT.gameObject.SetActive(true);
                    if (findPackageButtonT != null) findPackageButtonT.gameObject.SetActive(false);
                }
                else
                {
                    if (saveProjectButtonT != null) saveProjectButtonT.gameObject.SetActive(false);
                    if (findPackageButtonT != null) findPackageButtonT.gameObject.SetActive(true);
                }
            }

            Transform previewT = null;
            Image preview = null;
            if (infoT != null)
            {
                previewT = infoT.FindDeepChildLiberal(str_preview); 
                if (previewT != null)
                {
                    preview = previewT.GetComponentInChildren<Image>(true);
                    preview.gameObject.SetActive(true);
                    preview.sprite = null;
                    preview.enabled = false;
                }
                if (preview != null)
                {
                    if (proj.previewTextureAsset != null)
                    {
                        preview.enabled = true;
                        preview.sprite = proj.previewTextureAsset.Sprite;
                    }
                }

                CustomEditorUtils.SetInputOrTextComponentTextByName(infoT, str_projectName, proj.name);
                CustomEditorUtils.SetInputFieldOnValueChangeActionByName(infoT, str_packageName, OnPackageNameChange);
                string pkgStr = proj.packageInfo.HasValue ? proj.packageInfo.Value.GetIdentityString() : string.Empty;
                CustomEditorUtils.SetInputOrTextComponentTextByName(infoT, str_packageName, pkgStr);
                OnPackageNameChange(pkgStr); 
            }

            Transform errorT = windowT.FindDeepChildLiberal(str_errorMessage);
            UIPopupMessageFadable errorMessage = null;
            if (errorT != null)
            {
                errorMessage = errorT.GetComponentInChildren<UIPopupMessageFadable>(true);
            }

            void FindPackage()
            {
                string pkg_name = CustomEditorUtils.GetInputOrTextComponentTextByName(infoT, str_packageName);
                if (string.IsNullOrWhiteSpace(pkg_name))
                {
                    errorMessage.SetMessage("Package name cannot be empty.").SetDisplayTime(errorMessage.DefaultDisplayTime).Show();
                    return;
                }
                if (!SwoleUtil.IsPackageString(pkg_name))
                {
                    errorMessage.SetMessage("Package name is invalid.").SetDisplayTime(errorMessage.DefaultDisplayTime).Show();
                    return;
                }
                var lpkg = ContentManager.FindLocalPackage(new PackageIdentifier(pkg_name));
                if (lpkg == null || lpkg.Content == null)
                {
                    errorMessage.SetMessage($"Local package '{pkg_name}' could not be found.").SetDisplayTime(errorMessage.DefaultDisplayTime).Show();
                    return;
                }

                CustomEditorUtils.SetInputOrTextComponentTextByName(windowT, str_packageName, lpkg.GetIdentityString());
                if (saveProjectButtonT != null) saveProjectButtonT.gameObject.SetActive(true);
                if (findPackageButtonT != null) findPackageButtonT.gameObject.SetActive(false);
            }

            bool saved = false;
            void Save()
            {
                if (runtimeEditor != null) runtimeEditor.DisableClickSelectForTime(0.4f); 

                if (proj == null || !proj.IsValid)
                {
                    errorMessage.SetMessage("An unexpected error occurred.").SetDisplayTime(errorMessage.DefaultDisplayTime).Show(); 
                    return;
                }

                string proj_name = CustomEditorUtils.GetInputOrTextComponentTextByName(infoT, str_projectName);
                if (string.IsNullOrWhiteSpace(proj_name))
                {
                    errorMessage.SetMessage("Project name cannot be empty.").SetDisplayTime(errorMessage.DefaultDisplayTime).Show();
                    return;
                }
                if (!SwoleUtil.IsContentName(proj_name))
                {
                    errorMessage.SetMessage("Project name is invalid.").SetDisplayTime(errorMessage.DefaultDisplayTime).Show();
                    return;
                }

                string pkg_name = CustomEditorUtils.GetInputOrTextComponentTextByName(infoT, str_packageName);
                if (string.IsNullOrWhiteSpace(pkg_name))
                {
                    errorMessage.SetMessage("Package name cannot be empty.").SetDisplayTime(errorMessage.DefaultDisplayTime).Show();
                    return;
                }
                if (!SwoleUtil.IsPackageString(pkg_name))
                {
                    errorMessage.SetMessage("Package name is invalid.").SetDisplayTime(errorMessage.DefaultDisplayTime).Show();
                    return;
                }
                var lpkg = ContentManager.FindLocalPackage(new PackageIdentifier(pkg_name));
                if (lpkg == null || lpkg.Content == null)
                {
                    errorMessage.SetMessage($"Local package '{pkg_name}' could not be found.").SetDisplayTime(errorMessage.DefaultDisplayTime).Show();
                    return;
                }

                proj.name = proj_name;
                proj.packageInfo = lpkg.Content.Manifest;
                if (SaveProject(proj)) 
                { 
                    saved = true;
                    onSave?.Invoke();
                }

                windowT.gameObject.SetActive(false);
            }

            CustomEditorUtils.SetButtonOnClickActionByName(windowT, str_saveProject, Save, true, true, false);
            CustomEditorUtils.SetButtonOnClickActionByName(windowT, str_findPackage, FindPackage, true, true, false);

            if (onComplete != null)
            {
                IEnumerator WaitForClose()
                {
                    while (true)
                    {
                        if (windowT == null) break;
                        if (!windowT.gameObject.activeInHierarchy)
                        {
                            if (!saved) onFail?.Invoke();  
                            onComplete?.Invoke();
                            break;
                        }
                        yield return null;
                    }
                }
                StartCoroutine(WaitForClose());
            } 
        }
        public void TryQuickSave()
        {
            TryQuickSave(ActiveProject);
        }
        public void TryQuickSave(Project proj, bool openSaveProjectWindowOnFail = true, VoidParameterlessDelegate onSave = null, VoidParameterlessDelegate onFail = null, VoidParameterlessDelegate onComplete = null)
        {
            if (proj == null) return;

            if (proj.packageInfo.HasValue && proj.packageInfo.Value.NameIsValid)  
            {
                if (SaveProject(proj)) 
                {
                    onSave?.Invoke();
                    onComplete?.Invoke();
                    return; 
                }
            }

            if (openSaveProjectWindowOnFail) 
            { 
                OpenSaveProjectWindow(proj, onSave, onFail, onComplete); 
            } 
            else
            {
                onFail?.Invoke();
                onComplete?.Invoke();
            }
        }
        public void QuickSaveAll()
        {
            if (openProjects == null) return;

            foreach(var proj in openProjects) TryQuickSave(proj, false);
        }


        [Tooltip("A window for importing tile collections.")]
        public GameObject tileCollectionImportWindow;
        private List<TileCollection> tileCollectionCache = new List<TileCollection>();
        public void OpenTileCollectionImportWindow()
        {
            if (prefabHandler == null) return;
            OpenTileCollectionImportWindow(tileCollectionImportWindow);
        }
        public void OpenTileCollectionImportWindow(GameObject tileCollectionImportWindow)
        {
            if (tileCollectionImportWindow == null) return;

            UIPopup popup = tileCollectionImportWindow.GetComponentInChildren<UIPopup>(true);

            UICategorizedList list = tileCollectionImportWindow.GetComponentInChildren<UICategorizedList>(true);
            if (list != null)
            {
                list.Clear(true);

                if (tileCollectionCache == null) tileCollectionCache = new List<TileCollection>();
                if (tileCollectionCache.Count <= 0) tileCollectionCache = ResourceLib.GetAllTileCollections(tileCollectionCache);

                foreach (var collection in tileCollectionCache)
                {
                    if (prefabHandler.ContainsTileCollection(collection)) continue; 
                    list.AddNewListMember(collection.name, collection.category.ToString().ToUpper(), () => {

                        if (popup != null) popup.Close();
                        prefabHandler.AddTileCollection(collection);   

                    }, null);
                }
            }

            tileCollectionImportWindow.gameObject.SetActive(true);
            tileCollectionImportWindow.transform.SetAsLastSibling(); 
            if (popup != null) popup.Elevate();
        }
        public GameObject packageImportWindow;
        public void OpenPackageImportWindow()
        {
            if (prefabHandler == null) return;
            OpenPackageImportWindow(packageImportWindow);
        }
        public Sprite localPackageIcon;
        public Sprite externalPackageIcon;
        private const string _localPackageCategory = "Local Packages";
        private const string _externalPackageCategory = "External Packages";
        public void OpenPackageImportWindow(GameObject packageImportWindow)
        {
            if (packageImportWindow == null) return;

            UIPopup popup = packageImportWindow.GetComponentInChildren<UIPopup>(true);

            UICategorizedList list = packageImportWindow.GetComponentInChildren<UICategorizedList>(true);
            if (list != null)
            {
                list.Clear(true);

                for(int a = 0; a < ContentManager.LocalPackageCount; a++)
                {
                    var pkg = ContentManager.GetLocalPackage(a);
                    if (pkg == null || pkg.Content == null) continue;

                    if (prefabHandler.ContainsPackageContent(pkg)) continue;
                    list.AddNewListMember(pkg.GetIdentityString(), _localPackageCategory.ToUpper(), () => {

                        if (popup != null) popup.Close();
                        prefabHandler.AddPackageContent(pkg);

                    }, localPackageIcon);
                }
                for (int a = 0; a < ContentManager.ExternalPackageCount; a++)
                {
                    var pkg = ContentManager.GetExternalPackage(a);
                    if (pkg.Content == null) continue;

                    if (prefabHandler.ContainsPackageContent(pkg)) continue; 
                    list.AddNewListMember(pkg.GetIdentityString(), _externalPackageCategory.ToUpper(), () => {

                        if (popup != null) popup.Close();
                        prefabHandler.AddPackageContent(pkg);

                    }, externalPackageIcon);
                }
            }

            packageImportWindow.gameObject.SetActive(true);
            packageImportWindow.transform.SetAsLastSibling();
            if (popup != null) popup.Elevate();
        }

        public void ClosePrefabPalette()
        {
            if (PrefabHandler == null || prefabHandler.tilePickerWindow == null) return;
            prefabHandler.tilePickerWindow.SetActive(false); 
        }
        public void OpenPrefabPalette() => OpenPrefabPalette(ActiveProject);
        public void OpenPrefabPalette(Project proj)
        {
            if (proj == null || PrefabHandler == null || prefabHandler.tilePickerWindow == null) return;

            var editorWindow = prefabHandler.tilePickerWindow; 
            OpenStatefulEditorWindow(windowStateID_objectPalette, proj, editorWindow);
        }

        public RectTransform curveWindow;
        public RectTransform animationCurveEditorWindow;
        public RectTransform bezierCurveEditorWindow;

        protected const string str_edit = "edit";
        protected const string str_delete = "delete";
        protected const string str_add = "add";
        protected const string str_bezier = "bezier";
        protected const string str_time = "time";
        protected const string str_create = "create";

        public void OpenCurveMenu() => OpenCurveMenu(curveWindow, ActiveProject);
        public void OpenCurveMenu(Project proj) => OpenCurveMenu(curveWindow, proj);
        public void OpenCurveMenu(RectTransform curveWindow) => OpenCurveMenu(curveWindow, ActiveProject);
        public void OpenCurveMenu(RectTransform curveWindow, Project proj)
        {
            if (curveWindow == null) return;

            OpenStatefulEditorWindow(windowStateID_curvesMenu, proj, curveWindow.gameObject);

            var list = curveWindow.GetComponentInChildren<UIRecyclingList>();
            if (list != null)
            {
                list.Clear();

                if (proj != null)
                {

                    void OnRefresh(UIRecyclingList.MemberData memData, GameObject listObj)
                    {
                        ICurve curve = null;
                        if (memData.storage is ICurve c) curve = c;

                        void OnUpdateName(string name)
                        {
                            memData.name = name;
                            if (curve != null) curve.Name = name;

                            list.AddOrUpdateMember(memData, false);
                        }

                        CustomEditorUtils.SetInputFieldOnValueChangeAction(listObj, OnUpdateName);

                        CustomEditorUtils.SetButtonOnClickActionByName(listObj, str_edit, () => StartEditingCurve(curve, proj));
                        CustomEditorUtils.SetButtonOnClickActionByName(listObj, str_delete, () =>
                        {
                            proj.RemoveCurve(curve);
                            OpenCurveMenu(curveWindow, proj);
                        });
                    }

                    for (int a = 0; a < proj.CurveCount; a++)
                    {
                        var curve = proj.GetCurve(a);
                        if (curve == null) continue;

                        list.AddNewMember(curve.Name, null, false, OnRefresh, curve);
                    }
                    list.Refresh(); 
                    
                    var addCurveMenu = curveWindow.FindDeepChildLiberal("AddCurveMenu"); 
                    if (addCurveMenu != null)
                    {
                        var type = curveWindow.FindDeepChildLiberal("Type");
                        if (type != null)
                        {
                            void CreateCurve()
                            {
                                if (addCurveMenu != null)
                                {
                                    var typeStr = CustomEditorUtils.GetComponentTextByName(type, "Current-Text").AsID();

                                    ICurve curve = null;
                                    if (typeStr == str_bezier.AsID())
                                    {
#if BULKOUT_ENV
                                        curve = new ExternalBezierCurve();
#endif
                                    }
                                    else if (typeStr == str_time.AsID())
                                    {
                                        curve = new EditableAnimationCurve(); 
                                    }

                                    addCurveMenu.gameObject.SetActive(false);
                                    if (curve != null)
                                    {
                                        string curveName = CustomEditorUtils.GetInputOrTextComponentTextByName(addCurveMenu, str_name);    
                                        if (string.IsNullOrWhiteSpace(curveName)) curveName = "unnamed_curve"; 
                                        curve.Name = curveName;

                                        int i = 2;
                                        while (proj.TryGetCurve(curveName, out _)) 
                                        { 
                                            curveName = curve.Name + $"({i})";
                                            i++;
                                        }
                                        curve.Name = curveName;

                                        proj.AddCurve(curve);
                                        OpenCurveMenu(curveWindow);  
                                    }
                                }
                            }
                            void OpenAddCurveMenu()
                            {
                                if (addCurveMenu != null)
                                {
                                    addCurveMenu.gameObject.SetActive(true);
                                    CustomEditorUtils.SetButtonOnClickActionByName(addCurveMenu, str_create, CreateCurve);
                                    CustomEditorUtils.SetInputFieldTextByName(addCurveMenu, str_name, string.Empty);
                                }
                            }

                            CustomEditorUtils.SetButtonOnClickActionByName(curveWindow, str_add, OpenAddCurveMenu); 
                        }
                    }
                }
            }
        }
        public void StartEditingCurve(ICurve curve, Project proj)
        {
            if (proj == null || !proj.IsValid) return;

#if BULKOUT_ENV
            if (curve is ExternalBezierCurve ebc)
            {
                if (bezierCurveEditorWindow != null)
                {
                    OpenStatefulEditorWindow(windowStateID_bezierCurveEditor, proj, bezierCurveEditorWindow.gameObject);
                    if (proj.TryGetWindowState(windowStateID_bezierCurveEditor, out var state))
                    {
                        state.dataStringA = curve.Name;
                        proj.SetWindowState(windowStateID_bezierCurveEditor, state);
                    }

                    var editor = bezierCurveEditorWindow.GetComponentInChildren<BezierCurveEditor2D>(true); 
                    if (editor != null)
                    {
                        if (editor.OnStateChange == null) editor.OnStateChange = new UnityEvent<AnimationCurveEditor.State, AnimationCurveEditor.State>(); else editor.OnStateChange.RemoveAllListeners();
                        editor.SetCurve(ebc); 
                        editor.OnStateChange.AddListener((AnimationCurveEditor.State stateA, AnimationCurveEditor.State stateB) => proj.MarkAsDirty()); 
                    }
                }
                return;
            }
#endif

            if (curve is EditableAnimationCurve eac)
            {
                if (animationCurveEditorWindow != null)
                {
                    OpenStatefulEditorWindow(windowStateID_animationCurveEditor, proj, animationCurveEditorWindow.gameObject);
                    if (proj.TryGetWindowState(windowStateID_animationCurveEditor, out var state))
                    {
                        state.dataStringA = curve.Name;
                        proj.SetWindowState(windowStateID_animationCurveEditor, state); 
                    }

                    var editor = animationCurveEditorWindow.GetComponentInChildren<SwoleCurveEditor>(true);  
                    if (editor != null)
                    {
                        if (editor.OnStateChange == null) editor.OnStateChange = new UnityEvent<AnimationCurveEditor.State, AnimationCurveEditor.State>(); else editor.OnStateChange.RemoveAllListeners();
                        editor.SetCurve(eac);
                        editor.OnStateChange.AddListener((AnimationCurveEditor.State stateA, AnimationCurveEditor.State stateB) => proj.MarkAsDirty());  
                    }
                }
            }
        }
        public void CloseCurveMenu()
        {
            if (curveWindow != null) curveWindow.gameObject.SetActive(false);
        }
        public void CloseCurveEditors() 
        {
            if (animationCurveEditorWindow != null) animationCurveEditorWindow.gameObject.SetActive(false);
            if (bezierCurveEditorWindow != null) bezierCurveEditorWindow.gameObject.SetActive(false);
        }

        public void CloseFloatingWindows()
        {
            ClosePrefabPalette();
            CloseCurveMenu();
            CloseCurveEditors();
        }

        #endregion

        private RectTransform placeholderTransform;
        private Image placeholderGraphic;

        private RectTransform slotInTransform;
        private Image slotInGraphic;

        private RectTransform parentToTransform;
        private Image parentToGraphic;

        private bool setupEditor;

        [Header("Builder Setup")]
        public UnityLayer editableLayer;
        public UnityLayer fullyEditableLayer;
        [SerializeField]
        protected RuntimePrefabHandler prefabHandler;
        public RuntimePrefabHandler PrefabHandler
        {
            get
            {
                if (prefabHandler == null)
                {
                    prefabHandler = GameObject.FindFirstObjectByType<RuntimePrefabHandler>();
                    SetupPrefabHandler(); 
                }
                
                return prefabHandler;            
            }
        }
        public void SetupPrefabHandler()
        {
            if (prefabHandler == null) return;
            if (prefabHandler.tilePickerWindow != null)
            {
                CustomEditorUtils.SetButtonOnClickActionByName(prefabHandler.tilePickerWindow, RuntimePrefabHandler._addCollectionButtonName, OpenTileCollectionImportWindow);
                CustomEditorUtils.SetButtonOnClickActionByName(prefabHandler.tilePickerWindow, RuntimePrefabHandler._addPackageButtonName, OpenPackageImportWindow);
                CustomEditorUtils.SetButtonOnClickActionByName(prefabHandler.tilePickerWindow, RuntimePrefabHandler._removeCollectionButtonName, RemoveSelectedPrefabCollection);
            }
        }
        public void RemoveSelectedPrefabCollection()
        {
            if (prefabHandler == null) return;
            var activePrefabCollectionName = prefabHandler.GetNameOfActiveCollection();
            if (string.IsNullOrWhiteSpace(activePrefabCollectionName)) return;

            var activePrefabCollectionNameId = activePrefabCollectionName.AsID();

            var proj = ActiveProject;
            if (proj == null) return;
            bool flag = false;
            for(int a = 0; a < proj.MemberCount; a++)
            {
                var mem = proj.GetMemberByIndex(a);
                if (mem == null || mem.sourceCollection == null || mem.sourceCollection.AsID() != activePrefabCollectionNameId) continue;
                flag = true; // Has spawned objects from collection
                break;
            }

            if (flag)
            {
                ShowPopupYesNoCancel("DELETE OBJECTS", $"Do you want to delete all objects associated with the collection '{activePrefabCollectionName}' as well?", () =>
                {
                    DeleteAllObjectsAssociatedWith(activePrefabCollectionName);
                    prefabHandler.RemoveActiveCollection();
                }, prefabHandler.RemoveActiveCollection, null); 
            }  
            else
            {
                prefabHandler.RemoveActiveCollection(); 
            }
        }
        public void DeleteAllObjectsAssociatedWith(TileCollection tileCollection) => DeleteAllObjectsAssociatedWith(tileCollection.name);
        public void DeleteAllObjectsAssociatedWith(ContentPackage package) => DeleteAllObjectsAssociatedWith(package.GetIdentityString());
        public void DeleteAllObjectsAssociatedWith(string collectionName)
        {
            if (string.IsNullOrWhiteSpace(collectionName)) return;
            var collectionNameId = collectionName.AsID();

            var proj = ActiveProject;
            if (proj == null) return;

            int i = 0;
            while(i < proj.MemberCount)
            {
                var mem = proj.GetMemberByIndex(i);
                if (mem == null)
                {
                    i++;
                    continue;
                }
                if (mem.sourceCollection != null && mem.sourceCollection.AsID() == collectionNameId)
                {
                    proj.RemoveMemberByIndex(i); 
                    mem.Destroy();
                    i = 0; // hotfix (in case multiple members are removed)
                    continue;
                }
                i++;
            }
        }

        [SerializeField]
        private UnityEvent OnBuilderSetupSuccess = new UnityEvent();
        [SerializeField]
        private UnityEvent OnBuilderSetupFail = new UnityEvent();

#if BULKOUT_ENV
        [Header("Editor Setup")]
        public string additiveEditorSetupScene = "sc_RLD-Add"; // Paid Asset Integration https://assetstore.unity.com/packages/tools/modeling/runtime-level-design-52325
#else
        [Header("Editor Setup")]
        public string additiveEditorSetupScene = "";
#endif
        [Tooltip("Primary object used for manipulating the scene.")]
        public RuntimeEditor runtimeEditor;

        [SerializeField]
        private UnityEvent OnEditorSetupSuccess = new UnityEvent();
        [SerializeField]
        private UnityEvent OnEditorSetupFail = new UnityEvent();

        [Header("Popups")]
        public GameObject promptMessageYesNo;
        public bool skipInstantiatePromptMessageYesNo;
        public GameObject promptMessageYesNoCancel;
        public bool skipInstantiatePromptMessageYesNoCancel;

        public GameObject loadingOverlay;

        [Header("Buttons")]
        public GameObject quickSaveButton;
        private const string str_unsaved = "unsaved";
        private const string str_unsynced = "unsynced";
        private const string str_synced = "synced";
        public void RefreshQuickSaveButton()
        {
            if (quickSaveButton == null) return; 

            var proj = ActiveProject;

            var quickSaveButtonT = quickSaveButton.transform; 

            var unsavedT = quickSaveButtonT.FindDeepChildLiberal(str_unsaved);
            var unsyncedT = quickSaveButtonT.FindDeepChildLiberal(str_unsynced);
            var syncedT = quickSaveButtonT.FindDeepChildLiberal(str_synced);

            if (proj != null && (!proj.packageInfo.HasValue || !proj.packageInfo.Value.NameIsValid)) 
            {
                if (unsavedT != null) unsavedT.gameObject.SetActive(true);
                if (unsyncedT != null) unsyncedT.gameObject.SetActive(false);
                if (syncedT != null) syncedT.gameObject.SetActive(false);  
            } 
            else if (proj != null && proj.IsDirty)
            {
                if (unsavedT != null) unsavedT.gameObject.SetActive(false);
                if (unsyncedT != null) unsyncedT.gameObject.SetActive(true);
                if (syncedT != null) syncedT.gameObject.SetActive(false);
            } 
            else
            {
                if (unsavedT != null) unsavedT.gameObject.SetActive(false);
                if (unsyncedT != null) unsyncedT.gameObject.SetActive(false);
                if (syncedT != null) syncedT.gameObject.SetActive(true);
            }
        }

        public GameObject playButton;
        public GameObject stopButton;

        public GameObject pauseButton;
        public GameObject unpauseButton;

        [Header("Testing"), SerializeField]
        protected Camera testingCamera;
        protected float testingCamFOV;
        protected float testingCamNearClip;
        protected float testingCamFarClip;
        /*protected EngineInternal.Vector3 testingCamPos;
        protected EngineInternal.Quaternion testingCamRot;
        protected EngineInternal.Vector3 testingCamScale;*/
        public Camera TestingCamera
        {
            get
            {
                if (testingCamera == null)
                {
                    if (PlayModeCamera.activeInstance != null && PlayModeCamera.activeInstance.camera != null)
                    {
                        testingCamera = PlayModeCamera.activeInstance.camera; 
                    }
                    else
                    {
                        var mainCam = Camera.main;
                        if (mainCam != null)
                        {
                            testingCamera = Instantiate(mainCam);
                        }
                        else
                        {
                            testingCamera = new GameObject().AddComponent<Camera>();
                            testingCamera.orthographic = false;
                            testingCamera.fieldOfView = 60;
                        }

                        testingCamera.name = "testingCam";
                        testingCamera.gameObject.SetActive(false);
                        testingCamera.gameObject.tag = "Untagged";

                        var pmc = testingCamera.gameObject.AddComponent<PlayModeCamera>();
                        pmc.camera = testingCamera;
                    }
                }

                if (testingCamFOV <= 0) // cache the same settings for next run
                {
                    testingCamFOV = testingCamera.fieldOfView;
                    testingCamNearClip = testingCamera.nearClipPlane;
                    testingCamFarClip = testingCamera.farClipPlane;
                    var camT = UnityEngineHook.AsSwoleGameObject(testingCamera.gameObject).transform;
                    /*testingCamPos = camT.position;
                    testingCamRot = camT.rotation;
                    testingCamScale = camT.localScale;*/
                }

                return testingCamera;
            }
        }
        protected Camera mainCamera;
        /// <summary>
        /// The default main camera in the unity scene
        /// </summary>
        public Camera MainCamera
        {
            get
            {
                if (mainCamera == null) mainCamera = Camera.main;
                return mainCamera;
            }
        }

        [Header("Other")]
        public Text setupErrorTextOutput;
        public TMP_Text setupErrorTextOutputTMP; 

        protected void ShowSetupError(string msg)
        {

            if (setupErrorTextOutput != null)
            {
                setupErrorTextOutput.gameObject.SetActive(true);
                setupErrorTextOutput.enabled = true;
                setupErrorTextOutput.text = msg;
            }

            if (setupErrorTextOutputTMP != null)
            {
                setupErrorTextOutputTMP.gameObject.SetActive(true);
                setupErrorTextOutputTMP.enabled = true;
                setupErrorTextOutputTMP.text = msg;
            }

        }

        protected void ShowBuilderSetupError(string msg) => ShowSetupError($"BUILDER SETUP ERROR => \"{msg}\"");

        protected void ShowEditorSetupError(string msg) => ShowSetupError($"EDITOR SETUP ERROR => \"{msg}\"");

        protected virtual void Awake()
        {

            void FailSetup(string msg)
            {
                swole.LogError(msg);
                OnBuilderSetupFail?.Invoke();
                ShowBuilderSetupError(msg);
                Destroy(this);
            }

            if (canvasMain == null)
            {
                FailSetup($"Main Canvas not set for Creation Builder '{name}'");
                return;
            }
            canvasMainTransform = canvasMain.GetComponent<RectTransform>();
            if (canvasElevated == null)
            {
                FailSetup($"Elevated Canvas not set for Creation Builder '{name}'");
                return;
            }
            canvasElevatedTransform = canvasElevated.GetComponent<RectTransform>();

            if (creationTabsLayoutGroup == null)
            {
                FailSetup($"Creation Tabs Layout Group not set for Creation Builder '{name}'");
                return;
            }
            if (creationTabPrototype == null)
            {
                FailSetup($"Creation Tab Prototype not set for Creation Builder '{name}'");
                return;
            }

            if (hierarchyLayoutGroup == null)
            {
                FailSetup($"Hierarchy Layout Group not set for Creation Builder '{name}'");
                return;
            }
            if (hierarchyMemberPrototype == null)
            {
                FailSetup($"Hierarchy Member Prototype not set for Creation Builder '{name}'");
                return;
            }
            if (hierarchyPlaceholderPrototype == null)
            {
                FailSetup($"Hierarchy Placeholder Prototype not set for Creation Builder '{name}'");
                return;
            }
            if (hierarchySlotInPrototype == null)
            {
                FailSetup($"Hierarchy Slot In Prototype not set for Creation Builder '{name}'");
                return;
            }

            if (objectEditorWindow == null)
            {
                FailSetup($"Object Editor Window not set for Creation Builder '{name}'");
                return;
            }
            if (codeEditorWindow == null)
            {
                FailSetup($"Code Editor Window not set for Creation Builder '{name}'");
                return;
            }

            if (promptMessageYesNo == null)
            {
                FailSetup($"Popup Message Yes/No not set for Creation Builder '{name}'");
                return;
            }
            if (promptMessageYesNoCancel == null)
            {
                FailSetup($"Popup Message Yes/No/Cancel not set for Creation Builder '{name}'");
                return;
            }

            try
            {

                creationTabsTransform = creationTabsLayoutGroup.GetComponent<RectTransform>();
                if (creationTabPool == null) creationTabPool = creationTabsLayoutGroup.gameObject.AddComponent<PrefabPool>();
                creationTabPool.Reinitialize(creationTabPrototype, PoolGrowthMethod.Incremental, 1, 1, 256);
                creationTabPool.SetContainerTransform(creationTabsTransform);
                creationTabPrototype.SetActive(false);

                hierarchyLayoutGroup.childControlWidth = true;
                hierarchyLayoutGroup.childControlHeight = false;

                hierarchyLayoutGroup.childScaleWidth = false;
                hierarchyLayoutGroup.childScaleHeight = true;

                hierarchyLayoutGroup.childForceExpandWidth = true;
                hierarchyLayoutGroup.childForceExpandHeight = false;

                hierarchyTransform = hierarchyLayoutGroup.GetComponent<RectTransform>();

                placeholderGraphic = skipInstantiatePlaceholderPrototype ? hierarchyPlaceholderPrototype : Instantiate(hierarchyPlaceholderPrototype);
                placeholderGraphic.gameObject.SetActive(true);
                placeholderGraphic.enabled = false;
                placeholderGraphic.raycastTarget = false;
                placeholderTransform = placeholderGraphic.rectTransform;
                placeholderTransform.SetParent(canvasMainTransform);

                slotInGraphic = skipInstantiateSlotInPrototype ? hierarchySlotInPrototype : Instantiate(hierarchySlotInPrototype);
                slotInGraphic.gameObject.SetActive(true);
                slotInGraphic.enabled = false;
                slotInGraphic.raycastTarget = false;
                slotInTransform = slotInGraphic.rectTransform;
                slotInTransform.SetParent(canvasMainTransform);

                if (hierarchyParentToPrototype != null)
                {
                    parentToGraphic = skipInstantiateParentToPrototype ? hierarchyParentToPrototype : Instantiate(hierarchyParentToPrototype);
                    parentToGraphic.gameObject.SetActive(true);
                    parentToGraphic.enabled = false;
                    parentToGraphic.raycastTarget = false;
                    parentToTransform = parentToGraphic.rectTransform;
                    parentToTransform.SetParent(canvasMainTransform);
                }

                objectEditorPool = new GameObject("_objectEditorWindows").AddComponent<PrefabPool>();
                objectEditorPool.SetContainerTransform(canvasElevatedTransform, false, false);
                objectEditorPool.Reinitialize(objectEditorWindow.gameObject, PoolGrowthMethod.Incremental, 1, 1, maxOpenWindowsPerType);
                objectEditorWindow.gameObject.SetActive(false);

                codeEditorPool = new GameObject("_codeEditorWindows").AddComponent<PrefabPool>();
                codeEditorPool.SetContainerTransform(canvasElevatedTransform, false, false);
                codeEditorPool.Reinitialize(codeEditorWindow.gameObject, PoolGrowthMethod.Incremental, 1, 1, maxOpenWindowsPerType);
                codeEditorPool.gameObject.SetActive(false);

                // Pop ups
                promptMessageYesNo = skipInstantiatePromptMessageYesNo ? promptMessageYesNo : Instantiate(promptMessageYesNo);
                promptMessageYesNo.gameObject.SetActive(false);
                promptMessageYesNo.transform.SetParent(canvasElevatedTransform);

                promptMessageYesNoCancel = skipInstantiatePromptMessageYesNoCancel ? promptMessageYesNoCancel : Instantiate(promptMessageYesNoCancel);
                promptMessageYesNoCancel.gameObject.SetActive(false);
                promptMessageYesNoCancel.transform.SetParent(canvasElevatedTransform);
                //

                OnBuilderSetupSuccess?.Invoke();

            } 
            catch(Exception ex)
            {
                OnBuilderSetupFail?.Invoke();
                ShowBuilderSetupError(ex.Message);
                swole.LogError(ex);
            }

            if (!setupEditor)
            {

                if (string.IsNullOrEmpty(additiveEditorSetupScene))
                {
                    string msg = "No additive editor setup scene was set. There will be no way to control the scene camera or to load and manipulate prefabs without it!";
                    swole.LogWarning(msg);
                    OnEditorSetupFail?.Invoke();
                    ShowEditorSetupError(msg);
                }
                else
                {

                    try
                    {

                        Camera mainCam = Camera.main;
                        EventSystem[] eventSystems = GameObject.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);

                        SceneManager.LoadScene(additiveEditorSetupScene, LoadSceneMode.Additive);
                        var scn = SceneManager.GetSceneByName(additiveEditorSetupScene);
                        if (scn.IsValid())
                        {
                             
                            if (mainCam != null) Destroy(mainCam.gameObject); // Make room for new main camera in setup scene
                            foreach (var system in eventSystems) if (system != null) Destroy(system.gameObject); // Remove any existing event systems and use the one in the setup scene

                            IEnumerator FindRuntimeEditor()
                            {                      
                                while (!scn.isLoaded)  
                                {
                                    yield return null;
                                    scn = SceneManager.GetSceneByBuildIndex(scn.buildIndex);
                                }

                                if (runtimeEditor == null) runtimeEditor = GameObject.FindFirstObjectByType<RuntimeEditor>();
                                if (runtimeEditor == null)
                                {
                                    string msg = "No RuntimeEditor object was found! Objects cannot be edited without it. The additive editor setup scene should contain one.";
                                    swole.LogWarning(msg);
                                    OnEditorSetupFail?.Invoke();
                                    ShowEditorSetupError(msg);
                                }
                                else
                                {
                                    runtimeEditor.OnPreSelect += OnPreSelectCustomize;
                                    runtimeEditor.OnSelectionChanged += OnSelectionChange;
                                    runtimeEditor.OnSpawnPrefab += OnSpawnPrefabFromPicker;
                                    runtimeEditor.OnManipulateTransforms += RecordManipulationAction;
                                    runtimeEditor.OnDuplicateObjects += OnDuplicateObjects;
                                    runtimeEditor.OnPreDelete += OnDeleteObjects; 
                                    runtimeEditor.DisableGroupSelect = true; 

                                    setupEditor = true;
                                    OnEditorSetupSuccess.Invoke();
                                }

                                if (prefabHandler != null) SetupPrefabHandler(); else prefabHandler = PrefabHandler;   
                                /*if (prefabHandler != null)
                                {
                                    IEnumerator DeactivatePickerWindows()
                                    {
                                        yield return null;
                                        if (prefabHandler.tilePickerWindow != null) prefabHandler.tilePickerWindow.SetActive(false);
                                    }
                                    StartCoroutine(DeactivatePickerWindows()); 
                                }*/

                                EndTestMode();

                                Init(); 
                            }

                            StartCoroutine(FindRuntimeEditor()); // Wait for scene to fully load, then find the runtime editor.

                        } 
                        else
                        {
                            OnEditorSetupFail?.Invoke();
                            ShowEditorSetupError($"Invalid scene name '{additiveEditorSetupScene}'");
                        }

                    } 
                    catch(Exception ex)
                    {
                        OnEditorSetupFail?.Invoke();
                        ShowEditorSetupError(ex.Message);
                        throw ex;
                    }

                }

            }

        }

        protected virtual void Init()
        {
            //if (ProjectCount <= 0) CreateNewProject(); 
            if (ProjectCount <= 0 && EditPackageState.TargetContent is Creation creation) OpenProject(creation);

            if (sceneSwapper == null) sceneSwapper = GameObject.FindFirstObjectByType<SceneSwap>();
        }

        public void ExitToPreviousScene(int fallbackSceneId)
        {
            void FullExit()
            {
                sceneSwapper.SwapToPreviousOrDefault(fallbackSceneId); 
            }
            void CloseNext()
            {
                if (openProjects != null)
                {
                    if (openProjects.Count > 0)
                    {
                        CloseProject(activeProjectIndex < 0 || activeProjectIndex >= openProjects.Count ? 0 : activeProjectIndex, CloseNext, null, null);
                    }
                    else
                    {
                        FullExit();
                    }
                }
                else
                {
                    FullExit();
                }
            }

            CloseNext();
        }
        public void Exit()
        {
            void FullExit()
            {
                if (string.IsNullOrWhiteSpace(EditPackageState.SwapBackScene))
                {
                    SceneSwap.ReturnToCreateMenu();
                } 
                else
                {
                    if (sceneSwapper != null) 
                    { 
                        sceneSwapper.Swap(EditPackageState.ConsumeSwapBackScene()); 
                    }
                }
            }
            void CloseNext()
            {
                if (openProjects != null)
                {
                    if (openProjects.Count > 0)
                    {
                        CloseProject(activeProjectIndex < 0 || activeProjectIndex >= openProjects.Count ? 0 : activeProjectIndex, CloseNext, null, null);    
                    } 
                    else
                    {
                        FullExit(); 
                    }
                } 
                else
                {
                    FullExit(); 
                }
            }

            CloseNext(); 
        }

        protected virtual UIPopup CreateNewUIMember(Member member)
        {

            member.hierarchyObject = Instantiate(hierarchyMemberPrototype);
            member.hierarchyObject.name = member.name;

            member.hierarchyObject.targetGraphic.color = hierarchyMemberTint;
            
            if (member.hierarchyObject.OnLeftClick == null) member.hierarchyObject.OnLeftClick = new UnityEvent();
            if (member.hierarchyObject.OnLeftClickRelease == null) member.hierarchyObject.OnLeftClickRelease = new UnityEvent();
            member.hierarchyObject.OnLeftClick.AddListener(new UnityAction(() => LeftClickMember(member)));
            member.hierarchyObject.OnLeftClickRelease.AddListener(new UnityAction(() => LeftClickReleaseMember(member)));

            var rootTransform = member.hierarchyObject.GetComponent<RectTransform>();
            member.rectTransform = rootTransform;
            member.rectTransform.SetParent(hierarchyTransform, false);

            var nameTransform = rootTransform.FindDeepChildLiberal(str_name);
            if (nameTransform != null)
            {
                var nameText = nameTransform.GetComponent<TMP_Text>();
                if (nameText == null)
                {
                    var nameTextLegacy = nameTransform.GetComponent<Text>();
                    if (nameTextLegacy != null)
                    {
                        nameTextLegacy.text = member.name;
                        member.setDisplayName = (text) => nameTextLegacy.text = text;
                    }
                }
                else
                {
                    nameText.text = member.name;
                    member.setDisplayName = (text) => nameText.SetText(text);
                }
            }

            var parentMaskLookup = rootTransform.FindDeepChildLiberal(str_parentMask);
            if (parentMaskLookup != null) member.parentMask = (RectTransform)parentMaskLookup;

            if (member.hierarchyObject.OnDragStart == null) member.hierarchyObject.OnDragStart = new UnityEvent();
            member.hierarchyObject.OnDragStart.AddListener(new UnityAction(() => StartDragMember(member)));

            if (member.hierarchyObject.OnDragStop == null) member.hierarchyObject.OnDragStop = new UnityEvent();
            member.hierarchyObject.OnDragStop.AddListener(new UnityAction(() => StopDragMember(member)));

            member.hierarchyObject.gameObject.SetActive(true);
            return member.hierarchyObject;

        }

        private delegate void SetFloatDelegate(float val);
        private delegate float GetFloatDelegate();
        protected virtual void StartEditingMember(Member targetMember)
        {
            if (targetMember == null || targetMember.swoleGameObject == null) return;

            Transform targetTransform = targetMember.swoleGameObject.transform;

            if (targetMember.activeEditorWindow == null && objectEditorPool.TryGetNewInstance(out GameObject newWindow))
            {
                targetMember.activeEditorWindow = newWindow.GetComponent<UIPopup>();
                if (targetMember.activeEditorWindow != null)
                {
                    if (targetMember.activeEditorWindow.OnClose == null) targetMember.activeEditorWindow.OnClose = new UnityEvent();
                    targetMember.activeEditorWindow.OnClose.RemoveAllListeners();
                    targetMember.activeEditorWindow.OnClose.AddListener(() =>
                    {
                        StopEditingMember(targetMember);
                    });
                    var optionsTransform = newWindow.transform.FindDeepChildLiberal("options");
                    if (optionsTransform != null)
                    {
                        var name = optionsTransform.FindDeepChildLiberal("name");
                        if (name != null)
                        {
                            var nameText = name.gameObject.GetComponentInChildren<InputField>(true);
                            var nameTextTMP = name.gameObject.GetComponentInChildren<TMP_InputField>(true);

                            if (nameText != null)
                            {
                                if (nameText.onValueChanged == null) nameText.onValueChanged = new InputField.OnChangeEvent();
                                nameText.onValueChanged.RemoveAllListeners();
                                nameText.onValueChanged.AddListener((string newName) => targetMember.SetName(newName));
                                nameText.SetTextWithoutNotify(targetMember.name);
                            }
                            if (nameTextTMP != null)
                            {
                                if (nameTextTMP.onValueChanged == null) nameTextTMP.onValueChanged = new TMP_InputField.OnChangeEvent();
                                nameTextTMP.onValueChanged.RemoveAllListeners();
                                nameTextTMP.onValueChanged.AddListener((string newName) => targetMember.SetName(newName));
                                nameTextTMP.SetTextWithoutNotify(targetMember.name);
                            }
                        } 

                        void SetVectorComponent(Transform root, string subName, SetFloatDelegate setComp, GetFloatDelegate getComp)
                        {
                            var eventSystem = EventSystem.current;

                            var sub = root.FindDeepChildLiberal(subName);
                            if (sub == null) return;

                            var comp = sub.gameObject.GetComponentInChildren<InputField>(true);
                            var compTMP = sub.gameObject.GetComponentInChildren<TMP_InputField>(true);

                            if (comp != null)
                            {
                                if (comp.onValueChanged == null) comp.onValueChanged = new InputField.OnChangeEvent();
                                comp.onValueChanged.RemoveAllListeners();
                                comp.onValueChanged.AddListener((string val) => { if (float.TryParse(val, out float result)) setComp(result); });
                                comp.SetTextWithoutNotify(getComp().ToString());
                            }
                            if (compTMP != null)
                            {
                                if (compTMP.onValueChanged == null) compTMP.onValueChanged = new TMP_InputField.OnChangeEvent();
                                compTMP.onValueChanged.RemoveAllListeners();
                                compTMP.onValueChanged.AddListener((string val) => { if (float.TryParse(val, out float result)) setComp(result); });
                                compTMP.SetTextWithoutNotify(getComp().ToString());
                            }

                            float prevValue = getComp();
                            IEnumerator StaySynced()
                            {

                                while (targetMember.swoleGameObject != null && targetMember.activeEditorWindow != null) 
                                {
                                    // make sure ui is not being interacted with
                                    if ((eventSystem == null || eventSystem.currentSelectedGameObject == null) && targetMember.activeEditorWindow.gameObject.activeInHierarchy)
                                    {
                                        float val = getComp();

                                        if (val != prevValue) 
                                        {
                                            if (comp != null) comp.SetTextWithoutNotify(val.ToString());
                                            if (compTMP != null) compTMP.SetTextWithoutNotify(val.ToString());
                                            prevValue = val;
                                        }
                                    }

                                    yield return null;
                                }

                            }

                            StartCoroutine(StaySynced());
                        }

                        var position = optionsTransform.FindDeepChildLiberal("position");
                        if (position != null)
                        {
                            SetVectorComponent(position, "x", (float val) =>
                            {
                                UnityEngine.Vector3 v = targetTransform.localPosition;
                                v.x = val;
                                targetTransform.localPosition = v;
                            }, () => targetTransform.localPosition.x);
                            SetVectorComponent(position, "y", (float val) =>
                            {
                                UnityEngine.Vector3 v = targetTransform.localPosition;
                                v.y = val;
                                targetTransform.localPosition = v;
                            }, () => targetTransform.localPosition.y);
                            SetVectorComponent(position, "z", (float val) =>
                            {
                                UnityEngine.Vector3 v = targetTransform.localPosition;
                                v.z = val;
                                targetTransform.localPosition = v;
                            }, () => targetTransform.localPosition.z);
                        }

                        var rotation = optionsTransform.FindDeepChildLiberal("rotation");
                        if (rotation != null)
                        {
                            SetVectorComponent(rotation, "x", (float val) =>
                            {
                                UnityEngine.Vector3 v = targetTransform.localRotation.eulerAngles;
                                v.x = val;
                                targetTransform.localRotation = Quaternion.Euler(v);
                            }, () => targetTransform.localRotation.eulerAngles.x);
                            SetVectorComponent(rotation, "y", (float val) =>
                            {
                                UnityEngine.Vector3 v = targetTransform.localRotation.eulerAngles;
                                v.y = val;
                                targetTransform.localRotation = Quaternion.Euler(v);
                            }, () => targetTransform.localRotation.eulerAngles.y);
                            SetVectorComponent(rotation, "z", (float val) =>
                            {
                                UnityEngine.Vector3 v = targetTransform.localRotation.eulerAngles;
                                v.z = val;
                                targetTransform.localRotation = Quaternion.Euler(v);
                            }, () => targetTransform.localRotation.eulerAngles.z); 
                        }

                        var scale = optionsTransform.FindDeepChildLiberal("scale");
                        if (scale != null)
                        {
                            SetVectorComponent(scale, "x", (float val) =>
                            {
                                UnityEngine.Vector3 v = targetTransform.localScale;
                                v.x = val;
                                targetTransform.localScale = v;
                            }, () => targetTransform.localScale.x);
                            SetVectorComponent(scale, "y", (float val) =>
                            {
                                UnityEngine.Vector3 v = targetTransform.localScale;
                                v.y = val;
                                targetTransform.localScale = v;
                            }, () => targetTransform.localScale.y);
                            SetVectorComponent(scale, "z", (float val) =>
                            {
                                UnityEngine.Vector3 v = targetTransform.localScale;
                                v.z = val;
                                targetTransform.localScale = v;
                            }, () => targetTransform.localScale.z);
                        }
                    }
                }
            }
            if (targetMember.activeEditorWindow != null)
            {
                targetMember.activeEditorWindow.gameObject.SetActive(true);
                targetMember.activeEditorWindow.Elevate();
            }
        }

        protected virtual void StopEditingMember(Member targetMember)
        {
            if (targetMember == null || targetMember.activeEditorWindow == null) return;
            targetMember.activeEditorWindow.gameObject.SetActive(false);
            objectEditorPool.Release(targetMember.activeEditorWindow);
            targetMember.activeEditorWindow = null;
        }

        protected virtual void StartDragMember(Member targetMember)
        {
            if (draggedMember != null) StopDragMember(draggedMember);
            draggedMember = targetMember;
            ActivatePlaceholder(targetMember);
            targetMember.rectTransform.SetParent(canvasMainTransform, true);
        }
        protected virtual void StopDragMember(Member targetMember)
        {
            DeactivatePlaceholder(targetMember);
            if (draggedMember == targetMember) draggedMember = null;
            ReevaluateMember(targetMember);
        }

        //protected List<Member> selectedMembers = new List<Member>();

        protected virtual void ClickMemberById(int id) => LeftClickMember(ActiveProject.GetMemberById(id));
        protected virtual void LeftClickMember(Member targetMember)
        {
            if (targetMember == null) return;

            //foreach (var mem in selectedMembers) if (mem.buttonObject != null) mem.buttonObject.ToggleOff(); // Deselect
            //selectedMembers.Clear();

            // TODO: Add Multi Selection funcionality

            //if (targetMember.buttonObject != null && !targetMember.buttonObject.IsToggledOn) targetMember.buttonObject.ToggleOn(); // Select
            //selectedMembers.Add(targetMember);

            targetMember.clickStart = Time.realtimeSinceStartup;
        }

        protected virtual void ClickReleaseMemberById(int id) => LeftClickReleaseMember(ActiveProject.GetMemberById(id));
        protected virtual void LeftClickReleaseMember(Member targetMember)
        {
            if (targetMember == null) return;

            float time = Time.realtimeSinceStartup;
            if (time - targetMember.clickStart < 0.35f) // User didn't hold down the click so select the object from the hierarchy.
            {
                if (time - targetMember.clickLast < InputProxy.DoubleClickSpeed) // Double click opens object editor window
                {
                    StartEditingMember(targetMember);
                    if (targetMember.swoleGameObject != null) runtimeEditor.Select(targetMember.swoleGameObject.gameObject);
                    targetMember.clickLast = 0;
                } 
                else
                {
                    targetMember.clickLast = time;
                    if (!InputProxy.Modding_ModifyActionKey)
                    {
                        runtimeEditor.DeselectAll();
                    }
                    if (targetMember.swoleGameObject != null) runtimeEditor.ToggleSelect(targetMember.swoleGameObject.gameObject);
                }
            } 
            else
            {
                targetMember.clickLast = 0;
            }
        }

        public void SelectAll() 
        {
            var proj = ActiveProject;
            if (proj == null) return;

            runtimeEditor.SelectAll(proj.containerTransform); 
        }

        public void DeselectAll() => runtimeEditor.DeselectAll();

        protected virtual void ReevaluateMemberById(int id) => ReevaluateMember(ActiveProject.GetMemberById(id));
        protected virtual void ReevaluateMember(Member targetMember)
        {
            if (targetMember == null) return;

            var proj = ActiveProject;
            if (proj == null) return;

            Vector2 cursorPos = CursorProxy.ScreenPosition;
            bool setParent = false;
            for (int a = 0; a < proj.members.Count; a++) 
            {
                var member = proj.members[a];
                if (member.id == targetMember.id) continue;
                if (member.ScreenPosIsInParentMask(cursorPos))
                {
                    SetMemberParent(targetMember, member);
                    setParent = true;
                    break;
                }
            }
            if (!setParent)
            {
                targetMember.rectTransform.localPosition = targetMember.hierarchyObject.PreDragLocalPosition;
                SetMemberParent(targetMember, null);
            }

        }

        public void RefreshLayout()
        {
            hierarchyLayoutGroup.CalculateLayoutInputVertical();
            hierarchyLayoutGroup.SetLayoutVertical();
            LayoutRebuilder.ForceRebuildLayoutImmediate(hierarchyTransform); 
        }

        public virtual void SetMemberParent(int childId, int parentId) => SetMemberParent(ActiveProject.GetMemberById(childId), ActiveProject.GetMemberById(parentId));

        protected virtual void SetMemberParent(Member child, Member parent)
        {

            if (child == null) return;

            if (child.parent != null) child.parent.RemoveChild(child);
            if (parent == null)
            {
                child.parent = null;
                if (child.swoleGameObject != null) 
                {
                    var proj = ActiveProject;
                    child.swoleGameObject.transform.SetParent(proj == null ? null : proj.containerTransform, true);  
                }
                RefreshLayout();
            }
            else
            {
                child.SetSiblingIndex(parent.GetSiblingIndexEnd() + (child.GetSiblingIndexStart() < parent.GetSiblingIndexEnd() ? 0 : 1));
                parent.AddChild(child);
                RefreshLayout();
            }

        }

        private readonly Vector3[] corners = new Vector3[4];
        protected int GetSiblingIndexFromScreenPosition(Vector3 screenPosition, Member parent = null) => GetSiblingIndexFromScreenPosition(screenPosition, out _, out _, parent);
        protected int GetSiblingIndexFromScreenPosition(Vector3 screenPosition, out Member toReplace, out Member previousMember, Member parent = null)
        {
            toReplace = previousMember = null;

            var proj = ActiveProject;
            if (proj == null) return 0;

            RefreshLayout();

            for (int a = 0; a < hierarchyTransform.childCount; a++)
            {
                var childTransform = (RectTransform)hierarchyTransform.GetChild(a);
                if (!childTransform.gameObject.activeInHierarchy) continue;
                var childMember = proj.GetMember(childTransform);
                if (childMember == null) continue;
                if (parent != null && childMember.IsChildOf(parent)) continue;
                childTransform.GetWorldCorners(corners);
                Vector3 center = (corners[0] + corners[1] + corners[2] + corners[3]) / 4f;
                if (!hierarchyLayoutGroup.reverseArrangement && screenPosition.y >= center.y || hierarchyLayoutGroup.reverseArrangement && screenPosition.y <= center.y)
                {
                    toReplace = childMember;
                    return Mathf.Max(0, a - 1);
                }
                previousMember = childMember;
            }

            return hierarchyTransform.childCount;

        }
        protected Member slotInReplacing;
        protected int UpdateSlotInIndex(Vector3 screenPosition, Member parent = null)
        {

            int slotInIndex = GetSiblingIndexFromScreenPosition(screenPosition, out slotInReplacing, out Member previousMember, parent);
            if (parentToGraphic != null && slotInReplacing != null && slotInReplacing.ScreenPosIsInParentMask(screenPosition))
            {
                ActivateParentToHighlight(slotInReplacing); // Show parent-to highlight graphic instead
                HideSlotIn();
            }
            else if (parentToGraphic != null && previousMember != null && previousMember.ScreenPosIsInParentMask(screenPosition))
            {
                ActivateParentToHighlight(previousMember); // Show parent-to highlight graphic instead
                HideSlotIn();
            }
            else
            {
                DeactivateParentToHighlight();
                ShowSlotIn();
            }
            return slotInIndex;

        }

        protected Member prevDraggedMember;
        protected Member draggedMember;

        protected void ActivatePlaceholder(Member toSub)
        {
            placeholderGraphic.raycastTarget = false;
            placeholderGraphic.enabled = true;
            placeholderTransform.SetParent(hierarchyTransform, false);
            if (toSub != null) placeholderTransform.SetSiblingIndex(toSub.GetSiblingIndexStart());
        }

        protected void DeactivatePlaceholder(Member toPutBack)
        {
            placeholderGraphic.raycastTarget = false;
            placeholderGraphic.enabled = false;
            if (toPutBack != null)
            {
                toPutBack.rectTransform.SetParent(hierarchyTransform);
                toPutBack.rectTransform.SetSiblingIndex(placeholderTransform.GetSiblingIndex());
            }
            placeholderTransform.SetParent(canvasMainTransform, false);
        }

        protected void ActivateParentToHighlight(Member toHighlight)
        {
            if (parentToGraphic == null || parentToTransform == null) return;
            parentToGraphic.raycastTarget = false;
            parentToGraphic.enabled = true;
            parentToTransform.SetParent(toHighlight.rectTransform, false);
            parentToTransform.SetAsLastSibling();
            parentToTransform.SetAnchor(AnchorPresets.StretchAll, false);
            parentToTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, toHighlight.rectTransform.rect.width);
            parentToTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, toHighlight.rectTransform.rect.height);
        }

        protected void DeactivateParentToHighlight()
        {
            if (parentToGraphic == null || parentToTransform == null) return;
            parentToGraphic.raycastTarget = false;
            parentToGraphic.enabled = false;
            parentToTransform.SetParent(canvasMainTransform, false);
        }

        protected void ShowSlotIn()
        {
            slotInGraphic.raycastTarget = false;
            slotInGraphic.enabled = true;
        }
        protected void HideSlotIn()
        {
            slotInGraphic.raycastTarget = false;
            slotInGraphic.enabled = false;
        }

        protected void ActivateSlotIn()
        {
            ShowSlotIn();
            slotInTransform.SetParent(hierarchyTransform, false);
        }
        protected void DeactivateSlotIn()
        {
            HideSlotIn();
            slotInTransform.SetParent(canvasMainTransform);
        }

        protected void OnSelectionChange(List<GameObject> fullSelection, List<GameObject> newlySelected, List<GameObject> deselected)
        {
            var proj = ActiveProject;
            if (proj == null) return; 

            // Highlight selected members in hierarchy
            foreach (var obj in deselected)
            {
                var member = proj.GetMember(obj);
                if (member == null || member.hierarchyObject == null) continue; 

                var img = member.hierarchyObject.targetGraphic;
                if (img == null) continue;
                img.color = hierarchyMemberTint;
            }

            foreach (var obj in newlySelected)
            {
                var member = proj.GetMember(obj);
                if (member == null || member.hierarchyObject == null) continue;

                var img = member.hierarchyObject.targetGraphic;
                if (img == null) continue;
                img.color = hierarchyMemberSelectedTint;
            }
            //

            proj.MarkProjectAsDirty();  
        }

        private const string str_actionName = "actionName";
        private const string str_message = "message";
        private const string str_yes = "yes";
        private const string str_no = "no";
        private const string str_cancel = "cancel";
        public bool ShowPopupYesNo(string actionName, string message, VoidParameterlessDelegate onYes, VoidParameterlessDelegate onNo) 
        {
            if (promptMessageYesNo.gameObject.activeSelf) return false;

            CustomEditorUtils.SetComponentTextByName(promptMessageYesNo, str_actionName, actionName);
            CustomEditorUtils.SetComponentTextByName(promptMessageYesNo, str_message, message);
            CustomEditorUtils.SetButtonOnClickActionByName(promptMessageYesNo, str_yes, () => { promptMessageYesNo.gameObject.SetActive(false); onYes?.Invoke(); });
            CustomEditorUtils.SetButtonOnClickActionByName(promptMessageYesNo, str_no, () => { promptMessageYesNo.gameObject.SetActive(false); onNo?.Invoke(); });

            CustomEditorUtils.SetButtonOnClickActionByName(promptMessageYesNo, str_close, () => { promptMessageYesNo.gameObject.SetActive(false); onNo?.Invoke(); });

            promptMessageYesNo.gameObject.SetActive(true);
            promptMessageYesNo.transform.SetAsLastSibling();

            return true;
        }
        public bool ShowPopupYesNoCancel(string actionName, string message, VoidParameterlessDelegate onYes, VoidParameterlessDelegate onNo, VoidParameterlessDelegate onCancel) 
        {
            if (promptMessageYesNoCancel.gameObject.activeSelf) return false;

            CustomEditorUtils.SetComponentTextByName(promptMessageYesNoCancel, str_actionName, actionName);
            CustomEditorUtils.SetComponentTextByName(promptMessageYesNoCancel, str_message, message);
            CustomEditorUtils.SetButtonOnClickActionByName(promptMessageYesNoCancel, str_yes, () => { promptMessageYesNoCancel.gameObject.SetActive(false); onYes?.Invoke(); });
            CustomEditorUtils.SetButtonOnClickActionByName(promptMessageYesNoCancel, str_no, () => { promptMessageYesNoCancel.gameObject.SetActive(false); onNo?.Invoke(); });
            CustomEditorUtils.SetButtonOnClickActionByName(promptMessageYesNoCancel, str_cancel, () => { promptMessageYesNoCancel.gameObject.SetActive(false); onCancel?.Invoke(); });

            CustomEditorUtils.SetButtonOnClickActionByName(promptMessageYesNoCancel, str_close, () => { promptMessageYesNoCancel.gameObject.SetActive(false); onCancel?.Invoke(); });

            promptMessageYesNoCancel.gameObject.SetActive(true);
            promptMessageYesNoCancel.transform.SetAsLastSibling();  

            return true;
        }

        protected void OnGUI()
        {
            if (draggedMember != null)
            {
                if (prevDraggedMember != draggedMember || slotInTransform.parent != hierarchyTransform) ActivateSlotIn();
                prevDraggedMember = draggedMember;
                int slotInIndex = UpdateSlotInIndex(CursorProxy.ScreenPosition, draggedMember);
                slotInTransform.SetSiblingIndex(slotInIndex); // Show slot-in graphic where the dragged element would be placed if dropped
            }
            else if (prevDraggedMember != null)
            {
                if (prevDraggedMember.parent == null)
                {
                    if (slotInReplacing != null && slotInReplacing.parent != null) // Auto-parent if dropping into parent hierarchy
                    {
                        SetMemberParent(prevDraggedMember, slotInReplacing.parent);
                    }
                    prevDraggedMember.SetSiblingIndex(slotInTransform.GetSiblingIndex()); // Place dropped element where the slot-in graphic was
                    prevDraggedMember.Refresh();
                }
                prevDraggedMember = null;
                DeactivateSlotIn();
                DeactivateParentToHighlight();
            }
        }

        public bool test;

        public string testPackage;
        public string testName;

        public bool testLoad;
        public bool testSave;

        public void Update()
        {
             
            RefreshQuickSaveButton();

            if (test)
            { 
                test = false;
                CreateNewProject(string.IsNullOrEmpty(testName) ? "test" : testName);  
                //CreateNewMember(GameObject.CreatePrimitive(PrimitiveType.Sphere)); 
            }

            if (testSave)
            {
                testSave = false;
                SaveProject(activeProjectIndex, null, null, null, ContentManager.FindLocalPackage(new PackageIdentifier(testPackage)).Content.Manifest); 
            }
            if (testLoad)
            {
                testLoad = false;
                var pkg = ContentManager.FindLocalPackage(new PackageIdentifier(testPackage)).Content;
                if (pkg.TryFind<Creation>(out var creation, testName))
                {
                    OpenProject(creation); 
                } 
                else
                {
                    swole.Log($"Could not find '{testName}' in '{pkg.Manifest.GetIdentityString()}'");
                }
            }

        }

        protected Member CreateNewMember(GameObject gameObject, string name = null, int projectIndex = -1)
        {
            if (projectIndex < 0 || projectIndex >= openProjects.Count) projectIndex = activeProjectIndex;
            return CreateNewMember(gameObject, this[projectIndex], projectIndex == activeProjectIndex, name);
        }
        protected Member CreateNewMember(GameObject gameObject, Project project, bool isActiveProject, string name = null)
        {
            if (gameObject == null) return null;

            var sgo = gameObject.AddOrGetComponent<SwoleGameObject>();

            if (project != null)
            {
                sgo.id = SwoleUtil.GetUniqueId(project.GetIdAtIndex, project.MemberCount);
            }

            return CreateNewMember(sgo, project, isActiveProject, name);
        }
        protected Member CreateNewMember(SwoleGameObject gameObject, string name = null, int projectIndex = -1)
        {
            if (projectIndex < 0 || projectIndex >= openProjects.Count) projectIndex = activeProjectIndex;
            return CreateNewMember(gameObject, this[projectIndex], projectIndex == activeProjectIndex, name);
        }
        protected Member CreateNewMember(SwoleGameObject gameObject, Project project, bool isActiveProject, string name = null)  
        {
            if (gameObject == null) return null;

            var mem = new Member() { id = gameObject.id, project = project };
            mem.name = string.IsNullOrWhiteSpace(name) ? $"{gameObject.name} [{gameObject.id}]" : name;   
            gameObject.name = mem.name;

            mem.swoleGameObject = gameObject;
            mem.swoleGameObject.gameObject.SetLayerAllChildren(fullyEditableLayer);  

            if (project == null) 
            {
                mem.project = null;
                return mem;
            }

            project.MarkAsDirty();
            project.members.Add(mem);
            var uiMember = CreateNewUIMember(mem);
            if (uiMember != null) uiMember.gameObject.SetActive(isActiveProject);

            return mem;        
        }

        protected void OnSpawnPrefabFromPicker(GameObject prefab, GameObject instance)
        {
            if (instance == null) return;

            if (openProjects == null || activeProjectIndex < 0 || activeProjectIndex >= openProjects.Count)
            {
                Destroy(instance); 
                return;
            }  

            var tilePrototype = instance.GetComponentInChildren<TilePrototype>(true);
            var creationPrototype = instance.GetComponentInChildren<CreationPrototype>(true); 
            if (tilePrototype != null)
            {
                //swole.Log("Spawned new tile");
            }
            else if (creationPrototype != null)
            {
                swole.Log("Spawned new creation");
            }
            else
            {
                swole.LogWarning($"[{nameof(CreationBuilder)}] Tried to spawn invalid prefab '{(prefab == null ? instance.name : prefab.name)}'");
                Destroy(instance);
                return;
            }

            var mem = CreateNewMember(instance);
            if (mem != null)
            {
                if (PrefabHandler != null)
                {
                    mem.sourceCollection = prefabHandler.GetNameOfActiveCollection();
                }
            }

        }

        private readonly HashSet<GameObject> objectSetA = new HashSet<GameObject>();
        protected void OnPreSelectCustomize(int selectReason, List<GameObject> toSelect, List<GameObject> toIgnore)
        {

            objectSetA.Clear();
             
            foreach (var obj in toSelect)
            {
                if (obj == null) continue;

                var proxy = obj.GetComponent<SelectionProxy>();
                if (proxy == null && obj.transform.parent != null) proxy = obj.transform.parent.GetComponent<SelectionProxy>();
                if (proxy != null)
                {
                    if (proxy.includeSelf) objectSetA.Add(obj);
                    foreach (var selected in proxy.toSelect) objectSetA.Add(selected); 
                }
                else
                {
                    SwoleGameObject swoleGO = obj.GetComponentInParent<SwoleGameObject>();
                    if (swoleGO != null)
                    {
                        objectSetA.Add(swoleGO.gameObject); 
                    } 
                    else
                    {
                        objectSetA.Add(obj);
                    }
                }
            }

            toSelect.Clear();
            toSelect.AddRange(objectSetA);
        }

        public static string _defaultSource_LoadProgress = $"// read data from {RuntimeHandler.Experience._serializedProgressIdentifier} {System.Environment.NewLine}{System.Environment.NewLine}";
        public static string _defaultSource_SaveProgress = $"// write data to {RuntimeHandler.Experience._serializedProgressIdentifier} {System.Environment.NewLine}{System.Environment.NewLine}"; 

        public static string _defaultSource_Update = $"// for frame-independent calculations, use {RuntimeEnvironment._localVarsAccessor}.{CreationBehaviour.varId_deltaTime} {System.Environment.NewLine}{System.Environment.NewLine}";
        public static string _defaultSource_FixedUpdate = $"// for frame-independent calculations, use {RuntimeEnvironment._localVarsAccessor}.{CreationBehaviour.varId_fixedDeltaTime} {System.Environment.NewLine}{System.Environment.NewLine}";
        public static string GetDefaultSource(ExecutionLayer layer)
        {
            switch(layer)
            {
                case ExecutionLayer.LoadProgress:
                    return _defaultSource_LoadProgress;

                case ExecutionLayer.SaveProgress:
                    return _defaultSource_SaveProgress;

                case ExecutionLayer.EarlyUpdate:
                case ExecutionLayer.Update:
                case ExecutionLayer.LateUpdate:
                    return _defaultSource_Update;

                case ExecutionLayer.FixedUpdate:
                    return _defaultSource_FixedUpdate;
            }
             
            return string.Empty;
        } 

        public void StartEditingCode(string layerName)
        {
            if (string.IsNullOrWhiteSpace(layerName) || !Scripting.TryParseExecutionLayer(layerName, out var layer)) return; 

            StartEditingCode(layer); 
        } 
        public void StartEditingCode(int layerId) => StartEditingCode((ExecutionLayer)layerId);
        public void StartEditingCode(int layerId, Project proj) => StartEditingCode((ExecutionLayer)layerId, proj);
        public void StartEditingCode(ExecutionLayer layer) => StartEditingCode(layer, ActiveProject);
        public void StartEditingCode(ExecutionLayer layer, Project proj)
        {
            if (proj == null) return;

            ICodeEditor.CodeEditCallback codeEditCallback = null;
            string layerName = layer.ToString();
            string currentSource = string.Empty;
            switch (layer)
            {
                case ExecutionLayer.Load:
                    codeEditCallback = (string sourceCode) =>
                    {
                        if (proj == null || !proj.IsValid)
                        {
                            proj = null;
                            return;
                        }

                        var script = proj.Script;
                        script.source_OnLoadExperience = sourceCode;
                        proj.Script = script;
                    };
                    currentSource = proj.Script.source_OnLoadExperience;
                    break;

                case ExecutionLayer.Unload:
                    codeEditCallback = (string sourceCode) =>
                    {
                        if (proj == null || !proj.IsValid)
                        {
                            proj = null;
                            return;
                        }

                        var script = proj.Script;
                        script.source_OnUnloadExperience = sourceCode;
                        proj.Script = script;
                    };
                    currentSource = proj.Script.source_OnUnloadExperience;
                    break;

                case ExecutionLayer.Begin:
                    codeEditCallback = (string sourceCode) =>
                    {
                        if (proj == null || !proj.IsValid)
                        {
                            proj = null;
                            return;
                        }

                        var script = proj.Script;
                        script.source_OnBeginExperience = sourceCode;
                        proj.Script = script;
                    };
                    currentSource = proj.Script.source_OnBeginExperience;
                    break;

                case ExecutionLayer.End:
                    codeEditCallback = (string sourceCode) =>
                    {
                        if (proj == null || !proj.IsValid)
                        {
                            proj = null;
                            return;
                        }

                        var script = proj.Script;
                        script.source_OnEndExperience = sourceCode;
                        proj.Script = script;
                    };
                    currentSource = proj.Script.source_OnEndExperience;
                    break;

                case ExecutionLayer.Restart:
                    codeEditCallback = (string sourceCode) =>
                    {
                        if (proj == null || !proj.IsValid)
                        {
                            proj = null;
                            return;
                        }

                        var script = proj.Script;
                        script.source_OnRestartExperience = sourceCode;
                        proj.Script = script;
                    };
                    currentSource = proj.Script.source_OnRestartExperience;
                    break;

                case ExecutionLayer.Initialization:
                    codeEditCallback = (string sourceCode) =>
                    {
                        if (proj == null || !proj.IsValid)
                        {
                            proj = null;
                            return;
                        }

                        var script = proj.Script;
                        script.source_OnInitialize = sourceCode;
                        proj.Script = script;
                    };
                    currentSource = proj.Script.source_OnInitialize;
                    break;

                case ExecutionLayer.Enable:
                    codeEditCallback = (string sourceCode) =>
                    {
                        if (proj == null || !proj.IsValid)
                        {
                            proj = null;
                            return;
                        }

                        var script = proj.Script;
                        script.source_OnEnable = sourceCode;
                        proj.Script = script;
                    };
                    currentSource = proj.Script.source_OnEnable;
                    break;

                case ExecutionLayer.Disable:
                    codeEditCallback = (string sourceCode) =>
                    {
                        if (proj == null || !proj.IsValid)
                        {
                            proj = null;
                            return;
                        }

                        var script = proj.Script;
                        script.source_OnDisable = sourceCode;
                        proj.Script = script;
                    };
                    currentSource = proj.Script.source_OnDisable;
                    break;

                case ExecutionLayer.Destroy:
                    codeEditCallback = (string sourceCode) =>
                    {
                        if (proj == null || !proj.IsValid)
                        {
                            proj = null;
                            return;
                        }

                        var script = proj.Script;
                        script.source_OnDestroy = sourceCode;
                        proj.Script = script;
                    };
                    currentSource = proj.Script.source_OnDestroy;
                    break;

                case ExecutionLayer.EarlyUpdate:
                    layerName = "TickEarly";
                    codeEditCallback = (string sourceCode) =>
                    {
                        if (proj == null || !proj.IsValid)
                        {
                            proj = null;
                            return;
                        }

                        var script = proj.Script;
                        script.source_OnEarlyUpdate = sourceCode;
                        proj.Script = script;
                    };
                    currentSource = proj.Script.source_OnEarlyUpdate;
                    break;

                case ExecutionLayer.Update:
                    layerName = "Tick";
                    codeEditCallback = (string sourceCode) =>
                    {
                        if (proj == null || !proj.IsValid)
                        {
                            proj = null;
                            return;
                        }

                        var script = proj.Script;
                        script.source_OnUpdate = sourceCode;
                        proj.Script = script;
                    };
                    currentSource = proj.Script.source_OnUpdate;
                    break;

                case ExecutionLayer.LateUpdate:
                    layerName = "TickLate";
                    codeEditCallback = (string sourceCode) =>
                    {
                        if (proj == null || !proj.IsValid)
                        {
                            proj = null;
                            return;
                        }

                        var script = proj.Script;
                        script.source_OnLateUpdate = sourceCode;
                        proj.Script = script;
                    };
                    currentSource = proj.Script.source_OnLateUpdate;
                    break;

                case ExecutionLayer.FixedUpdate:
                    codeEditCallback = (string sourceCode) =>
                    {
                        if (proj == null || !proj.IsValid)
                        {
                            proj = null;
                            return;
                        }

                        var script = proj.Script;
                        script.source_OnFixedUpdate = sourceCode;
                        proj.Script = script;
                    };
                    currentSource = proj.Script.source_OnFixedUpdate;
                    break;

                case ExecutionLayer.Interaction:
                    codeEditCallback = (string sourceCode) =>
                    {
                        if (proj == null || !proj.IsValid)
                        {
                            proj = null;
                            return;
                        }

                        var script = proj.Script;
                        script.source_OnInteract = sourceCode;
                        proj.Script = script;
                    };
                    currentSource = proj.Script.source_OnInteract;
                    break;

                case ExecutionLayer.CollisionEnter:
                    codeEditCallback = (string sourceCode) =>
                    {
                        if (proj == null || !proj.IsValid)
                        {
                            proj = null;
                            return;
                        }

                        var script = proj.Script;
                        script.source_OnCollisionEnter = sourceCode;
                        proj.Script = script;
                    };
                    currentSource = proj.Script.source_OnCollisionEnter;
                    break;

                case ExecutionLayer.CollisionStay:
                    codeEditCallback = (string sourceCode) =>
                    {
                        if (proj == null || !proj.IsValid)
                        {
                            proj = null;
                            return;
                        }

                        var script = proj.Script;
                        script.source_OnCollisionStay = sourceCode;
                        proj.Script = script;
                    };
                    currentSource = proj.Script.source_OnCollisionStay;
                    break;

                case ExecutionLayer.CollisionExit:
                    codeEditCallback = (string sourceCode) =>
                    {
                        if (proj == null || !proj.IsValid)
                        {
                            proj = null;
                            return;
                        }

                        var script = proj.Script;
                        script.source_OnCollisionExit = sourceCode;
                        proj.Script = script;
                    };
                    currentSource = proj.Script.source_OnCollisionExit;
                    break;

                case ExecutionLayer.TriggerEnter:
                    codeEditCallback = (string sourceCode) =>
                    {
                        if (proj == null || !proj.IsValid)
                        {
                            proj = null;
                            return;
                        }

                        var script = proj.Script;
                        script.source_OnTriggerEnter = sourceCode;
                        proj.Script = script;
                    };
                    currentSource = proj.Script.source_OnTriggerEnter;
                    break;

                case ExecutionLayer.TriggerStay:
                    codeEditCallback = (string sourceCode) =>
                    {
                        if (proj == null || !proj.IsValid)
                        {
                            proj = null;
                            return;
                        }

                        var script = proj.Script;
                        script.source_OnTriggerStay = sourceCode;
                        proj.Script = script;
                    };
                    currentSource = proj.Script.source_OnTriggerStay;
                    break;

                case ExecutionLayer.TriggerExit:
                    codeEditCallback = (string sourceCode) =>
                    {
                        if (proj == null || !proj.IsValid)
                        {
                            proj = null;
                            return;
                        }

                        var script = proj.Script;
                        script.source_OnTriggerExit = sourceCode;
                        proj.Script = script;
                    };
                    currentSource = proj.Script.source_OnTriggerExit;
                    break;

                case ExecutionLayer.LoadProgress:
                    codeEditCallback = (string sourceCode) =>
                    {
                        if (proj == null || !proj.IsValid)
                        {
                            proj = null;
                            return;
                        }

                        var script = proj.Script;
                        script.source_OnLoadProgress = sourceCode;
                        proj.Script = script;
                    };
                    currentSource = proj.Script.source_OnLoadProgress;
                    break;

                case ExecutionLayer.SaveProgress:
                    codeEditCallback = (string sourceCode) =>
                    {
                        if (proj == null || !proj.IsValid)
                        {
                            proj = null;
                            return;
                        }

                        var script = proj.Script;
                        script.source_OnSaveProgress = sourceCode;
                        proj.Script = script;
                    };
                    currentSource = proj.Script.source_OnSaveProgress;  
                    break;
            }

            if (string.IsNullOrEmpty(currentSource)) currentSource = GetDefaultSource(layer);

            CodeEditorWindow editor = default;
            GameObject editorWindow = null;
            RectTransform editorWindowRT = null;
            if (proj.activeCodeEditorWindows.TryGetValue(layer, out editorWindowRT) && editorWindowRT != null) 
            {
                editorWindow = editorWindowRT.gameObject;
                editorWindow.SetActive(true);
                editor = editorWindow.GetComponentInChildren<CodeEditorWindow>(true);    
            }         
            else
            {
                Canvas canvas = null;
                RectTransform canvasRT = null;
                UIResizer editorResizer = null;
                UIPopup editorPopup = null;
                void OnClose(string source)
                {
                    if (proj == null || !proj.IsValid)
                    {
                        proj = null;
                        return;
                    }

                    proj.SetWindowState(layer.ToString(), editorWindowRT.AsResizableWindowState(false, canvasRT));
                    codeEditCallback?.Invoke(source);
                    if (editorPopup != null) editorPopup.OnDragStep?.RemoveAllListeners();
                    if (editorResizer != null) editorResizer.OnDragStep?.RemoveAllListeners(); 
                    proj.activeCodeEditorWindows.Remove(layer);
                    editorPopup = null;
                    editorResizer = null;
                }
                editorWindow = OpenCodeEditorWindow("EDITING SOURCE [" + layerName + "]", out editor, codeEditCallback, OnClose);
                editorWindowRT = editorWindow.GetComponent<RectTransform>();
                canvas = editorWindow.GetComponentInParent<Canvas>();
                canvasRT = canvas == null ? null : canvas.GetComponent<RectTransform>();

                if (proj.TryGetWindowState(layer.ToString(), out var state))
                {
                    state.visible = true;
                    state.ApplyWindowState(editorWindowRT, canvasRT);
                    proj.SetWindowState(layer.ToString(), state); 
                }

                proj.activeCodeEditorWindows[layer] = editorWindowRT;
                if (IsInTestMode)
                {
                    if (IsRecompilableLayer(layer))
                    {
                        var creation = proj.CachedAsset;
                        CustomEditorUtils.SetButtonOnClickActionByName(editorWindow, str_recompile, () =>
                        {
                            if (proj == null || !proj.IsValid)
                            {
                                proj = null;
                                return;
                            }

                            var editor = editorWindow.GetComponentInChildren<CodeEditorWindow>(true);
                            if (editor == null || editor.Editor == null) return;

                            RecompileInTestMode(layer, editor.Editor, creation.Author);
                        });
                    } 
                    else
                    {
                        CustomEditorUtils.SetButtonInteractableByName(editorWindow, str_recompile, false);
                    }
                }
                void OnDrag()
                {
                    if (proj == null || !proj.IsValid)
                    {
                        proj = null;
                        return;
                    }

                    proj.SetWindowState(layer.ToString(), editorWindowRT.AsResizableWindowState(true, canvasRT));
                }
                editorPopup = editor == null || editor.Window == null || editor.Window.rootWindowElement == null ? editorWindow.GetComponentInChildren<UIPopup>(true) : editor.Window.rootWindowElement; 
                if (editorPopup != null)
                {
                    if (editorPopup.OnDragStep == null) editorPopup.OnDragStep = new UnityEvent(); else editorPopup.OnDragStep.RemoveAllListeners();
                    editorPopup.OnDragStep.AddListener(OnDrag);
                }
                editorResizer = editor == null || editor.Window == null || editor.Window.resizer == null ? editorWindow.GetComponentInChildren<UIResizer>(true) : editor.Window.resizer;
                if (editorResizer != null)
                {
                    if (editorResizer.OnDragStep == null) editorResizer.OnDragStep = new UnityEvent(); else editorResizer.OnDragStep.RemoveAllListeners();
                    editorResizer.OnDragStep.AddListener(OnDrag);
                }
            }
            if (editor != null) 
            {
                var win = editor.Window;
                if (win != null)
                {
                    win.gameObject.SetActive(true);
                    if (win.rootWindowElement != null)
                    {
                        win.rootWindowElement.transform.SetAsLastSibling();
                    }
                    else
                    {
                        win.transform.SetAsLastSibling();
                    }
                }

                editor.Editor.Code = currentSource;
            }
             
        }
        public GameObject OpenCodeEditorWindow(string windowTitle, ICodeEditor.CodeEditCallback onValueUpdate, ICodeEditor.CodeEditCallback onClose) => OpenCodeEditorWindow(windowTitle, out _, onValueUpdate, onClose);
        public GameObject OpenCodeEditorWindow(string windowTitle, out CodeEditorWindow editor, ICodeEditor.CodeEditCallback onValueUpdate, ICodeEditor.CodeEditCallback onClose)
        {
            editor = default;
            if (!codeEditorPool.TryGetNewInstance(out GameObject window)) return null;

            window.SetActive(true);
            CustomEditorUtils.SetComponentTextByName(window, str_title, windowTitle);

            editor = window.GetComponentInChildren<CodeEditorWindow>(true);

            if (editor == null) return null;
            if (editor.Window != null)
            {
                var win = editor.Window;
                win.gameObject.SetActive(true);
                if (win.rootWindowElement != null)
                {
                    win.rootWindowElement.transform.SetAsLastSibling();
                } 
                else
                {
                    win.transform.SetAsLastSibling();
                }             
            }

            void OnClose(string code)
            {
                onClose?.Invoke(code);
                codeEditorPool.Release(window);
            }

            var codeEditor = editor.Editor;
            if (codeEditor != null)
            {
                codeEditor.ClearAllListeners();
                codeEditor.ListenForChanges(onValueUpdate);
                codeEditor.ListenForClosure(OnClose);
            }

            return window;
        }

        protected struct ObjectActiveState
        {
            public GameObject obj;
            public bool active;
        }
        protected readonly List<ObjectActiveState> objectActiveStates = new List<ObjectActiveState>();
        public void DisableEditor()
        {
            if (runtimeEditor != null) runtimeEditor.SetDisabled(true);

            objectActiveStates.Clear();
            var proj = ActiveProject;
            if (proj != null)
            {
                if (proj.members != null)
                {
                    foreach (var mem in proj.members) if (mem != null && mem.swoleGameObject != null) 
                        { 
                            objectActiveStates.Add(new ObjectActiveState() { obj = mem.swoleGameObject.gameObject, active = mem.swoleGameObject.gameObject.activeSelf }); 
                            mem.swoleGameObject.gameObject.SetActive(false);
                        }
                }
            }
        }
        public void EnableEditor()
        {
            if (runtimeEditor != null) runtimeEditor.SetDisabled(false);

            foreach (var state in objectActiveStates) if (state.obj != null) state.obj.SetActive(state.active);  
            objectActiveStates.Clear();
        }

        public bool IsRecompilableLayer(ExecutionLayer layer)
        {
            return layer != ExecutionLayer.Load && layer != ExecutionLayer.LoadProgress && layer != ExecutionLayer.Begin;
        }
        public void RecompileInTestMode(ExecutionLayer layer, ICodeEditor codeEditor, string debugAuthor)
        {
            if (testExperience == null || testExperience.CreationInstance == null) return;
            var testBehaviour = testExperience.CreationInstance.Behaviour;
            if (testBehaviour == null) return;

            testBehaviour.Recompile(layer, codeEditor.Code, false, debugAuthor);
            if (layer == ExecutionLayer.Initialization)
            {
                // run initialization again 
                testBehaviour.ExecuteToCompletion(ExecutionLayer.Initialization, CreationBehaviour._initializationCompletionTimeout, testExperience.CreationInstance.Instance is CreationBehaviour cb ? cb.Logger : swole.DefaultLogger);
            }
        }

        protected RuntimeHandler.Experience testExperience;
        public bool IsInTestMode => testExperience != null;
        public void EnterTestMode()
        {
            if (IsInTestMode) return;

            var activeProject = ActiveProject;
            if (activeProject == null) return;

            var creation = activeProject.GetCreation(false);
            if (creation == null)
            {
                swole.LogError("Failed to create creation asset!");
                return;
            }

            DisableEditor();

            testExperience = RuntimeHandler.InitiateNewExperience(creation);
            if (testExperience == null)
            {
                swole.LogError("Failed to create test experience!"); 
                EnableEditor();
                return;
            }

            if (activeProject.activeCodeEditorWindows != null)
            {
                foreach (var pair in activeProject.activeCodeEditorWindows) if (pair.Value != null)
                    {
                        var layer = pair.Key;
                        var window = pair.Value;
                        if (IsRecompilableLayer(layer))
                        {
                            CustomEditorUtils.SetButtonOnClickActionByName(window, str_recompile, () =>
                            {
                                var editor = window.GetComponentInChildren<CodeEditorWindow>(true);
                                if (editor == null || editor.Editor == null) return;

                                RecompileInTestMode(layer, editor.Editor, creation.Author);
                            });
                        }
                        else
                        {
                            CustomEditorUtils.SetButtonInteractableByName(window, str_recompile, false);  
                        }
                    }
            }

            RuntimeHandler.MainCamera = UnityEngineHook.AsSwoleCamera(TestingCamera);
            
            var editorCam = MainCamera; // the default main camera in the unity scene
            var testCam = TestingCamera;

            testCam.fieldOfView = testingCamFOV;
            testCam.nearClipPlane = testingCamNearClip;  
            testCam.farClipPlane = testingCamFarClip;
            var camT = UnityEngineHook.AsSwoleGameObject(testingCamera.gameObject).transform; // use swole transform because cam is likely using a proxy
            camT.SetPositionAndRotation(UnityEngineHook.AsSwoleVector(editorCam.transform.position), UnityEngineHook.AsSwoleQuaternion(editorCam.transform.rotation)); // start in same position as the editor camera
            //camT.localScale = testingCamScale; 
             
            editorCam.gameObject.SetActive(false);
            testCam.gameObject.SetActive(true);

            swole.State = swole.RuntimeState.EditorPlayTest;
            if (playButton != null && stopButton != null) playButton.SetActive(false);
            if (stopButton != null) stopButton.SetActive(true);

            IEnumerator Load()
            {
                yield return null;
                testExperience.Load();
                yield return null;
                testExperience.Begin();
                if (loadingOverlay != null) loadingOverlay.SetActive(false);
            }

            if (loadingOverlay != null) loadingOverlay.SetActive(true);

            StartCoroutine(Load());

        }
        protected bool paused;
        public bool IsPaused => paused;
        public void PauseTestMode()
        {
            if (paused || !IsInTestMode) return;

            paused = true;

            if (pauseButton != null && unpauseButton != null) pauseButton.SetActive(false);
            if (unpauseButton != null) unpauseButton.SetActive(true); 
        }
        public void UnpauseTestMode()
        {
            if (!paused || !IsInTestMode) return;  

            paused = false;

            if (pauseButton != null) pauseButton.SetActive(true);
            if (unpauseButton != null) unpauseButton.SetActive(false);
        }
        public void EndTestMode()
        {
            UnpauseTestMode();

            swole.State = swole.RuntimeState.Editor;
            if (playButton != null) playButton.SetActive(true);
            if (stopButton != null) stopButton.SetActive(false);

            var activeProject = ActiveProject;
            if (activeProject != null)
            {
                if (activeProject.activeCodeEditorWindows != null)
                {
                    foreach (var pair in activeProject.activeCodeEditorWindows) if (pair.Value != null) CustomEditorUtils.SetButtonInteractableByName(pair.Value, str_recompile, false);
                }
            }

            if (!IsInTestMode) return;

            MainCamera.gameObject.SetActive(true);
            TestingCamera.gameObject.SetActive(false); 

            testExperience.Invalidate();
            testExperience = null;

            EnableEditor();
        }
         
        protected virtual void RecordManipulationAction(List<Transform> affectedTransforms, Vector3 relativeOffset, Quaternion relativeRotation, Vector3 relativeScale, Vector3 gizmoWorldPosition, Quaternion gizmoWorldRotation)
        {
            var proj = ActiveProject;
            if (proj == null) return;

            bool flag = false;
            foreach (var transform in affectedTransforms)
            {
                if (proj.IsAssociatedWith(transform))
                {
                    flag = true;
                    break;
                }
            }
             
            if (flag) proj.MarkAsDirty();     
        }

        private static readonly List<SwoleGameObject> duplicateRoots = new List<SwoleGameObject>();
        protected virtual void OnDuplicateObjects(List<GameObject> objects)
        {
            duplicateRoots.Clear();
            foreach(var obj in objects) if (obj != null)
                {
                    duplicateRoots.AddRange(obj.GetComponentsInChildren<SwoleGameObject>(true));
                }
             
            foreach(var root in duplicateRoots)
            {
                var go = root.gameObject;
                go.name.Replace("(Clone)", string.Empty);
                // TODO: revise this when ids are no longer added to names
                int endIndex = go.name.LastIndexOf('('); // most likely an id
                if (endIndex >= 0) go.name = go.name.Substring(0, endIndex).TrimEnd();
                CreateNewMember(go);
            }
        }

        protected virtual void OnDeleteObjects(List<GameObject> objects)
        {
            var proj = ActiveProject;
            if (proj == null) return;

            bool flag = false;
            foreach (var obj in objects)
            {
                bool flag2 = true;
                while(flag2)
                {
                    flag2 = false;
                    if (proj.members == null) break;
                    for(int a = 0; a < proj.members.Count; a++)
                    {
                        var mem = proj.members[a];
                        if (mem == null || mem.swoleGameObject == null) continue;
                        if (obj == mem.swoleGameObject.gameObject)
                        {
                            flag = true;
                            flag2 = true;
                            proj.RemoveMember(mem);
                            break;
                        }
                    }
                } 
            }

            if (flag) proj.MarkAsDirty();
        }

    }

}

#endif
