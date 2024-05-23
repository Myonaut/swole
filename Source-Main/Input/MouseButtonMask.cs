using System;

namespace Swole
{

    [Serializable, Flags]
    public enum MouseButtonMask
    {

        None = 0, LeftMouseButton = 1, RightMouseButton = 2, MiddleMouseButton = 4, All = LeftMouseButton | RightMouseButton | MiddleMouseButton

    }

}
