using Newtonsoft.Json;
using PeterHan.PLib.Options;
using PeterHan.PLib.UI;
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
            get => _extendableConfigSettings;
            set
            {
                _extendableConfigSettings = value;
                _extendableConfigSettings_Hsoe.Value = value;
            }
        }
        private string _extendableConfigSettings;

        [JsonProperty]
        public string AnimSplittingSettings
        {
            get => _animSplittingSettings;
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
            get => _configNameToAnimNameMap;
            set
            {
                _configNameToAnimNameMap = value;
                _configNameToAnimNameMap_Hsoe.Value = value;
            }
        }
        private string _configNameToAnimNameMap;

        [Option("STRINGS.UI.MODSETTINGS.BUTTON_STARTDIALOG_CONFIGMAIN")]
        public System.Action<object> Button_EditConfigJson => new Dialog_EditConfigJson(this).CreateAndShow;

        [Option("STRINGS.UI.MODSETTINGS.BUTTON_STARTDIALOG_ANIMSLICING")]
        public System.Action<object> Button_EditAnimSlicingSettings => new Dialog_EditAnimSlicingSettings(this).CreateAndShow;

        [Option("STRINGS.UI.MODSETTINGS.BUTTON_CREATEALLBUILDINGNAMES", "STRINGS.UI.MODSETTINGS.BUTTON_CREATEALLBUILDINGNAMES_TOOLTIP")]
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

        public void SetConfigNameToAnimNameMap(Dictionary<string, string> src)
        {
            ConfigNameToAnimNameMap = JsonConvert.SerializeObject(src);
        }

        /// <summary>
        /// This dictionary contains records that are only relevant to 'AnimSplittingSettings'.
        /// Do not confuse with 'DynamicBuildingsManager.ConfigNameToAnimNameMap'.
        /// </summary>
        public Dictionary<string, string> GetConfigNameToAnimNameMap() => JsonConvert.DeserializeObject<Dictionary<string, string>>(ConfigNameToAnimNameMap);

        /// <summary>
        /// The constructor is called in 2 cases:
        /// 1) if 'config.json' is read via 'POptions.ReadSettings' (in this case the constructor results are overwritten)
        /// 2) if 'config.json' is not read, and new 'Instance' is created via 'Activator.CreateInstance'
        /// </summary>
        public ModSettings()
        {
            SetExtendableConfigSettings(SettingsManager.GenerateDefaultValues_For_ExtendableConfigSettings());
            SetAnimSplittingSettings(SettingsManager.GenerateDefaultValues_For_AnimSplittingSettings());
            SetConfigNameToAnimNameMap(SettingsManager.GenerateDefaultValues_For_ConfigNameToAnimNamesMap());
        }
    }
}