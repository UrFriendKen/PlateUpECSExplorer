using Kitchen;
using KitchenMods;
using System;
using System.Collections.Generic;
using Unity.Entities;

namespace KitchenECSExplorer
{
    public class PlateUpExplorerInitialisationSystem : FranchiseFirstFrameSystem, IModSystem
    {
        bool firstUpdate = true;
        protected override void OnUpdate()
        {
            if (firstUpdate)
            {
                SystemsMenu.PopulateWorldSystems(GetSystemOrder());
                ViewsMenu.Populate(AssetDirectory);
                firstUpdate = false;
            }

            Dictionary<Type, SystemsMenu.System> GetSystemOrder()
            {
                Dictionary<Type, SystemsMenu.System> topLevelSystems = new Dictionary<Type, SystemsMenu.System>();
                Dictionary<Type, (SystemsMenu.System, int)> pendingSubsystems = new Dictionary<Type, (SystemsMenu.System, int)>();

                foreach (var systemBase in World.Systems)
                {
                    var systemData = new SystemsMenu.System()
                    {
                        Name = systemBase.GetType().FullName
                    };
                    if (systemBase is ComponentSystemGroup systemGroup)
                    {
                        systemGroup.SortSystems();
                        for (int i = 0; i < systemGroup.Systems.Count; i++)
                        {
                            var subsystem = systemGroup.Systems[i];
                            if (topLevelSystems.TryGetValue(subsystem.GetType(), out var subsystemData))
                            {
                                systemData.AddSubsystem(subsystemData, i);
                                topLevelSystems.Remove(subsystem.GetType());
                            }
                            else
                            {
                                pendingSubsystems.Add(subsystem.GetType(), (systemData, i));
                            }
                        }
                    }

                    if (pendingSubsystems.TryGetValue(systemBase.GetType(), out var parentSystemGroupAndIndex))
                    {
                        (SystemsMenu.System parentSystemGroup, int insertIndex) = parentSystemGroupAndIndex;
                        parentSystemGroup.AddSubsystem(systemData, insertIndex);
                        pendingSubsystems.Remove(systemBase.GetType());
                    }
                    else
                    {
                        topLevelSystems.Add(systemBase.GetType(), systemData);
                    }
                }
                return topLevelSystems;
            }
        }
    }
}
