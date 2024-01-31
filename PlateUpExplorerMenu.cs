using Kitchen.Layouts.Modules;
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

        protected List<T> GetFuzzyMatches<T>(IEnumerable<T> items, string matchString, Func<T, string> selector, int minCharMatch = 3, int maxExtraChars = 3, int maxDistance = 5, bool ignoreCase = true)
        {
            maxDistance = Mathf.Clamp(maxDistance, 0, 999);
            
            int matchStringLength = matchString.Length;

            List<(T value, int length, int distance)> matches = new List<(T, int, int)>();
            foreach (T item in items)
            {
                string candidate = selector(item);

                bool containsSubstring = candidate.Contains(matchString);
                if ((maxExtraChars >= 0 && matchString.Length - candidate.Length > maxExtraChars) ||
                    (minCharMatch < Mathf.Min(candidate.Length, matchStringLength) && candidate.Intersect(matchString).Count() < minCharMatch) ||
                    (!IsFuzzyMatch(selector(item), matchString, out int distance, true, maxDistance, ignoreCase) && !containsSubstring))
                    continue;

                bool startsWithSubstring = candidate.StartsWith(matchString);
                distance *= 1 * (containsSubstring ? 1 : 1000) * (startsWithSubstring ? 1 : 1000);
                matches.Add((item, candidate.Length, distance));
            }
            return matches
                .OrderBy(item => item.distance)
                .ThenBy(item => item.length)
                .Select(item => item.value)
                .ToList();
        }

        protected bool IsFuzzyMatch(string s1, string s2, out int editDistance, bool ignoreLength = false, int maxDistance = 10, bool ignoreCase = true)
        {
            if (ignoreCase)
            {
                s1 = s1.ToLowerInvariant();
                s2 = s2.ToLowerInvariant();
            }

            maxDistance = Mathf.Clamp(maxDistance, 0, 999);
            editDistance = StringUtils.LevenshteinDistance(s1, s2);
            if (ignoreLength)
                editDistance -= Mathf.Abs(s1.Length - s2.Length);
            return editDistance <= maxDistance;
        }

        protected class ObjectData
        {
            public enum TypeClassification
            {
                Unknown,
                Null,
                Class,
                Struct,
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
                        case TypeClassification.Class:
                        case TypeClassification.Struct:
                        case TypeClassification.Collection:
                        case TypeClassification.Enum:
                        case TypeClassification.Tuple:
                        case TypeClassification.GameObject:
                        case TypeClassification.NodeGraph:
                        case TypeClassification.Node:
                            IsInit = false;
                            break;
                        case TypeClassification.Native:
                        case TypeClassification.Interface:
                        case TypeClassification.Pointer:
                        case TypeClassification.Anonymous:
                        case TypeClassification.Texture:
                        default:
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
                        PropertyInfo[] properties = Class.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                        for (int i = 0; i < properties.Length; i++)
                        {
                            PropertyInfo property = properties[i];
                            FieldDatas.Add(new ObjectData($"{property.Name} {{{(property.CanRead ? " get;" : string.Empty)}{(property.CanWrite ? " set;" : string.Empty)} }}", property.GetValue(Value)));
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
                        List<GameObject> children = new List<GameObject>();
                        for (int i = 0; i < gameObject.transform.childCount; i++)
                        {
                            children.Add(gameObject.transform.GetChild(i).gameObject);
                        }
                        FieldDatas.Add(new ObjectData("Children", children.ToArray()));
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
                string typeReadableName = ReflectionUtils.GetReadableTypeName(Class);
                switch (Classification)
                {
                    case TypeClassification.Struct:
                    case TypeClassification.Class:
                    case TypeClassification.Collection:
                    case TypeClassification.GameObject:
                        valueString = null;
                        try
                        {
                            if (Value is UnityEngine.Object obj)
                            {
                                valueString = $"{obj.name} ({typeReadableName})";
                            }
                        }
                        catch (NullReferenceException) { }

                        if (valueString == null)
                            valueString = typeReadableName;
                        
                        return true;
                    case TypeClassification.Enum:
                        valueString = $"{typeReadableName}.{Value}";
                        return true;
                    default:
                        return false;
                }
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
                label += $"{data.Name}";
                label += data.Value == null ? " = null" : $" = {(data.GetValueStringOverride(out string valueString) ? valueString : data.Value)}";
                if (GUILayout.Button(label, LabelLeftStyle, GUILayout.MinWidth(600)))
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
