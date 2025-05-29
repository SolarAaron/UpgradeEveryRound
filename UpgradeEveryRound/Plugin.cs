using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MenuLib.MonoBehaviors;
using Photon.Pun;
using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using REPOLib;
using REPOLib.Modules;
using ExitGames.Client.Photon;
using Photon.Realtime;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace UpgradeEveryRound;

[BepInPlugin(modGUID, modName, modVersion), BepInDependency("nickklmao.menulib", "2.1.3"), BepInDependency("REPOLib", "2.1.0")]
[BepInDependency(REPOLib.MyPluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
public class Plugin : BaseUnityPlugin
{
    public const string modGUID = "dev.redfops.repo.upgradeeveryround";
    public const string modName = "Upgrade Every Round";
    public const string modVersion = "1.3.0";

    public static ConfigEntry<int> upgradesPerRound;
    public static ConfigEntry<bool> limitedChoices;
    public static ConfigEntry<int> numChoices;

    public static Plugin Instance;
    
    public static ConfigEntry<bool> allowMapCount;
    public static ConfigEntry<bool> allowEnergy;
    public static ConfigEntry<bool> allowExtraJump;
    public static ConfigEntry<bool> allowRange;
    public static ConfigEntry<bool> allowStrength;
    public static ConfigEntry<bool> allowHealth;
    public static ConfigEntry<bool> allowSpeed;
    public static ConfigEntry<bool> allowTumbleLaunch;
    public static Dictionary<int, ConfigEntry<bool>> AllowExtras = new(); // bit index gets fixed on load
    public static Dictionary<int, string> ExtraLabels = new();
    private bool initialized = false;
    
    public static NetworkData localNetworkData;
    public static NetworkData remoteNetworkData;
    public static NetworkData CurrentNetworkData {
        get
        {
            if (!GameManager.instance || SemiFunc.IsMasterClientOrSingleplayer()) return localNetworkData;
            if (remoteNetworkData == null)
            {
                Plugin.Logger.LogError("Missing remote data");
                return localNetworkData; //Less than ideal but just return local data to prevent anything from breaking.
            }
            return remoteNetworkData;
        }
    }

    public static NetworkedEvent ConfigUpdateEvent;

    public static RaiseEventOptions ConfigUpdateEventOptions = new() { CachingOption = EventCaching.AddToRoomCache, Receivers = ReceiverGroup.Others};
    public static List<BitVector32> ExtraConfigs = new(); // These hold the config data for mod upgrades

    internal static new ManualLogSource Logger;
    private readonly Harmony harmony = new Harmony(modGUID);

    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Instance = this;

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

        ConfigUpdateEvent = new NetworkedEvent("ConfigUpdate", HandleConfigUpdateEvent);

        harmony.PatchAll(typeof(SceneSwitchPatch));
        harmony.PatchAll(typeof(OnLevelGenDonePatch));
        harmony.PatchAll(typeof(SteamManagerPatch));
        harmony.PatchAll(typeof(RunManagerMainMenuPatch));
        harmony.PatchAll(typeof(StatsManagerStartPatch));
        harmony.PatchAll(typeof(StatsUIPatch));
        harmony.PatchAll(typeof(UpgradeMapPlayerCountPatch));
        harmony.PatchAll(typeof(UpgradePlayerEnergyPatch));
        harmony.PatchAll(typeof(UpgradePlayerExtraJumpPatch));
        harmony.PatchAll(typeof(UpgradePlayerGrabRangePatch));
        harmony.PatchAll(typeof(UpgradePlayerGrabStrengthPatch));
        harmony.PatchAll(typeof(UpgradePlayerHealthPatch));
        harmony.PatchAll(typeof(UpgradePlayerSprintSpeedPatch));
        harmony.PatchAll(typeof(UpgradePlayerTumbleLaunchPatch));

        PhotonPeer.RegisterType(typeof(NetworkData), 0xF8, (x) => NetworkData.Serealize(x), NetworkData.Deserialize);

        StartCoroutine(UERNetworkManager.Go());

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    public void BuildExtraUpgradeList() 
    {
        if(!initialized) 
        {
            int bitIndex = 0;
            var labelSplitter = new Regex("(?<!^)(?=[A-Z])");

            AllowExtras.Clear();
            ExtraLabels.Clear();
            ExtraConfigs.Clear();

            foreach (var registeredUpgrade in Upgrades.PlayerUpgrades) {
                var label = string.Join(" ", labelSplitter.Split(registeredUpgrade.UpgradeId));
                var allowCustomUpgrade = Config.Bind("Enabled upgrades", $"Enable {label}", true,
                                                     new ConfigDescription($"Allows {label} Upgrade to be chosen"));
                AllowExtras.Add(bitIndex, allowCustomUpgrade);
                ExtraLabels.Add(bitIndex, label);
                allowCustomUpgrade.SettingChanged += ConfigUpdated;
                UpdateExtraConfig(bitIndex, allowCustomUpgrade);
                bitIndex++;
            }
            
            localNetworkData = new(); // local network data needs the extra upgrade list
            
            initialized = true;
        }
    }

    private static int UpdateExtraConfig(int configIndex, ConfigEntry<bool> allowCustomUpgrade) {
        int bitField = configIndex / 32;
        int bit = configIndex % 32;
                                                     
        while (ExtraConfigs.Count <= bitField) {
            ExtraConfigs.Add(new BitVector32());
        }
        
        var extraConfig = ExtraConfigs[bitField];
        extraConfig[bit] = allowCustomUpgrade.Value;
        ExtraConfigs[bitField] = extraConfig;
        
        return bitField;
    }

    //Allow config to be updated and synced midgame
    private void ConfigUpdated(object sender, EventArgs args)
    {
        localNetworkData = new();
        if (!SemiFunc.IsMultiplayer() || !PhotonNetwork.IsMasterClient)
        {
            return;
        }
        SendConfig();
    }

    public static void SendConfig()
    {
        ConfigUpdateEvent.RaiseEvent(localNetworkData, ConfigUpdateEventOptions, SendOptions.SendReliable);
    }

    private static void HandleConfigUpdateEvent(EventData eventData)
    {
        NetworkData data = (NetworkData)eventData.CustomData;
        remoteNetworkData = data;
    }

    //Get config from either synced data or directly depending on whether we're a host, client, or in singleplayer
    public static int UpgradesPerRound => CurrentNetworkData.upgradesPerRound;
    public static bool LimitedChoices => CurrentNetworkData.limitedChoices;
    public static int NumChoices => CurrentNetworkData.numChoices;
    public static bool AllowMapCount => CurrentNetworkData.allowMapCount;
    public static bool AllowEnergy => CurrentNetworkData.allowEnergy;
    public static bool AllowExtraJump => CurrentNetworkData.allowExtraJump;
    public static bool AllowRange => CurrentNetworkData.allowRange;
    public static bool AllowStrength => CurrentNetworkData.allowStrength;
    public static bool AllowHealth => CurrentNetworkData.allowHealth;
    public static bool AllowSpeed => CurrentNetworkData.allowSpeed;
    public static bool AllowTumbleLaunch => CurrentNetworkData.allowTumbleLaunch;

    public static bool AllowCustomUpgradeByIndex(int configIndex) {
        int bitField = configIndex / 32;
        int bit = configIndex % 32;
        var hostValue = new BitVector32(CurrentNetworkData.extraData[bitField]);
        
        return hostValue[bit];
    }


    //Helper function containing a good chunk of the repeated code from the buttons
    public static void ApplyUpgrade(string _steamID, REPOPopupPage popupPage)
    {
        //Update UI to reflect upgrade
        StatsUI.instance.Fetch();
        StatsUI.instance.ShowStats();
        CameraGlitch.Instance.PlayUpgrade();

        int value = NumUpgradesUsed(_steamID) + 1;
        UERNetworkManager.upgradesUsedLocal = value;
        PunManager.instance.UpdateStat("playerUpgradesUsed", _steamID, value);
        
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
        if (!SemiFunc.IsMasterClientOrSingleplayer()) return UERNetworkManager.upgradesUsedLocal;
        return StatsManager.instance.dictionaryOfDictionaries["playerUpgradesUsed"][_steamID];
    }
}
