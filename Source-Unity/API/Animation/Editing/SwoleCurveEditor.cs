#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Swole.API.Unity.Animation;

namespace Swole.API.Unity
{
    public class SwoleCurveEditor : AnimationCurveEditor
    {

        public delegate void CurveHistoryDelegate(bool undo);
        public struct ChangeStateAction : IRevertableAction
        {
            public bool ReapplyWhenRevertedTo => true;

            public AnimationCurveEditor curveEditor;
            public AnimationCurveEditor.State oldState;
            public AnimationCurveEditor.State newState;

            public CurveHistoryDelegate onChange;

            public void Reapply()
            {
                onChange?.Invoke(false);
                if (curveEditor != null) 
                {
                    if (!curveEditor.gameObject.activeSelf) curveEditor.gameObject.SetActive(true);
                    if (curveEditor.gameObject.activeInHierarchy) curveEditor.SetState(newState);  
                }
            }

            public void Revert()
            {
                onChange?.Invoke(true);
                if (curveEditor != null) 
                {
                    if (!curveEditor.gameObject.activeSelf) curveEditor.gameObject.SetActive(true);
                    if (curveEditor.gameObject.activeInHierarchy) curveEditor.SetState(oldState); 
                }
            }

            public void Perpetuate() { }
            public void PerpetuateUndo() { }

            public bool undoState;
            public bool GetUndoState() => undoState;
            public IRevertableAction SetUndoState(bool undone)
            {
                var newState = this;
                newState.undoState = undone;
                return newState;
            }
        }
        public override void SetCurve(AnimationCurve curve) => SetCurve(curve, true);
        public virtual void SetCurve(AnimationCurve curve, bool notifyListeners)
        {
            if (notifyListeners) PrepNewState(); else SetDirty();

            this.curve = curve;
            CurveRenderer.curve = this.curve;

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
            if (curve != null)
            {
                keyframes = this.curve.keys;
                if (keyframes != null)
                {
                    keyframeData = new KeyframeData[keyframes.Length];

                    var tangentSettings = KeyframeTangentSettings.Default;
                    tangentSettings.tangentMode = defaultKeyTangentMode;
                    for (int a = 0; a < keyframes.Length; a++) keyframeData[a] = CreateNewKeyframeData(keyframes[a], a, tangentSettings);

                }
                else
                {
                    keyframeData = new KeyframeData[0];
                }
            }

            if (notifyListeners) FinalizeState();

            rangeX = CurveRangeX;
            rangeY = CurveRangeY;

            Redraw();
        }

        protected EditableAnimationCurve editableCurve;
        public EditableAnimationCurve EditableCurve => editableCurve; 
        public void ClearEditableCurve() => editableCurve = null;
        protected void OnEditableCurveStateChange()
        {
            if (!enabled || !gameObject.activeInHierarchy) return;

            int length = editableCurve.length;
            if (keyframeData != null && length == keyframeData.Length)
            {
                for (int a = 0; a < length; a++) keyframeData[a].state = editableCurve[a]; 
            } 
            else
            {
                keyframeData = new KeyframeData[length];
                for (int a = 0; a < length; a++) keyframeData[a] = CreateNewKeyframeData(editableCurve[a], a, editableCurve[a], false);
            }

            if (keyframeData == null) return;

            foreach (var key in keyframeData) ReevaluateKeyframeData(key, true, true, false, false, false, false, false); 
        }

        public override AnimationCurve Curve
        {
            get
            {
                if (editableCurve != null) this.curve = editableCurve;
                return this.curve;
            }
            set => SetCurve(value);
        }
        public void SetCurve(EditableAnimationCurve curve) => SetCurve(curve, true);
        public virtual void SetCurve(EditableAnimationCurve curve, bool notifyListeners)
        {
            if (notifyListeners) PrepNewState(); else SetDirty();

            if (editableCurve != null) editableCurve.OnStateChange -= OnEditableCurveStateChange;
            if (curve != null) curve.OnStateChange += OnEditableCurveStateChange;

            this.editableCurve = curve;
            this.curve = curve;
            CurveRenderer.curve = this.curve;

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
            if (curve != null) 
            {
                SetState(curve, false, false);
                isDirty = false;  
            }
            
            if (notifyListeners) FinalizeState();

            rangeX = CurveRangeX;
            rangeY = CurveRangeY;

            Redraw();
        }

        public override void SetState(State state, bool notifyListeners = false, bool redraw = true)
        {
            if (editableCurve == null)
            {
                base.SetState(state, notifyListeners, redraw);
                return;
            }

            State preState = default;
            if (notifyListeners) preState = CurrentState;

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

            if (state.selectedKeys != null) foreach (var index in state.selectedKeys) selectedKeys.Add(index);

            keyframeData = new KeyframeData[state.keyframes == null ? 0 : state.keyframes.Length]; 
            for (int a = 0; a < keyframeData.Length; a++) keyframeData[a] = CreateNewKeyframeData(state.keyframes[a], a, state.keyframes[a], false);

            editableCurve.SetState(state, false);

            currentState = state;
            isDirty = false;

            if (notifyListeners) OnStateChange?.Invoke(preState, state);

            if (redraw) Redraw();
        }

        protected override void FinalizeState()
        {
            if (OnStateChange != null || EditableCurve != null)
            {
                var newState = CurrentState;
                if (EditableCurve != null) EditableCurve.SetState(newState, false); 
                OnStateChange?.Invoke(oldState, newState);
            }
        }

        public class InputManager : AnimationCurveEditorInput
        {
            public override List<GameObject> GetObjectsUnderCursor(List<GameObject> appendList = null, bool forceQuery = false, bool refreshRaycastersList = false) => CursorProxy.GetObjectsUnderCursor(appendList, forceQuery, refreshRaycastersList);

            public override Vector3 CursorScreenPosition => CursorProxy.ScreenPosition;
            public override float Scroll => Swole.InputProxy.Scroll;

            public override bool IsAltPressed => Swole.InputProxy.Modding_AlternateActionKey;
            public override bool IsCtrlPressed => Swole.InputProxy.Modding_PrimeActionKey;
            public override bool IsShiftPressed => Swole.InputProxy.Modding_ModifyActionKey;
            public override bool IsSpacePressed => Swole.InputProxy.Modding_PanKey;
            public override bool PressedCurveFocusKey => Swole.InputProxy.Modding_FocusKeyDown;
            public override bool PressedDeleteKey => Swole.InputProxy.Modding_DeleteKeyDown;
            public override bool PressedSelectAllDeselectAllKey => Swole.InputProxy.Modding_SelectAllKeyDown;
        }

        public override AnimationCurveEditorInput InputProxy
        {
            get
            {
                if (inputProxy == null) inputProxy = new InputManager();
                return inputProxy;
            }
            set
            {
                inputProxy = value;
            }
        }
    }
}

#endif