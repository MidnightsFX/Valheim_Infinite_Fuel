using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using ValheimInfiniteFire.common;

namespace ValheimInfiniteFire {
    internal static class Patches {

        /// <summary>
        ///  Infinite fuel stations report a full tank and never write a burn back, so the fuel actually stored in
        ///  the ZDO stays exactly what players put in. This is raised around the two places that must see that
        ///  real value: adding fuel, and refunding it when the station is destroyed.
        /// </summary>
        private static bool UseStoredFuel;

        private static bool HasInfiniteFuel(Component station) {
            if (UseStoredFuel) { return false; }
            ValConfig.NoFuelConfigs.TryGetValue(Utils.GetPrefabName(station.gameObject.name), out ConfigEntry<bool> status);
            return status != null && status.Value;
        }

        /// <summary>
        ///  Report a full tank, not just "some" fuel. A token 1 can be less than m_fuelPerProduct, so production
        ///  mods that make several products a tick and check for a whole product's worth first would stall, and
        ///  auto fuel mods would keep feeding it. m_fuelPerProduct can be configured above m_maxFuel, so cover
        ///  that too.
        /// </summary>
        [HarmonyPatch(typeof(Smelter), nameof(Smelter.GetFuel))]
        internal static class SmelterGetFuel {
            [HarmonyPostfix]
            internal static void Postfix(Smelter __instance, ref float __result) {
                if (!HasInfiniteFuel(__instance)) { return; }
                __result = Mathf.Max(__instance.m_maxFuel, __instance.m_fuelPerProduct);
            }
        }

        /// <summary>
        ///  UpdateSmelter writes GetFuel() minus the burn back every tick. Against the full tank reported above that
        ///  would store fuel nobody added, and DropAllItems refunds whatever the ZDO holds, so every destroyed
        ///  smelter would pay out a tank of coal. Skipping the write leaves the stored fuel untouched.
        /// </summary>
        [HarmonyPatch(typeof(Smelter), nameof(Smelter.SetFuel))]
        internal static class SmelterSetFuel {
            [HarmonyPrefix]
            internal static bool Prefix(Smelter __instance) {
                return !HasInfiniteFuel(__instance);
            }
        }

        /// <summary>
        ///  Vanilla won't offer fuel to a full station, but auto fuel mods that read the ZDO directly still send it.
        ///  Store it for real so it is refunded on destroy instead of vanishing into a blocked SetFuel.
        /// </summary>
        [HarmonyPatch(typeof(Smelter), nameof(Smelter.RPC_AddFuel))]
        internal static class SmelterAddStoredFuel {
            [HarmonyPriority(Priority.First)]
            [HarmonyPrefix]
            internal static void Prefix() { UseStoredFuel = true; }

            [HarmonyFinalizer]
            internal static void Finalizer() { UseStoredFuel = false; }
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.GetFuel))]
        internal static class CookerGetFuel {
            [HarmonyPostfix]
            internal static void Postfix(CookingStation __instance, ref float __result) {
                if (!HasInfiniteFuel(__instance)) { return; }
                __result = __instance.m_maxFuel;
            }
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.SetFuel))]
        internal static class CookerSetFuel {
            [HarmonyPrefix]
            internal static bool Prefix(CookingStation __instance) {
                return !HasInfiniteFuel(__instance);
            }
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.RPC_AddFuel))]
        internal static class CookerAddStoredFuel {
            [HarmonyPriority(Priority.First)]
            [HarmonyPrefix]
            internal static void Prefix() { UseStoredFuel = true; }

            [HarmonyFinalizer]
            internal static void Finalizer() { UseStoredFuel = false; }
        }

        /// <summary>
        ///  Unlike the smelter, the cooking station refunds fuel through GetFuel(), so without this every destroyed
        ///  oven drops a full tank of wood it never held.
        /// </summary>
        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.DropAllItems))]
        internal static class CookerRefundStoredFuel {
            [HarmonyPriority(Priority.First)]
            [HarmonyPrefix]
            internal static void Prefix() { UseStoredFuel = true; }

            [HarmonyFinalizer]
            internal static void Finalizer() { UseStoredFuel = false; }
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.OnHoverFuelSwitch))]
        internal static class OnHoverDisplayNoFuelRequirementCookingStation {
            [HarmonyAfter("shudnal.MyLittleUI")]
            [HarmonyPostfix]
            internal static void Postfix(CookingStation __instance, ref string __result) {
                if (!HasInfiniteFuel(__instance)) { return; }
                __result = "No Fuel Needed.";
            }
        }

        [HarmonyPatch(typeof(Smelter), nameof(Smelter.OnHoverAddFuel))]
        internal static class OnHoverDisplayNoFuelRequirementSmelter {
            [HarmonyAfter("shudnal.MyLittleUI")]
            [HarmonyPostfix]
            internal static void Postfix(Smelter __instance, ref string __result) {
                if (!HasInfiniteFuel(__instance)) { return; }
                __result = "No Fuel Needed.";
            }
        }

        /// <summary>
        ///  Decouples smoke from the gameplay it chokes. IsBlocked feeds Fireplace.CheckUnderTerrain and
        ///  Smelter.UpdateSmoke, which run at 0.25Hz and 1Hz per loaded piece, so this costs nothing per frame.
        /// </summary>
        [HarmonyPatch(typeof(SmokeSpawner), nameof(SmokeSpawner.IsBlocked))]
        internal static class SmokeSpawnerNeverBlocked {
            [HarmonyPostfix]
            internal static void Postfix(ref bool __result) {
                if (ValConfig.SmokeSuffocation.Value) { return; }
                __result = false;
            }
        }
    }
}
