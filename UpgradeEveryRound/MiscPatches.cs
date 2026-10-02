using HarmonyLib;
using System.Collections.Generic;

namespace UpgradeEveryRound
{

    [HarmonyPatch(typeof(StatsUI), "Update")]
    public static class StatsUIPatch
    {
        static void Prefix(ref float ___showStatsTimer)
        {
            if (UpgradeMenu.isOpen) ___showStatsTimer = 5f;
        }
    }
    //Our custom save data handling
    [HarmonyPatch(typeof(StatsManager))]
    [HarmonyPatch("Start")]
    public static class StatsManagerStartPatch
    {
        static void Prefix(StatsManager __instance)
        {
            UpgradeUsageData.Initialize(__instance);
        }
        static void Postfix(StatsManager __instance)
        {
            ModUpgradeHandler.BuildExtraUpgradeList();
        }
    }

    [HarmonyPatch(typeof(StatsManager), "LoadGame")]
    public static class StatsManagerLoadGamePatch
    {
        static void Postfix(StatsManager __instance)
        {
            UpgradeUsageData.Initialize(__instance);
        }
    }

    [HarmonyPatch(typeof(RunManager))]
    [HarmonyPatch(nameof(RunManager.LeaveToMainMenu))]
    public static class RunManagerMainMenuPatch
    {
        static void Prefix()
        {
            UpgradeMenu.isOpen = false;
            Plugin.remoteNetworkData = null;
        }
    }
}
