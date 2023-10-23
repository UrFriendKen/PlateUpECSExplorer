using KitchenLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEngine;

namespace KitchenECSExplorer
{
    public class SystemsMenu : PlateUpExplorerMenu
    {
        private static System _worldSystems = new System()
        {
            Name = "World"
        };
        private Vector2 scrollPos = Vector2.zero;
        private static bool _isWorldSystemInit = false;

        private const float windowWidth = 775f;

        List<string> _filters = new List<string>();
        List<Color> _filterColors = new List<Color>()
        {
            new Color(1f, 0.65f, 0.65f),
            new Color(0.65f, 1f, 0.65f),
            new Color(0.65f, 0.65f, 1f),
            new Color(1f, 0.65f, 1f),
            new Color(0.65f, 1f, 1f)
        };
        readonly Color _filterFallbackColor = Color.white;
        const int MAX_FILTER_COUNT = 5;

        private struct World
        {
            public Dictionary<Type, System> SystemGroups;

            public World()
            {
                SystemGroups = new Dictionary<Type, System>();
            }
        }

        public class System
        {
            public string Name;
            private Dictionary<int, System> Subsystems;
            public bool IsExpanded;
            public System Parent;

            public System()
            {
                Name = null;
                Subsystems = new Dictionary<int, System>();
            }

            public void AddSubsystem(System data, int index)
            {
                data.Parent = this;
                Subsystems[index] = data;
            }

            public void Log(int indentLevel = 0)
            {
                Main.LogInfo($"{String.Concat(Enumerable.Repeat("\t", indentLevel))}{Name}");
                List<(int, System)> orderedSubsystems = Subsystems.OrderBy(x => x.Key).Select(x => (x.Key, x.Value)).ToList();
                foreach (var subsystem in orderedSubsystems)
                {
                    subsystem.Item2.Log(indentLevel + 1);
                }
            }

            public IEnumerable<System> GetChildSystemsOrdered()
            {
                return Subsystems.OrderBy(x => x.Key).Select(x => x.Value);
            }

            public bool ContainsSystemByNameRecurse(string systemNameSubstring)
            {
                string lowerSubstring = systemNameSubstring.ToLowerInvariant();
                if (Name.ToLowerInvariant().Contains(lowerSubstring))
                    return true;
                foreach (System subsystem in Subsystems.Values)
                {
                    if (subsystem.ContainsSystemByNameRecurse(systemNameSubstring))
                        return true;
                }
                return false;
            }

            public bool ContainsParentSystemByNameRecurse(string systemNameSubstring)
            {
                string lowerSubstring = systemNameSubstring.ToLowerInvariant();
                if (Parent == null)
                    return false;
                if (Parent.Name.ToLowerInvariant().Contains(lowerSubstring))
                    return true;
                return Parent.ContainsParentSystemByNameRecurse(systemNameSubstring);
            }

            public void CollapseRecurse()
            {
                IsExpanded = false;
                foreach (System subsystem in Subsystems.Values)
                    subsystem.CollapseRecurse();
            }
        }

        internal static void PopulateWorldSystems(Dictionary<Type, System> topLevelSystems)
        {
            List<Type> expectedTopLevelSystemTypes = new List<Type>()
            {
                typeof(InitializationSystemGroup),
                typeof(SimulationSystemGroup),
                typeof(PresentationSystemGroup)
            };

            for (int i = 0; i < expectedTopLevelSystemTypes.Count; i++)
            {
                Type systemType = expectedTopLevelSystemTypes[i];
                if (topLevelSystems.TryGetValue(systemType, out System system))
                {
                    _worldSystems.AddSubsystem(system, i);
                    continue;
                }
                Main.LogWarning($"Could not find {systemType}");
            }
            _isWorldSystemInit = true;
        }

        public SystemsMenu()
        {
            ButtonName = "Systems";
        }

        protected override void OnSetup() // This is called evey frame the menu is open, This is also where you draw your UnityGUI
        {
            GUILayout.BeginArea(new Rect(10f, 0f, windowWidth, 1050f));
            GUI.DrawTexture(new Rect(0f, 0f, windowWidth, 1050f), Background, ScaleMode.StretchToFill);

            DrawMultiFilter();
            if (!_isWorldSystemInit)
            {
                GUILayout.Label("World Systems not initialized!", LabelMiddleCentreStyle);
                return;
            }
            _worldSystems = DrawSystemsHierarchy(_worldSystems, ref scrollPos);
            GUILayout.EndArea();
        }

        private void DrawMultiFilter()
        {
            List<int> markedForDeletion = new List<int>();
            for (int i = 0; i < _filters.Count(); i++)
            {
                GUILayout.BeginHorizontal();
                _filters[i] = GUILayout.TextArea(_filters[i]);
                if (GUILayout.Button("X", GUILayout.Width(30f)))
                {
                    markedForDeletion.Add(i);
                }
                GUILayout.EndHorizontal();
            }
            for (int i = markedForDeletion.Count() - 1; i > -1; i--)
            {
                Main.LogInfo(i);
                _filters.RemoveAt(markedForDeletion[i]);
            }
            if (_filters.Count() < MAX_FILTER_COUNT && GUILayout.Button("Add Filter"))
            {
                _filters.Add(string.Empty);
            }
        }

        private Color GetFilterColor(int index)
        {
            if (index >= 0 && index < _filterColors.Count())
                return _filterColors[index];
            return _filterFallbackColor;
        }

        protected System DrawSystemsHierarchy(System system, ref Vector2 scrollPosition, string objName = null, float? width = null)
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

            system = DrawSystem(system);
            GUILayout.EndScrollView();

            return system;
        }

        private System DrawSystem(System system, int indentLevel = 0, int unitIndent = 20)
        {
            List<System> subsystems = system.GetChildSystemsOrdered().ToList();
            
            // Change indent to move label start position to the right
            string label = "";
            label += subsystems.Count() > 0 ? (system.IsExpanded ? "▼ " : "▶ ") : "    ";
            label += $"{system.Name}";

            Color defaultContentColor = GUI.contentColor;
            if (indentLevel != 0 && !_filters.IsNullOrEmpty())
            {
                bool hasMatch = false;
                bool hasFilter = false;
                bool colorSet = false;
                for (int i = 0; i < _filters.Count(); i++)
                {
                    string highlightFilterText = _filters[i];
                    if (highlightFilterText.IsNullOrEmpty())
                        continue;
                    hasFilter = true;
                    if (!colorSet && system.Name.ToLowerInvariant().Contains(highlightFilterText.ToLowerInvariant()))
                    {
                        GUI.contentColor = GetFilterColor(i);
                        colorSet = true;
                    }
                    if (system.ContainsSystemByNameRecurse(highlightFilterText) || system.ContainsParentSystemByNameRecurse(highlightFilterText))
                        hasMatch = true;
                    if (colorSet && hasMatch)
                        break;
                }
                if (hasFilter && !hasMatch)
                    return system;
            }
            GUILayout.BeginHorizontal();
            GUILayout.Space(unitIndent * indentLevel);

            if (GUILayout.Button(label, LabelLeftStyle, GUILayout.MinWidth(600)))
            {
                system.IsExpanded = !system.IsExpanded;
            }
            GUI.contentColor = defaultContentColor;
            GUILayout.EndHorizontal();

            if (system.IsExpanded)
            {
                for (int i = 0; i < subsystems.Count(); i++)
                {
                    system.AddSubsystem(DrawSystem(subsystems[i], indentLevel + 1, unitIndent), i);
                }
            }

            return system;
        }
    }
}
