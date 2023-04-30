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
        private static List<bool> watchingEntitiesDisplayUseHierarchy = new List<bool>();
        private static List<ObjectData> watchingEntitiesComponentObjectData = new List<ObjectData>();
        private static Vector2 watchingEntitiesScrollPosition = new Vector2(0, 0);
        private static List<Vector2> watchingEntitiesComponentsScrollPosition = new List<Vector2>();
        private static List<Vector2> watchingEntitiesComponentInfoScrollPosition = new List<Vector2>();

        public EntityQueryMenu()
        {
            ButtonName = "Entity Query";
        }

        protected override void OnInitialise()
        {
            int i = 0;
            foreach (var typeInfo in TypeManager.AllTypes.Where(componentType => 
                componentType.Category == TypeManager.TypeCategory.ComponentData ||
                componentType.Category == TypeManager.TypeCategory.BufferData ||
                componentType.Category == TypeManager.TypeCategory.ISharedComponentData ||
                componentType.Category == TypeManager.TypeCategory.EntityData
                ))
            {
                if (typeInfo.Type != null && !ComponentNames.Contains(typeInfo.Type.Name))
                {
                    i++;
                    ComponentNames.Add(typeInfo.Type.Name);
                    Components.Add(typeInfo.Type.Name, typeInfo.Type);
                }
            }
            Main.LogInfo($"Number of components = {i}");
        }

        protected override void OnSetup() // This is called evey frame the menu is open, This is also where you draw your UnityGUI
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
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(results[i].LabelTextWithCount, GUILayout.Width(windowWidth * 0.8f)))
                    {
                        watchingEntities.Add(results[i]);
                        watchingEntitiesComponentsScrollPosition.Add(new Vector2(0, 0));
                        watchingEntitiesComponentInfoScrollPosition.Add(new Vector2(0, 0));

                        bool useHierarchy = true;
                        if (watchingEntitiesDisplayUseHierarchy.Count > 0)
                        {
                            useHierarchy = watchingEntitiesDisplayUseHierarchy.Last();
                        }
                        watchingEntitiesDisplayUseHierarchy.Add(useHierarchy);
                        watchingEntitiesComponentObjectData.Add(null);
                    }
                    if (GUILayout.Button("Destroy"))
                    {
                        TryRemoveWatchingEntity(results[i]);
                        ECSExplorerController.Destroy(results[i]);
                        results.RemoveAt(i);
                        i--;
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndArea();
            #endregion

            if (watchingEntities.Count > 0)
            {
                GUILayout.BeginArea(new Rect(10f, 570f, windowWidth, 490f));
                GUI.DrawTexture(new Rect(0f, 0f, windowWidth, 490f), Background, ScaleMode.StretchToFill);
                watchingEntitiesScrollPosition = GUILayout.BeginScrollView(watchingEntitiesScrollPosition, false, true, GUIStyle.none, GUI.skin.verticalScrollbar);


                List<int> toRemove = new List<int>();
                for (int i = 0; i < watchingEntities.Count; i++)
                {
                    Vector2 newScrollPosition = watchingEntitiesComponentsScrollPosition[i];
                    Vector2 newScrollPostion2 = watchingEntitiesComponentInfoScrollPosition[i];
                    EntityData entityData = watchingEntities[i];
                    bool useHierarchy = watchingEntitiesDisplayUseHierarchy[i];
                    ObjectData objectData = watchingEntitiesComponentObjectData[i];
                    if (DrawEntityData(ref entityData, windowWidth, ref newScrollPosition, ref newScrollPostion2, ref useHierarchy, ref objectData))
                    {
                        entityData.SelectedComponentType = null;
                        toRemove.Add(i);
                    }
                    watchingEntities[i] = entityData;
                    watchingEntitiesComponentsScrollPosition[i] = newScrollPosition;
                    watchingEntitiesComponentInfoScrollPosition[i] = newScrollPostion2;
                    watchingEntitiesDisplayUseHierarchy[i] = useHierarchy;
                    watchingEntitiesComponentObjectData[i] = objectData;
                }
                toRemove.Reverse();
                foreach (int i in toRemove)
                {
                    RemoveWatchingEntityAt(i);
                }

                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }
        }

        private bool TryRemoveWatchingEntity(EntityData entityData)
        {
            if (watchingEntities.Contains(entityData))
            {
                RemoveWatchingEntityAt(watchingEntities.IndexOf(entityData));
                return true;
            }
            return false;
        }

        private void RemoveWatchingEntityAt(int i)
        {
            watchingEntities.RemoveAt(i);
            watchingEntitiesComponentsScrollPosition.RemoveAt(i);
            watchingEntitiesComponentInfoScrollPosition.RemoveAt(i);
            watchingEntitiesDisplayUseHierarchy.RemoveAt(i);
            watchingEntitiesComponentObjectData.RemoveAt(i);
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

        private bool DrawEntityData(ref EntityData entityData, float windowWidth, ref Vector2 componentsScrollPosition, ref Vector2 componentsInfoScrollPosition, ref bool useHierarchy, ref ObjectData objectData)
        {
            bool closeButtonPressed = false;

            float scrollBarWidth = 20f;
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();

            float rowHeight = 300f;
            Entity entity = entityData.Entity;

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{(entity == default ? "" : entityData.LabelText)}", LabelCentreStyle, GUILayout.Width(windowWidth * 0.76f));
            if (GUILayout.Button(useHierarchy? "Hierarchy" : "Table", GUILayout.Width(windowWidth * 0.1f)))
            {
                useHierarchy = !useHierarchy;
                componentsInfoScrollPosition = new Vector2(0, 0);
            }
            if (GUILayout.Button("Close", GUILayout.Width(windowWidth * 0.1f)))
            {
                closeButtonPressed = true;
            }
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
                            entityData.SelectedComponentType = components[i];
                            entityData.SelectedBufferIndex = 0;
                            objectData = null;
                        }
                    }
                    else
                    {
                        GUILayout.Label(componentName, LabelCentreStyle);
                    }
                }
                GUILayout.EndScrollView();

                if (entityData.SelectedComponentType == default)
                {
                    GUILayout.Label("Select a component", LabelMiddleCentreStyle);
                    componentsInfoScrollPosition = new Vector2(0, 0);
                }
                else if (!components.Contains(entityData.SelectedComponentType))
                {
                    GUILayout.Label("Component is removed!", LabelMiddleCentreStyle);
                    componentsInfoScrollPosition = new Vector2(0, 0);
                }
                else
                {
                    float componentDataWidth = windowWidth * 0.58f;
                    ComponentData data = ECSExplorerController.instance.GetComponentData(entity, entityData.SelectedComponentType, ref entityData.SelectedBufferIndex, ref entityData.BufferLength);

                    GUILayout.BeginVertical();
                    GUILayout.Label($"{entityData.SelectedComponentType.GetManagedType()} ({data.Classification})", LabelCentreStyle);


                    if (data.State == ActionState.Error)
                    {
                        string noFieldsString;
                        if (CompatibilityReporter.NO_FIELDS.TryGetValue(data.Type, out string compatibilityErrorString) || data.Classification == ComponentTypeClassification.None)
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
                        noFieldsString += $"\nPlease close and perform another entity query. If the error persists, please contact the mod developer ({Main.MOD_AUTHOR}) and provide your Player.log file.";
                        GUILayout.Label(noFieldsString, LabelMiddleCentreStyle);
                    }
                    else if (data.FieldCount == 0)
                    {
                        GUILayout.Label("No fields", LabelMiddleCentreStyle);
                    }
                    else if (data.Classification == ComponentTypeClassification.Buffer && entityData.SelectedBufferIndex == -1)
                    {
                        GUILayout.Label("No buffer elements", LabelMiddleCentreStyle);
                    }
                    else
                    {
                        switch (data.Classification)
                        {
                            case ComponentTypeClassification.SharedData:
                            case ComponentTypeClassification.Data:
                                DrawComponentData(data, entityData, ref objectData, useHierarchy, componentDataWidth, ref componentsInfoScrollPosition);
                                break;
                            case ComponentTypeClassification.Buffer:
                                DrawBufferData(data, entityData, ref objectData, useHierarchy, ref entityData.SelectedBufferIndex, entityData.BufferLength, componentDataWidth, ref componentsInfoScrollPosition);
                                break;
                        }
                    }
                    GUILayout.EndVertical();

                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
            GUILayout.Label("", GUILayout.Width(scrollBarWidth));
            GUILayout.EndHorizontal();
            return closeButtonPressed;
        }

        private void DrawComponentData(ComponentData data, EntityData entityData, ref ObjectData objectData, bool useHierarchy, float componentDataWidth, ref Vector2 componentsInfoScrollPosition)
        {
            if (useHierarchy)
            {
                DrawHierarchy(ref objectData, entityData, data, ref componentsInfoScrollPosition);
            }
            else
            {
                DrawComponentFieldTable(data, componentDataWidth, objectData, ref componentsInfoScrollPosition);
            }
        }

        private void DrawBufferData(ComponentData data, EntityData entityData, ref ObjectData objectData, bool useHierarchy, ref int selectedBufferIndex, in int bufferCount, float componentDataWidth, ref Vector2 componentsInfoScrollPosition)
        {
            bool indexChanged = false;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Previous", GUILayout.Width(componentDataWidth * 0.2f)))
            {
                indexChanged = true;
                selectedBufferIndex--;
            }
            GUILayout.Label($"Element {selectedBufferIndex + 1}/{bufferCount}", LabelCentreStyle, GUILayout.Width(componentDataWidth * 0.6f));
            if (GUILayout.Button("Next", GUILayout.Width(componentDataWidth * 0.2f)))
            {
                indexChanged = true;
                selectedBufferIndex++;
            }
            GUILayout.EndHorizontal();

            if (useHierarchy)
            {
                DrawHierarchy(ref objectData, entityData, data, ref componentsInfoScrollPosition);
            }
            else
            {
                DrawComponentFieldTable(data, componentDataWidth, objectData, ref componentsInfoScrollPosition);
            }

            if (indexChanged)
            {
                objectData = null;
            }
        }

        private void DrawHierarchy(ref ObjectData objectData, EntityData entityData, ComponentData data, ref Vector2 componentsInfoScrollPosition)
        {
            GUILayout.Label("------------------ Note: Contents of hierarchy does not auto-update ------------------", LabelCentreStyle);
            if (objectData == null)
            {
                objectData = new ObjectData($"{entityData.SelectedComponentType.GetManagedType()} ({data.Classification})", data.Instance);
            }
            objectData = DrawObjectHierarchy(objectData, ref componentsInfoScrollPosition);
        }

        private void DrawComponentFieldTable(ComponentData data, float componentDataWidth, ObjectData objectData, ref Vector2 componentsInfoScrollPosition)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name", LabelCentreStyle, GUILayout.Width(componentDataWidth * 0.23f));
            GUILayout.Label("Type", LabelCentreStyle, GUILayout.Width(componentDataWidth * 0.4f));
            GUILayout.Label("Value", LabelCentreStyle, GUILayout.Width(componentDataWidth * 0.3f));
            GUILayout.EndHorizontal();

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
    }
}
