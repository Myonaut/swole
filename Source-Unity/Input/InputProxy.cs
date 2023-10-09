#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Swole
{

    public static class InputProxy
    {

#if ENABLE_INPUT_SYSTEM // TODO: Add support for new unity input system
#endif

        #region Inventory

        public static KeyCode CloseOrQuitKeyCode = KeyCode.Escape;
        public static bool CloseOrQuitKey { get { return Input.GetKey(ItemCombineKeyCode); } }
        public static bool CloseOrQuitKeyDown { get { return Input.GetKeyUp(ItemCombineKeyCode); } }
        public static bool CloseOrQuitKeyUp { get { return Input.GetKeyDown(ItemCombineKeyCode); } }

        public static KeyCode ItemCombineKeyCode = KeyCode.LeftShift;
        public static bool ItemCombineKey { get { return Input.GetKey(ItemCombineKeyCode); } }
        public static bool ItemCombineKeyDown { get { return Input.GetKeyUp(ItemCombineKeyCode); } }
        public static bool ItemCombineKeyUp { get { return Input.GetKeyDown(ItemCombineKeyCode); } }

        public static KeyCode ItemAlternateKeyCode = KeyCode.LeftControl;
        public static bool ItemAlternateKey { get { return Input.GetKey(ItemAlternateKeyCode); } }
        public static bool ItemAlternateKeyDown { get { return Input.GetKeyUp(ItemAlternateKeyCode); } }
        public static bool ItemAlternateKeyUp { get { return Input.GetKeyDown(ItemAlternateKeyCode); } }

        public static KeyCode ItemTransferKeyCode = KeyCode.T;
        public static bool ItemTransferKey { get { return Input.GetKey(ItemTransferKeyCode); } }
        public static bool ItemTransferKeyDown { get { return Input.GetKeyUp(ItemTransferKeyCode); } }
        public static bool ItemTransferKeyUp { get { return Input.GetKeyDown(ItemTransferKeyCode); } }

        public static KeyCode ItemRotateKeyCode = KeyCode.R;
        public static bool ItemRotateKey { get { return Input.GetKey(ItemRotateKeyCode); } }
        public static bool ItemRotateKeyUp { get { return Input.GetKeyUp(ItemRotateKeyCode); } }
        public static bool ItemRotateKeyDown { get { return Input.GetKeyDown(ItemRotateKeyCode); } }

        public static KeyCode InventoryKeyCode = KeyCode.I;
        public static bool InventoryKey { get { return Input.GetKey(InventoryKeyCode); } }
        public static bool InventoryKeyUp { get { return Input.GetKeyUp(InventoryKeyCode); } }
        public static bool InventoryKeyDown { get { return Input.GetKeyDown(InventoryKeyCode); } }

        public static KeyCode EquipmentKeyCode = KeyCode.E;
        public static bool EquipmentKey { get { return Input.GetKey(EquipmentKeyCode); } }
        public static bool EquipmentKeyUp { get { return Input.GetKeyUp(EquipmentKeyCode); } }
        public static bool EquipmentKeyDown { get { return Input.GetKeyDown(EquipmentKeyCode); } }

        #endregion


        #region Modding

        public static KeyCode Modding_PrimeActionKeyCode = KeyCode.LeftControl;
        public static bool Modding_PrimeActionKey { get { return Input.GetKey(Modding_PrimeActionKeyCode); } }
        public static bool Modding_PrimeActionKeyUp { get { return Input.GetKeyUp(Modding_PrimeActionKeyCode); } }
        public static bool Modding_PrimeActionKeyDown { get { return Input.GetKeyDown(Modding_PrimeActionKeyCode); } }

        public static KeyCode Modding_ModifyActionKeyCodeA = KeyCode.LeftShift;
        public static KeyCode Modding_ModifyActionKeyCodeB = KeyCode.RightShift;
        public static bool Modding_ModifyActionKey { get { return Input.GetKey(Modding_ModifyActionKeyCodeA) || Input.GetKey(Modding_ModifyActionKeyCodeB); } }
        public static bool Modding_ModifyActionKeyUp { get { return Input.GetKeyUp(Modding_ModifyActionKeyCodeA) || Input.GetKeyUp(Modding_ModifyActionKeyCodeB); } }
        public static bool Modding_ModifyActionKeyDown { get { return Input.GetKeyDown(Modding_ModifyActionKeyCodeA) || Input.GetKeyDown(Modding_ModifyActionKeyCodeB); } }

        public static KeyCode Modding_UndoKeyCode = KeyCode.Z;
        public static bool Modding_UndoKey { get { return Modding_PrimeActionKey && Input.GetKey(Modding_UndoKeyCode); } }
        public static bool Modding_UndoKeyUp { get { return Input.GetKeyUp(Modding_UndoKeyCode); } }
        public static bool Modding_UndoKeyDown { get { return Modding_PrimeActionKey && Input.GetKeyDown(Modding_UndoKeyCode); } }

        public static bool Modding_RedoKey { get { return Modding_ModifyActionKey && Modding_UndoKey; } }
        public static bool Modding_RedoKeyUp { get { return Modding_UndoKeyUp; } }
        public static bool Modding_RedoKeyDown { get { return Modding_ModifyActionKey && Modding_UndoKeyDown; } }

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

        public static Vector2 CursorPosition { get { return Input.mousePosition; } set { /* Currently unsupported */ } }

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
            set => Cursor.lockState = value;

        }

        public static bool IsCursorVisible
        {

            get => Cursor.visible;
            set => Cursor.visible = value;

        }

        private static Vector2 lockedCursorPosition;

        private static CursorLockMode prevLockState;

        public static void LockAndHideCursor()
        {

            LockAndHideCursor(CursorPosition);

        }

        public static void LockAndHideCursor(Vector3 cursorPosition)
        {

            lockedCursorPosition = cursorPosition;

            prevLockState = Cursor.lockState;

            Cursor.lockState = CursorLockMode.Locked;

            Cursor.visible = false;

        }

        public static void UnlockAndShowCursor()
        {

            Cursor.lockState = prevLockState;

            CursorPosition = lockedCursorPosition;

            Cursor.visible = true;

        }

        public static float ScrollSpeed { get { return 0.3f; } }

        public static float PanningSpeed { get { return 0.05f; } }

    }

}

#endif
