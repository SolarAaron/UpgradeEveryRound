using HarmonyLib;
using System.Collections;

namespace UpgradeEveryRound
{
    // This runs only after GenerateDone has accepted the master's RPC and the
    // local generator has finished its setup checks. The GenerateDone RPC
    // postfix is too early: rejected RPCs still execute Harmony postfixes.
    [HarmonyPatch(typeof(LevelGenerator), "GenerateDoneApply")]
    internal static class LevelGeneratorGenerateDoneApplyPatch
    {
        internal static void Postfix(LevelGenerator __instance)
        {
            UERNetworkManager.levelLoaded = true;

            if (SemiFunc.IsMasterClientOrSingleplayer())
            {
                Plugin.Instance.StartCoroutine(OpenMenuNextFrame(__instance));
            }
        }

        private static IEnumerator OpenMenuNextFrame(LevelGenerator generator)
        {
            yield return null;

            if (generator != null
                && LevelGenerator.Instance == generator
                && SemiFunc.LevelGenDone())
            {
                UpgradeMenu.OpenMenu();
            }
        }
    }
}
