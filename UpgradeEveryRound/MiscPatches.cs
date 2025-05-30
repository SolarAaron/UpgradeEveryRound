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
            __instance.dictionaryOfDictionaries.Add("playerUpgradesUsed", []); //Keeps track of how many upgrades each player has used so far
        }
        static void Postfix(ref StatsManager __instance)
        {
            ModUpgradeHandler.BuildExtraUpgradeList();
        }
    }

    [HarmonyPatch(typeof(RunManager))]
    [HarmonyPatch(nameof(RunManager.LeaveToMainMenu))]
    public static class RunManagerMainMenuPatch
    {
        static void Prefix()
        {
            UpgradeMenu.isOpen = false;
        }
    }
}
