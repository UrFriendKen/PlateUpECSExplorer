using Kitchen;
using KitchenECSExplorer.Persistence;
using KitchenECSExplorer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEngine;

namespace KitchenECSExplorer
{
    public class EntityQueryMenu : PlateUpExplorerMenu
    {
        private string componentFilterText = "";
        private Vector2 filterScrollPosition = new Vector2(0, 0);

        public Dictionary<string, ComponentType> Components = new Dictionary<string, ComponentType>();
        public List<(string name, string key)> ComponentsList = new List<(string name, string key)>();
        public Dictionary<string, string> ComponentNames = new Dictionary<string, string>();

        private List<string> QueryAll = new List<string>();
        private Vector2 queryAllScrollPosition = new Vector2(0, 0);
        private List<string> QueryAny = new List<string>();
        private Vector2 queryAnyScrollPosition = new Vector2(0, 0);
        private List<string> QueryNone = new List<string>();
        private Vector2 queryNoneScrollPosition = new Vector2(0, 0);

        private Vector2 resultsScrollPosition = new Vector2(0, 0);

        private List<EntityData> watchingEntities = new List<EntityData>();
        private List<bool> watchingEntitiesDisplayUseHierarchy = new List<bool>();
        private List<ObjectData> watchingEntitiesComponentObjectData = new List<ObjectData>();
        private Vector2 watchingEntitiesScrollPosition = new Vector2(0, 0);
        private List<Vector2> watchingEntitiesComponentsScrollPosition = new List<Vector2>();
        private List<Vector2> watchingEntitiesComponentInfoScrollPosition = new List<Vector2>();

        private const string FAVOURITE_QUERIES_FILENAME = "entityQueryFavourites.puexplorersave";
        private List<PersistentEntityQuery> favouriteQueries = new List<PersistentEntityQuery>();
        private Vector2 favouriteQueriesScrollPosition = new Vector2(0, 0);
        private string newFavouriteName = string.Empty;


        // Temp while OnInit does not work
        private bool _isInit = false;



        public EntityQueryMenu()
        {
            ButtonName = "Entity Query";
        }

        protected override void OnInitialise()
        {
            if (_isInit)
                return;
            int i = 0;
            foreach (var typeInfo in TypeManager.AllTypes.Where(componentType => 
                componentType.Category == TypeManager.TypeCategory.ComponentData ||
                componentType.Category == TypeManager.TypeCategory.BufferData ||
                componentType.Category == TypeManager.TypeCategory.ISharedComponentData ||
                componentType.Category == TypeManager.TypeCategory.EntityData
                ))
            {
                if (typeInfo.Type != null && !Components.ContainsKey(typeInfo.Type.AssemblyQualifiedName))
                {
                    i++;
                    Type type = typeInfo.Type;
                    string displayName = ReflectionUtils.GetReadableTypeName(type);
                    ComponentsList.Add((displayName, type.AssemblyQualifiedName));
                    ComponentNames[typeInfo.Type.AssemblyQualifiedName] = displayName;
                    Components.Add(typeInfo.Type.AssemblyQualifiedName, typeInfo.Type);
                }
            }
            Main.LogInfo($"Number of components = {i}");
            LoadFavouriteQueries();
            _isInit = true;
        }

        protected override void OnSetup() // This is called evey frame the menu is open, This is also where you draw your UnityGUI
        {
            OnInitialise();

            float windowWidth = 775f;
            float componentListWidth = windowWidth - 40f;
            float queryListWidth = windowWidth / 3f - 15f;

            #region All Components List
            GUILayout.BeginArea(new Rect(10f, 0f, windowWidth, 250f));
            GUI.DrawTexture(new Rect(0f, 0f, windowWidth, 250f), Background, ScaleMode.StretchToFill);

            GUILayout.Label("Filter");

            List<(string name, string key)> matchingComponents = string.IsNullOrEmpty(componentFilterText) ?
                ComponentsList : GetFuzzyMatches(ComponentsList, componentFilterText, x => x.name);

            componentFilterText = DoTabCompleteTextField("componentFilter", componentFilterText, matchingComponents.Select(item => item.name));

            filterScrollPosition = GUILayout.BeginScrollView(filterScrollPosition, false, true, GUIStyle.none, GUI.skin.verticalScrollbar);
            foreach (var component in matchingComponents)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(component.name, GUILayout.Width(componentListWidth / 2));
                if (GUILayout.Button("All", GUILayout.Width(componentListWidth / 6)))
                {
                    AddToQuery(component.key, ComponentPresence.All);
                }
                if (GUILayout.Button("Any", GUILayout.Width(componentListWidth / 6)))
                {
                    AddToQuery(component.key, ComponentPresence.Any);
                }
                if (GUILayout.Button("None", GUILayout.Width(componentListWidth / 6)))
                {
                    AddToQuery(component.key, ComponentPresence.None);
                }
                GUILayout.EndHorizontal();
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

            GUILayout.BeginArea(new Rect(10f, 370f, windowWidth, 130f));
            GUI.DrawTexture(new Rect(0f, 0f, windowWidth, 130f), Background, ScaleMode.StretchToFill);
            GUILayout.Label($"Favourites", LabelCentreStyle);
            if (DrawFavouriteQueriesList(ref favouriteQueries, ref favouriteQueriesScrollPosition, windowWidth, windowWidth - 15f, out int selectedFavouriteQueryIndex))
            {
                PersistentEntityQuery selectedFavouriteQuery = favouriteQueries[selectedFavouriteQueryIndex];
                LoadQueryByPresence(selectedFavouriteQuery.All, ComponentPresence.All);
                LoadQueryByPresence(selectedFavouriteQuery.Any, ComponentPresence.Any);
                LoadQueryByPresence(selectedFavouriteQuery.None, ComponentPresence.None);
                void LoadQueryByPresence(PersistentList<PersistentString> list, ComponentPresence presence)
                {
                    ClearQuery(presence);
                    foreach (PersistentString item in list.Items)
                    {
                        if (!Components.ContainsKey(item))
                            continue;
                        AddToQuery(item, presence);
                    }
                }
            }
            GUILayout.BeginHorizontal();
            newFavouriteName = GUILayout.TextField(newFavouriteName, GUILayout.Width(windowWidth * 0.7f));
            if (GUILayout.Button("Add Favourite") && !string.IsNullOrEmpty(newFavouriteName.Trim()))
            {
                favouriteQueries.Add(new PersistentEntityQuery(newFavouriteName.Trim(), QueryAll, QueryAny, QueryNone));
                SaveFavouriteQueries();
                newFavouriteName = string.Empty;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(10f, 510f, windowWidth, 30f));
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

            GUILayout.BeginArea(new Rect(10f, 550f, windowWidth, 140f));
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
                GUILayout.BeginArea(new Rect(10f, 700f, windowWidth, 360f));
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

        private void SaveFavouriteQueries()
        {
            new PersistentList<PersistentEntityQuery>(favouriteQueries).Save(FAVOURITE_QUERIES_FILENAME);
        }

        private void LoadFavouriteQueries()
        {
            PersistentList<PersistentEntityQuery> loaded = new PersistentList<PersistentEntityQuery>();
            if (!loaded.Load(FAVOURITE_QUERIES_FILENAME))
                return;
            favouriteQueries = loaded.Items;
        }

        private void AddToQuery(string componentKey, ComponentPresence componentPresence)
        {
            switch (componentPresence)
            {
                case ComponentPresence.All:
                    if (!QueryAll.Contains(componentKey))
                        QueryAll.Add(componentKey);
                    break;
                case ComponentPresence.Any:
                    if (!QueryAny.Contains(componentKey))
                        QueryAny.Add(componentKey);
                    break;
                case ComponentPresence.None:
                    if (!QueryNone.Contains(componentKey))
                        QueryNone.Add(componentKey);
                    break;
                default:
                    break;
            }
        }

        private void ClearQuery(ComponentPresence componentPresence)
        {
            switch (componentPresence)
            {
                case ComponentPresence.All:
                    QueryAll.Clear();
                    break;
                case ComponentPresence.Any:
                    QueryAny.Clear();
                    break;
                case ComponentPresence.None:
                    QueryNone.Clear();
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
                GUILayout.Label(ComponentNames[queryList[i]], GUILayout.Width(itemWidth * 0.7f));
                if (GUILayout.Button("Remove", GUILayout.Width(itemWidth * 0.3f)))
                {
                    queryList.RemoveAt(i);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }

        private bool DrawFavouriteQueriesList(ref List<PersistentEntityQuery> favouriteQueries, ref Vector2 scrollPosition, float scrollWindowWidth, float itemWidth, out int selectedIndex)
        {
            selectedIndex = -1;
            bool hasSelection = false;
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.Width(scrollWindowWidth));
            for (int i = 0; i < favouriteQueries.Count; ++i)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(favouriteQueries[i].Name, GUILayout.Width(itemWidth * 0.6f));
                if (GUILayout.Button("Load", GUILayout.Width(itemWidth * 0.2f)))
                {
                    selectedIndex = i;
                    hasSelection = true;
                }
                if (GUILayout.Button("Delete", GUILayout.Width(itemWidth * 0.2f)))
                {
                    favouriteQueries.RemoveAt(i);
                    SaveFavouriteQueries();
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            return hasSelection;
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
                    string componentKey = components[i].GetManagedType().AssemblyQualifiedName;
                    if (Components.ContainsKey(componentKey))
                    {
                        if (GUILayout.Button(ComponentNames[componentKey]))
                        {
                            entityData.SelectedComponentType = components[i];
                            entityData.SelectedBufferIndex = 0;
                            objectData = null;
                        }
                    }
                    else
                    {
                        GUILayout.Label(components[i].GetManagedType().Name, LabelCentreStyle);
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
                        noFieldsString += $"\nPlease close and perform another entity query. If the error persists, please contact the mod developer (IcedMilo) and provide your Player.log file.";
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
            GUILayout.Label("------------------ Note: Content of hierarchy does not auto-update ------------------", LabelCentreStyle);
            if (objectData == null)
            {
                objectData = new ObjectData($"{entityData.SelectedComponentType.GetManagedType()} ({data.Classification})", data.Instance);
            }
            objectData = DrawObjectHierarchy(objectData, ref componentsInfoScrollPosition);
        }

        private void DrawComponentFieldTable(ComponentData data, float componentDataWidth, ObjectData objectData, ref Vector2 componentsInfoScrollPosition)
        {
            float nameWidth = componentDataWidth * 0.23f;
            float typeWidth = componentDataWidth * 0.4f;
            float valueWidth = componentDataWidth * 0.3f;

            GUILayout.BeginHorizontal();
            GUILayout.Label("Name", LabelCentreStyle, GUILayout.Width(nameWidth));
            GUILayout.Label("Type", LabelCentreStyle, GUILayout.Width(typeWidth));
            GUILayout.Label("Value", LabelCentreStyle, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            componentsInfoScrollPosition = GUILayout.BeginScrollView(componentsInfoScrollPosition, false, true, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.Width(componentDataWidth));
            for (int i = 0; i < data.FieldCount; i++)
            {
                DrawComponentFieldTableLine(data, i, componentDataWidth, nameWidth, typeWidth, valueWidth);
            }
            GUILayout.EndScrollView();
        }

        private void DrawComponentFieldTableLine(ComponentData data, int fieldIndex, float componentDataWidth, float nameWidth, float typeWidth, float valueWidth)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(data.FieldNames[fieldIndex], LabelLeftStyle, GUILayout.Width(nameWidth));
            GUILayout.Label(data.FieldTypes[fieldIndex].ToString(), LabelLeftStyle, GUILayout.Width(typeWidth));
            GUILayout.Label(GetCustomValueString(data.FieldValues[fieldIndex]), LabelLeftStyle, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private string GetCustomValueString(object value)
        {
            if (value is SystemReference systemReference)
            {
                string systemName = SystemReference.GetName(systemReference);
                if (!string.IsNullOrEmpty(systemName))
                    return $"{SystemReference.GetName(systemReference)} ({(int)systemReference})";
            }
            return value.ToString();
        }
    }
}
