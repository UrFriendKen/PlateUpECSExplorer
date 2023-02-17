using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEngine;

namespace KitchenECSExplorer
{
    public class EntityQueryMenu : PlateUpExplorerMenu
    {
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

        public EntityQueryMenu()
        {
            ButtonName = "Entity Query";
        }

        public override void OnInitialise()
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
        }

        public override void OnSetup() // This is called evey frame the menu is open, This is also where you draw your UnityGUI
        {
            float windowWidth = 775f;
            float componentListWidth = windowWidth - 40f;
            float queryListWidth = windowWidth / 3f - 15f;

            #region All Components List
            GUILayout.BeginArea(new Rect(10f, 0f, windowWidth, 250f));
            GUI.DrawTexture(new Rect(0f, 0f, windowWidth, 250f), Background, ScaleMode.StretchToFill);

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
            GUI.DrawTexture(new Rect(0f, 0f, windowWidth, 100f), Background, ScaleMode.StretchToFill);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"All ({QueryAll.Count})", LabelCentreStyle, GUILayout.Width(windowWidth / 3f));
            GUILayout.Label($"Any ({QueryAny.Count})", LabelCentreStyle, GUILayout.Width(windowWidth / 3f));
            GUILayout.Label($"None ({QueryNone.Count})", LabelCentreStyle, GUILayout.Width(windowWidth / 3f));
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
            GUI.DrawTexture(new Rect(0f, 0f, windowWidth, 150f), Background, ScaleMode.StretchToFill);
            if (results.Count == 0)
            {
                GUILayout.Label("No entities matching query!", LabelMiddleCentreStyle);
            }
            else
            {
                GUILayout.Label($"Entities ({results.Count})", LabelCentreStyle);
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
                GUI.DrawTexture(new Rect(0f, 0f, windowWidth, 390f), Background, ScaleMode.StretchToFill);
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
            GUILayout.Label($"{(entity == default ? "" : $"Entity {entity.Index}")}", LabelCentreStyle, GUILayout.Width(windowWidth * 0.87f));
            if (GUILayout.Button("Close", GUILayout.Width(windowWidth * 0.1f)))
            {
                closeButtonPressed = true;
            }
            GUILayout.Label("", GUILayout.Width(windowWidth * 0.03f));
            GUILayout.EndHorizontal();

            if (entity == default)
            {
                GUILayout.Label("Entity Destroyed!", LabelMiddleCentreStyle, new GUILayoutOption[] { GUILayout.Height(rowHeight) });
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
                        GUILayout.Label(componentName, LabelCentreStyle);
                    }
                }
                GUILayout.EndScrollView();

                if (componentType == null)
                {
                    GUILayout.Label("Select a component", LabelMiddleCentreStyle);
                    componentsInfoScrollPosition = new Vector2(0, 0);
                }
                else if (!components.Contains(componentType))
                {
                    GUILayout.Label("Component is removed!", LabelMiddleCentreStyle);
                    componentsInfoScrollPosition = new Vector2(0, 0);
                }
                else
                {
                    float componentDataWidth = windowWidth * 0.58f;
                    ComponentData data = ECSExplorerController.instance.GetComponentData(entity, componentType);

                    GUILayout.BeginVertical();
                    GUILayout.Label(componentType.GetManagedType().ToString(), LabelCentreStyle);

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Name", LabelCentreStyle, GUILayout.Width(componentDataWidth * 0.23f));
                    GUILayout.Label("Type", LabelCentreStyle, GUILayout.Width(componentDataWidth * 0.4f));
                    GUILayout.Label("Value", LabelCentreStyle, GUILayout.Width(componentDataWidth * 0.3f));
                    GUILayout.EndHorizontal();


                    if (data.State == ActionState.Error)
                    {
                        string noFieldsString;
                        if (CompatibilityReporter.NO_FIELDS.TryGetValue(data.Type, out string compatibilityErrorString))
                        {
                            noFieldsString = $"Unable to obtain field information!\n{compatibilityErrorString}";
                            Main.LogError("--- EntityQueryFieldsNotFound ---");
                            Main.LogError($"Reported causes of error = {compatibilityErrorString}");
                            Main.LogError($"Component Type = {data.Type}");
                            Main.LogError($"FieldCount = {data.FieldCount}");
                        }
                        else
                        {
                            noFieldsString = $"An unknown error has occured!";
                            Main.LogError("--- EntityQueryFieldsNotFound ---");
                            Main.LogError($"Component Type = {data.Type}");
                            Main.LogError($"FieldCount = {data.FieldCount}");
                        }
                        noFieldsString += $"\nIf the error persists, please contact the mod developer ({Main.MOD_AUTHOR}) and provide your Player.log file.";
                        GUILayout.Label(noFieldsString, LabelMiddleCentreStyle);
                    }
                    else if (data.FieldCount == 0)
                    {
                        GUILayout.Label("No fields", LabelMiddleCentreStyle);
                    }
                    else
                    {
                        componentsInfoScrollPosition = GUILayout.BeginScrollView(componentsInfoScrollPosition, false, true, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.Width(componentDataWidth));
                        for (int i = 0; i < data.FieldCount; i++)
                        {
                            GUILayout.BeginHorizontal();

                            GUILayout.Label(data.FieldNames[i], LabelLeftStyle, GUILayout.Width(componentDataWidth * 0.23f));
                            GUILayout.Label(data.FieldTypes[i].ToString(), LabelLeftStyle, GUILayout.Width(componentDataWidth * 0.38f));
                            GUILayout.Label(data.FieldValues[i].ToString(), LabelLeftStyle, GUILayout.Width(componentDataWidth * 0.3f));

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
