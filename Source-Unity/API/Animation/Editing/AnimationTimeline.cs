#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

using TMPro;

using Swole.UI;

namespace Swole.API.Unity.Animation
{
    public class AnimationTimeline : MonoBehaviour
    {

        public const int frameRate = CustomAnimation.DefaultFrameRate;

        public bool disableNavigation;
        public float zoomSensitivity = 30;

        public GameObject frameMarkerPrototype;
        public bool frameMarkerPrototypeIsPrefab;
        [SerializeField]
        protected RectTransform frameMarkerContainerTransform;
        private PrefabPool frameMarkerPool;
        public RectTransform terminatingMarker;

        protected bool hideFrameMarkers;
        public void SetRenderFrameMarkers(bool render)
        {
            hideFrameMarkers = !render;
            RefreshFrameMarkers(previousWidth);
        }

        public RectTransform scrubber;
        public RectTransform nonLinearScrubber;

        public RectTransform frameIndicator;
        public RectTransform nonLinearFrameIndicator;

        [SerializeField]
        protected RectTransform containerTransform;
        protected const float minContainerWidth = 100;
        protected float defaultContainerWidth = minContainerWidth;
        public float containerPadding = 25;
        public RectTransform ContainerTransform
        {
            get
            {
                if (containerTransform == null) 
                { 
                    containerTransform = gameObject.AddOrGetComponent<RectTransform>();
                    defaultContainerWidth = containerTransform.rect.width / 10;
                    if (defaultContainerWidth < minContainerWidth) defaultContainerWidth = minContainerWidth;
                }
                return containerTransform;
            }
        }
        protected UIDraggable containerDraggable;
        public UIDraggable ContainerDraggable
        {
            get
            {
                if (containerDraggable == null)
                {
                    containerDraggable = containerTransform.gameObject.AddOrGetComponent<UIDraggable>();
                    containerDraggable.freeze = true;
                    if (containerDraggable.OnDragStep == null) containerDraggable.OnDragStep = new UnityEvent(); else containerDraggable.OnDragStep.RemoveAllListeners();
                    containerDraggable.OnDragStep.AddListener(OnDrag);
                    if (containerDraggable.OnPress == null) containerDraggable.OnPress = new UnityEvent(); else containerDraggable.OnPress.RemoveAllListeners();
                    containerDraggable.OnPress.AddListener(OnPress);
                }
                return containerDraggable;
            }
        }
        protected Canvas canvas;
        public Canvas Canvas
        {
            get
            {
                if (canvas == null)
                {
                    if (containerTransform != null) canvas = containerTransform.GetComponentInParent<Canvas>(true); else gameObject.GetComponentInParent<Canvas>(true);
                }
                return canvas;
            }
        }
        public Vector3 ScreenPositionToWorldPosition(Vector3 screenPos)
        {
            var canv = Canvas;
            return canv.transform.TransformPoint(canv.ScreenToCanvasSpace(screenPos));
        }

        public UIPannable pannable;

        public InputField lengthInput;
        public TMP_InputField lengthInputTMP;

        public InputField timeInput;
        public TMP_InputField timeInputTMP;

        public float widthToMarkersRatio = 100;
        public float minWidth = 100;

        public bool useFrameIncrements;
        public int frameIncrement = 5;
        public float frameIncrementScale = 2;

        [SerializeField]
        protected float length = 2.5f;
        public void SetLength(float newLength, bool notifyListeners=true)
        {
            if (newLength == length) return;
            float oldLength = length;
            newLength = Mathf.Min(newLength, AnimationEditor.maxAnimationLength);
            length = newLength;
            if (lengthInput != null) lengthInput.SetTextWithoutNotify(length.ToString());
            if (lengthInputTMP != null) lengthInputTMP.SetTextWithoutNotify(length.ToString());

            if (containerTransform != null)
            {
                var rect = containerTransform.rect;
                float width = rect.width;
                if (oldLength > 0 && newLength > 0) 
                {
                    width = width * (newLength / oldLength);
                } 
                else
                {
                    width = defaultContainerWidth; 
                }
                width = Mathf.Max(minWidth, width);
                containerTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                previousWidth = width;
                RefreshFrameMarkers(width);
            }
            if (notifyListeners && OnSetLength != null) OnSetLength.Invoke(newLength);
            SetScrubPosition(Mathf.Min(ScrubPosition, newLength), notifyListeners);
        }
        public float Length
        {
            get => length;
            set => SetLength(value);
        }
        public int LengthInFrames => Mathf.FloorToInt(Length * frameRate);
        public int ClampInFrameRange(int frame) => Mathf.Clamp(frame, 0, LengthInFrames);

        [SerializeField]
        protected float scrubPosition = 1;
        public float ScrubPosition
        {
            get => scrubPosition;
            set => SetScrubPosition(value);
        }
        public void SetScrubPosition(float position, bool notifyListeners=true, bool clamp=true, bool moveView=false)
        {
            if (clamp) position = Mathf.Clamp(position, 0, length);
            scrubPosition = position;
            if (timeInput != null) timeInput.SetTextWithoutNotify(position.ToString());
            if (timeInputTMP != null) timeInputTMP.SetTextWithoutNotify(position.ToString());
            if (notifyListeners && OnSetScrubPosition != null) OnSetScrubPosition.Invoke(position);
            RefreshScrubber();
            RefreshFrameIndicator();
            if (moveView && pannable != null)
            {
                pannable.SetPanPositionHor(length <= 0 ? 0 : (position / length));
            }
        }
        [SerializeField]
        protected float nonlinearScrubPosition = 1;
        public float NonLinearScrubPosition
        {
            get => nonlinearScrubPosition;
            set => SetNonlinearScrubPosition(value);
        }
        public void SetNonlinearScrubPosition(float position, bool notifyListeners = true)
        {
            nonlinearScrubPosition = position;
            if (notifyListeners && OnSetNonlinearScrubPosition != null) OnSetNonlinearScrubPosition.Invoke(position);
            RefreshNonlinearScrubber();
            RefreshNonlinearFrameIndicator();
        }

        public UnityEvent<float> OnSetScrubPosition;
        public UnityEvent<float> OnSetNonlinearScrubPosition;
        public UnityEvent<float> OnSetLength;

        private float previousWidth;

        public int CalculateFrameAtTimelinePosition(decimal time) => /*length <= 0 ? 1 : */CalculateFrameAtTimelinePosition(time, frameRate);
        public int CalculateFrameAtTimelinePositionFloat(float time) => CalculateFrameAtTimelinePosition((decimal)time);
        public static int CalculateFrameAtTimelinePosition(decimal time, int frameRate) => Mathf.FloorToInt(((float)time + 0.00001f) * frameRate);
        public static int CalculateFrameAtTimelinePositionFloat(float time, int frameRate) => CalculateFrameAtTimelinePosition((decimal)time, frameRate);
        public decimal CalculateFrameTimeAtTimelinePosition(decimal time) => CalculateFrameTimeAtTimelinePosition(time, frameRate);
        public decimal CalculateFrameTimeAtTimelinePositionFloat(float time) => CalculateFrameTimeAtTimelinePosition((decimal)time);
        public static decimal CalculateFrameTimeAtTimelinePosition(decimal time, int frameRate) => FrameToTimelinePosition(CalculateFrameAtTimelinePosition(time, frameRate), frameRate);
        public static float CalculateFrameTimeAtTimelinePositionFloat(float time, int frameRate) => (float)CalculateFrameTimeAtTimelinePosition((decimal)time, frameRate);
        public decimal FrameToTimelinePosition(int frame) => FrameToTimelinePosition(frame, frameRate);
        public static decimal FrameToTimelinePosition(int frame, int frameRate) => frame / (decimal) frameRate;


        public int ScrubFrame => CalculateFrameAtTimelinePosition((decimal)ScrubPosition);
        public decimal ScrubFrameTime => CalculateFrameTimeAtTimelinePosition((decimal)ScrubPosition);

        public int NonLinearScrubFrame => CalculateFrameAtTimelinePosition((decimal)nonlinearScrubPosition);
        public decimal NonLinearScrubFrameTime => CalculateFrameTimeAtTimelinePosition((decimal)nonlinearScrubPosition);

        protected void Awake()
        {
            if (!Application.isPlaying) return;

            if (frameMarkerPrototype == null)
            {
                swole.LogError($"Frame Marker Prototype not set for Animation Timeline Editor '{name}'");
                Destroy(this);
                return;
            }

            if (scrubber == null)
            {
                swole.LogError($"Scrubber not set for Animation Timeline Editor '{name}'");
                Destroy(this);
                return;
            }

            Image containerImg = ContainerTransform.GetComponent<Image>();
            if (containerImg == null)
            {
                containerImg = containerTransform.gameObject.AddComponent<Image>();
                containerImg.color = new Color(1, 1, 1, 0.001f);
            }
            containerImg.raycastTarget = true;
            ContainerDraggable.gameObject.AddOrGetComponent<UIDraggable>();

            Vector2 vec;
            if (!frameMarkerPrototypeIsPrefab)
            {
                RectTransform rT = frameMarkerPrototype.GetComponent<RectTransform>();
                vec = new Vector2(0.5f, 0.5f);
                rT.pivot = vec;
                vec = new Vector2(0.5f, 0);
                rT.anchorMin = vec;
                vec = new Vector2(0.5f, 1);
                rT.anchorMax = vec;

                rT.SetParent(frameMarkerContainerTransform == null ? containerTransform : frameMarkerContainerTransform);
                frameMarkerPrototype.gameObject.SetActive(false);
            }

            if (scrubber != null)
            {
                scrubber.SetParent(containerTransform);
                vec = scrubber.pivot;
                vec.y = 1;
                scrubber.pivot = vec;

                CustomEditorUtils.SetAllRaycastTargets(scrubber, false);
            }
            if (nonLinearScrubber != null)
            {
                nonLinearScrubber.SetParent(containerTransform);
                vec = nonLinearScrubber.pivot;
                vec.y = 1;
                nonLinearScrubber.pivot = vec;

                CustomEditorUtils.SetAllRaycastTargets(nonLinearScrubber, false);
            }

            frameMarkerPool = new GameObject("frameMarkerPool").AddComponent<PrefabPool>();
            frameMarkerPool.transform.SetParent(transform, false);
            frameMarkerPool.SetContainerTransform(frameMarkerContainerTransform == null ? containerTransform : frameMarkerContainerTransform, false, false);
            frameMarkerPool.Reinitialize(frameMarkerPrototype, PoolGrowthMethod.Incremental, 1, 1, 1024);

            if (pannable == null) pannable = gameObject.GetComponent<UIPannable>();
            if (pannable != null) pannable.disableInput = true;

            #region Length Input
            if (lengthInput != null)
            {
                if (lengthInput.onEndEdit == null) lengthInput.onEndEdit = new InputField.EndEditEvent();
                lengthInput.onEndEdit.AddListener((string valText) =>
                {
                    if (valText.TryParseFloatStrict(out float val)) SetLength(val);
                });
                lengthInput.SetTextWithoutNotify(length.ToString());
            }
            if (lengthInputTMP != null)
            {
                if (lengthInputTMP.onEndEdit == null) lengthInputTMP.onEndEdit = new TMP_InputField.SubmitEvent();
                lengthInputTMP.onEndEdit.AddListener((string valText) =>
                {
                    if (valText.TryParseFloatStrict(out float val)) SetLength(val); 
                });
                lengthInputTMP.SetTextWithoutNotify(length.ToString());
            }
            #endregion

            #region Time Input
            if (timeInput != null)
            {
                if (timeInput.onEndEdit == null) timeInput.onEndEdit = new InputField.EndEditEvent();
                timeInput.onEndEdit.AddListener((string valText) =>
                {
                    if (valText.TryParseFloatStrict(out float val)) SetScrubPosition(val, true, true, true);
                });
                timeInput.SetTextWithoutNotify(ScrubPosition.ToString()); 
            }
            if (timeInputTMP != null)
            {
                if (timeInputTMP.onEndEdit == null) timeInputTMP.onEndEdit = new TMP_InputField.SubmitEvent();
                timeInputTMP.onEndEdit.AddListener((string valText) =>
                {
                    if (valText.TryParseFloatStrict(out float val)) SetScrubPosition(val, true, true, true);
                });
                timeInputTMP.SetTextWithoutNotify(ScrubPosition.ToString());
            }
            #endregion
        } 

        private readonly List<GameObject> activeFrameMarkers = new List<GameObject>();
        private readonly List<GameObject> activeFrameLines = new List<GameObject>();
        public float NormalizedContainerPadding => lastWidth <= 0 ? 0 : (containerPadding / lastWidth);
        public float NormalizedInnerRange => lastWidth <= 0 ? 0 : (Mathf.Max(0, lastWidth - (containerPadding * 2)) / lastWidth);

        private float lastWidth;
        public void RefreshFrameMarkers(float width)
        {
            lastWidth = width;
            if (frameMarkerPool == null) return;

            if (hideFrameMarkers)
            {
                foreach (var marker in activeFrameMarkers) if (marker != null) marker.gameObject.SetActive(false);
                foreach (var line in activeFrameLines) if (line != null) line.gameObject.SetActive(false);
                return;
            }

            float fullWidth = width;
            width = Mathf.Max(0, width - (containerPadding * 2));

            int maxCount = Mathf.Min(Mathf.FloorToInt(length * frameRate), Mathf.FloorToInt(width / widthToMarkersRatio));
            int count = Mathf.Max(1, maxCount);
            if (useFrameIncrements && count > 1) 
            {
                int frameLength = Mathf.FloorToInt(length * frameRate);
                float increment = frameIncrement;
                if (frameIncrementScale > 0) 
                {
                    while (Mathf.FloorToInt(frameLength / increment) < count) increment = increment / frameIncrementScale;
                    while (Mathf.FloorToInt(frameLength / increment) > count && Mathf.FloorToInt(frameLength / (increment * frameIncrementScale)) >= count) increment = increment * frameIncrementScale;

                    count = Mathf.Max(1, Mathf.Min(maxCount, Mathf.FloorToInt(frameLength / increment)));  
                }
            }

            activeFrameMarkers.RemoveAll(i => i == null);
            while (activeFrameMarkers.Count < count)
            {
                if (frameMarkerPool.TryGetNewInstance(out GameObject newMarker))
                {
                    activeFrameMarkers.Add(newMarker);
                    newMarker.SetActive(true);
                    CustomEditorUtils.SetAllRaycastTargets(newMarker, false);
                }
                else break; 
            }
            while (activeFrameMarkers.Count > count && count >= 0)
            {
                int i = activeFrameMarkers.Count - 1;
                var marker = activeFrameMarkers[i];
                marker.SetActive(false);
                frameMarkerPool.Release(marker);
                activeFrameMarkers.RemoveAt(i);
            }

            count = activeFrameMarkers.Count;

            Vector2 containerPivot = containerTransform.pivot;
            Transform container = frameMarkerContainerTransform == null ? containerTransform : frameMarkerContainerTransform;

            float step = count <= 1 ? 1 : (1f / count);
            float paddingOffset = containerPadding / fullWidth;
            float innerRange = width / fullWidth;
            for (int a = 0; a < count; a++)
            {
                GameObject marker = activeFrameMarkers[a]; 
                if (marker == null) continue;

                marker.SetActive(true);

                RectTransform markerTransform = marker.GetComponent<RectTransform>();
                markerTransform.SetParent(container);
                markerTransform.SetSiblingIndex(a);

                markerTransform.pivot = new Vector2(0, 1);
                float x1 = ((a * step) * innerRange) + paddingOffset;
                markerTransform.anchorMin = new Vector2(x1, 0);
                float x2 = (((a * step) + step) * innerRange) + paddingOffset;
                markerTransform.anchorMax = new Vector2(x2, 1);
                markerTransform.anchoredPosition3D = Vector3.zero;
                markerTransform.sizeDelta = Vector2.zero;

                float timef = count <= 1 ? length : GetTimeFromNormalizedPosition(x1); 
                int frame = CalculateFrameAtTimelinePosition((decimal)timef);

                int minutes = Mathf.FloorToInt(timef / 60);
                int seconds = Mathf.FloorToInt(timef - (minutes * 60));
                int ms = Mathf.RoundToInt(((timef - (minutes * 60)) - seconds) * 1000); 

                string time = $"{(minutes > 0 ? $"{minutes}:" : "")}{(minutes > 0 ? (seconds < 10 ? $"0{seconds}" : $"{seconds}") : $"{seconds}")}{(ms <= 0 ? "" : (":" + (ms < 10 ? $"00{ms}" : (ms < 100 ? $"0{ms}" : $"{ms}"))))}";

                Transform timeTransform = markerTransform.FindDeepChildLiberal("time");
                Transform frameTransform = markerTransform.FindDeepChildLiberal("frame");

                if (timeTransform != null)
                {
                    var text = timeTransform.GetComponentInChildren<Text>();
                    var textTMP = timeTransform.GetComponentInChildren<TMP_Text>();

                    if (text != null) text.text = time; 
                    if (textTMP != null) textTMP.SetText(time);
                }

                if (frameTransform != null)
                {
                    var text = frameTransform.GetComponentInChildren<Text>();
                    var textTMP = frameTransform.GetComponentInChildren<TMP_Text>(); 

                    string frameStr = frame.ToString();

                    if (text != null) text.text = frameStr;
                    if (textTMP != null) textTMP.SetText(frameStr);
                }
            }

            if (terminatingMarker != null)
            {
                float end = paddingOffset + innerRange;
                terminatingMarker.anchorMin = new Vector2(end, 0);
                terminatingMarker.anchorMax = new Vector2(end, 1);
                terminatingMarker.anchoredPosition3D = Vector3.zero;
                Vector2 sizeDelta = terminatingMarker.sizeDelta; 
                sizeDelta.y = 0;
                terminatingMarker.sizeDelta = sizeDelta;
            }
        }

        public float GetVisibleTimelineNormalizedPositionFromNormalizedRange(float range) => (range * NormalizedInnerRange) + NormalizedContainerPadding;
        public float GetVisibleTimelineNormalizedPosition(float time)
        {
            if (length <= 0) return 0;
            time = time / length;
            return GetVisibleTimelineNormalizedPositionFromNormalizedRange(time);
        }
        public float GetVisibleTimelineNormalizedFramePosition(float time) => GetVisibleTimelineNormalizedFramePosition(CalculateFrameAtTimelinePosition((decimal)time));
        public float GetVisibleTimelineNormalizedFramePosition(int frame)
        {
            float lengthFrames = LengthInFrames;
            if (lengthFrames <= 0) return 0;
            if (activeFrameMarkers.Count <= 1) return GetVisibleTimelineNormalizedPositionFromNormalizedRange(frame / lengthFrames);

            float spacing = 1f / activeFrameMarkers.Count;

            int startMarker = -1; 
            float endPos;
            while(true)
            {
                startMarker++;
                endPos = GetVisibleTimelineNormalizedPositionFromNormalizedRange((startMarker + 1) * spacing);
                int markerFrame = CalculateFrameAtTimelinePosition((decimal)GetTimeFromNormalizedPosition(endPos));
                if (markerFrame >= frame) break;
            }

            float startPos = GetVisibleTimelineNormalizedPositionFromNormalizedRange(startMarker * spacing);
            int startFrame = CalculateFrameAtTimelinePosition((decimal)GetTimeFromNormalizedPosition(startPos)); 
            int endFrame = CalculateFrameAtTimelinePosition((decimal)GetTimeFromNormalizedPosition(endPos));

            if (startFrame == endFrame) return startPos;

            return startPos + ((endPos - startPos) * ((frame - startFrame) / ((float)(endFrame - startFrame))));
        }
        public float GetTimeFromNormalizedPosition(float pos, bool clamp = false)
        {
            float time = ((pos - NormalizedContainerPadding) / NormalizedInnerRange) * length;

            if (clamp) time = Mathf.Clamp(time, 0, length); 
            return time;
        }
        protected AnimationCurve timeCurve;
        public AnimationCurve TimeCurve
        {
            get => timeCurve;
            set => SetTimeCurve(value);
        }
        public void SetTimeCurve(AnimationCurve curve)
        {
            timeCurve = curve;     
            NonLinearScrubPosition = GetNonlinearTimeFromTime(ScrubPosition);
        }
        public float GetNonlinearTimeFromNormalizedPosition(float pos, bool clamp = false) => GetNonlinearTimeFromTime(GetTimeFromNormalizedPosition(pos, clamp)); 
        public float GetNonlinearTimeFromTime(float time)
        {
            if (timeCurve == null) return time;
            return timeCurve.Evaluate(Length <= 0 ? 0 : (time / Length)) * Length;
        }

        protected readonly Vector3[] fourCornersArray = new Vector3[4];
        protected void OnDrag()
        {
            var draggable = ContainerDraggable;
            if (draggable == null) return;

            draggable.cancelNextClick = true;
            ScrubTimeWithCursor(draggable.DragCursorPositionLocal); 
        }
        protected void OnPress()
        {
            var draggable = ContainerDraggable;
            if (draggable == null) return;

            ScrubTimeWithCursor(draggable.LastClickLocalPosition);
        }
        protected void ScrubTimeWithCursor(Vector2 cursorPositionLocal)
        {
            frameIndicatorDespawnTimeout = frameIndicatorDespawnDelay; 

            ScrubPosition = GetTimeFromNormalizedPosition(GetNormalizedPositionFromLocalPosition(cursorPositionLocal), true);
            NonLinearScrubPosition = GetNonlinearTimeFromTime(ScrubPosition);
        }
        public float GetNormalizedPositionFromLocalPosition(Vector2 localPosition)
        {
            ContainerTransform.GetLocalCorners(fourCornersArray);
            Vector2 min = new Vector2(Mathf.Min(fourCornersArray[0].x, fourCornersArray[1].x, fourCornersArray[2].x, fourCornersArray[3].x), Mathf.Min(fourCornersArray[0].y, fourCornersArray[1].y, fourCornersArray[2].y, fourCornersArray[3].y));
            Vector2 max = new Vector2(Mathf.Max(fourCornersArray[0].x, fourCornersArray[1].x, fourCornersArray[2].x, fourCornersArray[3].x), Mathf.Max(fourCornersArray[0].y, fourCornersArray[1].y, fourCornersArray[2].y, fourCornersArray[3].y));
            Vector2 range = max - min;
            if (range.x == 0) return 0;

            return (localPosition.x - min.x) / range.x;
        }

        public void RefreshScrubber()
        {
            if (scrubber != null)
            {
                float anchorX = GetVisibleTimelineNormalizedPosition(ScrubPosition);
                scrubber.anchorMin = new Vector2(anchorX, 0);
                scrubber.anchorMax = new Vector2(anchorX, 1);
                scrubber.anchoredPosition3D = Vector3.zero;

                Vector2 vec = scrubber.sizeDelta;
                vec.y = 0;
                scrubber.sizeDelta = vec;
            }
        }
        public void RefreshNonlinearScrubber()
        {
            if (nonLinearScrubber != null)
            {
                float anchorX = GetVisibleTimelineNormalizedPosition(nonlinearScrubPosition);
                nonLinearScrubber.anchorMin = new Vector2(anchorX, 0);
                nonLinearScrubber.anchorMax = new Vector2(anchorX, 1);
                nonLinearScrubber.anchoredPosition3D = Vector3.zero;

                Vector2 vec = nonLinearScrubber.sizeDelta;
                vec.y = 0;
                nonLinearScrubber.sizeDelta = vec;
            }
        }
        public void RefreshScrubbers()
        {
            RefreshScrubber();
            RefreshNonlinearScrubber();
        }

        public void RefreshFrameIndicator()
        {
            if (frameIndicator != null && frameIndicatorDespawnTimeout > 0)
            {
                frameIndicator.gameObject.SetActive(true);
                CustomEditorUtils.SetComponentText(frameIndicator, $"{ScrubFrame}");

                float anchorX = GetVisibleTimelineNormalizedPosition(ScrubPosition);
                frameIndicator.anchorMin = new Vector2(anchorX, 1);
                frameIndicator.anchorMax = new Vector2(anchorX, 1);
                frameIndicator.anchoredPosition3D = Vector3.up;
            }
        }
        public void RefreshNonlinearFrameIndicator()
        {
            if (nonLinearFrameIndicator != null && frameIndicatorDespawnTimeout > 0)
            {
                nonLinearFrameIndicator.gameObject.SetActive(true);
                CustomEditorUtils.SetComponentText(nonLinearFrameIndicator, $"{NonLinearScrubFrame}");

                float anchorX = GetVisibleTimelineNormalizedPosition(nonlinearScrubPosition);
                nonLinearFrameIndicator.anchorMin = new Vector2(anchorX, 1);
                nonLinearFrameIndicator.anchorMax = new Vector2(anchorX, 1); 
                nonLinearFrameIndicator.anchoredPosition3D = Vector3.up;
            }
        }
        public void RefreshFrameIndicators()
        {
            RefreshFrameIndicator();
            RefreshNonlinearFrameIndicator();
        }

        public UnityEvent OnResize = new UnityEvent();

        protected void OnGUI()
        {
            Rect rect = containerTransform.rect;
            float width = rect.width;
            if (previousWidth != width)
            {
                previousWidth = width;
                RefreshFrameMarkers(width);
                
                OnResize?.Invoke();
            }

            RefreshScrubbers();
            RefreshFrameIndicators();
        }

        public void Zoom(float amount)
        {
            if (containerTransform != null)
            {
                var rect = containerTransform.rect;
                float width = rect.width;
                amount = amount * (containerTransform.rect.width / widthToMarkersRatio); 

                width = Mathf.Clamp(width + amount, minWidth, AnimationEditor.maxAnimationLength * frameRate * widthToMarkersRatio);
                containerTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                previousWidth = width;
                RefreshFrameMarkers(width);
                OnResize?.Invoke();
                RefreshScrubbers();
                RefreshFrameIndicators();
            }
        }

        public float frameIndicatorDespawnDelay = 1f;
        protected float frameIndicatorDespawnTimeout;

        protected void Update()
        {
            float zoom = InputProxy.Scroll * zoomSensitivity;
            if (zoom != 0 && !disableNavigation)
            {
                /*var hovered = CursorProxy.ObjectsUnderCursor;
                var transform = this.transform;

                bool flag = false;
                foreach (var obj in hovered)
                {
                    if (obj == null) continue;
                    if (obj == gameObject || obj.transform.IsChildOf(transform))
                    {
                        flag = true;
                        break;
                    }
                }*/
                var hovered = CursorProxy.FirstObjectUnderCursor;
                bool flag = hovered != null && (hovered == gameObject || hovered.transform.IsChildOf(transform));
                if (flag)
                {
                    Zoom(zoom);
                }
            }

            if (frameIndicatorDespawnTimeout > 0) 
            { 
                frameIndicatorDespawnTimeout -= Time.deltaTime;

                if (frameIndicatorDespawnTimeout <= 0)
                {
                    if (frameIndicator != null) frameIndicator.gameObject.SetActive(false);
                    if (nonLinearFrameIndicator != null) nonLinearFrameIndicator.gameObject.SetActive(false);
                }
            }
        }
    }
}

#endif