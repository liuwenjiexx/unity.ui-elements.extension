using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UIElements;
using Bindings;
using Unity;

namespace UnityEditor.UIElements.Extension
{
    public class TargetFieldBinding<T> : IBinding
    {
        public FieldInfo fieldInfo;
        public IAccessor accessor;
        public INotifyValueChanged<T> owner;
        private IValueConverter converter;
        private object target;

        public TargetFieldBinding(INotifyValueChanged<T> owner, object target, FieldInfo fieldInfo)
        {
            this.target = target;
            this.fieldInfo = fieldInfo;
            accessor = Accessor.Member(fieldInfo);
            this.owner = owner;
            owner.RegisterValueChangedCallback(OnChangeEvent);
        }

        public IValueConverter Converter { get => converter; set => converter = value; }


        void OnChangeEvent(ChangeEvent<T> e)
        {
            object newValue = e.newValue;

            if (converter != null)
            {
                newValue = converter.ConvertBack(newValue, fieldInfo.FieldType, null);
            }
            accessor.SetValue(target, newValue);
        }

        public void PreUpdate()
        {
        }
        public void Update()
        {
            var value = accessor.GetValue(target);
            if (converter != null)
            {
                value = converter.Convert(value, typeof(T), null);
            }
            if (!object.Equals(value, owner.value))
            {
                owner.SetValueWithoutNotify((T)value);
            }
        }

        public void Release()
        {
            owner.UnregisterValueChangedCallback(OnChangeEvent);
        }
    }
}
