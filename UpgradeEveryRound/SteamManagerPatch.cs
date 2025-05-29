using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace UpgradeEveryRound
{
    [HarmonyPatch(typeof(SteamManager))]
    internal static class SteamManagerPatch
    {
        [HarmonyPatch(nameof(SteamManager.UnlockLobby))]
        [HarmonyPostfix]
        internal static void UnlockLobbyPostfix()
        {
            Plugin.SendConfig();
        }
    }
}
