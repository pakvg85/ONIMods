using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using KMod;
using System.Reflection;

namespace ExtendedBuildingWidth
{
    [HarmonyPatch(typeof(BuildingTemplates), "CreateBuildingDef")]
    public class Patch_BuildingTemplates_CreateBuildingDef
    {
        public static bool CreatingDynamicBuildingDefStarted = false;
        public static int NewWidthForDynamicBuildingDef = 0;

        public static void Prefix(ref string id, ref int width, ref string anim, ref float[] construction_mass, out float[] __state)
        {
            __state = null;

            if (CreatingDynamicBuildingDefStarted)
            {
                int originalWidth = width;
                width = NewWidthForDynamicBuildingDef;
                id = DynamicBuildingsManager.GetDynamicName(id, width);
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

        public static void Postfix(BuildingDef __result, ref string id, ref int width, ref string anim, ref float[] construction_mass, ref float[] __state)
        {
            if (!CreatingDynamicBuildingDefStarted)
            {
                DynamicBuildingsManager.AddBuildingDefToAnimNameMap(__result, anim);
            }

            if (CreatingDynamicBuildingDefStarted)
            {
                construction_mass = __state;

                CreatingDynamicBuildingDefStarted = false;
            }
        }
    }

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
                return;
            }

            var buildingDef = lastSelectedBuilding.Def;

            if (DynamicBuildingsManager.IsDynamicallyCreated(buildingDef))
            {
                var originalDef = DynamicBuildingsManager.DynamicDefToOriginalDefMap[buildingDef];
                var originalBuilding = originalDef.BuildingComplete.GetComponent<Building>();
                Traverse.Create(PlanScreen.Instance).Property("LastSelectedBuilding").SetValue(originalBuilding);
            }
        }
    }

    /// <summary>
    /// Animations for dynamic buildings should be cloned before 'KAnimGroupFile.LoadAll', but after 'modManager.Load' in 'Assets.LoadAnims()'.
    /// The reason - assets for mods are loaded in 'modManager.Load'.
    /// </summary>
    [HarmonyPatch(typeof(KAnimGroupFile), "LoadAll")]
    public class Patch_KAnimGroupFile_LoadAll
    {
        public static void Prefix()
        {
            DynamicAnimManager.AddDynamicAnimsNames_To_ModLoadedKAnims();
        }
    }

    [HarmonyPatch(typeof(Localization), "Initialize")]
    public class Localization_Initialize_Patch
    {
        public static void Postfix() => Translate(typeof(STRINGS));

        public static void Translate(Type root)
        {
            // Basic intended way to register strings, keeps namespace
            Localization.RegisterForTranslation(root);

            // Load user created translation files
            LoadStrings();

            // Register strings without namespace
            // because we already loaded user transltions, custom languages will overwrite these
            LocString.CreateLocStringKeys(root, null);

            // Creates template for users to edit
            Localization.GenerateStringsTemplate(root, Path.Combine(Manager.GetDirectory(), "strings_templates"));
        }

        private static void LoadStrings()
        {
            string path = Path.Combine(ModPath, "translations", Localization.GetLocale()?.Code + ".po");
            if (File.Exists(path))
                Localization.OverloadStrings(Localization.LoadStringsFile(path, false));
        }

        public static string ModPath => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    }
}