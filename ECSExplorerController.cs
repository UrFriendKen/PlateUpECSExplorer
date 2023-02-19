using Kitchen;
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


    internal struct EntityData
    {
        public Entity Entity;
        public int NumberOfComponents;
    }

    internal struct ComponentData
    {
        public ComponentType Type;
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
                    NativeArray<ComponentType> components = EntityManager.GetComponentTypes(entity);
                    int count = components.Count();
                    components.Dispose();
                    entityData.Add(new EntityData()
                    {
                        Entity = entity,
                        NumberOfComponents = count
                    });
                }
                entities.Dispose();
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
            Type type = componentType.GetManagedType();
            data.Type = type;
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            data.FieldCount = fields.Count();
            data.FieldNames = new List<string>();
            data.FieldTypes = new List<Type>();
            data.FieldValues = new List<object>();
            int fieldDataObtainedCount = 0;
            if (EntityManager.HasComponent(entity, componentType))
            {
                MethodInfo genericMethod = null;
                if (componentType.IsSharedComponent)
                {
                    genericMethod = mGetSharedComponentData.MakeGenericMethod(type);
                }
                //else if (componentType.IsBuffer)
                //{
                //    genericMethod = mGetBuffer.MakeGenericMethod(type);
                //}
                else if (type.IsValueType && !type.IsPrimitive && !type.IsEnum)
                {
                    genericMethod = mGetComponentData.MakeGenericMethod(type);
                }

                if (genericMethod != null)
                {
                    var componentInstance = genericMethod.Invoke(EntityManager, new object[] { entity });
                    foreach (var field in fields)
                    {
                        data.FieldNames.Add(field.Name);
                        data.FieldTypes.Add(field.FieldType);
                        data.FieldValues.Add(field.GetValue(componentInstance));
                        fieldDataObtainedCount++;
                    }
                }
            }
            data.State = data.FieldCount == fieldDataObtainedCount ? ActionState.Success : ActionState.Error;
            return data;
        }
    }
}
