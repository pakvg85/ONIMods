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
            Type typeFromHandle = typeof(IBuildingConfig);
            var extendableTypes = new List<Type>(types.Where(type =>
                    typeFromHandle.IsAssignableFrom(type)
                    && !type.IsAbstract
                    && !type.IsInterface
                    && DynamicBuildingsManager.CouldTypeBeDynamicallyExtended(type.Name)
                    ).ToList()
                );

            foreach (var type in extendableTypes)
            {
                try
                {
                    var config = DynamicBuildingsManager.GetConfigByName(type.Name);
                    DynamicBuildingsManager.RegisterDynamicBuildings(config);
                }
                catch (Exception e)
                {
                    DebugUtil.LogException(null, "Exception in Postfix RegisterBuilding for type " + type.FullName + " from " + type.Assembly.GetName().Name, e);
                }
            }
        }
    }

}