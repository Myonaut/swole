using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.UI
{
    public static class UIInput
    {
        private static DefaultUIInputHandler defaultHandler = new DefaultUIInputHandler();
        private static IUIInputHandler handler = null;
        public static void SetHandler(IUIInputHandler uiHandler)
        {
            handler = uiHandler;
        }
        public static void ResetHandler() => handler = null;

        public static IUIInputHandler Handler => handler == null ? defaultHandler : handler;

        public static float MainLeftJoystickHorizontal => Handler.MainLeftJoystickHorizontal;
        public static float MainLeftJoystickVertical => Handler.MainLeftJoystickVertical; 

        public static float MainRightJoystickHorizontal => Handler.MainRightJoystickHorizontal;
        public static float MainRightJoystickVertical => Handler.MainRightJoystickVertical; 
    }

    public class DefaultUIInputHandler : IUIInputHandler
    {
        public float MainLeftJoystickHorizontal => InputProxy.MainLeftJoystickHorizontal;
        public float MainLeftJoystickVertical => InputProxy.MainLeftJoystickVertical;

        public float MainRightJoystickHorizontal => InputProxy.MainRightJoystickHorizontal;
        public float MainRightJoystickVertical => InputProxy.MainRightJoystickVertical;
    }

    public interface IUIInputHandler
    {
        public float MainLeftJoystickHorizontal { get; }
        public float MainLeftJoystickVertical { get; }

        public float MainRightJoystickHorizontal { get; }
        public float MainRightJoystickVertical { get; }
    }

}
