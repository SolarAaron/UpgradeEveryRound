using HarmonyLib;

namespace UpgradeEveryRound
{
    [HarmonyPatch(typeof(InputManager), nameof(InputManager.SwitchToPlayerInputs))]
    internal static class InputManagerSwitchToPlayerInputsPatch
    {
        internal static void Postfix()
        {
            UERNetworkManager.ClientPlayerInputsRestored();
        }
    }
}
