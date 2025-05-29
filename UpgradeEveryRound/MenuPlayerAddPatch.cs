using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace UpgradeEveryRound
{
    [HarmonyPatch(typeof(MenuPageLobby))]
    internal class MenuPlayerAddPatch
    {
        [HarmonyPatch("PlayerAdd")]
        [HarmonyPostfix]
        public static void PlayerAddPostfix()
        {

        }
    }
}
