using KitchenLib;
using KitchenLib.DevUI;
using KitchenMods;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Entities;
using UnityEngine;

// Namespace should have "Kitchen" in the beginning
namespace KitchenECSExplorer
{
    public class Main : BaseMod, IModSystem
    {
        // GUID must be unique and is recommended to be in reverse domain name notation
        // Mod Name is displayed to the player and listed in the mods menu
        // Mod Version must follow semver notation e.g. "1.2.3"
        public const string MOD_GUID = "com.example.mymod";
        public const string MOD_NAME = "My Mod";
        public const string MOD_VERSION = "0.1.0";
        public const string MOD_AUTHOR = "My Name";
        public const string MOD_GAMEVERSION = ">=1.1.3";
        // Game version this mod is designed for in semver
        // e.g. ">=1.1.3" current and all future
        // e.g. ">=1.1.3 <=1.2.3" for all from/until

        // Boolean constant whose value depends on whether you built with DEBUG or RELEASE mode, useful for testing
#if DEBUG
        public const bool DEBUG_MODE = true;
#else
        public const bool DEBUG_MODE = false;
#endif

        public Main() : base(MOD_GUID, MOD_NAME, MOD_AUTHOR, MOD_VERSION, MOD_GAMEVERSION, Assembly.GetExecutingAssembly()) { }

        protected override void OnInitialise()
        {
            LogWarning($"{MOD_GUID} v{MOD_VERSION} in use!");
        }

        protected override void OnUpdate()
        {
        }

        protected override void OnPostActivate(KitchenMods.Mod mod)
        {
            RegisterMenu<ECSExplorerMenu>();
        }
        #region Logging
        public static void LogInfo(string _log) { Debug.Log($"[{MOD_NAME}] " + _log); }
        public static void LogWarning(string _log) { Debug.LogWarning($"[{MOD_NAME}] " + _log); }
        public static void LogError(string _log) { Debug.LogError($"[{MOD_NAME}] " + _log); }
        public static void LogInfo(object _log) { LogInfo(_log.ToString()); }
        public static void LogWarning(object _log) { LogWarning(_log.ToString()); }
        public static void LogError(object _log) { LogError(_log.ToString()); }
        #endregion
    }

    public class ECSExplorerMenu : BaseUI
    {

        GUIStyle textLeftStyle;
        GUIStyle textCentreStyle;
        GUIStyle textMiddleCentreStyle;

        private string componentFilterText = "";
        private static Vector2 filterScrollPosition = new Vector2(0, 0);

        public static Dictionary<string, ComponentType> Components = new Dictionary<string, ComponentType>();
        public static List<string> ComponentNames = new List<string>();

        private static List<string> QueryAll = new List<string>();
        private static Vector2 queryAllScrollPosition = new Vector2(0, 0);
        private static List<string> QueryAny = new List<string>();
        private static Vector2 queryAnyScrollPosition = new Vector2(0, 0);
        private static List<string> QueryNone = new List<string>();
        private static Vector2 queryNoneScrollPosition = new Vector2(0, 0);

        private static Vector2 resultsScrollPosition = new Vector2(0, 0);

        private static List<EntityData> watchingEntities = new List<EntityData>();
        private static List<ComponentType> watchingEntitiesSelectedComponent = new List<ComponentType>();
        private static Vector2 watchingEntitiesScrollPosition = new Vector2(0, 0);
        private static List<Vector2> watchingEntitiesComponentsScrollPosition = new List<Vector2>();
        private static List<Vector2> watchingEntitiesComponentInfoScrollPosition = new List<Vector2>();

        Texture2D background;

        public ECSExplorerMenu()
        {
            ButtonName = "Entity Query";
        }

        public override void OnInit()
        {
            int i = 0;
            foreach (var typeInfo in TypeManager.AllTypes.Where(componentType => componentType.Category == TypeManager.TypeCategory.ComponentData))
            {
                if (typeInfo.Type != null && typeInfo.Category == TypeManager.TypeCategory.ComponentData && !ComponentNames.Contains(typeInfo.Type.Name))
                {
                    i++;
                    ComponentNames.Add(typeInfo.Type.Name);
                    Components.Add(typeInfo.Type.Name, typeInfo.Type);
                }
            }
            Main.LogInfo($"Number of components = {i}");

            background = new Texture2D(64, 64);
            Color grayWithAlpha = new Color(0.2f, 0.2f, 0.2f, 0.6f);
            for (int x = 0; x < 64; x++)
            {
                for (int y = 0; y < 64; y++)
                {
                    background.SetPixel(x, y, grayWithAlpha);
                }
            }
            background.Apply();
        }

        public override void Setup() // This is called evey frame the menu is open, This is also where you draw your UnityGUI
        {
            float windowWidth = 775f;
            float componentListWidth = windowWidth - 40f;
            float queryListWidth = windowWidth / 3f - 15f;

            if (textLeftStyle == null)
            {
                textLeftStyle = new GUIStyle(GUI.skin.label);
                textLeftStyle.alignment = TextAnchor.MiddleLeft;
                textLeftStyle.padding.left = 10;
                textLeftStyle.stretchWidth = true;
            }

            if (textCentreStyle == null)
            {
                textCentreStyle = new GUIStyle(GUI.skin.label);
                textCentreStyle.alignment = TextAnchor.MiddleCenter;
                textCentreStyle.stretchWidth = true;
            }

            if (textMiddleCentreStyle == null)
            {
                textMiddleCentreStyle = new GUIStyle(GUI.skin.label);
                textMiddleCentreStyle.alignment = TextAnchor.MiddleCenter;
                textMiddleCentreStyle.stretchWidth = true;
                textMiddleCentreStyle.stretchHeight = true;
            }

            #region All Components List
            GUILayout.BeginArea(new Rect(10f, 0f, windowWidth, 250f));
            GUI.DrawTexture(new Rect(0f, 0f, windowWidth, 250f), background, ScaleMode.StretchToFill);

            GUILayout.Label("Filter");

            componentFilterText = GUILayout.TextField(componentFilterText);

            filterScrollPosition = GUILayout.BeginScrollView(filterScrollPosition, false, true, GUIStyle.none, GUI.skin.verticalScrollbar);

            for (int i = 0; i < ComponentNames.Count; i++)
            {
                if (string.IsNullOrEmpty(componentFilterText) || ComponentNames[i].ToLower().Contains(componentFilterText.ToLower()))
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(ComponentNames[i], GUILayout.Width(componentListWidth / 2));
                    if (GUILayout.Button("All", GUILayout.Width(componentListWidth / 6)))
                    {
                        AddToQuery(ComponentNames[i], ComponentPresence.All);
                    }
                    if (GUILayout.Button("Any", GUILayout.Width(componentListWidth / 6)))
                    {
                        AddToQuery(ComponentNames[i], ComponentPresence.Any);
                    }
                    if (GUILayout.Button("None", GUILayout.Width(componentListWidth / 6)))
                    {
                        AddToQuery(ComponentNames[i], ComponentPresence.None);
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            #endregion
            
            #region Selected Components
            GUILayout.BeginArea(new Rect(10f, 260f, windowWidth, 100f));
            GUI.DrawTexture(new Rect(0f, 0f, windowWidth, 100f), background, ScaleMode.StretchToFill);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"All ({QueryAll.Count})", textCentreStyle, GUILayout.Width(windowWidth / 3f));
            GUILayout.Label($"Any ({QueryAny.Count})", textCentreStyle, GUILayout.Width(windowWidth / 3f));
            GUILayout.Label($"None ({QueryNone.Count})", textCentreStyle, GUILayout.Width(windowWidth / 3f));
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            DrawQueryList(ref QueryAll, ref queryAllScrollPosition, windowWidth / 3f, queryListWidth);
            DrawQueryList(ref QueryAny, ref queryAnyScrollPosition, windowWidth / 3f, queryListWidth);
            DrawQueryList(ref QueryNone, ref queryNoneScrollPosition, windowWidth / 3f, queryListWidth);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            
            GUILayout.BeginArea(new Rect(10f, 370f, windowWidth, 30f));
            if (GUILayout.Button("Get Entity Query"))
            {
                ECSExplorerController.PerformQuery(
                    ParseQueryArray(QueryAll),
                    ParseQueryArray(QueryAny),
                    ParseQueryArray(QueryNone));
            }
            
            GUILayout.EndArea();
            #endregion

            
            #region Query Results
            List<EntityData> results = ECSExplorerController.GetQueryResult();

            GUILayout.BeginArea(new Rect(10f, 410f, windowWidth, 150f));
            GUI.DrawTexture(new Rect(0f, 0f, windowWidth, 150f), background, ScaleMode.StretchToFill);
            if (results.Count == 0)
            {
                GUILayout.Label("No entities matching query!", textMiddleCentreStyle);
            }
            else
            {
                GUILayout.Label("Entities", textCentreStyle);
                resultsScrollPosition = GUILayout.BeginScrollView(resultsScrollPosition, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);

                for (int i = 0; i < results.Count; i++)
                {
                    if (GUILayout.Button($"Entity {results[i].Entity.Index} ({results[i].NumberOfComponents})"))
                    {
                        watchingEntities.Add(results[i]);
                        watchingEntitiesSelectedComponent.Add(null);
                        watchingEntitiesComponentsScrollPosition.Add(new Vector2(0, 0));
                        watchingEntitiesComponentInfoScrollPosition.Add(new Vector2(0, 0));
                    }
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndArea();
            #endregion

            if (watchingEntities.Count > 0)
            {
                GUILayout.BeginArea(new Rect(10f, 570f, windowWidth, 390f));
                GUI.DrawTexture(new Rect(0f, 0f, windowWidth, 390f), background, ScaleMode.StretchToFill);
                watchingEntitiesScrollPosition = GUILayout.BeginScrollView(watchingEntitiesScrollPosition, false, true, GUIStyle.none, GUI.skin.verticalScrollbar);


                List<int> toRemove = new List<int>();
                for (int i = 0; i < watchingEntities.Count; i++)
                {
                    Vector2 newScrollPosition = watchingEntitiesComponentsScrollPosition[i];
                    Vector2 newScrollPostion2 = watchingEntitiesComponentInfoScrollPosition[i];
                    ComponentType selectedComponentType = watchingEntitiesSelectedComponent[i];
                    if (DrawEntityData(watchingEntities[i], windowWidth, ref newScrollPosition, ref newScrollPostion2, ref selectedComponentType))
                    {
                        toRemove.Add(i);
                    }
                    watchingEntitiesSelectedComponent[i] = selectedComponentType;
                    watchingEntitiesComponentsScrollPosition[i] = newScrollPosition;
                    watchingEntitiesComponentInfoScrollPosition[i] = newScrollPostion2;
                }
                toRemove.Reverse();
                foreach (int i in toRemove)
                {
                    watchingEntities.RemoveAt(i);
                    watchingEntitiesSelectedComponent.RemoveAt(i);
                    watchingEntitiesComponentsScrollPosition.RemoveAt(i);
                    watchingEntitiesComponentInfoScrollPosition.RemoveAt(i);
                }

                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }
        }

        private ComponentType[] ParseQueryArray(List<string> componentNames)
        {
            ComponentType[] componentTypes = new ComponentType[componentNames.Count];
            for (int i = 0; i < componentNames.Count; i++)
            {
                componentTypes[i] = Components[componentNames[i]];
            }
            return componentTypes;
        }

        private enum ComponentPresence
        {
            All,
            Any,
            None
        }

        private void AddToQuery(string componentName, ComponentPresence componentPresence)
        {
            switch (componentPresence)
            {
                case ComponentPresence.All:
                    if (!QueryAll.Contains(componentName))
                        QueryAll.Add(componentName);
                    break;
                case ComponentPresence.Any:
                    if (!QueryAny.Contains(componentName))
                        QueryAny.Add(componentName);
                    break;
                case ComponentPresence.None:
                    if (!QueryNone.Contains(componentName))
                        QueryNone.Add(componentName);
                    break;
                default:
                    break;
            }
        }

        private void DrawQueryList(ref List<string> queryList, ref Vector2 scrollPosition, float scrollWindowWidth, float itemWidth)
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.Width(scrollWindowWidth));
            for (int i = 0; i < queryList.Count; ++i)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(queryList[i], GUILayout.Width(itemWidth * 0.7f));
                if (GUILayout.Button("Remove", GUILayout.Width(itemWidth * 0.3f)))
                {
                    queryList.RemoveAt(i);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }

        private bool DrawEntityData(EntityData entityData, float windowWidth, ref Vector2 componentsScrollPosition, ref Vector2 componentsInfoScrollPosition, ref ComponentType componentType)
        {
            bool closeButtonPressed = false;
            float rowHeight = 300f;

            Entity entity = entityData.Entity;

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{(entity == default ? "" : $"Entity {entity.Index}")}", textCentreStyle, GUILayout.Width(windowWidth * 0.87f));
            if (GUILayout.Button("Close", GUILayout.Width(windowWidth * 0.1f)))
            {
                closeButtonPressed = true;
            }
            GUILayout.Label("", GUILayout.Width(windowWidth * 0.03f));
            GUILayout.EndHorizontal();

            if (entity == default)
            {
                GUILayout.Label("Entity Destroyed!", textMiddleCentreStyle, new GUILayoutOption[] { GUILayout.Height(rowHeight) });
            }
            else
            {
                GUILayout.BeginHorizontal(new GUILayoutOption[] { GUILayout.Height(rowHeight) });

                List<ComponentType> components = ECSExplorerController.GetAllEntityComponents(entity);
                componentsScrollPosition = GUILayout.BeginScrollView(componentsScrollPosition, false, true, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.Width(windowWidth * 0.4f));
                for (int i = 0; i < components.Count; i++)
                {
                    string componentName = components[i].GetManagedType().Name;
                    if (Components.ContainsKey(componentName))
                    {
                        if (GUILayout.Button(componentName))
                        {
                            componentType = components[i];
                        }
                    }
                    else
                    {
                        GUILayout.Label(componentName, textCentreStyle);
                    }
                }
                GUILayout.EndScrollView();

                if (componentType == null)
                {
                    GUILayout.Label("Select a component", textMiddleCentreStyle);
                    componentsInfoScrollPosition = new Vector2(0, 0);
                }
                else if (!components.Contains(componentType))
                {
                    GUILayout.Label("Component is removed!", textMiddleCentreStyle);
                    componentsInfoScrollPosition = new Vector2(0, 0);
                }
                else
                {
                    float componentDataWidth = windowWidth * 0.58f;
                    ComponentData data = ECSExplorerController.instance.GetComponentData(entity, componentType);

                    GUILayout.BeginVertical();
                    GUILayout.Label(componentType.GetManagedType().ToString(), textCentreStyle);

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Name", textCentreStyle, GUILayout.Width(componentDataWidth * 0.23f));
                    GUILayout.Label("Type", textCentreStyle, GUILayout.Width(componentDataWidth * 0.4f));
                    GUILayout.Label("Value", textCentreStyle, GUILayout.Width(componentDataWidth * 0.3f));
                    GUILayout.EndHorizontal();

                    if (data.FieldCount == 0)
                    {
                        GUILayout.Label("No fields", textMiddleCentreStyle);
                    }
                    else
                    {
                        componentsInfoScrollPosition = GUILayout.BeginScrollView(componentsInfoScrollPosition, false, true, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.Width(componentDataWidth));
                        for (int i = 0; i < data.FieldCount; i++)
                        {
                            GUILayout.BeginHorizontal();

                            GUILayout.Label(data.FieldNames[i], textLeftStyle, GUILayout.Width(componentDataWidth * 0.23f));
                            GUILayout.Label(data.FieldTypes[i].ToString(), textLeftStyle, GUILayout.Width(componentDataWidth * 0.38f));
                            GUILayout.Label(data.FieldValues[i].ToString(), textLeftStyle, GUILayout.Width(componentDataWidth * 0.3f));

                            GUILayout.EndHorizontal();
                        }
                        GUILayout.EndScrollView();
                    }
                    
                    GUILayout.EndVertical();
                }

                GUILayout.EndHorizontal();
            }

            return closeButtonPressed;
        }
    }
}
