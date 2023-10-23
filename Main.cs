using KitchenLib;
using KitchenMods;
using System;
using System.Collections.Generic;
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
        public const string MOD_GUID = "IcedMilo.PlateUp.PlateUpExplorer";
        public const string MOD_NAME = "PlateUp! Explorer";
        public const string MOD_VERSION = "0.3.6";
        public const string MOD_AUTHOR = "IcedMilo";
        public const string MOD_GAMEVERSION = ">=1.1.4";
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
            RegisterMenu<AchievementMenu>();
            RegisterMenu<EntityQueryMenu>();
            RegisterMenu<GDOMenu>();
            RegisterMenu<SystemsMenu>();
        }


        bool firstUpdate = true;
        protected override void OnUpdate()
        {
            if (firstUpdate)
            {
                SystemsMenu.PopulateWorldSystems(GetSystemOrder());
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

        protected override void OnPostActivate(KitchenMods.Mod mod)
        {
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
}
