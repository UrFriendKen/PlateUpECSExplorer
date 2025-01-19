using KitchenData;
using KitchenECSExplorer.Utils;
using KitchenLib.Customs;
using KitchenLib.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        private string gdoTypeFilterText = "";
        private Vector2 vanillafilterScrollPosition = new Vector2(0, 0);
        private Vector2 customsfilterScrollPosition = new Vector2(0, 0);

        private List<Type> VanillaGDOs = new List<Type>();
        private List<Type> CustomGDOs = new List<Type>();

        private ObjectData selectedGDOInstance = null;
        private Type SelectedGDOType = null;
        private bool IsSelectedVanilla = false;
        private MethodInfo GenericVanillaGetGDO = null;
        private string instanceFilterText = "";

        private Vector2 gDOInstanceListScrollPosition = new Vector2(0, 0);
        private Vector2 hierarchyScrollPosition = new Vector2(0, 0);

        private const float windowWidth = 775f;

        //Temp while OnInit is not working
        private bool _isInit = false;

        public GDOMenu()
        {
            ButtonName = "GDOs";
        }

        protected override void OnInitialise()
        {
            if (_isInit)
                return;

            VanillaGDOs.Clear();
            CustomGDOs.Clear();
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
            _isInit = true;
        }

        protected override void OnSetup() // This is called evey frame the menu is open, This is also where you draw your UnityGUI
        {
            OnInitialise();

            #region All Components List
            GUILayout.BeginArea(new Rect(10f, 0f, windowWidth, 250f));
            GUI.DrawTexture(new Rect(0f, 0f, windowWidth, 250f), Background, ScaleMode.StretchToFill);

            GUILayout.Label("Filter");

            IEnumerable<Type> matchingVanillaGDOTypes = string.IsNullOrEmpty(gdoTypeFilterText) ?
                VanillaGDOs : GetFuzzyMatches(VanillaGDOs, gdoTypeFilterText, type => type.FullName);
            IEnumerable<Type> matchingCustomGDOTypes = string.IsNullOrEmpty(gdoTypeFilterText) ?
                CustomGDOs : GetFuzzyMatches(CustomGDOs, gdoTypeFilterText, type => type.FullName);
            gdoTypeFilterText = DoTabCompleteTextField("gdoTypeFilter", gdoTypeFilterText, matchingVanillaGDOTypes.Concat(matchingCustomGDOTypes).Select(type => type.FullName));

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(windowWidth * 0.4f));
            GUILayout.Label("Vanilla GDO Types", LabelCentreStyle);
            vanillafilterScrollPosition = GUILayout.BeginScrollView(vanillafilterScrollPosition, false, true, GUIStyle.none, GUI.skin.verticalScrollbar);

            foreach (Type gdoType in matchingVanillaGDOTypes)
            {
                string typeString = gdoType.FullName;
                if (GUILayout.Button(typeString, ButtonLeftStyle, GUILayout.Width(windowWidth * 0.4f - 15f)))
                {
                    Clear();
                    IsSelectedVanilla = true;
                    SelectedGDOType = gdoType;
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.Width(windowWidth * 0.6f));
            GUILayout.Label("KitchenLib Registered CustomGDO Types", LabelCentreStyle);
            customsfilterScrollPosition = GUILayout.BeginScrollView(customsfilterScrollPosition, false, true, GUIStyle.none, GUI.skin.verticalScrollbar);

            foreach (Type gdoType in matchingCustomGDOTypes)
            {
                string typeString = gdoType.FullName;
                if (GUILayout.Button(typeString, ButtonLeftStyle, GUILayout.Width(windowWidth * 0.6f - 15f)))
                {
                    Clear();
                    IsSelectedVanilla = false;
                    SelectedGDOType = gdoType;
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
            selectedGDOInstance = null;
            IsSelectedVanilla = false;
            gDOInstanceListScrollPosition = new Vector2(0, 0);
            hierarchyScrollPosition = new Vector2(0, 0);
            GenericVanillaGetGDO = null;
            instanceFilterText = string.Empty;
        }

        private void DrawVanilla()
        {
            if (GenericVanillaGetGDO == null)
            {
                GenericVanillaGetGDO = mVanillaGetGDO.MakeGenericMethod(SelectedGDOType);
            }
            DrawInstanceList(GenericVanillaGetGDO.Invoke(GameData.Main, null) as IEnumerable<object>);
        }

        private void DrawCustom()
        {
            IEnumerable<CustomGameDataObject> instances;
            if (SelectedGDOType.IsGenericTypeDefinition)
            {
                instances = CustomGDO.GDOs.Values.Where(x => x.GetType().IsConstructedGenericType && x.GetType().GetGenericTypeDefinition() == SelectedGDOType);
            }
            else
            {
                instances = CustomGDO.GDOs.Values.Where(x => SelectedGDOType.IsAssignableFrom(x.GetType()));
            }

            if (instances.Count() > 1)
            {
                DrawInstanceList(instances);
                return;
            }
            if (selectedGDOInstance == null)
            {
                MethodInfo genericGetCustomGameDataObject = mGetCustomGameDataObject.MakeGenericMethod(SelectedGDOType);
                var instance = genericGetCustomGameDataObject.Invoke(null, null);
                selectedGDOInstance = new ObjectData(SelectedGDOType.Name, instance);
                selectedGDOInstance.IsExpanded = true;
            }
            selectedGDOInstance = DrawObjectHierarchy(selectedGDOInstance, ref hierarchyScrollPosition, SelectedGDOType.Name);
        }

        private void DrawInstanceList(IEnumerable<object> instances)
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label($"Derived Types ({SelectedGDOType.Name})", LabelCentreStyle, GUILayout.Width(windowWidth * 0.3f));

            bool usingIDMatch;
            IEnumerable<object> matchingInstances;
            if (instanceFilterText.IsNullOrEmpty() || instanceFilterText.IsNumber())
            {
                usingIDMatch = true;
                matchingInstances = instances;
            }
            else
            {
                usingIDMatch = false;
                matchingInstances = GetFuzzyMatches(instances, instanceFilterText, obj =>
                {
                    if (obj == null)
                        return "";
                    if (obj is GameDataObject gdo)
                        return gdo.name;
                    if (obj is CustomGameDataObject customGdo)
                        return customGdo.GameDataObject?.name ?? "";
                    return obj.ToString();
                });
            }

            instanceFilterText = DoTabCompleteTextField("instanceFilter", instanceFilterText, matchingInstances.Select(item => item.ToString()), GUILayout.Width(windowWidth * 0.3f));
            gDOInstanceListScrollPosition = GUILayout.BeginScrollView(gDOInstanceListScrollPosition, false, true, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.Width(windowWidth * 0.3f));

            foreach (var instance in matchingInstances)
            {
                string buttonText = "";
                string idString = "";
                if (instance is GameDataObject gdo)
                {
                    buttonText = gdo.name;
                    idString = gdo.ID.ToString();
                }
                else if (instance is CustomGameDataObject customGdo)
                {
                    buttonText = customGdo.GameDataObject?.name;
                    idString = customGdo.ID.ToString();
                }

                if (usingIDMatch && !idString.Contains(instanceFilterText))
                    continue;

                if (GUILayout.Button(buttonText, ButtonLeftStyle, GUILayout.Width(windowWidth * 0.3f - 15f)))
                {
                    string typeName = Utils.ReflectionUtils.GetReadableTypeName(SelectedGDOType);
                    selectedGDOInstance = new ObjectData(typeName, instance);
                    selectedGDOInstance.IsExpanded = true;
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            if (selectedGDOInstance != null)
            {
                selectedGDOInstance = DrawObjectHierarchy(selectedGDOInstance, ref hierarchyScrollPosition);
            }
            GUILayout.EndHorizontal();
        }
    }
}
