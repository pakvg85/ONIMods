using PeterHan.PLib.Options;
using PeterHan.PLib.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ExtendedBuildingWidth
{
    public class SettingsManager
    {
        public class BuildingDescription
        {
            public string TechName { get; set; }
            public string Caption { get; set; }
            public string Desc { get; set; }
        }

        public const string AllAvailableBuildings_FileName = "AllAvailableBuildings.txt";
        public const string SourceFileForConfigJson_FileName = "SourceFileForConfigJson.txt";
        public const int DefaultMinWidth = 2;
        public const int DefaultMaxWidth = 16;
        public const float DefaultAnimStretchModifier = 1.12f;

        public static List<BuildingDescription> ListOfAllBuildings
        {
            get
            {
                if (_listOfAllBuildings == null)
                {
                    _listOfAllBuildings = GetListOfAllBuildings();
                }
                return _listOfAllBuildings;
            }
        }
        private static List<BuildingDescription> _listOfAllBuildings;

        public static string Get_ConfigJson_Path() => Path.GetDirectoryName(POptions.GetConfigFilePath(typeof(ModSettings)));
        public static string Get_ConfigJson_FullFileName() => POptions.GetConfigFilePath(typeof(ModSettings));
        public static string Get_SourceFileForConfigJson_FullFileName() => Get_ConfigJson_Path() + "\\" + SourceFileForConfigJson_FileName;
        public static string Get_AllAvailableBuildings_FullFileName() => Get_ConfigJson_Path() + "\\" + AllAvailableBuildings_FileName;
        public static string Get_DebugLog_FullFileName() => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\AppData\LocalLow\Klei\Oxygen Not Included\Player.log";
        public static string Get_DebugLog_Path() => Path.GetDirectoryName(Get_DebugLog_FullFileName());

        private readonly ModSettings _modSettings;

        public SettingsManager(ModSettings modSettings_Instance)
        {
            _modSettings = modSettings_Instance;
        }

        public static void CreateFileWithAllAvailableBuildings(object obj)
        {
            var allBuildingsList = ListOfAllBuildings;

            try
            {
                string fullFileName = Get_AllAvailableBuildings_FullFileName();

                if (File.Exists(fullFileName))
                {
                    File.Delete(fullFileName);
                }

                using (StreamWriter fs = File.CreateText(fullFileName))
                {
                    foreach (var line in allBuildingsList)
                    {
                        fs.WriteLine(line.TechName + "\t" + line.Caption + "\t" + line.Desc);
                    }
                }

                System.Diagnostics.Process.Start(Get_ConfigJson_Path());
                System.Diagnostics.Process.Start(Get_AllAvailableBuildings_FullFileName());
            }
            catch (Exception e)
            {
                Debug.Log("ExtendedBuildingWidth ERROR - Exception while writing to AllAvailableBuildings file");
                Debug.Log(e.Message);

                try
                {
                    PUIElements.ShowMessageDialog(null, "Error occured while writing to AllAvailableBuildings file. Check logs.");
                    System.Diagnostics.Process.Start(Get_DebugLog_Path());
                }
                catch (Exception e2)
                {
                    Debug.Log("ExtendedBuildingWidth ERROR - Error occured while handling an error. Welp...");
                    Debug.Log(e2.Message);
                }
            }
        }

        public static List<string> Read_From_SourceTextFile()
        {
            var result = new List<string>();

            string fullFileName = Get_SourceFileForConfigJson_FullFileName();
            using (StreamReader fs = File.OpenText(fullFileName))
            {
                string line = null;
                var charsToBeTrimmed = new char[] { ' ', '\t', '\r', '\n' };

                int counterToPreventInfiniteLoop = 0;
                while (!fs.EndOfStream && counterToPreventInfiniteLoop++ < 777)
                {
                    line = fs.ReadLine().Trim(charsToBeTrimmed);
                    if (!string.IsNullOrEmpty(line))
                    {
                        result.Add(line);
                    }
                }
                if (!fs.EndOfStream && counterToPreventInfiniteLoop >= 777)
                {
                    Debug.Log("ExtendedBuildingWidth ERROR - there was a problem reading from SourceFileForConfigJson");
                }

                if (!result.Any())
                {
                    Debug.Log("ExtendedBuildingWidth - no records was read from SourceFileForConfigJson");
                }

                Debug.Log("ExtendedBuildingWidth - " + result.Count + " lines was read");
            }

            return result;
        }

        public static Dictionary<string, Type> FindAllSubclassesOf_STRINGS_BUILDINGS_PREFABS()
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
        public static string GetTextFromLocalization(Dictionary<string, Type> localizationStrings, string prefabIdUpper, string fieldName)
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

        private static string RemoveBetween(string sourceString, string startTag, string endTag)
        {
            Regex regex = new Regex(string.Format("{0}(.*?){1}", Regex.Escape(startTag), Regex.Escape(endTag)), RegexOptions.RightToLeft);
            return regex.Replace(sourceString, "");
        }

        private static List<BuildingDescription> GetListOfAllBuildings()
        {
            var result = new List<BuildingDescription>();

            try
            {
                var buildingDefPrefabIdToConfigMap = DynamicBuildingsManager.ConfigToBuildingDefMap.ToDictionary(x => x.Value.PrefabID, x => x.Key);

                var localizationStrings = FindAllSubclassesOf_STRINGS_BUILDINGS_PREFABS();

                foreach (var buildingDefConfig in buildingDefPrefabIdToConfigMap)
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

                    if (!string.IsNullOrEmpty(nameValue) && nameValue.Contains("<link="))
                    {
                        nameValue = RemoveBetween(nameValue, "</link", ">");
                        nameValue = RemoveBetween(nameValue, "<link=", ">");
                    }
                    result.Add(new BuildingDescription() { TechName = buildingDefType.FullName, Caption = nameValue, Desc = descValue });
                }
            }
            catch (Exception e)
            {
                Debug.Log("ExtendedBuildingWidth ERROR - Exception while scanning for names");
                Debug.Log(e.Message);
            }

            return result;
        }

        public static List<ExtendableConfigSettings> GenerateDefaultValues_For_ExtendableConfigSettings()
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
                        MinWidth = DefaultMinWidth,
                        MaxWidth = DefaultMaxWidth,
                        AnimStretchModifier = DefaultAnimStretchModifier
                    }
                    )
                );
            return result;
        }

        public static List<AnimSplittingSettings> GenerateDefaultValues_For_AnimSplittingSettings()
        {
            var result = new List<AnimSplittingSettings>();

            var item = new AnimSplittingSettings()
            {
                ConfigName = "GasConduitBridgeConfig",
                IsActive = true,
                MiddlePart_X = 135,
                MiddlePart_Width = 1,
                FillingMethod = FillingMethod.Stretch,
                DoFlipEverySecondIteration = false
            };
            result.Add(item);

            item = new AnimSplittingSettings()
            {
                ConfigName = "LiquidConduitBridgeConfig",
                IsActive = true,
                MiddlePart_X = 145,
                MiddlePart_Width = 1,
                FillingMethod = FillingMethod.Stretch,
                DoFlipEverySecondIteration = false
            };
            result.Add(item);

            item = new AnimSplittingSettings()
            {
                ConfigName = "SolidConduitBridgeConfig",
                IsActive = true,
                MiddlePart_X = 110,
                MiddlePart_Width = 1,
                FillingMethod = FillingMethod.Stretch,
                DoFlipEverySecondIteration = false
            };
            result.Add(item);

            item = new AnimSplittingSettings()
            {
                ConfigName = "High_Pressure_Applications.BuildingConfigs.HighPressureGasConduitBridgeConfig",
                IsActive = true,
                MiddlePart_X = 158,
                MiddlePart_Width = 1,
                FillingMethod = FillingMethod.Stretch,
                DoFlipEverySecondIteration = false
            };
            result.Add(item);

            item = new AnimSplittingSettings()
            {
                ConfigName = "High_Pressure_Applications.BuildingConfigs.HighPressureLiquidConduitBridgeConfig",
                IsActive = true,
                MiddlePart_X = 147,
                MiddlePart_Width = 1,
                FillingMethod = FillingMethod.Stretch,
                DoFlipEverySecondIteration = false
            };
            result.Add(item);

            item = new AnimSplittingSettings()
            {
                ConfigName = "LogicWireBridgeConfig",
                IsActive = true,
                MiddlePart_X = 130,
                MiddlePart_Width = 1,
                FillingMethod = FillingMethod.Stretch,
                DoFlipEverySecondIteration = false
            };
            result.Add(item);

            item = new AnimSplittingSettings()
            {
                ConfigName = "WireBridgeConfig",
                IsActive = true,
                MiddlePart_X = 97,
                MiddlePart_Width = 85,
                FillingMethod = FillingMethod.Repeat,
                DoFlipEverySecondIteration = true
            };
            result.Add(item);

            item = new AnimSplittingSettings()
            {
                ConfigName = "WireRefinedBridgeConfig",
                IsActive = true,
                MiddlePart_X = 115,
                MiddlePart_Width = 50,
                FillingMethod = FillingMethod.Repeat,
                DoFlipEverySecondIteration = true
            };
            result.Add(item);

            return result;
        }

        public static Dictionary<string, string> GenerateDefaultValues_For_ConfigNameToAnimNamesMap()
        {
            var result = new Dictionary<string, string>();
            result.Add("GasConduitBridgeConfig", "utilitygasbridge_kanim");
            result.Add("LiquidConduitBridgeConfig", "utilityliquidbridge_kanim");
            result.Add("SolidConduitBridgeConfig", "utilities_conveyorbridge_kanim");
            result.Add("High_Pressure_Applications.BuildingConfigs.HighPressureGasConduitBridgeConfig", "pressure_gas_bridge_kanim");
            result.Add("High_Pressure_Applications.BuildingConfigs.HighPressureLiquidConduitBridgeConfig", "pressure_liquid_bridge_kanim");
            result.Add("LogicWireBridgeConfig", "logic_bridge_kanim");
            result.Add("WireBridgeConfig", "utilityelectricbridge_kanim");
            result.Add("WireRefinedBridgeConfig", "utilityelectricbridgeconductive_kanim");
            return result;
        }
    }
}