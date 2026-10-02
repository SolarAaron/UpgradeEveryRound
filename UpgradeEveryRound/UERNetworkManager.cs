using Steamworks;
using System;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UpgradeEveryRound
{
    public static class UERNetworkManager
    {
        public static bool synced = false;
        public static bool levelLoaded = false;
        public static bool menuOpened = false;
        private static bool joinedGame = false;
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

                if (!joinedGame)
                {
                    UpgradeUsageData.SetCount(StatsManager.instance, SteamClient.SteamId.Value.ToString(), -1); // Wait for the host's saved count.
                    joinedGame = true;
                }

                if (!levelLoaded) continue;

                synced = StatsManager.instance.statsSynced;

                if (!synced || !levelLoaded || menuOpened || Plugin.remoteNetworkData == null) continue;

                int syncedUpgradeCount = UpgradeUsageData.GetCount(StatsManager.instance, SteamClient.SteamId.Value.ToString());
                if (syncedUpgradeCount == -1) continue;

                if (upgradesUsedLocal == -1) upgradesUsedLocal = syncedUpgradeCount;

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
