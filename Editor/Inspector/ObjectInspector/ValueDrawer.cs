using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine.UIElements;
using Bindings;
using Unity;

namespace UnityEditor.UIElements.Extension
{

    public abstract class ValueDrawer
    {

        internal object target;
        public int Depth { get; internal set; }
        public ObjectInspector Inspector { get; internal set; }
        public string DisplayName { get; set; }
        public BindingSet<object> BindingSet { get; internal set; }

        public string PropertyPath { get; set; }

        public string MemberName => Member.Name;
        public Type ValueType { get; internal set; }

        public MemberInfo Member { get; internal set; }

        public List<BindingBase> Bindings { get; private set; } = new();

        internal object value;
        internal bool changed;
        internal bool enabled;

        public object Value
        {
            get => this.value;// member.accessor.GetValue(target);
            set
            {
                var prevValue = this.Value;
                if (!Equals(prevValue, value))
                {
                    //member.accessor.SetValue(target, value);
                    this.value = value;
                    changed = true;
                    Changed?.Invoke(this);
                    OnValueChanged(prevValue);
                }
            }
        }

        public event Action<ValueDrawer> Changed;

        public virtual void OnEnable()
        {
 
        }

        public virtual void OnDisable()
        {
            BindingSet.Unbind();
        }

         

        public virtual void CreateDrawer()
        {
        }

        protected virtual void OnValueChanged(object prevValue)
        {

        }



        public abstract VisualElement CreateUI();

        public virtual void UpdateSourceToTarget()
        {

        }


        static Dictionary<object, DrawerType> drawerTypes;

        struct DrawerType
        {
            public Type type;
            public bool editorForChildClasses;

            public DrawerType(Type type, bool editorForChildClasses)
            {
                this.type = type;
                this.editorForChildClasses = editorForChildClasses;
            }
        }

        public static Type GetDrawerType(object key)
        {
            if (drawerTypes == null)
            {
                drawerTypes = new();

                foreach (var type in TypeCache.GetTypesWithAttribute<MemberDrawerAttribute>())
                {
                    if (type.IsAbstract) continue;
                    if (!type.IsSubclassOf(typeof(ValueDrawer))) continue;

                    var attr = type.GetCustomAttribute<MemberDrawerAttribute>();
                    if (attr.ValueType == null) continue;

                    drawerTypes[attr.ValueType] = new DrawerType(type, attr.EditorForChildClasses);
                }
            }

            if (drawerTypes.TryGetValue(key, out var drawerType))
            {
                return drawerType.type;
            }

            Type targetType = key as Type;

            if (targetType != null)
            {
                targetType = targetType.BaseType;
                while (targetType != null)
                {
                    if (drawerTypes.TryGetValue(targetType, out drawerType) && drawerType.editorForChildClasses)
                    {
                        return drawerType.type;
                    }
                    targetType = targetType.BaseType;
                }
            }

            return null;
        }

        public virtual void Clear()
        {
            foreach (var binding in Bindings)
            {
                binding.Unbind();
                BindingSet.Bindings.Remove(binding);
            }
            Bindings.Clear();
        }

        public override string ToString()
        {
            return $"{PropertyPath}";
        }
    }


}
