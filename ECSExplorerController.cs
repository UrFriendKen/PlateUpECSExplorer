using Kitchen;
using Kitchen.ShopBuilder;
using KitchenData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace KitchenECSExplorer
{
    internal enum ActionState
    {
        BufferEmpty,
        Success,
        Error
    }

    internal enum ComponentTypeClassification
    {
        None,
        Data,
        SharedData,
        Buffer
    }


    internal struct EntityData
    {
        public Entity Entity;
        public string LabelText;
        public string LabelTextWithCount;
        public int NumberOfComponents;
        public List<ComponentType> ComponentTypes;
        
        public ComponentType SelectedComponentType;
        public int SelectedBufferIndex;
        public int BufferLength;
    }

    internal struct ComponentData
    {
        public object Instance;
        public ComponentType Type;
        public ComponentTypeClassification Classification;
        public ActionState State;
        public int FieldCount;
        public List<string> FieldNames;
        public List<Type> FieldTypes;
        public List<object> FieldValues;
    }

    internal class ECSExplorerController : GameSystemBase
    {
        private static EntityQuery Query = default;

        private static List<EntityData> entityData = new List<EntityData>();

        internal static ECSExplorerController instance;

        private static readonly MethodInfo mGetComponentData = typeof(EntityManager).GetMethod("GetComponentData");
        private static readonly MethodInfo mGetBuffer = typeof(EntityManager).GetMethod("GetBuffer");
        private static readonly MethodInfo mGetSharedComponentData = typeof(EntityManager).GetMethod("GetSharedComponentData", new Type[] { typeof(Entity) });
        private static readonly Type tDynamicBuffer = typeof(DynamicBuffer<>);

        protected override void OnUpdate()
        {
            if (instance == null)
            {
                instance = this;
            }

            if (Query != default)
            {
                entityData.Clear();
                NativeArray<Entity> entities = Query.ToEntityArray(Allocator.Temp);
                Query = default;

                foreach (Entity entity in entities)
                {
                    List<ComponentType> components = EntityManager.GetComponentTypes(entity).ToList();
                    int count = components.Count();
                    GetLabelText(entity, count, components, out string labelWithoutCount, out string labelWithCount);
                    entityData.Add(new EntityData()
                    {
                        Entity = entity,
                        LabelText = labelWithoutCount,
                        LabelTextWithCount = labelWithCount,
                        ComponentTypes = components,
                        NumberOfComponents = count
                    });
                }
                entities.Dispose();
            }
        }

        protected void GetLabelText(Entity entity, int componentCount, List<ComponentType> components, out string textWithoutComponentCount, out string textWithComponentCount)
        {
            string name = null;
            if (Require(entity, out CPlayer player))
            {
                name = $"Player - {Players.Main.Get(player.ID).Username}";
            }
            else if (Require(entity, out CExpGrant expGrant))
            {
                name = $"Exp Grant";
            }
            else if (Require(entity, out CDishUpgrade dishUpgrade) && GameData.Main.TryGet(dishUpgrade.DishID, out Dish dishGDO))
            {
                name = $"Dish Upgrade - {dishGDO.Name}";
            }
            else if (Require(entity, out CSettingUpgrade settingUpgrade) && GameData.Main.TryGet(settingUpgrade.SettingID, out RestaurantSetting restaurantSettingGDO))
            {
                name = $"Setting Upgrade - {restaurantSettingGDO.name}";
            }
            else if (Require(entity, out CLayoutUpgrade layoutUpgrade) && GameData.Main.TryGet(layoutUpgrade.LayoutID, out LayoutProfile layoutProfileGDO))
            {
                name = $"Layout Upgrade - {layoutProfileGDO.name}";
            }
            else if (Has<CUpgradeExtraLayout>(entity))
            {
                name = $"Franchise Upgrade - Extra Layout Choice";
            }
            else if (Has<CUpgradeExtraDish>(entity))
            {
                name = $"Franchise Upgrade - Extra Dish Choice";
            }
            else if (Require(entity, out CCrateAppliance crateAppliance) && GameData.Main.TryGet(crateAppliance.Appliance, out Appliance applianceGDO))
            {
                name = $"Crate - {applianceGDO.name}";
            }
            else if (Require(entity, out CUpgrade upgrade))
            {
                name = $"{(upgrade.IsFromLevel ? "Level" : string.Empty)}Upgrade";
            }
            else if (Require(entity, out CFranchiseTier franchiseTier))
            {
                string franchiseName = null;
                if (Require(entity, out CFranchiseItem franchiseItem))
                {
                    franchiseName = $" ({franchiseItem.Name})";
                }
                name = $"Stored Franchise - Tier {franchiseTier.Tier}{franchiseName}";
            }
            else if (Has<RebuildKitchen.CFranchiseKitchenSlot>(entity))
            {
                name = $"Franchise Kitchen Slot";
            }
            else if (HasBuffer<CLayoutAppliancePlacement>(entity))
            {
                name = $"Map Blueprint Data";
            }
            else if (Require(entity, out CProgressionUnlock progressionUnlock) && GameData.Main.TryGet(progressionUnlock.ID, out Unlock unlockGDO))
            {
                name = $"Active Card - {unlockGDO.name}";
            }
            else if (Require(entity, out CLetterIngredient letterIngredient) && GameData.Main.TryGet(letterIngredient.IngredientID, out Item itemGDO))
            {
                name = $"Parcel - {itemGDO.name}";
            }
            else if (Require(entity, out CLetterAppliance letterAppliance) && GameData.Main.TryGet(letterAppliance.ApplianceID, out applianceGDO))
            {
                name = $"Parcel - {applianceGDO.name}";
            }
            else if (Require(entity, out CLetterBlueprint letterBlueprint) && GameData.Main.TryGet(letterBlueprint.ApplianceID, out applianceGDO))
            {
                name = $"Letter - {applianceGDO.name}";
            }
            else if (Require(entity, out CAppliance appliance) && GameData.Main.TryGet(appliance.ID, out applianceGDO))
            {
                name = $"Appliance - {applianceGDO.name}";
            }
            else if (Require(entity, out CItem item) && GameData.Main.TryGet(item.ID, out itemGDO))
            {
                name = $"Item - {itemGDO.name}";
            }
            else if (Require(entity, out CMenuItem menuItem) && GameData.Main.TryGet(menuItem.SourceDish, out dishGDO))
            {
                name = $"Menu Item - {dishGDO.name}";
            }
            else if (Require(entity, out CShopBuilderOption shopOption) && GameData.Main.TryGet(shopOption.Appliance, out applianceGDO))
            {
                name = $"Shop Option ({applianceGDO.name})";
            }
            else if (Require(entity, out CRequiresView view))
            {
                name = $"View - {view.Type}";
            }
            else if (Require(entity, out CHasIndicator hasIndicator))
            {
                name = $"Indicator - {hasIndicator.IndicatorType}";
            }
            else
            {
                List<(string, string)> singletonComponentTypes = components.Select(x => (x.GetManagedType().Name, x.GetManagedType().Namespace)).Where(x => x.Name.ToUpperInvariant().StartsWith("S")).ToList();
                if (singletonComponentTypes.Count > 0)
                {
                    List<string> parsedSingletonNames = new List<string>();
                    foreach ((string, string) singleton in singletonComponentTypes)
                    {
                        parsedSingletonNames.Add(singleton.Item1.ToLowerInvariant() != "sstate" ? singleton.Item1 : $"SState - {singleton.Item2}");
                    }
                    name = String.Join(", ", parsedSingletonNames);
                }
                else if (componentCount == 1)
                {
                    name = $"{components[0].GetManagedType().Name}";
                }
            }

            string entityIndex = $"Entity {entity.Index}";
            textWithoutComponentCount = $"{entityIndex}";
            textWithComponentCount = $"{entityIndex} - {componentCount}";
            if (name != null)
            {
                string formattedName = $" ({name})";
                textWithoutComponentCount += formattedName;
                textWithComponentCount += formattedName;
            }
        }

        public static void PerformQuery(ComponentType[] all = null, ComponentType[] any = null, ComponentType[] none = null)
        {
            bool hasData = false;
            QueryHelper queryHelper = new QueryHelper();
            if (all != null)
            {
                queryHelper.All(all);
                hasData = true;
            }
            if (any != null)
            {
                queryHelper.Any(any);
                hasData = true;
            }
            if (none != null)
            {
                queryHelper.None(none);
                hasData = true;
            }

            if (!hasData)
            {
                Query = default;
                ClearResult();
            }
            Query = instance.GetEntityQuery(queryHelper);
        }

        public static void ClearResult()
        {
            entityData.Clear();
        }

        public static List<EntityData> GetQueryResult()
        {
            return entityData;
        }

        public static void Destroy(EntityData entityData)
        {
            instance.EntityManager.DestroyEntity(entityData.Entity);
        }

        public static List<ComponentType> GetAllEntityComponents(Entity entity)
        {
            return instance.EntityManager.GetComponentTypes(entity).ToList();
        }

        public ComponentData GetComponentData(Entity entity, ComponentType componentType, ref int selectedBufferIndex, ref int bufferLength)
        {
            ComponentData data = new ComponentData();
            data.Instance = null;
            Type type = componentType.GetManagedType();
            data.Type = type;
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            data.FieldCount = fields.Count();
            data.FieldNames = new List<string>();
            data.FieldTypes = new List<Type>();
            data.FieldValues = new List<object>();
            data.Classification = ComponentTypeClassification.None;
            int fieldDataObtainedCount = 0;
            if (EntityManager.HasComponent(entity, componentType))
            {
                MethodInfo genericMethod = null;
                if (componentType.IsSharedComponent)
                {
                    data.Classification = ComponentTypeClassification.SharedData;
                    genericMethod = mGetSharedComponentData.MakeGenericMethod(type);
                }
                else if (componentType.IsBuffer)
                {
                    data.Classification = ComponentTypeClassification.Buffer;
                    genericMethod = mGetBuffer.MakeGenericMethod(type);
                }
                else if (type.IsValueType && !type.IsPrimitive && !type.IsEnum)
                {
                    data.Classification = ComponentTypeClassification.Data;
                    genericMethod = mGetComponentData.MakeGenericMethod(type);
                }

                if (genericMethod != null)
                {
                    object componentInstance = genericMethod.Invoke(EntityManager, new object[] { entity });
                    switch (data.Classification)
                    {
                        case ComponentTypeClassification.SharedData:
                        case ComponentTypeClassification.Data:
                            data.Instance = componentInstance;
                            break;
                        case ComponentTypeClassification.Buffer:
                            dynamic buffer = componentInstance;
                            bufferLength = buffer.Length;
                            if (!(buffer.Length > 0))
                            {
                                selectedBufferIndex = -1;
                                data.Instance = null;
                            }
                            else
                            {
                                selectedBufferIndex = Mathf.Clamp(selectedBufferIndex, 0, buffer.Length - 1);
                                data.Instance = buffer[selectedBufferIndex]; // To loop through buffer
                            }
                            break;
                    }

                    if (data.Instance != null)
                    {
                        foreach (var field in fields)
                        {
                            data.FieldNames.Add(field.Name);
                            data.FieldTypes.Add(field.FieldType);
                            data.FieldValues.Add(field.GetValue(data.Instance));
                            fieldDataObtainedCount++;
                        }
                    }
                }
            }

            if (data.Classification == ComponentTypeClassification.None)
            {
                bufferLength = 0;
                data.State = ActionState.Error;
            }
            else if (data.FieldCount != fieldDataObtainedCount)
            {
                if (data.Classification == ComponentTypeClassification.Buffer && selectedBufferIndex == -1)
                {
                    data.State = ActionState.BufferEmpty;
                }
                else
                {
                    data.State = ActionState.Error;
                }
            }
            else
            {
                data.State = ActionState.Success;
            }
            return data;
        }
    }
}
