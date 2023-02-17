using Kitchen;
using System.Collections.Generic;
using Unity.Entities;

namespace KitchenECSExplorer
{
    internal static class CompatibilityReporter
    {
        public static Dictionary<ComponentType, string> NO_FIELDS => new Dictionary<ComponentType, string>()
        {
            { typeof(CCustomerType), "If you have Custom Difficulty, ensure \"Custom Group Count\" is Disabled then remove this entity and perform another Entity Query." },
            { typeof(CScheduledCustomer), "If you have Custom Difficulty, ensure \"Custom Group Count\" is Disabled then remove this entity and perform another Entity Query." }
        };
    }
}
