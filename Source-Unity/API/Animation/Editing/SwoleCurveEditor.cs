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
            if (curve == null) return;

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

            if (notifyListeners) FinalizeState();

            rangeX = CurveRangeX;
            rangeY = CurveRangeY;

            Redraw();
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