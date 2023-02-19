using KitchenLib.DevUI;
using KitchenLib.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace KitchenECSExplorer
{
    public abstract class PlateUpExplorerMenu : BaseUI
    {
        protected GUIStyle LabelLeftStyle { get; private set; }
        protected GUIStyle LabelCentreStyle { get; private set; }
        protected GUIStyle LabelMiddleCentreStyle { get; private set; }

        protected GUIStyle ButtonLeftStyle { get; private set; }

        protected Texture2D Background { get; private set; }
        public sealed override void OnInit()
        {
            Background = new Texture2D(64, 64);
            Color grayWithAlpha = new Color(0.2f, 0.2f, 0.2f, 0.6f);
            for (int x = 0; x < 64; x++)
            {
                for (int y = 0; y < 64; y++)
                {
                    Background.SetPixel(x, y, grayWithAlpha);
                }
            }
            Background.Apply();
            OnInitialise();
        }

        public sealed override void Setup()
        {
            if (LabelLeftStyle == null)
            {
                LabelLeftStyle = new GUIStyle(GUI.skin.label);
                LabelLeftStyle.alignment = TextAnchor.MiddleLeft;
                LabelLeftStyle.padding.left = 10;
                LabelLeftStyle.stretchWidth = true;
            }


            if (LabelCentreStyle == null)
            {
                LabelCentreStyle = new GUIStyle(GUI.skin.label);
                LabelCentreStyle.alignment = TextAnchor.MiddleCenter;
                LabelCentreStyle.stretchWidth = true;
            }

            if (LabelMiddleCentreStyle == null)
            {
                LabelMiddleCentreStyle = new GUIStyle(GUI.skin.label);
                LabelMiddleCentreStyle.alignment = TextAnchor.MiddleCenter;
                LabelMiddleCentreStyle.stretchWidth = true;
                LabelMiddleCentreStyle.stretchHeight = true;
            }

            if (ButtonLeftStyle == null)
            {
                ButtonLeftStyle = new GUIStyle(GUI.skin.button);
                ButtonLeftStyle.alignment = TextAnchor.MiddleLeft;
                ButtonLeftStyle.padding.left = 10;
                ButtonLeftStyle.stretchWidth = true;
            }
            OnSetup();
        }

        protected virtual void OnInitialise()
        {
        }

        protected virtual void OnSetup()
        {

        }


        protected class ObjectData
        {
            private enum TypeClassification
            {
                Class,
                Struct,
                Native,
                Collection,
                Interface,
                Pointer,
                Enum,
                Anonymous,
                Tuple,
                Unknown
            }

            public readonly string Name;
            public readonly Type Class;
            public readonly object Value;
            public readonly List<ObjectData> FieldDatas;

            public bool IsInit { get; private set; }

            private bool _isExpanded;
            public bool IsExpanded
            {
                get { return _isExpanded; }
                set
                {
                    _isExpanded = value;
                    if (value && !IsInit)
                    {
                        PopulateFieldDatas();
                        IsInit = true;
                    }
                }
            }

            public ObjectData(string name, object instance)
            {
                Name = name;
                Value = instance;
                IsExpanded = false;
                FieldDatas = new List<ObjectData>();
                Class = Value == null ? null : instance.GetType();

                if (Value != null)
                {
                    TypeClassification typeClassification = DetermineTypeClassification(Class);
                    switch (typeClassification)
                    {
                        case TypeClassification.Class:
                        case TypeClassification.Struct:
                        case TypeClassification.Collection:
                        case TypeClassification.Enum:
                        case TypeClassification.Tuple:
                            IsInit = false;
                            break;
                        case TypeClassification.Native:
                        case TypeClassification.Interface:
                        case TypeClassification.Pointer:
                        case TypeClassification.Anonymous:
                        default:
                            IsInit = true;
                            break;
                    }
                }
                else
                {
                    IsInit = true;
                }
            }

            private void PopulateFieldDatas()
            {
                if (Value == null)
                {
                    return;
                }
                TypeClassification classification = DetermineTypeClassification(Class);
                switch (classification)
                {
                    case TypeClassification.Native:
                        break;
                    case TypeClassification.Class:
                    case TypeClassification.Struct:
                        FieldInfo[] fields = Class.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                        for (int i = 0; i < fields.Length; i++)
                        {
                            FieldInfo field = fields[i];
                            FieldDatas.Add(new ObjectData(field.Name, field.GetValue(Value)));
                        }
                        break;
                    case TypeClassification.Collection:
                        IEnumerable collection = Value as IEnumerable;
                        if (collection != null)
                        {
                            int count = 0;
                            foreach (object element in collection)
                            {
                                count++;
                            }
                            FieldDatas.Add(new ObjectData($"Number of Elements", count));
                            int index = 0;
                            foreach (object element in collection)
                            {
                                FieldDatas.Add(new ObjectData($"[{index++}]", element));
                            }
                        }
                        break;
                    case TypeClassification.Enum:
                        Array enumValues = Enum.GetValues(Class);
                        for (int i = 0; i < enumValues.Length; i++)
                        {
                            string enumValueName = Enum.GetName(Class, enumValues.GetValue(i));
                            int enumIntValue = (int)enumValues.GetValue(i);
                            FieldDatas.Add(new ObjectData($"{enumValueName}", enumIntValue));
                        }
                        break;
                    case TypeClassification.Tuple:
                        Type[] tupleTypes = Class.GetGenericArguments();
                        for (int i = 0; i < tupleTypes.Length; i++)
                        {
                            FieldDatas.Add(new ObjectData($"Item{i}", Class.GetProperty($"Item{i + 1}").GetValue(Value)));
                        }
                        break;
                    case TypeClassification.Interface:
                        //PropertyInfo[] interfaceProperties = Class.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                        //foreach (PropertyInfo property in interfaceProperties)
                        //{
                        //    FieldDatas.Add(new ObjectData(property.Name, property.GetValue(Value)));
                        //}
                        break;
                    case TypeClassification.Pointer:
                        IntPtr ptrValue = (IntPtr)Value;
                        if (ptrValue == IntPtr.Zero)
                        {
                            FieldDatas.Add(new ObjectData("Pointer", "NULL"));
                        }
                        else
                        {
                            FieldDatas.Add(new ObjectData("Pointer", ptrValue.ToString()));
                        }
                        break;
                    case TypeClassification.Anonymous:
                        break;
                    default:
                        // do something for unknown type
                        break;
                }
            }

            private TypeClassification DetermineTypeClassification(Type type)
            {
                if (type.IsPrimitive || type == typeof(string))
                {
                    return TypeClassification.Native;
                }
                else if (type.IsEnum)
                {
                    return TypeClassification.Enum;
                }
                else if (type.IsArray || type.IsGenericType &&
                         typeof(IEnumerable).IsAssignableFrom(type))
                {
                    return TypeClassification.Collection;
                }
                else if (type.IsValueType)
                {
                    if (type.Name.Contains("ValueTuple") || type.Name.Contains("Tuple"))
                    {
                        return TypeClassification.Tuple;
                    }
                    else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        return TypeClassification.Unknown;
                    }
                    else if (type.IsValueType)
                    {
                        return TypeClassification.Struct;
                    }
                    else
                    {
                        return TypeClassification.Anonymous;
                    }
                }
                else if (type.IsInterface)
                {
                    return TypeClassification.Interface;
                }
                else if (type.IsPointer)
                {
                    return TypeClassification.Pointer;
                }
                else if (type.IsClass)
                {
                    return TypeClassification.Class;
                }
                else
                {
                    return TypeClassification.Unknown;
                }
            }
        }

        protected ObjectData DrawObjectHierarchy(ObjectData objectData, ref Vector2 scrollPosition, string objName = null, float? width = null)
        {
            if (width.HasValue)
            {
                float widthValue = width.Value;
                scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUILayout.Width(widthValue));
            }
            else
            {
                scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar);
            }

            objectData = DrawGDOData(objectData);
            GUILayout.EndScrollView();

            return objectData;
        }

        private ObjectData DrawGDOData(ObjectData data, int indentLevel = 0, int unitIndent = 20)
        {
            // Change indent to move label start position to the right
            string label = "";
            label += data.FieldDatas.Count > 0 || !data.IsInit ? (data.IsExpanded ? "▼ " : "▶ ") : "    ";
            //label += $"{data.Name} ({data.Class})";
            label += $"{data.Name}";
            label += data.Value == null ? " = null" : $" = {data.Value}";

            GUILayout.BeginHorizontal();
            GUILayout.Space(unitIndent * indentLevel);
            if (GUILayout.Button(label, LabelLeftStyle, GUILayout.MinWidth(600)))
            {
                data.IsExpanded = !data.IsExpanded;
            }
            GUILayout.EndHorizontal();

            if (data.IsExpanded)
            {
                for (int i = 0; i < data.FieldDatas.Count; i++)
                {
                    data.FieldDatas[i] = DrawGDOData(data.FieldDatas[i], indentLevel + 1, unitIndent);
                }
            }

            return data;
        }
    }
}
