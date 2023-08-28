using System;
using System.Collections.Generic;
using HarmonyLib;

namespace ExtendedBuildingWidth
{
    [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
    public class Patch_GeneratedBuildings_LoadGeneratedBuildings
    {
        public static void Postfix(List<Type> types)
        {
            DynamicBuildingsManager.RegisterDynamicBuildings_For_ExtendableConfigSettings();
        }
    }

}