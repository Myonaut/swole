#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

using UnityEngine.UI;
using TMPro;

using Swole.API.Unity;

namespace Swole.UI
{
    public class UICategorizedList : MonoBehaviour
    {

        public RectTransform categoryPrototype;
        public RectTransform listMemberPrototype;

        public RectTransform layoutTransform;

        public bool allowCategoryCollapse = true;

        public bool usePrefabPool = true;
        private PrefabPool prefabPool;

        [NonSerialized]
        private bool initialized=false;

        protected void Awake()
        {
            if (!initialized) Reinitialize();
        }

        public void Reinitialize()
        {
            initialized = false;

            Clear();

            if (usePrefabPool)
            {
                if (prefabPool == null)
                {
                    prefabPool = gameObject.AddOrGetComponent<PrefabPool>();
                    prefabPool.Reinitialize(listMemberPrototype.gameObject, PoolGrowthMethod.Incremental, 1, 1, 2048);
                }
            }
            categoryPrototype.gameObject.SetActive(false);
            listMemberPrototype.gameObject.SetActive(false);

            if (layoutTransform == null) layoutTransform = gameObject.GetComponent<RectTransform>();

            initialized = true;
        }

        public class Member
        {
            public string name;
            public RectTransform rectTransform;
            public GameObject gameObject;
            public RectTransform buttonRT;

            public Category category;
            public int Index
            {
                get
                {
                    if (category == null || category.members == null) return -1;
                    return category.members.IndexOf(this);
                }
                set
                {
                    if (category == null || category.members == null) return;

                    int currentIndex = category.members.IndexOf(this);
                    if (currentIndex >= 0) 
                    { 
                        category.members.RemoveAt(currentIndex); 
                    }

                    int index = value;
                    if (index >= category.members.Count || category.members.Count <= 0) 
                    {
                        index = category.members.Count;
                        category.members.Add(this); 
                    } 
                    else
                    {
                        index = Mathf.Max(0, value);
                        category.members.Insert(index, this); 
                    }

                    if (rectTransform != null && category.rectTransform != null) rectTransform.SetSiblingIndex(category.rectTransform.GetSiblingIndex() + 1 + index); // + 1 to position after category
                }
            }

            public void SetName(string name)
            {
                this.name = name;

                if (gameObject != null)
                {
                    var text = gameObject.GetComponentInChildren<Text>(true);
                    var textTMP = gameObject.GetComponentInChildren<TMP_Text>(true);
                    if (text != null)
                    {
                        text.text = name;
                    }
                    if (textTMP != null)
                    {
                        textTMP.SetText(name);
                    }
                }
            }
        }

        public class Category
        { 

            public string name;
            public RectTransform rectTransform;
            public GameObject gameObject;
            public bool expanded;
            public bool IsExpanded => expanded;
            public List<Member> members = new List<Member>();

            public int MemberCount => members == null ? 0 : members.Count;
            public Member GetMember(int memberIndex) => members == null ? null : members[memberIndex];
            public Member this[int memberIndex] => GetMember(memberIndex);

            public Text text;
            public TMP_Text textTMP;
            public void SetDisplayName(string name)
            {
                if (text != null)
                {
                    text.text = name;
                }
                if (textTMP != null)
                {
                    textTMP.SetText(name);
                }
            }

            public void SortByIndex()
            {
                if (members == null) return;

                var containerTransform = rectTransform.parent;
                for (int i = members.Count - 1; i >= 0; i--)
                {
                    var mem = members[i];
                    if (mem.gameObject == null || mem.rectTransform == null) continue;
                    mem.gameObject.SetActive(true);
                    int index = rectTransform.GetSiblingIndex() + 1;
                    if (index >= containerTransform.childCount)
                    {
                        mem.rectTransform.SetAsLastSibling();
                    }
                    else
                    {
                        mem.rectTransform.SetSiblingIndex(index);
                    }
                }
            }

            public UnityEvent OnExpand = new UnityEvent();
            public void Expand()
            {
                expanded = true;
                if (rectTransform == null || members == null) return;

                SortByIndex();

                var exp = rectTransform.FindDeepChildLiberal("expand");
                if (exp != null) exp.gameObject.SetActive(false);
                var ret = rectTransform.FindDeepChildLiberal("retract");
                if (ret != null) ret.gameObject.SetActive(true);

                OnExpand?.Invoke();
            }

            public UnityEvent OnRetract = new UnityEvent();
            public void Retract()
            {
                expanded = false;
                if (members == null) return;

                foreach (var mem in members) 
                {
                    if (mem.gameObject == null) continue;
                    mem.gameObject.SetActive(false); 
                }

                var exp = rectTransform.FindDeepChildLiberal("expand");
                if (exp != null) exp.gameObject.SetActive(true);
                var ret = rectTransform.FindDeepChildLiberal("retract");
                if (ret != null) ret.gameObject.SetActive(false);

                OnRetract?.Invoke();
            }
            public void Toggle()
            {
                if (expanded) Retract(); else Expand();
            }

        }

        private Dictionary<string, bool> preservedCategoryStates;
        public void Clear(bool includeCategories = true, bool preserveCategoryStates = false)
        {
            if (categories == null) categories = new List<Category>();
            if (preserveCategoryStates) preservedCategoryStates = new Dictionary<string, bool>(); else preservedCategoryStates = null;
            foreach (var cat in categories)
            {
                if (cat == null || cat.members == null) continue;

                if (preservedCategoryStates != null) preservedCategoryStates[cat.name] = cat.expanded; 

                foreach (var mem in cat.members)
                {
                    if (mem == null || mem.gameObject == null) continue;

                    if (prefabPool == null)
                    {
                        GameObject.Destroy(mem.gameObject);
                    }
                    else
                    {
                        prefabPool.Release(mem.gameObject);
                    }
                }

                cat.members.Clear();
                if (includeCategories && cat.gameObject != null) GameObject.Destroy(cat.gameObject);
            }
            if (includeCategories) categories.Clear();
        }

        private List<Category> categories = new List<Category>();

        public int CategoryCount => categories == null ? 0 : categories.Count;
        public Category GetCategory(int categoryIndex) => categories == null ? null : categories[categoryIndex];
        public Category this[int categoryIndex] => GetCategory(categoryIndex);

        public Category AddNewCategory(string categoryName, Sprite categoryIcon=null)
        {
            if (!initialized) Reinitialize();
            var inst = GameObject.Instantiate(categoryPrototype.gameObject);
            inst.name = categoryName;
            inst.SetActive(true);
            var rt = inst.AddOrGetComponent<RectTransform>();
            rt.SetParent(layoutTransform, false);
            rt.SetAsLastSibling();

            Category category = new Category() { gameObject=inst, name=categoryName, rectTransform=rt };
            categories.Add(category);

            category.text = inst.GetComponentInChildren<Text>(true);
            category.textTMP = inst.GetComponentInChildren<TMP_Text>(true);

            if (category.text != null) category.text.text = categoryName;  
            if (category.textTMP != null) category.textTMP.SetText(categoryName);        

            if (categoryIcon != null)
            {
                var iconObj = rt.FindDeepChildLiberal("icon");
                if (iconObj != null)
                {
                    var iconImg = iconObj.GetComponent<Image>();
                    if (iconImg != null)
                    {
                        iconImg.sprite = categoryIcon;
                    }
                }
            }

            if (allowCategoryCollapse)
            {

                var button = inst.GetComponentInChildren<Button>();
                var tabButton = inst.GetComponentInChildren<UITabButton>();

                if (button != null)
                {
                    if (button.onClick == null) button.onClick = new Button.ButtonClickedEvent();
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(category.Toggle);
                }
                if (tabButton != null)
                {
                    if (tabButton.OnClick == null) tabButton.OnClick = new UnityEvent();
                    tabButton.OnClick.RemoveAllListeners();
                    tabButton.OnClick.AddListener(category.Toggle);
                }

                if (preservedCategoryStates != null && preservedCategoryStates.TryGetValue(categoryName, out var state))
                {
                    if (state) category.Expand(); else category.Retract();
                    preservedCategoryStates.Remove(categoryName);
                }

            }
            
            return category;
        }
        public Category AddOrGetCategory(string categoryName, Sprite categoryIcon = null)
        {
            if (!initialized) Reinitialize();
            foreach (var category in categories) if (category.name.AsID() == categoryName.AsID()) return category;
            return AddNewCategory(categoryName, categoryIcon);
        }
        public Category FindCategory(string categoryName)
        {
            if (!initialized) return null;
            categoryName = categoryName.AsID();
            foreach (var category in categories) if (category.name.AsID() == categoryName) return category;
            return null;
        }

        public void SortCategoryListBySiblingIndex()
        {
            if (categories == null) return;
            categories.Sort((Category x, Category y) => (int)Mathf.Sign(x.rectTransform.GetSiblingIndex() - y.rectTransform.GetSiblingIndex()));
        }
        public void MoveCategory(Category category, int index)
        {
            if (category == null || category.rectTransform == null || categories == null || !categories.Contains(category)) return;

            SortCategoryListBySiblingIndex();

            index = Mathf.Clamp(index, 0, categories.Count - 1);
            var toNudge = categories[index];

            int siblingIndex = toNudge.rectTransform.GetSiblingIndex(); 
            category.rectTransform.SetSiblingIndex(siblingIndex);
            if (category.members != null)
            {
                for (int a = category.members.Count - 1; a >= 0; a--) 
                { 
                    var mem = categories[a];
                    if (mem.rectTransform != null) mem.rectTransform.SetSiblingIndex(siblingIndex + 1); 
                }
            }
        }

        protected GameObject GetNewListMemberInstance(string memberName)
        {
            if (!prefabPool.TryGetNewInstance(out GameObject inst)) return null;

            inst.name = memberName;

            var text = inst.GetComponentInChildren<Text>(true);
            var textTMP = inst.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                text.text = memberName;
            }
            if (textTMP != null)
            {
                textTMP.SetText(memberName);
            }
            return inst;
        }
        public Member AddNewListMember(string memberName, string category, UnityAction onClick=null, Sprite categoryIcon=null)
        {
            if (!initialized) Reinitialize();
            GameObject inst = GetNewListMemberInstance(memberName);
            if (inst == null) return null;

            return AddNewListMember(inst, AddOrGetCategory(category, categoryIcon), onClick);
        }

        public Member AddNewListMember(string memberName, Category category, UnityAction onClick = null)
        {
            if (!initialized) Reinitialize();
            GameObject inst = GetNewListMemberInstance(memberName);
            if (inst == null) return null;

            return AddNewListMember(inst, category, onClick);
        }

        protected Member AddNewListMember(GameObject inst, Category category, UnityAction onClick = null)
        {
            RectTransform buttonRT = null;

            var button = inst.GetComponent<Button>();
            var tabButton = inst.GetComponent<UITabButton>();

            if (button == null && tabButton == null)
            {
                button = inst.GetComponentInChildren<Button>(false);
                if (button == null) tabButton = inst.GetComponentInChildren<UITabButton>(false);
            }

            if (button != null)
            {
                if (button.onClick == null) button.onClick = new Button.ButtonClickedEvent();
                button.onClick.RemoveAllListeners();
                if (onClick != null) button.onClick.AddListener(onClick);
                buttonRT = button.GetComponent<RectTransform>();
            }
            if (tabButton != null)
            {
                if (tabButton.OnClick == null) tabButton.OnClick = new UnityEvent(); 
                tabButton.OnClick.RemoveAllListeners();
                if (onClick != null) tabButton.OnClick.AddListener(onClick);
                buttonRT = tabButton.GetComponent<RectTransform>();
            }


            Member member = new Member() { category = category, gameObject = inst, name = inst.name, rectTransform = inst.GetComponent<RectTransform>(), buttonRT = buttonRT };

            category.members.Add(member);

            member.rectTransform.SetParent(layoutTransform, false);
            int index = category.rectTransform.GetSiblingIndex() + 1;
            if (index >= layoutTransform.childCount)
            {
                member.rectTransform.SetAsLastSibling();
            }
            else
            {
                member.rectTransform.SetSiblingIndex(index);
            }

            if (allowCategoryCollapse)
            {
                member.gameObject.SetActive(category.expanded);
                if (category.expanded) category.SortByIndex();
            }
            else
            {
                member.gameObject.SetActive(true);
                category.SortByIndex();
            }

            return member;
        }

        public bool RemoveListMember(Member member)
        {
            if (!initialized) return false;
            if (member == null) return false;

            bool flag = false;
            foreach(var category in categories)
            {
                if (category == null || category.members == null) continue;
                int removed = category.members.RemoveAll(i => i == member);
                flag = flag || removed > 0;
            }

            if (member.gameObject != null) prefabPool?.Release(member.gameObject);

            return flag;
        }
        public bool RemoveListMember(string memberName, string category=null)
        {
            if (!initialized) return false;
            if (string.IsNullOrEmpty(memberName)) return false;

            bool removed = false;
            if (string.IsNullOrEmpty(category))
            {
                bool flag = false;
                foreach(var c in categories)
                {
                    if (c == null || c.members == null) continue;
                    flag = true;
                    while (flag)
                    {
                        flag = false;
                        for(int i = 0; i < c.members.Count; i++)
                        {
                            var mem = c.members[i];
                            if (mem == null || mem.name == memberName)
                            {
                                flag = true;
                                removed = true;
                                c.members.RemoveAt(i);
                                prefabPool?.Release(mem.gameObject);
                            }
                        }
                    }
                }
                return removed;
            }

            var cat = FindCategory(category);
            if (cat != null && cat.members != null)
            {
                bool flag = true;
                while (flag)
                {
                    flag = false;
                    for (int i = 0; i < cat.members.Count; i++)
                    {
                        var mem = cat.members[i];
                        if (mem == null || mem.name == memberName)
                        {
                            flag = true;
                            removed = true;
                            cat.members.RemoveAt(i);
                            prefabPool?.Release(mem.gameObject);
                        }
                    }
                }
            }

            return removed;
        }

        public bool DeleteCategory(string categoryName)
        {
            if (!initialized) return false;
            if (string.IsNullOrEmpty(categoryName)) return false;

            categoryName = categoryName.AsID();

            int index = -1;
            for(int i = 0; i < categories.Count;i++)
            {
                var category = categories[i];
                if (category.name.AsID() == categoryName)
                {
                    index = i;
                    break;
                }
            }

            return DeleteCategory(index);
        }
        public bool DeleteCategory(Category category)
        {
            if (!initialized) return false;
            if (category == null) return false;

            return DeleteCategory(categories.IndexOf(category));
        }
        public bool DeleteCategory(int index)
        {
            if (index >= 0)
            {
                var category = categories[index];
                categories.RemoveAt(index);

                if (category != null)
                {
                    if (category.members != null)
                    {
                        if (prefabPool != null)
                        {
                            foreach (var member in category.members)
                            {
                                prefabPool.Release(member.gameObject);
                            }
                        }
                        else
                        {
                            foreach (var member in category.members)
                            {
                                if (member.gameObject != null) GameObject.Destroy(member.gameObject);
                            }
                        }
                        category.members.Clear();
                    }
                    if (category.gameObject != null)
                    {
                        GameObject.Destroy(category.gameObject);
                    }
                }

                return true;
            }

            return false;
        }

        public void ClearFilters()
        {
            foreach (var cat in categories)
            {
                if (cat.gameObject != null) cat.gameObject.SetActive(true);
                if (cat.expanded) cat.Expand(); else cat.Retract(); 
            }
        }

        private readonly HashSet<Member> visibleMembersFilter = new HashSet<Member>();
        private readonly HashSet<Category> visibleCategoriesFilter = new HashSet<Category>();
        private readonly HashSet<Category> fullyVisibleCategoriesFilter = new HashSet<Category>();
        public void FilterMembersAndCategoriesByStartString(string str) => FilterMembersAndCategoriesByStartString(str, false);
        public void FilterMembersAndCategoriesByStartString(string str, bool caseSensitive)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                ClearFilters(); 
            }
            else
            {
                visibleMembersFilter.Clear();
                visibleCategoriesFilter.Clear();
                fullyVisibleCategoriesFilter.Clear();

                string originalStr = str;
                if (!caseSensitive) str = str.ToLower();

                foreach (var cat in categories)
                {
                    if (cat.members != null)
                    {
                        bool expanded = cat.expanded;
                        cat.Expand();
                        bool flag = !expanded;
                        foreach (var mem in cat.members)
                        {
                            if (mem.gameObject != null)
                            {
                                string memName = mem.name;
                                if (!caseSensitive && memName != null) memName = memName.ToLower();

                                if (AssetFiltering.ContainsStartString(memName, str) || AssetFiltering.ContainsCapitalizedWord(mem.name, originalStr))
                                {
                                    visibleMembersFilter.Add(mem);
                                    visibleCategoriesFilter.Add(cat);
                                    flag = false;
                                }
                                else
                                {
                                    mem.gameObject.SetActive(false);
                                }
                            }
                        }

                        if (flag) cat.Retract();
                    }
                }

                foreach (var cat in categories)
                {
                    if (visibleCategoriesFilter.Contains(cat)) continue;
                    
                    string catName = cat.name;
                    if (!caseSensitive && catName != null) catName = catName.ToLower();

                    if (AssetFiltering.ContainsStartString(catName, str) || AssetFiltering.ContainsCapitalizedWord(cat.name, originalStr))
                    {
                        fullyVisibleCategoriesFilter.Add(cat); 
                    }
                    else
                    {
                        cat.gameObject.SetActive(false);
                    }
                }

                foreach (var mem in visibleMembersFilter) if (mem.gameObject != null) mem.gameObject.SetActive(true);
                foreach (var cat in visibleCategoriesFilter) if (cat.gameObject != null) cat.gameObject.SetActive(true);
                 
                foreach (var cat in fullyVisibleCategoriesFilter)
                {
                    if (cat.gameObject != null) cat.gameObject.SetActive(true);
                    if (cat.expanded) cat.Expand(); 
                }
            }
        }

    }
}

#endif