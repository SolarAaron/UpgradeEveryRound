using System;
using System.Collections.Generic;
using HarmonyLib;

namespace UpgradeEveryRound
{
    internal static class UpgradeUsageData
    {
        // StatsUI.FetchPlayerUpgrades displays every dictionary whose name starts with "playerUpgrade".
        internal const string DictionaryName = "upgradeEveryRoundUsage";
        private const string LegacyDictionaryName = "playerUpgradesUsed";

        internal static void Initialize(StatsManager statsManager)
        {
            var dictionaries = GetDictionaries(statsManager);
            if (!dictionaries.TryGetValue(DictionaryName, out var counts))
            {
                counts = new Dictionary<string, int>();
                dictionaries.Add(DictionaryName, counts);
            }

            // Migrate older saves and remove the old playerUpgrade-prefixed key so the vanilla overlay stops listing it.
            if (dictionaries.TryGetValue(LegacyDictionaryName, out var legacyCounts))
            {
                foreach (var entry in legacyCounts)
                {
                    if (!counts.ContainsKey(entry.Key)) counts[entry.Key] = entry.Value;
                }

                dictionaries.Remove(LegacyDictionaryName);
            }
        }

        internal static int GetCount(StatsManager statsManager, string steamId)
        {
            var counts = GetCounts(statsManager);
            return counts.TryGetValue(steamId, out int count) ? count : 0;
        }

        internal static void SetCount(StatsManager statsManager, string steamId, int count)
        {
            GetCounts(statsManager)[steamId] = count;
        }

        private static Dictionary<string, int> GetCounts(StatsManager statsManager)
        {
            var dictionaries = GetDictionaries(statsManager);
            if (!dictionaries.TryGetValue(DictionaryName, out var counts))
            {
                counts = new Dictionary<string, int>();
                dictionaries.Add(DictionaryName, counts);
            }

            return counts;
        }

        private static IDictionary<string, Dictionary<string, int>> GetDictionaries(StatsManager statsManager)
        {
            if (statsManager == null) throw new ArgumentNullException(nameof(statsManager));

            var dictionaries = Traverse.Create(statsManager)
                .Field("dictionaryOfDictionaries")
                .GetValue() as IDictionary<string, Dictionary<string, int>>;

            if (dictionaries == null)
            {
                throw new MissingFieldException(typeof(StatsManager).FullName, "dictionaryOfDictionaries");
            }

            return dictionaries;
        }
    }
}
