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

        private string highlightFilterText = String.Empty;
        private readonly Color _highlightColor = Color.red;

        private struct World
        {
            public Dictionary<Type, System> SystemGroups;

            public World()
            {
                SystemGroups = new Dictionary<Type, System>();
            }
        }

        public struct System
        {
            public string Name;
            private Dictionary<int, System> Subsystems;
            public bool IsExpanded;

            public System()
            {
                Name = null;
                Subsystems = new Dictionary<int, System>();
            }

            public void AddSubsystem(System data, int index)
            {
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

            highlightFilterText = GUILayout.TextArea(highlightFilterText);
            if (!_isWorldSystemInit)
            {
                GUILayout.Label("World Systems not initialized!", LabelMiddleCentreStyle);
                return;
            }
            _worldSystems = DrawSystemsHierarchy(_worldSystems, ref scrollPos);
            GUILayout.EndArea();
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

            //Color defaultContentColor = GUI.contentColor;
            if (indentLevel != 0 && !highlightFilterText.IsNullOrEmpty())
            {
                if (!system.ContainsSystemByNameRecurse(highlightFilterText))
                {
                    //system.CollapseRecurse();
                    return system;
                }
                //if (system.Name.ToLowerInvariant().Contains(highlightFilterText.ToLowerInvariant()))
                //    GUI.contentColor = _highlightColor;
            }
            GUILayout.BeginHorizontal();
            GUILayout.Space(unitIndent * indentLevel);

            if (GUILayout.Button(label, LabelLeftStyle, GUILayout.MinWidth(600)))
            {
                system.IsExpanded = !system.IsExpanded;
            }
            //GUI.contentColor = defaultContentColor;
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
