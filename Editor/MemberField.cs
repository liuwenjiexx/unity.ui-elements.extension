using PlasticGui;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Extension;
namespace UnityEditor.UIElements.Extension
{
    public class MemberField
    {
        public InputView InputView { get; private set; }
        public VisualElement View;
        public MemberInfo Member { get; internal set; }


        public string DisplayName { get; internal set; }

        public string Tooltip { get; internal set; }

        public Type ValueType { get; internal set; }
         
        public object Target { get; set; }

        Label label;
        private Action updateLabel;

        public VisualElement CreateView()
        {
            Type viewType = null;

            if (viewType == null)
            {
                viewType = EditorUIElementsUtility.GetInputViewType(ValueType);
            }

            View = new VisualElement();
            //View.AddToClassList(SettingField_ClassName);

            VisualElement inputViewElem = null;
            if (viewType != null)
            {
                var inputView = Activator.CreateInstance(viewType) as InputView;
                inputView.DisplayName = DisplayName;
                inputView.ValueType = ValueType;
                this.InputView = inputView;
                InputView.DisplayName = null;
                View.AddToClassList("unity-base-field");
                label = new Label();
                label.AddToClassList("unity-base-field__label");
                label.text = DisplayName;
                //label.AddToClassList(SettingLabel_ClassName);

                View.Add(label);
                //VisualElement fieldContainer2=new VisualElement();
                //View.Add(fieldContainer2);
                inputViewElem = InputView.CreateView();
                inputViewElem.style.flexGrow = 1f;

            }
            /*  else if (ValueType.IsArray || typeof(IList).IsAssignableFrom(ValueType))
              {
                  var inputView = ArrayView.CreateFromSetting(Setting, Metadata?.ElementAttribute);
                  inputView.DisplayName = DisplayName;
                  InputView = inputView;
                  this.InputView = inputView;
                  inputViewElem = InputView.CreateView();
                  label = inputViewElem.Q<Label>(className: ArrayView.SettingsArrayPanel_Label_ClassName);
              }*/

            if (InputView == null)
            {
                return View;
            }

            View.Add(inputViewElem);
            inputViewElem.tooltip = Tooltip;

            InputView.ValueChanged += OnValueChanged;


            if (label != null)
            {

            }


            Refresh();
            return View;
        }

        bool IsBoldLabel()
        {
            bool b=false;
            b = InputView.IsBoldLabel(b);
            return b;
        }

        private void SettingsUtility_VariantChanged()
        {
            Refresh();
        }

        private void OnValueChanged(object newValue)
        {
            if (Member is FieldInfo field)
            {
                field.SetValue(Target, newValue);
            }
            else if (Member is PropertyInfo property)
            {
                property.SetValue(Target, newValue);
            }
            Refresh();
        }

        private object GetOrDefaultValue()
        {
            object value = null;
            if (Member is FieldInfo field)
            {
                value = field.GetValue(Target);
            }
            else if (Member is PropertyInfo property)
            {
                value = property.GetValue(Target);
            }
            return value;
        }

        private void Refresh()
        {
            object value = GetOrDefaultValue();
            InputView.SetValue(value);

            if (label != null)
            {
                //EditorSettingsUtility.UpdateSettingFieldLabel(label, IsBoldLabel());
            }

        }

    }
}
