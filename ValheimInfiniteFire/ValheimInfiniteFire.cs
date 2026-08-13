using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using ValheimInfiniteFire.common;

namespace ValheimInfiniteFire
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    [BepInDependency("shudnal.MyLittleUI", BepInDependency.DependencyFlags.SoftDependency)]
    internal class ValheimInfiniteFire : BaseUnityPlugin
    {
        public const string PluginGUID = "MidnightsFX.InfiniteFire";
        public const string PluginName = "InfiniteFire";
        public const string PluginVersion = "1.2.0";

        public ValConfig cfg;
        public static ManualLogSource Log;
        public static Harmony HarmonyInstance { get; private set; }

        /// <summary>
        ///  PrefabManager.OnPrefabsRegistered is a ZNetScene.Awake postfix, so it runs again on every world entry.
        ///  Only attach a SettingChanged handler the first time we see a prefab, or they stack up.
        /// </summary>
        private static readonly HashSet<string> SubscribedFuel = new HashSet<string>();

        public void Awake() {

            Log = this.Logger;
            cfg = new ValConfig(Config);


            HarmonyInstance = new Harmony(PluginGUID);
            HarmonyInstance.PatchAll();
            PrefabManager.OnPrefabsRegistered += FindAllFireTypes;
            PrefabManager.OnPrefabsRegistered += FindAllSmelters;
            PrefabManager.OnPrefabsRegistered += FindAllCookingStation;
            PrefabManager.OnPrefabsRegistered += SmokeControl.OnPrefabsRegistered;
            common.Logger.LogDebug("Lets Light it up");
        }

        public static void FindAllCookingStation() {
            foreach(CookingStation station in Resources.FindObjectsOfTypeAll<CookingStation>()) {
                if (station.m_fuelItem == null) { continue; }

                string prefabname = Utils.GetPrefabName(station.gameObject.name);
                ConfigEntry<bool> enableFuel = ValConfig.BindServerConfig("InfiniteFuel", prefabname, true, "Enable infinite fuel for this cooking station.");
                ValConfig.NoFuelConfigs[prefabname] = enableFuel;
                common.Logger.LogDebug($"Registering {prefabname} with InfiniteFuel {enableFuel.Value}");
            }
        }

        public static void FindAllFireTypes() {
            foreach(Fireplace fire in Resources.FindObjectsOfTypeAll<Fireplace>()) {
                string prefabname = Utils.GetPrefabName(fire.gameObject.name);
                ConfigEntry<bool> enableFire = ValConfig.BindServerConfig("InfiniteFire", prefabname, true, "Enable infinite fuel for this fire.");
                common.Logger.LogDebug($"Registering {prefabname} with infinitefire {enableFire.Value}");
                fire.m_infiniteFuel = enableFire.Value;
                if (SubscribedFuel.Add(prefabname)) {
                    enableFire.SettingChanged += (sender, args) => {
                        // Exact match, not StartsWith: fire_pit must not drag fire_pit_iron along with it.
                        foreach(Fireplace fp in Resources.FindObjectsOfTypeAll<Fireplace>().Where(fp => Utils.GetPrefabName(fp.gameObject.name) == prefabname)) {
                            common.Logger.LogDebug($"Updating {fp.name} to InfiniteFire:{enableFire.Value}");
                            fp.m_infiniteFuel = enableFire.Value;
                        }
                    };
                }
                ValConfig.NoFuelConfigs[prefabname] = enableFire;
            }
        }

        public static void FindAllSmelters() {
            foreach(Smelter smelter in Resources.FindObjectsOfTypeAll<Smelter>()) {
                string prefabname = Utils.GetPrefabName(smelter.gameObject.name);
                ConfigEntry<bool> enableFuel = ValConfig.BindServerConfig("InfiniteFuel", prefabname, false, "Enable infinite fuel for this smelter.");
                ValConfig.NoFuelConfigs[prefabname] = enableFuel;
                common.Logger.LogDebug($"Registering {prefabname} with InfiniteFuel {enableFuel.Value}");
            }
        }

    }
}