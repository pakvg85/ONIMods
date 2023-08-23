using HarmonyLib;

namespace ExtendedBuildingWidth
{
    [HarmonyPatch(typeof(BuildTool), "OnKeyDown")]
    class Patch_BuildTool_OnKeyDown
    {
        static bool Prefix(KButtonEvent e)
        {
            // These keybindings are equivalent to 'ALT + X' and 'ALT + C' by default
            if (!e.IsAction(Action.DebugNotification)
                && !e.IsAction(Action.DebugNotificationMessage)
                )
            {
                return true;
            }

            var currentlySelectedDef = Traverse.Create(BuildTool.Instance).Field("def").GetValue() as BuildingDef;
            if (!DynamicBuildingsManager.IsDynamicallyCreated(currentlySelectedDef)
                && !DynamicBuildingsManager.IsOriginalForDynamicallyCreated(currentlySelectedDef))
            {
                return true;
            }

            if (e.TryConsume(Action.DebugNotification)) // ALT + X
            {
                var successfullySwitched = DynamicBuildingsManager.SwitchToPrevWidth(currentlySelectedDef);
                return !successfullySwitched;
            }
            if (e.TryConsume(Action.DebugNotificationMessage)) // ALT + C
            {
                var successfullySwitched = DynamicBuildingsManager.SwitchToNextWidth(currentlySelectedDef);
                return !successfullySwitched;
            }

            return true;
        }
    }
}