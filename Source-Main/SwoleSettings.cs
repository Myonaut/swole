using System;

namespace Swole
{
    [Serializable]
    public struct SwoleSettings
    {
        public static SwoleSettings Default => new SwoleSettings()
        {
            weightAsKilograms = false,

            disableAllCharacterPhysics = false,

            disableGamepadRumble = false,
            gamepadRumbleScale = 1,

            gamepadLeftStickSensitivityBase = 1,
            gamepadLeftStickSensitivityX = 1,
            gamepadLeftStickSensitivityY = 1,

            gamepadRightStickSensitivityBase = 1,
            gamepadRightStickSensitivityX = 1,
            gamepadRightStickSensitivityY = 1,

            gamepadLeftStickDeadzoneX = 0.11f,
            gamepadLeftStickDeadzoneY = 0.11f,
            gamepadRightStickDeadzoneX = 0.11f,
            gamepadRightStickDeadzoneY = 0.11f, 

            mouseSensitivityBase = 1,
            mouseSensitivityX = 1,
            mouseSensitivityY = 1,

            mouseSensitivityBase2 = 1,
            mouseSensitivityX2 = 1,
            mouseSensitivityY2 = 1,

        };

        public bool weightAsKilograms;

        public bool disableAllCharacterPhysics;

        public bool disableGamepadRumble; 
        public float gamepadRumbleScale;

        public float gamepadLeftStickSensitivityBase;
        public float gamepadLeftStickSensitivityX;
        public float gamepadLeftStickSensitivityY;

        public float gamepadLeftStickDeadzoneX;
        public float gamepadLeftStickDeadzoneY;

        public float gamepadRightStickSensitivityBase;
        public float gamepadRightStickSensitivityX;
        public float gamepadRightStickSensitivityY;

        public float gamepadRightStickDeadzoneX;
        public float gamepadRightStickDeadzoneY;

        public float mouseSensitivityBase;
        public float mouseSensitivityX;
        public float mouseSensitivityY;

        public float mouseSensitivityBase2;
        public float mouseSensitivityX2;
        public float mouseSensitivityY2;
    }
}
