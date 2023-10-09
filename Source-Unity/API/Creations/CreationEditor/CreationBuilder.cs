#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

using TMPro;

using Swole.UI;

namespace Swole.API.Unity
{

    public class CreationBuilder : MonoBehaviour
    {

        public delegate void SetTextDelegate(string text);

        protected class Member
        {

            public bool showChildren = true;

            public Member parent;
            public int id;
            public string name;
            public UIPopup hierarchyObject;
            public RectTransform rectTransform;

            public SetTextDelegate setDisplayName;
            public RectTransform parentMask;

            public GameObject rootObject;

            public bool IsInParentMask(Vector3 pos)
            {
                return parentMask == null && rectTransform.Contains(pos) || parentMask != null && parentMask.Contains(pos);
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

        }

        protected List<Member> members = new List<Member>();

        protected int GetIdAtIndex(int index)
        {
            if (index < 0 || index >= members.Count) return -1;
            return members[index].id;
        }

        protected Member GetMember(int id)
        {
            for (int a = 0; a < members.Count; a++) if (members[a].id == id) return members[a];
            return null;
        }

        protected Member GetMember(GameObject rootObject)
        {
            for (int a = 0; a < members.Count; a++) if (members[a].rootObject == rootObject) return members[a];
            return null;
        }

        protected Member GetMember(RectTransform rectTransform)
        {
            for (int a = 0; a < members.Count; a++) if (members[a].rectTransform == rectTransform) return members[a];
            return null;
        }

        private const string objName_Identifier = "Name";
        private const string objParentMask_Identifier = "ParentMask";

        public VerticalLayoutGroup layoutGroup;
        private RectTransform layoutGroupTransform;

        /// <summary>
        /// A prefab for a UI object that represents a scene object in the creation hierarchy. Will look for a text object called "Name" to display the name of the member. Also looks for a RectTransform called "ParentMask" to act as a region for drag and drop parenting.
        /// </summary>
        public UIPopup hierarchyMemberPrototype;
        /// <summary>
        /// A prefab for a UI object that is used as a placeholder object in the ui hierarchy when a member is being dragged around the screen.
        /// </summary>
        public Image hierarchyPlaceholderPrototype;
        /// <summary>
        /// A prefab for a UI graphic that displays where a hovering member will be slotted into the hierarchy.
        /// </summary>
        public Image hierarchySlotInPrototype;
        /// <summary>
        /// A prefab for a UI graphic that appears when a dragged member will be parented to a member below it.
        /// </summary>
        public Image hierarchyParentToPrototype;

        private Canvas canvas;
        private RectTransform canvasTransform;

        private RectTransform placeholderTransform;
        private Image placeholderGraphic;

        private RectTransform slotInTransform;
        private Image slotInGraphic;

        private RectTransform parentToTransform;
        private Image parentToGraphic;

        protected virtual void Awake()
        {

            if (layoutGroup == null)
            {
                Debug.LogError($"Layout Group not set for Creation Builder '{name}'");
                Destroy(this);
                return;
            }

            if (hierarchyMemberPrototype == null)
            {
                Debug.LogError($"Hierarchy Member Prototype not set for Creation Builder '{name}'");
                Destroy(this);
                return;
            }

            if (hierarchyPlaceholderPrototype == null)
            {
                Debug.LogError($"Hierarchy Placeholder Prototype not set for Creation Builder '{name}'");
                Destroy(this);
                return;
            }

            if (hierarchySlotInPrototype == null)
            {
                Debug.LogError($"Hierarchy Slot In Prototype not set for Creation Builder '{name}'");
                Destroy(this);
                return;
            }

            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;

            layoutGroup.childScaleWidth = false;
            layoutGroup.childScaleHeight = true;

            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;

            layoutGroupTransform = layoutGroup.GetComponent<RectTransform>();

            canvas = layoutGroup.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.GetComponentInParent<Canvas>();
                layoutGroup.transform.SetParent(canvas.transform, true);
            }
            canvasTransform = canvas.GetComponent<RectTransform>();

            placeholderGraphic = Instantiate(hierarchyPlaceholderPrototype);
            placeholderGraphic.gameObject.SetActive(true);
            placeholderGraphic.enabled = false;
            placeholderGraphic.raycastTarget = false;
            placeholderTransform = placeholderGraphic.rectTransform;
            placeholderTransform.SetParent(canvas.transform);

            slotInGraphic = Instantiate(hierarchySlotInPrototype);
            slotInGraphic.gameObject.SetActive(true);
            slotInGraphic.enabled = false;
            slotInGraphic.raycastTarget = false;
            slotInTransform = slotInGraphic.rectTransform;
            slotInTransform.SetParent(canvas.transform);

            if (hierarchyParentToPrototype != null)
            {
                parentToGraphic = Instantiate(hierarchyParentToPrototype);
                parentToGraphic.gameObject.SetActive(true);
                parentToGraphic.enabled = false;
                parentToGraphic.raycastTarget = false;
                parentToTransform = parentToGraphic.rectTransform;
                parentToTransform.SetParent(canvas.transform);
            }

        }

        protected virtual UIPopup CreateNewUIMember(Member member)
        {

            member.hierarchyObject = Instantiate(hierarchyMemberPrototype);
            member.hierarchyObject.name = member.name;

            var rootTransform = member.hierarchyObject.GetComponent<RectTransform>();
            member.rectTransform = rootTransform;
            member.rectTransform.SetParent(layoutGroupTransform, false);

            var nameTransform = rootTransform.FindDeepChildLiberal(objName_Identifier);
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

            var parentMaskLookup = rootTransform.FindDeepChildLiberal(objParentMask_Identifier);
            if (parentMaskLookup != null) member.parentMask = (RectTransform)parentMaskLookup;

            member.hierarchyObject.OnDragStart.AddListener(new UnityAction(() =>
            {

                draggedMember = member;
                ActivatePlaceholder(member);
                draggedMember.rectTransform.SetParent(canvasTransform, true);

            }));

            member.hierarchyObject.OnDragStop.AddListener(new UnityAction(() =>
            {

                DeactivatePlaceholder(member);
                if (draggedMember == member) draggedMember = null;
                ReevaluateMember(member);

            }));

            member.hierarchyObject.gameObject.SetActive(true);
            return member.hierarchyObject;

        }

        protected virtual void ReevaluateMember(int id) => ReevaluateMember(GetMember(id));

        protected virtual void ReevaluateMember(Member targetMember)
        {
            if (targetMember == null) return;

            Vector2 cursorPos = CursorProxy.Position;
            bool setParent = false;
            for (int a = 0; a < members.Count; a++)
            {
                var member = members[a];
                if (member.id == targetMember.id) continue;
                if (member.IsInParentMask(cursorPos))
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
            layoutGroup.CalculateLayoutInputVertical();
            layoutGroup.SetLayoutVertical();
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroupTransform);
        }

        public virtual void SetMemberParent(int childId, int parentId) => SetMemberParent(GetMember(childId), GetMember(parentId));

        protected virtual void SetMemberParent(Member child, Member parent)
        {

            if (child == null) return;

            if (child.parent != null) child.parent.RemoveChild(child);
            if (parent == null)
            {
                child.parent = null;
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

            RefreshLayout();

            for (int a = 0; a < layoutGroupTransform.childCount; a++)
            {
                var childTransform = (RectTransform)layoutGroupTransform.GetChild(a);
                if (!childTransform.gameObject.activeInHierarchy) continue;
                var childMember = GetMember(childTransform);
                if (childMember == null) continue;
                if (parent != null && childMember.IsChildOf(parent)) continue;
                childTransform.GetWorldCorners(corners);
                Vector3 center = (corners[0] + corners[1] + corners[2] + corners[3]) / 4f;
                if (!layoutGroup.reverseArrangement && screenPosition.y >= center.y || layoutGroup.reverseArrangement && screenPosition.y <= center.y)
                {
                    toReplace = childMember;
                    return Mathf.Max(0, a - 1);
                }
                previousMember = childMember;
            }

            return layoutGroupTransform.childCount;

        }
        protected int UpdateSlotInIndex(Vector3 screenPosition, Member parent = null)
        {

            int slotInIndex = GetSiblingIndexFromScreenPosition(screenPosition, out Member toReplace, out Member previousMember, parent);
            if (parentToGraphic != null && toReplace != null && toReplace.IsInParentMask(screenPosition))
            {
                ActivateParentToHighlight(toReplace); // Show parent-to highlight graphic instead
                HideSlotIn();
            }
            else if (parentToGraphic != null && previousMember != null && previousMember.IsInParentMask(screenPosition))
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
            placeholderTransform.SetParent(layoutGroupTransform, false);
            placeholderTransform.SetSiblingIndex(toSub.GetSiblingIndexStart());
        }

        protected void DeactivatePlaceholder(Member toPutBack)
        {
            placeholderGraphic.raycastTarget = false;
            placeholderGraphic.enabled = false;
            toPutBack.rectTransform.SetParent(layoutGroupTransform);
            toPutBack.rectTransform.SetSiblingIndex(placeholderTransform.GetSiblingIndex());
            placeholderTransform.SetParent(canvasTransform, false);
        }

        protected void ActivateParentToHighlight(Member toHighlight)
        {
            if (parentToGraphic == null || parentToTransform == null) return;
            parentToGraphic.raycastTarget = false;
            parentToGraphic.enabled = true;
            parentToTransform.SetParent(toHighlight.rectTransform, false);
            parentToTransform.SetAsLastSibling();
            parentToTransform.SetAnchor(AnchorPresets.StretchAll);
            parentToTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, toHighlight.rectTransform.rect.width);
            parentToTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, toHighlight.rectTransform.rect.height);
        }

        protected void DeactivateParentToHighlight()
        {
            if (parentToGraphic == null || parentToTransform == null) return;
            parentToGraphic.raycastTarget = false;
            parentToGraphic.enabled = false;
            parentToTransform.SetParent(canvasTransform, false);
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
            slotInTransform.SetParent(layoutGroupTransform, false);
        }
        protected void DeactivateSlotIn()
        {
            HideSlotIn();
            slotInTransform.SetParent(canvasTransform);
        }

        protected void OnGUI()
        {
            if (draggedMember != null)
            {
                if (prevDraggedMember != draggedMember || slotInTransform.parent != layoutGroupTransform) ActivateSlotIn();
                prevDraggedMember = draggedMember;
                int slotInIndex = UpdateSlotInIndex(CursorProxy.Position, draggedMember);
                slotInTransform.SetSiblingIndex(slotInIndex); // Show slot-in graphic where the dragged element would be placed if dropped
            }
            else if (prevDraggedMember != null)
            {
                if (prevDraggedMember.parent == null)
                {
                    prevDraggedMember.SetSiblingIndex(slotInTransform.GetSiblingIndex()); // Place dropped element where the slot-in graphic was
                    prevDraggedMember.Refresh();
                }
                prevDraggedMember = null;
                DeactivateSlotIn();
                DeactivateParentToHighlight();
            }
        }

        public bool test;

        public void Update()
        {

            if (test)
            {
                test = false;
                var mem = new Member() { id = SwoleUtil.GetUniqueId(GetIdAtIndex, members.Count) };
                mem.name = $"test_{mem.id}";
                members.Add(mem);
                CreateNewUIMember(mem);
            }

        }

    }

}

#endif
