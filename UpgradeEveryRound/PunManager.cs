using HarmonyLib;
using Photon.Pun;

namespace UpgradeEveryRound
{

    //Yippee networking and boilerplate!

    [HarmonyPatch(typeof(PunManager))]
    [HarmonyPatch(nameof(PunManager.UpgradeMapPlayerCount))]
    public static class UpgradeMapPlayerCountPatch
    {
        static void Postfix(string _steamID, PhotonView ___photonView, StatsManager ___statsManager)
        {
            if (!SemiFunc.IsMasterClient() && GameManager.Multiplayer() && UpgradeMenu.isOpen)
            {
                ___photonView.RPC("UpgradeMapPlayerCountRPC", RpcTarget.Others, _steamID, ___statsManager.playerUpgradeMapPlayerCount[_steamID]);
            }
        }
    }

    [HarmonyPatch(typeof(PunManager))]
    [HarmonyPatch(nameof(PunManager.UpgradePlayerEnergy))]
    public static class UpgradePlayerEnergyPatch
    {
        static void Postfix(string _steamID, PhotonView ___photonView, StatsManager ___statsManager)
        {
            if (!SemiFunc.IsMasterClient() && GameManager.Multiplayer() && UpgradeMenu.isOpen)
            {
                ___photonView.RPC("UpgradePlayerEnergyCountRPC", RpcTarget.Others, _steamID, ___statsManager.playerUpgradeStamina[_steamID]);
            }
        }
    }

    [HarmonyPatch(typeof(PunManager))]
    [HarmonyPatch(nameof(PunManager.UpgradePlayerExtraJump))]
    public static class UpgradePlayerExtraJumpPatch
    {
        static void Postfix(string _steamID, PhotonView ___photonView, StatsManager ___statsManager)
        {
            if (!SemiFunc.IsMasterClient() && GameManager.Multiplayer() && UpgradeMenu.isOpen)
            {
                ___photonView.RPC("UpgradePlayerExtraJumpRPC", RpcTarget.Others, _steamID, ___statsManager.playerUpgradeExtraJump[_steamID]);
            }
        }
    }

    [HarmonyPatch(typeof(PunManager))]
    [HarmonyPatch(nameof(PunManager.UpgradePlayerGrabRange))]
    public static class UpgradePlayerGrabRangePatch
    {
        static void Postfix(string _steamID, PhotonView ___photonView, StatsManager ___statsManager)
        {
            if (!SemiFunc.IsMasterClient() && GameManager.Multiplayer() && UpgradeMenu.isOpen)
            {
                ___photonView.RPC("UpgradePlayerGrabRangeRPC", RpcTarget.Others, _steamID, ___statsManager.playerUpgradeRange[_steamID]);
            }
        }
    }

    [HarmonyPatch(typeof(PunManager))]
    [HarmonyPatch(nameof(PunManager.UpgradePlayerGrabStrength))]
    public static class UpgradePlayerGrabStrengthPatch
    {
        static void Postfix(string _steamID, PhotonView ___photonView, StatsManager ___statsManager)
        {
            if (!SemiFunc.IsMasterClient() && GameManager.Multiplayer() && UpgradeMenu.isOpen)
            {
                ___photonView.RPC("UpgradePlayerGrabStrengthRPC", RpcTarget.Others, _steamID, ___statsManager.playerUpgradeStrength[_steamID]);
            }
        }
    }

    [HarmonyPatch(typeof(PunManager))]
    [HarmonyPatch(nameof(PunManager.UpgradePlayerHealth))]
    public static class UpgradePlayerHealthPatch
    {
        static void Postfix(string playerName, PhotonView ___photonView, StatsManager ___statsManager)
        {
            if (!SemiFunc.IsMasterClient() && GameManager.Multiplayer() && UpgradeMenu.isOpen)
            {
                ___photonView.RPC("UpgradePlayerHealthRPC", RpcTarget.Others, playerName, ___statsManager.playerUpgradeHealth[playerName]);
            }
        }
    }

    [HarmonyPatch(typeof(PunManager))]
    [HarmonyPatch(nameof(PunManager.UpgradePlayerSprintSpeed))]
    public static class UpgradePlayerSprintSpeedPatch
    {
        static void Postfix(string _steamID, PhotonView ___photonView, StatsManager ___statsManager)
        {
            if (!SemiFunc.IsMasterClient() && GameManager.Multiplayer() && UpgradeMenu.isOpen)
            {
                ___photonView.RPC("UpgradePlayerSprintSpeedRPC", RpcTarget.Others, _steamID, ___statsManager.playerUpgradeSpeed[_steamID]);
            }
        }
    }

    [HarmonyPatch(typeof(PunManager))]
    [HarmonyPatch(nameof(PunManager.UpgradePlayerTumbleLaunch))]
    public static class UpgradePlayerTumbleLaunchPatch
    {
        static void Postfix(string _steamID, PhotonView ___photonView, StatsManager ___statsManager)
        {
            if (!SemiFunc.IsMasterClient() && GameManager.Multiplayer() && UpgradeMenu.isOpen)
            {
                ___photonView.RPC("UpgradePlayerTumbleLaunchRPC", RpcTarget.Others, _steamID, ___statsManager.playerUpgradeLaunch[_steamID]);
            }
        }
    }
}
