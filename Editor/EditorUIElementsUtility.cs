using Codice.Client.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity;
using Unity.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Extension;

namespace UnityEditor.UIElements.Extension
{
    public class EditorUIElementsUtility
    {
        private static Dictionary<Type, Type> inputFieldTypes;
        private static Dictionary<Type, Lazy<ICreateUIFromField>> createInputFieldTypes;

        public const string USS_EditorUI = "EditorUI";

        #region UnityPackage

        private static string unityPackageName;
        internal static string UnityPackageName
        {
            get
            {
                if (unityPackageName == null)
                {
                    unityPackageName = typeof(EditorUIUtility).Assembly.GetUnityPackageName();
                }
                return unityPackageName;
            }
        }

        private static string unityPackageDir;

        internal static string UnityPackageDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(unityPackageDir))
                    unityPackageDir = typeof(EditorUIUtility).Assembly.GetUnityPackageDirectory();
                return unityPackageDir;
            }
        }

        #endregion

        public static StyleSheet AddStyle(VisualElement elem, string uss)
        {
            return elem.AddStyle(typeof(EditorUIUtility), uss);
        }
        public static StyleSheet AddEditorStyle(VisualElement elem)
        {
            return AddStyle(elem, USS_EditorUI);
        }

        static Type GetInputFieldType(Type valueType)
        {
            if (inputFieldTypes == null)
            {
                inputFieldTypes = new()
                {
                    { typeof(string), typeof(TextField) },
                    { typeof(int), typeof(IntegerField) },
                    { typeof(float), typeof(FloatField) },
                    { typeof(long), typeof(LongField) },
                    { typeof(double), typeof(DoubleField) },
                    { typeof(bool), typeof(Toggle) },
                };

            }

            if (!inputFieldTypes.TryGetValue(valueType, out var fieldType))
            {
                foreach (var baseType in new Type[] { typeof(TextInputBaseField<>), typeof(TextValueField<>), typeof(BaseField<>) })
                {
                    Type baseType2 = baseType;
                    if (baseType2.IsGenericTypeDefinition)
                        baseType2 = baseType2.MakeGenericType(valueType);
                    foreach (var type in TypeCache.GetTypesDerivedFrom(baseType2))
                    {
                        if (type.IsAbstract) continue;
                        fieldType = type;
                        break;
                    }
                }

                if (fieldType == null)
                {
                    if (valueType.IsEnum)
                    {
                        fieldType = typeof(EnumField);
                    }
                }

                inputFieldTypes[valueType] = fieldType;
            }
            return fieldType;
        }
        public static VisualElement CreateInputField(Type valueType)
        {
            VisualElement input = null;
            Type inputType = GetInputFieldType(valueType);


            if (inputType == null)
            {
                if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(ConfigValue<>))
                {
                    inputType = typeof(ConfigValueField<>).MakeGenericType(valueType.GetGenericArguments()[0]);
                }
            }

            if (inputType != null)
                input = Activator.CreateInstance(inputType) as VisualElement;

            return input;
        }

        public static VisualElement CreateInputField(FieldInfo fieldInfo)
        {
            Type valueType = fieldInfo.FieldType;
            VisualElement input = null;

            Type inputType = GetInputFieldType(valueType);

            if (inputType != null)
                input = Activator.CreateInstance(inputType) as VisualElement;


            if (inputType == null)
            {
                if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(ConfigValue<>))
                {
                    inputType = typeof(ConfigValueField<>).MakeGenericType(valueType.GetGenericArguments()[0]);
                    input = Activator.CreateInstance(inputType, new object[] { fieldInfo }) as VisualElement;
                }
            }

            return input;
        }

        public static IEnumerable<(VisualElement inputField, FieldInfo fieldInfo)> CreateInputFields(object target, Func<MemberInfo, bool> filter = null)
        {
            HashSet<FieldInfo> fields = new HashSet<FieldInfo>();
            List<Type> types = new List<Type>();
            Type t = target.GetType();

            while (t != null)
            {
                types.Add(t);
                t = t.BaseType;
            }
            for (int i = types.Count - 1; i >= 0; i--)
            {
                foreach (var item in CreateInputFields(fields, target, types[i], filter))
                    yield return item;
            }
        }
        public static IEnumerable<(VisualElement inputField, FieldInfo fieldInfo)> CreateInputFields(Type targetType, Func<MemberInfo, bool> filter = null)
        {
            HashSet<FieldInfo> fields = new HashSet<FieldInfo>();
            List<Type> types = new List<Type>();
            Type t = targetType;

            while (t != null)
            {
                types.Add(t);
                t = t.BaseType;
            }
            for (int i = types.Count - 1; i >= 0; i--)
            {
                foreach (var item in CreateInputFields(fields, null, types[i], filter))
                    yield return item;
            }
        }

        private static IEnumerable<(VisualElement inputField, FieldInfo fieldInfo)> CreateInputFields(HashSet<FieldInfo> fields, object target, Type targetType, Func<MemberInfo, bool> filter = null)
        {

            foreach (var field in targetType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (fields.Contains(field))
                {
                    Debug.LogError("exists field: " + field);
                    continue;
                }
                if (!field.IsPublic && !field.IsDefined(typeof(SerializeField)))
                    continue;
                if (field.IsDefined(typeof(HideInInspector)))
                    continue;
                if (filter != null && !filter(field))
                    continue;

                VisualElement inputField = null;


                if (createInputFieldTypes == null)
                {
                    createInputFieldTypes = new();
                    var drawerTypeField = typeof(CustomPropertyDrawer).GetField("m_Type", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    foreach (var t in TypeCache.GetTypesWithAttribute(typeof(CustomPropertyDrawer)))
                    {
                        if (t.IsAbstract) continue;
                        if (!typeof(ICreateUIFromField).IsAssignableFrom(t))
                            continue;
                        var ctor = t.GetConstructor(Type.EmptyTypes);
                        if (ctor == null || !ctor.IsPublic) continue;
                        var creator = new Lazy<ICreateUIFromField>(() => Activator.CreateInstance(t) as ICreateUIFromField);
                        foreach (var attr in t.GetCustomAttributes<CustomPropertyDrawer>())
                        {
                            Type valueType = (Type)drawerTypeField.GetValue(attr);
                            createInputFieldTypes[valueType] = creator;
                        }
                    }
                }

                if (target != null && createInputFieldTypes.TryGetValue(field.FieldType, out var createInput))
                {
                    inputField = createInput.Value.CreateUIFromField(target, field);
                }

                if (inputField == null)
                    inputField = CreateInputField(field);
                if (inputField == null) continue;

                var inputFieldType = inputField.GetType();
                var labelProp = inputFieldType.GetProperty("label");
                if (labelProp != null)
                {
                    labelProp.SetValue(inputField, ObjectNames.NicifyVariableName(field.Name));
                }
                fields.Add(field);
                yield return (inputField, field);
            }
        }


        private static Dictionary<Type, Type> typeMapViewTypes;
        private static List<InputViewMetadata> inputViewMetadatas;
        class InputViewMetadata
        {
            public Type ValueType;
            public Type ViewType;
            public bool IncludeChildren;
        }

        public static Type GetInputViewType(Type valueType)
        {
            Type viewType = null;
            if (typeMapViewTypes == null)
            {
                typeMapViewTypes = new();
                inputViewMetadatas = new();
                foreach (var type in TypeCache.GetTypesWithAttribute(typeof(CustomInputViewAttribute)))
                {
                    if (!type.IsClass || type.IsAbstract)
                        continue;
                    if (!typeof(InputView).IsAssignableFrom(type))
                    {
                        Debug.LogError($"{nameof(CustomInputViewAttribute)} Type '{type.Name}' not interit '{typeof(InputView).Name}'");
                        continue;
                    }
                    var viewAttr = type.GetCustomAttribute<CustomInputViewAttribute>();
                    var targetType = viewAttr.TargetType;
                    if (targetType == null) continue;
                    InputViewMetadata metadata = new();
                    metadata.ViewType = type;
                    metadata.ValueType = targetType;
                    metadata.IncludeChildren = true;
                    inputViewMetadatas.Add(metadata);

                    if (!metadata.ViewType.IsAbstract)
                    {
                        typeMapViewTypes[targetType] = type;
                    }
                }
            }

            if (typeMapViewTypes.TryGetValue(valueType, out viewType))
                return viewType;

            if (BaseInputView.IsBaseField(valueType))
            {
                viewType = typeof(BaseInputView);
                typeMapViewTypes[valueType] = viewType;
                return viewType;
            }

            if (valueType.IsEnum)
            {
                if (typeMapViewTypes.TryGetValue(typeof(Enum), out viewType))
                {
                    typeMapViewTypes[valueType] = viewType;
                }
                return viewType;
            }

            foreach (var metadata in inputViewMetadatas)
            {
                if (metadata.IncludeChildren && metadata.ValueType.IsAssignableFrom(valueType))
                {
                    viewType = metadata.ViewType;
                    typeMapViewTypes[valueType] = viewType;
                    break;
                }

                if (metadata.ValueType.IsGenericTypeDefinition)
                {
                    if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == metadata.ValueType)
                    {

                        viewType = metadata.ViewType;
                        typeMapViewTypes[valueType] = viewType;
                        break;
                    }
                }
            }

            return null;
        }

        public static InputView CreateInputView(Type valueType)
        {
            Type viewType = GetInputViewType(valueType);
            if (viewType == null)
                return null;
            InputView view = Activator.CreateInstance(viewType) as InputView;
            if (view == null)
                return null;
            view.ValueType = valueType;
            return view;
        }

        public static List<MemberField> CreateMemberInputFields(CreateMemberInputOptions options)
        {
            List<InputFieldMetadata> memberMetdatas = new();
            List<MemberField> memberFields = new List<MemberField>();

            GroupBox groupBox = null;

            foreach (var metadata in InputFieldMetadata.GetMembers(options.target.GetType()))
            {
                if (metadata.IsHidden.HasValue && metadata.IsHidden.Value)
                {
                    continue;
                }

                if (options.filter != null && !options.filter(metadata))
                    continue;


                memberMetdatas.Add(metadata);

                if (!string.IsNullOrEmpty(metadata.GroupTitle))
                {
                    groupBox = new GroupBox();
                    groupBox.AddToClassList("setting-group");
                    groupBox.text = metadata.GroupTitle;
                    options.parent.Add(groupBox);
                    //Label headerLabel = new Label();
                    //headerLabel.AddToClassList("setting-group__title");
                    //headerLabel.text=member.GroupTitle;
                    //options.parent.Add(headerLabel);
                }

                if (options.CreateFieldBefore != null)
                {
                    if (!options.CreateFieldBefore(metadata))
                        continue;
                }
                var field = metadata.CreateInputField(options.target);
                var inputView = field.View;
                if (inputView != null)
                {
                    inputView.name = metadata.Name;
                    //inputView.AddToClassList(GetSettingFieldMemberClassName(member));
                    if (groupBox != null)
                    {
                        groupBox.contentContainer.Add(inputView);
                    }
                    else
                    {
                        options.parent.Add(inputView);
                    }
                    options.CreateFieldAfter?.Invoke(metadata, inputView);
                    memberFields.Add(field);
                }
            }
            return memberFields;
        }
    }

    public class EditorUIUtility : EditorUIElementsUtility
    {
    }
    public class CreateMemberInputOptions
    {
        public VisualElement parent;
        public object target;
        public Func<InputFieldMetadata, bool> filter;

        public Func<InputFieldMetadata, bool> CreateFieldBefore;
        public Action<InputFieldMetadata, VisualElement> CreateFieldAfter;


    }
}