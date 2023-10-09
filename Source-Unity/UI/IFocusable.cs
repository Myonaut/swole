#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.UI
{

    public interface IFocusable
    {

        public bool IsInFocus { get; }

        void AddFocus();

        void SubtractFocus();

    }

}

#endif
