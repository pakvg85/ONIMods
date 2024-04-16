using HarmonyLib;

namespace ExtendedBuildingWidth
{
    [HarmonyPatch(typeof(BuildTool), "OnKeyDown")]
    class Patch_BuildTool_OnKeyDown
    {
        public static bool Prefix(KButtonEvent e)
        {
            if (e.TryConsume(Mod.PAction_GetSmallerBuilding.GetKAction()))
            {
                var currentlySelectedDef = Traverse.Create(BuildTool.Instance).Field("def").GetValue() as BuildingDef;
                if (DynamicBuildingsManager.IsDynamicallyCreated(currentlySelectedDef)
                    || DynamicBuildingsManager.IsOriginalForDynamicallyCreated(currentlySelectedDef))
                {
                    DynamicBuildingsManager.SwitchToPrevWidth(currentlySelectedDef);
                }
                return false;
            }
            if (e.TryConsume(Mod.PAction_GetBiggerBuilding.GetKAction()))
            {
                var currentlySelectedDef = Traverse.Create(BuildTool.Instance).Field("def").GetValue() as BuildingDef;

                if (DynamicBuildingsManager.IsDynamicallyCreated(currentlySelectedDef)
                    || DynamicBuildingsManager.IsOriginalForDynamicallyCreated(currentlySelectedDef))
                {
                    DynamicBuildingsManager.SwitchToNextWidth(currentlySelectedDef);
                }
                return false;
            }

            return true;
        }
    }
}