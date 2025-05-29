using Steamworks;
using System;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UpgradeEveryRound
{
    public class UERNetworkManager
    {
        public static bool synced = false;
        public static bool levelLoaded = false;
        public static bool menuOpened = false;
        public static int upgradesUsedLocal = -1;
        public static IEnumerator Go()
        {
            while (true)
            {
                yield return new WaitForFixedUpdate();
                if (!GameManager.instance || SemiFunc.IsMasterClientOrSingleplayer()) 
                {
                    synced = false;
                    levelLoaded = false;
                    menuOpened = false;
                    upgradesUsedLocal = -1;
                    continue;
                };

                if (!levelLoaded) continue;

                synced = StatsManager.instance.statsSynced;

                if (!synced || !levelLoaded || menuOpened) continue;

                if (upgradesUsedLocal == -1) upgradesUsedLocal = StatsManager.instance.dictionaryOfDictionaries["playerUpgradesUsed"][SteamClient.SteamId.Value.ToString()];

                UpgradeMenu.OpenMenu();
                menuOpened = true;
            }
        }

        

        public static void LevelChange()
        {
            synced = false;
            levelLoaded = false;
            menuOpened = false;
        }
    }
}
