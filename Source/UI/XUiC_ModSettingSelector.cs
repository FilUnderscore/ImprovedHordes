﻿using UnityEngine;

namespace CustomModManager.UI
{
    public class XUiC_ModSettingSelector : XUiController
    {
        private XUiC_ComboBoxList<ModOptionValue> controlCombo;
        private XUiC_TextInput controlText;

        public ModManagerModSettings.BaseModSetting modSetting;

        public XUiC_ModSettingSelector() { }

        public override void Init()
        {
            base.Init();

            this.controlCombo = this.GetChildById("ControlCombo").GetChildByType<XUiC_ComboBoxList<ModOptionValue>>();
            this.controlCombo.OnValueChanged += ControlCombo_OnValueChanged;
            this.controlText = this.GetChildById("ControlText").GetChildByType<XUiC_TextInput>();
            this.controlText.OnChangeHandler += ControlText_OnChangeHandler;
        }

        public void UpdateModSetting(string key, ModManagerModSettings.BaseModSetting modSetting)
        {
            this.modSetting = modSetting;

            if (this.modSetting != null)
            {
                if(!this.IsTextInput())
                    this.SetupOptions();
                else
                    this.controlText.Text = this.modSetting.GetValueAsString();

                this.RefreshBindings(true);
                this.controlCombo.ViewComponent.IsVisible = !this.IsTextInput();
                this.controlText.ViewComponent.IsVisible = this.IsTextInput();
            }
        }

        private void SetupOptions()
        {
            this.controlCombo.Elements.Clear();
            
            string[] allowedValues = this.modSetting.GetAllowedValuesAsStrings();
            bool detectedSetting = false;
            for (int index = 0; index < allowedValues.Length; index++)
            {
                this.controlCombo.Elements.Add(new ModOptionValue(allowedValues[index]));

                if(allowedValues[index] == this.modSetting.GetValueAsString())
                {
                    this.controlCombo.SelectedIndex = index;
                    detectedSetting = true;
                }
            }

            if (!detectedSetting)
                this.controlCombo.SelectedIndex = this.controlCombo.MinIndex;
        }

        private bool IsTextInput()
        {
            return this.modSetting != null ? this.modSetting.GetAllowedValuesAsStrings() == null : true;
        }

        public override bool GetBindingValue(ref string _value, string _bindingName)
        {
            switch(_bindingName)
            {
                case "title":
                    _value = modSetting != null ? modSetting.unlocalizedName : "";
                    return true;
                default:
                    return false;
            }
        }

        private void ControlText_OnChangeHandler(XUiController _sender, string _text, bool _changeFromCode)
        {
            if (!this.IsTextInput())
                return;

            this.controlText.ActiveTextColor = this.modSetting.SetValueFromString(_text) ? Color.white : Color.red;
        }

        private void ControlCombo_OnValueChanged(XUiController _sender, ModOptionValue _oldValue, ModOptionValue _newValue)
        {
            this.modSetting.SetValueFromString(_newValue.Value);
        }

        public struct ModOptionValue
        {
            public readonly string Value;

            public ModOptionValue(string value)
            {
                this.Value = value;
            }

            public override string ToString()
            {
                return this.Value;
            }
        }
    }
}