using BepInEx.Configuration;
using HarmonyLib;
using REPOLib.Modules;
using System;
using System.Collections.Generic;
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

        public static Dictionary<int, ConfigEntry<bool>> AllowExtras = new(); // Menu/config index maps to the stable key in ExtraKeys.
        public static Dictionary<int, string> ExtraLabels = new();
        public static Dictionary<int, string> ExtraKeys = new();
        public static Dictionary<string, GameObject> ExtraUpgrades = new();
        public static Dictionary<string, string> ExtraUpgradeIds = new();
        public static Dictionary<string, GameObject> ExtraUpgradePrefabs = new();

        //For the record this hackiness is required just to support moreupgrades lol
        internal static bool DoUpgrade(string upgradeKey, string _steamID)
        {
            if (ExtraUpgradeIds.TryGetValue(upgradeKey, out var upgradeId))
            {
                var upgrade = Upgrades.GetUpgrade(upgradeId);
                if (upgrade == null)
                {
                    Plugin.Logger.LogError($"Registered upgrade '{upgradeId}' is no longer available.");
                    return false;
                }

                upgrade.AddLevel(_steamID);
                return true;
            }

            if (!ExtraUpgradePrefabs.TryGetValue(upgradeKey, out var upgradePrefab) || upgradePrefab == null)
            {
                Plugin.Logger.LogError($"No registered upgrade ID or item prefab found for key '{upgradeKey}'.");
                return false;
            }

            //Yes, we literally instantiate it, use it, and get rid of it. It's awful but it's what you need to do for these ones.
            var tempUpgradeObj = Object.Instantiate(upgradePrefab);

            foreach(var component in tempUpgradeObj.GetComponents<MonoBehaviour>())
            {
                var traverseComponent = Traverse.Create(component);
                var methodStart = traverseComponent.Method("Start", []);
                if (methodStart.MethodExists()) methodStart.GetValue([]);
            }

            var itemToggle = tempUpgradeObj.GetComponent<ItemToggle>();
            var itemUpgrade = tempUpgradeObj.GetComponent<ItemUpgrade>();
            var traverseToggle = Traverse.Create(itemToggle);
            traverseToggle.Field<int>("playerTogglePhotonID").Value = SemiFunc.PlayerGetFromSteamID(_steamID).photonView.ViewID;
            var traverseUpgrade = Traverse.Create(itemUpgrade);
            
            traverseUpgrade.Field<PhysGrabObjectImpactDetector>("impactDetector").Value.destroyDisable = true;
            var method = traverseUpgrade.Method("PlayerUpgrade", []);
            method.GetValue([]);
            Object.Destroy(tempUpgradeObj);
            return true;
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
                ExtraKeys.Clear();
                ExtraUpgrades.Clear();
                ExtraUpgradeIds.Clear();
                ExtraUpgradePrefabs.Clear();

                foreach (var registeredUpgrade in Upgrades.PlayerUpgrades.OrderBy(x => x.UpgradeId, StringComparer.Ordinal))
                {
                    var label = string.Join(" ", labelSplitter.Split(registeredUpgrade.UpgradeId));
                    var allowCustomUpgrade = Plugin.Instance.Config.Bind("Enabled upgrades", $"Enable {label}", true,
                                                         new ConfigDescription($"Allows {label} Upgrade to be chosen"));
                    AllowExtras.Add(bitIndex, allowCustomUpgrade);
                    ExtraLabels.Add(bitIndex, label);
                    var key = "upgrade:" + registeredUpgrade.UpgradeId;
                    ExtraKeys.Add(bitIndex, key);
                    ExtraUpgrades.Add(label, null);
                    ExtraUpgradeIds.Add(key, registeredUpgrade.UpgradeId);
                    BindExtraConfig(allowCustomUpgrade);
                    bitIndex++;
                }

                string[] nativeUpgradeLabels = [
                    "Map Player Count", "Stamina", "Extra Jump", "Range", "Strength", "Health", "Sprint Speed", "Tumble Launch",
                    "Tumble Climb", "Death Head Battery", "Tumble Wings", "Crouch Rest"
                ];
                // The game defines this item label, but its upgrade is disabled and has no implementation.
                string[] disabledGameUpgradeLabels = ["Throw Strength"];

                var itemUpgrades = Items.AllItems
                    .Where(x => x.itemType == SemiFunc.itemType.item_upgrade)
                    .OrderBy(x => x.itemName, StringComparer.Ordinal)
                    .ThenBy(x => x.name, StringComparer.Ordinal);
                foreach (var upgrade in itemUpgrades)
                {
                    var label = upgrade.itemName[..^8].Trim();

                    if (ExtraUpgrades.ContainsKey(label)) continue;
                    if (nativeUpgradeLabels.Contains(label) || disabledGameUpgradeLabels.Contains(label)) continue;

                    var prefab = upgrade.prefab.Prefab;
                    if (prefab == null)
                    {
                        Plugin.Logger.LogWarning($"Ignoring item upgrade '{upgrade.itemName}' because it has no prefab.");
                        continue;
                    }

                    var allowCustomUpgrade = Plugin.Instance.Config.Bind("Enabled upgrades", $"Enable {label}", true,
                                         new ConfigDescription($"Allows {label} Upgrade to be chosen"));
                    AllowExtras.Add(bitIndex, allowCustomUpgrade);
                    ExtraLabels.Add(bitIndex, label);
                    var key = "item:" + upgrade.name;
                    ExtraKeys.Add(bitIndex, key);
                    ExtraUpgrades.Add(label, prefab);
                    ExtraUpgradePrefabs.Add(key, prefab);
                    BindExtraConfig(allowCustomUpgrade);
                    bitIndex++;
                }

                // Rebuild and broadcast the host snapshot now that every custom upgrade has a stable key.
                Plugin.ConfigUpdated(null, EventArgs.Empty);

                initialized = true;
            }
        }

        private static void BindExtraConfig(ConfigEntry<bool> allowCustomUpgrade)
        {
            allowCustomUpgrade.SettingChanged += Plugin.ConfigUpdated;
        }

        public static bool AllowCustomUpgradeByIndex(int configIndex)
        {
            var networkData = Plugin.CurrentNetworkData;
            if (networkData?.extraData == null || networkData.extraConfigHash != GetExtraConfigSchemaHash()) return false;

            int bitField = configIndex / 32;
            if (bitField >= networkData.extraData.Length) return false;

            int bitMask = 1 << (configIndex % 32);
            return (networkData.extraData[bitField] & bitMask) != 0;
        }

        internal static int[] GetExtraConfigBits()
        {
            var extraConfig = new int[(AllowExtras.Count + 31) / 32];
            foreach (var entry in AllowExtras)
            {
                if (entry.Value.Value)
                {
                    int bitField = entry.Key / 32;
                    int bitMask = 1 << (entry.Key % 32);
                    extraConfig[bitField] |= bitMask;
                }
            }

            return extraConfig;
        }

        internal static int GetExtraConfigSchemaHash()
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (var entry in ExtraKeys.OrderBy(x => x.Key))
                {
                    foreach (char character in entry.Value)
                    {
                        hash = (hash ^ character) * 16777619;
                    }

                    hash = (hash ^ 0xFF) * 16777619;
                }

                return (int)hash;
            }
        }
    }
}
