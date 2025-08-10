#if (UNITY_STANDALONE || UNITY_EDITOR)

using UnityEngine;
using UnityEngine.UI;

using TMPro;

namespace Swole.UI
{

    public class UITooltip : MonoBehaviour 
    {

        protected RectTransform rectTransform;
        public RectTransform RectTransform => rectTransform ?? (rectTransform = gameObject.AddOrGetComponent<RectTransform>());

        protected Canvas canvas;
        public Canvas Canvas => canvas ?? (canvas = gameObject.GetComponentInParent<Canvas>());

        protected RectTransform canvasRectTransform;
        public RectTransform CanvasRectTransform => canvasRectTransform ?? (canvasRectTransform = Canvas.gameObject.AddOrGetComponent<RectTransform>());

        public Text title;
        public TMP_Text tmpTitle;

        public void SetTitle(string text)
        {
            if (title != null) title.text = text;
            if (tmpTitle != null) tmpTitle.SetText(text); 
        }

        public Text message;
        public TMP_Text tmpMessage;

        public void SetMessage(string text)
        {
            if (message != null) message.text = text;
            if (tmpMessage != null) tmpMessage.SetText(text); 
        }

        public void RefetchCanvas()
        {
            canvas = gameObject.GetComponentInParent<Canvas>();

            canvasRectTransform = canvas.gameObject.GetComponent<RectTransform>();
        }

        public Vector2 cursorAnchorPoint;

        protected void Update()
        {
            Vector3 size = RectTransform.rect.size;

            Vector3 canvasSpacePosition = Canvas.ScreenToCanvasSpace(CursorProxy.ScreenPosition);

            canvasSpacePosition = canvasSpacePosition + new Vector3(cursorAnchorPoint.x * size.x, cursorAnchorPoint.y * size.y, 0);

            RectTransform.position = CanvasRectTransform.TransformPoint(canvasSpacePosition);

            rectTransform.ConstrainInsideRectTransform(canvasRectTransform);
        }

        protected void OnEnable()
        {
            Update();
        }

    }

}

#endif
