using HarmonyLib;
using System.Collections;

namespace UpgradeEveryRound
{
    // GenerateDone is broadcast to every peer, but the game's RPC body returns
    // early on clients. On the host, Generated may still wait for local setup.
    [HarmonyPatch(typeof(LevelGenerator), "GenerateDone")]
    internal static class LevelGeneratorGenerateDonePatch
    {
        internal static void Postfix(LevelGenerator __instance)
        {
            UERNetworkManager.levelLoaded = true;

            if (SemiFunc.IsMasterClientOrSingleplayer())
            {
                Plugin.Instance.StartCoroutine(OpenMenuWhenReady(__instance));
            }
        }

        private static IEnumerator OpenMenuWhenReady(LevelGenerator generator)
        {
            while (generator != null
                && LevelGenerator.Instance == generator
                && !SemiFunc.LevelGenDone())
            {
                yield return null;
            }

            if (generator != null
                && LevelGenerator.Instance == generator
                && SemiFunc.LevelGenDone())
            {
                UpgradeMenu.OpenMenu();
            }
        }
    }
}
