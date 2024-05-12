using Newtonsoft.Json;
using PeterHan.PLib.Options;
using PeterHan.PLib.UI;
using System;
using System.Collections.Generic;

namespace ExtendedBuildingWidth
{
    [JsonObject(MemberSerialization.OptIn)]
    [RestartRequired]
    [ConfigFile(SharedConfigLocation: true)]
    public class ModSettings : IOptions
    {
        private class HiddenStringOptionsEntry : StringOptionsEntry
        {
            public HiddenStringOptionsEntry(string field, IOptionSpec spec, LimitAttribute limit = null): base(field, spec, limit) { }
            public override void CreateUIEntry(PGridPanel parent, ref int row) { }
        }

        private HiddenStringOptionsEntry _extendableConfigSettings_Hsoe = new HiddenStringOptionsEntry(
            nameof(ExtendableConfigSettings),
            new OptionAttribute("DUMMY_TITLE")
        );

        private HiddenStringOptionsEntry _animSplittingSettings_Hsoe = new HiddenStringOptionsEntry(
            nameof(AnimSplittingSettings),
            new OptionAttribute("DUMMY_TITLE")
        );

        private HiddenStringOptionsEntry _configNameToAnimNameMap_Hsoe = new HiddenStringOptionsEntry(
            nameof(ConfigNameToAnimNameMap),
            new OptionAttribute("DUMMY_TITLE")
        );

        IEnumerable<IOptionsEntry> IOptions.CreateOptions() {
            yield return _extendableConfigSettings_Hsoe;
            yield return _animSplittingSettings_Hsoe;
            yield return _configNameToAnimNameMap_Hsoe;
        }

        void IOptions.OnOptionsChanged() { }

        [JsonProperty]
        public string ExtendableConfigSettings
        {
            get { return _extendableConfigSettings; }
            set { _extendableConfigSettings = value;
                  _extendableConfigSettings_Hsoe.Value = value;
            }
        }
        private string _extendableConfigSettings;

        [JsonProperty]
        public string AnimSplittingSettings
        {
            get { return _animSplittingSettings; }
            set
            {
                _animSplittingSettings = value;
                _animSplittingSettings_Hsoe.Value = value;
            }
        }
        private string _animSplittingSettings;

        [JsonProperty]
        public string ConfigNameToAnimNameMap
        {
            get { return _configNameToAnimNameMap; }
            set
            {
                _configNameToAnimNameMap = value;
                _configNameToAnimNameMap_Hsoe.Value = value;
            }
        }
        private string _configNameToAnimNameMap;

        [Option("Config Settings GUI")]
        public System.Action<object> Button_EditConfigJson => _dialog_EditConfigJson.CreateAndShow;

        [Option("Anim Slicing Settings GUI")]
        public System.Action<object> Button_EditAnimSlicingSettings => _dialog_EditAnimSlicingSettings.CreateAndShow;

        [Option("Create list of all in-game buildings", "Create a text file with all available buildings in the game")]
        public System.Action<object> Button_CreateFileWithAllAvailableBuildings => SettingsManager.CreateFileWithAllAvailableBuildings;

        public void SetExtendableConfigSettings(List<ExtendableConfigSettings> src)
        {
            ExtendableConfigSettings = JsonConvert.SerializeObject(src);
        }

        public List<ExtendableConfigSettings> GetExtendableConfigSettingsList() => JsonConvert.DeserializeObject<List<ExtendableConfigSettings>>(ExtendableConfigSettings);

        public void SetAnimSplittingSettings(List<AnimSplittingSettings> src)
        {
            AnimSplittingSettings = JsonConvert.SerializeObject(src);
        }

        public List<AnimSplittingSettings> GetAnimSplittingSettingsList() => JsonConvert.DeserializeObject<List<AnimSplittingSettings>>(AnimSplittingSettings);

        public void SetConfigNameToAnimNamesMap(Dictionary<string, string> src)
        {
            ConfigNameToAnimNameMap = JsonConvert.SerializeObject(src);
        }

        public Dictionary<string, string> GetConfigNameToAnimNamesMap() => JsonConvert.DeserializeObject<Dictionary<string, string>>(ConfigNameToAnimNameMap);

        private readonly SettingsManager _settingsManager;
        private readonly Dialog_EditConfigJson _dialog_EditConfigJson;
        private readonly Dialog_EditAnimSlicingSettings _dialog_EditAnimSlicingSettings;

        /// <summary>
        /// The constructor is called in 2 cases:
        /// 1) if 'config.json' is read via 'POptions.ReadSettings' (in this case the constructor results are overwritten)
        /// 2) if 'config.json' is not read, and new 'Instance' is created via 'Activator.CreateInstance'
        /// </summary>
        public ModSettings()
        {
            _settingsManager = new SettingsManager(this);
            _dialog_EditConfigJson = new Dialog_EditConfigJson(this);
            _dialog_EditAnimSlicingSettings = new Dialog_EditAnimSlicingSettings(this);
            SetExtendableConfigSettings(SettingsManager.GenerateDefaultValues_For_ExtendableConfigSettings());
            SetAnimSplittingSettings(SettingsManager.GenerateDefaultValues_For_AnimSplittingSettings());
            SetConfigNameToAnimNamesMap(SettingsManager.GenerateDefaultValues_For_ConfigNameToAnimNamesMap());
        }
    }
}