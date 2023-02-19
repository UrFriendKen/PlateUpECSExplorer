using KitchenData;
using KitchenLib.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Entities;
using UnityEngine;

namespace KitchenECSExplorer
{
    internal class GDOMenu : PlateUpExplorerMenu
    {
        private MethodInfo mGetCustomGameDataObject = typeof(GDOUtils).GetMethod("GetCustomGameDataObject", new Type[] { });
        private MethodInfo mVanillaGetGDO = typeof(GameData).GetMethod("Get", new Type[] { });

        private class GDOData
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
            public readonly List<GDOData> FieldDatas;

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

            public GDOData(string name, object instance)
            {
                Name = name;
                Value = instance;
                IsExpanded = false;
                FieldDatas = new List<GDOData>();
                Class = Value == null? null : instance.GetType();

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
                            FieldDatas.Add(new GDOData(field.Name, field.GetValue(Value)));
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
                            FieldDatas.Add(new GDOData($"Number of Elements", count));
                            int index = 0;
                            foreach (object element in collection)
                            {
                                FieldDatas.Add(new GDOData($"[{index++}]", element));
                            }
                        }
                        break;
                    case TypeClassification.Enum:
                        Array enumValues = Enum.GetValues(Class);
                        for (int i = 0; i < enumValues.Length; i++)
                        {
                            string enumValueName = Enum.GetName(Class, enumValues.GetValue(i));
                            int enumIntValue = (int)enumValues.GetValue(i);
                            FieldDatas.Add(new GDOData($"{enumValueName}", enumIntValue));
                        }
                        break;
                    case TypeClassification.Tuple:
                        Type[] tupleTypes = Class.GetGenericArguments();
                        for (int i = 0; i < tupleTypes.Length; i++)
                        {
                            FieldDatas.Add(new GDOData($"Item{i}", Class.GetProperty($"Item{i + 1}").GetValue(Value)));
                        }
                        break;
                    case TypeClassification.Interface:
                        //PropertyInfo[] interfaceProperties = Class.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                        //foreach (PropertyInfo property in interfaceProperties)
                        //{
                        //    FieldDatas.Add(new GDOData(property.Name, property.GetValue(Value)));
                        //}
                        break;
                    case TypeClassification.Pointer:
                        IntPtr ptrValue = (IntPtr)Value;
                        if (ptrValue == IntPtr.Zero)
                        {
                            FieldDatas.Add(new GDOData("Pointer", "NULL"));
                        }
                        else
                        {
                            FieldDatas.Add(new GDOData("Pointer", ptrValue.ToString()));
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

        private string componentFilterText = "";
        private static Vector2 vanillafilterScrollPosition = new Vector2(0, 0);
        private static Vector2 customsfilterScrollPosition = new Vector2(0, 0);

        private static List<Type> VanillaGDOs = new List<Type>();
        private static List<Type> CustomGDOs = new List<Type>();

        private GDOData SelectedGDO = null;
        private Type SelectedGDOType = null;
        private bool IsSelectedVanilla = false;
        private MethodInfo GenericVanillaGetGDO = null;
        private string GDOFilterText = "";

        private static Vector2 vanillaGDOInstanceListScrollPosition = new Vector2(0, 0);
        private static Vector2 hierarchyScrollPosition = new Vector2(0, 0);

        private const float windowWidth = 775f;

        public GDOMenu()
        {
            ButtonName = "GDOs";
        }

        public override void OnInitialise()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // Get all non-abstract types in the assembly that inherit from KitchenData.GameDataObject
                VanillaGDOs.AddRange(assembly.GetTypes().Where(
                        type => type.IsSubclassOf(typeof(KitchenData.GameDataObject)) &&
                        !type.IsAbstract).ToList());

                // Get all non-abstract types in the assembly that inherit from KitchenLib.Customs.CustomGameDataObject
                CustomGDOs.AddRange(assembly.GetTypes().Where(
                    type => type.IsSubclassOf(
                    typeof(KitchenLib.Customs.CustomGameDataObject)) &&
                    !type.IsAbstract));
            }
            VanillaGDOs = VanillaGDOs.OrderBy(type => type.FullName).ToList();
            CustomGDOs = CustomGDOs.OrderBy(type => type.FullName).ToList();
            Main.LogInfo($"Number of Vanilla GDOs = {VanillaGDOs.Count}");
            Main.LogInfo($"Number of Custom GDOs = {CustomGDOs.Count}");
        }

        public override void OnSetup() // This is called evey frame the menu is open, This is also where you draw your UnityGUI
        {

            #region All Components List
            GUILayout.BeginArea(new Rect(10f, 0f, windowWidth, 250f));
            GUI.DrawTexture(new Rect(0f, 0f, windowWidth, 250f), Background, ScaleMode.StretchToFill);

            GUILayout.Label("Filter");

            componentFilterText = GUILayout.TextField(componentFilterText);
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(windowWidth * 0.4f));
            GUILayout.Label("Vanilla GDO Types", LabelCentreStyle);
            vanillafilterScrollPosition = GUILayout.BeginScrollView(vanillafilterScrollPosition, false, true, GUIStyle.none, GUI.skin.verticalScrollbar);

            for (int i = 0; i < VanillaGDOs.Count; i++)
            {
                string typeString = VanillaGDOs[i].FullName;
                if (string.IsNullOrEmpty(componentFilterText) || typeString.ToLower().Contains(componentFilterText.ToLower()))
                {
                    if (GUILayout.Button(typeString, ButtonLeftStyle, GUILayout.Width(windowWidth * 0.4f - 15f)))
                    {
                        Clear();
                        IsSelectedVanilla = true;
                        SelectedGDOType = VanillaGDOs[i];
                    }
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.Width(windowWidth * 0.6f));
            GUILayout.Label("Kitchen Lib Registered CustomGDO Types", LabelCentreStyle);
            customsfilterScrollPosition = GUILayout.BeginScrollView(customsfilterScrollPosition, false, true, GUIStyle.none, GUI.skin.verticalScrollbar);

            for (int i = 0; i < CustomGDOs.Count; i++)
            {
                string typeString = CustomGDOs[i].FullName;
                if (string.IsNullOrEmpty(componentFilterText) || typeString.ToLower().Contains(componentFilterText.ToLower()))
                {
                    if (GUILayout.Button(typeString, ButtonLeftStyle, GUILayout.Width(windowWidth * 0.6f - 15f)))
                    {
                        Clear();
                        IsSelectedVanilla = false;
                        SelectedGDOType = CustomGDOs[i];
                    }
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            #endregion

            if (SelectedGDOType != null)
            {
                GUILayout.BeginArea(new Rect(10f, 260f, windowWidth, 700));
                GUI.DrawTexture(new Rect(0f, 0f, windowWidth, 700f), Background, ScaleMode.StretchToFill);
                if (IsSelectedVanilla)
                {
                    DrawVanilla();
                }
                else
                {
                    DrawCustom();
                }
                GUILayout.EndArea();
            }
        }
        private void Clear()
        {
            SelectedGDOType = null;
            SelectedGDO = null;
            IsSelectedVanilla = false;
            vanillaGDOInstanceListScrollPosition = new Vector2(0, 0);
            hierarchyScrollPosition = new Vector2(0, 0);
            GenericVanillaGetGDO = null;
            GDOFilterText = string.Empty;
        }

        private void DrawVanilla()
        {
            if (GenericVanillaGetGDO == null)
            {
                GenericVanillaGetGDO = mVanillaGetGDO.MakeGenericMethod(SelectedGDOType);
            }

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label($"Derived Types ({SelectedGDOType.Name})", LabelCentreStyle, GUILayout.Width(windowWidth * 0.3f));
            GDOFilterText = GUILayout.TextField(GDOFilterText, GUILayout.Width(windowWidth * 0.3f));
            vanillaGDOInstanceListScrollPosition = GUILayout.BeginScrollView(vanillaGDOInstanceListScrollPosition, false, true, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.Width(windowWidth * 0.3f));

            foreach (var gDO in GenericVanillaGetGDO.Invoke(GameData.Main, null) as IEnumerable<object>)
            {
                string typeName = gDO.ToString();
                if (string.IsNullOrEmpty(GDOFilterText) || typeName.ToLower().Contains(GDOFilterText.ToLower()))
                {
                    if (GUILayout.Button(typeName, ButtonLeftStyle, GUILayout.Width(windowWidth * 0.3f - 15f)))
                    {
                        SelectedGDO = new GDOData(typeName, gDO);
                    }
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            if (SelectedGDO != null)
            {
                DrawHierarchy();
            }
            GUILayout.EndHorizontal();
        }

        private void DrawCustom()
        {
            if (SelectedGDO == null)
            {
                MethodInfo genericGetCustomGameDataObject = mGetCustomGameDataObject.MakeGenericMethod(SelectedGDOType);
                var instance = genericGetCustomGameDataObject.Invoke(null, null);
                SelectedGDO = new GDOData(SelectedGDOType.Name, instance);
            }
            DrawHierarchy();
        }

        private void DrawHierarchy(float? width = null)
        {
            if (SelectedGDO != null)
            {
                if (width.HasValue)
                {
                    float widthValue = width.Value;
                    hierarchyScrollPosition = GUILayout.BeginScrollView(hierarchyScrollPosition, false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUILayout.Width(widthValue));
                }
                else
                {
                    hierarchyScrollPosition = GUILayout.BeginScrollView(hierarchyScrollPosition, false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar);
                }

                DrawGDOData(SelectedGDO);

                GUILayout.EndScrollView();
            }
            else
            {
                // Error Message
            }
        }

        private GDOData DrawGDOData(GDOData data, int indentLevel = 0, int unitIndent = 20)
        {
            // Change indent to move label start position to the right
            string label = "";
            label += data.FieldDatas.Count > 0 || !data.IsInit? (data.IsExpanded? "▼ " : "▶ ") : "    ";
            //label += $"{data.Name} ({data.Class})";
            label += $"{data.Name}";
            label += data.Value == null? " = null" : $" = {data.Value}";

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
