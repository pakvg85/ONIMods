using HarmonyLib;

namespace ExtendedBuildingWidth
{
    [HarmonyPatch(typeof(BuildingTemplates), "CreateBuildingDef")]
    public class Patch_BuildingTemplates_CreateBuildingDef
    {
        public static bool CreatingDynamicBuildingDefStarted = false;
        public static int WidthDeltaForDynamicBuildingDef = 0;

        public static void Prefix(ref string id, ref int width, ref float[] construction_mass, out float[] __state)
        {
            __state = null;
            if (CreatingDynamicBuildingDefStarted)
            {
                int originalWidth = width;
                width += WidthDeltaForDynamicBuildingDef;
                id += "_width" + width.ToString();
                Patch_GeneratedBuildings_RegisterWithOverlay.DynamicallyGeneratedPrefabId = id;

                // Adjust building mass.
                // In 'GasConduitBridgeConfig' field 'float[] tier' is assigned via reference from 'BUILDINGS.CONSTRUCTION_MASS_KG.TIER1'
                // and later is passed to 'CreateBuildingDef' also by reference.
                // As a result, changing contents of 'construction_mass' will affect 'BUILDINGS.CONSTRUCTION_MASS_KG.TIER1'.
                // To avoid this, we have to:
                // 1) keep original reference to construction_mass
                var originalConstruction_mass = construction_mass;
                // 2) create new instance of type float[] and change its contents according to our needs
                construction_mass = new ValueArray<float>(construction_mass.Length).Values;
                for (int i = 0; i <= originalConstruction_mass.Length - 1; i++)
                {
                    construction_mass[i] = originalConstruction_mass[i] / (float)originalWidth * (float)width;
                }
                // 3) restore original reference of construction_mass in Postfix after all calculations with construction_mass are done
                __state = originalConstruction_mass;
            }
        }

        public static void Postfix(ref float[] construction_mass, ref float[] __state)
        {
            if (CreatingDynamicBuildingDefStarted)
            {
                construction_mass = __state;

                WidthDeltaForDynamicBuildingDef = 0;
                CreatingDynamicBuildingDefStarted = false;
            }
        }
    }
}