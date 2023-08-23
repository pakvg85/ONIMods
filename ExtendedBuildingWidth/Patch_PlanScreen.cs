using HarmonyLib;

namespace ExtendedBuildingWidth
{
    /// <summary>
    /// 'CopyLastBuilding' button does not work with dynamic BuildingDef, so it has to be replaced with original BuildingDef.
    /// </summary>
    [HarmonyPatch(typeof(PlanScreen), "OnClickCopyBuilding")]
    public class Patch_PlanScreen_OnClickCopyBuilding
    {
        public static void Prefix()
        {
            // An exception will occur if no building was selected before (at game load for example).
            var lastSelectedBuilding = Traverse.Create(PlanScreen.Instance).Property("LastSelectedBuilding").GetValue() as Building;
            if (lastSelectedBuilding == null)
            {
                Debug.Log("ExtendedBuildingWidth - lastSelectedBuilding is null");
                return;
            }

            var buildingDef = lastSelectedBuilding.Def;

            if (DynamicBuildingsManager.IsDynamicallyCreated(buildingDef))
            {
                var originalDef = DynamicBuildingsManager.GetOriginalDefByDynamicDef(buildingDef);
                var originalBuilding = originalDef.BuildingComplete.GetComponent<Building>();
                Traverse.Create(PlanScreen.Instance).Property("LastSelectedBuilding").SetValue(originalBuilding);
            }
        }
    }
}