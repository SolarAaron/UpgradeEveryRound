using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace UpgradeEveryRound
{
    [HarmonyPatch(typeof(SemiFunc))]
    [HarmonyPatch(nameof(SemiFunc.OnSceneSwitch))]
    internal static class SceneSwitchPatch
    {
        static void Prefix()
        {
            UERNetworkManager.LevelChange();
        }
    }
}
