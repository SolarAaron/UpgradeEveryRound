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
        static void Prefix(ref StatsManager __instance)
        {
            __instance.dictionaryOfDictionaries.Add("playerUpgradesUsed", []); //Keeps track of how many upgrades each player has used so far
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
