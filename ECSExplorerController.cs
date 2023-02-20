using Kitchen;
using KitchenData;
using KitchenLib.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;

namespace KitchenECSExplorer
{
    internal enum ActionState
    {
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
            if (Require(entity, out CAppliance appliance))
            {
                name = GameData.Main.Get<Appliance>(appliance.ID).name;
            }
            else if (Require(entity, out CItem item))
            {
                name = GameData.Main.Get<Item>(item.ID).name;
            }
            else if (Require(entity, out CPlayer upgrade))
            {
                name = "Player";
            }
            else if (Require(entity, out CRequiresView view))
            {
                name = view.Type.ToString();
            }
            else if (componentCount == 1)
            {
                name = $"{components[0].GetManagedType().Name}";
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

        public static List<ComponentType> GetAllEntityComponents(Entity entity)
        {
            return instance.EntityManager.GetComponentTypes(entity).ToList();
        }

        public ComponentData GetComponentData(Entity entity, ComponentType componentType)
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
                    //    genericMethod = mGetBuffer.MakeGenericMethod(type);
                }
                else if (type.IsValueType && !type.IsPrimitive && !type.IsEnum)
                {
                    data.Classification = ComponentTypeClassification.Data;
                    genericMethod = mGetComponentData.MakeGenericMethod(type);
                }

                if (genericMethod != null)
                {
                    data.Instance = genericMethod.Invoke(EntityManager, new object[] { entity });
                    foreach (var field in fields)
                    {
                        data.FieldNames.Add(field.Name);
                        data.FieldTypes.Add(field.FieldType);
                        data.FieldValues.Add(field.GetValue(data.Instance));
                        fieldDataObtainedCount++;
                    }
                }
            }
            data.State = (data.FieldCount == fieldDataObtainedCount || data.Classification == ComponentTypeClassification.Buffer) ? ActionState.Success : ActionState.Error;
            return data;
        }
    }
}
