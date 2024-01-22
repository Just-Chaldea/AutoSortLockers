using HarmonyLib;

namespace AutosortLockers.Patches
{
    class AutosortLocker_Initializer_Patches
    {
        private static bool initialized;

        [HarmonyPatch(typeof(uGUI_PowerIndicator))]
        [HarmonyPatch(nameof(uGUI_PowerIndicator.Initialize))]
        class uGUI_PowerIndicator_Initialize_Patch
        {
            private static void Postfix(uGUI_PowerIndicator __instance)
            {
                if (initialized)
                {
                    if (Player.main == null)
                    {
                        Logger.Log("Deinitialize from no player");
                        initialized = false;
                    }
                    return;
                }

                if (Inventory.main == null)
                {
                    return;
                }

                Plugin.LoadSaveData();
                initialized = true;
            }
        }

        [HarmonyPatch(typeof(uGUI_PowerIndicator))]
        [HarmonyPatch(nameof(uGUI_PowerIndicator.Deinitialize))]
        class uGUI_PowerIndicator_Deinitialize_Patch
        {
            private static void PostFix()
            {
                Logger.Log("Deinitialize");
                initialized = false;
            }
        }
    }
}
