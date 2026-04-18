using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Editor;
using System.ComponentModel;

namespace UnityEditor.UIElements.Extension
{
    public static class Extensions
    {
        #region UXML

        internal static string GetUSSPath(string uss, Type type = null)
        {
            string dir = (type ?? typeof(Extensions)).Assembly.GetUnityPackageDirectory();
            if (string.IsNullOrEmpty(dir))
                return null;
            return $"{dir}/Editor/USS/{uss}.uss";
        }

        internal static string GetUXMLPath(string uxml, Type type = null)
        {
            string dir = (type ?? typeof(Extensions)).Assembly.GetUnityPackageDirectory();
            return $"{dir}/Editor/UXML/{uxml}.uxml";
        }

        public static StyleSheet TryAddStyle(this VisualElement elem, Type type, string uss = null)
        {

            if (uss == null)
            {
                var uxmlAttr = type.GetCustomAttribute<UXMLAttribute>();
                if (uxmlAttr != null)
                {
                    if (!string.IsNullOrEmpty(uxmlAttr.USS))
                    {
                        uss = uxmlAttr.USS;
                    }
                    else if (!string.IsNullOrEmpty(uxmlAttr.UXML))
                    {
                        uss = uxmlAttr.UXML;
                    }
                }
            }

            if (uss == null)
            {
                uss = type.Name;
            }
            string assetPath = GetUSSPath(uss, type);
            var style = AssetDatabase.LoadAssetAtPath<StyleSheet>(assetPath);
            if (style)
            {
                elem.styleSheets.Add(style);
            }
            return style;
        }

        public static StyleSheet AddStyle(this VisualElement elem, Type type, string uss = null)
        {
            return TryAddStyle(elem, type, uss);
        }

        public static StyleSheet AddStyle(this VisualElement elem, string uss)
        {
            return AddStyle(elem, typeof(Extensions), uss);
        }

        public static TemplateContainer LoadUXML(this Type type, string uxml, VisualElement parent = null)
        {
            var uxmlAttr = type.GetCustomAttribute<UXMLAttribute>();

            if (uxml == null)
            {
                uxml = uxmlAttr?.UXML;
                if (string.IsNullOrEmpty(uxml))
                    uxml = type.Name;
            }
            string path = GetUXMLPath(uxml, type);
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            TemplateContainer treeRoot = null;
            if (asset)
            {
                treeRoot = asset.CloneTree();
                string uss = null;
                if (uxmlAttr != null)
                {
                    if (!string.IsNullOrEmpty(uxmlAttr.USS))
                        uss = uxmlAttr.USS;
                }

                if (!string.IsNullOrEmpty(uss))
                    treeRoot.AddStyle(type, uss);
                else
                    treeRoot.TryAddStyle(type, uxml);

                if (parent != null)
                {
                    parent.Add(treeRoot);
                }
            }
            return treeRoot;
        }
        internal static TemplateContainer LoadUXML(string uxml, VisualElement parent = null)
        {
            return LoadUXML(typeof(Extensions), uxml, parent);
        }
        public static TemplateContainer LoadUXML(this Type type, VisualElement parent = null)
        {
            return LoadUXML(type, null, parent);
        }
        public static TemplateContainer LoadUXML(this UnityEditor.SettingsProvider self, VisualElement rootElement)
        {
            return LoadUXML(self.GetType(), rootElement);
        }
        public static TemplateContainer LoadUXML(this EditorWindow self)
        {
            return LoadUXML(self.GetType(), self.rootVisualElement);
        }

        public static TemplateContainer LoadUXML(this UnityEditor.Editor self)
        {
            return LoadUXML(self.GetType());
        }

        static FieldInfo m_FormatSelectedValueCallback;
        static FieldInfo m_FormatListItemCallback;

        public static void SetFormatSelectedValueCallback(this DropdownField field, Func<string, string> format)
        {
            if (m_FormatSelectedValueCallback == null)
                m_FormatSelectedValueCallback = typeof(DropdownField).GetField("m_FormatSelectedValueCallback", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            m_FormatSelectedValueCallback?.SetValue(field, format);
        }

        public static void SetFormatListItemCallback(this DropdownField field, Func<string, string> format)
        {
            if (m_FormatListItemCallback == null)
                m_FormatListItemCallback = typeof(DropdownField).GetField("m_FormatListItemCallback", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            m_FormatListItemCallback?.SetValue(field, format);
        }

        public static void ShowEmptyLabel(this ListView listView, bool visiable)
        {
            var label = listView.Q(className: "unity-list-view__empty-label");
            if (label != null)
            {
                if (visiable)
                {
                    label.style.display = DisplayStyle.Flex;
                }
                else
                {
                    label.style.display = DisplayStyle.None;
                }
            }
        }
        #endregion

        #region None Generic INotifyValueChanged

        static MethodInfo NotifyValueChanged_GetValueMethod;
        static MethodInfo NotifyValueChanged_SetValueMethod;
        static MethodInfo NotifyValueChanged_SetValueWithoutNotifyMethod;
        static MethodInfo NotifyValueChanged_RegisterValueChangeCallbackMethod;
        static MethodInfo RegisterChangeCallbackMethod;

        static bool InvokeNotifyValueChangedMethod(ref MethodInfo method, string methodName, object target, object[] args, out object returnValue)
        {
            if (method == null)
            {
                method = typeof(Extensions).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
            }
            returnValue = null;
            Type valueType;
            var notifyType = target.GetType().FindByGenericTypeDefinition(typeof(INotifyValueChanged<>));
            if (notifyType == null)
                return false;
            valueType = notifyType.GetGenericArguments()[0];

            returnValue = method.MakeGenericMethod(valueType).Invoke(target, args);
            return true;
        }

        public static object GetValue(this VisualElement target)
        {
            if (InvokeNotifyValueChangedMethod(ref NotifyValueChanged_GetValueMethod, nameof(_GetValue), target, new object[] { target }, out var ret))
            {
                return ret;
            }
            return null;
        }
        static T _GetValue<T>(INotifyValueChanged<T> target)
        {
            return target.value;
        }

        public static void SetValue(this VisualElement target, object newValue)
        {
            InvokeNotifyValueChangedMethod(ref NotifyValueChanged_SetValueMethod, nameof(_SetValue), target, new object[] { target, newValue }, out var ret);
        }
        static void _SetValue<T>(INotifyValueChanged<T> target, T newValue)
        {
            target.value = newValue;
        }
        public static void SetValueWithoutNotify(this VisualElement target, object newValue)
        {
            InvokeNotifyValueChangedMethod(ref NotifyValueChanged_SetValueWithoutNotifyMethod, nameof(_SetValueWithoutNotify), target, new object[] { target, newValue }, out var ret);
        }
        static void _SetValueWithoutNotify<T>(INotifyValueChanged<T> target, T newValue)
        {
            target.SetValueWithoutNotify(newValue);
        }

        /// <summary>
        /// 非泛型版本
        /// </summary>
        public static bool RegisterValueChangeCallback(this VisualElement target, Action<IChangeEvent, object> onChanged)
        {
          return  InvokeNotifyValueChangedMethod(ref NotifyValueChanged_RegisterValueChangeCallbackMethod, nameof(_RegisterValueChangeCallback), target, new object[] { target, onChanged }, out var ret);
        }

        static void _RegisterValueChangeCallback<T>(INotifyValueChanged<T> target, Action<IChangeEvent, object> onChanged)
        {
            target.RegisterValueChangedCallback(e =>
            {
                onChanged?.Invoke(e, e.newValue);
            });
        }

        static bool InvokeNotifyChangedMethod(ref MethodInfo method, string methodName, Type valueType, object target, object[] args)
        {
            if (method == null)
            {
                method = typeof(Extensions).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
            }
            method.MakeGenericMethod(valueType).Invoke(target, args);
            return true;
        }
        public static void RegisterChangeCallback(this VisualElement target, Type valueType, Action<IChangeEvent, object> onChanged)
        {
            InvokeNotifyChangedMethod(ref RegisterChangeCallbackMethod, nameof(_RegisterChangeCallback), valueType, target, new object[] { target, onChanged });
        }

        static void _RegisterChangeCallback<T>(VisualElement target, Action<IChangeEvent, object> onChanged)
        {
            target.RegisterCallback<ChangeEvent<T>>(e =>
            {
                onChanged?.Invoke(e, e.newValue);
            });
        }

        #endregion

        public static void CenterOnMainWin(this EditorWindow window)
        {
            Rect main = EditorGUIUtility.GetMainWindowPosition();
            Rect pos = window.position;
            float centerWidth = (main.width - pos.width) * 0.5f;
            float centerHeight = (main.height - pos.height) * 0.5f;
            pos.x = main.x + centerWidth;
            pos.y = main.y + centerHeight;
            window.position = pos;
        }

        public static bool Invoke<TValue>(this PropertyChangedEventHandler self, object sender, string propertyName, ref TValue oldValue, TValue newValue)
        {
            if (!object.Equals(oldValue, newValue))
            {
                oldValue = newValue;
                PropertyChangedEventArgs args = new PropertyChangedEventArgs(propertyName);
                self.Invoke(sender, args);
                return true;
            }
            return false;
        }

    }
}