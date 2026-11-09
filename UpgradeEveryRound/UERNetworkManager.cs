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
        private const float ClientMenuSettleTimeoutSeconds = 2f;
        public static bool synced = false;
        public static bool levelLoaded = false;
        public static bool menuOpened = false;
        private static bool clientPlayerInputsReady = false;
        private static bool joinedGame = false;
        private static string lastClientReadinessState;
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
                    clientPlayerInputsReady = false;
                    lastClientReadinessState = null;
                    continue;
                };

                if (!joinedGame)
                {
                    UpgradeUsageData.SetCount(StatsManager.instance, SteamClient.SteamId.Value.ToString(), -1); // Wait for the host's saved count.
                    joinedGame = true;
                }

                // GenerateDoneApply is the authoritative completion point, but some clients
                // have missed the Harmony postfix. Generated is set at the end of that method,
                // so observe the game state as a fallback instead of waiting forever on the hook.
                if (!levelLoaded && LevelGenerator.Instance != null && LevelGenerator.Instance.Generated)
                {
                    levelLoaded = true;
                    ReportClientReadiness("generator reports Generated=true");
                }

                if (!levelLoaded) continue;

                StatsManager statsManager = StatsManager.instance;
                if (statsManager == null)
                {
                    ReportClientReadiness("waiting: StatsManager unavailable");
                    continue;
                }

                synced = statsManager.statsSynced;
                if (!synced)
                {
                    ReportClientReadiness("waiting: player stats are not synced");
                    continue;
                }

                if (Plugin.remoteNetworkData == null)
                {
                    ReportClientReadiness("waiting: host upgrade configuration not received");
                    continue;
                }

                if (menuOpened) continue;

                int syncedUpgradeCount = UpgradeUsageData.GetCount(statsManager, SteamClient.SteamId.Value.ToString());
                if (syncedUpgradeCount == -1)
                {
                    ReportClientReadiness("waiting: host upgrade usage count not received");
                    continue;
                }

                if (upgradesUsedLocal == -1) upgradesUsedLocal = syncedUpgradeCount;

                // The game completes its final menu-closing/input transition after
                // GenerateDoneApply on clients. Wait for that signal, with a bounded
                // fallback for game paths that do not emit it.
                LevelGenerator generator = LevelGenerator.Instance;
                ReportClientReadiness("waiting: final player-input/menu transition");
                float inputTransitionDeadline = Time.realtimeSinceStartup + ClientMenuSettleTimeoutSeconds;
                while (!clientPlayerInputsReady && Time.realtimeSinceStartup < inputTransitionDeadline)
                {
                    yield return null;
                }
                if (!clientPlayerInputsReady)
                {
                    ReportClientReadiness("final player-input/menu transition timed out; continuing after fallback");
                }
                yield return null;

                bool isCurrentGenerator = generator != null && LevelGenerator.Instance == generator;
                bool generated = isCurrentGenerator && generator.Generated;
                bool generationDone = generated && SemiFunc.LevelGenDone();
                if (!generationDone)
                {
                    ReportClientReadiness($"waiting: level finalization; currentGenerator={isCurrentGenerator}, generated={generated}, levelGenDone={generationDone}");
                    continue;
                }

                if (!levelLoaded || StatsManager.instance != statsManager || !statsManager.statsSynced || Plugin.remoteNetworkData == null)
                {
                    ReportClientReadiness("waiting: client state changed during menu transition");
                    continue;
                }

                ReportClientReadiness($"readiness gates passed; usage={upgradesUsedLocal}, levelsCompleted={RunManager.instance.levelsCompleted}");
                UpgradeMenu.OpenMenu();
                menuOpened = true;
                ReportClientReadiness($"popup attempt finished; menuIsOpen={UpgradeMenu.isOpen}");
            }
        }

        

        public static void LevelChange()
        {
            synced = false;
            levelLoaded = false;
            menuOpened = false;
            clientPlayerInputsReady = false;
            lastClientReadinessState = null;
        }

        public static void ClientPlayerInputsRestored()
        {
            if (GameManager.instance && !SemiFunc.IsMasterClientOrSingleplayer())
            {
                clientPlayerInputsReady = true;
                ReportClientReadiness("game returned to player inputs");
            }
        }

        private static void ReportClientReadiness(string state)
        {
            if (lastClientReadinessState == state) return;
            lastClientReadinessState = state;
            Plugin.Logger.LogInfo($"[UER] Client popup readiness: {state}");
        }
    }
}
