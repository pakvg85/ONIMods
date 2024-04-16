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

        private HiddenStringOptionsEntry _hiddenOptionsEntry = new HiddenStringOptionsEntry(
            nameof(ExtendableConfigSettings),
            new OptionAttribute("DUMMY_TITLE")
        );

        IEnumerable<IOptionsEntry> IOptions.CreateOptions() {
            yield return _hiddenOptionsEntry;
        }

        void IOptions.OnOptionsChanged() { }

        [JsonProperty]
        public string ExtendableConfigSettings {
            get { return _extendableConfigSettings; }
            set { _extendableConfigSettings = value;
                  _hiddenOptionsEntry.Value = value;
            }
        }
        private string _extendableConfigSettings;

        [Option("Modify config settings in GUI")]
        public System.Action<object> Button_EditConfigJson => _dialog_EditConfigJson.CreateAndShow;

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

        private readonly SettingsManager _settingsManager;
        private readonly Dialog_EditConfigJson _dialog_EditConfigJson;

        /// <summary>
        /// The constructor is called in 2 cases:
        /// 1) if 'config.json' is read via 'POptions.ReadSettings' (in this case the constructor results are overwritten)
        /// 2) if 'config.json' is not read, and new 'Instance' is created via 'Activator.CreateInstance'
        /// </summary>
        public ModSettings()
        {
            _settingsManager = new SettingsManager(this);
            _dialog_EditConfigJson = new Dialog_EditConfigJson(this);
            SetExtendableConfigSettings(SettingsManager.GenerateDefaultValues_For_ExtendableConfigSettings());
        }
    }
}