using HarmonyLib;

namespace ExtendedBuildingWidth
{
    [HarmonyPatch(typeof(BuildTool), "OnKeyDown")]
    class Patch_BuildTool_OnKeyDown
    {
        static bool Prefix(KButtonEvent e)
        {
            Debug.Log("ExtendedBuildingWidth - BuildTool.OnKeyDown - begin");
            // These keybindings are equivalent to 'ALT + X' and 'ALT + C' by default
            if (!e.IsAction(Action.DebugNotification) && !e.IsAction(Action.DebugNotificationMessage))
            {
                return true;
            }

            BuildingDef currentlySelectedDef = Traverse.Create(BuildTool.Instance).Field("def").GetValue() as BuildingDef;
            if (!DynamicBuildingsManager.IsDynamicallyCreated(currentlySelectedDef)
                && !DynamicBuildingsManager.IsOriginalForDynamicallyCreated(currentlySelectedDef))
            {
                return true;
            }

            if (e.TryConsume(Action.DebugNotification)) // ALT + X
            {
                DynamicBuildingsManager.SwitchToPrevWidth(currentlySelectedDef);
                return false;
            }
            if (e.TryConsume(Action.DebugNotificationMessage)) // ALT + C
            {
                DynamicBuildingsManager.SwitchToNextWidth(currentlySelectedDef);
                return false;
            }

            return true;
        }
    }

}