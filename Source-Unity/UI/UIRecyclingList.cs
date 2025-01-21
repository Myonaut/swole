#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using Swole.API.Unity;

namespace Swole.UI
{
    public class UIRecyclingList : UIBehaviour, IPointerEnterHandler, IPointerExitHandler
    {

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ClearAllListeners();
        }

        public bool autoRefreshChildLists = true;
        public bool setDisableMemberButtonsWithNoOnClick = true;
        public bool membersAreNotButtons;

        [Serializable]
        public enum Ordering
        {
            TopToBottom, BottomToTop, LeftToRight, RightToLeft
        }
        [SerializeField]
        private Ordering ordering;
        public void SetOrdering(Ordering ordering)
        {
            this.ordering = ordering;
            Refresh();
        }
        public Ordering Order
        {
            get => ordering;
            set => SetOrdering(value);
        }

        private HorizontalOrVerticalLayoutGroup layoutGroup;
        private List<Transform> toParent = new List<Transform>();
        protected void RefreshLayout()
        {
            toParent.Clear();
            void DestroyPreviousGroup()
            {
                if (layoutGroup == null) return;

                var t = layoutGroup.transform;
                while(t.childCount > 0)
                {
                    var child = t.GetChild(0);
                    toParent.Add(child);
                    child.SetParent(RectTransform, false); 
                }
                Destroy(layoutGroup.gameObject);
                layoutGroup = null;
            } 

            VerticalLayoutGroup vlg;
            HorizontalLayoutGroup hlg;
            switch (ordering)
            {
                case Ordering.TopToBottom:
                    if (layoutGroup == null || layoutGroup is not VerticalLayoutGroup)
                    {
                        DestroyPreviousGroup();

                        layoutGroup = vlg = new GameObject("layout").AddComponent<VerticalLayoutGroup>();

                        vlg.childAlignment = TextAnchor.UpperCenter;
                        vlg.childControlWidth = true;
                        vlg.childForceExpandWidth = true;
                        vlg.childScaleWidth = false;
                        vlg.childControlHeight = false;
                        vlg.childForceExpandHeight = false;
                        vlg.childScaleHeight = false;
                    }
                    break;
                case Ordering.BottomToTop:
                    if (layoutGroup == null || layoutGroup is not VerticalLayoutGroup)
                    {
                        DestroyPreviousGroup();

                        layoutGroup = vlg = new GameObject("layout").AddComponent<VerticalLayoutGroup>();

                        vlg.childAlignment = TextAnchor.LowerCenter;
                        vlg.childControlWidth = true;
                        vlg.childForceExpandWidth = true;
                        vlg.childScaleWidth = false;
                        vlg.childControlHeight = false;
                        vlg.childForceExpandHeight = false;
                        vlg.childScaleHeight = false;
                    }
                    break;
                case Ordering.LeftToRight:
                    if (layoutGroup == null || layoutGroup is not HorizontalLayoutGroup)
                    {
                        DestroyPreviousGroup();

                        layoutGroup = hlg = new GameObject("layout").AddComponent<HorizontalLayoutGroup>();

                        hlg.childAlignment = TextAnchor.MiddleLeft;
                        hlg.childControlWidth = false;
                        hlg.childForceExpandWidth = false;
                        hlg.childScaleWidth = false;
                        hlg.childControlHeight = true;
                        hlg.childForceExpandHeight = true;
                        hlg.childScaleHeight = false;
                    }
                    break;
                case Ordering.RightToLeft:
                    if (layoutGroup == null || layoutGroup is not HorizontalLayoutGroup)
                    {
                        DestroyPreviousGroup();

                        layoutGroup = hlg = new GameObject("layout").AddComponent<HorizontalLayoutGroup>();

                        hlg.childAlignment = TextAnchor.MiddleRight;
                        hlg.childControlWidth = false;
                        hlg.childForceExpandWidth = false;
                        hlg.childScaleWidth = false;
                        hlg.childControlHeight = true;
                        hlg.childForceExpandHeight = true;
                        hlg.childScaleHeight = false;
                    }
                    break;
            }

            layoutGroup.padding = Padding;
            layoutGroup.spacing = Spacing;

            var layoutTransform = layoutGroup.GetComponent<RectTransform>();
            layoutTransform.SetParent(Container, false);
            layoutTransform.localRotation = Quaternion.identity;
            layoutTransform.localScale = new Vector3(1, 1, 1);
            layoutTransform.anchorMin = new Vector2(0, 0); 
            layoutTransform.anchorMax = new Vector2(1, 1); 
            layoutTransform.pivot = new Vector2(0.5f, 0.5f);
            layoutTransform.sizeDelta = Vector2.zero;
            layoutTransform.anchoredPosition = Vector2.zero;

            foreach(var child in toParent)
            {
                if (child == null) continue;
                child.SetParent(layoutTransform, false);
            }
            toParent.Clear();
        }
        public HorizontalOrVerticalLayoutGroup LayoutGroup
        {
            get
            {
                if (layoutGroup == null) RefreshLayout();
                return layoutGroup;
            }
        }

        public float scrollSpeedMultiplier = 1;

        [SerializeField]
        private int spacing;
        public void SetSpacing(int spacing)
        {
            this.spacing = spacing;
            Refresh();
        }
        public int Spacing
        {
            get => spacing;
            set => SetSpacing(value);
        }
        [SerializeField]
        private int paddingLeft;
        public void SetPaddingLeft(int paddingLeft) => SetPadding(paddingLeft, paddingRight, paddingTop, paddingBottom);   
        public int PaddingLeft
        {
            get => paddingLeft;
            set => SetPaddingLeft(value);
        }
        [SerializeField]
        private int paddingRight;
        public void SetPaddingRight(int paddingRight) => SetPadding(paddingLeft, paddingRight, paddingTop, paddingBottom);
        public int PaddingRight
        {
            get => PaddingRight;
            set => SetPaddingRight(value);
        }
        [SerializeField]
        private int paddingTop;
        public void SetPaddingTop(int paddingTop) => SetPadding(paddingLeft, paddingRight, paddingTop, paddingBottom);
        public int PaddingTop
        {
            get => paddingTop;
            set => SetPaddingTop(value);
        }
        [SerializeField]
        private int paddingBottom;
        public void SetPaddingBottom(int paddingBottom) => SetPadding(paddingLeft, paddingRight, paddingTop, paddingBottom);
        public int PaddingBottom
        {
            get => paddingBottom;
            set => SetPaddingBottom(value);
        }
        private RectOffset padding;
        public void SetPadding(RectOffset padding)
        {
            if (padding == null)
            {
                SetPadding(0, 0, 0, 0);
                return;
            }

            SetPadding(padding.left, padding.right, padding.top, padding.bottom);
        }
        public void SetPadding(int left, int right, int top, int bottom)
        {
            if (padding == null) padding = new RectOffset();

            padding.left = paddingLeft = left;
            padding.right = paddingRight = right;
            padding.top = paddingTop = top;
            padding.bottom = paddingBottom = bottom;

            Refresh();
        }
        public RectOffset Padding
        {
            get
            {
                if (padding == null) padding = new RectOffset();
                
                padding.left = paddingLeft;
                padding.right = paddingRight;
                padding.top = paddingTop;
                padding.bottom = paddingBottom;

                return padding;
            }
            set => SetPadding(value);
        }

        [SerializeField]
        private GameObject listMemberPrototype;
        public void SetListMemberPrototype(GameObject prototype)
        {
            for(int a = 0; a < listMemberInstances.Count; a++)
            {
                var inst = listMemberInstances[a];
                if (inst == null || inst.gameObject == null) continue;
                Destroy(inst.gameObject);
            }
            listMemberInstances.Clear();

            listMemberPrototype = prototype;

            if (listMemberPrototype != null) listMemberPrototype.SetActive(false);

            Refresh();
        }
        public float MemberSize
        {
            get
            {
                if (listMemberPrototype == null) return 0;
                var rT = listMemberPrototype.GetComponent<RectTransform>();
                switch(ordering)
                {
                    case Ordering.BottomToTop:
                    case Ordering.TopToBottom:
                        return rT.rect.height;

                    case Ordering.LeftToRight:
                    case Ordering.RightToLeft:
                        return rT.rect.width;
                }
                return 0;
            }
        }
        public GameObject ListMemberPrototype
        {
            get => listMemberPrototype;
            set => SetListMemberPrototype(value);
        }
        private ListMemberInstance GetNewMemberInstance()
        {
            if (listMemberPrototype == null) return null;
            var inst = Instantiate(listMemberPrototype);
            inst.SetActive(false);
            inst.transform.SetParent(transform, false);
            var lmi = new ListMemberInstance() { gameObject = inst, children = inst.GetComponentsInChildren<UIRecyclingList>(true) };
            if (lmi.children != null && lmi.children.Length == 0) lmi.children = null;
            listMemberInstances.Add(lmi);
            return lmi;
        }

        [SerializeField]
        private Scrollbar scrollbar;
        public void SetScrollbar(Scrollbar scrollbar)
        {
            if (this.scrollbar != null && this.scrollbar.onValueChanged != null) this.scrollbar.onValueChanged.RemoveListener(SetViewPosition);
            
            this.scrollbar = scrollbar;
            if (scrollbar != null)
            {
                if (scrollbar.onValueChanged == null) scrollbar.onValueChanged = new Scrollbar.ScrollEvent();
                scrollbar.onValueChanged.AddListener(SetViewPosition);
            }
        }
        public Scrollbar Scrollbar
        {
            get => scrollbar;
            set => SetScrollbar(value);
        }

        private float viewPosition;
        public void SetViewPosition(float viewPos)
        {
            viewPosition = viewPos;
            Refresh();
        }
        public float ViewPosition
        {
            get => viewPosition;
            set => SetViewPosition(value);
        }

        private RectTransform rectTransform;
        public RectTransform RectTransform
        {
            get
            {
                if (rectTransform == null) rectTransform = gameObject.GetComponent<RectTransform>();
                return rectTransform;
            }
        }

        [SerializeField]
        private RectTransform container;
        public void SetContainer(RectTransform container)
        {
            this.container = container;
            Refresh();
        }
        public RectTransform Container
        {
            get 
            {
                if (container == null) container = gameObject.GetComponent<RectTransform>();
                return container;
            }
            set => SetContainer(value);
        }

        public class ListMemberInstance
        {
            public GameObject gameObject;
            public UIRecyclingList[] children;
        }
        private readonly List<ListMemberInstance> listMemberInstances = new List<ListMemberInstance>();
        public int VisibleMemberInstanceCount => listMemberInstances.Count;
        public ListMemberInstance GetVisibleMemberInstance(int instanceIndex) => instanceIndex < 0 || instanceIndex >= listMemberInstances.Count ? null : listMemberInstances[instanceIndex];
        public int IndexOfVisibleMemberInstance(ListMemberInstance inst) => IndexOfVisibleMemberInstance(inst.gameObject);
        public int IndexOfVisibleMemberInstance(GameObject go)
        {
            for(int a = 0; a < listMemberInstances.Count; a++)
            {
                var inst = listMemberInstances[a];
                if (inst.gameObject == go) return a; 
            }

            return -1;
        }
        public int GetMemberIndexFromVisibleIndex(int visibleIndex)
        {
            int memberIndex = VisibleRangeStart + visibleIndex;
            return memberIndex < 0 || memberIndex >= listMembers.Count ? -1 : memberIndex;
        }

        public delegate void OnRefreshMember(MemberData memberData, GameObject instance);

        [Serializable]
        public struct MemberData
        {
            public string name;
            public UnityAction onClick;

            public MemberID id;

            public OnRefreshMember onRefresh;
            public object storage;

            public bool hidden;
        }

        private bool isDirty;
        public bool IsDirty => isDirty;
        public void MarkAsDirty() => isDirty=true;

        private readonly List<MemberData> listMembers = new List<MemberData>();
        public int Count => listMembers.Count;

        private int prevCountForNonHiddenCount;
        private int nonHiddenCount;
        public int NonHiddenCount
        {
            get
            {
                if (prevCountForNonHiddenCount != Count)
                {
                    prevCountForNonHiddenCount = Count;

                    nonHiddenCount = 0;
                    foreach (var mem in listMembers) if (!mem.hidden) nonHiddenCount++;
                }

                return nonHiddenCount;
            }
        }
        protected void RecalculateMemberIndices()
        {
            for(int a = 0; a < listMembers.Count; a++)
            {
                var mem = listMembers[a];
                if (mem.id == null) mem.id = new MemberID();
                mem.id.index = a;
            }
        }
        [Serializable]
        public class MemberID
        {
            public int index = -1;
        }
        public MemberID AddNewMember(string name, UnityAction onClick, bool refreshUI = false, OnRefreshMember onRefresh = null, object storage = null) => AddOrUpdateMember(new MemberData() { name = name, onClick = onClick, onRefresh = onRefresh, storage = storage }, refreshUI);
        public MemberID AddOrUpdateMember(MemberData data, bool refreshUI = true)
        {
            isDirty = true;

            if (data.id == null) 
            { 
                data.id = new MemberID();
                data.id.index = -1;
            }
            if (data.id.index < 0 || data.id.index >= listMembers.Count)
            {
                data.id.index = listMembers.Count;
                listMembers.Add(data);
            } 
            else
            {
                listMembers[data.id.index] = data; 
            }
            if (refreshUI) Refresh();

            return data.id;
        }
        public MemberID AddOrUpdateMemberWithStorageComparison(MemberData data, bool refreshUI = true)
        {
            isDirty = true;

            foreach(var mem in listMembers)
            {
                if (mem.storage == data.storage)
                {
                    data.id = mem.id;
                    break;
                }
            }

            return AddOrUpdateMember(data, refreshUI);
        }
        public MemberData AddOrGetMemberWithStorageComparison(MemberData data, bool refreshUI = true)
        {
            isDirty = true;

            foreach (var mem in listMembers)
            {
                if (mem.storage == data.storage)
                {
                    data = mem;
                    break;
                }
            }

            data.id = AddOrUpdateMember(data, refreshUI);
            return data;
        }
        public MemberData GetMember(MemberID id) => GetMember(id == null ? -1 : id.index);
        public MemberData GetMember(int index)
        {
            if (index < 0 || index >= listMembers.Count) return default;
            return listMembers[index]; 
        }
        public MemberData FindMember(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return default;
            foreach (var mem in listMembers) if (mem.name == name) return mem;  
            return default;
        }
        public bool TryGetMember(MemberID id, out MemberData mem) => TryGetMember(id == null ? -1 : id.index, out mem);
        public bool TryGetMember(int index, out MemberData mem)
        {
            mem = default;
            if (index < 0 || index >= listMembers.Count) return default;
            mem = listMembers[index];
            return true;
        }
        public bool TryGetMemberByStorageComparison(MemberData memIn, out MemberData mem) 
        {
            mem = memIn;
            foreach(var mem_ in listMembers) if (mem_.storage == memIn.storage)
                {
                    mem = mem_;
                    return true;
                }
            return false;
        }

        public void RemoveMember(MemberData data) => RemoveMember(data.id);
        public void RemoveMember(MemberID id) 
        {
            if (id == null) return;
            RemoveMember(id.index);
            id.index = -1;
        }
        public void RemoveMember(int index)
        {
            if (index < 0 || index >= listMembers.Count) return;
            isDirty = true;
            var mem = listMembers[index];
            if (mem.id != null) mem.id.index = -1;
            listMembers.RemoveAt(index);
            RecalculateMemberIndices();
        }

        public void Sort(Comparison<MemberData> comparer)
        {
            isDirty = true;
            listMembers.Sort(comparer);
        }

        public void Clear()
        {
            for (int a =  0; a < listMembers.Count; a++)
            {
                var mem = listMembers[a];
                if (mem.id == null) continue;
                mem.id.index = -1;
            }
            isDirty = true;
            listMembers.Clear();
        }

        private void RefreshAnchoring()
        {
            /*var delta = RectTransform.sizeDelta;
            switch (ordering)
            {
                case Ordering.TopToBottom:
                    RectTransform.anchorMin = new Vector2(0, 1);
                    rectTransform.anchorMax = new Vector2(1, 1);
                    rectTransform.pivot = new Vector2(0.5f, 1);
                    delta.x = 0;
                    break;
                case Ordering.BottomToTop:
                    RectTransform.anchorMin = new Vector2(0, 0);
                    rectTransform.anchorMax = new Vector2(1, 0);
                    rectTransform.pivot = new Vector2(0.5f, 0);
                    delta.x = 0;
                    break;
                case Ordering.LeftToRight:
                    RectTransform.anchorMin = new Vector2(0, 0);
                    rectTransform.anchorMax = new Vector2(0, 1);
                    rectTransform.pivot = new Vector2(0, 0.5f);
                    delta.y = 0;
                    break;
                case Ordering.RightToLeft:
                    RectTransform.anchorMin = new Vector2(1, 0);
                    rectTransform.anchorMax = new Vector2(1, 1);
                    rectTransform.pivot = new Vector2(1, 0.5f);
                    delta.y = 0;
                    break;
            }

            rectTransform.sizeDelta = delta; 
            rectTransform.anchoredPosition = Vector2.zero;*/
        }
        protected override void Awake()
        {
            RefreshAnchoring();

            if (scrollbar != null)
            {
                var temp = scrollbar; 
                scrollbar = null;
                SetScrollbar(temp);
            }

            if (listMemberPrototype != null) listMemberPrototype.SetActive(false);

        }

        public delegate void RefreshDelegate();
        public event RefreshDelegate OnRefresh;
        public event RefreshDelegate AfterRefresh;
        public void ClearAllListeners()
        {
            OnRefresh = null;
            AfterRefresh = null;
        }
        private readonly Vector3[] fourCornersArray = new Vector3[4];
        private int startIndex = -2, endIndex = -2;
        public int VisibleRangeStart => startIndex;
        public int VisibleRangeEnd => endIndex;
        public int GetReorderedIndex(int memberIndex)
        {
            switch (ordering)
            {
                case Ordering.RightToLeft:
                case Ordering.BottomToTop:
                    memberIndex = listMembers.Count - 1 - memberIndex;
                    break;
            }

            return memberIndex;
        }
        public void Refresh()
        {
            if (listMemberPrototype == null) return;
            if (container == null)
            {
                if (RectTransform.parent == null)
                {
                    container = rectTransform;
                }
                else
                {
                    container = rectTransform.parent.GetComponent<RectTransform>();
                    if (container == null) container = rectTransform;
                }
                if (container == null) return;
            }

            OnRefresh?.Invoke();
            
            RefreshAnchoring();
            RefreshLayout();

            if (layoutGroup == null) return;

            var layoutTransform = layoutGroup.GetComponent<RectTransform>();

            bool newImg = false;
            Image containerImage = container.gameObject.GetComponent<Image>();
            if (containerImage == null)
            {
                containerImage = container.gameObject.AddComponent<Image>();
                newImg = true;
            }
            Mask containerMask = container.gameObject.GetComponent<Mask>();
            if (containerMask == null)
            {
                containerMask = container.gameObject.AddComponent<Mask>();
                if (newImg) containerMask.showMaskGraphic = false;
            }

            container.GetLocalCorners(fourCornersArray);

            var min = new Vector2(Mathf.Min(fourCornersArray[0].x, fourCornersArray[1].x, fourCornersArray[2].x, fourCornersArray[3].x), Mathf.Min(fourCornersArray[0].y, fourCornersArray[1].y, fourCornersArray[2].y, fourCornersArray[3].y));
            var max = new Vector2(Mathf.Max(fourCornersArray[0].x, fourCornersArray[1].x, fourCornersArray[2].x, fourCornersArray[3].x), Mathf.Max(fourCornersArray[0].y, fourCornersArray[1].y, fourCornersArray[2].y, fourCornersArray[3].y));

            float width = max.x - min.x;
            float height = max.y - min.y;

            float containerSize = 0;  
            float paddingSize = 0;
            switch (ordering)
            {
                case Ordering.BottomToTop:
                case Ordering.TopToBottom:
                    containerSize = height;
                    paddingSize = paddingTop + paddingBottom;
                    break;

                case Ordering.LeftToRight:
                case Ordering.RightToLeft:
                    containerSize = width;
                    paddingSize = paddingLeft + paddingRight;
                    break;
            }

            float memberSize = MemberSize;
            float memberSizePlusSpacing = memberSize + Spacing;

            int visibleMemberCount = Mathf.FloorToInt((containerSize + Spacing) / memberSizePlusSpacing);
            float visibleMargin = containerSize - ((visibleMemberCount * memberSizePlusSpacing) - Spacing);

            float fullListSize = ((NonHiddenCount/*listMembers.Count*/ * memberSizePlusSpacing) - Spacing) - paddingSize;

            bool canScroll = fullListSize >= containerSize;
            if (scrollbar != null) scrollbar.gameObject.SetActive(canScroll);

            int maxEndIndex = listMembers.Count - 1;
            while (maxEndIndex > 0 && listMembers[GetReorderedIndex(maxEndIndex)].hidden)
            {
                maxEndIndex--;
            }
            int maxIndex = maxEndIndex + 1;// Mathf.Max(0, listMembers.Count - visibleMemberCount);
            int i = 0;
            while (maxIndex > 0 && i < visibleMemberCount)
            {
                maxIndex--;
                if (!listMembers[GetReorderedIndex(maxIndex)].hidden) i++;
            }
            /*float ClampMemberIndex(float memberIndex)
            {
                return Mathf.Clamp(memberIndex, 0, maxIndex);
            }*/
            int maxNonHiddenIndex = Mathf.Max(0, NonHiddenCount - i);
            float flexibleWindowPos = (maxNonHiddenIndex * memberSizePlusSpacing) - visibleMargin;//(maxIndex * memberSizePlusSpacing) - visibleMargin; 
            float GetFlexibleWindowPosition(float viewPos)
            {
                return viewPos * flexibleWindowPos;
            }
            maxNonHiddenIndex = maxNonHiddenIndex - 1;
            
            float windowPosition = GetFlexibleWindowPosition(canScroll ? ViewPosition : 0);
            //startIndex = (int)ClampMemberIndex(Mathf.FloorToInt(windowPosition / memberSizePlusSpacing));

            int j = Mathf.Min(maxNonHiddenIndex, Mathf.FloorToInt(windowPosition / memberSizePlusSpacing));
            int k = 0;
            startIndex = 0;
            while (startIndex < maxIndex && listMembers[GetReorderedIndex(startIndex)].hidden) startIndex++;  
            while(startIndex < maxIndex && k < j)
            {
                startIndex++;
                if (!listMembers[GetReorderedIndex(startIndex)].hidden) k++; 
            }

            float startWindowPosition = k * memberSizePlusSpacing;

            
            endIndex = startIndex + visibleMemberCount + 1;  
            for(int index = startIndex + 1; index <= endIndex; index++)
            {
                if (index >= listMembers.Count) break;

                var member = listMembers[GetReorderedIndex(index)];
                if (member.hidden) 
                { 
                    endIndex++;
                }

                if (endIndex >= listMembers.Count) break;  
            }

            /*endIndex = startIndex + 1;
            int l = 0;
            while(endIndex < listMembers.Count && l < visibleMemberCount)
            {
                var member = listMembers[GetReorderedIndex(endIndex)];
                if (!member.hidden) l++;
                endIndex++; 
            }*/

            endIndex = Mathf.Min(listMembers.Count - 1, endIndex);
            //Debug.Log($"{startIndex} {endIndex} :::: {j} {k}");
            foreach (var mem in listMemberInstances)
            {
                if (mem.gameObject != null) mem.gameObject.SetActive(false);
            }

            for (int index = startIndex; index <= endIndex; index++)
            {
                int dataIndex = GetReorderedIndex(index);
                if (dataIndex < 0 || dataIndex >= listMembers.Count)
                {
                    continue;
                }
                var data = listMembers[dataIndex];

                int instanceIndex = index - startIndex;
                ListMemberInstance instance = null;
                while (instance == null || instance.gameObject == null)
                {
                    while (listMemberInstances.Count <= instanceIndex) GetNewMemberInstance();  
                    instance = listMemberInstances[instanceIndex];
                    if (instance == null || instance.gameObject == null) 
                    {
                        if (listMemberPrototype == null) break;
                        listMemberInstances.RemoveAt(instanceIndex);
                    }
                }
                if (instance == null || instance.gameObject == null) break;

                instance.gameObject.SetActive(true);
                var instanceTransform = instance.gameObject.GetComponent<RectTransform>(); 

                var nameTransform = instanceTransform.FindDeepChildLiberal("name");
                if (nameTransform == null) nameTransform = instanceTransform;

                CustomEditorUtils.SetInputOrTextComponentText(nameTransform, data.name);
                if (!membersAreNotButtons)
                {
                    if (data.onClick == null && setDisableMemberButtonsWithNoOnClick) CustomEditorUtils.SetButtonInteractable(instanceTransform, false); else CustomEditorUtils.SetButtonOnClickAction(instanceTransform, data.onClick);
                }

                instanceTransform.SetParent(layoutTransform, false); 
               
                switch (ordering)
                {
                    case Ordering.BottomToTop:
                    case Ordering.TopToBottom:
                        instanceTransform.SetAsLastSibling();
                        instanceTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, memberSize);
                        break;

                    case Ordering.LeftToRight:
                    case Ordering.RightToLeft:
                        instanceTransform.SetAsLastSibling();
                        instanceTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, memberSize); 
                        break;
                }

                if (data.hidden)
                {
                    instance.gameObject.SetActive(false);
                }
                else
                {
                    data.onRefresh?.Invoke(data, instance.gameObject);
                    if (autoRefreshChildLists && instance.children != null) foreach (var child in instance.children) child.Refresh();
                }
            }

            switch (ordering)
            {
                case Ordering.BottomToTop:
                case Ordering.TopToBottom:
                    layoutTransform.anchoredPosition = new Vector2(layoutTransform.anchoredPosition.x, windowPosition - startWindowPosition);  
                    break;

                case Ordering.LeftToRight:
                case Ordering.RightToLeft:
                    layoutTransform.anchoredPosition = new Vector2(startWindowPosition - windowPosition, layoutTransform.anchoredPosition.y);
                    break;
            }

            AfterRefresh?.Invoke();
            isDirty = false;
        }

        public bool MemberIsVisible(MemberData data) => MemberIsVisible(data.id);
        public bool MemberIsVisible(MemberID id) => MemberIsVisible(id.index);
        public bool MemberIsVisible(int memberIndex) => memberIndex >= startIndex && memberIndex <= endIndex;// && !listMembers[memberIndex].hidden;

        public bool TryGetVisibleMemberInstance(MemberData data, out GameObject instance) => TryGetVisibleMemberInstance(data.id, out instance);
        public bool TryGetVisibleMemberInstance(MemberID id, out GameObject instance) => TryGetVisibleMemberInstance(id == null ? -1 : id.index, out instance);
        public bool TryGetVisibleMemberInstance(int memberIndex, out GameObject instance)
        {
            instance = null;
            if (!MemberIsVisible(memberIndex)) return false; 

            int instanceIndex = memberIndex - startIndex;

            if (instanceIndex >= 0 && instanceIndex < listMemberInstances.Count)
            {
                var inst = listMemberInstances[instanceIndex];
                if (inst == null) return false; 
                instance = inst.gameObject;
            }

            return instance != null;
        }

        private Vector2 prevContainerSize;
        protected void OnGUI()
        {
            if (container == null) return;
            Vector2 containerSize = container.rect.size;
            if (containerSize != prevContainerSize)
            {
                prevContainerSize = containerSize;
                Refresh();
            }
        }

        protected void LateUpdate()
        {
            if (scrollbar != null && scrollbar.gameObject.activeInHierarchy)
            {
                if (isInFocus) scrollbar.value = Mathf.Clamp01(scrollbar.value + (InputProxy.Scroll * scrollSpeedMultiplier * InputProxy.ScrollSpeed * (scrollbar.direction == Scrollbar.Direction.TopToBottom || scrollbar.direction == Scrollbar.Direction.RightToLeft ? -1 : 1)) / (1 + (NonHiddenCount * 0.1f)));
                scrollbar.size = 1f / Mathf.Min(25, NonHiddenCount + 1); 
            }
        }

        protected bool isInFocus;
        public void OnPointerEnter(PointerEventData eventData)
        {
            isInFocus = true;
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            isInFocus = false; 
        }

        public void ClearFilters(bool refresh)
        {
            for (int a = 0; a < listMembers.Count; a++)
            {
                var mem = listMembers[a];
                mem.hidden = false;
                listMembers[a] = mem;
            }

            prevCountForNonHiddenCount = -1;
            if (refresh) Refresh();
        }

        public void FilterMembersByStartString(string str, bool caseSensitive = false, bool refresh = true)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                ClearFilters(refresh);
            }
            else
            {
                string originalStr = str;
                if (!caseSensitive) str = str.ToLower();

                for (int a = 0; a < listMembers.Count; a++)
                {
                    var mem = listMembers[a];
                    string memName = mem.name;
                    if (!caseSensitive && memName != null) memName = memName.ToLower();

                    if (AssetFiltering.ContainsStartString(memName, str) || AssetFiltering.ContainsCapitalizedWord(mem.name, originalStr))
                    {
                        mem.hidden = false;  
                    }
                    else
                    {
                        mem.hidden = true;
                    }

                    listMembers[a] = mem; 
                }

                prevCountForNonHiddenCount = -1;
                if (refresh) Refresh();
            }
        }

    }
}

#endif