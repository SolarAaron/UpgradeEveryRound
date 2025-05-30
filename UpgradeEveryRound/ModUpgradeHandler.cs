using BepInEx.Configuration;
using HarmonyLib;
using MonoMod.Core.Utils;
using REPOLib.Modules;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UpgradeEveryRound
{
    internal static class ModUpgradeHandler
    {

        private static bool initialized = false;

        public static Dictionary<int, ConfigEntry<bool>> AllowExtras = new(); // bit index gets fixed on load
        public static Dictionary<int, string> ExtraLabels = new();
        public static Dictionary<string, GameObject> ExtraUpgrades = new();

        public static List<BitVector32> ExtraConfigs = new(); // These hold the config data for mod upgrades

        //For the record this hackiness is required just to support moreupgrades lol
        internal static void DoUpgrade(string upgradeLabel, string _steamID)
        {
            if (ExtraUpgrades[upgradeLabel] == null)
            {
                Upgrades.GetUpgrade(upgradeLabel.Replace(" ", ""))?.AddLevel(_steamID);
                return;
            }
            //Yes, we literally instantiate it, use it, and get rid of it. It's awful but it's what you need to do for these ones.
            var tempUpgradeObj = Object.Instantiate(ExtraUpgrades[upgradeLabel]);
            var itemToggle = tempUpgradeObj.GetComponent<ItemToggle>();
            var itemUpgrade = tempUpgradeObj.GetComponent<ItemUpgrade>();
            var traverseToggle = Traverse.Create(itemToggle);
            traverseToggle.Field<int>("playerTogglePhotonID").Value = SemiFunc.PhotonViewIDPlayerAvatarLocal();
            var traverseUpgrade = Traverse.Create(itemUpgrade);
            var method = traverseUpgrade.Method("PlayerUpgrade", []);
            method.GetValue([]);
            Object.Destroy(tempUpgradeObj);
        }

        public static void BuildExtraUpgradeList()
        {
            if (!initialized)
            {
                Plugin.Logger.LogMessage("Building extra upgrades list");
                int bitIndex = 0;
                var labelSplitter = new Regex("(?<!^)(?=[A-Z])");

                AllowExtras.Clear();
                ExtraLabels.Clear();
                ExtraConfigs.Clear();

                foreach (var registeredUpgrade in Upgrades.PlayerUpgrades)
                {
                    var label = string.Join(" ", labelSplitter.Split(registeredUpgrade.UpgradeId));
                    var allowCustomUpgrade = Plugin.Instance.Config.Bind("Enabled upgrades", $"Enable {label}", true,
                                                         new ConfigDescription($"Allows {label} Upgrade to be chosen"));
                    AllowExtras.Add(bitIndex, allowCustomUpgrade);
                    ExtraLabels.Add(bitIndex, label);
                    ExtraUpgrades.Add(label, null);
                    allowCustomUpgrade.SettingChanged += Plugin.ConfigUpdated;
                    UpdateExtraConfig(bitIndex, allowCustomUpgrade);
                    bitIndex++;
                }

                string[] defaultUpgrades = ["Map Player Count", "Stamina", "Extra Jump", "Range", "Strength", "Health", "Sprint Speed", "Tumble Launch"];

                var itemUpgrades = Items.GetItems().Where((x) => x.itemType == SemiFunc.itemType.item_upgrade);
                foreach (var upgrade in itemUpgrades)
                {
                    var label = upgrade.itemName[..^8];

                    if (ExtraUpgrades.ContainsKey(label)) continue;
                    if (defaultUpgrades.Contains(label)) continue;

                    var allowCustomUpgrade = Plugin.Instance.Config.Bind("Enabled upgrades", $"Enable {label}", true,
                                         new ConfigDescription($"Allows {label} Upgrade to be chosen"));
                    AllowExtras.Add(bitIndex, allowCustomUpgrade);
                    ExtraLabels.Add(bitIndex, label);
                    ExtraUpgrades.Add(label, upgrade.prefab);
                    allowCustomUpgrade.SettingChanged += Plugin.ConfigUpdated;
                    UpdateExtraConfig(bitIndex, allowCustomUpgrade);
                    bitIndex++;
                }

                Plugin.localNetworkData = new(); // local network data needs the extra upgrade list

                initialized = true;
            }
        }

        private static int UpdateExtraConfig(int configIndex, ConfigEntry<bool> allowCustomUpgrade)
        {
            int bitField = configIndex / 32;
            int bit = configIndex % 32;

            while (ExtraConfigs.Count <= bitField)
            {
                ExtraConfigs.Add(new BitVector32());
            }

            var extraConfig = ExtraConfigs[bitField];
            extraConfig[bit] = allowCustomUpgrade.Value;
            ExtraConfigs[bitField] = extraConfig;

            return bitField;
        }

        public static bool AllowCustomUpgradeByIndex(int configIndex)
        {
            int bitField = configIndex / 32;
            int bit = configIndex % 32;
            var hostValue = new BitVector32(Plugin.CurrentNetworkData.extraData[bitField]);

            return hostValue[bit];
        }
    }
}
