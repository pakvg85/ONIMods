using PeterHan.PLib.Core;
using PeterHan.PLib.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using static STRINGS.BUILDING.STATUSITEMS;
using static STRINGS.DUPLICANTS.PERSONALITIES;

namespace ExtendedBuildingWidth
{
    public class Dialog_EditAnimSlicingSettings
    {
        private class AnimSplittingSettings_Item
        {
            public Guid Guid { get; init; }
            public string ConfigName { get; init; }
            public string SymbolName { get; set; }
            public string FrameIndex { get; set; }
            public bool IsActive { get; set; }
            public int MiddlePart_X { get; set; }
            public int MiddlePart_Width { get; set; }
            public FillingStyle FillingStyle { get; set; }
            public bool DoFlipEverySecondIteration { get; set; }
        }

        private GameObject _dataPanelParentGo = null;
        private GameObject _previewPanelParentGo = null;
        private GameObject _dataPanelGo = null;
        private GameObject _previewPanelGo = null;
        private readonly List<AnimSplittingSettings_Item> _dialogData = new List<AnimSplittingSettings_Item>();
        private Dictionary<Guid, Dictionary<string, GameObject>> _dialogDataRecordToFrameDropdownGoMap = new Dictionary<Guid, Dictionary<string, GameObject>>();
        private Dictionary<Guid, Dictionary<string, PComboBox<StringListOption>>> _comboBoxes = new Dictionary<Guid, Dictionary<string, PComboBox<StringListOption>>>();
        private readonly ModSettings _modSettings;

        const string DialogId = "EditAnimSlicingSettings";
        const string DialogTitle = "Edit Anim Slicing Settings";
        const string DialogBodyGridPanelId = "EditAnimSlicingSettingsDialogBody";

        const string MiddlePartAlias = "Middle Part";
        const string KAnimSpriteAlias = "KAnim Sprite";
        const string FillingStyleAlias = "filling style";
        string ExpansionStyleCombobox_Tooltip = $"Stretch: {MiddlePartAlias} will be drawn stretched between left and right parts."
                        + Environment.NewLine + $"Repeat: {MiddlePartAlias} will be drawn repeatedly starting from the left part, until the right part is reached.";
        string DoFlipEverySecondIteration_Tooltip = $"When 'Repeat' {FillingStyleAlias} is chosen: {MiddlePartAlias} will be flipped horizontally every second time it is drawn.";
        string IsActive_Tooltip = "Checked: dynamic buildings of this config will be drawn according to these settings."
          + Environment.NewLine + "Unchecked: dynamic buildings of this config will be drawn old style (fully stretched from left to right).";
        string MiddlePartX_Tooltip = $"X position (in pixels) of the original {KAnimSpriteAlias} that defines {MiddlePartAlias}.";
        string MiddlePartWidth_Tooltip = $"Width (in pixels) of the original {KAnimSpriteAlias} that defines {MiddlePartAlias}.";

        const string DialogOption_Ok = "ok";
        const string DialogOption_Cancel = "cancel";
        const int SpacingInPixels = 7;
        const string OptionKeyEmpty = " ";
        public const string JsonValueEmpty = "";

        private List<int> DefaultGridColumnWidths = new List<int>()
        {
            400,    // config name
            30,     // + (add record)
            30,     // - (remove record)
            120,    // symbol
            80,     // frame index
            80,     // enabled (IsActive)
            120,    // middle part filling style
            80,     // middle part pos X
            80,     // middle part width
            80,     // flip every second time
            80      // preview button
        };
        Vector2 DataGridCellFlex { get; } = Vector2.right; // whatever value is set, it will always stretch to 100%. I don't know why.
        private List<StringListOption> _fillingStyle_Options = new List<StringListOption>()
        {
            new StringListOption("Stretch"),
            new StringListOption("Repeat")
        };
        private Dictionary<string, List<StringListOption>> _configToSymbolDropdownMap;
        private Dictionary<string, Dictionary<string, List<StringListOption>>> _configToSymbolToFrameOptionsMap;

        const int MaxBuildingWidthToShow = 8;
        const int DefaultMiddlePartX = 50;
        const int DefaultMiddlePartWidth = 15;
        const int INDEX_NOT_FOUND = -1;

        public bool ShowTechName { get; set; } = false;
        public bool ShowDropdownsForSymbolsAndFrames { get; set; } = true;
        public bool ActiveRecordInitialized { get; set; } = false;
        public Guid ActiveRecordId { get; set; } = default;
        public int DesiredBuildingWidthToShow { get; set; } = 5;

        public Dialog_EditAnimSlicingSettings(ModSettings modSettings)
        {
            _modSettings = modSettings;
        }

        public void CreateAndShow(object obj)
        {
            var dialog = new PDialog(DialogId)
            {
                Title = DialogTitle,
                DialogClosed = OnDialogClosed,
                Size = new Vector2 { x = 1280, y = 900 },
                MaxSize = new Vector2 { x = 1280, y = 900 },
                SortKey = 200.0f
            }.AddButton(DialogOption_Ok, "OK", null, PUITuning.Colors.ButtonPinkStyle)
            .AddButton(DialogOption_Cancel, "CANCEL", null, PUITuning.Colors.ButtonBlueStyle);

            GenerateInitialData();

            dialog.Body.DynamicSize = true;
            dialog.Body.FlexSize = Vector2.one;
            dialog.Body.Margin = new RectOffset(10, 10, 10, 10);
            dialog.Body.Alignment = TextAnchor.UpperLeft;
            //dialog.Body.BackColor = Color.white;

            var gridPanel = new PGridPanel()
            {
                //BackColor = Color.black,
                DynamicSize = true, FlexSize = Vector2.one // does matter - so the scroll slider will be at far right
            };
            gridPanel.AddColumn(new GridColumnSpec(0, 100));
            gridPanel.AddRow(new GridRowSpec(50, 0));
            gridPanel.AddRow(new GridRowSpec(500, 0));
            gridPanel.AddRow(new GridRowSpec(70, 0));
            gridPanel.AddRow(new GridRowSpec(200, 0));
            dialog.Body.AddChild(gridPanel);

            // contents should lean to left border
            var headerPanelParent = new PPanel("HeaderPanelParent")
            { 
                Alignment = TextAnchor.UpperLeft, // does matter - contents are not stretched and should lean to left
                //BackColor = Color.green,
                DynamicSize = true, FlexSize = Vector2.one // does matter - if not set, then contents will be centered
            };
            var titlesPanel = GenerateTitles();
            //titlesPanel.FlexSize = ... ; // should not be stretched - contents should lean to the left
            //titlesPanel.BackColor = Color.gray;
            headerPanelParent.AddChild(titlesPanel);
            gridPanel.AddChild(headerPanelParent, new GridComponentSpec(0, 0));

            // contents should lean to left border
            // and scroll slider should lean to the right border
            var dataPanelParent = new PPanel("DataPanelParent")
            { 
                //Alignment = ... // doesn't matter - contents are stretched by ScrollPane
                //BackColor = Color.magenta,
                DynamicSize = true, FlexSize = Vector2.one // does matter - so the scroll slider will be at far right
            };
            dataPanelParent.OnRealize += (realized) => { _dataPanelParentGo = realized; };
            var dataPanel = GenerateDataPanel();
            //dataPanel.BackColor = Color.yellow;
            // dataPanel.FlexSize = Vector2.one; // should not be stretched - contents should lean to the left
            var dataPanelWithScroll = CreateScrollForPanel(dataPanel);
            dataPanelWithScroll.FlexSize = Vector2.one; // does matter - so the scroll slider will be at far right
            dataPanelWithScroll.OnRealize += (realized) => { _dataPanelGo = realized; };
            dataPanelParent.AddChild(dataPanelWithScroll);
            gridPanel.AddChild(dataPanelParent, new GridComponentSpec(1, 0));

            // contents should be centered
            var controlPanelParent = new PPanel("ControlPanelParent")
            { 
                //Alignment = ... FlexSize = ... // doesn't matter - contents will be centered anyway
            };
            var controlPanel = GenerateControlPanel();
            //controlPanel.BackColor = Color.blue;
            controlPanelParent.AddChild(controlPanel);
            gridPanel.AddChild(controlPanelParent, new GridComponentSpec(2, 0));

            // contents should be centered
            // and scroll slider should lean to the right border
            var previewPanelParent = new PPanel("PreviewPanelParent")
            {
                Direction = PanelDirection.Vertical,
                //BackColor = Color.cyan,
                //Alignment = TextAnchor.UpperCenter, // doesn't matter - contents are stretched by ScrollPane
                DynamicSize = true, FlexSize = Vector2.one // does matter - so the scroll slider will be at far right
            };
            previewPanelParent.OnRealize += (realized) => { _previewPanelParentGo = realized; };
            if (   ActiveRecordInitialized
                && TryGetRecord(ActiveRecordId, out var settings_Item)
                && !string.IsNullOrEmpty(settings_Item.ConfigName)
                && DynamicBuildingsManager.ConfigMap.ContainsKey(settings_Item.ConfigName)
                && TryGenerateDynamicBuildingPreviewPanel(settings_Item, out var previewPanel)
                )
            {
                previewPanel.FlexSize = Vector2.right; // does matter - contents should be stretched so the contents will be centered and scroll will be at far right
                //previewPanel.BackColor = Color.red;
                var previewPanelWithScroll = CreateScrollForPanel(previewPanel);
                previewPanelWithScroll.FlexSize = Vector2.one; // does matter - contents should be stretched so the scroll slider will be at far right
                previewPanelWithScroll.OnRealize += (realized) => { _previewPanelGo = realized; };
                previewPanelParent.AddChild(previewPanelWithScroll);
            }
            gridPanel.AddChild(previewPanelParent, new GridComponentSpec(3, 0));

            dialog.Show();
        }

        internal void RebuildDataPanel()
        {
#if DEBUG
            Debug.Log("--- RebuildDataPanel");
            Debug.Log($"_dialogDataRecordToFrameDropdownGoMap.Clear()");
#endif
            _dialogDataRecordToFrameDropdownGoMap.Clear();

            if (_dataPanelGo != null)
            {
                _dataPanelGo.SetParent(null);
                _dataPanelGo.SetActive(false);
                _dataPanelGo = null;
            }

            var dataPanel = GenerateDataPanel();
            //dataPanel.BackColor = Color.yellow;
            var dataPanelWithScroll = CreateScrollForPanel(dataPanel);
            dataPanelWithScroll.FlexSize = Vector2.one;
            dataPanelWithScroll.OnRealize += (realized) => { _dataPanelGo = realized; };
            dataPanelWithScroll.AddTo(_dataPanelParentGo);
        }

        private void RebuildPreviewPanel(AnimSplittingSettings_Item record)
        {
            if (_previewPanelGo != null)
            {
                _previewPanelGo.SetParent(null);
                _previewPanelGo.SetActive(false);
                _previewPanelGo = null;
            }

            if (TryGenerateDynamicBuildingPreviewPanel(record, out var previewPanel))
            {
                previewPanel.FlexSize = Vector2.right;
                //previewPanel.BackColor = Color.red;
                var previewPanelWithScroll = CreateScrollForPanel(previewPanel);
                previewPanelWithScroll.FlexSize = Vector2.one;
                previewPanelWithScroll.OnRealize += (realized) => { _previewPanelGo = realized; };
                previewPanelWithScroll.AddTo(_previewPanelParentGo);
            }
        }

        public List<string> GetConfigNames()
        {
            return _dialogData.Select(d => d.ConfigName).Distinct().ToList();
        }

        public void ApplyChanges(ICollection<System.Tuple<string, bool>> modifiedRecords)
        {
            foreach (var entry in modifiedRecords)
            {
                var configName = entry.Item1;
                bool doAddNewRecord = entry.Item2;
                var hasRecordsWithThisConfig = _dialogData.Any(x => x.ConfigName == configName);

                if (!doAddNewRecord)
                {
                    if (hasRecordsWithThisConfig)
                    {
                        _dialogData.RemoveAll(x => x.ConfigName == configName);
                    }
                }
                else
                {
                    if (!hasRecordsWithThisConfig)
                    {
                        var newRec = new AnimSplittingSettings_Item()
                        {
                            Guid = Guid.NewGuid(),
                            ConfigName = configName,
                            SymbolName = JsonValueEmpty,
                            FrameIndex = JsonValueEmpty,
                            IsActive = true,
                            MiddlePart_X = DefaultMiddlePartX,
                            MiddlePart_Width = DefaultMiddlePartWidth,
                            FillingStyle = FillingStyle.Stretch,
                            DoFlipEverySecondIteration = false
                        };
                        _dialogData.Add(newRec);
                    }
                }
            }
        }

        private PGridPanel GenerateTitles()
        {
            var tableTitlesPanel = new PGridPanel(DialogBodyGridPanelId);
            foreach (var defaultGridColumnWidth in DefaultGridColumnWidths)
            {
                tableTitlesPanel.AddColumn(new GridColumnSpec(defaultGridColumnWidth));
            }
            tableTitlesPanel.AddRow(new GridRowSpec());
            tableTitlesPanel.AddRow(new GridRowSpec());

            int iRow = 0;
            int iCol = -1;
            tableTitlesPanel.AddChild(new PLabel() { Text = "Config Name" }, new GridComponentSpec(iRow, ++iCol));
            tableTitlesPanel.AddChild(new PLabel() { Text = "+" }, new GridComponentSpec(iRow, ++iCol));
            tableTitlesPanel.AddChild(new PLabel() { Text = "-" }, new GridComponentSpec(iRow, ++iCol));
            tableTitlesPanel.AddChild(new PLabel() { Text = "Symbol" }, new GridComponentSpec(iRow, ++iCol));
            tableTitlesPanel.AddChild(new PLabel() { Text = "Frame" }, new GridComponentSpec(iRow, ++iCol));
            tableTitlesPanel.AddChild(new PLabel() { Text = "Enabled", ToolTip = IsActive_Tooltip }, new GridComponentSpec(iRow, ++iCol));
            tableTitlesPanel.AddChild(new PLabel() { Text = $"{MiddlePartAlias}", ToolTip = ExpansionStyleCombobox_Tooltip }, new GridComponentSpec(iRow, ++iCol));
            tableTitlesPanel.AddChild(new PLabel() { Text = "Filling Style", ToolTip = ExpansionStyleCombobox_Tooltip }, new GridComponentSpec(iRow + 1, iCol));
            tableTitlesPanel.AddChild(new PLabel() { Text = $"{MiddlePartAlias}", ToolTip = MiddlePartX_Tooltip }, new GridComponentSpec(iRow, ++iCol));
            tableTitlesPanel.AddChild(new PLabel() { Text = "Position X", ToolTip = MiddlePartX_Tooltip }, new GridComponentSpec(iRow + 1, iCol));
            tableTitlesPanel.AddChild(new PLabel() { Text = $"{MiddlePartAlias}", ToolTip = MiddlePartWidth_Tooltip }, new GridComponentSpec(iRow, ++iCol));
            tableTitlesPanel.AddChild(new PLabel() { Text = "Width", ToolTip = MiddlePartWidth_Tooltip }, new GridComponentSpec(iRow + 1, iCol));
            tableTitlesPanel.AddChild(new PLabel() { Text = "Flip Every", ToolTip = DoFlipEverySecondIteration_Tooltip }, new GridComponentSpec(iRow, ++iCol));
            tableTitlesPanel.AddChild(new PLabel() { Text = "Second Time", ToolTip = DoFlipEverySecondIteration_Tooltip }, new GridComponentSpec(iRow + 1, iCol));
            tableTitlesPanel.AddChild(new PLabel() { Text = "preview" }, new GridComponentSpec(iRow, ++iCol));
            return tableTitlesPanel;
        }

        private void GetConfigLabelTextAndTooltip(string configName, Dictionary<string, BuildingDescription> allBuildingsDict, bool showTechName, out string configLabelText, out string configLabelTooltip)
        {
            string configCaption = string.Empty;
            if (allBuildingsDict.TryGetValue(configName, out var buildingDescription))
            {
                configCaption = buildingDescription.Caption;
            }
            if (string.IsNullOrEmpty(configCaption))
            {
                configCaption = configName;
            }
            configLabelText = showTechName ? configName : configCaption;
            configLabelTooltip = !showTechName ? configName : configCaption;
        }

        private bool TryFindOptionInDropdown(string jsonValue, List<StringListOption> options, out StringListOption chosenOption)
        {
#if DEBUG
            Debug.Log("--- TryFindOptionInDropdown");
            Debug.Log($"jsonValue='{jsonValue}',");
#endif
            // don't allow to set ' ' (OptionKeyEmpty) into jsonValue. only '' (JsonValueEmpty) is allowed.
            if (   jsonValue == OptionKeyEmpty
                && OptionKeyEmpty != JsonValueEmpty
                )
            {
                chosenOption = null;
                return false;
            }

            var optionsKey = OptionKeyByJsonValue(jsonValue);
            var optionIndex = options.FindIndex(x => x.ToString() == optionsKey);
#if DEBUG
            Debug.Log($"optionsKey='{optionsKey}', optionIndex='{optionIndex}'");
#endif
            if (optionIndex == INDEX_NOT_FOUND)
            {
                chosenOption = null;
                return false;
            }

            chosenOption = options[optionIndex];
            return true;
        }

        private bool TryGetSymbolDropdownOption(string symbolName, string configName, out StringListOption chosenOption, out List<StringListOption> symbolOptions)
        {
            if (   !_configToSymbolDropdownMap.TryGetValue(configName, out symbolOptions)
                || !TryFindOptionInDropdown(symbolName, symbolOptions, out chosenOption)
                )
            {
                symbolOptions = null;
                chosenOption = null;
                return false;
            }
            return true;
        }

        private bool TryGetFrameDropdownOption(string frameIndex, string configName, string symbolName, out StringListOption chosenOption, out List<StringListOption> frameOptions)
        {
            if (   !_configToSymbolToFrameOptionsMap.TryGetValue(configName, out var symbolToFrameOptionsMap)
                || !symbolToFrameOptionsMap.TryGetValue(OptionKeyByJsonValue(symbolName), out frameOptions)
                || !TryFindOptionInDropdown(frameIndex, frameOptions, out chosenOption)
                )
            {
                frameOptions = null;
                chosenOption = null;
                return false;
            }
            return true;
        }

        private void FillDataPanel_Block(
                PGridPanel gridPanel,
                int iRow,
                AnimSplittingSettings_Item entry,
                Dictionary<string, BuildingDescription> allBuildingsDict
            )
        {
            int iCol = -1;

            var screenElementName = GenerateScreenElementName(entry);
            var configName = entry.ConfigName;

            if (iRow == 0)
            {
                // 0) Config Name
                iCol++;
                GetConfigLabelTextAndTooltip(configName, allBuildingsDict, ShowTechName, out var configLabelText, out var configLabelTooltip);
                var cnN = new PLabel(screenElementName)
                {
                    Text = configLabelText,
                    ToolTip = configLabelTooltip
                };
                gridPanel.AddChild(cnN, new GridComponentSpec(iRow, iCol) { Alignment = TextAnchor.MiddleLeft });

                // 1) Add new record for config
                iCol++;
                var addS = new PButton(screenElementName) { OnClick = OnClick_AddNew, Text = "+" };
                gridPanel.AddChild(addS, new GridComponentSpec(iRow, iCol));
            }
            else
            {
                iCol++;
                iCol++;
            }

            // 2) Remove record
            iCol++;
            var remS = new PButton(screenElementName) { OnClick = OnClick_RemoveRecord, Text = "-" };
            gridPanel.AddChild(remS, new GridComponentSpec(iRow, iCol));

            // 3) Symbol Name
            iCol++;
            bool symbolDropdownAdded = false;
            if (   ShowDropdownsForSymbolsAndFrames
                && TryGetSymbolDropdownOption(entry.SymbolName, configName, out var symbolChosenOption, out var symbolOptions)
                )
            {
                var smN = new PComboBox<StringListOption>(screenElementName)
                {
                    Content = symbolOptions,
                    InitialItem = symbolChosenOption,
                    OnOptionSelected = OnOptionSelected_Symbols,
                    FlexSize = DataGridCellFlex
                };
                gridPanel.AddChild(smN, new GridComponentSpec(iRow, iCol));
                symbolDropdownAdded = true;
            }
            else
            {
                var smN = new PTextField(screenElementName)
                {
                    Text = entry.SymbolName,
                    OnTextChanged = OnTextChanged_SymbolName,
                    FlexSize = DataGridCellFlex
                };
                gridPanel.AddChild(smN, new GridComponentSpec(iRow, iCol));
            }

            // 4) Frame Index
            iCol++;
            if (   ShowDropdownsForSymbolsAndFrames
                && symbolDropdownAdded
                && _configToSymbolToFrameOptionsMap.TryGetValue(configName, out var symbolToFrameOptionsMap)
                && (entry.FrameIndex == JsonValueEmpty || int.TryParse(entry.FrameIndex, out _))
                )
            {
                var symbolOptionKey_current = OptionKeyByJsonValue(entry.SymbolName);
                foreach (var symbolOptionKey_iteration in symbolToFrameOptionsMap.Keys)
                {
                    var symbolJsonValue_iteration = JsonValueByOptionKey(symbolOptionKey_iteration);
                    string frameIndex_iteration = (symbolOptionKey_iteration == symbolOptionKey_current) ? entry.FrameIndex : JsonValueEmpty;
                    TryGetFrameDropdownOption(frameIndex_iteration, configName, symbolJsonValue_iteration, out var chosenFrameOption, out var frameOptions);
                    var frI = new PComboBox<StringListOption>(screenElementName + "/" + symbolOptionKey_iteration)
                    {
                        Content = frameOptions,
                        InitialItem = chosenFrameOption,
                        OnOptionSelected = OnOptionSelected_Frames,
                        FlexSize = DataGridCellFlex
                    };
                    frI.OnRealize += OnRealize_FrameDropdown;
                    gridPanel.AddChild(frI, new GridComponentSpec(iRow, iCol));
                }
            }
            else
            {
                var frI = new PTextField(screenElementName)
                {
                    Text = entry.FrameIndex,
                    OnTextChanged = OnTextChanged_FrameIndex,
                    FlexSize = DataGridCellFlex
                };
                gridPanel.AddChild(frI, new GridComponentSpec(iRow, iCol));
            }

            // 5) IsActive checkbox
            iCol++;
            var iA = new PCheckBox(screenElementName)
            {
                InitialState = entry.IsActive ? 1 : 0,
                OnChecked = OnChecked_IsActive
            };
            gridPanel.AddChild(iA, new GridComponentSpec(iRow, iCol));

            // 6) Middle Part Filling style
            iCol++;
            var eSt = new PComboBox<StringListOption>(screenElementName)
            {
                Content = _fillingStyle_Options,
                InitialItem = _fillingStyle_Options[(int)entry.FillingStyle],
                OnOptionSelected = OnOptionSelected_FillingStyle,
                FlexSize = DataGridCellFlex
            };
            gridPanel.AddChild(eSt, new GridComponentSpec(iRow, iCol));

            // 7) Middle Part starting X position
            iCol++;
            var mfX = new PTextField(screenElementName)
            {
                Text = entry.MiddlePart_X.ToString(),
                OnTextChanged = OnTextChanged_MiddlePart_X,
                FlexSize = DataGridCellFlex
            };
            gridPanel.AddChild(mfX, new GridComponentSpec(iRow, iCol));

            // 8) Middle Part Width
            iCol++;
            var mfW = new PTextField(screenElementName)
            {
                Text = entry.MiddlePart_Width.ToString(),
                OnTextChanged = OnTextChanged_MiddlePart_Width,
                FlexSize = DataGridCellFlex
            };
            gridPanel.AddChild(mfW, new GridComponentSpec(iRow, iCol));

            // 9) Flip every second time Checkbox
            iCol++;
            var df = new PCheckBox(screenElementName)
            {
                InitialState = entry.DoFlipEverySecondIteration ? 1 : 0,
                OnChecked = OnChecked_DoFlipEverySecondIteration
            };
            gridPanel.AddChild(df, new GridComponentSpec(iRow, iCol));

            // 10) Preview button
            iCol++;
            if (DynamicBuildingsManager.ConfigMap.ContainsKey(configName))
            {
                var prvB = new PButton(screenElementName) { OnClick = OnClick_Preview, Text = "preview" };
                gridPanel.AddChild(prvB, new GridComponentSpec(iRow, iCol));
            }
        }

        private bool TryGetSymbolDropdownsForConfig(
                string configName,
                out List<StringListOption> availableSymsols
            )
        {
            if (   !DynamicBuildingsManager.ConfigMap.TryGetValue(configName, out var config)
                || !DynamicBuildingsManager.ConfigToBuildingDefMap.TryGetValue(config, out var originalDef)
                )
            {
                availableSymsols = null;
                return false;
            }

            availableSymsols = new List<StringListOption>() { new StringListOption(OptionKeyEmpty) };
            foreach (var symbol in originalDef.AnimFiles[0].GetData().build.symbols)
            {
                var symbolName = symbol.hash.ToString();
                if (symbolName == "ui")
                {
                    continue;
                }

                availableSymsols.Add(new StringListOption(symbolName));
            }
            return true;
        }

        private Dictionary<string, List<StringListOption>> GenerateDropdownsForSymbols(List<string> configNames)
        {
#if DEBUG
            Debug.Log("--- GenerateDropdownsForSymbols");
#endif
            var configToSymbolDropdownMap = new Dictionary<string, List<StringListOption>>();
            foreach (var configName in configNames)
            {
                if (TryGetSymbolDropdownsForConfig(configName, out var availableSymsols))
                {
                    configToSymbolDropdownMap.Add(configName, availableSymsols);
                }
            }
#if DEBUG
            Debug.Log($"--- _symbolsForConfigs:");
            foreach (var c in configToSymbolDropdownMap)
            {
                Debug.Log($"config: '{c.Key}'");
                foreach (var s in c.Value)
                {
                    Debug.Log($"symbol: '{s}'");
                }
            }
#endif
            return configToSymbolDropdownMap;
        }

        private bool TryGetFrameDropdownsForConfig(
                string configName,
                out Dictionary<string, List<StringListOption>> symbolToFrameOptionsMap
            )
        {
            if (!DynamicBuildingsManager.ConfigMap.TryGetValue(configName, out var config)
                || !DynamicBuildingsManager.ConfigToBuildingDefMap.TryGetValue(config, out var originalDef)
                )
            {
                symbolToFrameOptionsMap = null;
                return false;
            }

            symbolToFrameOptionsMap = new Dictionary<string, List<StringListOption>>();

            var frameEmptyOptions = new List<StringListOption>() { new StringListOption(OptionKeyEmpty) };
            symbolToFrameOptionsMap.Add(OptionKeyEmpty, frameEmptyOptions);

            foreach (var symbol in originalDef.AnimFiles[0].GetData().build.symbols)
            {
                var symbolName = symbol.hash.ToString();
                if (symbolName == "ui")
                {
                    continue;
                }

                var framesOptions = new List<StringListOption>() { new StringListOption(OptionKeyEmpty) };
                for (var frameIdx = 0; frameIdx < symbol.numFrames; frameIdx++)
                {
                    framesOptions.Add(new StringListOption(frameIdx.ToString()));
                }
                symbolToFrameOptionsMap.Add(symbolName, framesOptions);
            }
            return true;
        }

        private Dictionary<string, Dictionary<string, List<StringListOption>>> GenerateDropdownsForFrames(List<string> configNames)
        {
            var configToSymbolToFramesDropdownsMap = new Dictionary<string, Dictionary<string, List<StringListOption>>>();
            foreach (var configName in configNames)
            {
                if (TryGetFrameDropdownsForConfig(configName, out var framesForSymbols))
                {
                    configToSymbolToFramesDropdownsMap.Add(configName, framesForSymbols);
                }
            }
#if DEBUG
            Debug.Log($"--- _framesForSymbolsForConfigs:");
            foreach (var c in configToSymbolToFramesDropdownsMap)
            {
                Debug.Log($"config: '{c.Key}'");
                foreach (var s in c.Value)
                {
                    Debug.Log($"symbol: '{s.Key}'");
                    foreach (var f in s.Value)
                    {
                        Debug.Log($"frame: '{f}'");
                    }
                }
            }
#endif
            return configToSymbolToFramesDropdownsMap;
        }

        private PScrollPane CreateScrollForPanel(PPanel panel)
        {
            var scrollPane = new PScrollPane()
            {
                Child = panel,
                TrackSize = 20,
                ScrollHorizontal = false,
                ScrollVertical = true,
                AlwaysShowHorizontal = false,
                AlwaysShowVertical = false
            };
            return scrollPane;
        }

        private PPanel GenerateDataPanel()
        {
            var dialogData_GroupedByConfigName = _dialogData.GroupBy(r => r.ConfigName).ToDictionary(t => t.Key, y => y.Select(r => r).ToList());

            var configNames = dialogData_GroupedByConfigName.Keys.ToList();
            _configToSymbolDropdownMap = GenerateDropdownsForSymbols(configNames);
            _configToSymbolToFrameOptionsMap = GenerateDropdownsForFrames(configNames);

            var recordsPanel = new PPanel("RecordsPanel")
            {
                Direction = PanelDirection.Vertical
            };

            var allBuildingsDict = SettingsManager.ListOfAllBuildings.ToDictionary(x => x.ConfigName, y => y);

            var iRowOuter = -1;
            foreach (var kvp in dialogData_GroupedByConfigName)
            {
                var configName = kvp.Key;
                var recordsOfConfig = kvp.Value;
                ++iRowOuter;
                var gridPanel = new PGridPanel(DialogBodyGridPanelId);
                foreach (var defaultGridColumnWidth in DefaultGridColumnWidths)
                {
                    gridPanel.AddColumn(new GridColumnSpec(defaultGridColumnWidth));
                }
                for (var i = 0; i < recordsOfConfig.Count; i++)
                {
                    gridPanel.AddRow(new GridRowSpec());
                }
                recordsPanel.AddChild(gridPanel);

                int iRow = -1;
                foreach (var entry in recordsOfConfig)
                {
                    ++iRow;
                    FillDataPanel_Block(gridPanel, iRow, entry, allBuildingsDict);
                }
            }

            return recordsPanel;
        }

        private static AnimSplittingSettings_Internal MapToInternal(AnimSplittingSettings_Item entry, KAnimHashedString symbolName, int frameIndex)
        {
            var newEntry = new AnimSplittingSettings_Internal
            {
                ConfigName = entry.ConfigName,
                SymbolName = symbolName,
                FrameIndex = frameIndex,
                IsActive = entry.IsActive,
                MiddlePart_X = entry.MiddlePart_X,
                MiddlePart_Width = entry.MiddlePart_Width,
                FillingStyle = entry.FillingStyle,
                DoFlipEverySecondIteration = entry.DoFlipEverySecondIteration
            };
            return newEntry;
        }

        public static KAnimHashedString SymbolHashOrFirstSymbolFromConfig(string symbolName, string configName)
        {
            if (!string.IsNullOrEmpty(symbolName))
            {
                return new KAnimHashedString(symbolName);
            }

            var config = DynamicBuildingsManager.ConfigMap[configName];
            var originalDef = DynamicBuildingsManager.ConfigToBuildingDefMap[config];
            var symbol = originalDef.AnimFiles.First().GetData().build.symbols.First();
            return symbol.hash;
        }

        public static int FrameIndexOrFirstFrameFromConfig(string frameIndex, string configName)
        {
            if (!string.IsNullOrEmpty(frameIndex))
            {
                return int.Parse(frameIndex);
            }

            return 0; // first frame for every symbol is always '0'.
        }

        private bool TryGenerateDynamicBuildingPreviewPanel(AnimSplittingSettings_Item settings_Item, out PPanel result)
        {
            if (!DynamicBuildingsManager.ConfigMap.TryGetValue(settings_Item.ConfigName, out var config))
            {
                result = null;
                return false;
            }

            var originalDef = DynamicBuildingsManager.ConfigToBuildingDefMap[config];
            if (DesiredBuildingWidthToShow < originalDef.WidthInCells)
            {
                DesiredBuildingWidthToShow = originalDef.WidthInCells;
            }

            var symbolName = SymbolHashOrFirstSymbolFromConfig(settings_Item.SymbolName, settings_Item.ConfigName);
            var frameIndex = FrameIndexOrFirstFrameFromConfig(settings_Item.FrameIndex, settings_Item.ConfigName);
            var settings_Internal = MapToInternal(settings_Item, symbolName, frameIndex);
            if (!TryGenerateSpritesToShow(settings_Internal, DesiredBuildingWidthToShow, out var sprites))
            {
                result = null;
                return false;
            }

            var previewPanel = new PPanel("PreviewPanel")
            { 
                Direction = PanelDirection.Vertical
            };

            var basicInfo = GenerateBasicInfoPanel(settings_Internal);
            previewPanel.AddChild(basicInfo);
            var panelWithCaptions = GenerateCaptionsForSprites(sprites);
            previewPanel.AddChild(panelWithCaptions);
            var panelWithSprites = GenerateScreenComponentsForSprites(sprites, settings_Internal.DoFlipEverySecondIteration);
            previewPanel.AddChild(panelWithSprites);

            result = previewPanel;
            return true;
        }

        private PPanel GenerateBasicInfoPanel(AnimSplittingSettings_Internal settings_Internal)
        {
            var result = new PPanel()
            {
                Direction = PanelDirection.Horizontal,
                Margin = new RectOffset(0, 0, 10, 20)
            };

            var configName = settings_Internal.ConfigName;

            var config = DynamicBuildingsManager.ConfigMap[configName];
            var originalDef = DynamicBuildingsManager.ConfigToBuildingDefMap[config];
            var symbol = originalDef.AnimFiles.First().GetData().build.GetSymbol(settings_Internal.SymbolName);
            if (symbol == null)
            {
                throw new ArgumentException($"symbolName='{settings_Internal.SymbolName}' not found for config '{configName}'");
            }

            var symbolFrame = symbol.GetFrame(settings_Internal.FrameIndex);
            if (symbolFrame.symbolIdx == INDEX_NOT_FOUND)
            {
                throw new ArgumentException($"symbolFrame '{settings_Internal.FrameIndex}' not found for config '{configName}' and symbol '{settings_Internal.SymbolName}'");
            }

            var texture = originalDef.AnimFiles.First().GetData().build.GetTexture(0);

            var margin = new RectOffset(20, 20, 0, 0);
            var origWidth = ((int)((symbolFrame.uvMax.x - symbolFrame.uvMin.x) * texture.width)).ToString();
            var origPositionX = ((int)(symbolFrame.uvMin.x * texture.width)).ToString();

            result.AddChild(new PLabel() { Text = $"Original X position: {origPositionX}", Margin = margin });
            result.AddChild(new PLabel() { Text = $"Original Width: {origWidth}", Margin = margin });
            result.AddChild(new PLabel() { Text = $"Dynamic building size: {DesiredBuildingWidthToShow}", Margin = margin });

            result.AddChild(new PButton() { Text = "<", Margin = margin, FlexSize = Vector2.up, OnClick = OnClick_MakeSmaller });
            result.AddChild(new PButton() { Text = ">", Margin = margin, FlexSize = Vector2.up, OnClick = OnClick_MakeBigger });

            return result;
        }

        private bool TryGenerateSpritesToShow(AnimSplittingSettings_Internal settings_Internal, int desiredBuildingWidthToShow, out List<Sprite> result)
        {
#if DEBUG
            Debug.Log("--- GenerateSpritesToShow");
            Debug.Log($"configName='{settings_Internal.ConfigName}', symbolName='{settings_Internal.SymbolName}', frameIndex='{settings_Internal.FrameIndex}'");
#endif
            var configName = settings_Internal.ConfigName;

            var config = DynamicBuildingsManager.ConfigMap[configName];
            var extendableConfigSettings = _modSettings.GetExtendableConfigSettingsList().ToDictionary(x => x.ConfigName, y => y);
            var extendableConfigSettingsItem = extendableConfigSettings[configName];
            var originalDef = DynamicBuildingsManager.ConfigToBuildingDefMap[config];
            var texture = originalDef.AnimFiles.First().GetData().build.GetTexture(0);
            int textureWidth = texture.width;
            var dynamicBuildingWidthToShow = Math.Min(Math.Min(desiredBuildingWidthToShow, extendableConfigSettingsItem.MaxWidth), MaxBuildingWidthToShow);
            int widthInCellsDelta = dynamicBuildingWidthToShow - originalDef.WidthInCells;

            var symbol = originalDef.AnimFiles.First().GetData().build.GetSymbol(settings_Internal.SymbolName);
            if (symbol == null)
            {
                result = null;
                return false;
            }
#if DEBUG
            Debug.Log($"animSlicingSettingsItem.FrameIndex='{settings_Internal.FrameIndex}'");
#endif
            var frameIdx = symbol.GetFrameIdx(settings_Internal.FrameIndex);
#if DEBUG
            Debug.Log($"frameIdx='{frameIdx}'");
#endif
            var symbolFrame = symbol.GetFrame(settings_Internal.FrameIndex);
            if (symbolFrame.symbolIdx == INDEX_NOT_FOUND)
            {
                result = null;
                return false;
            }
#if DEBUG
            Debug.Log($"origFrame: [{symbolFrame.symbolIdx}, {symbolFrame.sourceFrameNum}] - [{symbolFrame.uvMin.x}, {symbolFrame.uvMax.x}]");
#endif
            var smallerFrames = DynamicAnimManager.SplitFrameIntoParts(
                origFrame: symbolFrame,
                textureWidth: textureWidth,
                widthInCellsDelta: widthInCellsDelta,
                middle_X: settings_Internal.MiddlePart_X,
                middle_Width: settings_Internal.MiddlePart_Width,
                fillingStyle: settings_Internal.FillingStyle,
                doFlipEverySecondIteration: settings_Internal.DoFlipEverySecondIteration
            );

            try
            {
                var sprites = new List<Sprite>();
                foreach (var frame in smallerFrames)
                {
                    var sprite = DynamicAnimManager.GenerateSpriteForFrame(frame, texture);
                    sprites.Add(sprite);
                }
                result = sprites;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("ExtendedBuildingWidth - unable to generate sprites");
                Debug.LogWarning(e.ToString());
                result = null;
                return false;
            }
        }

        private PPanel GenerateCaptionsForSprites(List<Sprite> sprites)
        {
#if DEBUG
            Debug.Log("--- GenerateCaptionsForSprites");
#endif
            var panelWithSprites = new PPanel() { DynamicSize = false };
            if (sprites.Count == 0)
            {
                return panelWithSprites;
            }

            var gridPanelWithSprites = new PGridPanel() { DynamicSize = false };
            panelWithSprites.AddChild(gridPanelWithSprites);
            for (var spriteIdx = 0; spriteIdx < sprites.Count; spriteIdx++)
            {
                int outputWidth = (int)(sprites[spriteIdx].rect.width / sprites[spriteIdx].pixelsPerUnit * 100);
                gridPanelWithSprites.AddColumn(new GridColumnSpec(outputWidth, 100));
            }
            gridPanelWithSprites.AddRow(new GridRowSpec());

            int columnIdx = -1;
            for (var spriteIdx = 0; spriteIdx < sprites.Count; spriteIdx++)
            {
                columnIdx++;

                var label = new PLabel() { DynamicSize = false };
                if (spriteIdx == 0)
                {
                    label.Text = "Left part";
                }
                else if (spriteIdx == sprites.Count - 1)
                {
                    label.Text = "Right part";
                }
                else
                {
                    label.Text = "Middle part";
                }
                gridPanelWithSprites.AddChild(label, new GridComponentSpec(0, columnIdx));
            }

            return panelWithSprites;
        }

        private PPanel GenerateScreenComponentsForSprites(List<Sprite> sprites, bool doFlipEverySecondIteration)
        {
#if DEBUG
            Debug.Log("--- GenerateScreenComponentsForSprites");
#endif
            var panelWithSprites = new PPanel() { DynamicSize = false };
            if (sprites.Count == 0)
            {
                return panelWithSprites;
            }

            var gridPanelWithSprites = new PGridPanel() { DynamicSize = false };
            panelWithSprites.AddChild(gridPanelWithSprites);
            for (var spriteIdx = 0; spriteIdx < sprites.Count; spriteIdx++)
            {
                int outputWidth = (int)(sprites[spriteIdx].rect.width / sprites[spriteIdx].pixelsPerUnit * 100);
                gridPanelWithSprites.AddColumn(new GridColumnSpec(outputWidth, 100));
            }
            gridPanelWithSprites.AddRow(new GridRowSpec(sprites[0].rect.height, 100));
            int columnIdx = -1;
            for (var spriteIdx = 0; spriteIdx < sprites.Count; spriteIdx++)
            {
                columnIdx++;

                bool doFlip = doFlipEverySecondIteration
                    && spriteIdx > 0 && spriteIdx < sprites.Count - 1
                    && (spriteIdx % 2 == 0);

                var ppanel = new PPanelWithFlippableImage()
                {
                    DynamicSize = false,
                    FlexSize = new Vector2(100, 100),
                    FlipByX = doFlip
                };
                ppanel.BackImage = sprites[spriteIdx];
                ppanel.ImageMode = UnityEngine.UI.Image.Type.Sliced;
                ppanel.BackColor = UnityEngine.Color.white;

                int outputWidth = (int)(sprites[spriteIdx].rect.width / sprites[spriteIdx].pixelsPerUnit);
                int outputHeight = (int)(sprites[spriteIdx].rect.height);
                gridPanelWithSprites.AddChild(ppanel, new GridComponentSpec(0, columnIdx));
            }

            return panelWithSprites;
        }

        private PPanel GenerateControlPanel()
        {
            var controlPanel = new PPanel("ControlPanel")
            {
                Direction = PanelDirection.Horizontal,
                Spacing = SpacingInPixels,
                Margin = new RectOffset(40, 40, 10, 10)
            };
            var cbShowTechName = new PCheckBox();
            cbShowTechName.InitialState = ShowTechName ? 1 : 0;
            cbShowTechName.Text = "Show tech names";
            cbShowTechName.OnChecked = OnChecked_ShowTechName;
            controlPanel.AddChild(cbShowTechName);

            var cbShowDropdowns = new PCheckBox();
            cbShowDropdowns.InitialState = ShowDropdownsForSymbolsAndFrames ? 1 : 0;
            cbShowDropdowns.Text = "Show Dropdowns for Symbols and Frames";
            cbShowDropdowns.OnChecked = OnChecked_ShowDropdownsForSymbolsAndFrames;
            controlPanel.AddChild(cbShowDropdowns);

            var btnAdd = new PButton();
            btnAdd.Text = "Add or remove records";
            btnAdd.OnClick = OnClick_AddRemoveRecords;
            controlPanel.AddChild(btnAdd);

            return controlPanel;
        }

        private AnimSplittingSettings MapToSource(AnimSplittingSettings_Item entry)
        {
            var result = new AnimSplittingSettings()
            {
                ConfigName = entry.ConfigName,
                SymbolName = entry.SymbolName,
                FrameIndex = entry.FrameIndex,
                IsActive = entry.IsActive,
                MiddlePart_X = entry.MiddlePart_X,
                MiddlePart_Width = entry.MiddlePart_Width,
                FillingMethod = entry.FillingStyle,
                DoFlipEverySecondIteration = entry.DoFlipEverySecondIteration
            };
            return result;
        }

        private AnimSplittingSettings_Item MapToDialog(AnimSplittingSettings entry)
        {
            var result = new AnimSplittingSettings_Item()
            {
                Guid = Guid.NewGuid(),
                ConfigName = entry.ConfigName,
                SymbolName = entry.SymbolName,
                FrameIndex = entry.FrameIndex,
                IsActive = entry.IsActive,
                MiddlePart_X = entry.MiddlePart_X,
                MiddlePart_Width = entry.MiddlePart_Width,
                FillingStyle = entry.FillingMethod,
                DoFlipEverySecondIteration = entry.DoFlipEverySecondIteration
            };
            return result;
        }

        private void GenerateInitialData()
        {
            _dialogData.Clear();
            var sourceData = _modSettings.GetAnimSplittingSettingsList();
            if (sourceData.Count == 0)
            {
                return;
            }

            var dialogData = sourceData.Select(x => MapToDialog(x)).ToList();
            _dialogData.AddRange(dialogData);

            ActiveRecordInitialized = false;
            if (_dialogData.Count > 0)
            {
                var xx = _dialogData.First();
                ActiveRecordInitialized = true;
                ActiveRecordId = GetRecordId(xx);
            }
        }

        private string GenerateScreenElementName(AnimSplittingSettings_Item item)
        {
            return item.Guid.ToString();
        }

        private bool TryParseScreenElementName(string screenElementName, out Guid recordId)
        {
            recordId = Guid.Parse(screenElementName);
            return true;
        }

        private Guid GetRecordId(AnimSplittingSettings_Item record)
        {
            return record.Guid;
        }

        private bool TryGetRecord(Guid recordId, out AnimSplittingSettings_Item record)
        {
            record = _dialogData.Where(x => x.Guid == recordId).FirstOrDefault();
            return record != null;
        }

        private bool TryGetRecordByScreenElementName(string screenElementName, out AnimSplittingSettings_Item record)
        {
            if (!TryParseScreenElementName(screenElementName, out var key))
            {
                record = default;
                return false;
            }
            if (!TryGetRecord(key, out record))
            {
                record = default;
                return false;
            }
            return true;
        }

        private void RedrawFrameDropdownForSymbol(string symbolOptionKey, AnimSplittingSettings_Item record)
        {
            var symbolToFrameDropdownGoMap = _dialogDataRecordToFrameDropdownGoMap[record.Guid];
            foreach (var symbolToFrameDropdownGo in symbolToFrameDropdownGoMap)
            {
                var symbolOptionKey_iteration = symbolToFrameDropdownGo.Key;
                var frameDropdownGo = symbolToFrameDropdownGo.Value;

                if (symbolOptionKey == symbolOptionKey_iteration)
                {
                    frameDropdownGo.SetActive(true);
                    TryGetFrameDropdownOption(JsonValueEmpty, record.ConfigName, record.SymbolName, out var frameChosenOption, out _);
                    PComboBox<StringListOption>.SetSelectedItem(frameDropdownGo, frameChosenOption, false);
                }
                else
                {
                    frameDropdownGo.SetActive(false);
                }
            }
        }

        private string OptionKeyByJsonValue(string jsonValue)
        {
            return (jsonValue != JsonValueEmpty) ? jsonValue : OptionKeyEmpty;
        }

        private string JsonValueByOptionKey(string optionKey)
        {
            return (optionKey != OptionKeyEmpty) ? optionKey : JsonValueEmpty;
        }

        private void OnDialogClosed(string option)
        {
            if (option != DialogOption_Ok)
            {
                return;
            }
            var newRez = _dialogData.Select(x => MapToSource(x)).ToList();
            _modSettings.SetAnimSplittingSettings(newRez);
        }

        private void OnTextChanged_MiddlePart_X(GameObject source, string text)
        {
            if (   !TryGetRecordByScreenElementName(source.name, out var record)
                || !int.TryParse(text, out var parsed)
                )
            {
                source.GetComponent<TMP_InputField>().text = record.MiddlePart_X.ToString();
                return;
            }
            record.MiddlePart_X = parsed;
            RebuildPreviewPanel(record);
        }

        private void OnTextChanged_MiddlePart_Width(GameObject source, string text)
        {
            if (   !TryGetRecordByScreenElementName(source.name, out var record)
                || !int.TryParse(text, out var parsed)
                )
            {
                source.GetComponent<TMP_InputField>().text = record.MiddlePart_Width.ToString();
                return;
            }
            record.MiddlePart_Width = parsed;
            RebuildPreviewPanel(record);
        }

        private void OnTextChanged_SymbolName(GameObject source, string text)
        {
            var jsonValueCandidate = text;
            if (!TryGetRecordByScreenElementName(source.name, out var record)
                || DynamicBuildingsManager.ConfigMap.TryGetValue(record.ConfigName, out _)
                    && !TryGetSymbolDropdownOption(jsonValueCandidate, record.ConfigName, out _, out _)
                )
            {
                source.GetComponent<TMP_InputField>().text = record.SymbolName;
                return;
            }
            record.SymbolName = jsonValueCandidate;
            RebuildPreviewPanel(record);
        }

        private void OnTextChanged_FrameIndex(GameObject source, string text)
        {
            var jsonValueCandidate = text;
            if (   !TryGetRecordByScreenElementName(source.name, out var record)
                ||     DynamicBuildingsManager.ConfigMap.TryGetValue(record.ConfigName, out _)
                    && !TryGetFrameDropdownOption(jsonValueCandidate, record.ConfigName, record.SymbolName, out _, out _)
                )
            {
                source.GetComponent<TMP_InputField>().text = record.FrameIndex;
                return;
            }
            record.FrameIndex = jsonValueCandidate;
            RebuildPreviewPanel(record);
        }

        private void OnOptionSelected_FillingStyle(GameObject source, StringListOption option)
        {
            if (!TryGetRecordByScreenElementName(source.name, out var record))
            {
                return;
            }
            int index = _fillingStyle_Options.IndexOf(option);
            if (index == INDEX_NOT_FOUND)
            {
                return;
            }
            record.FillingStyle = (FillingStyle)index;
            RebuildPreviewPanel(record);
        }

        private void OnOptionSelected_Symbols(GameObject source, StringListOption option)
        {
            if (!TryGetRecordByScreenElementName(source.name, out var record))
            {
                return;
            }
            // no need - we garantee that the value is from a dropdown
            //TryGetSymbolDropdownOption(option.ToString(), record.ConfigName, out var chosenOption, out _);
            var symbolOptionKey = option.ToString();
            record.SymbolName = JsonValueByOptionKey(symbolOptionKey);
            record.FrameIndex = JsonValueEmpty;
            RedrawFrameDropdownForSymbol(symbolOptionKey, record);
            RebuildPreviewPanel(record);
        }

        private void OnOptionSelected_Frames(GameObject source, StringListOption option)
        {
            var split = source.name.Split('/');
            var recordGuid = Guid.Parse(split[0]);
            var symbolOptionKey0 = split[1];
            if (   !TryGetRecord(recordGuid, out var record)
                // no need - we garantee that the value is from a dropdown
                //|| !TryGetFrameDropdownOption(option.ToString(), record.ConfigName, record.SymbolName, out var chosenOption, out _)
                )
            {
                return;
            }
            var frameOptionKey = option.ToString();
            record.FrameIndex = JsonValueByOptionKey(frameOptionKey);
            RebuildPreviewPanel(record);
        }

        private void OnRealize_FrameDropdown(GameObject source)
        {
            var split = source.name.Split('/');
            var recordGuid = Guid.Parse(split[0]);
            var symbolOptionKey_candidate = split[1];

            Dictionary<string, GameObject> subDict;
            if (!_dialogDataRecordToFrameDropdownGoMap.TryGetValue(recordGuid, out subDict))
            {
                subDict = new Dictionary<string, GameObject>();
                _dialogDataRecordToFrameDropdownGoMap.Add(recordGuid, subDict);
            }
            subDict.Add(symbolOptionKey_candidate, source);

            bool activeFlag = false;
            if (TryGetRecord(recordGuid, out var record))
            {
                activeFlag =   recordGuid == record.Guid
                            && symbolOptionKey_candidate == OptionKeyByJsonValue(record.SymbolName);
            }
            source.SetActive(activeFlag);
        }

        private void OnChecked_IsActive(GameObject source, int state)
        {
            int newState = (state + 1) % 2;
            PCheckBox.SetCheckState(source, newState);
            var checkButton = source.GetComponentInChildren<MultiToggle>();
            if (!TryGetRecordByScreenElementName(checkButton.name, out var record))
            {
                return;
            }
            record.IsActive = (newState == PCheckBox.STATE_CHECKED);
        }

        private void OnChecked_DoFlipEverySecondIteration(GameObject source, int state)
        {
            int newState = (state + 1) % 2;
            PCheckBox.SetCheckState(source, newState);
            var checkButton = source.GetComponentInChildren<MultiToggle>();
            if (!TryGetRecordByScreenElementName(checkButton.name, out var record))
            {
                return;
            }
            record.DoFlipEverySecondIteration = (newState == PCheckBox.STATE_CHECKED);
            RebuildPreviewPanel(record);
        }

        private void OnChecked_ShowTechName(GameObject source, int state)
        {
            int newState = (state + 1) % 2;
            PCheckBox.SetCheckState(source, newState);
            ShowTechName = (newState == PCheckBox.STATE_CHECKED);
            RebuildDataPanel();
        }

        private void OnChecked_ShowDropdownsForSymbolsAndFrames(GameObject source, int state)
        {
            int newState = (state + 1) % 2;
            PCheckBox.SetCheckState(source, newState);
            ShowDropdownsForSymbolsAndFrames = (newState == PCheckBox.STATE_CHECKED);
            RebuildDataPanel();
        }

        private void OnClick_Preview(GameObject source)
        {
            if (!TryGetRecordByScreenElementName(source.name, out var record))
            {
                return;
            }
            ActiveRecordInitialized = true;
            ActiveRecordId = GetRecordId(record);
            RebuildPreviewPanel(record);
        }

        private void OnClick_AddNew(GameObject source)
        {
            if (!TryGetRecordByScreenElementName(source.name, out var record))
            {
                return;
            }
            var newRecord = new AnimSplittingSettings_Item()
            {
                Guid = Guid.NewGuid(),
                ConfigName = record.ConfigName,
                SymbolName = JsonValueEmpty,
                FrameIndex = JsonValueEmpty,
                FillingStyle = FillingStyle.Stretch,
                IsActive = true,
                MiddlePart_X = DefaultMiddlePartX,
                MiddlePart_Width = DefaultMiddlePartWidth,
                DoFlipEverySecondIteration = false
            };
            var lastIndex = _dialogData.FindLastIndex(x => x.ConfigName == record.ConfigName);
            _dialogData.Insert(lastIndex + 1, newRecord);
            ActiveRecordInitialized = false;
            ActiveRecordId = default;
            RebuildDataPanel();
        }

        private void OnClick_RemoveRecord(GameObject source)
        {
            if (!TryGetRecordByScreenElementName(source.name, out var record))
            {
                return;
            }
            _dialogData.Remove(record);
            ActiveRecordInitialized = false;
            ActiveRecordId = default;
            RebuildDataPanel();
        }

        private void OnClick_MakeSmaller(GameObject source)
        {
            if (   !TryGetRecord(ActiveRecordId, out var record)
                || !DynamicBuildingsManager.ConfigMap.TryGetValue(record.ConfigName, out var config)
                || !DynamicBuildingsManager.ConfigToBuildingDefMap.TryGetValue(config, out var originalDef)
                || DesiredBuildingWidthToShow - 1 < originalDef.WidthInCells
                )
            {
                return;
            }
            DesiredBuildingWidthToShow--;
            RebuildPreviewPanel(record);
        }

        private void OnClick_MakeBigger(GameObject source)
        {
            if (   DesiredBuildingWidthToShow + 1 > MaxBuildingWidthToShow
                || !TryGetRecord(ActiveRecordId, out var record)
                )
            {
                return;
            }
            DesiredBuildingWidthToShow++;
            RebuildPreviewPanel(record);
        }

        private void OnClick_AddRemoveRecords(GameObject source)
        {
            var dARASS = new Dialog_AddRemoveAnimSlicingSettings(this, _modSettings);
            dARASS.CreateAndShow(null);
        }
    }
}