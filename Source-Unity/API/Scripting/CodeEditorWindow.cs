#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

using Swole.UI;
using Swole.Script;

namespace Swole.API.Unity
{

    public class CodeEditorWindow : MonoBehaviour
    {

        public const string _defaultFallbackCodeEditorResourcePath = "Scripting/DefaultCodeEditor";

        protected UIResizeableWindow window;
        public UIResizeableWindow Window => window;

        public RectTransform containerOverride;

        public string codeEditorResourcePath = "";
        public string fallbackCodeEditorResourcePath = "";

        protected ICodeEditor editor;
        public ICodeEditor Editor
        {
            get
            {
                if (editor == null) Initialize();       
                return editor;
            }
        }
        protected virtual void Initialize()
        {
            if (string.IsNullOrEmpty(fallbackCodeEditorResourcePath)) fallbackCodeEditorResourcePath = _defaultFallbackCodeEditorResourcePath;
            if (window == null) window = GetComponentInParent<UIResizeableWindow>(true);

            RectTransform container = containerOverride;
            if (container == null && window != null) container = window.contentContainer;
            if (container == null || editor != null) return;  

            if (string.IsNullOrEmpty(codeEditorResourcePath)) codeEditorResourcePath = fallbackCodeEditorResourcePath;
            if (string.IsNullOrEmpty(codeEditorResourcePath)) return;

            var resource = Resources.Load(codeEditorResourcePath);
            if (resource == null)
            {
                if (!string.IsNullOrEmpty(fallbackCodeEditorResourcePath)) resource = Resources.Load(fallbackCodeEditorResourcePath); else return;
            }
            if (resource is GameObject editorResource)
            {

                var instance = Instantiate(editorResource);
                instance.transform.SetParent(container, false);
                var instanceRT = instance.GetComponent<RectTransform>();
                instanceRT.SetAnchor(AnchorPresets.StretchAll, 0, 0);

                editor = instance.GetComponentInChildren<ICodeEditor>();
                if (editor != null)
                {

                    if (window.rootWindowElement != null) editor.RootObject = window.rootWindowElement.gameObject;
                    if (window.titleText != null) editor.TitleObject = window.titleText;
                    if (window.titleTextTMP != null) editor.TitleObject = window.titleTextTMP;

                    var closeAction = new UnityAction(() =>
                    {

                        editor.SpoofClose();
                        if (window.rootWindowElement != null) window.rootWindowElement.gameObject.SetActive(false);

                    });
                    if (window.closingButton != null) window.closingButton.onClick.AddListener(closeAction);
                    if (window.closingButtonAlt != null) window.closingButtonAlt.OnClick.AddListener(closeAction);

                }

            }
        }
        protected virtual void Start()
        {
            Initialize();
        }

    }

}

#endif