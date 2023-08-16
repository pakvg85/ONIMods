using HarmonyLib;

namespace ExtendedBuildingWidth
{
    [HarmonyPatch(typeof(BuildingTemplates), "CreateBuildingDef")]
    public class Patch_BuildingTemplates_CreateBuildingDef
    {
        public static bool CreatingDynamicBuildingDefStarted = false;
        public static int WidthDeltaForDynamicBuildingDef = 0;

        public static void Prefix(ref string id, ref int width, ref float[] construction_mass, out float __state)
        {
            __state = 0;

            if (CreatingDynamicBuildingDefStarted)
            {
                var originalWidth = width;
                width += WidthDeltaForDynamicBuildingDef;
                id += width.ToString();

                //// (Does not work yet)
                //// Attempt to also adjust building mass.
                //// In 'GasConduitBridgeConfig' field 'float[] tier' is assigned via reference from 'BUILDINGS.CONSTRUCTION_MASS_KG.TIER1'
                //// and later is passed to 'CreateBuildingDef' also by reference.
                //// As a result, changing contents of 'construction_mass' will affect 'BUILDINGS.CONSTRUCTION_MASS_KG.TIER1'.
                //// So we have to change back 'construction_mass' later in Postfix.
                //__state = construction_mass[0];
                //construction_mass[0] = __state / originalWidth * width;
            }
        }

        public static void Postfix(ref float[] construction_mass, float __state)
        {
            if (CreatingDynamicBuildingDefStarted)
            {
                //construction_mass[0] = __state;

                WidthDeltaForDynamicBuildingDef = 0;
                CreatingDynamicBuildingDefStarted = false;
            }
        }
    }

}