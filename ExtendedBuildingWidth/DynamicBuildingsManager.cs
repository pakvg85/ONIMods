using System;
using System.Collections.Generic;
using System.Collections;
using HarmonyLib;

namespace ExtendedBuildingWidth
{
    public static class DynamicBuildingsManager
    {
        public struct WidthRange { public int Min; public int Max; }

        private static Dictionary<IBuildingConfig, BuildingDef> _configTable = Traverse.Create(BuildingConfigManager.Instance).Field("configTable").GetValue() as Dictionary<IBuildingConfig, BuildingDef>;
        private static Dictionary<string, IBuildingConfig> configNameToObjectMap = new Dictionary<string, IBuildingConfig>();
        private static Dictionary<IBuildingConfig, WidthRange> configsToBeDynamicallyExtended = new Dictionary<IBuildingConfig, WidthRange>();
        private static Dictionary<BuildingDef, BuildingDef> dynamicDefToOriginalDefMap = new Dictionary<BuildingDef, BuildingDef>();
        private static Dictionary<BuildingDef, Dictionary<int, BuildingDef>> originalDefToDynamicDefAndWidthMap = new Dictionary<BuildingDef, Dictionary<int, BuildingDef>>();

        static DynamicBuildingsManager()
        {
            var configList = new List<IBuildingConfig>(_configTable.Keys);
            foreach(var config in configList)
            {
                configNameToObjectMap.Add(config.ToString(), config);
            }

            /// TODO - make it editable via 'Options' menu?
            configsToBeDynamicallyExtended.Add(GetConfigByName("GasConduitBridgeConfig"), new WidthRange { Min = ModSettings.Instance.MinWidth, Max = ModSettings.Instance.MaxWidth });
            configsToBeDynamicallyExtended.Add(GetConfigByName("LiquidConduitBridgeConfig"), new WidthRange { Min = ModSettings.Instance.MinWidth, Max = ModSettings.Instance.MaxWidth });
        }

        public static void RegisterDynamicBuildings(IBuildingConfig config)
        {
            if (!DlcManager.IsDlcListValidForCurrentContent(config.GetDlcIds()))
            {
                return;
            }

            var originalDef = GetBuildingDefByConfig(config);
            var widthRange = GetWidthRange(config);
            for (var width = widthRange.Min; width <= widthRange.Max; width++)
            {
                var delta = width - originalDef.WidthInCells;
                if (delta != 0)
                {
                    var dynamicDef = RegisterDynamicBuilding(config, delta);
                    AddMapping(dynamicDef, originalDef);
                }
            }
            // Add the original 'BuildingDef' so it could also be picked when switching between building widths.
            AddMapping(originalDef, originalDef);
        }

        /// <summary>
        /// This method is ripped from standard 'BuildingConfigManager.RegisterBuilding', few adjustments are made to create dynamic 'BuildingDef' entities.
        /// </summary>
        private static BuildingDef RegisterDynamicBuilding(IBuildingConfig config, int widthDelta)
        {
            // Fields 'dynamicDef.PrefabID' and 'dynamicDef.WidthInCells' will be adjusted in Prefix of 'Patch_BuildingTemplates_CreateBuildingDef'
            Patch_BuildingTemplates_CreateBuildingDef.CreatingDynamicBuildingDefStarted = true;
            Patch_BuildingTemplates_CreateBuildingDef.WidthDeltaForDynamicBuildingDef = widthDelta;
            BuildingDef dynamicDef = config.CreateBuildingDef();
            Patch_BuildingTemplates_CreateBuildingDef.WidthDeltaForDynamicBuildingDef = 0;
            Patch_BuildingTemplates_CreateBuildingDef.CreatingDynamicBuildingDefStarted = false;

            // Utility Offset fields should be overwritten because they are generated independently in 'IBuildingConfig.CreateBuildingDef' implementations.
            ChangeUtilityOffsets(dynamicDef);

            var originalDef = GetBuildingDefByConfig(config);
            // 'Strings' entities are copied from the original
            ChangeStrings(dynamicDef, originalDef);

            // ---
            // This big chunk of code is more or less similair to the rest of original 'BuildingConfigManager.RegisterBuilding'.
            // Exception is - assigning of 'configTable[config] = dynamicDef' is cropped because obviously it will break the mapping of the original 'BuildingDef'
            // and also verification for 'NonBuildableBuildings' is cropped as it is unnecessary for dynamic buildings.
            // ---
            dynamicDef.RequiredDlcIds = config.GetDlcIds();
            var baseTemplate = Traverse.Create(BuildingConfigManager.Instance).Field("baseTemplate").GetValue() as UnityEngine.GameObject;
            UnityEngine.GameObject gameObject = UnityEngine.Object.Instantiate<UnityEngine.GameObject>(baseTemplate);
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
            gameObject.GetComponent<KPrefabID>().PrefabTag = dynamicDef.Tag;
            gameObject.name = dynamicDef.PrefabID + "Template";
            gameObject.GetComponent<Building>().Def = dynamicDef;
            gameObject.GetComponent<OccupyArea>().SetCellOffsets(dynamicDef.PlacementOffsets);
            if (dynamicDef.Deprecated)
            {
                gameObject.GetComponent<KPrefabID>().AddTag(GameTags.DeprecatedContent, false);
            }
            config.ConfigureBuildingTemplate(gameObject, dynamicDef.Tag);
            dynamicDef.BuildingComplete = BuildingLoader.Instance.CreateBuildingComplete(gameObject, dynamicDef);
            dynamicDef.BuildingUnderConstruction = BuildingLoader.Instance.CreateBuildingUnderConstruction(dynamicDef);
            dynamicDef.BuildingUnderConstruction.name = BuildingConfigManager.GetUnderConstructionName(dynamicDef.BuildingUnderConstruction.name);
            dynamicDef.BuildingPreview = BuildingLoader.Instance.CreateBuildingPreview(dynamicDef);
            dynamicDef.BuildingPreview.name += "Preview";

            dynamicDef.PostProcess();
            config.DoPostConfigureComplete(dynamicDef.BuildingComplete);

            config.DoPostConfigurePreview(dynamicDef, dynamicDef.BuildingPreview);
            config.DoPostConfigureUnderConstruction(dynamicDef.BuildingUnderConstruction);

            Assets.AddBuildingDef(dynamicDef);

            // ---
            // 'Building' game objects for dynamically created BuildingDefs have to be stretched explicitly.
            // ---
            float originalWidthInCells = originalDef.WidthInCells;
            float widthInCells = dynamicDef.WidthInCells;
            StretchBuildingGameObject(dynamicDef, dynamicDef.BuildingUnderConstruction, widthInCells, originalWidthInCells);
            StretchBuildingGameObject(dynamicDef, dynamicDef.BuildingPreview, widthInCells, originalWidthInCells);
            StretchBuildingGameObject(dynamicDef, dynamicDef.BuildingComplete, widthInCells, originalWidthInCells);

            return dynamicDef;
        }

        private static void StretchBuildingGameObject(BuildingDef buildingDef, UnityEngine.GameObject gameObject, float widthInCells, float originalWidthInCells)
        {
            try
            {
                var buildingGameObject = gameObject.GetComponent<Building>().gameObject;
                var animController = buildingGameObject.GetComponent<KBatchedAnimController>();
                animController.animWidth = widthInCells / originalWidthInCells;
            }
            catch (Exception e)
            {
                Debug.Log("ExtendedBuildingWidth - stretching failed for " + gameObject.name + " because:");
                Debug.Log(e.Message);
            }
        }

        private static void ChangeUtilityOffsets(BuildingDef buildingDef)
        {
            buildingDef.UtilityInputOffset.x = -(buildingDef.WidthInCells - 1) / 2;
            buildingDef.UtilityOutputOffset.x = (buildingDef.WidthInCells) / 2;
        }

        private static void ChangeStrings(BuildingDef dynamicDef, BuildingDef originalDef)
        {
            var nameID = "STRINGS.BUILDINGS.PREFABS." + dynamicDef.PrefabID.ToUpper() + ".NAME";
            var descID = "STRINGS.BUILDINGS.PREFABS." + dynamicDef.PrefabID.ToUpper() + ".DESC";
            var effectID = "STRINGS.BUILDINGS.PREFABS." + dynamicDef.PrefabID.ToUpper() + ".EFFECT";

            var origNameID = "STRINGS.BUILDINGS.PREFABS." + originalDef.PrefabID.ToUpper() + ".NAME";
            var origDescID = "STRINGS.BUILDINGS.PREFABS." + originalDef.PrefabID.ToUpper() + ".DESC";
            var origEffectID = "STRINGS.BUILDINGS.PREFABS." + originalDef.PrefabID.ToUpper() + ".EFFECT";

            var origNameEntry = Strings.Get(origNameID);
            var origDescEntry = Strings.Get(origDescID);
            var origEffectEntry = Strings.Get(origEffectID);

            Strings.Add(nameID, origNameEntry);
            Strings.Add(descID, origDescEntry);
            Strings.Add(effectID, origEffectEntry);
        }

        private static void AddMapping(BuildingDef dynamicDef, BuildingDef originalDef)
        {
            if (dynamicDefToOriginalDefMap.ContainsKey(dynamicDef))
            {
                throw new Exception("ExtendedBuildingWidth - 'DynamicDefToOriginalDefMap' already has key " + dynamicDef.PrefabID + ". How come?");
            }
            dynamicDefToOriginalDefMap[dynamicDef] = originalDef;

            if (!originalDefToDynamicDefAndWidthMap.ContainsKey(originalDef))
            {
                originalDefToDynamicDefAndWidthMap[originalDef] = new Dictionary<int, BuildingDef>();
            }

            if (originalDefToDynamicDefAndWidthMap[originalDef].ContainsKey(dynamicDef.WidthInCells))
            {
                throw new Exception("ExtendedBuildingWidth - 'OriginalDefToDynamicDefAndWidthMap' already has key (" + originalDef.PrefabID + ", " + dynamicDef.WidthInCells + "). How come?");
            }
            originalDefToDynamicDefAndWidthMap[originalDef][dynamicDef.WidthInCells] = dynamicDef;
        }

        public static void SwitchToPrevWidth(BuildingDef currentlySelectedDef)
        {
            var originalDef = GetOriginalDefByDynamicDef(currentlySelectedDef);

            BuildingDef shiftedDef = null;
            if (currentlySelectedDef.WidthInCells - 1 >= ModSettings.Instance.MinWidth)
            {
                shiftedDef = GetDynamicDefByOriginalDefAndWidth(originalDef, currentlySelectedDef.WidthInCells - 1);
            }
            else
            {
                // Lower limit for width range is reached. Roll back to the max width.
                shiftedDef = GetDynamicDefByOriginalDefAndWidth(originalDef, ModSettings.Instance.MaxWidth);
            }

            SetActiveBuildingDef(shiftedDef, originalDef);
        }

        public static void SwitchToNextWidth(BuildingDef currentlySelectedDef)
        {
            var originalDef = GetOriginalDefByDynamicDef(currentlySelectedDef);

            BuildingDef shiftedDef = null;
            if (currentlySelectedDef.WidthInCells + 1 <= ModSettings.Instance.MaxWidth)
            {
                shiftedDef = GetDynamicDefByOriginalDefAndWidth(originalDef, currentlySelectedDef.WidthInCells + 1);
            }
            else
            {
                // Upper limit for width range is reached. Roll back to the min width.
                shiftedDef = GetDynamicDefByOriginalDefAndWidth(originalDef, ModSettings.Instance.MinWidth);
            }

            SetActiveBuildingDef(shiftedDef, originalDef);
        }

        /// <summary>
        /// There are few approaches to replace current BuildingDef with dynamic BuildingDef. None is perfect.
        /// </summary>
        public static void SetActiveBuildingDef(BuildingDef shiftedDef, BuildingDef originalDef)
        {
            int algorithmToProceed = 1;

            switch (algorithmToProceed)
            {
                // Variant 1. Reset current 'def' value of 'BuildTool' and 'PrebuildTool'.
                // + The white siluette of enlarged building that is already placed but not yet built (is it BuildingUnderConstruction? or Preview?)
                //   and the final building (BuildingComplete) are displayed correctly, have correct width, and could be saved and loaded.
                // - The white siluette of enlarged building that is not yet placed (moves when mouse is moving) is not correctly displayed - its width
                //   equals oridinal BuildingDef's width.
                // - Button 'CopyLastBuilding' does not work
                case 1: 
                    Traverse.Create(BuildTool.Instance).Field("def").SetValue(shiftedDef);
                    Traverse.Create(PrebuildTool.Instance).Field("def").SetValue(shiftedDef);
                    PlanScreen.Instance.ProductInfoScreen.currentDef = shiftedDef;
                    //var lastSelectedBuilding = shiftedDef.BuildingComplete.GetComponent<Building>();
                    //Traverse.Create(PlanScreen.Instance).Field("LastSelectedBuilding").SetValue(lastSelectedBuilding);
                    //PlanScreen.Instance.RefreshCopyBuildingButton(null);
                    break;

                // Variant 2. Simulate 'OnSelectBuilding' with some parts thrown out.
                // - Does not work.
                case 2: 
                    PlanScreen_OnSelectBuilding_Ripoff(shiftedDef);
                    break;

                // Variant 3. Call the original 'OnSelectBuilding' with some workarounds.
                // - Does not work.
                case 3: 
                    // 'SelectedBuildingGameObject' should be set to null to avoid early exiting from the method (this field will be reset inside the method 'OnSelectBuilding').
                    var selectedBuildingGameObject = Traverse.Create(PlanScreen.Instance).Property("SelectedBuildingGameObject").GetValue() as UnityEngine.GameObject;
                    Traverse.Create(PlanScreen.Instance).Property("SelectedBuildingGameObject").SetValue(null);
                    // 'Tag' should be set to original 'Tag' because of some coding inside 'OnSelectBuilding'.
                    var shiftedDefTagSave = shiftedDef.Tag;
                    shiftedDef.Tag = originalDef.Tag;
                    ////var shiftedDefPrefabIDSave = shiftedDef.PrefabID;
                    ////shiftedDef.PrefabID = originalDef.PrefabID;
                    PlanScreen.Instance.OnSelectBuilding(selectedBuildingGameObject, shiftedDef, null);
                    ////shiftedDef.PrefabID = shiftedDefPrefabIDSave;
                    shiftedDef.Tag = shiftedDefTagSave;
                    break;

                default:
                    break;
            }
        }

        public static void PlanScreen_OnSelectBuilding_Ripoff(BuildingDef def, string facadeID = null)
        {
            var currentlySelectedToggle = Traverse.Create(PlanScreen.Instance).Field("currentlySelectedToggle").GetValue() as KToggle;
            PlanBuildingToggle planBuildingToggle = null;
            if (currentlySelectedToggle != null)
            {
                planBuildingToggle = currentlySelectedToggle.GetComponent<PlanBuildingToggle>();
            }

            PlanScreen.Instance.ProductInfoScreen.ClearProduct(false);
            if (planBuildingToggle != null)
            {
                planBuildingToggle.Refresh();
            }

            ToolMenu.Instance.ClearSelection();

            PrebuildTool.Instance.Activate(def, PlanScreen.Instance.GetTooltipForBuildable(def));
            ////PrebuildTool.Instance.Activate(def, "waaaaaagh");

            var lastSelectedBuilding = def.BuildingComplete.GetComponent<Building>();
            Traverse.Create(PlanScreen.Instance).Field("LastSelectedBuilding").SetValue(lastSelectedBuilding);
            PlanScreen.Instance.RefreshCopyBuildingButton(null);

            PlanScreen.Instance.ProductInfoScreen.Show(true);
            PlanScreen.Instance.ProductInfoScreen.ConfigureScreen(def, facadeID);
        }

        // public static bool CouldTypeBeDynamicallyExtended(string typeName) => configNameToObjectMap.ContainsKey(typeName) && configsToBeDynamicallyExtended.ContainsKey(GetConfigByName(typeName));
        public static bool CouldTypeBeDynamicallyExtended(string typeName)
        {
            if (!configNameToObjectMap.ContainsKey(typeName))
            {
                return false;
            }

            var config = GetConfigByName(typeName);
            return configsToBeDynamicallyExtended.ContainsKey(config);
        }

        public static bool IsOriginalForDynamicallyCreated(BuildingDef buildingDef) => originalDefToDynamicDefAndWidthMap.ContainsKey(buildingDef);
        public static bool IsDynamicallyCreated(BuildingDef buildingDef) => dynamicDefToOriginalDefMap.ContainsKey(buildingDef) && (dynamicDefToOriginalDefMap[buildingDef] != buildingDef);
        public static IBuildingConfig GetConfigByName(string configName) => configNameToObjectMap[configName];
        public static WidthRange GetWidthRange(IBuildingConfig config) => configsToBeDynamicallyExtended[config];
        public static BuildingDef GetBuildingDefByConfig(IBuildingConfig config) => _configTable[config];
        public static BuildingDef GetOriginalDefByDynamicDef(BuildingDef dynamicDef) => dynamicDefToOriginalDefMap[dynamicDef];
        public static BuildingDef GetDynamicDefByOriginalDefAndWidth(BuildingDef originalDef, int defWidth) => originalDefToDynamicDefAndWidthMap[originalDef][defWidth];
    }

}