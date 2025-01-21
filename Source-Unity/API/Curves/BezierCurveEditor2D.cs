#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Swole.UI;
using Swole.API.Unity.Animation;

namespace Swole.API.Unity
{
    public class BezierCurveEditor2D : AnimationCurveEditor
    {

        public static int GetSegmentIndex(int index) => index / 3;
        public static int GetEndNodeIndex(int segmentIndex) => segmentIndex * 3;

        new protected IBezierCurve curve;
        protected KeyframeTangentSettings bezierTangentMode = new KeyframeTangentSettings() { inTangentMode = BrokenTangentMode.Linear, outTangentMode = BrokenTangentMode.Linear, tangentMode = TangentMode.Broken };

        new public virtual IBezierCurve Curve
        {
            get => curve;
            set => SetCurve(value); 
        }

        public override void SetCurve(AnimationCurve curve) {}
        public virtual void SetCurve(IBezierCurve curve)
        {
            PrepNewState();

            this.curve = curve;
            if (CurveRenderer is UIBezierCurveRenderer bcr) bcr.curve = curve;

            if (keyframeData != null)
            {
                foreach (var data in keyframeData)
                {
                    if (data == null) continue;
                    data.Destroy(keyframePool, tangentPool, tangentLinePool);
                }
            }
            selectedKeys.Clear();
            keyframes = null;
            if (curve == null) return;

            keyframes = curve.GetPointsAsKeyframes();

            if (keyframes != null)
            {
                keyframeData = new KeyframeData[keyframes.Length];
                for (int a = 0; a < keyframes.Length; a++) keyframeData[a] = CreateNewKeyframeData(keyframes[a], a, bezierTangentMode, false);  
            }
            else
            {
                keyframeData = new KeyframeData[0]; 
            }

            FinalizeState();

            rangeX = CurveRangeX;
            rangeY = CurveRangeY;

            Redraw();
        }

        public override UIAnimationCurveRenderer CurveRenderer 
        {
            get
            {
                if (curveRenderer == null)
                {
                    curveRenderer = gameObject.GetComponent<UIBezierCurveRenderer>(); 
                    if (curveRenderer == null)
                    {
                        curveRenderer = new GameObject("curve").AddComponent<UIBezierCurveRenderer>();
                        curveRenderer.LineThickness = 1;
                    }
                    RectTransform curveRectTransform = AnimationCurveEditorUtils.AddOrGetComponent<RectTransform>(curveRenderer.gameObject);
                    curveRectTransform.SetParent(TimelineTransform, false);
                    curveRectTransform.anchorMin = Vector2.zero;
                    curveRectTransform.anchorMax = Vector2.one;
                    curveRectTransform.anchoredPosition3D = Vector3.zero;
                    curveRectTransform.sizeDelta = Vector2.zero;

                    curveRenderer.SetRaycastTarget(false);
                    curveRenderer.SetRendererContainer(CurveRendererContainer);

                    curveRenderer.LineColor = curveColor;
                }
                return curveRenderer;
            }
            set => base.CurveRenderer = value;
        }

        public override void RebuildCurveMesh()
        {
            if (CurveRenderer is UIBezierCurveRenderer bcr)
            {
                bcr.curve = curve;
            }
            base.RebuildCurveMesh(); 
        }

        protected override void UpdateCurveInstance()
        {
            // convert keys to points
            Vector3[] points = new Vector3[keyframes == null ? 0 : keyframes.Length];
            for (int a = 0; a < points.Length; a++) points[a] = new Vector3(keyframes[a].time, keyframes[a].value, 0);
            if (curve != null) curve.SetPoints(points);

            RebuildCurveMesh();
        }

        public class BezierKeyframeData : KeyframeData
        {
            public BezierKeyframeData(KeyframeData baseData)
            {
                if (baseData == null) return;

                this.index = baseData.index;

                this.instance = baseData.instance;
                this.state = baseData.state;

                this.baseColor = baseData.baseColor;
                this.selectedColor = baseData.selectedColor;

                this.draggable = baseData.draggable;

                this.inTangentDraggable = baseData.inTangentDraggable;
                this.outTangentDraggable = baseData.outTangentDraggable;

                this.inTangentInstance = baseData.inTangentInstance;
                this.outTangentInstance = baseData.outTangentInstance;

                this.inTangentLine = baseData.inTangentLine;
                this.outTangentLine = baseData.outTangentLine; 
            }

            public override void UpdateInUI(KeyframeData[] keyframeData, RectTransform timelineTransform, Vector2 rangeX, Vector2 rangeY, bool showTangents = true, bool isSelected = false, float unweightedTangentHandleSize = 50, bool onlyUnweightedTangents = false)
            {
                base.UpdateInUI(keyframeData, timelineTransform, rangeX, rangeY, showTangents, isSelected, unweightedTangentHandleSize, onlyUnweightedTangents);

                bool isPathNode = index % 3 == 0;
                if (isPathNode)
                {

                    if (inTangentLine != null)
                    {
                        bool hide = true;
                        if (index > 0)
                        {
                            var leftNeighbor = keyframeData[index - 1];
                            if (leftNeighbor != null && leftNeighbor.instance != null)
                            {
                                hide = false;
                                inTangentLine.anchorB = leftNeighbor.instance.GetComponent<RectTransform>();
                            }
                        }
                        inTangentLine.gameObject.SetActive(!hide);
                        if (!hide) inTangentLine.RefreshIfUpdated();
                    }

                    if (outTangentLine != null)
                    {
                        bool hide = true;
                        if (index < keyframeData.Length - 1)
                        {
                            var rightNeighbor = keyframeData[index + 1]; 
                            if (rightNeighbor != null && rightNeighbor.instance != null)
                            {
                                hide = false;
                                outTangentLine.anchorB = rightNeighbor.instance.GetComponent<RectTransform>();    
                            }
                        }
                        outTangentLine.gameObject.SetActive(!hide);
                        if (!hide) outTangentLine.RefreshIfUpdated(); 
                    }
                }
            }
        }
        protected override KeyframeData CreateNewKeyframeData(Keyframe initialData, int index = -1, KeyframeTangentSettings tangentSettings = default, bool autoUpdateTangentSettings = true)
        {
            var keyData = base.CreateNewKeyframeData(initialData, index, tangentSettings, autoUpdateTangentSettings); 

            //if (keyData != null) keyData = new BezierKeyframeData(keyData);

            return keyData;
        }

        protected override void ReevaluateKeyframeData(KeyframeData keyData, bool updateInTangent = false, bool updateOutTangent = false, bool lockInTangent = false, bool lockOutTangent = false, bool updateState = true, bool canPropagateLeft = true, bool canPropagateRight = true, bool overrideTimeValue = false, float newTime = 0)
        {

            int keyframeIndex = keyData.index;

            if (keyframeIndex < 0 || keyframeIndex >= keyframeData.Length) 
            {
                keyData.Destroy(keyframePool, tangentPool, tangentLinePool);
                return;
            }

            var keyframe = keyData.Key;
            var oldKeyframe = keyframe;
            var position = keyData.RectTransform.position;
            var localPosition = TimelineTransform.InverseTransformPoint(position);

            bool isSelected = selectedKeys.Contains(keyframeIndex);

            OnKeyEditStart?.Invoke(keyframeIndex);

            if (updateState) PrepNewState();

            if (!overrideTimeValue) newTime = CalculateTimelinePosition(timelineTransform, localPosition, rangeX);

            keyframe.time = newTime;
            keyframe.value = CalculateTimelineValue(timelineTransform, localPosition, rangeY);
            keyframeData[keyframeIndex] = keyData;
            keyData.index = keyframeIndex;
            keyData.Key = keyframe;

            Rect timelineRect = timelineTransform.rect;

            //KeyframeData leftNeighbor = null;
            //KeyframeData rightNeighbor = null;

            //if (keyData.index > 0) leftNeighbor = keyframeData[keyData.index - 1];
            //if (keyData.index < keyframeData.Length - 1) rightNeighbor = keyframeData[keyData.index + 1];

            keyData.Key = keyframe;

            OnKeyEditEnd?.Invoke(keyframeIndex, keyframeIndex);

            bool hasMoved = (keyframe.time != oldKeyframe.time || keyframe.value != oldKeyframe.value);
            keyData.UpdateInUI(keyframeData, timelineTransform, rangeX, rangeY, false, isSelected, unweightedTangentHandleSize);

            if (updateState)
            {
                FinalizeState();
            }
            RefreshKeyframes();
        }

        protected override void SortKeys(List<KeyframeData> list) {}
        public override int AddKey(Keyframe data) => AddKey(data, -1);
        public int AddKey(Keyframe data, int index)
        {
            PrepNewState();
            PrepKeyCountEdit();

            if (index >= 0) index = Mathf.Max(1, Mathf.FloorToInt(index / 3f) * 3); // limit index to after the first node, and snap index to last node in a segment
            if (index > 0) index = index - 2;

            if (keyframeData != null)
            {
                tempKeys.Clear();
                tempKeys.AddRange(keyframeData);
                tempKeys.RemoveAll(i => i == null);

                if (tempKeys.Count <= 0)
                {
                    var key = CreateNewKeyframeData(data, -1, bezierTangentMode);
                    if (index < 0 || index >= tempKeys.Count) tempKeys.Add(key); else tempKeys.Insert(index, key);

                    for (int a = 0; a < tempKeys.Count; a++) tempKeys[a].index = a;
                    index = key.index; 
                } 
                else
                {
                    bool outOfRange = index < 0 || index >= tempKeys.Count;
                    var prevKey = outOfRange ? tempKeys[tempKeys.Count - 1] : tempKeys[index - 1];

                    var keyA = CreateNewKeyframeData(data, -1, bezierTangentMode);
                    var keyB = CreateNewKeyframeData(data, -1, bezierTangentMode);
                    var keyC = CreateNewKeyframeData(data, -1, bezierTangentMode);

                    keyA.Value = prevKey.Value + (data.value - prevKey.Value) * 0.35f;
                    keyB.Value = prevKey.Value + (data.value - prevKey.Value) * 0.7f; 

                    keyA.Time = prevKey.Time + (data.time - prevKey.Time) * 0.35f;
                    keyB.Time = prevKey.Time + (data.time - prevKey.Time) * 0.7f;  

                    if (outOfRange) 
                    { 
                        tempKeys.Add(keyA);
                        tempKeys.Add(keyB);
                        tempKeys.Add(keyC);
                    } 
                    else 
                    { 
                        tempKeys.Insert(index, keyC);
                        tempKeys.Insert(index, keyB);
                        tempKeys.Insert(index, keyA); 
                    }

                    for (int a = 0; a < tempKeys.Count; a++) tempKeys[a].index = a;
                    index = keyC.index;
                }

                keyframeData = tempKeys.ToArray(); 
            }
            else
            {
                index = 0;
                keyframeData = new KeyframeData[] { CreateNewKeyframeData(data, 0, bezierTangentMode) }; 
            }

            FinalizeKeyCountEdit();
            FinalizeState();
            RefreshKeyframes();

            OnKeyAdd?.Invoke(index);

            return index;
        }
        public int AddKey(Vector3 key, int index = -1) => AddKey(new Keyframe() { time = key.x, value = key.y }, index);
        public int AddKey(Vector2 key, int index = -1) => AddKey(new Keyframe() { time = key.x, value = key.y }, index);

        public override void DeleteSelectedKeys(bool redraw = true)
        {
            if (selectedKeys.Count == 0) return;

            PrepNewState();

            tempKeys.Clear();
            tempKeys.AddRange(keyframeData);

            foreach (var index in selectedKeys)
            {
                if (index <= 0 || index >= keyframeData.Length) continue; // dont allow first key to be deleted

                var key = keyframeData[index];
                if (key == null) continue;
                OnKeyDelete?.Invoke(index);

                int segIndex = 0;
                if (index % 3 == 0)
                {
                    segIndex = (index / 3) - 1;
                } 
                else
                {
                    segIndex = index / 3;
                }
                tempKeys.RemoveAll(i => ReferenceEquals(i, key) || i == null || (i.index % 3 != 0 && i.index / 3 == segIndex) || (i.index % 3 == 0 && i.index / 3 == segIndex + 1));

                key.index = -1;
                key.Destroy(keyframePool, tangentPool, tangentLinePool);
            }
            selectedKeys.Clear();

            tempKeys.RemoveAll(i => i == null);
            SortKeys(tempKeys);
            for (int a = 0; a < tempKeys.Count; a++) tempKeys[a].index = a;
            foreach (var key_ in keyframeData)
            {
                if (key_ == null || tempKeys.Contains(key_)) continue; 
                key_.Destroy();
            }
            keyframeData = tempKeys.ToArray();

            FinalizeState();
            RefreshKeyframes();

            if (redraw) Redraw();
        }
        public override void DeleteKeyStateless(int keyIndex, bool redraw = true)
        {
            if (keyframeData == null || keyIndex < 0 || keyIndex >= keyframeData.Length) return; 
            OnKeyDelete?.Invoke(keyIndex);

            SetDirty();

            PrepKeyCountEdit();

            tempKeys.Clear();
            tempKeys.AddRange(keyframeData);

            var key = tempKeys[keyIndex];
            if (key != null)
            {
                key.index = -1;
                key.Destroy(keyframePool, tangentPool, tangentLinePool);
            }

            tempKeys.RemoveAt(keyIndex);

            int segIndex = 0;
            if (keyIndex % 3 == 0)
            {
                segIndex = (keyIndex / 3) - 1;
            }
            else
            {
                segIndex = keyIndex / 3;
            }
            tempKeys.RemoveAll(i => i == null || (i.index % 3 != 0 && i.index / 3 == segIndex) || (i.index % 3 == 0 && i.index / 3 == segIndex + 1));

            SortKeys(tempKeys);
            for (int a = 0; a < tempKeys.Count; a++) tempKeys[a].index = a;
            foreach (var key_ in keyframeData)
            {
                if (key_ == null || tempKeys.Contains(key_)) continue;
                key_.Destroy();
            }
            keyframeData = tempKeys.ToArray();

            FinalizeKeyCountEdit();
        }
        public override void DeleteKeyStateless(KeyframeData key, bool redraw = true)
        {
            if (key == null || keyframeData == null) return;
            OnKeyDelete?.Invoke(key.index);

            SetDirty();

            PrepKeyCountEdit();

            tempKeys.Clear();
            tempKeys.AddRange(keyframeData);

            int segIndex = 0;
            if (key.index % 3 == 0)
            {
                segIndex = (key.index / 3) - 1;
            }
            else
            {
                segIndex = key.index / 3;
            }
            tempKeys.RemoveAll(i => ReferenceEquals(i, key) || i == null || (i.index % 3 != 0 && i.index / 3 == segIndex) || (i.index % 3 == 0 && i.index / 3 == segIndex + 1));

            key.index = -1;
            key.Destroy(keyframePool, tangentPool, tangentLinePool);

            SortKeys(tempKeys);
            for (int a = 0; a < tempKeys.Count; a++) tempKeys[a].index = a;
            foreach (var key_ in keyframeData)
            {
                if (key_ == null || tempKeys.Contains(key_)) continue;
                key_.Destroy();
            }
            keyframeData = tempKeys.ToArray();

            FinalizeKeyCountEdit();
        }

    }
}

#endif