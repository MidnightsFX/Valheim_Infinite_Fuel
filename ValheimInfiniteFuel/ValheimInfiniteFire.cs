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
        public const string PluginVersion = "1.1.0";

        public ValConfig cfg;
        public static ManualLogSource Log;
        public static Harmony HarmonyInstance { get; private set; }
        public static Dictionary<string, SmokeSpawner> SmokeSpawners = new Dictionary<string, SmokeSpawner>();

        public void Awake() {

            Log = this.Logger;
            cfg = new ValConfig(Config);


            HarmonyInstance = new Harmony(PluginGUID);
            HarmonyInstance.PatchAll();
            PrefabManager.OnPrefabsRegistered += FindAllFireTypes;
            PrefabManager.OnPrefabsRegistered += FindAllSmelters;
            PrefabManager.OnPrefabsRegistered += FindAllCookingStation;
            // TODO: Nosmoke dynamics
            //PrefabManager.OnPrefabsRegistered += SmokeLess;
            common.Logger.LogDebug("Lets Light it up");
        }

        public static void FindAllCookingStation() {
            foreach(CookingStation station in Resources.FindObjectsOfTypeAll<CookingStation>()) {
                if (station.m_fuelItem == null) { continue; }

                string prefabname = Utils.GetPrefabName(station.gameObject.name);
                ConfigEntry<bool> enableFuel = ValConfig.BindServerConfig("InfiniteFuel", prefabname, true, "Enable infinite fuel for this cooking station.");
                ValConfig.NoFuelConfigs.Add(prefabname, enableFuel);
                common.Logger.LogDebug($"Registering {prefabname} with InfiniteFuel {enableFuel.Value}");
            }
        }

        public static void FindAllFireTypes() {
            foreach(Fireplace fire in Resources.FindObjectsOfTypeAll<Fireplace>()) {
                string prefabname = Utils.GetPrefabName(fire.gameObject.name);
                ConfigEntry<bool> enableFire = ValConfig.BindServerConfig("InfiniteFire", prefabname, true, "Enable infinite fuel for this fire.");
                common.Logger.LogDebug($"Registering {prefabname} with infinitefire {enableFire.Value}");
                fire.m_infiniteFuel = enableFire.Value;
                enableFire.SettingChanged += (sender, args) => {
                    foreach(Fireplace fp in Resources.FindObjectsOfTypeAll<Fireplace>().Where(fp => fp.gameObject.name.StartsWith(prefabname))) {
                        common.Logger.LogDebug($"Updating {fp.name} to InfiniteFire:{enableFire.Value}");
                        fp.m_infiniteFuel = enableFire.Value;
                    }
                };
                ValConfig.NoFuelConfigs.Add(prefabname, enableFire);
            }
        }

        public static void FindAllSmelters() {
            foreach(Smelter smelter in Resources.FindObjectsOfTypeAll<Smelter>()) {
                string prefabname = Utils.GetPrefabName(smelter.gameObject.name);
                ConfigEntry<bool> enableFuel = ValConfig.BindServerConfig("InfiniteFuel", prefabname, false, "Enable infinite fuel for this smelter.");
                ValConfig.NoFuelConfigs.Add(prefabname, enableFuel);
                common.Logger.LogDebug($"Registering {prefabname} with InfiniteFuel {enableFuel.Value}");
            }
        }

        public static void SmokeLess() {
            foreach (Smelter smelter in Resources.FindObjectsOfTypeAll<Smelter>()) {
                if (smelter.m_smokeSpawner == null) { continue; }

                string prefabname = Utils.GetPrefabName(smelter.gameObject.name);
                ConfigEntry<bool> noSmoke = ValConfig.BindServerConfig("NoSmoke", prefabname, false, "Enable or disable smoke for this smelter.");
                SmokeSpawners.Add(prefabname, smelter.m_smokeSpawner);
                common.Logger.LogDebug($"Registering {prefabname} for NoSmoke Options {noSmoke.Value}");

                noSmoke.SettingChanged += (sender, args) => {
                    foreach (Smelter fp in Resources.FindObjectsOfTypeAll<Smelter>().Where(fp => fp.gameObject.name.StartsWith(prefabname))) {
                        common.Logger.LogDebug($"Updating {fp.name} to NoSmoke:{noSmoke.Value}");
                        if (noSmoke.Value) {
                            if (smelter.m_smokeSpawner != null) {
                                Destroy(smelter.m_smokeSpawner);
                            }
                            smelter.m_smokeSpawner = null;
                        } else {
                            SmokeSpawners.TryGetValue(prefabname, out SmokeSpawner smoker);
                            if (smoker != null) {
                                smelter.m_smokeSpawner = smoker;
                            }
                        }
                    }
                };
            }

            foreach (Fireplace smelter in Resources.FindObjectsOfTypeAll<Fireplace>()) {
                if (smelter.m_smokeSpawner == null) { continue; }

                string prefabname = Utils.GetPrefabName(smelter.gameObject.name);
                ConfigEntry<bool> noSmoke = ValConfig.BindServerConfig("NoSmoke", prefabname, false, "Enable or disable smoke for this Fireplace.");
                SmokeSpawners.Add(prefabname, smelter.m_smokeSpawner);
                common.Logger.LogDebug($"Registering {prefabname} for NoSmoke Options {noSmoke.Value}");

                noSmoke.SettingChanged += (sender, args) => {
                    foreach (Fireplace fp in Resources.FindObjectsOfTypeAll<Fireplace>().Where(fp => fp.gameObject.name.StartsWith(prefabname))) {
                        common.Logger.LogDebug($"Updating {fp.name} to NoSmoke:{noSmoke.Value}");
                        if (noSmoke.Value) {
                            if (smelter.m_smokeSpawner != null) {
                                Destroy(smelter.m_smokeSpawner);
                            }
                            smelter.m_smokeSpawner = null;
                        } else {
                            SmokeSpawners.TryGetValue(prefabname, out SmokeSpawner smoker);
                            if (smoker != null) {
                                smelter.m_smokeSpawner = smoker;
                            }
                        }
                    }
                };
            }
        }
    }
}