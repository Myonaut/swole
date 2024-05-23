#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

using TMPro;

namespace Swole
{

    public class VersionFetcher : MonoBehaviour
    {

        private string versionString;

        public string versionKeyword = "$version";
        public string outputString;

        public UnityEvent OnAwake = new UnityEvent();

        protected void Awake()
        {

            versionString = Application.version;

            if (versionString.EndsWith(".0"))
            {

                int dotCount = 0;
                for (int a = 0; a < versionString.Length; a++) if (versionString[a] == '.') dotCount++;

                if (dotCount > 2) versionString = versionString.Substring(0, versionString.LastIndexOf('.'));

            }

            outputString = versionString;

            OnAwake?.Invoke();

        }

        public void SetOutput(string inputStr)
        {

            outputString = inputStr.Replace(versionKeyword, versionString);

        }

        public void SetText(Text textComp)
        {

            textComp.text = outputString;

        }

        public void SetText(TMP_Text textComp)
        {

            textComp.text = outputString;

        }

    }

}

#endif