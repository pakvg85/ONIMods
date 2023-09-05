using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace ExtendedBuildingWidth
{
    public static class DynamicBuildingsManager
    {
        public static void RegisterDynamicBuildings_For_ExtendableConfigSettings()
        {
            var configTable = GetBuildingConfigManager_ConfigTable();
            var configNameToInstanceMapping = new Dictionary<string, IBuildingConfig>();
            foreach (var cfg in configTable.Keys.ToList())
            {
                try
                {
                    configNameToInstanceMapping.Add(cfg.GetType().FullName, cfg);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("ExtendedBuildingWidth WARNING - " + e.Message);
                }
            }

            IBuildingConfig config = null;
            var configsToBeExtended = ModSettings.GetExtendableConfigSettingsList();
            foreach (var configSettingsItem in configsToBeExtended)
            {
                try
                {
                    config = configNameToInstanceMapping[configSettingsItem.ConfigName];
                }
                catch (Exception e)
                {
                    config = null;
                    Debug.Log("ExtendedBuildingWidth ERROR - Exception while loading config " + configSettingsItem.ConfigName);
                    Debug.Log(e.Message);

                    continue;
                }

                try
                {
                    RegisterDynamicBuildings(config, configSettingsItem.MinWidth, configSettingsItem.MaxWidth, configSettingsItem.AnimStretchModifier);
                }
                catch (Exception e)
                {
                    Debug.Log("ExtendedBuildingWidth ERROR - Exception while register buildings for config " + configSettingsItem.ConfigName);
                    Debug.Log(e.Message);
                }
            }
        }

        private static void RegisterDynamicBuildings(IBuildingConfig config, int minWidth, int maxWidth, float animStretchModifier)
        {
            if (!DlcManager.IsDlcListValidForCurrentContent(config.GetDlcIds()))
            {
                return;
            }

            var originalDef = GetBuildingDefByConfig(config);
            for (var width = minWidth; width <= maxWidth; width++)
            {
                if (width != originalDef.WidthInCells)
                {
                    var dynamicDef = RegisterDynamicBuilding(config, width, originalDef.WidthInCells, animStretchModifier);
                    AddMapping(dynamicDef, originalDef);
                }
            }
            // Add the original 'BuildingDef' so it could also be picked when switching between widths via ALT+X and ALT+C.
            AddMapping(originalDef, originalDef);
        }

        /// <summary>
        /// This method is ripped from standard 'BuildingConfigManager.RegisterBuilding', few adjustments are made to create dynamic 'BuildingDef' entities.
        /// </summary>
        private static BuildingDef RegisterDynamicBuilding(IBuildingConfig config, int width, int originalWidth, float animStretchModifier)
        {
            // Fields 'dynamicDef.PrefabID' and 'dynamicDef.WidthInCells' will be adjusted in Prefix of 'Patch_BuildingTemplates_CreateBuildingDef'
            Patch_BuildingTemplates_CreateBuildingDef.CreatingDynamicBuildingDefStarted = true;
            Patch_BuildingTemplates_CreateBuildingDef.WidthDeltaForDynamicBuildingDef = width - originalWidth;
            Patch_GeneratedBuildings_RegisterWithOverlay.CreatingDynamicBuildingDefStarted = true;
            BuildingDef dynamicDef = config.CreateBuildingDef();
            Patch_GeneratedBuildings_RegisterWithOverlay.CreatingDynamicBuildingDefStarted = false;
            Patch_BuildingTemplates_CreateBuildingDef.WidthDeltaForDynamicBuildingDef = 0;
            Patch_BuildingTemplates_CreateBuildingDef.CreatingDynamicBuildingDefStarted = false;

            // Utility Offset fields should be overwritten because they are generated independently in 'IBuildingConfig.CreateBuildingDef' implementations.
            AdjustUtilityPortsOffsets(dynamicDef);

            var originalDef = GetBuildingDefByConfig(config);
            // 'Strings' entities are copied from the original
            AdjustStrings(dynamicDef, originalDef);

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

            AdjustLogicPortsOffsets(dynamicDef);
            AdjustPowerPortsOffsets(dynamicDef);

            Assets.AddBuildingDef(dynamicDef);

            // ---
            // 'Building' game objects for dynamically created BuildingDefs have to be stretched explicitly.
            // ---
            StretchBuildingGameObject(dynamicDef.BuildingUnderConstruction, width, originalWidth, animStretchModifier);
            StretchBuildingGameObject(dynamicDef.BuildingPreview, width, originalWidth, animStretchModifier);
            StretchBuildingGameObject(dynamicDef.BuildingComplete, width, originalWidth, animStretchModifier);

            return dynamicDef;
        }

        public static void StretchBuildingGameObject(UnityEngine.GameObject gameObject, int width, int originalWidth, float animStretchModifier)
        {
            try
            {
                var buildingGameObject = gameObject.GetComponent<Building>().gameObject;
                var animController = buildingGameObject.GetComponent<KBatchedAnimController>();
                // Just stretching animController width for a building does not look fine (because for example dynamic bridges get progressively
                // more narrow), so it should be adjusted with modifier. For gas bridge this modifier is '1.12f'
                animController.animWidth = (float)width / (float)originalWidth * animStretchModifier;
            }
            catch (Exception e)
            {
                Debug.Log("ExtendedBuildingWidth ERROR - stretching failed for " + gameObject.name + " because:");
                Debug.Log(e.Message);
            }
        }

        /// <summary>
        /// Extending logic port offsets (supposedly for logic bridges).
        /// </summary>
        private static void AdjustLogicPortsOffsets(BuildingDef buildingDef)
        {
            if (buildingDef.LogicInputPorts == null || buildingDef.LogicInputPorts.Count == 0)
            {
                return;
            }

            var newLogicPorts = new List<LogicPorts.Port>();
            foreach(var port in buildingDef.LogicInputPorts)
            {
                var newPort = port;
                AdjustPortOffset(ref newPort.cellOffset, buildingDef.WidthInCells);
                newLogicPorts.Add(newPort);
            }
            buildingDef.LogicInputPorts.Clear();
            buildingDef.LogicInputPorts.AddRange(newLogicPorts.ToArray());

            ReInitializeLogicPorts(buildingDef.BuildingComplete, buildingDef);
            ReInitializeLogicPorts(buildingDef.BuildingPreview, buildingDef);
            ReInitializeLogicPorts(buildingDef.BuildingUnderConstruction, buildingDef);

            if (buildingDef.BuildingComplete.TryGetComponent<LogicUtilityNetworkLink>(out var logicNetworkLinkBuildingComplete))
            {
                AdjustPortOffset(ref logicNetworkLinkBuildingComplete.link1, buildingDef.WidthInCells);
                AdjustPortOffset(ref logicNetworkLinkBuildingComplete.link2, buildingDef.WidthInCells);
            }
            if (buildingDef.BuildingUnderConstruction.TryGetComponent<LogicUtilityNetworkLink>(out var logicNetworkLinkBuildingUnderConstruction))
            {
                AdjustPortOffset(ref logicNetworkLinkBuildingUnderConstruction.link1, buildingDef.WidthInCells);
                AdjustPortOffset(ref logicNetworkLinkBuildingUnderConstruction.link2, buildingDef.WidthInCells);
            }
            if (buildingDef.BuildingPreview.TryGetComponent<LogicUtilityNetworkLink>(out var logicNetworkLinkBuildingPreview))
            {
                AdjustPortOffset(ref logicNetworkLinkBuildingPreview.link1, buildingDef.WidthInCells);
                AdjustPortOffset(ref logicNetworkLinkBuildingPreview.link2, buildingDef.WidthInCells);
            }
        }

        private static void ReInitializeLogicPorts(UnityEngine.GameObject gameObject, BuildingDef buildingDef)
        {
            if (buildingDef.LogicInputPorts != null)
            {
                gameObject.AddOrGet<LogicPorts>().inputPortInfo = buildingDef.LogicInputPorts.ToArray();
            }
            if (buildingDef.LogicOutputPorts != null)
            {
                gameObject.AddOrGet<LogicPorts>().outputPortInfo = buildingDef.LogicOutputPorts.ToArray();
            }
        }

        private static void AdjustPowerPortsOffsets(BuildingDef buildingDef)
        {
            if (buildingDef.BuildingComplete.TryGetComponent<WireUtilityNetworkLink>(out var wireNetworkLinkBuildingComplete))
            {
                AdjustPortOffset(ref wireNetworkLinkBuildingComplete.link1, buildingDef.WidthInCells);
                AdjustPortOffset(ref wireNetworkLinkBuildingComplete.link2, buildingDef.WidthInCells);
            }
            if (buildingDef.BuildingUnderConstruction.TryGetComponent<WireUtilityNetworkLink>(out var wireNetworkLinkBuildingUnderConstruction))
            {
                AdjustPortOffset(ref wireNetworkLinkBuildingUnderConstruction.link1, buildingDef.WidthInCells);
                AdjustPortOffset(ref wireNetworkLinkBuildingUnderConstruction.link2, buildingDef.WidthInCells);
            }
            if (buildingDef.BuildingPreview.TryGetComponent<WireUtilityNetworkLink>(out var wireNetworkLinkBuildingPreview))
            {
                AdjustPortOffset(ref wireNetworkLinkBuildingPreview.link1, buildingDef.WidthInCells);
                AdjustPortOffset(ref wireNetworkLinkBuildingPreview.link2, buildingDef.WidthInCells);
            }
        }

        /// <summary>
        /// Accessing struct CellOffset without 'ref' modifier will result in passing the parameter by value and not changing the original.
        /// </summary>
        public static void AdjustPortOffset(ref CellOffset portCellOffset, int width)
        {
            portCellOffset.x = AdjustOffsetValueByWidth(portCellOffset.x, width);
        }

        /// <summary>
        /// Default bridge utility offsets are '(-1;0),(1;0)'. When a bridge is extended, the offsets should also be adjusted.
        /// For example, offsets for width 4 should be '(-1;0),(2;0)', for width 5: '(-2;0),(2;0)', for width 6: '(-2;0),(3;0)' etc.
        /// Notice that integer division operation will round the results, so "1/2 = 0", "2/2 = 1", "3/2 = 1", "4/2 = 2", "5/2 = 2" etc.
        /// </summary>
        private static void AdjustUtilityPortsOffsets(BuildingDef buildingDef)
        {
            AdjustPortOffset(ref buildingDef.UtilityInputOffset, buildingDef.WidthInCells);
            AdjustPortOffset(ref buildingDef.UtilityOutputOffset, buildingDef.WidthInCells);
        }

        /// <summary>
        /// If an utility or logic port default offset value (X) equals -1, then we suggest that this port have to be placed to the left side.
        /// If that value equals +1 - then this port have to be placed to the right side.
        /// If it equals 0 - then it should not be adjusted and kept as it is.
        /// </summary>
        private static int AdjustOffsetValueByWidth(int defaultOffsetValue, int width) => (defaultOffsetValue < 0) ? -(width - 1) / 2 : (defaultOffsetValue > 0) ? (width) / 2 : 0;

        private static void AdjustStrings(BuildingDef dynamicDef, BuildingDef originalDef)
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

            Strings.Add(nameID, origNameEntry + " (width " + dynamicDef.WidthInCells + ")");
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

        public static bool SwitchToPrevWidth(BuildingDef currentlySelectedDef)
        {
            var originalDef = GetOriginalDefByDynamicDef(currentlySelectedDef);
            if (!HasDynamicDefByOriginalDefAndWidth(originalDef, currentlySelectedDef.WidthInCells - 1))
            {
                return false;
            }
            var shiftedDef = GetDynamicDefByOriginalDefAndWidth(originalDef, currentlySelectedDef.WidthInCells - 1);

            SetActiveBuildingDef(shiftedDef);
            return true;
        }

        public static bool SwitchToNextWidth(BuildingDef currentlySelectedDef)
        {
            var originalDef = GetOriginalDefByDynamicDef(currentlySelectedDef);
            if (!HasDynamicDefByOriginalDefAndWidth(originalDef, currentlySelectedDef.WidthInCells + 1))
            {
                return false;
            }
            var shiftedDef = GetDynamicDefByOriginalDefAndWidth(originalDef, currentlySelectedDef.WidthInCells + 1);

            SetActiveBuildingDef(shiftedDef);
            return true;
        }

        /// <summary>
        /// Standard buildings are bound to buttons in build menu, so when such button is pressed, method 'PlanScreen.OnSelectBuilding' is triggered.
        /// Dynamic buildings are not bound to buttons, so some workaround should be done to visualize particular dynamic building.
        /// </summary>
        public static void SetActiveBuildingDef(BuildingDef dynamicDef)
        {
            var currentOrientation = (Orientation)(Traverse.Create(BuildTool.Instance).Field("buildingOrientation").GetValue() as object);

            PlanScreen_OnSelectBuilding_HugeRipoff(dynamicDef);

            BuildTool.Instance.SetToolOrientation(currentOrientation);
        }

        /// <summary>
        /// + The white siluette of enlarged building that is already placed but not yet built (is it BuildingUnderConstruction? or Preview?)
        ///   and the final building (BuildingComplete) are displayed correctly (have correct width), and could be saved and loaded.
        /// + The white siluette of enlarged building that is not yet placed (moves when mouse is moving) is also displayed correctly.
        /// + Hovering text card over the dynamic building shows correct required resources
        /// - Button 'CopyLastBuilding' does not work for enlarged buildings. Enlarged building is replaced with its original in 'Patch_PlanScreen_OnClickCopyBuilding'
        /// - Material selection screen does not show correct amount of materials for enlarged buildings. It could be probably
        ///   fixed via correct call of 'PlanScreen.Instance.ProductInfoScreen.ConfigureScreen', but for now it causes exception deep inside
        /// </summary>
        public static void PlanScreen_OnSelectBuilding_HugeRipoff(BuildingDef dynamicDef)
        {
            Traverse.Create(PrebuildTool.Instance).Field("def").SetValue(dynamicDef);

            BuildTool.Instance.Activate(dynamicDef,
                PlanScreen.Instance.ProductInfoScreen.materialSelectionPanel.GetSelectedElementAsList,
                PlanScreen.Instance.ProductInfoScreen.FacadeSelectionPanel.SelectedFacade);

            var building = dynamicDef.BuildingComplete.GetComponent<Building>();
            Traverse.Create(PlanScreen.Instance).Property("LastSelectedBuilding").SetValue(building);
        }

        /// <summary>
        /// Simulate 'OnSelectBuilding' with some parts thrown out. Does not work
        /// </summary>
        public static void PlanScreen_OnSelectBuilding_Ripoff(BuildingDef dynamicDef, string facadeID = null)
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

            PrebuildTool.Instance.Activate(dynamicDef, PlanScreen.Instance.GetTooltipForBuildable(dynamicDef));

            var lastSelectedBuilding = dynamicDef.BuildingComplete.GetComponent<Building>();
            Traverse.Create(PlanScreen.Instance).Field("LastSelectedBuilding").SetValue(lastSelectedBuilding);
            PlanScreen.Instance.RefreshCopyBuildingButton(null);

            PlanScreen.Instance.ProductInfoScreen.Show(true);
            PlanScreen.Instance.ProductInfoScreen.ConfigureScreen(dynamicDef, facadeID);
        }

        /// <summary>
        /// Call the original 'OnSelectBuilding' with minimuum workarounds. Does not work
        /// </summary>
        public static void PlanScreen_OnSelectBuilding(BuildingDef dynamicDef)
        {
            var originalDef = GetOriginalDefByDynamicDef(dynamicDef);

            // 'SelectedBuildingGameObject' should be set to null to avoid early exiting from the method (this field will be reset inside the method 'OnSelectBuilding').
            var selectedBuildingGameObject = Traverse.Create(PlanScreen.Instance).Property("SelectedBuildingGameObject").GetValue() as UnityEngine.GameObject;
            Traverse.Create(PlanScreen.Instance).Property("SelectedBuildingGameObject").SetValue(null);
            // 'Tag' should be set to original 'Tag' because of some coding inside 'OnSelectBuilding'.
            var dynamicDefTagSave = dynamicDef.Tag;
            dynamicDef.Tag = originalDef.Tag;
            // Dynamic 'PrefabID' value could also cause exceptions because dynamic buildings are not attached to buttons in main build menu.
            ////var dynamicDefPrefabIDSave = dynamicDef.PrefabID;
            ////dynamicDef.PrefabID = originalDef.PrefabID;
            PlanScreen.Instance.OnSelectBuilding(selectedBuildingGameObject, dynamicDef, null);
            ////dynamicDef.PrefabID = dynamicDefPrefabIDSave;
            dynamicDef.Tag = dynamicDefTagSave;
        }

        public static Dictionary<IBuildingConfig, BuildingDef> GetBuildingConfigManager_ConfigTable() => _configTable;
        public static bool IsOriginalForDynamicallyCreated(BuildingDef buildingDef) => originalDefToDynamicDefAndWidthMap.ContainsKey(buildingDef);
        public static bool IsDynamicallyCreated(BuildingDef buildingDef) => dynamicDefToOriginalDefMap.ContainsKey(buildingDef) && (dynamicDefToOriginalDefMap[buildingDef] != buildingDef);
        public static BuildingDef GetBuildingDefByConfig(IBuildingConfig config) => _configTable[config];
        public static BuildingDef GetOriginalDefByDynamicDef(BuildingDef dynamicDef) => dynamicDefToOriginalDefMap[dynamicDef];
        public static BuildingDef GetDynamicDefByOriginalDefAndWidth(BuildingDef originalDef, int defWidth) => originalDefToDynamicDefAndWidthMap[originalDef][defWidth];
        public static bool HasDynamicDefByOriginalDefAndWidth(BuildingDef originalDef, int defWidth) => originalDefToDynamicDefAndWidthMap.ContainsKey(originalDef) && originalDefToDynamicDefAndWidthMap[originalDef].ContainsKey(defWidth);

        private static Dictionary<IBuildingConfig, BuildingDef> _configTable = Traverse.Create(BuildingConfigManager.Instance).Field("configTable").GetValue() as Dictionary<IBuildingConfig, BuildingDef>;
        private static Dictionary<BuildingDef, BuildingDef> dynamicDefToOriginalDefMap = new Dictionary<BuildingDef, BuildingDef>();
        private static Dictionary<BuildingDef, Dictionary<int, BuildingDef>> originalDefToDynamicDefAndWidthMap = new Dictionary<BuildingDef, Dictionary<int, BuildingDef>>();
    }

}