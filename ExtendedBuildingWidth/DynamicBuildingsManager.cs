using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using PeterHan.PLib.Options;

namespace ExtendedBuildingWidth
{
    public static class DynamicBuildingsManager
    {
        public static void RegisterDynamicBuildings_For_ExtendableConfigSettings()
        {
            var dummyModSettings = POptions.ReadSettings<ModSettings>() ?? new ModSettings();
            var configsToBeExtended = dummyModSettings.GetExtendableConfigSettingsList();
            var configNameToAnimNamesMap = dummyModSettings.GetConfigNameToAnimNameMap();

            var splitSettingsList = dummyModSettings.GetAnimSplittingSettingsList();
            Dictionary<string, List<AnimSplittingSettings>> splittingSettingsDict =
                splitSettingsList.GroupBy(r => r.ConfigName).ToDictionary(t => t.Key, t => t.Select(r => r).ToList());

            foreach (var configSettings in configsToBeExtended)
            {
                IBuildingConfig config;
                if (!ConfigMap.TryGetValue(configSettings.ConfigName, out config))
                {
                    Debug.LogWarning($"ExtendedBuildingWidth - DynamicBuildingsManager: config {configSettings.ConfigName} was not loaded");
                    continue;
                }

                try
                {
                    if (!DlcManager.IsDlcListValidForCurrentContent(config.GetDlcIds()))
                    {
                        continue;
                    }

                    var originalDef = ConfigToBuildingDefMap[config];
                    for (var width = configSettings.MinWidth; width <= configSettings.MaxWidth; width++)
                    {
                        if (width == originalDef.WidthInCells)
                        {
                            continue;
                        }

                        var dynamicDef = CreateDynamicDef(config, width);

                        var originalWidth = originalDef.WidthInCells;
                        var widthInCells = dynamicDef.WidthInCells;
                        var widthInCellsDelta = widthInCells - originalWidth;

                        bool canSplitAnim = false;
                        if (   configNameToAnimNamesMap.TryGetValue(configSettings.ConfigName, out var origAnimName)
                            && splittingSettingsDict.TryGetValue(configSettings.ConfigName, out var splittingSettingsItems)
                            && splittingSettingsItems.Any(x => x.IsActive)
                            && widthInCellsDelta > 0
                            )
                        {
                            canSplitAnim = true;
                            var dynamicAnimName = GetDynamicName(origAnimName, widthInCells);
                            DynamicAnimManager.OverwriteAnimFiles(dynamicDef, dynamicAnimName);
                            DynamicAnimManager.SplitAnim(
                                animFile: dynamicDef.AnimFiles.First(),
                                widthInCellsDelta: widthInCellsDelta,
                                settingsItems: splittingSettingsItems);
                        }

                        RegisterEverythingElse(dynamicDef, originalDef, config);

                        if (!canSplitAnim)
                        {
                            DynamicAnimManager.StretchBuildingGameObject(dynamicDef.BuildingComplete, widthInCells, originalWidth, configSettings.AnimStretchModifier);
                            DynamicAnimManager.StretchBuildingGameObject(dynamicDef.BuildingPreview, widthInCells, originalWidth, configSettings.AnimStretchModifier);
                            DynamicAnimManager.StretchBuildingGameObject(dynamicDef.BuildingUnderConstruction, widthInCells, originalWidth, configSettings.AnimStretchModifier);
                        }
                        //else
                        //{
                        //    var animController = dynamicDef.BuildingComplete.GetComponent<KBatchedAnimController>();
                        //    var batch = animController.GetBatch();
                        //    animController.SetDirty();
                        //}

                        AddMapping(dynamicDef, originalDef);
                    }
                    // Add the original 'BuildingDef' so it could also be picked when switching between widths via ALT+X and ALT+C.
                    AddMapping(originalDef, originalDef);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"ExtendedBuildingWidth - exception while register buildings for config {configSettings.ConfigName}");
                    Debug.LogWarning(e.ToString());
                }
            }

            ApplyCompatibilityWith_HighPressureApplications();
        }

        /// <summary>
        /// Bridges of 'High_Pressure_Applications' have higher capacity of its 'Conduit' and 'ConduitBridge' objects.
        /// To apply that higher capacity values to the newly created High_Pressure_Applications bridges,
        /// we have to add some values to 'PressurizedTuning.PressurizedLookup' dictionary of 'High_Pressure_Applications' lib.
        /// To do so, we must call method 'PressurizedTuning.TryAddPressurizedInfo' to all the newly created dynamic PrefabID.
        /// </summary>
        private static void ApplyCompatibilityWith_HighPressureApplications()
        {
            try
            {
                Type dynamicType_PressurizedTuning = null;

                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    dynamicType_PressurizedTuning = assemblies[i].GetTypes().Where(x =>
                        x.Namespace == "High_Pressure_Applications"
                        && x.Name.Contains("PressurizedTuning")
                        ).FirstOrDefault();

                    if (dynamicType_PressurizedTuning != null)
                    {
                        break;
                    }
                }
                if (dynamicType_PressurizedTuning is null)
                {
                    return;
                }

                var dynamicMethod_GetPressurizedInfo = dynamicType_PressurizedTuning.GetMethod("GetPressurizedInfo");
                var dynamicMethod_TryAddPressurizedInfo = dynamicType_PressurizedTuning.GetMethod("TryAddPressurizedInfo");

                foreach (var originalDef in OriginalDefToDynamicDefAndWidthMap.Keys)
                {
                    bool methodTryAddPressurizedInfo_WasSuccessfullyCalledAtLeastOnce = false;

                    foreach (var keyValuePair in OriginalDefToDynamicDefAndWidthMap[originalDef])
                    {
                        var dynamicDef = keyValuePair.Value;

                        if (dynamicDef.PrefabID == originalDef.PrefabID)
                        {
                            continue;
                        }

                        var parameters = new List<object>();
                        parameters.Add(originalDef.PrefabID);
                        var dynamicObject_PressurizedInfo = dynamicMethod_GetPressurizedInfo.Invoke(dynamicType_PressurizedTuning, parameters.ToArray());
                        if (dynamicObject_PressurizedInfo is null)
                        {
                            return;
                        }

                        var resultType = dynamicObject_PressurizedInfo.GetType();
                        if (resultType is null)
                        {
                            return;
                        }

                        var dynamicProperty_IsDefault = resultType.GetField("IsDefault");
                        var dynamicProperty_Capacity = resultType.GetField("Capacity");
                        if (dynamicProperty_IsDefault is null || dynamicProperty_Capacity is null)
                        {
                            continue;
                        }

                        var dynamicProperty_IsDefault_Value = dynamicProperty_IsDefault.GetValue(dynamicObject_PressurizedInfo);
                        var dynamicProperty_Capacity_Value = dynamicProperty_Capacity.GetValue(dynamicObject_PressurizedInfo);
                        if (dynamicProperty_IsDefault_Value is null || dynamicProperty_Capacity_Value is null)
                        {
                            continue;
                        }

                        var boolIsDefault = (bool)dynamicProperty_IsDefault_Value;
                        if (!boolIsDefault)
                        {
                            var parameters2 = new List<object>();
                            parameters2.Add(dynamicDef.PrefabID);
                            parameters2.Add(dynamicObject_PressurizedInfo);
                            dynamicMethod_TryAddPressurizedInfo.Invoke(dynamicType_PressurizedTuning, parameters2.ToArray());
                            methodTryAddPressurizedInfo_WasSuccessfullyCalledAtLeastOnce = true;
                        }
                    }

                    if (methodTryAddPressurizedInfo_WasSuccessfullyCalledAtLeastOnce)
                    {
                        Debug.Log("ExtendedBuildingWidth - 'PressurizedTuning.PressurizedLookup' successfully extended for '" + originalDef.PrefabID + "'");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("ExtendedBuildingWidth - failed to apply compability with 'High_Pressure_Applications'");
                Debug.LogWarning(e.ToString());
            }
        }

        private static BuildingDef CreateDynamicDef(IBuildingConfig config, int width)
        {
            var originalDef = ConfigToBuildingDefMap[config];

            // Fields 'dynamicDef.PrefabID' and 'dynamicDef.WidthInCells' will be adjusted in Prefix of 'Patch_BuildingTemplates_CreateBuildingDef'
            Patch_BuildingTemplates_CreateBuildingDef.CreatingDynamicBuildingDefStarted = true;
            Patch_BuildingTemplates_CreateBuildingDef.NewWidthForDynamicBuildingDef = width;
            Patch_GeneratedBuildings_RegisterWithOverlay.CreatingDynamicBuildingDefStarted = true;

            BuildingDef dynamicDef = config.CreateBuildingDef();

            Patch_GeneratedBuildings_RegisterWithOverlay.CreatingDynamicBuildingDefStarted = false;
            Patch_BuildingTemplates_CreateBuildingDef.NewWidthForDynamicBuildingDef = 0;
            Patch_BuildingTemplates_CreateBuildingDef.CreatingDynamicBuildingDefStarted = false;

            return dynamicDef;
        }

        private static void RegisterEverythingElse(BuildingDef dynamicDef, BuildingDef originalDef, IBuildingConfig config)
        {
            // Utility Offset fields should be overwritten because they are generated independently in 'IBuildingConfig.CreateBuildingDef' implementations.
            AdjustUtilityPortsOffsets(dynamicDef);

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
            foreach (var port in buildingDef.LogicInputPorts)
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
            var width = buildingDef.WidthInCells;
            if (buildingDef.IsFoundation)
            {
                width = buildingDef.WidthInCells + 2;
            }

            if (buildingDef.BuildingComplete.TryGetComponent<WireUtilityNetworkLink>(out var wireNetworkLinkBuildingComplete))
            {
                AdjustPortOffset(ref wireNetworkLinkBuildingComplete.link1, width);
                AdjustPortOffset(ref wireNetworkLinkBuildingComplete.link2, width);
            }
            if (buildingDef.BuildingUnderConstruction.TryGetComponent<WireUtilityNetworkLink>(out var wireNetworkLinkBuildingUnderConstruction))
            {
                AdjustPortOffset(ref wireNetworkLinkBuildingUnderConstruction.link1, width);
                AdjustPortOffset(ref wireNetworkLinkBuildingUnderConstruction.link2, width);
            }
            if (buildingDef.BuildingPreview.TryGetComponent<WireUtilityNetworkLink>(out var wireNetworkLinkBuildingPreview))
            {
                AdjustPortOffset(ref wireNetworkLinkBuildingPreview.link1, width);
                AdjustPortOffset(ref wireNetworkLinkBuildingPreview.link2, width);
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
        public static int AdjustOffsetValueByWidth(int defaultOffsetValue, int width) => (defaultOffsetValue < 0) ? -(width - 1) / 2 : (defaultOffsetValue > 0) ? (width) / 2 : 0;

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
            DynamicDefToOriginalDefMap.Add(dynamicDef, originalDef);

            Dictionary<int, BuildingDef> widthToDynamicDefMap;
            if (!OriginalDefToDynamicDefAndWidthMap.TryGetValue(originalDef, out widthToDynamicDefMap))
            {
                OriginalDefToDynamicDefAndWidthMap.Add(originalDef, new Dictionary<int, BuildingDef>());
                widthToDynamicDefMap = OriginalDefToDynamicDefAndWidthMap[originalDef];
            }

            widthToDynamicDefMap.Add(dynamicDef.WidthInCells, dynamicDef);
        }

        public static bool SwitchToPrevWidth(BuildingDef currentlySelectedDef)
        {
            var originalDef = DynamicDefToOriginalDefMap[currentlySelectedDef];
            if (!HasDynamicDefByOriginalDefAndWidth(originalDef, currentlySelectedDef.WidthInCells - 1))
            {
                return false;
            }
            var shiftedDef = OriginalDefToDynamicDefAndWidthMap[originalDef][currentlySelectedDef.WidthInCells - 1];

            SetActiveBuildingDef(shiftedDef);
            return true;
        }

        public static bool SwitchToNextWidth(BuildingDef currentlySelectedDef)
        {
            var originalDef = DynamicDefToOriginalDefMap[currentlySelectedDef];
            if (!HasDynamicDefByOriginalDefAndWidth(originalDef, currentlySelectedDef.WidthInCells + 1))
            {
                return false;
            }
            var shiftedDef = OriginalDefToDynamicDefAndWidthMap[originalDef][currentlySelectedDef.WidthInCells + 1];

            SetActiveBuildingDef(shiftedDef);
            return true;
        }

        /// <summary>
        /// Standard buildings are bound to buttons in build menu, so when such button is pressed, method 'PlanScreen.OnSelectBuilding' is triggered.
        /// Dynamic buildings are not bound to buttons, so some workaround should be done to visualize particular dynamic building.
        /// </summary>
        private static void SetActiveBuildingDef(BuildingDef dynamicDef)
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
        private static void PlanScreen_OnSelectBuilding_HugeRipoff(BuildingDef dynamicDef)
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
        private static void PlanScreen_OnSelectBuilding_Ripoff(BuildingDef dynamicDef, string facadeID = null)
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
        private static void PlanScreen_OnSelectBuilding(BuildingDef dynamicDef)
        {
            var originalDef = DynamicDefToOriginalDefMap[dynamicDef];

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

        public static string GetDynamicName(string origName, int width) => origName + "_width" + width.ToString();

        public static bool IsOriginalForDynamicallyCreated(BuildingDef buildingDef) => OriginalDefToDynamicDefAndWidthMap.ContainsKey(buildingDef);
        public static bool IsDynamicallyCreated(BuildingDef buildingDef) => DynamicDefToOriginalDefMap.ContainsKey(buildingDef) && (DynamicDefToOriginalDefMap[buildingDef] != buildingDef);
        public static bool HasDynamicDefByOriginalDefAndWidth(BuildingDef originalDef, int defWidth) => OriginalDefToDynamicDefAndWidthMap.ContainsKey(originalDef) && OriginalDefToDynamicDefAndWidthMap[originalDef].ContainsKey(defWidth);

        public static Dictionary<IBuildingConfig, BuildingDef> ConfigToBuildingDefMap
        {
            get
            {
                if (_configToBuildingDefMap == null)
                {
                    var configTable = Traverse.Create(BuildingConfigManager.Instance).Field("configTable").GetValue() as Dictionary<IBuildingConfig, BuildingDef>;
                    _configToBuildingDefMap = configTable;
                }
                return _configToBuildingDefMap;
            }
        }
        private static Dictionary<IBuildingConfig, BuildingDef> _configToBuildingDefMap;

        public static Dictionary<string, IBuildingConfig> PrefabIdToConfigMap
        {
            get
            {
                if (_prefabIdToConfigMap == null)
                {
                    _prefabIdToConfigMap = ConfigToBuildingDefMap.ToDictionary(x => x.Value.PrefabID, x => x.Key);
                }
                return _prefabIdToConfigMap;
            }
        }
        private static Dictionary<string, IBuildingConfig> _prefabIdToConfigMap;

        public static Dictionary<string, IBuildingConfig> ConfigMap
        {
            get
            {
                if (_configNameToInstanceMap == null)
                {
                    _configNameToInstanceMap = ConfigToBuildingDefMap.Keys.ToDictionary(x => x.GetType().FullName, y => y);
                }
                return _configNameToInstanceMap;
            }
        }
        private static Dictionary<string, IBuildingConfig> _configNameToInstanceMap;

        public static Dictionary<BuildingDef, BuildingDef> DynamicDefToOriginalDefMap { get; } = new Dictionary<BuildingDef, BuildingDef>();
        public static Dictionary<BuildingDef, Dictionary<int, BuildingDef>> OriginalDefToDynamicDefAndWidthMap { get; } = new Dictionary<BuildingDef, Dictionary<int, BuildingDef>>();

        public static Dictionary<BuildingDef, string> BuildingDefToAnimNameMap
        {
            get
            {
                if (_buildingDefToAnimNameMap == null)
                {
                    _buildingDefToAnimNameMap = ConfigToBuildingDefMap.ToDictionary(x => x.Value, x => x.Value.AnimFiles.First().name);
                }
                return _buildingDefToAnimNameMap;
                ;
            }
        }
        private static Dictionary<BuildingDef, string> _buildingDefToAnimNameMap;

        /// <summary>
        /// This dictionary is created for all in-game buildings.
        /// Do not confuse with 'ModSettings.ConfigNameToAnimNameMap'.
        /// </summary>
        public static Dictionary<string, string> ConfigNameToAnimNameMap
        {
            get
            {
                if (_configNameToAnimNameMap == null)
                {
                    //ConfigMap                     configName -> IBuldingConfig
                    //ConfigToBuildingDefMap        IBuldingConfig -> BuildingDef
                    //BuildingDefToAnimNameMap      BuildingDef -> AnimName
                    _configNameToAnimNameMap =
                        (from c1 in ConfigMap
                         join c2 in ConfigToBuildingDefMap on c1.Value equals c2.Key
                         join c3 in BuildingDefToAnimNameMap on c2.Value equals c3.Key
                         select new { ConfigName = c1.Key, AnimName = c3.Value })
                        .ToDictionary(x => x.ConfigName, y => y.AnimName);
                }
                return _configNameToAnimNameMap;
            }
        }
        private static Dictionary<string, string> _configNameToAnimNameMap;
    }
}