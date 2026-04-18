using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Bindings;
using Unity.Editor;
using Unity;

namespace UnityEditor.UIElements.Extension
{

    public class ObjectInspector
    {
        List<ValueDrawer> drawers;
        private BindingSet<object> bindingSet;


        public ObjectInspector(Type targetType, object target)
        {
            if (targetType == null) throw new ArgumentNullException(nameof(targetType));
            this.TargetType = targetType;
            this.Target = target;
        }

        public Type TargetType { get; private set; }
        public object Target { get; private set; }

        public Func<Type, MemberInfo[]> ResolveMember { get; set; }

        public bool Diried { get; set; }

        public void OnEnable()
        {
            drawers = new List<ValueDrawer>();
            if (bindingSet == null)
            {
                bindingSet = new BindingSet<object>(Target);
                bindingSet.SourcePropertyChanged += BindingSet_SourcePropertyChanged;
            }

            foreach (var drawer in CreateDrawer(Target, null, null))
            {
                drawers.Add(drawer);
            }
        }

        private void BindingSet_SourcePropertyChanged(object sender, BindingPropertyChangedEventArgs e)
        {
            //Debug.Log("change: " + e.PropertyPath);
            Diried = true;
        }

        public IEnumerable<ValueDrawer> CreateDrawer(object target, ValueDrawer parent, string parentPropetyPath)
        {
            if (target == null) yield break;

            MemberInfo[] members = null;
            Type targetType = target.GetType();
            if (ResolveMember != null)
            {
                members = ResolveMember(targetType);
            }

            if (members == null)
            {
                members = EditableMember.GetMembers(targetType)
                    .Select(o => o.Member)
                    .Where(o => o != null)
                    .ToArray();
            }

            foreach (var member in members)
            {
                object value;
                Type valueType = null;
                var fInfo = member as FieldInfo;
                var pInfo = member as PropertyInfo;
                if (fInfo != null)
                {
                    value = fInfo.GetValue(target);
                    if (value == null)
                    {
                        value = fInfo.FieldType.CreateInstance();
                        fInfo.SetValue(target, value);
                        Diried = true;
                    }
                    valueType = value.GetType();
                }
                else
                {
                    value = pInfo.GetValue(target, null);
                    if (value == null)
                    {
                        value = pInfo.PropertyType.CreateInstance();
                        pInfo.SetValue(target, value);
                        Diried = true;
                    }
                    valueType = value.GetType();
                }

                string propertyPath;
                if (parentPropetyPath == null)
                    propertyPath = member.Name;
                else
                    propertyPath = parentPropetyPath + "." + member.Name;

                ValueDrawer drawer;
                drawer = CreateValueDrawer(member.Name, value, valueType, parent, propertyPath);
                drawer.Member = member;
                drawer.CreateDrawer();
                yield return drawer;
            }

        }

        public ValueDrawer CreateValueDrawer(string name, object value, Type valueType, ValueDrawer parent, string propertyPath)
        {
            Type drawerType;
            if (valueType.IsArray || valueType.FindByGenericTypeDefinition(typeof(IList<>)) != null)
            {
                drawerType = ValueDrawer.GetDrawerType(typeof(Array));
            }
            else
            {
                drawerType = ValueDrawer.GetDrawerType(valueType);
            }

            ValueDrawer drawer;

            if (drawerType == null)
            {
                drawer = new ObjectDrawer();
            }
            else
            {
                drawer = Activator.CreateInstance(drawerType) as ValueDrawer;
            }
            if (parent != null)
            {
                drawer.Depth = parent.Depth + 1;
            }
            else
            {
                drawer.Depth = 0;
            }
            drawer.Inspector = this;
            drawer.DisplayName = name;
            drawer.ValueType = valueType;
            drawer.PropertyPath = propertyPath;
            drawer.Value = value;
            drawer.target = Target;
            drawer.BindingSet = bindingSet;
            return drawer;
        }


        public void OnDisable()
        {
            if (bindingSet != null)
            {
                bindingSet.Unbind();
                bindingSet = null;
            }

            foreach (var drawer in drawers)
            { 
                if (drawer.enabled)
                {
                    drawer.enabled = false;
                    drawer.OnDisable();
                }
            }
            drawers.Clear();
        }

        public void UpdateIndent(ValueDrawer drawer, VisualElement view)
        {
            var label = view.Q(className: "unity-base-field__label");
            if (label != null)
            {
                label.style.paddingLeft = drawer.Depth * 16;
            }
        }

        public VisualElement CreateUI()
        {
            VisualElement container = new VisualElement();


            foreach (var drawer in drawers)
            {
                if (!drawer.enabled)
                {
                    //drawer.value = drawer.member.accessor.GetValue(Target);
                    drawer.enabled = true;
                    drawer.OnEnable();
                }

                var view = drawer.CreateUI();
                if (view != null)
                {
                    UpdateIndent(drawer, view);
                    container.Add(view);
                }
      
                //drawer.UpdateSourceToTarget();
            }

            bindingSet.Bind();
            //bindingSet.UpdateSourceToTarget();

            return container;
        }

        string GetDisplayTypeName(Type type)
        {
            var itemType = GetItemType(type);
            if (itemType != null)
            {
                return GetDisplayTypeName(itemType) + "[]";
            }
            return type.Name;
        }

        Type GetItemType(Type type)
        {
            Type itemType = null;
            if (type.IsArray)
            {
                itemType = type.GetElementType();
            }
            else
            {
                var listType = type.FindByGenericTypeDefinition(typeof(IList<>));
                if (listType != null)
                {
                    itemType = listType.GetGenericArguments()[0];
                }
            }

            return itemType;
        }

    }


}
