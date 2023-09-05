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

    [HarmonyPatch(typeof(GeneratedBuildings), "RegisterWithOverlay")]
    public class Patch_GeneratedBuildings_RegisterWithOverlay
    {
        public static bool CreatingDynamicBuildingDefStarted = false;
        public static string DynamicallyGeneratedPrefabId = string.Empty;

        public static void Prefix(HashSet<Tag> overlay_tags, ref string id)
        {
            if (CreatingDynamicBuildingDefStarted)
            {
                // Call of 'RegisterWithOverlay' makes buildings fully white when appropriate overlay is selected.
                // Different implementations of 'IBuildingConfig' call this method differently with the 2nd parameter (id).
                // Some call it with hardcoded string value, some - with 'PrefabID', some - with 'ID', etc.
                // For dynamic buildings it is crucial for visualization to call this method with correct 'PrefabID'
                // that is adjusted in 'Patch_BuildingTemplates_CreateBuildingDef'.
                id = DynamicallyGeneratedPrefabId;
            }
        }

        public static void Postfix()
        {
            if (CreatingDynamicBuildingDefStarted)
            {
                DynamicallyGeneratedPrefabId = string.Empty;
                CreatingDynamicBuildingDefStarted = false;
            }
        }
    }
}