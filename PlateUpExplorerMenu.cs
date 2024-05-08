using Kitchen.Layouts.Modules;
using KitchenData;
using KitchenECSExplorer.Utils;
using KitchenLib.DevUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using XNode;

namespace KitchenECSExplorer
{
    public abstract class PlateUpExplorerMenu : BaseUI
    {
        protected GUIStyle LabelLeftStyle { get; private set; }
        protected GUIStyle LabelLeftStyleNoRichText { get; private set; }
        protected GUIStyle LabelLeftStyleNoWordWrapNoRichText { get; private set; }
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
                LabelLeftStyle.fixedWidth = 0;
                LabelLeftStyle.stretchWidth = true;
            }

            if (LabelLeftStyleNoRichText == null)
            {
                LabelLeftStyleNoRichText = new GUIStyle(LabelLeftStyle);
                LabelLeftStyleNoRichText.richText = false;
            }

            if (LabelLeftStyleNoWordWrapNoRichText == null)
            {
                LabelLeftStyleNoWordWrapNoRichText = new GUIStyle(LabelLeftStyleNoRichText);
                LabelLeftStyleNoWordWrapNoRichText.wordWrap = false;
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

        protected string DoTabCompleteTextField(string controlName, string text, IEnumerable<string> orderedMatches, params GUILayoutOption[] options)
        {
            Event ev = Event.current;
            if (GUI.GetNameOfFocusedControl() == controlName &&
                !string.IsNullOrEmpty(text) &&
                ev.type == EventType.KeyDown &&
                (ev.keyCode == KeyCode.Tab || ev.character == '\t'))
            {
                ev.Use();
                text = orderedMatches?.FirstOrDefault() ?? text;
                
                GUI.FocusControl(controlName);
            }
            GUI.SetNextControlName(controlName);
            text = GUILayout.TextField(text, options);
            return text;
        }

        protected List<T> GetFuzzyMatches<T>(IEnumerable<T> items, string matchString, Func<T, string> selector, StringUtils.FuzzyMatchStrategy matchStrategy = StringUtils.FuzzyMatchStrategy.IgnoreCaseAndLength, int maxLengthDifference = 3)
        {
            int matchStringLength = matchString.Length;

            List<(T value, int length, int distance)> matches = new List<(T, int, int)>();
            foreach (T item in items)
            {
                string candidate = selector(item);

                bool containsSubstring = candidate.Contains(matchString);
                if (matchStringLength - candidate.Length > maxLengthDifference ||
                    !StringUtils.IsFuzzyMatch(candidate, matchString, out int distance, matchStrategy: matchStrategy, maxDistance: matchStringLength / 2) && !containsSubstring)
                    continue;

                bool startsWithSubstring = candidate.StartsWith(matchString);
                distance *= (containsSubstring ? 1 : 1000) * (startsWithSubstring ? 1 : 1000);
                matches.Add((item, candidate.Length, distance));
            }
            return matches
                .OrderBy(item => item.distance)
                .ThenBy(item => item.length)
                .Select(item => item.value)
                .ToList();
        }

        protected class ObjectData
        {
            private struct PossibleGDO
            {
                public int ID;
                public GameDataObject GDO;
            }

            public enum TypeClassification
            {
                Unknown,
                Null,
                Class,
                
                Struct,
                PossibleGDO,
                DataObjectList,
                Vector2,
                Vector3,
                Vector4,
                Quaternion,

                Native,
                Collection,
                Interface,
                Pointer,
                Enum,
                Anonymous,
                Tuple,
                Texture,
                GameObject,
                NodeGraph,
                Node
            }

            public readonly string Name;
            public readonly Type Class;
            public readonly TypeClassification Classification;
            public readonly object Value;
            public readonly List<ObjectData> FieldDatas;

            private static readonly Dictionary<Type, TypeClassification> _specialTypeClassification = new Dictionary<Type, TypeClassification>()
            {
                { typeof(PossibleGDO), TypeClassification.PossibleGDO },
                { typeof(DataObjectList), TypeClassification.DataObjectList },
                { typeof(Vector2), TypeClassification.Vector2 },
                { typeof(Vector3), TypeClassification.Vector3 },
                { typeof(Vector4), TypeClassification.Vector4 },
                { typeof(Quaternion), TypeClassification.Quaternion }
            };

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
                    Classification = DetermineTypeClassification(Class);
                    switch (Classification)
                    {
                        case TypeClassification.Native:
                        case TypeClassification.Interface:
                        case TypeClassification.Pointer:
                        case TypeClassification.Anonymous:
                        case TypeClassification.Texture:
                            IsInit = true;
                            break;
                    }
                }
                else
                {
                    Classification = TypeClassification.Null;
                    IsInit = true;
                }
            }

            private void PopulateFieldDatas()
            {
                if (Value == null)
                {
                    return;
                }
                switch (Classification)
                {
                    case TypeClassification.Class:
                    case TypeClassification.Struct:
                        FieldInfo[] fields = Class.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                        for (int i = 0; i < fields.Length; i++)
                        {
                            FieldInfo field = fields[i];
                            FieldDatas.Add(new ObjectData(field.Name, field.GetValue(Value)));
                        }
                        PropertyInfo[] properties = Class.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                        for (int i = 0; i < properties.Length; i++)
                        {
                            PropertyInfo property = properties[i];
                            FieldDatas.Add(new ObjectData($"{property.Name} {{{(property.CanRead ? " get;" : string.Empty)}{(property.CanWrite ? " set;" : string.Empty)} }}", property.GetValue(Value)));
                        }
                        break;
                    case TypeClassification.DataObjectList:
                        if (Value != default && Value is DataObjectList valueDataObjectList)
                        {
                            FieldDatas.Add(new ObjectData($"Number of Elements", valueDataObjectList.Count));
                            int valueDataObjectListIndex = 0;
                            foreach (int item in valueDataObjectList)
                            {
                                object element;
                                if (GameData.Main.TryGet(item, out GameDataObject gdo))
                                {
                                    element = new PossibleGDO()
                                    {
                                        ID = item,
                                        GDO = gdo
                                    };
                                }
                                else
                                    element = item;
                                FieldDatas.Add(new ObjectData($"[{valueDataObjectListIndex}]", element));
                                valueDataObjectListIndex++;
                            }
                        }
                        break;
                    case TypeClassification.PossibleGDO:
                        if (Value is PossibleGDO possibleGDO &&
                            possibleGDO.GDO)
                        {
                            FieldDatas.Add(new ObjectData($"Matching {possibleGDO.GDO.GetType().Name}", possibleGDO.GDO));
                        }
                        break;
                    case TypeClassification.Vector2:
                        if (Value != default && Value is Vector2 vector2)
                        {
                            FieldDatas.Add(new ObjectData($"x", vector2.x));
                            FieldDatas.Add(new ObjectData($"y", vector2.y));
                            FieldDatas.Add(new ObjectData($"magnitude", vector2.magnitude));
                            FieldDatas.Add(new ObjectData($"sqr Magnitude", vector2.sqrMagnitude));
                            FieldDatas.Add(new ObjectData($"normalized", vector2.normalized));
                        }
                        break;
                    case TypeClassification.Vector3:
                        if (Value != default && Value is Vector3 vector3)
                        {
                            FieldDatas.Add(new ObjectData($"x", vector3.x));
                            FieldDatas.Add(new ObjectData($"y", vector3.y));
                            FieldDatas.Add(new ObjectData($"z", vector3.z));
                            FieldDatas.Add(new ObjectData($"magnitude", vector3.magnitude));
                            FieldDatas.Add(new ObjectData($"sqr Magnitude", vector3.sqrMagnitude));
                            FieldDatas.Add(new ObjectData($"normalized", vector3.normalized));
                        }
                        break;
                    case TypeClassification.Vector4:
                        if (Value != default && Value is Vector4 vector4)
                        {
                            FieldDatas.Add(new ObjectData($"x", vector4.x));
                            FieldDatas.Add(new ObjectData($"y", vector4.y));
                            FieldDatas.Add(new ObjectData($"z", vector4.z));
                            FieldDatas.Add(new ObjectData($"w", vector4.w));
                            FieldDatas.Add(new ObjectData($"magnitude", vector4.magnitude));
                            FieldDatas.Add(new ObjectData($"sqr Magnitude", vector4.sqrMagnitude));
                            FieldDatas.Add(new ObjectData($"normalized", vector4.normalized));
                        }
                        break;
                    case TypeClassification.Quaternion:
                        if (Value != default && Value is Quaternion quaternion)
                        {
                            FieldDatas.Add(new ObjectData($"x", quaternion.x));
                            FieldDatas.Add(new ObjectData($"y", quaternion.y));
                            FieldDatas.Add(new ObjectData($"z", quaternion.z));
                            FieldDatas.Add(new ObjectData($"w", quaternion.w));
                            FieldDatas.Add(new ObjectData($"eulerAngles", quaternion.eulerAngles));
                            FieldDatas.Add(new ObjectData($"normalized", quaternion.normalized));
                        }
                        break;
                    case TypeClassification.Collection:
                        IEnumerable collection = Value as IEnumerable;
                        int count = 0;
                        foreach (object element in collection)
                        {
                            count++;
                        }
                        FieldDatas.Add(new ObjectData($"Number of Elements", count));

                        int index = 0;
                        foreach (object element in collection)
                        {
                            Type elementType = element?.GetType();
                            if (elementType != null && elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                            {
                                object key = elementType.GetProperty("Key").GetValue(element);
                                object value = elementType.GetProperty("Value").GetValue(element);
                                FieldDatas.Add(new ObjectData($"[{key}]", value));
                            }
                            else
                                FieldDatas.Add(new ObjectData($"[{index}]", element));
                            index++;
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
                    case TypeClassification.GameObject:
                        GameObject gameObject = (GameObject)Value;
                        Component[] components = gameObject.GetComponents<Component>();
                        FieldDatas.Add(new ObjectData("Components", components));
                        GameObject[] children = Enumerable.Range(0, gameObject.transform.childCount)
                            .Select(childIndex => gameObject.transform.GetChild(childIndex).gameObject)
                            .ToArray();
                        FieldDatas.Add(new ObjectData("Children", children));
                        FieldDatas.Add(new ObjectData("Layer", LayerMask.LayerToName(gameObject.layer)));
                        FieldDatas.Add(new ObjectData("Tag", gameObject.tag));
                        FieldDatas.Add(new ObjectData("HideFlags", gameObject.hideFlags));
                        FieldDatas.Add(new ObjectData("Scene", gameObject.scene));
                        FieldDatas.Add(new ObjectData("SceneCullingMask", gameObject.sceneCullingMask));
                        FieldDatas.Add(new ObjectData("IsStatic", gameObject.isStatic));
                        FieldDatas.Add(new ObjectData("Transform", gameObject.transform));
                        FieldDatas.Add(new ObjectData(gameObject.name, CustomPrefabSnapshot.GetSnapshot(gameObject, imageSize: 256)));
                        break;
                    case TypeClassification.NodeGraph:
                        NodeGraph graph = (NodeGraph)Value;
                        //FieldDatas.Add(new ObjectData("Number of nodes", graph.nodes.Count));
                        FieldDatas.Add(new ObjectData("Nodes", graph.nodes));
                        break;
                    case TypeClassification.Node:
                        Node node = (Node)Value;
                        FieldInfo[] nodeFields = Class.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                        HashSet<string> blacklistedFieldNamesNode = new HashSet<string>()
                        {
                            "graph",
                            "position"
                        };

                        if (node is LayoutModule)
                        {
                            blacklistedFieldNamesNode.UnionWith(new HashSet<string>()
                            {
                                "Texture",
                                "Input",
                                "Output",
                                "AppendFrom",
                                "Result"
                            });
                        }

                        for (int i = 0; i < nodeFields.Length; i++)
                        {
                            FieldInfo field = nodeFields[i];
                            if (blacklistedFieldNamesNode.Contains(field.Name))
                                continue;
                            FieldDatas.Add(new ObjectData(field.Name, field.GetValue(Value)));
                        }

                        Dictionary<string, List<Node>> connectedNodesDict = new Dictionary<string, List<Node>>();
                        foreach (KeyValuePair<NodePort, List<NodePort>> kvp in GraphUtils.GetConnections(node))
                        {
                            NodePort port = kvp.Key;
                            if (port.ConnectionCount <= 0)
                                continue;
                            List<Node> connectedNodes = kvp.Value.Select(nodePort => nodePort.node).ToList();
                            string portName = $"{port.fieldName}";
                            connectedNodesDict.Add(portName, connectedNodes);
                        }
                        FieldDatas.Add(new ObjectData("Connections", connectedNodesDict));
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
                if (_specialTypeClassification.TryGetValue(type, out TypeClassification specialTypeClassification))
                    return specialTypeClassification;

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
                else if (typeof(NodeGraph).IsAssignableFrom(type))
                {
                    return TypeClassification.NodeGraph;
                }
                else if (typeof(Node).IsAssignableFrom(type))
                {
                    return TypeClassification.Node;
                }
                else if (typeof(GameObject).IsAssignableFrom(type))
                {
                    return TypeClassification.GameObject;
                }
                else if (typeof(Texture).IsAssignableFrom(type))
                {
                    return TypeClassification.Texture;
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

            public bool GetValueStringOverride(out string valueString)
            {
                valueString = null;
                string typeReadableName = ReflectionUtils.GetReadableTypeName(Class, useFullname: false);
                string typeReadableFullName = ReflectionUtils.GetReadableTypeName(Class);
                switch (Classification)
                {
                    case TypeClassification.Native:
                        if (!(Value is string str))
                            break;
                        valueString = str == string.Empty ? "string.Empty" : $"\"{str}\"";
                        return true;
                    case TypeClassification.Struct:
                    case TypeClassification.Class:
                    case TypeClassification.Collection:
                    case TypeClassification.GameObject:
                        valueString = null;
                        try
                        {
                            if (Value is GameDataObject gdo)
                            {
                                valueString = $"{gdo.name} ({typeReadableName})";
                            }
                            else if (Value is UnityEngine.Object obj)
                            {
                                valueString = $"{obj.name} ({typeReadableFullName})";
                            }
                        }
                        catch (NullReferenceException) { }

                        if (valueString == null)
                            valueString = typeReadableFullName;
                        
                        return true;
                    case TypeClassification.Enum:
                        valueString = $"{typeReadableFullName}.{Value}";
                        return true;
                    case TypeClassification.PossibleGDO:
                        if (Value is PossibleGDO possibleGDO)
                        {
                            valueString = $"{possibleGDO.ID} ({possibleGDO.GDO.name})";
                            return true;
                        }
                        break;
                    default:
                        break;
                }
                return false;
            }

            public bool DrawValueOverride()
            {
                switch (Classification)
                {
                    case TypeClassification.Texture:
                        GUILayout.Box((Texture)Value);
                        return true;
                    default:
                        return false;
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

            objectData = DrawObject(objectData);
            GUILayout.EndScrollView();

            return objectData;
        }

        private ObjectData DrawObject(ObjectData data, int indentLevel = 0, int unitIndent = 20)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(unitIndent * indentLevel);
            if (!data.DrawValueOverride())
            {
                string label = "";
                label += data.FieldDatas.Count > 0 || !data.IsInit ? (data.IsExpanded ? "▼ " : "▶ ") : "    ";
                label += data.Name;
                label += data.Value == null ? " = null" : $" = {(data.GetValueStringOverride(out string valueString) ? valueString : data.Value)}";
                if (GUILayout.Button(label, LabelLeftStyleNoWordWrapNoRichText))
                {
                    data.IsExpanded = !data.IsExpanded;
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (data.IsExpanded)
            {
                for (int i = 0; i < data.FieldDatas.Count; i++)
                {
                    data.FieldDatas[i] = DrawObject(data.FieldDatas[i], indentLevel + 1, unitIndent);
                }
            }

            return data;
        }
    }
}
