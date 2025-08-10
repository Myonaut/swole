#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

namespace Swole.UI
{
    public class UISliderTextSync : MonoBehaviour
    {

        public string numberStringFormat;

        [SerializeField]
        protected Slider slider;
        public void SetSlider(Slider slider)
        {
            if (this.slider != null && this.slider.onValueChanged != null)
            {
                this.slider.onValueChanged.RemoveListener(UpdateText);
            }

            this.slider = slider;
            if (this.slider != null)
            {
                if (this.slider.onValueChanged == null) this.slider.onValueChanged = new Slider.SliderEvent();
                this.slider.onValueChanged.AddListener(UpdateText);
            }
        }

        [SerializeField]
        protected Text displayText;
        public void SetDisplayText(Text displayText)
        {
            this.displayText = displayText;
        }
        [SerializeField]
        protected InputField inputField;
        public void SetInputField(InputField inputField)
        {
            if (this.inputField != null && this.inputField.onEndEdit != null)
            {
                this.inputField.onEndEdit.RemoveListener(UpdateSlider);
            }

            this.inputField = inputField;
            if (this.inputField != null)
            {
                if (this.inputField.onEndEdit == null) this.inputField.onEndEdit = new InputField.EndEditEvent();
                this.inputField.onEndEdit.AddListener(UpdateSlider);
            }
        }

        [SerializeField]
        protected TMP_Text tmp_displayText;
        public void SetDisplayTextTMP(TMP_Text displayText)
        {
            this.tmp_displayText = displayText;
        }
        [SerializeField]
        protected TMP_InputField tmp_inputField;
        public void SetInputFieldTMP(TMP_InputField inputField)
        {
            if (this.tmp_inputField != null && this.tmp_inputField.onEndEdit != null)
            {
                this.tmp_inputField.onEndEdit.RemoveListener(UpdateSlider);
            }

            this.tmp_inputField = inputField;
            if (this.tmp_inputField != null)
            {
                if (this.tmp_inputField.onEndEdit == null) this.tmp_inputField.onEndEdit = new TMP_InputField.SubmitEvent();
                this.tmp_inputField.onEndEdit.AddListener(UpdateSlider);
            }
        }

        private float lastValue;
        protected virtual void UpdateSlider(string valueStr)
        {
            if (!float.TryParse(valueStr, out float value)) return;

            if (slider != null) 
            {
                value = Mathf.Clamp(value, slider.minValue, slider.maxValue);
                if (lastValue == value) return;

                slider.value = value;
            }

            lastValue = value;
        }
        protected virtual void UpdateText(float value)
        {
            if (slider != null)
            {
                value = Mathf.Clamp(value, slider.minValue, slider.maxValue);
            }

            if (lastValue == value) return;

            string valueText = string.IsNullOrWhiteSpace(numberStringFormat) ? value.ToString() : value.ToString(numberStringFormat);

            if (displayText != null) displayText.text = valueText;
            if (tmp_displayText != null) tmp_displayText.text = valueText;
            if (inputField != null) inputField.text = valueText;
            if (tmp_inputField != null) tmp_inputField.text = valueText;

            lastValue = value;
        }

        void Awake()
        {
            if (slider == null) slider = gameObject.GetComponent<Slider>();

            SetSlider(slider);
            SetDisplayText(displayText);
            SetInputField(inputField);
            SetDisplayTextTMP(tmp_displayText);
            SetInputFieldTMP(tmp_inputField);

            if (slider != null) UpdateText(slider.value);
        }
    }
}

#endif