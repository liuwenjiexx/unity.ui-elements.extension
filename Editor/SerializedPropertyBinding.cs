using System.Collections;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Unity;
using Bindings;

namespace UnityEditor.UIElements.Extension
{
    public class SerializedPropertyBinding<T> : IBinding
    {
        public SerializedProperty property;
        public IAccessor<T> accessor;
        public INotifyValueChanged<T> owner;
        public SerializedPropertyBinding(INotifyValueChanged<T> owner, SerializedProperty property)
        {
            this.property = property;
            accessor = new SerializedPropertyAccessor<T>();
            this.owner = owner;
            owner.RegisterValueChangedCallback(OnChangeEvent);
            
        }

        void OnChangeEvent(ChangeEvent<T> e)
        {
            accessor.SetValue(property, e.newValue);
            EditorUtility.SetDirty(property.serializedObject.targetObject);
            var view = (VisualElement)owner;
            PropertyField propField = null;
            if (view is PropertyField)
            {
                view= (PropertyField)view;
            }else
            {
                propField= view.parent as PropertyField;
            }

            if (propField != null)
            {
                using (var changeEvent = SerializedPropertyChangeEvent.GetPooled(property))
                { 
                    changeEvent.target = propField;
                    view.SendEvent(changeEvent);
                }
            }
        }

        public void PreUpdate()
        {
        }
        public void Update()
        {
            var value = accessor.GetValue(property);
            if (!object.Equals(value, owner.value))
            {
                owner.SetValueWithoutNotify(value);
            }
        }

        public void Release()
        {
            owner.UnregisterValueChangedCallback(OnChangeEvent);
        }

    }
}
