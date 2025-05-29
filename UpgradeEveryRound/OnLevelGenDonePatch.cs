using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace UpgradeEveryRound
{
    [HarmonyPatch(typeof(SemiFunc))]
    [HarmonyPatch(nameof(SemiFunc.OnLevelGenDone))]
    internal static class OnLevelGenDonePatch
    {
        internal static void Postfix()
        {
            UERNetworkManager.levelLoaded = true;
            if (SemiFunc.IsMasterClientOrSingleplayer()) UpgradeMenu.OpenMenu();
        }
    }
}
