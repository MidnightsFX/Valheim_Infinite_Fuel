using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Configs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValheimInfiniteFire.common;

namespace ValheimInfiniteFire {
    internal static class Patches {

        [HarmonyPatch(typeof(Smelter), nameof(Smelter.GetFuel))]
        internal static class SmelterGetFuel {
            [HarmonyPostfix]
            internal static void Postfix(Smelter __instance, ref float __result) {
                string prefabname = Utils.GetPrefabName(__instance.gameObject.name);

                ValConfig.NoFuelConfigs.TryGetValue(prefabname, out ConfigEntry<bool> status);
                if (status == null) { return; }
                if (status.Value == true) {
                    __result = 1f;
                }
            }
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.GetFuel))]
        internal static class CookerGetFuel {
            [HarmonyPostfix]
            internal static void Postfix(Smelter __instance, ref float __result) {
                string prefabname = Utils.GetPrefabName(__instance.gameObject.name);

                ValConfig.NoFuelConfigs.TryGetValue(prefabname, out ConfigEntry<bool> status);
                if (status == null) { return; }
                if (status.Value == true) {
                    __result = 1f;
                }
            }
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.OnHoverFuelSwitch))]
        internal static class OnHoverDisplayNoFuelRequirementCookingStation {
            [HarmonyAfter("shudnal.MyLittleUI")]
            [HarmonyPostfix]
            internal static void Postfix(CookingStation __instance, ref string __result) {
                string prefabname = Utils.GetPrefabName(__instance.gameObject.name);

                ValConfig.NoFuelConfigs.TryGetValue(prefabname, out ConfigEntry<bool> status);
                if (status == null) { return; }
                if (status.Value == true) {
                    __result = "No Fuel Needed.";
                    return;
                }
            }
        }

        [HarmonyPatch(typeof(Smelter), nameof(Smelter.OnHoverAddFuel))]
        internal static class OnHoverDisplayNoFuelRequirementSmelter {
            [HarmonyAfter("shudnal.MyLittleUI")]
            [HarmonyPostfix]
            internal static void Postfix(Smelter __instance, ref string __result) {
                string prefabname = Utils.GetPrefabName(__instance.gameObject.name);

                ValConfig.NoFuelConfigs.TryGetValue(prefabname, out ConfigEntry<bool> status);
                if (status == null) { return; }
                if (status.Value == true) {
                    __result = "No Fuel Needed.";
                    return;
                }
            }
        }
    }
}
