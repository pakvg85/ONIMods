using Newtonsoft.Json;
using PeterHan.PLib.Options;
using PeterHan.PLib.UI;
using System.Collections.Generic;

namespace ExtendedBuildingWidth
{
    // ModSettings should not be a singleton - 'POptions.ReadSettings()' creates an instance every time
    // when button 'MOD SETTINGS' is pressed, so 'SingletonOptions<>' part is removed to avoid ambiguity
    // between multiple instances.
    [JsonObject(MemberSerialization.OptIn)]
    [RestartRequired]
    [ConfigFile(SharedConfigLocation: true)]
    public class ModSettings : IOptions
    {
        public class HiddenOptionsEntry<T> : OptionsEntry
        {
            private ModSettings _modSettings;
            public override object Value
            {
                get
                {
                    return _value;
                }
                set
                {
                    this._value = (T)value;
                }
            }
            private T _value;

            public HiddenOptionsEntry(string field, IOptionSpec attr, ModSettings modSettings) : base(field, attr)
            {
                _modSettings = modSettings;
            }

            public override UnityEngine.GameObject GetUIComponent()
            {
                // A dummy not-null UI component have to be returned so the engine won't crash
                var dummyTextField = new PTextField
                {
                    Type = PTextField.FieldType.Text
                }.Build();

                return dummyTextField;
            }

            public override void CreateUIEntry(PGridPanel parent, ref int row)
            {
                // Should stay empty - we don't have to create any real UI element
            }

            public override void WriteTo(object settings)
            {
                _modSettings.ConvertOldValuesIfNeeded();
                base.WriteTo(settings);
            }
        }

        // POptions take values not from the Json properties from ModSettings - but from the OptionsEntries.
        // OptionsEntries are created in 'IOptions.CreateOptions()' and are synced with Json properties via their setters.

        private HiddenOptionsEntry<string> _extendableConfigSettings_Hoe;
        private HiddenOptionsEntry<string> _animSplittingSettings_Hoe;
        private HiddenOptionsEntry<string> _configNameToAnimNameMap_Hoe;
        private HiddenOptionsEntry<List<ExtendableConfigSettings>> _extendConfigSettings_Hoe;
        private HiddenOptionsEntry<List<AnimSplittingSettings>> _animSplitSettings_Hoe;
        private HiddenOptionsEntry<Dictionary<string, string>> _configToAnimMap_Hoe;

        IEnumerable<IOptionsEntry> IOptions.CreateOptions()
        {
            yield return _extendableConfigSettings_Hoe;
            yield return _animSplittingSettings_Hoe;
            yield return _configNameToAnimNameMap_Hoe;
            yield return _extendConfigSettings_Hoe;
            yield return _animSplitSettings_Hoe;
            yield return _configToAnimMap_Hoe;
        }

        void IOptions.OnOptionsChanged() { }

        // Json string properties 'ExtendableConfigSettings', 'AnimSplittingSettings' and 'ConfigNameToAnimNameMap'
        // are not used from now on, but can still be present in other players' config files.
        // So they have to stay.
        // These properties are converted in 'ConvertOldValuesIfNeeded' method.

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ExtendableConfigSettings
        {
            get => _extendableConfigSettings;
            set
            {
                _extendableConfigSettings = value;
                _extendableConfigSettings_Hoe.Value = value;
            }
        }
        private string _extendableConfigSettings;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string AnimSplittingSettings
        {
            get => _animSplittingSettings;
            set
            {
                _animSplittingSettings = value;
                _animSplittingSettings_Hoe.Value = value;
            }
        }
        private string _animSplittingSettings;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ConfigNameToAnimNameMap
        {
            get => _configNameToAnimNameMap;
            set
            {
                _configNameToAnimNameMap = value;
                _configNameToAnimNameMap_Hoe.Value = value;
            }
        }
        private string _configNameToAnimNameMap;

        // Json parameter 'Replace' allows to correctly deserialize collections like 'List' and 'Dictionary'.
        // If removed - these properties will start containing not only the deserialized values from config.json,
        // but also the default values from constructor (this behavior is incorrect)

        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<ExtendableConfigSettings> ExtendConfigSettings
        {
            get => _extendConfigSettings;
            set
            {
                _extendConfigSettings = value;
                _extendConfigSettings_Hoe.Value = value;
            }
        }
        private List<ExtendableConfigSettings> _extendConfigSettings;

        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<AnimSplittingSettings> AnimSplitSettings
        {
            get => _animSplitSettings;
            set
            {
                _animSplitSettings = value;
                _animSplitSettings_Hoe.Value = value;
            }
        }
        private List<AnimSplittingSettings> _animSplitSettings;

        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public Dictionary<string, string> ConfigToAnimMap
        {
            get => _configToAnimMap;
            set
            {
                _configToAnimMap = value;
                _configToAnimMap_Hoe.Value = value;
            }
        }
        private Dictionary<string, string> _configToAnimMap;

        [Option("STRINGS.UI.MODSETTINGS.BUTTON_STARTDIALOG_CONFIGMAIN")]
        public System.Action<object> Button_EditConfigJson => new Dialog_EditConfigJson(this).CreateAndShow;

        [Option("STRINGS.UI.MODSETTINGS.BUTTON_STARTDIALOG_ANIMSLICING")]
        public System.Action<object> Button_EditAnimSlicingSettings => new Dialog_EditAnimSlicingSettings(this).CreateAndShow;

        [Option("STRINGS.UI.MODSETTINGS.BUTTON_CREATEALLBUILDINGNAMES", "STRINGS.UI.MODSETTINGS.BUTTON_CREATEALLBUILDINGNAMES_TOOLTIP")]
        public System.Action<object> Button_CreateFileWithAllAvailableBuildings => SettingsManager.CreateFileWithAllAvailableBuildings;

        public void SetExtendableConfigSettings(List<ExtendableConfigSettings> src)
        {
            ExtendConfigSettings = src;
        }
        public void SetAnimSplittingSettings(List<AnimSplittingSettings> src)
        {
            AnimSplitSettings = src;
        }
        public void SetConfigNameToAnimNameMap(Dictionary<string, string> src)
        {
            ConfigToAnimMap = src;
        }

        public List<ExtendableConfigSettings> GetExtendableConfigSettingsList()
        {
            ConvertOldValuesIfNeeded();
            return ExtendConfigSettings;
        }

        public List<AnimSplittingSettings> GetAnimSplittingSettingsList()
        {
            ConvertOldValuesIfNeeded();
            return AnimSplitSettings;
        }

        /// <summary>
        /// This dictionary contains mappings for config names to match their anim names (ex: 'GasConduitBridgeConfig' - 'utilitygasbridge_kanim').
        /// Important note:
        /// This dict contains only config names that exist in 'Visual Output Seetings' list.
        /// 'DynamicBuildingsManager.ConfigNameToAnimNameMap' has similar purpose, but is generated AFTER the game recources are loaded.
        /// We don't have access to these game recources in 'DynamicAnimManager.AddDynamicAnimsNames_To_ModLoadedKAnims',
        /// so we have to maintain mappings in 'config.json'.
        /// </summary>
        public Dictionary<string, string> GetConfigNameToAnimNameMap()
        {
            ConvertOldValuesIfNeeded();
            return ConfigToAnimMap;
        }

        /// <summary>
        /// The constructor is called in 2 cases:
        /// 1) if 'config.json' is read via 'POptions.ReadSettings' (in this case the constructor results are overwritten)
        /// 2) if 'config.json' is not read, and new 'Instance' is created via 'Activator.CreateInstance'
        /// </summary>
        public ModSettings()
        {
            _extendableConfigSettings_Hoe = new HiddenOptionsEntry<string>(
                nameof(ExtendableConfigSettings),
                new OptionAttribute("DUMMY_TITLE"),
                this
            );
            _animSplittingSettings_Hoe = new HiddenOptionsEntry<string>(
                nameof(AnimSplittingSettings),
                new OptionAttribute("DUMMY_TITLE"),
                this
            );
            _configNameToAnimNameMap_Hoe = new HiddenOptionsEntry<string>(
                nameof(ConfigNameToAnimNameMap),
                new OptionAttribute("DUMMY_TITLE"),
                this
            );
            _extendConfigSettings_Hoe = new HiddenOptionsEntry<List<ExtendableConfigSettings>>(
                nameof(ExtendConfigSettings),
                new OptionAttribute("DUMMY_TITLE"),
                this
            );
            _animSplitSettings_Hoe = new HiddenOptionsEntry<List<AnimSplittingSettings>>(
                nameof(AnimSplitSettings),
                new OptionAttribute("DUMMY_TITLE"),
                this
            );
            _configToAnimMap_Hoe = new HiddenOptionsEntry<Dictionary<string, string>>(
                nameof(ConfigToAnimMap),
                new OptionAttribute("DUMMY_TITLE"),
                this
            );

            SetExtendableConfigSettings(SettingsManager.GenerateDefaultValues_For_ExtendableConfigSettings());
            SetAnimSplittingSettings(SettingsManager.GenerateDefaultValues_For_AnimSplittingSettings());
            SetConfigNameToAnimNameMap(SettingsManager.GenerateDefaultValues_For_ConfigNameToAnimNamesMap());
        }

        public void ConvertOldValuesIfNeeded()
        {
            if (!string.IsNullOrEmpty(ExtendableConfigSettings)
                //&& (ExtendableConfigSettingsList is null || ExtendableConfigSettingsList.Length == 0)
                )
            {
                var list = JsonConvert.DeserializeObject<List<ExtendableConfigSettings>>(ExtendableConfigSettings);
                ExtendableConfigSettings = null;
                SetExtendableConfigSettings(list);
            }
            if (!string.IsNullOrEmpty(AnimSplittingSettings)
                //&& (AnimSplittingSettingsList is null || AnimSplittingSettingsList.Length == 0)
                )
            {
                var list = JsonConvert.DeserializeObject<List<AnimSplittingSettings>>(AnimSplittingSettings);
                AnimSplittingSettings = null;
                SetAnimSplittingSettings(list);
            }
            if (!string.IsNullOrEmpty(ConfigNameToAnimNameMap)
                //&& (ConfigNameToAnimNameMapList is null || ConfigNameToAnimNameMapList.Length == 0)
                )
            {
                var list = JsonConvert.DeserializeObject<Dictionary<string, string>>(ConfigNameToAnimNameMap);
                ConfigNameToAnimNameMap = null;
                SetConfigNameToAnimNameMap(list);
            }
        }
    }
}