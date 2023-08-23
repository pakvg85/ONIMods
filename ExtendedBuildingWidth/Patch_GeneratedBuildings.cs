using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace ExtendedBuildingWidth
{
    [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
    public class Patch_GeneratedBuildings_LoadGeneratedBuildings
    {
        public static void Postfix(List<Type> types)
        {
            var configTable = DynamicBuildingsManager.GetBuildingConfigManager_ConfigTable();
            var configNameToInstanceMapping = new Dictionary<string, IBuildingConfig>();
            configTable.Keys.ToList().ForEach(
                    config => configNameToInstanceMapping.Add(config.GetType().FullName, config)
                );

            var configsToBeExtended = ModSettings.GetExtendableConfigSettingsList();

            foreach (var configSettingsItem in configsToBeExtended)
            {
                try
                {
                    var config = configNameToInstanceMapping[configSettingsItem.ConfigName];
                    DynamicBuildingsManager.RegisterDynamicBuildings(config, configSettingsItem.MinWidth, configSettingsItem.MaxWidth, configSettingsItem.AnimStretchModifier);
                }
                catch (Exception e)
                {
                    DebugUtil.LogException(null, "Exception in Postfix RegisterBuilding for type " + configSettingsItem.ConfigName, e);
                }
            }
        }
    }

}