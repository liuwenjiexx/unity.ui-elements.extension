using System;
using UnityEditor;
using Unity;
using Bindings;

namespace UnityEditor.UIElements.Extension
{
    class SerializedPropertyAccessor<TValue> : IAccessor<TValue>
    {
        public Type ValueType => typeof(TValue);

        Type IAccessor.ValueType => throw new NotImplementedException();

        public bool CanGetValue(object target)
        {
            return true;
        }

        public bool CanSetValue(object target)
        {
            return true;
        }

        public TValue GetValue(object target)
        {
            var property = (SerializedProperty)target;
            var value = property.GetObjectOfProperty();
            return (TValue)value;
        }

        public object SetValue(object target, TValue value)
        {
            var property = (SerializedProperty)target;
            property.SetObjectOfProperty(value);
            return value;
        }

        public object SetValue(object target, object value)
        {
            return SetValue(target, (TValue)value);
        }

        object IAccessor.GetValue(object target)
        {
            var property = (SerializedProperty)target;
            var value = property.GetObjectOfProperty();
            return value;
        }
    }

}
