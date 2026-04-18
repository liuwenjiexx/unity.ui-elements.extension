using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bindings;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.UIElements.Extension
{
    public class EditableMember
    {
        private Type valueType;
        private FieldInfo field;
        public PropertyInfo property;
        internal TypeCode typeCode;
        private string displayName;
        internal Type drawerType;
        internal IAccessor accessor;

        static Dictionary<Type, EditableMember[]> cachedMembers;
        public EditableMember()
        {
        }

        public EditableMember(string displayName, Type valueType, MemberInfo member)
        {
            this.displayName = displayName;
            this.valueType = valueType;
            if (member != null)
            {
                if (member is PropertyInfo)
                {
                    property = (PropertyInfo)member;
                }
                else
                {
                    field = (FieldInfo)member;
                }
            }
        }

        public string Name { get => Member.Name; }
        public Type ValueType { get => valueType; }
        public MemberInfo Member { get => field != null ? field : property; }

        public string DisplayName
        {
            get
            {
                if (displayName == null)
                    displayName = ObjectNames.NicifyVariableName(Name);
                return displayName;
            }
        }

        public static EditableMember[] GetMembers(Type type)
        {
            if (cachedMembers == null) cachedMembers = new();

            EditableMember[] members;

            if (cachedMembers.TryGetValue(type, out members))
                return members;

            Dictionary<string, EditableMember> list = new();

            Type parent = type.BaseType;
            if (parent != null)
            {
                var tmp = GetMembers(parent);
                if (tmp != null)
                {
                    foreach (var item in tmp)
                    {
                        if (list.ContainsKey(item.Name))
                            continue;
                        list[item.Name] = item;
                    }

                }
            }


            EditableMember member;
            foreach (var mInfo in type.GetMembers(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetField | BindingFlags.SetProperty))
            {
                if (!(mInfo.MemberType == MemberTypes.Field || mInfo.MemberType == MemberTypes.Property))
                    continue;
                if (mInfo.DeclaringType != type)
                    continue;
                member = null;

                var field = mInfo as FieldInfo;
                Type drawerType = null;

                if (field != null)
                {
                    if (field.IsInitOnly) continue;
                    if (field.IsDefined(typeof(NonSerializedAttribute), true))
                        continue;

                    if (!field.IsDefined(typeof(SerializeField), true))
                    {
                        if (!field.IsPublic)
                            continue;
                    }

                    member = new EditableMember()
                    {
                        field = field,
                        valueType = field.FieldType,
                    };
                }
                else
                {
                    var property = mInfo as PropertyInfo;
                    if (property != null)
                    {
                        if (!property.CanWrite || property.SetMethod == null || !property.SetMethod.IsPublic) continue;
                        if (property.IsIndexer()) continue;
                        member = new EditableMember()
                        {
                            property = property,
                            valueType = property.PropertyType
                        };
                    }
                }

                if (drawerType == null)
                {
                    drawerType = ValueDrawer.GetDrawerType(member.ValueType);
                }

                if (drawerType == null)
                    continue;


                var typeCode = Type.GetTypeCode(member.ValueType);
                if (typeCode == TypeCode.Object)
                {
                    if (!member.ValueType.IsDefined(typeof(SerializableAttribute), false))
                        continue;
                }

                //else if ((typeCode & SerializableTypeCode.Array) != 0)
                //{
                //    SerializableTypeCode itemTypeCode = typeCode & ~SerializableTypeCode.Array;
                //    if (itemTypeCode == SerializableTypeCode.Object)
                //    {
                //        if (!itemType.IsDefined(typeof(SerializableAttribute), false))
                //            continue;
                //    }
                //}



                if (member != null)
                {
                    member.displayName = ObjectNames.NicifyVariableName(member.Name);
                    member.typeCode = typeCode;
                    member.accessor = Accessor.Member(mInfo);
                    member.drawerType = drawerType;
                    list[member.Name] = member;
                }
            }
            members = list.Values.OrderBy(o => o.displayName).ToArray();
            cachedMembers[type] = members;
            return members;
        }
    }


}
