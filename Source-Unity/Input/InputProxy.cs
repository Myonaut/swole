#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Swole
{

    public static class InputProxy
    {

        private static InputManager manager;
        public static InputManager Manager
        {
            get
            {
                if (manager == null)
                {
                    PropertyInfo[] props = typeof(InputProxy).GetProperties(BindingFlags.Static | BindingFlags.Public);
                    FieldInfo[] fields = typeof(InputProxy).GetFields(BindingFlags.Static | BindingFlags.Public);
                    MethodInfo[] methods = typeof(InputProxy).GetMethods(BindingFlags.Static | BindingFlags.Public);

                    manager = new InputManager(props, fields, methods);
                }

                return manager;
            }
        }

#if ENABLE_INPUT_SYSTEM // TODO: Add support for new unity input system
#endif

        #region Standard

        private static KeyCode CloseOrQuitKeyCode = KeyCode.Escape;
        public static bool CloseOrQuitKey { get { return Input.GetKey(CloseOrQuitKeyCode); } }
        public static bool CloseOrQuitKeyDown { get { return Input.GetKeyUp(CloseOrQuitKeyCode); } }
        public static bool CloseOrQuitKeyUp { get { return Input.GetKeyDown(CloseOrQuitKeyCode); } }

        #endregion

        #region Inventory

        private static KeyCode ItemCombineKeyCode = KeyCode.LeftShift;
        public static bool ItemCombineKey { get { return Input.GetKey(ItemCombineKeyCode); } }
        public static bool ItemCombineKeyDown { get { return Input.GetKeyUp(ItemCombineKeyCode); } }
        public static bool ItemCombineKeyUp { get { return Input.GetKeyDown(ItemCombineKeyCode); } }

        private static KeyCode ItemAlternateKeyCodeA = KeyCode.LeftControl;
        private static KeyCode ItemAlternateKeyCodeB = KeyCode.LeftCommand;
        public static bool ItemAlternateKey { get { return Input.GetKey(ItemAlternateKeyCodeA) || Input.GetKey(ItemAlternateKeyCodeB); } }
        public static bool ItemAlternateKeyDown { get { return Input.GetKeyUp(ItemAlternateKeyCodeA) || Input.GetKeyUp(ItemAlternateKeyCodeB); } }
        public static bool ItemAlternateKeyUp { get { return Input.GetKeyDown(ItemAlternateKeyCodeA) || Input.GetKeyDown(ItemAlternateKeyCodeB); } }

        private static KeyCode ItemTransferKeyCode = KeyCode.T;
        public static bool ItemTransferKey { get { return Input.GetKey(ItemTransferKeyCode); } }
        public static bool ItemTransferKeyDown { get { return Input.GetKeyUp(ItemTransferKeyCode); } }
        public static bool ItemTransferKeyUp { get { return Input.GetKeyDown(ItemTransferKeyCode); } }

        private static KeyCode ItemRotateKeyCode = KeyCode.R;
        public static bool ItemRotateKey { get { return Input.GetKey(ItemRotateKeyCode); } }
        public static bool ItemRotateKeyUp { get { return Input.GetKeyUp(ItemRotateKeyCode); } }
        public static bool ItemRotateKeyDown { get { return Input.GetKeyDown(ItemRotateKeyCode); } }

        private static KeyCode InventoryKeyCode = KeyCode.I;
        public static bool InventoryKey { get { return Input.GetKey(InventoryKeyCode); } }
        public static bool InventoryKeyUp { get { return Input.GetKeyUp(InventoryKeyCode); } }
        public static bool InventoryKeyDown { get { return Input.GetKeyDown(InventoryKeyCode); } }

        private static KeyCode EquipmentKeyCode = KeyCode.E;
        public static bool EquipmentKey { get { return Input.GetKey(EquipmentKeyCode); } }
        public static bool EquipmentKeyUp { get { return Input.GetKeyUp(EquipmentKeyCode); } }
        public static bool EquipmentKeyDown { get { return Input.GetKeyDown(EquipmentKeyCode); } }

        #endregion

        #region Modding

        private static KeyCode Modding_PrimeActionKeyCodeA = KeyCode.LeftControl;
        private static KeyCode Modding_PrimeActionKeyCodeB = KeyCode.RightControl;
        private static KeyCode Modding_PrimeActionKeyCodeC = KeyCode.LeftCommand;
        private static KeyCode Modding_PrimeActionKeyCodeD = KeyCode.RightCommand;
        public static bool Modding_PrimeActionKey { get { return Input.GetKey(Modding_PrimeActionKeyCodeA) || Input.GetKey(Modding_PrimeActionKeyCodeB) || Input.GetKey(Modding_PrimeActionKeyCodeC) || Input.GetKey(Modding_PrimeActionKeyCodeD); } }
        public static bool Modding_PrimeActionKeyUp { get { return Input.GetKeyUp(Modding_PrimeActionKeyCodeA) || Input.GetKeyUp(Modding_PrimeActionKeyCodeB) || Input.GetKeyUp(Modding_PrimeActionKeyCodeC) || Input.GetKeyUp(Modding_PrimeActionKeyCodeD); } }
        public static bool Modding_PrimeActionKeyDown { get { return Input.GetKeyDown(Modding_PrimeActionKeyCodeA) || Input.GetKeyDown(Modding_PrimeActionKeyCodeB) || Input.GetKeyDown(Modding_PrimeActionKeyCodeC) || Input.GetKeyDown(Modding_PrimeActionKeyCodeD); } }

        private static KeyCode Modding_ModifyActionKeyCodeA = KeyCode.LeftShift;
        private static KeyCode Modding_ModifyActionKeyCodeB = KeyCode.RightShift;
        public static bool Modding_ModifyActionKey { get { return Input.GetKey(Modding_ModifyActionKeyCodeA) || Input.GetKey(Modding_ModifyActionKeyCodeB); } }
        public static bool Modding_ModifyActionKeyUp { get { return Input.GetKeyUp(Modding_ModifyActionKeyCodeA) || Input.GetKeyUp(Modding_ModifyActionKeyCodeB); } }
        public static bool Modding_ModifyActionKeyDown { get { return Input.GetKeyDown(Modding_ModifyActionKeyCodeA) || Input.GetKeyDown(Modding_ModifyActionKeyCodeB); } }

        private static KeyCode Modding_AlternateActionKeyCodeA = KeyCode.LeftAlt;
        private static KeyCode Modding_AlternateActionKeyCodeB = KeyCode.RightAlt;
        public static bool Modding_AlternateActionKey { get { return Input.GetKey(Modding_AlternateActionKeyCodeA) || Input.GetKey(Modding_AlternateActionKeyCodeB); } }
        public static bool Modding_AlternateActionKeyUp { get { return Input.GetKeyUp(Modding_AlternateActionKeyCodeA) || Input.GetKeyUp(Modding_AlternateActionKeyCodeB); } }
        public static bool Modding_AlternateActionKeyDown { get { return Input.GetKeyDown(Modding_AlternateActionKeyCodeA) || Input.GetKeyDown(Modding_AlternateActionKeyCodeB); } }

        private static KeyCode Modding_PanKeyCode = KeyCode.Space;
        public static bool Modding_PanKey { get { return Input.GetKey(Modding_PanKeyCode); } }
        public static bool Modding_PanUp { get { return Input.GetKeyUp(Modding_PanKeyCode); } }
        public static bool Modding_PanDown { get { return Input.GetKeyDown(Modding_PanKeyCode); } }

        private static KeyCode Modding_DeleteKeyCode = KeyCode.Delete;
        public static bool Modding_DeleteKey { get { return Input.GetKey(Modding_DeleteKeyCode); } }
        public static bool Modding_DeleteKeyUp { get { return Input.GetKeyUp(Modding_DeleteKeyCode); } }
        public static bool Modding_DeleteKeyDown { get { return Input.GetKeyDown(Modding_DeleteKeyCode); } }

        private static KeyCode Modding_SelectAllKeyCode = KeyCode.A;
        public static bool Modding_SelectAllKey { get { return Input.GetKey(Modding_SelectAllKeyCode); } }
        public static bool Modding_SelectAllKeyUp { get { return Input.GetKeyUp(Modding_SelectAllKeyCode); } }
        public static bool Modding_SelectAllKeyDown { get { return Input.GetKeyDown(Modding_SelectAllKeyCode); } }

        private static KeyCode Modding_FocusKeyCode = KeyCode.F;
        public static bool Modding_FocusKey { get { return Input.GetKey(Modding_FocusKeyCode); } }
        public static bool Modding_FocusKeyUp { get { return Input.GetKeyUp(Modding_FocusKeyCode); } }
        public static bool Modding_FocusKeyDown { get { return Input.GetKeyDown(Modding_FocusKeyCode); } }

        private static KeyCode Modding_UndoKeyCode = KeyCode.Z;
        public static bool Modding_UndoKey { get { return Modding_PrimeActionKey && Input.GetKey(Modding_UndoKeyCode); } }
        public static bool Modding_UndoKeyUp { get { return Input.GetKeyUp(Modding_UndoKeyCode); } }
        public static bool Modding_UndoKeyDown { get { return Modding_PrimeActionKey && Input.GetKeyDown(Modding_UndoKeyCode); } }

        public static bool Modding_RedoKey { get { return Modding_ModifyActionKey && Modding_UndoKey; } }
        public static bool Modding_RedoKeyUp { get { return Modding_UndoKeyUp; } }
        public static bool Modding_RedoKeyDown { get { return Modding_ModifyActionKey && Modding_UndoKeyDown; } }

        private static KeyCode Modding_CopyKeyCode = KeyCode.C;
        public static bool Modding_CopyKey { get { return Input.GetKey(Modding_CopyKeyCode); } }
        public static bool Modding_CopyKeyUp { get { return Input.GetKeyUp(Modding_CopyKeyCode); } }
        public static bool Modding_CopyKeyDown { get { return Input.GetKeyDown(Modding_CopyKeyCode); } }

        private static KeyCode Modding_PasteKeyCode = KeyCode.V;
        public static bool Modding_PasteKey { get { return Input.GetKey(Modding_PasteKeyCode); } }
        public static bool Modding_PasteKeyUp { get { return Input.GetKeyUp(Modding_PasteKeyCode); } }
        public static bool Modding_PasteKeyDown { get { return Input.GetKeyDown(Modding_PasteKeyCode); } }

        #endregion

        #region Gameplay

        private static KeyCode MoveForwardKeyCode = KeyCode.W;
        public static bool MoveForwardKey { get { return Input.GetKey(MoveForwardKeyCode); } }
        public static bool MoveForwardKeyDown { get { return Input.GetKeyUp(MoveForwardKeyCode); } }
        public static bool MoveForwardKeyUp { get { return Input.GetKeyDown(MoveForwardKeyCode); } }

        private static KeyCode MoveBackwardKeyCode = KeyCode.S;
        public static bool MoveBackwardKey { get { return Input.GetKey(MoveBackwardKeyCode); } }
        public static bool MoveBackwardKeyDown { get { return Input.GetKeyUp(MoveBackwardKeyCode); } }
        public static bool MoveBackwardKeyUp { get { return Input.GetKeyDown(MoveBackwardKeyCode); } }

        private static KeyCode MoveLeftKeyCode = KeyCode.A;
        public static bool MoveLeftKey { get { return Input.GetKey(MoveLeftKeyCode); } }
        public static bool MoveLeftKeyDown { get { return Input.GetKeyUp(MoveLeftKeyCode); } }
        public static bool MoveLeftKeyUp { get { return Input.GetKeyDown(MoveLeftKeyCode); } }

        private static KeyCode MoveRightKeyCode = KeyCode.D;
        public static bool MoveRightKey { get { return Input.GetKey(MoveRightKeyCode); } }
        public static bool MoveRightKeyDown { get { return Input.GetKeyUp(MoveRightKeyCode); } }
        public static bool MoveRightKeyUp { get { return Input.GetKeyDown(MoveRightKeyCode); } }
          
        public static float MoveAxisX
        {
            get
            {
                return Mathf.Clamp(((MoveRightKey ? 1 : 0) - (MoveLeftKey ? 1 : 0)) + Input.GetAxis("Left Joystick Hor"), -1, 1); 
            }
        }
        public static float MoveAxisY
        {
            get
            {
                return Mathf.Clamp(((MoveForwardKey ? 1 : 0) - (MoveBackwardKey ? 1 : 0)) - Input.GetAxis("Left Joystick Ver"), -1, 1);
            }
        }

        private static KeyCode JumpKeyCode = KeyCode.Space;
        public static bool JumpKey { get { return Input.GetKey(JumpKeyCode); } }
        public static bool JumpKeyDown { get { return Input.GetKeyUp(JumpKeyCode); } }
        public static bool JumpKeyUp { get { return Input.GetKeyDown(JumpKeyCode); } }
         
        public static bool JumpButton => JumpKey || Input.GetButton("Jump");
        public static bool JumpButtonDown => JumpKeyDown || Input.GetButtonDown("Jump");
        public static bool JumpButtonUp => JumpKeyUp || Input.GetButtonUp("Jump");

        #endregion

        public static float Scroll { get { return Input.mouseScrollDelta.y * ScrollSpeed; } }
        public static bool ScrollUp { get { return Scroll > 0; } }
        public static bool ScrollDown { get { return Scroll < 0; } }

        public static bool Panning { get { return Input.GetKey(KeyCode.Space); } }

        public static int CursorPrimaryButtonID = 0;
        public static bool CursorPrimaryButton { get { return Input.GetMouseButton(CursorPrimaryButtonID); } }
        public static bool CursorPrimaryButtonUp { get { return Input.GetMouseButtonUp(CursorPrimaryButtonID); } }
        public static bool CursorPrimaryButtonDown { get { return Input.GetMouseButtonDown(CursorPrimaryButtonID); } }

        public static int CursorSecondaryButtonID = 1;
        public static bool CursorSecondaryButton { get { return Input.GetMouseButton(CursorSecondaryButtonID); } }
        public static bool CursorSecondaryButtonUp { get { return Input.GetMouseButtonUp(CursorSecondaryButtonID); } }
        public static bool CursorSecondaryButtonDown { get { return Input.GetMouseButtonDown(CursorSecondaryButtonID); } }

        public static int CursorAuxiliaryButtonID = 2;
        public static bool CursorAuxiliaryButton { get { return Input.GetMouseButton(CursorAuxiliaryButtonID); } }
        public static bool CursorAuxiliaryButtonUp { get { return Input.GetMouseButtonUp(CursorAuxiliaryButtonID); } }
        public static bool CursorAuxiliaryButtonDown { get { return Input.GetMouseButtonDown(CursorAuxiliaryButtonID); } }

        public static EngineInternal.Vector2 CursorScreenPosition { get { return UnityEngineHook.AsSwoleVector(Input.mousePosition); } set { /* Currently unsupported */ } }
        public static EngineInternal.Vector3 CursorWorldPositionMainCameraNCP => GetCursorWorldPositionFromCamera(Camera.main);
        public static EngineInternal.Vector3 GetCursorWorldPositionFromCamera(Camera camera) => UnityEngineHook.AsSwoleVector(Utils.MousePositionWorld(camera)); 

        public static float CursorAxisX
        {
            get
            {
                return Input.GetAxis("Mouse X");
            }
        }

        public static float CursorAxisY
        {
            get
            {
                return Input.GetAxis("Mouse Y");
            }
        }

        public static CursorLockMode CursorLockState
        {
            get => Cursor.lockState;

            [Swole.Script.SwoleScriptIgnore]
            set => Cursor.lockState = value;
        }
         
        public static bool IsCursorVisible
        {
            get => Cursor.visible;

            [Swole.Script.SwoleScriptIgnore]
            set => Cursor.visible = value;
        }

        private static EngineInternal.Vector2 lockedCursorPosition;

        private static CursorLockMode prevLockState;

        [Swole.Script.SwoleScriptIgnore]
        public static void LockAndHideCursor()
        {
            LockAndHideCursor(CursorScreenPosition);
        }

        [Swole.Script.SwoleScriptIgnore]
        public static void LockAndHideCursor(EngineInternal.Vector3 cursorPosition)
        {

            lockedCursorPosition = cursorPosition;

            prevLockState = Cursor.lockState;

            Cursor.lockState = CursorLockMode.Locked;

            Cursor.visible = false;

        }

        [Swole.Script.SwoleScriptIgnore]
        public static void UnlockAndShowCursor()
        {

            Cursor.lockState = prevLockState;

            CursorScreenPosition = lockedCursorPosition;

            Cursor.visible = true;

        }

        public static float DoubleClickSpeed => 0.44f;

        public static float ScrollSpeed { get { return 0.3f; } }

        public static float PanningSpeed { get { return 0.05f; } }

    }

    public class InputManager : IInputManager
    {

        public bool IsStatic => true;

        public PropertyInfo[] InputProperties => inputProperties;
        public FieldInfo[] InputFields => inputFields;
        public MethodInfo[] InputMethods => inputMethods;

        public PropertyInfo[] inputProperties;
        public FieldInfo[] inputFields;
        public MethodInfo[] inputMethods;

        public InputManager(PropertyInfo[] inputProperties, FieldInfo[] inputFields, MethodInfo[] inputMethods)
        {
            this.inputProperties = inputProperties;
            this.inputFields = inputFields;
            this.inputMethods = inputMethods;
        }
    }

}

#endif
