using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements.Extension;
namespace UnityEditor.UIElements.Extension
{
    public class InputFieldMetadata
    {
        public string Name;
        public string DisplayName;
        public string Tooltip;
        public Type ValueType;
        public MemberInfo Member;
        public bool? IsHidden;
        public string GroupTitle;

        public InputFieldAttribue FieldAttribute;
        public Type ViewType;
        public InputFieldAttribue ElementAttribute;
        public Type ElementViewType;

        private static Dictionary<Type, List<InputFieldMetadata>> typeToMembers;

        public MemberField CreateInputField(object target)
        {
            MemberField field = new MemberField(); 
            field.Member = Member;
            field.DisplayName = DisplayName;
            field.Tooltip = Tooltip;
            field.ValueType = ValueType;
            field.Target = target;
            field.CreateView();
            return field;
        }

        internal static List<InputFieldMetadata> GetMembers(Type settingOwnerType)
        {
            if (typeToMembers == null)
            {
                typeToMembers = new();
            }

            if (!typeToMembers.TryGetValue(settingOwnerType, out var members))
            {
                members = new();
                foreach (var mInfo in settingOwnerType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                    .Select(o => (MemberInfo)o)
                    .Concat(settingOwnerType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).Select(o => (MemberInfo)o)))
                {
                    if (mInfo.IsDefined(typeof(CompilerGeneratedAttribute)))
                        continue;
                     

                    //if (mInfo.IsDefined(typeof(HideInInspector)))
                    //    continue;
                    Type valueType;

                    if (mInfo.MemberType == MemberTypes.Field)
                    {
                        FieldInfo fInfo = (FieldInfo)mInfo;
                        if (!fInfo.IsPublic && !fInfo.IsDefined(typeof(SerializeField)))
                        {
                            continue;
                        }
                        valueType = fInfo.FieldType;
                    }
                    else
                    {
                        PropertyInfo pInfo = (PropertyInfo)mInfo;
                        if (!pInfo.GetGetMethod().IsPublic )
                        {
                            continue;
                        }
                        valueType = pInfo.PropertyType;
                    }

                    //if(field.IsDefined(com)


                    InputFieldMetadata member = new InputFieldMetadata();
                    member.Member = mInfo;
                    member.ValueType = valueType;



                    if (mInfo.IsDefined(typeof(HideInInspector)))
                    {
                        member.IsHidden = true;
                    }

                    var headerAttr = mInfo.GetCustomAttribute<HeaderAttribute>();
                    if (headerAttr != null)
                    {
                        member.GroupTitle = headerAttr.header;
                    }

                    member.Name = mInfo.Name;

                    var nameAttr = mInfo.GetCustomAttribute<InspectorNameAttribute>();
                    if (nameAttr != null)
                    {
                        member.DisplayName = nameAttr.displayName;
                    }
                    if (string.IsNullOrEmpty(member.DisplayName))
                    {
                        member.DisplayName = ObjectNames.NicifyVariableName(member.Name);
                    }

                    //member.ViewType = CustomSettingViewAttribute.GetViewType(valueType);


                    foreach (var inputAttr in mInfo.GetCustomAttributes<InputFieldAttribue>())
                    {
                        if (inputAttr.IsElement)
                        {
                            member.ElementAttribute = inputAttr;
                        }
                        else
                        {
                            member.FieldAttribute = inputAttr;
                        }
                        Type viewType = EditorUIElementsUtility.GetInputViewType(inputAttr.GetType());
                        if (viewType != null)
                        {
                            if (inputAttr.IsElement)
                            {
                                member.ElementViewType = viewType;
                            }
                            else
                            {
                                member.ViewType = viewType;
                            }
                        }
                    }

                    if (member.ViewType == null)
                    {
                        member.ViewType = EditorUIElementsUtility.GetInputViewType(valueType);
                    }


                    members.Add(member);

                }
            }
            return members;
        }
    }
}
