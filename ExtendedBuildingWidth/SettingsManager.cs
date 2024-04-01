using PeterHan.PLib.Core;
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
        public const string AllAvailableBuildings_FileName = "AllAvailableBuildings.txt";
        public const string SourceFileForConfigJson_FileName = "SourceFileForConfigJson.txt";

        public static string Get_ConfigJson_Path() => Path.GetDirectoryName(POptions.GetConfigFilePath(typeof(ModSettings)));
        public static string Get_ConfigJson_FullFileName() => POptions.GetConfigFilePath(typeof(ModSettings));
        public static string Get_SourceFileForConfigJson_FullFileName() => Get_ConfigJson_Path() + "\\" + SourceFileForConfigJson_FileName;
        public static string Get_AllAvailableBuildings_FullFileName() => Get_ConfigJson_Path() + "\\" + AllAvailableBuildings_FileName;
        public static string Get_DebugLog_FullFileName() => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\AppData\LocalLow\Klei\Oxygen Not Included\Player.log";
        public static string Get_DebugLog_Path() => Path.GetDirectoryName(Get_DebugLog_FullFileName());

        public static void CreateSourceTextFile_From_ConfigJson(object obj)
        {
            try
            {
                string configJson_FullFileName = Get_ConfigJson_FullFileName();
                if (!File.Exists(configJson_FullFileName))
                {
                    POptions.WriteSettings(ModSettings.Instance);
                }

                ModSettings local = POptions.ReadSettings<ModSettings>();
                var rez = local.GetExtendableConfigSettingsList();
                ModSettings.Instance.SetExtendableConfigSettings(local.GetExtendableConfigSettingsList());

                string sourceText_FullFileName = Get_SourceFileForConfigJson_FullFileName();
                if (File.Exists(sourceText_FullFileName))
                {
                    File.Delete(sourceText_FullFileName);
                }

                using (StreamWriter fs = File.CreateText(sourceText_FullFileName))
                {
                    foreach (var line in rez)
                    {
                        fs.WriteLine(line.ConfigName + "\t" + line.MinWidth + "\t" + line.MaxWidth + "\t" + line.AnimStretchModifier);
                    }
                }

                System.Diagnostics.Process.Start(Get_ConfigJson_Path());
                System.Diagnostics.Process.Start(Get_SourceFileForConfigJson_FullFileName());

                PUIElements.ShowMessageDialog(null, "Source text file created successfully");
            }
            catch (Exception e)
            {
                Debug.Log("ExtendedBuildingWidth ERROR - Error occured while converting from config.json");
                Debug.Log(e.Message);

                try
                { 
                    PUIElements.ShowMessageDialog(null, "Error occured while converting from config.json. Check logs.");
                    System.Diagnostics.Process.Start(Get_DebugLog_Path());
                }
                catch (Exception e2)
                {
                    Debug.Log("ExtendedBuildingWidth ERROR - Error occured while handling an error. Welp...");
                    Debug.Log(e2.Message);
                }
            }
        }

        public static void CreateConfigJson_From_SourceTextFile(object obj)
        {
            try
            {
                Debug.Log("ExtendedBuildingWidth - trying to create ConfigJson from SourceFile");

                var lines = Read_From_SourceTextFile();

                var rezult = new List<ExtendableConfigSettings>();
                foreach (var line in lines)
                {
                    var values = line.Split('\t');

                    rezult.Add(new ExtendableConfigSettings()
                    {
                        ConfigName = values[0],
                        MinWidth = int.Parse(values[1]),
                        MaxWidth = int.Parse(values[2]),
                        AnimStretchModifier = float.Parse(values[3])
                    });
                }

                ModSettings.Instance.SetExtendableConfigSettings(rezult);

                POptions.WriteSettings(ModSettings.Instance);

                PUIElements.ShowConfirmDialog(null, "Config.json generated successfully. Restart to apply changes", RestartGame, null, PLibStrings.RESTART_OK, PLibStrings.RESTART_CANCEL);
            }
            catch (Exception e)
            {
                Debug.Log("ExtendedBuildingWidth ERROR - Error occured while generating new config.json");
                Debug.Log(e.Message);

                try
                {
                    PUIElements.ShowMessageDialog(null, "Error occured while generating new config.json. Check logs.");
                    System.Diagnostics.Process.Start(Get_DebugLog_Path());
                }
                catch (Exception e2)
                {
                    Debug.Log("ExtendedBuildingWidth ERROR - Error occured while handling an error. Welp...");
                    Debug.Log(e2.Message);
                }
            }
        }

        private static void RestartGame()
        {
            App.instance.Restart();
        }

        public static void CreateFileWithAllAvailableBuildings(object obj)
        {
            var allBuildingsList = GetListOfAllBuildings();

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
                        fs.WriteLine(line);
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

        private static List<string> Read_From_SourceTextFile()
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

        private static List<string> GetListOfAllBuildings()
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