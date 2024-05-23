#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

using TMPro;

using Swole.UI;

namespace Swole.API.Unity
{
    public class ObjectAnalyzer : MonoBehaviour
    {

        public bool test;

        public void Update()
        {
            if (test)
            {
                test = false;
                Refresh();
            }
        }

        public string GetObjectInfo(GameObject obj)
        {
            if (obj == null) return "Instance is invalid!";
            Transform objTransform = obj.transform;

            string info = string.Empty;

            info = info + $"Name: {obj.name}";
            if (objTransform.parent != null)
            {
                info = info + Environment.NewLine + $"Parent: {objTransform.parent.name}";
            }

            Vector3 pos, rot, scale;

            info = info + Environment.NewLine;
            pos = objTransform.localPosition;
            info = info + Environment.NewLine + $"Local Position: ({pos.x}, {pos.y}, {pos.z})";
            rot = objTransform.localRotation.eulerAngles;
            info = info + Environment.NewLine + $"Local Rotation (Euler): ({rot.x}, {rot.y}, {rot.z})";
            scale = objTransform.localPosition;
            info = info + Environment.NewLine + $"Local Scale: ({scale.x}, {scale.y}, {scale.z})";

            if (targetRoot != null)
            {
                info = info + Environment.NewLine;

                pos = targetRoot.InverseTransformPoint(objTransform.position);
                info = info + Environment.NewLine + $"Root Position: ({pos.x}, {pos.y}, {pos.z})";
                rot = (Quaternion.Inverse(targetRoot.rotation) * objTransform.rotation).eulerAngles;
                info = info + Environment.NewLine + $"Root Rotation (Euler): ({rot.x}, {rot.y}, {rot.z})";
            }

            info = info + Environment.NewLine;
            pos = objTransform.position;
            info = info + Environment.NewLine + $"World Position: ({pos.x}, {pos.y}, {pos.z})";
            rot = objTransform.rotation.eulerAngles;
            info = info + Environment.NewLine + $"World Rotation (Euler): ({rot.x}, {rot.y}, {rot.z})"; 

            return info;
        }

        public int maxVisibleEntries = 2048;

        public Color activeObjectTextColor = Color.white;
        public Color inactiveObjectTextColor = Color.red;

        public GameObject objectEntryPrototype;

        public Transform layoutTransform;

        public Text infoText;
        public TMP_Text infoTextTMP;

        protected PrefabPool entryPool;
        protected UITabGroup tabGroup;

        public Transform targetRoot;

        protected class ObjectEntry
        {

            public bool isExpanded;

            public int siblingIndex;

            public GameObject entryObj;

        }

        protected readonly Dictionary<GameObject, ObjectEntry> linkedObjects = new Dictionary<GameObject, ObjectEntry>();

        protected virtual void Awake()
        {

            entryPool = gameObject.AddComponent<PrefabPool>();

            entryPool.SetContainerTransform(layoutTransform, false, false);
            entryPool.Reinitialize(objectEntryPrototype, PoolGrowthMethod.Multiplicative, 2, 100, maxVisibleEntries);

            tabGroup = gameObject.AddOrGetComponent<UITabGroup>();
            tabGroup.allowNullActive = true;

        }

        protected readonly List<ObjectEntry> tempEntryList = new List<ObjectEntry>();
        protected readonly List<GameObject> objectList = new List<GameObject>();
        public virtual void ReorderEntryList()
        {
            tempEntryList.Clear();

            int siblingIndex = 0;
            void EvaluateObject(GameObject obj)
            {
                Transform objTransform = obj.transform;

                if (linkedObjects.TryGetValue(obj, out var entry))
                {
                    tempEntryList.Add(entry);
                    entry.siblingIndex = siblingIndex;
                    siblingIndex++;

                    for (int a = 0; a < objTransform.childCount; a++) EvaluateObject(objTransform.GetChild(a).gameObject);
                }
            }

            for(int a = 0; a < objectList.Count; a++)
            {
                var obj = objectList[a];
                if (obj == null) continue;
                EvaluateObject(obj);
            }

            tempEntryList.Sort((ObjectEntry entryA, ObjectEntry entryB) =>
            {

                if (entryA == null) return entryB == null ? 0 : 1;
                if (entryB == null) return entryA == null ? 0 : -1;

                return entryA.siblingIndex.CompareTo(entryB.siblingIndex);

            });

            for(int a = 0; a < tempEntryList.Count; a++)
            {
                var entry = tempEntryList[a];
                if (entry == null) continue;
                entry.entryObj.transform.SetSiblingIndex(a);
            }
        }

        public virtual void Refresh()
        {

            objectList.Clear();

            void RetractObject(GameObject obj, bool self = true, bool reorderList = false)
            {
                if (linkedObjects.TryGetValue(obj, out var entry))
                {
                    if (self)
                    {
                        linkedObjects.Remove(obj);
                        entryPool.Release(entry.entryObj);
                        entry.entryObj.SetActive(false);

                        var tabButton = entry.entryObj.GetComponentInChildren<UITabButton>();
                        if (tabButton != null)
                        {
                            tabButton.OnClick?.RemoveAllListeners();
                            tabGroup.Remove(tabButton);
                        }
                    }
                    else
                    {
                        entry.isExpanded = false;

                        var expand = entry.entryObj.transform.FindDeepChildLiberal("expand");
                        if (expand != null)
                        {
                            expand.gameObject.SetActive(true);
                        }

                        var retract = entry.entryObj.transform.FindDeepChildLiberal("retract");
                        if (retract != null)
                        {
                            retract.gameObject.SetActive(false);
                        }
                    }
                }

                Transform transform = obj.transform;
                for (int a = 0; a < transform.childCount; a++) RetractObject(transform.GetChild(a).gameObject);

                if (reorderList) ReorderEntryList();
            }

            void EvaluateObject(GameObject obj, GameObject parentObj, int depth = 0, bool reorderList = false)
            {
                Transform objTransform = obj.transform;
                Transform parentTransform = parentObj == null ? null : parentObj.transform;
                if (!linkedObjects.TryGetValue(obj, out var entry))
                {
                    if (entryPool.TryGetNewInstance(out GameObject entryObj))
                    {
                        entry = new ObjectEntry() { isExpanded = false, entryObj = entryObj };

                        var tabButton = entryObj.GetComponentInChildren<UITabButton>();
                        if (tabButton != null)
                        {
                            tabGroup.Add(tabButton);

                            tabButton.isHovering = false;
                            tabButton.toggle = false;
                            tabButton.UpdateGraphic();

                            if (tabButton.OnClick == null) tabButton.OnClick = new UnityEvent();
                            tabButton.OnClick.RemoveAllListeners();

                            tabButton.OnClick.AddListener(() => {

                                string text = GetObjectInfo(obj);

                                if (infoText != null) infoText.text = text;
                                if (infoTextTMP != null) infoTextTMP.SetText(text);

                            });
                        }

                        var expand = entryObj.transform.FindDeepChildLiberal("expand");
                        if (expand != null)
                        {
                            expand.gameObject.SetActive(objTransform.childCount > 0);
                            var button = expand.GetComponentInChildren<Button>(true);
                            if (button != null)
                            {
                                if (button.onClick == null) button.onClick = new Button.ButtonClickedEvent();
                                button.onClick.RemoveAllListeners();
                                button.onClick.AddListener(() => 
                                {                
                                    entry.isExpanded = true;
                                    EvaluateObject(obj, parentObj, depth, true);
                                });
                            }
                        }

                        var retract = entryObj.transform.FindDeepChildLiberal("retract");
                        if (retract != null)
                        {
                            retract.gameObject.SetActive(false);
                            var button = retract.GetComponentInChildren<Button>(true);
                            if (button != null)
                            {
                                if (button.onClick == null) button.onClick = new Button.ButtonClickedEvent();
                                button.onClick.RemoveAllListeners();
                                button.onClick.AddListener(() => 
                                { 
                                    RetractObject(obj, false);
                                });
                            }
                        }

                        /*int siblingIndex = -1;
                        if (parentObj != null && linkedObjects.TryGetValue(parentObj, out var parentEntry))
                        {
                            siblingIndex = parentEntry.entryObj.transform.GetSiblingIndex() + 1;
                        }*/

                        Transform entryTransform = entryObj.transform;
                        entryTransform.SetParent(layoutTransform);
                        //if (siblingIndex < 0) siblingIndex = entryTransform.GetSiblingIndex();
                        /*if (parentObj != null)
                        {
                            void CountVisibleSiblings(Transform t)
                            {
                                if (linkedObjects.TryGetValue(t.gameObject, out ObjectEntry entry))
                                {
                                    siblingIndex++;

                                    for(int a = 0; a < t.childCount; a++)
                                    {
                                        CountVisibleSiblings(t.GetChild(a));
                                    }
                                }
                            }

                            for(int a = 0; a < parentTransform.childCount; a++)
                            {
                                Transform child = parentTransform.GetChild(a);
                                if (child == objTransform) break;
                                CountVisibleSiblings(child);
                            }
                        }*/

                        //entryTransform.SetSiblingIndex(siblingIndex);
                        entryObj.SetActive(true);
                    }
                    else return;

                    linkedObjects[obj] = entry;
                }

                if (entry.isExpanded)
                {
                    var expand = entry.entryObj.transform.FindDeepChildLiberal("expand");
                    if (expand != null)
                    {
                        expand.gameObject.SetActive(false);
                    }
                    var retract = entry.entryObj.transform.FindDeepChildLiberal("retract");
                    if (retract != null)
                    {
                        retract.gameObject.SetActive(objTransform.childCount > 0);
                    }

                    for (int a = 0; a < objTransform.childCount; a++) EvaluateObject(objTransform.GetChild(a).gameObject, obj, depth + 1);
                } 
                else
                {
                    var expand = entry.entryObj.transform.FindDeepChildLiberal("expand");
                    if (expand != null)
                    {
                        expand.gameObject.SetActive(objTransform.childCount > 0);
                    }
                    var retract = entry.entryObj.transform.FindDeepChildLiberal("retract");
                    if (retract != null)
                    {
                        retract.gameObject.SetActive(false);
                    }
                }

                var text = entry.entryObj.transform.FindDeepChildLiberal("text");
                if (text != null)
                {
                    var textComp = text.gameObject.GetComponent<Text>();
                    var textCompTMP = text.gameObject.GetComponent<TMP_Text>();
                    
                    string name = (depth > 0 ? (new string(' ', depth * 2) + "╚ ") : string.Empty) + obj.name;

                    if (textComp != null) 
                    {
                        textComp.color = obj.activeInHierarchy ? activeObjectTextColor : inactiveObjectTextColor;
                        textComp.text = name; 
                    }
                    if (textCompTMP != null) 
                    {
                        textCompTMP.color = obj.activeInHierarchy ? activeObjectTextColor : inactiveObjectTextColor;
                        textCompTMP.SetText(name); 
                    }
                }

                if (reorderList) ReorderEntryList();  
            }

            if (targetRoot == null) // If there is no root object, get all top level objects in the active scene. (Won't get objects in additive scenes)
            {
                SceneManager.GetActiveScene().GetRootGameObjects(objectList);
                foreach (var obj in objectList) EvaluateObject(obj, null);
            }
            else
            {
                for (int a = 0; a < targetRoot.childCount; a++) 
                {
                    var obj = targetRoot.GetChild(a).gameObject;
                    objectList.Add(obj);
                    EvaluateObject(obj, null); 
                }
            }

            ReorderEntryList();
        }

    }
}

#endif