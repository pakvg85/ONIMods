using Newtonsoft.Json;
using PeterHan.PLib.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ExtendedBuildingWidth
{
    public class ExtendableConfigSettings
    {
        public string ConfigName { get; set; }
        public int MinWidth { get; set; }
        public int MaxWidth { get; set; }
        public float AnimStretchModifier { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    [RestartRequired]
    [ConfigFileAttribute(SharedConfigLocation:true)]
    public class ModSettings : SingletonOptions<ModSettings>
    {
        [JsonProperty]
        public string ExtendableConfigSettings { get; set; }

        /// <summary>
        /// The constructor is called inside getter method 'Instance.get' of 'SingletonOptions' in both cases:
        /// 1) if 'config.json' is read via 'POptions.ReadSettings' (in this case the constructor results are overwritten)
        /// 2) if 'config.json' is not read, and new 'Instance' is created via 'Activator.CreateInstance'
        /// </summary>
        public ModSettings()
        {
            ExtendableConfigSettings = JsonConvert.SerializeObject(GenerateDefaultValues_For_ExtendableConfigSettings());
        }

        [Option("Create list of all buildings", "Create a file with all available buildings in the game")]
        public System.Action<object> Button_CreateFileWithAllAvailableBuildings => Action_CreateFileWithAllAvailableBuildings;

        private void Action_CreateFileWithAllAvailableBuildings(object obj)
        {
            CreateFileWithAllAvailableBuildings();
            System.Diagnostics.Process.Start(Get_AllAvailableBuildings_Path());
            System.Diagnostics.Process.Start(Get_AllAvailableBuildings_FullFileName());
        }

        const string AllAvailableBuildings_FileName = "AllAvailableBuildings.txt";

        private static string Get_AllAvailableBuildings_Path() => Path.GetDirectoryName(POptions.GetConfigFilePath(typeof(ModSettings)));
        private static string Get_AllAvailableBuildings_FullFileName() => Get_AllAvailableBuildings_Path() + "\\" + AllAvailableBuildings_FileName;

        private static Dictionary<string, Type> FindAllSubclassesOf_STRINGS_BUILDINGS_PREFABS()
        {
            var result = new Dictionary<string, Type>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                assemblies[i].GetTypes().Where(j => j.Name == "BUILDINGS").ToList().ForEach(y =>
                    y.GetNestedTypes().Where(k => k.Name == "PREFABS").ToList().ForEach(z =>
                        z.GetNestedTypes().Where(m => !result.ContainsKey(m.Name)).ToList().ForEach(u =>
                            result.Add(u.Name, u)
                        )
                    )
                );
            }
            return result;
        }

        /// <summary>
        /// For some reason, localization texts are maintained via prefabId of BuildingDefConfig classes
        /// </summary>
        private static string GetTextFromLocalization(Dictionary<string, Type> localizationStrings, string prefabIdUpper, string fieldName)
        {
            string result = null;

            if (!localizationStrings.ContainsKey(prefabIdUpper))
            {
                return result;
            }

            var nestedClassTypeInfo = localizationStrings[prefabIdUpper];
            var fieldInfo = nestedClassTypeInfo.GetField(fieldName);
            var valueLocString = fieldInfo?.GetValue(nestedClassTypeInfo) as LocString;
            result = valueLocString?.text;

            return result;
        }

        public static void CreateFileWithAllAvailableBuildings()
        {
            var result = new List<string>();

            try
            {
                var allAvailableBuildingDefConfigs = DynamicBuildingsManager.GetBuildingConfigManager_ConfigTable().ToDictionary(x => x.Value.PrefabID, x => x.Key);

                var localizationStrings = FindAllSubclassesOf_STRINGS_BUILDINGS_PREFABS();

                foreach (var buildingDefConfig in allAvailableBuildingDefConfigs)
                {
                    var prefabIdUpper = buildingDefConfig.Key.ToUpper();
                    var buildingDefType = buildingDefConfig.Value.GetType();

                    string nameValue = GetTextFromLocalization(localizationStrings, prefabIdUpper, "NAME");
                    if (string.IsNullOrEmpty(nameValue))
                    {
                        nameValue = buildingDefType.GetField("DisplayName")?.GetValue(null) as string;
                    }

                    string descValue = GetTextFromLocalization(localizationStrings, prefabIdUpper, "DESC");
                    if (string.IsNullOrEmpty(descValue))
                    {
                        descValue = buildingDefType.GetField("Description")?.GetValue(null) as string;
                    }
                    if (!string.IsNullOrEmpty(descValue))
                    {
                        var descValueCleansed = Regex.Replace(descValue, "[\n|\t]", " ");
                        descValue = descValueCleansed;
                    }

                    result.Add(string.Concat(buildingDefType.FullName, "\t", nameValue, "\t", descValue));
                }

                result.Sort();
                if (result.Any())
                {
                    result.Insert(0, string.Concat("TechnicalName", "\t", "In-game Caption", "\t", "Description"));
                }
            }
            catch (Exception e)
            {
                Debug.Log("ExtendedBuildingWidth ERROR - Exception while scanning for names");
                Debug.Log(e.Message);
            }

            try
            {
                string fullFileName = Get_AllAvailableBuildings_FullFileName();

                if (File.Exists(fullFileName))
                {
                    File.Delete(fullFileName);
                }

                using (StreamWriter fs = File.CreateText(fullFileName))
                {
                    foreach (var line in result)
                    {
                        fs.WriteLine(line);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log("ExtendedBuildingWidth ERROR - Exception while writing to file");
                Debug.Log(e.Message);
            }
        }

        public static List<ExtendableConfigSettings> GetExtendableConfigSettingsList()
        {
            var result = JsonConvert.DeserializeObject<List<ExtendableConfigSettings>>(Instance.ExtendableConfigSettings);
            return result;
        }

        private static List<ExtendableConfigSettings> GenerateDefaultValues_For_ExtendableConfigSettings()
        {
            List<Type> list = new List<Type>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                try
                {
                    Type[] types = assemblies[i].GetTypes();
                    if (types != null)
                    {
                        list.AddRange(types);
                    }
                }
                catch (Exception)
                {
                    Debug.Log("ExtendedBuildingWidth ERROR - unable to get types from assembly " + assemblies[i].FullName);
                }
            }

            var typeFromHandle = typeof(IBuildingConfig);
            var extendableTypes = new List<Type>(
                list.Where(type =>
                    typeFromHandle.IsAssignableFrom(type)
                    && !type.IsAbstract
                    && !type.IsInterface
                    && type.Name.Contains("ConduitBridge")
                    ).ToList()
                );

            var result = new List<ExtendableConfigSettings>(
                extendableTypes.Select(x =>
                    new ExtendableConfigSettings()
                    {
                        ConfigName = x.FullName,
                        MinWidth = 2,
                        MaxWidth = 16,
                        AnimStretchModifier = 1.12f
                    }
                    )
                );
            return result;
        }

    }
}