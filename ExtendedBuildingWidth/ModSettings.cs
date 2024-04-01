using Newtonsoft.Json;
using PeterHan.PLib.Options;
using System;
using System.Collections.Generic;

namespace ExtendedBuildingWidth
{
    [JsonObject(MemberSerialization.OptIn)]
    [RestartRequired]
    [ConfigFileAttribute(SharedConfigLocation:true)]
    public class ModSettings : SingletonOptions<ModSettings>
    {
        [JsonProperty]
        public string ExtendableConfigSettings { get; set; }

        [Option("These 2 buttons can help maintaining config.json")]
        public LocText Caption_CreateSourceTextFile { get; set; }

        [Option("config.json -> plain text", "Convert config.json data into " + SettingsManager.SourceFileForConfigJson_FileName)]
        public System.Action<object> Button_CreateSourceTextFile => SettingsManager.CreateSourceTextFile_From_ConfigJson;

        [Option("plain text -> config.json", "Generate new config.json from " + SettingsManager.SourceFileForConfigJson_FileName)]
        public System.Action<object> Button_CreateConfigJsonFromSourceTextFile => SettingsManager.CreateConfigJson_From_SourceTextFile;

        [Option("Pressing this button will create a text file with all available buildings in the game")]
        public LocText Caption_CreateFileWithAllAvailableBuildings { get; set; }

        [Option("Create list of all in-game buildings", "Create a text file with all available buildings in the game")]
        public System.Action<object> Button_CreateFileWithAllAvailableBuildings => SettingsManager.CreateFileWithAllAvailableBuildings;

        public void SetExtendableConfigSettings(List<ExtendableConfigSettings> src)
        {
            ExtendableConfigSettings = JsonConvert.SerializeObject(src);
        }

        public List<ExtendableConfigSettings> GetExtendableConfigSettingsList()
        {
            try
            {
                var result = JsonConvert.DeserializeObject<List<ExtendableConfigSettings>>(ExtendableConfigSettings);
                return result;
            }
            catch (Exception e)
            {
                Debug.Log("ExtendedBuildingWidth ERROR - Exception while deserializing Json");
                Debug.Log(e.Message);
                return null;
            }
        }

        /// <summary>
        /// The constructor is called inside getter method 'Instance.get' of 'SingletonOptions' in both cases:
        /// 1) if 'config.json' is read via 'POptions.ReadSettings' (in this case the constructor results are overwritten)
        /// 2) if 'config.json' is not read, and new 'Instance' is created via 'Activator.CreateInstance'
        /// </summary>
        public ModSettings()
        {
            SetExtendableConfigSettings(SettingsManager.GenerateDefaultValues_For_ExtendableConfigSettings());
        }
    }
}