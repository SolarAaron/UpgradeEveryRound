using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using HarmonyLib.Tools;
using MenuLib.MonoBehaviors;
using MenuLib;
using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using Steamworks;
using BepInEx.Configuration;
using System.Linq;
using System;
using static UnityEngine.Rendering.DebugUI;
using MonoMod.RuntimeDetour;
using System.Reflection;
using System.Reflection.Emit;

namespace UpgradeEveryRound;

[BepInPlugin(modGUID, modName, modVersion), BepInDependency("nickklmao.menulib", "2.1.3")]
public class Plugin : BaseUnityPlugin
{
    public const string modGUID = "dev.redfops.repo.upgradeeveryround";
    public const string modName = "Upgrade Every Round";
    public const string modVersion = "1.2.0";

    private static ConfigEntry<int> upgradesPerRound;
    private static ConfigEntry<bool> limitedChoices;
    private static ConfigEntry<int> numChoices;

    private static ConfigEntry<bool> allowMapCount;
    private static ConfigEntry<bool> allowEnergy;
    private static ConfigEntry<bool> allowExtraJump;
    private static ConfigEntry<bool> allowRange;
    private static ConfigEntry<bool> allowStrength;
    private static ConfigEntry<bool> allowHealth;
    private static ConfigEntry<bool> allowSpeed;
    private static ConfigEntry<bool> allowTumbleLaunch;

    //Will contain all of the config data in one integer bitwise, used for syncing data with clients
    public static int configData = 0;

    internal static new ManualLogSource Logger;
    private readonly Harmony harmony = new Harmony(modGUID);

    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;

        upgradesPerRound = Config.Bind("Upgrades", "Upgrades Per Round", 1, new ConfigDescription("Number of upgrades per round", new AcceptableValueRange<int>(0, 7)));
        limitedChoices = Config.Bind("Upgrades", "Limited random choices", false, new ConfigDescription("Only presents a fixed number of random options"));
        numChoices = Config.Bind("Upgrades", "Number of choices", 3, new ConfigDescription("Number of options to choose from per upgrade", new AcceptableValueRange<int>(1, 8)));

        allowMapCount = Config.Bind("Enabled upgrades", "Enable Map Player Count", true, new ConfigDescription("Allows Map Player Count Upgrade to be chosen"));
        allowEnergy = Config.Bind("Enabled upgrades", "Enable Stamina", true, new ConfigDescription("Allows Stamina Upgrade to be chosen"));
        allowExtraJump = Config.Bind("Enabled upgrades", "Enable Extra Jump", true, new ConfigDescription("Allows Extra Jump Upgrade to be chosen"));
        allowRange = Config.Bind("Enabled upgrades", "Enable Range", true, new ConfigDescription("Allows Range Upgrade to be chosen"));
        allowStrength = Config.Bind("Enabled upgrades", "Enable Strength", true, new ConfigDescription("Allows Strength Upgrade to be chosen"));
        allowHealth = Config.Bind("Enabled upgrades", "Enable Health", true, new ConfigDescription("Allows Health Upgrade to be chosen"));
        allowSpeed = Config.Bind("Enabled upgrades", "Enable Speed", true, new ConfigDescription("Allows Speed Upgrade to be chosen"));
        allowTumbleLaunch = Config.Bind("Enabled upgrades", "Enable Tumble Launch", true, new ConfigDescription("Allows Tumble Launch Upgrade to be chosen"));

        upgradesPerRound.SettingChanged += ConfigUpdated;
        limitedChoices.SettingChanged += ConfigUpdated;
        numChoices.SettingChanged += ConfigUpdated;

        allowMapCount.SettingChanged += ConfigUpdated;
        allowEnergy.SettingChanged += ConfigUpdated;
        allowExtraJump.SettingChanged += ConfigUpdated;
        allowRange.SettingChanged += ConfigUpdated;
        allowStrength.SettingChanged += ConfigUpdated;
        allowHealth.SettingChanged += ConfigUpdated;
        allowSpeed.SettingChanged += ConfigUpdated;
        allowTumbleLaunch.SettingChanged += ConfigUpdated;

        UpdateConfigData();

        harmony.PatchAll(typeof(PlayerSpawnPatch));
        harmony.PatchAll(typeof(RunManagerChangeLevelPatch));
        harmony.PatchAll(typeof(RunManagerMainMenuPatch));
        harmony.PatchAll(typeof(StatsManagerPatch));
        harmony.PatchAll(typeof(StatsUIPatch));
        harmony.PatchAll(typeof(UpgradeMapPlayerCountPatch));
        harmony.PatchAll(typeof(UpgradePlayerEnergyPatch));
        harmony.PatchAll(typeof(UpgradePlayerExtraJumpPatch));
        harmony.PatchAll(typeof(UpgradePlayerGrabRangePatch));
        harmony.PatchAll(typeof(UpgradePlayerGrabStrengthPatch));
        harmony.PatchAll(typeof(UpgradePlayerHealthPatch));
        harmony.PatchAll(typeof(UpgradePlayerSprintSpeedPatch));
        harmony.PatchAll(typeof(UpgradePlayerTumbleLaunchPatch));


        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    //Allow config to be updated and synced midgame
    private void ConfigUpdated(object sender, EventArgs args)
    {
        UpdateConfigData();
        if (!SemiFunc.IsMultiplayer() || !PhotonNetwork.IsMasterClient)
        {
            return;
        }
        PhotonView _photonView = PunManager.instance.GetComponent<PhotonView>();
        _photonView.RPC("UpdateStatRPC", RpcTarget.Others, "UERDataSync", "HostConfig", configData);
    }

    private void UpdateConfigData()
    {
        configData = upgradesPerRound.Value & 0x7; ///Bits 1-3
        configData |= ((numChoices.Value - 1) << 3) & 0x38; //Bits 4-6
        configData |= limitedChoices.Value ? 0x40 : 0; //Bit 7
        configData |= allowMapCount.Value ? 0x80 : 0; //Bit 8
        configData |= allowEnergy.Value ? 0x100 : 0; //Bit 9
        configData |= allowExtraJump.Value ? 0x200 : 0; //Bit 10
        configData |= allowRange.Value ? 0x400 : 0; //Bit 11
        configData |= allowStrength.Value ? 0x800 : 0; //Bit 12
        configData |= allowHealth.Value ? 0x1000 : 0; //Bit 13
        configData |= allowSpeed.Value ? 0x2000 : 0; //Bit 14
        configData |= allowTumbleLaunch.Value ? 0x4000 : 0; //Bit 15
#if DEBUG
        Logger.LogInfo("upgradeData updated to " +  configData);
#endif
    }

    //Get config from either synced data or directly depending on whether we're a host, client, or in singleplayer
    public static int UpgradesPerRound => SemiFunc.IsMasterClientOrSingleplayer() ? upgradesPerRound.Value : StatsManager.instance.dictionaryOfDictionaries["UERDataSync"]["HostConfig"] & 0x7;
    public static bool LimitedChoices => SemiFunc.IsMasterClientOrSingleplayer() ? limitedChoices.Value : (StatsManager.instance.dictionaryOfDictionaries["UERDataSync"]["HostConfig"] & 0x40) == 1;
    public static int NumChoices => SemiFunc.IsMasterClientOrSingleplayer() ? numChoices.Value : ((StatsManager.instance.dictionaryOfDictionaries["UERDataSync"]["HostConfig"] & 0x38) >> 3) + 1;
    public static bool AllowMapCount => SemiFunc.IsMasterClientOrSingleplayer() ? allowMapCount.Value : (StatsManager.instance.dictionaryOfDictionaries["UERDataSync"]["HostConfig"] & 0x80) == 1;
    public static bool AllowEnergy => SemiFunc.IsMasterClientOrSingleplayer() ? allowEnergy.Value : (StatsManager.instance.dictionaryOfDictionaries["UERDataSync"]["HostConfig"] & 0x100) == 1;
    public static bool AllowExtraJump => SemiFunc.IsMasterClientOrSingleplayer() ? allowExtraJump.Value : (StatsManager.instance.dictionaryOfDictionaries["UERDataSync"]["HostConfig"] & 0x200) == 1;
    public static bool AllowRange => SemiFunc.IsMasterClientOrSingleplayer() ? allowRange.Value : (StatsManager.instance.dictionaryOfDictionaries["UERDataSync"]["HostConfig"] & 0x400) == 1;
    public static bool AllowStrength => SemiFunc.IsMasterClientOrSingleplayer() ? allowStrength.Value : (StatsManager.instance.dictionaryOfDictionaries["UERDataSync"]["HostConfig"] & 0x800) == 1;
    public static bool AllowHealth => SemiFunc.IsMasterClientOrSingleplayer() ? allowHealth.Value : (StatsManager.instance.dictionaryOfDictionaries["UERDataSync"]["HostConfig"] & 0x1000) == 1;
    public static bool AllowSpeed => SemiFunc.IsMasterClientOrSingleplayer() ? allowSpeed.Value : (StatsManager.instance.dictionaryOfDictionaries["UERDataSync"]["HostConfig"] & 0x2000) == 1;
    public static bool AllowTumbleLaunch => SemiFunc.IsMasterClientOrSingleplayer() ? allowTumbleLaunch.Value : (StatsManager.instance.dictionaryOfDictionaries["UERDataSync"]["HostConfig"] & 0x4000) == 1;


    //Helper function containing a good chunk of the repeated code from the buttons
    public static void ApplyUpgrade(string _steamID, REPOPopupPage popupPage)
    {
        //Update UI to reflect upgrade
        StatsUI.instance.Fetch();
        StatsUI.instance.ShowStats();
        CameraGlitch.Instance.PlayUpgrade();

        int value = ++StatsManager.instance.dictionaryOfDictionaries["playerUpgradesUsed"][_steamID];
        if (GameManager.Multiplayer())
        {
            //Broadcast that we used an upgrade
            PhotonView _photonView = PunManager.instance.GetComponent<PhotonView>();
            _photonView.RPC("UpdateStatRPC", RpcTarget.Others, "playerUpgradesUsed", _steamID, value);
        }
        
        //Close the menu
        UpgradeMenu.isOpen = false;
        popupPage.ClosePage(true);

        //If we are due for more upgrades then open the menu again
        int upgradesDeserved = RunManager.instance.levelsCompleted * UpgradesPerRound;
        if (upgradesDeserved > value)
        {
            var repoPopupPage = UpgradeMenu.createMenu(_steamID);
            repoPopupPage.OpenPage(false);
            UpgradeMenu.isOpen = true;
        }
    }

    public static int NumUpgradesUsed(string _steamID)
    {
        return StatsManager.instance.dictionaryOfDictionaries["playerUpgradesUsed"][_steamID];
    }
}

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
public static class StatsManagerPatch
{
    static void Prefix(StatsManager __instance)
    {
        __instance.dictionaryOfDictionaries.Add("playerUpgradesUsed",[]); //Keeps track of how many upgrades each player has used so far
        __instance.dictionaryOfDictionaries.Add("UERDataSync", new Dictionary<string, int>() { {"HostConfig", Plugin.configData} }); //Using this to sync any other necessary data across clients, currently config.
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