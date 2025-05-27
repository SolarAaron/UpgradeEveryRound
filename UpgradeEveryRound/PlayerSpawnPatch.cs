using HarmonyLib;
using Photon.Pun;
using System;
using System.Linq;

namespace UpgradeEveryRound
{
    [HarmonyPatch(typeof(PlayerAvatar))]
    [HarmonyPatch("SpawnRPC")]
    public static class PlayerSpawnPatch
    {
        static void Postfix(PhotonView ___photonView)
        {
            Level[] bannedLevels = [RunManager.instance.levelMainMenu, RunManager.instance.levelLobbyMenu, RunManager.instance.levelTutorial];
            if (bannedLevels.Contains(RunManager.instance.levelCurrent)) return;

            string _steamID = SemiFunc.PlayerGetSteamID(SemiFunc.PlayerAvatarGetFromPhotonID(SemiFunc.PhotonViewIDPlayerAvatarLocal()));
            int upgradesDeserved = RunManager.instance.levelsCompleted * Plugin.UpgradesPerRound;

#if DEBUG
            upgradesDeserved += 1;
#endif
            if (Plugin.NumUpgradesUsed(_steamID) >= upgradesDeserved) return;
            if (GameManager.Multiplayer() && !___photonView.IsMine) return;

            MenuManager.instance.PageCloseAll(); //Just in case somehow other menus were opened previously.

            var repoPopupPage = UpgradeMenu.createMenu(_steamID);

            repoPopupPage.OpenPage(false);
            UpgradeMenu.isOpen = true;
        }
    }
}
