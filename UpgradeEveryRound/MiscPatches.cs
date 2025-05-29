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
            Plugin.Instance.BuildExtraUpgradeList();
            __instance.dictionaryOfDictionaries.Add("playerUpgradesUsed", []); //Keeps track of how many upgrades each player has used so far
            __instance.dictionaryOfDictionaries.Add("UERDataSync", new Dictionary<string, int>() { ["HostConfig"] = Plugin.configData }); //Using this to sync any other necessary data across clients, currently config.
            for (var i = 0; i < Plugin.ExtraConfigs.Count; i++) {
                __instance.dictionaryOfDictionaries["UERDataSync"]["HostConfig.Extra." + i] = Plugin.ExtraConfigs[i].Data;
            }
        }
    }

    [HarmonyPatch(typeof(StatsManager))]
    [HarmonyPatch("SaveGame")]
    public static class StatsManagerSavePatch
    {

        static void Postfix(ref StatsManager __instance)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer())
            {
                return;
            }
            __instance.dictionaryOfDictionaries["UERDataSync"]["HostConfig"] = Plugin.configData;
            for (var i = 0; i < Plugin.ExtraConfigs.Count; i++) {
                __instance.dictionaryOfDictionaries["UERDataSync"]["HostConfig.Extra." + i] = Plugin.ExtraConfigs[i].Data;
            }
        }

    }

    [HarmonyPatch(typeof(StatsManager))]
    [HarmonyPatch("LoadGame")]
    public static class StatsManagerLoadPatch
    {
        static void Postfix(ref StatsManager __instance)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer())
            {
                return;
            }
            __instance.dictionaryOfDictionaries["UERDataSync"]["HostConfig"] = Plugin.configData;
            for (var i = 0; i < Plugin.ExtraConfigs.Count; i++) {
                __instance.dictionaryOfDictionaries["UERDataSync"]["HostConfig.Extra." + i] = Plugin.ExtraConfigs[i].Data;
            }
        }

    }
            //So it turns out that things break sometimes, make sure we reset this value incase they escape the menu through other means
            [HarmonyPatch(typeof(RunManager))]
    [HarmonyPatch(nameof(RunManager.ChangeLevel))]
    public static class RunManagerChangeLevelPatch
    {
        static void Prefix()
        {
            UpgradeMenu.isOpen = false;
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
