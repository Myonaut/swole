#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using UnityEngine.Events;

using Unity.Mathematics;

namespace Swole.UI
{

    public class UIInfiniteSlidingList : UITweenable
    {

        public bool vertical;

        public bool positiveToNegative;

        public float memberSize = 100;

        public float memberInactiveScale = 1;

        public float memberActiveScale = 1.2f;

        public AnimationCurve scalingCurve = AnimationCurve.Linear(0, 0, 1, 1);

        public float scalingDistance = 50;

        public PivotPresets memberAlignment = PivotPresets.MiddleCenter;

        public AnchorPresets memberAnchor = AnchorPresets.MiddleCenter;

        public RectOffset padding;

        public float defaultTweenTime = 0.4f;

        public bool easeIn = false;

        public bool easeOut = true;

        public RectTransform[] inputMembers = new RectTransform[0];

        protected List<RectTransform> members = new List<RectTransform>();

        public void AddMember(RectTransform member)
        {

            if (members.Contains(member)) return;

            members.Add(member);

            rebuild = true;

        }

        public void InsertMember(int index, RectTransform member)
        {

            if (members.Contains(member)) return;

            members.Insert(index, member);

            if (selectedIndex >= index) selectedIndex++;

            rebuild = true;

        }

        public bool RemoveMember(RectTransform member)
        {

            int i = -1;

            for (int a = 0; a < members.Count; a++)
            {

                if (members[a] == member)
                {

                    i = a;

                    break;

                }

            }

            if (i < 0) return false;

            if (selectedIndex > i) selectedIndex--;

            rebuild = true;

            return true;

        }

        public RectTransform SelectedMember
        {

            get
            {

                if (members == null || members.Count <= 0) return null;

                return members[selectedIndex];

            }

        }

        protected RectTransform rectTransform;

        public RectTransform RectTransform
        {

            get
            {

                if (rectTransform == null) rectTransform = gameObject.AddOrGetComponent<RectTransform>();

                return rectTransform;

            }

        }

        protected Image panel;

        protected RectTransform panelRectTransform;

        protected RectTransform content;

        protected Canvas canvas;

        public Canvas Canvas => canvas;

        protected Vector3[] corners = new Vector3[4];

        protected bool initialized;

        public int panelSiblingIndex = 0;

        public void Initialize()
        {

            if (members == null) members = new List<RectTransform>();

            if (inputMembers != null)
            {

                members.AddRange(inputMembers);

                inputMembers = null;

            }

            RectTransform.GetWorldCorners(corners);

            if (canvas == null) canvas = rectTransform.GetComponentInParent<Canvas>();

            Transform canvasTransform = canvas.transform;

            corners[0] = canvasTransform.InverseTransformPoint(corners[0]);
            corners[1] = canvasTransform.InverseTransformPoint(corners[1]);
            corners[2] = canvasTransform.InverseTransformPoint(corners[2]);
            corners[3] = canvasTransform.InverseTransformPoint(corners[3]);

            float mainWidth = Mathf.Abs(corners[3].x - corners[0].x);
            float mainHeight = Mathf.Abs(corners[1].y - corners[0].y);

            if (panel == null)
            {

                panel = new GameObject("panel").AddOrGetComponent<Image>();

                Mask mask = panel.gameObject.AddComponent<Mask>();

                mask.showMaskGraphic = false;

                panelRectTransform = panel.gameObject.GetComponent<RectTransform>();

                panelRectTransform.SetParent(RectTransform, false);

                panelRectTransform.SetSiblingIndex(panelSiblingIndex);

                panelRectTransform.SetAnchor(AnchorPresets.StretchAll, false);

                panelRectTransform.SetPivot(PivotPresets.MiddleCenter);

                float paddingHor = 0;
                float paddingVer = 0;

                if (padding != null)
                {

                    paddingHor = padding.left + padding.right;
                    paddingVer = padding.top + padding.bottom;

                }

                panelRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, mainWidth - paddingHor);
                panelRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, mainHeight - paddingVer);

                panelRectTransform.position = canvasTransform.TransformPoint((corners[0] + corners[1] + corners[2] + corners[3]) / 4f);

                content = new GameObject("content").AddOrGetComponent<RectTransform>();

                content.SetParent(panelRectTransform, false);

                content.SetAnchor(AnchorPresets.MiddleCenter, false);

                content.SetPivot(PivotPresets.MiddleCenter);

                content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, mainWidth);
                content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, mainHeight);

                content.position = panelRectTransform.position;

            }

            initialized = true;

        }

        protected List<GameObject> duplicatedMembers = new List<GameObject>();

        protected RectTransform[] visibleMembers;

        protected RectTransform[] visibleMembersProxy;

        public RectTransform GetMemberAtIndex(int index)
        {

            if (members == null || members.Count <= 0 || index < 0 || index >= members.Count) return null;

            return members[index];

        }

        public List<RectTransform> GetAllMembersAtIndex(int index, List<RectTransform> list = null)
        {

            if (list == null) list = new List<RectTransform>();

            if (members == null || members.Count <= 0) return list;

            if (index >= 0 && index < members.Count) list.Add(members[index]);

            foreach (GameObject obj in duplicatedMembers) if (obj.name == $"{index}") list.Add(obj.GetComponent<RectTransform>());

            return list;

        }

        public void RebuildMembers()
        {

            if (!initialized) Initialize();

#if SWOLE_ENV
            LeanTween.cancel(gameObject);
#endif

            currentTween = null;

            if (duplicatedMembers == null) duplicatedMembers = new List<GameObject>();

            foreach (GameObject obj in duplicatedMembers) if (obj != null) GameObject.DestroyImmediate(obj);

            duplicatedMembers.Clear();


            bool repeat = true;

            while (repeat)
            {

                repeat = false;

                for (int a = 0; a < members.Count; a++)
                {

                    RectTransform member = members[a];

                    if (member == null)
                    {

                        members.RemoveAt(a);

                        repeat = true;

                        break;

                    }

                }

            }

            if (members.Count <= 0)
            {

                visibleMembers = new RectTransform[0];

                return;

            }

            RectTransform.GetWorldCorners(corners);

            Transform canvasTransform = canvas.transform;

            corners[0] = canvasTransform.InverseTransformPoint(corners[0]);
            corners[1] = canvasTransform.InverseTransformPoint(corners[1]);
            corners[2] = canvasTransform.InverseTransformPoint(corners[2]);
            corners[3] = canvasTransform.InverseTransformPoint(corners[3]);

            Vector3 center = (corners[0] + corners[1] + corners[2] + corners[3]) / 4f;

            float mainWidth = Mathf.Abs(corners[3].x - corners[0].x);
            float mainHeight = Mathf.Abs(corners[1].y - corners[0].y);

            //int visibleCount = (vertical ? Mathf.CeilToInt(mainHeight / memberSize) : Mathf.CeilToInt(mainWidth / memberSize)) + 2;

            //if (visibleCount % 2 == 0) visibleCount = visibleCount + 1;

            int visibleCount = (vertical ? Mathf.CeilToInt(mainHeight / memberSize) : Mathf.CeilToInt(mainWidth / memberSize));

            int memberCount = members.Count;

            if (visibleCount < memberCount + 2) visibleCount = visibleCount + 2;

            while (visibleCount % memberCount != 0) visibleCount++;

            if (visibleCount % 2 == 0) visibleCount = visibleCount + 1;

            while (visibleCount % memberCount != 0) visibleCount++;

            visibleMembers = new RectTransform[visibleCount];
            visibleMembersProxy = new RectTransform[visibleCount];

            float halfVisibleCount = visibleCount / 2f;

            int halfVisibleCountInt = Mathf.FloorToInt(halfVisibleCount);

            float visibleCountM1 = (visibleCount % 2 == 0) ? visibleCount : (visibleCount - 1f);

            int x = vertical ? 0 : 1;

            for (int a = 0; a < visibleCount; a++)
            {

                int visibleIndex = AsVisibleIndexFast(a);

                int memberIndex = a;

                RectTransform member;

                if (a >= memberCount)
                {

                    /*if (visibleIndex < halfVisibleCountInt) 
                    { 

                        memberIndex = memberIndex = ExternalUtil.wrap(selectedIndex + (visibleIndex - halfVisibleCountInt), memberCount);

                    } 
                    else
                    {

                        memberIndex = ExternalUtil.wrap(a, memberCount);

                    }*/

                    memberIndex = Maths.wrap(a, memberCount);

                    member = GameObject.Instantiate(members[memberIndex]).GetComponent<RectTransform>();

                    member.gameObject.name = $"{memberIndex}";

                    duplicatedMembers.Add(member.gameObject);

                }
                else
                {

                    member = members[memberIndex];

                }

                member.SetParent(content, false);

                member.SetAnchor(memberAnchor, false);
                member.SetPivot(memberAlignment);

                member.anchorMax = member.anchorMin;

                visibleMembers[visibleIndex] = member;

                float t = visibleIndex / visibleCountM1;

                Vector3 canvasSpacePosition = new Vector3(center.x + (((visibleCount * memberSize * t) - (halfVisibleCount * memberSize)) * (positiveToNegative ? -1 : 1)) * x, center.y + (((visibleCount * memberSize * t) - (halfVisibleCount * memberSize)) * (positiveToNegative ? -1 : 1)) * (1 - x), center.z);

                member.position = canvasTransform.TransformPoint(canvasSpacePosition);

                member.localScale = (float3)CalculateScaling(canvasSpacePosition, corners);

            }

            SelectedIndex = selectedIndex;

            rebuild = false;

        }

        public bool rebuild;

        protected void Awake()
        {

            RebuildMembers();

        }

        protected void Update()
        {

            if (rebuild)
            {

                RebuildMembers();

            }

        }

        protected int selectedIndex;

        public int SelectedIndex
        {

            get
            {

                return selectedIndex;

            }

            protected set
            {

                selectedIndex = value;

                OnSelectionChange?.Invoke();

            }

        }

        public void ForceSelection(int index)
        {

            selectedIndex = index;

            RebuildMembers();

            OnSelectionChange?.Invoke();

        }

        public int MemberCount => members.Count;

        protected int AsVisibleIndex(int memberIndex)
        {

            return Maths.wrap((Maths.wrap(memberIndex, members.Count) - selectedIndex) + Mathf.FloorToInt(visibleMembers.Length / 2f), visibleMembers.Length);

        }

        protected int AsVisibleIndexFast(int memberIndex)
        {

            return Maths.wrap((memberIndex - selectedIndex) + Mathf.FloorToInt(visibleMembers.Length / 2f), visibleMembers.Length);

        }

        protected float CalculateScaling(Vector3 positionCanvasSpace, Vector3[] rootCorners = null)
        {

            if (rootCorners == null)
            {

                rootCorners = corners;

                RectTransform.GetWorldCorners(rootCorners);

                Transform canvasTransform = canvas.transform;

                rootCorners[0] = canvasTransform.InverseTransformPoint(rootCorners[0]);
                rootCorners[1] = canvasTransform.InverseTransformPoint(rootCorners[1]);
                rootCorners[2] = canvasTransform.InverseTransformPoint(rootCorners[2]);
                rootCorners[3] = canvasTransform.InverseTransformPoint(rootCorners[3]);

            }

            Vector3 center = (rootCorners[0] + rootCorners[1] + rootCorners[2] + rootCorners[3]) / 4f;

            return Mathf.LerpUnclamped(memberInactiveScale, memberActiveScale, scalingCurve.Evaluate(1 - Mathf.Min((positionCanvasSpace - center).magnitude / scalingDistance, 1)));

        }

        public UnityEvent OnSelectionChange;

        public UnityEvent OnSlideBegin;

        public UnityEvent OnSlideEnd;

        public void SlideMembers(int amount, float time = 0, bool easeIn = false, bool easeOut = false)
        {

            StartNewTween();

            Transform canvasTransform = canvas.transform;

            RectTransform.GetWorldCorners(corners);

            corners[0] = canvasTransform.InverseTransformPoint(corners[0]);
            corners[1] = canvasTransform.InverseTransformPoint(corners[1]);
            corners[2] = canvasTransform.InverseTransformPoint(corners[2]);
            corners[3] = canvasTransform.InverseTransformPoint(corners[3]);

            Vector3 center = (corners[0] + corners[1] + corners[2] + corners[3]) / 4f;

            float mainWidth = Mathf.Abs(corners[3].x - corners[0].x);
            float mainHeight = Mathf.Abs(corners[1].y - corners[0].y);

            int visibleCount = visibleMembers.Length;

            float halfVisibleCount = visibleCount / 2f;

            int halfVisibleCountInt = Mathf.FloorToInt(halfVisibleCount);

            float visibleCountM1 = (visibleCount % 2 == 0) ? visibleCount : (visibleCount - 1f);

            int previousSelection = SelectedIndex;

            SelectedIndex = Maths.wrap(previousSelection - amount, members.Count);

            int x = vertical ? 0 : 1;

            Vector3 startPos = canvasTransform.InverseTransformPoint(content.position);

            void ShiftMembers()
            {

                content.localPosition = startPos;

                for (int a = 0; a < visibleCount; a++) visibleMembersProxy[a] = visibleMembers[a];

                for (int a = 0; a < visibleCount; a++)
                {

                    int visibleIndex = Maths.wrap(a + amount, visibleCount);

                    RectTransform member = visibleMembersProxy[a];

                    float t = visibleIndex / visibleCountM1;

                    Vector3 canvasSpacePosition = new Vector3(center.x + (((visibleCount * memberSize * t) - (halfVisibleCount * memberSize)) * (positiveToNegative ? -1 : 1)) * x, center.y + (((visibleCount * memberSize * t) - (halfVisibleCount * memberSize)) * (positiveToNegative ? -1 : 1)) * (1 - x), center.z);

                    member.position = canvasTransform.TransformPoint(canvasSpacePosition);

                    member.localScale = (float3)CalculateScaling(canvasSpacePosition, corners);

                    visibleMembers[visibleIndex] = member;

                }

                OnSlideEnd?.Invoke();

            }

            if (time <= 0)
            {
#if SWOLE_ENV
                AppendTween(LeanTween.delayedCall(gameObject, 0.001f, ShiftMembers), null, OnSlideBegin.Invoke);
#endif

            }
            else
            {

                float mul = visibleCount / visibleCountM1;

                Vector3 targetPos = startPos + new Vector3(x * amount * mul * memberSize * (positiveToNegative ? -1 : 1), (1 - x) * amount * mul * memberSize * (positiveToNegative ? -1 : 1), 0);
#if SWOLE_ENV
                void SlidePosition(float t)
                {

                    content.position = canvasTransform.TransformPoint(Vector3.LerpUnclamped(startPos, targetPos, t));

                    for (int a = 0; a < visibleCount; a++)
                    {

                        visibleMembers[a].localScale = (float3)CalculateScaling(canvasTransform.InverseTransformPoint(visibleMembers[a].position), corners);

                    }

                }

                LTDescr newTween = LeanTween.value(gameObject, 0, 1, time).setOnUpdate(SlidePosition);

                if (easeIn) newTween.setEaseInExpo();

                if (easeOut) newTween.setEaseOutExpo();

                AppendTween(newTween, ShiftMembers, OnSlideBegin.Invoke);
#endif
            }

        }

        public void SlideLeft(int amount)
        {

            SlideMembers((positiveToNegative ? -1 : 1) * amount, defaultTweenTime, easeIn, easeOut);

        }

        public void SlideLeft()
        {

            SlideLeft(1);

        }

        public void SlideRight(int amount)
        {

            SlideMembers((positiveToNegative ? 1 : -1) * amount, defaultTweenTime, easeIn, easeOut);

        }

        public void SlideRight()
        {

            SlideRight(1);

        }

    }

}

#endif