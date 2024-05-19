using PeterHan.PLib.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ExtendedBuildingWidth.DynamicAnimManager;

namespace ExtendedBuildingWidth
{
    public class Dialog_EditAnimSlicingSettings
    {
        private class AnimSplittingSettings_Item
        {
            public Guid Guid { get; set; }
            public string ConfigName { get; set; }
            public string SymbolName { get; set; }
            public string FrameIndex { get; set; }
            public bool IsActive { get; set; }
            public int MiddlePart_X { get; set; }
            public int MiddlePart_Width { get; set; }
            public FillingStyle FillingStyle { get; set; }
            public bool DoFlipEverySecondIteration { get; set; }
        }

        private PDialog _pDialog = null;
        private PPanel _dialogBody = null;
        //private PPanelWithClearableChildren _dialogBodyChild = null;
        private PPanel _dialogBodyChild = null;
        private KScreen _componentScreen = null;
        private PPanel _dynamicBuildingPreviewPanel = null;
        private readonly List<AnimSplittingSettings_Item> _dialogData = new List<AnimSplittingSettings_Item>();
        //private Dictionary<Guid, GameObject> _gameObjects = new Dictionary<Guid, GameObject>();
        private Dictionary<Guid, Dictionary<string, GameObject>> _gameObjects = new Dictionary<Guid, Dictionary<string, GameObject>>();
        private Dictionary<Guid, Dictionary<string, PComboBox<StringListOption>>> _comboBoxes = new Dictionary<Guid, Dictionary<string, PComboBox<StringListOption>>>();
        private readonly ModSettings _modSettings;

        const string DialogId = "EditAnimSlicingSettings";
        const string DialogTitle = "Edit Anim Slicing Settings";
        const string DialogBodyGridPanelId = "EditAnimSlicingSettingsDialogBody";

        const string MiddlePartAlias = "Middle Part";
        const string KAnimSpriteAlias = "KAnim Sprite";
        string ExpansionStyleCombobox_Tooltip = $"Stretch: {MiddlePartAlias} will be drawn stretched between left and right parts."
                        + Environment.NewLine + $"Repeat: {MiddlePartAlias} will be drawn repeatedly starting from the left part, until the right part is reached.";
        string DoFlipEverySecondIteration_Tooltip = $"When 'Repeat' filling method is chosen: {MiddlePartAlias} will be flipped horizontally every second time it is drawn.";
        string IsActive_Tooltip = "Checked: dynamic buildings of this config will be drawn according to these settings."
          + Environment.NewLine + "Unchecked: dynamic buildings of this config will be drawn old style (fully stretched from left to right).";
        string MiddlePartX_Tooltip = $"X position (in pixels) of the original {KAnimSpriteAlias} that defines {MiddlePartAlias}.";
        string MiddlePartWidth_Tooltip = $"Width (in pixels) of the original {KAnimSpriteAlias} that defines {MiddlePartAlias}.";

        const string DialogOption_Ok = "ok";
        const string DialogOption_Cancel = "cancel";
        const int SpacingInPixels = 7;
        const string SymbolEmptyOptionText = " ";
        const string FrameEmptyOptionText = " ";

        private List<StringListOption> _fillingStyle_Options = new List<StringListOption>()
        {
            new StringListOption("Stretch"),
            new StringListOption("Repeat")
        };
        private Dictionary<string, List<StringListOption>> _configToSymbolMap;
        private Dictionary<string, Dictionary<string, List<StringListOption>>> _configToSymbolToFramesMap;

        const int MinBuildingWidthToShow = 3;
        const int MaxBuildingWidthToShow = 8;

        public bool ShowTechName { get; set; } = false;
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
                Size = new Vector2 { x = 1000, y = 700 },
                MaxSize = new Vector2 { x = 1000, y = 700 },
                SortKey = 200.0f
            }.AddButton(DialogOption_Ok, "OK", null, PUITuning.Colors.ButtonPinkStyle)
            .AddButton(DialogOption_Cancel, "CANCEL", null, PUITuning.Colors.ButtonBlueStyle);

            GenerateInitialData();

            _pDialog = dialog;
            _dialogBody = dialog.Body;

            RebuildBodyAndShow(showFirstTime: true);
        }

        private PScrollPane _recordsPane;
        private PPanel _controlPanel;
        
        internal void RebuildBodyAndShow(bool showFirstTime = false)
        {
            if (!showFirstTime)
            {
                _componentScreen.Deactivate();
            }

            ClearContents();

            _gameObjects.Clear();
            _comboBoxes.Clear();

            _recordsPane = GenerateRecordsPanel();
            _controlPanel = GenerateControlPanel();
            _dialogBodyChild.AddChild(_recordsPane);
            _dialogBodyChild.AddChild(_controlPanel);

            if (   ActiveRecordInitialized
                && TryGetRecord(ActiveRecordId, out var settings_Item)
                && !string.IsNullOrEmpty(settings_Item.ConfigName)
                && DynamicBuildingsManager.ConfigMap.ContainsKey(settings_Item.ConfigName)
                )
            {
                _dynamicBuildingPreviewPanel = GenerateDynamicBuildingPreviewPanel(settings_Item);
                _dialogBodyChild.AddChild(_dynamicBuildingPreviewPanel);
            }

            _dialogBody.AddChild(_dialogBodyChild);

            _componentScreen = null;
            var upperGo = _pDialog.Build();
            var isBuilt = upperGo.TryGetComponent<KScreen>(out _componentScreen);
            if (isBuilt)
            {
                _componentScreen.Activate();
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
                if (entry.Item2)
                {
                    if (_dialogData.Any(x => x.ConfigName == entry.Item1))
                    {
                        throw new ArgumentException($"ExtendedBuildingWidth ERROR - configName '{entry.Item1}' already exists in the dict");
                    }

                    var newRec = new AnimSplittingSettings_Item()
                    {
                        Guid = Guid.NewGuid(),
                        ConfigName = entry.Item1,
                        SymbolName = string.Empty,
                        FrameIndex = string.Empty,
                        IsActive = true,
                        MiddlePart_X = 130,
                        MiddlePart_Width = 50,
                        FillingStyle = FillingStyle.Stretch,
                        DoFlipEverySecondIteration = false
                    };
                    _dialogData.Add(newRec);
                }
                else
                {
                    _dialogData.RemoveAll(x => x.ConfigName == entry.Item1);
                }
            }
        }

        private void ClearContents()
        {
            if (_dialogBodyChild != null)
            {
                _dialogBody.RemoveChild(_dialogBodyChild);
            }
            _dialogBodyChild = new PPanel("DialogBodyChild"); // PPanelWithClearableChildren
        }

        private void GenerateTitles()
        {
            var tableTitlesPanel = new PGridPanel(DialogBodyGridPanelId) { Margin = new RectOffset(10, 40, 10, 0) };
            tableTitlesPanel.AddColumn(new GridColumnSpec(240));
            tableTitlesPanel.AddColumn(new GridColumnSpec(80));
            tableTitlesPanel.AddColumn(new GridColumnSpec(60));
            tableTitlesPanel.AddColumn(new GridColumnSpec(100));
            tableTitlesPanel.AddColumn(new GridColumnSpec(60));
            tableTitlesPanel.AddColumn(new GridColumnSpec(80));
            tableTitlesPanel.AddColumn(new GridColumnSpec(80));
            tableTitlesPanel.AddColumn(new GridColumnSpec(80));
            tableTitlesPanel.AddColumn(new GridColumnSpec(80));
            tableTitlesPanel.AddColumn(new GridColumnSpec(80));
            tableTitlesPanel.AddRow(new GridRowSpec());
            tableTitlesPanel.AddRow(new GridRowSpec());

            int iRow = 0;
            int iCol = -1;
            tableTitlesPanel.AddChild(new PLabel() { Text = "Config Name" }, new GridComponentSpec(iRow, ++iCol));
            tableTitlesPanel.AddChild(new PLabel() { Text = "preview" }, new GridComponentSpec(iRow, ++iCol));
            tableTitlesPanel.AddChild(new PLabel() { Text = "Add" }, new GridComponentSpec(iRow, ++iCol));
            tableTitlesPanel.AddChild(new PLabel() { Text = "Symbol" }, new GridComponentSpec(iRow, ++iCol));
            tableTitlesPanel.AddChild(new PLabel() { Text = "Frame No." }, new GridComponentSpec(iRow, ++iCol));
            tableTitlesPanel.AddChild(new PLabel() { Text = "Enabled", ToolTip = IsActive_Tooltip }, new GridComponentSpec(iRow, ++iCol));
            tableTitlesPanel.AddChild(new PLabel() { Text = $"{MiddlePartAlias}", ToolTip = ExpansionStyleCombobox_Tooltip }, new GridComponentSpec(iRow, ++iCol));
            tableTitlesPanel.AddChild(new PLabel() { Text = "Filling Method", ToolTip = ExpansionStyleCombobox_Tooltip }, new GridComponentSpec(iRow + 1, iCol));
            tableTitlesPanel.AddChild(new PLabel() { Text = $"{MiddlePartAlias}", ToolTip = MiddlePartX_Tooltip }, new GridComponentSpec(iRow, ++iCol));
            tableTitlesPanel.AddChild(new PLabel() { Text = "Position X", ToolTip = MiddlePartX_Tooltip }, new GridComponentSpec(iRow + 1, iCol));
            tableTitlesPanel.AddChild(new PLabel() { Text = $"{MiddlePartAlias}", ToolTip = MiddlePartWidth_Tooltip }, new GridComponentSpec(iRow, ++iCol));
            tableTitlesPanel.AddChild(new PLabel() { Text = "Width", ToolTip = MiddlePartWidth_Tooltip }, new GridComponentSpec(iRow + 1, iCol));
            tableTitlesPanel.AddChild(new PLabel() { Text = "Flip Every", ToolTip = DoFlipEverySecondIteration_Tooltip }, new GridComponentSpec(iRow, ++iCol));
            tableTitlesPanel.AddChild(new PLabel() { Text = "Second Time", ToolTip = DoFlipEverySecondIteration_Tooltip }, new GridComponentSpec(iRow + 1, iCol));
            _dialogBodyChild.AddChild(tableTitlesPanel);
        }

        private void FillRecordsPanel_Block(
                PGridPanel gridPanel,
                int iRow,
                AnimSplittingSettings_Item entry,
                Dictionary<string, BuildingDescription> allBuildingsDict
            )
        {
            var screenElementName = GenerateScreenElementName(entry);
            var configName = entry.ConfigName;

            int iCol = -1;
            if (iRow == 0)
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
                var text = ShowTechName ? configName : configCaption;
                var tooltip = !ShowTechName ? configName : configCaption;

                gridPanel.AddChild(new PLabel(screenElementName) { Text = text, ToolTip = tooltip }, new GridComponentSpec(iRow, ++iCol) { Alignment = TextAnchor.MiddleLeft });
            }
            else
            {
                ++iCol;
            }

            var prvB = new PButton(screenElementName) { OnClick = OnClick_ConfigName, Text = "preview" };
            gridPanel.AddChild(prvB, new GridComponentSpec(iRow, ++iCol));

            var addS = new PButton(screenElementName) { OnClick = OnClick_AddNew, Text = "Add" };
            gridPanel.AddChild(addS, new GridComponentSpec(iRow, ++iCol));

            //var smN = new PTextField(screenElementName)
            //{
            //    Text = entry.SymbolName,
            //    MinWidth = 80,
            //    OnTextChanged = OnTextChanged_SymbolName
            //};
            //gridPanel.AddChild(smN, new GridComponentSpec(iRow, ++iCol));

            var symbolDictKey = GetSymbolDictKeyBySymbolName(entry.SymbolName);
            var symbolIndex = _configToSymbolMap[configName].FindIndex(x => x.ToString() == symbolDictKey);
            var symbol_options = _configToSymbolMap[configName];
            var symbol_option = symbol_options[symbolIndex];
            var smN = new PComboBox<StringListOption>(screenElementName)
            {
                Content = _configToSymbolMap[configName],
                InitialItem = _configToSymbolMap[configName][symbolIndex],
                OnOptionSelected = OnOptionSelected_Symbols,
                MinWidth = 80
            };
            gridPanel.AddChild(smN, new GridComponentSpec(iRow, ++iCol));

            //var frI = new PTextField(screenElementName)
            //{
            //    Text = entry.FrameIndex,
            //    MinWidth = 35,
            //    OnTextChanged = OnTextChanged_FrameIndex
            //};
            //gridPanel.AddChild(frI, new GridComponentSpec(iRow, ++iCol));

            if (!int.TryParse(entry.FrameIndex, out var frameIndex))
            {
                frameIndex = -1;
            }

            ++iCol;
            foreach (var symbolToFramesDict in _configToSymbolToFramesMap[configName])
            {
                var symbolDictKey_iteration = symbolToFramesDict.Key;
                var frames = symbolToFramesDict.Value;

                var frameDictKey = (symbolToFramesDict.Key == symbolDictKey) ? frameIndex + 1 : 0;
                var initialItem = _configToSymbolToFramesMap[configName][symbolDictKey][frameDictKey];
                var frI = new PComboBox<StringListOption>(screenElementName + "/" + symbolDictKey_iteration)
                {
                    Content = frames,
                    InitialItem = initialItem,
                    OnOptionSelected = OnOptionSelected_Frames,
                    MinWidth = 35
                };
                frI.OnRealize += OnRealize_ComboBox;
                gridPanel.AddChild(frI, new GridComponentSpec(iRow, iCol));

                Dictionary<string, PComboBox<StringListOption>> subDict;
                if (!_comboBoxes.TryGetValue(entry.Guid, out subDict))
                {
                    subDict = new Dictionary<string, PComboBox<StringListOption>>();
                    _comboBoxes.Add(entry.Guid, subDict);
                }
                subDict.Add(symbolToFramesDict.Key, frI);
            }

            var iA = new PCheckBox(screenElementName)
            {
                InitialState = entry.IsActive ? 1 : 0,
                OnChecked = OnChecked_IsActive
            };
            gridPanel.AddChild(iA, new GridComponentSpec(iRow, ++iCol));

            var eSt = new PComboBox<StringListOption>(screenElementName)
            {
                Content = _fillingStyle_Options,
                InitialItem = _fillingStyle_Options[(int)entry.FillingStyle],
                OnOptionSelected = OnOptionSelected_FillingStyle,
                MinWidth = 60
            };
            gridPanel.AddChild(eSt, new GridComponentSpec(iRow, ++iCol));

            var mfX = new PTextField(screenElementName)
            {
                Text = entry.MiddlePart_X.ToString(),
                OnTextChanged = OnTextChanged_TemplatePart_X,
                MinWidth = 60
            };
            gridPanel.AddChild(mfX, new GridComponentSpec(iRow, ++iCol));

            var mfW = new PTextField(screenElementName)
            {
                Text = entry.MiddlePart_Width.ToString(),
                OnTextChanged = OnTextChanged_TemplatePart_Width,
                MinWidth = 60
            };
            gridPanel.AddChild(mfW, new GridComponentSpec(iRow, ++iCol));

            var df = new PCheckBox(screenElementName)
            {
                InitialState = entry.DoFlipEverySecondIteration ? 1 : 0,
                OnChecked = OnChecked_DoFlipEverySecondIteration
            };
            gridPanel.AddChild(df, new GridComponentSpec(iRow, ++iCol));
        }

        private void PrepareDropdowns(Dictionary<string, List<AnimSplittingSettings_Item>> dialogData_GroupedByConfigName)
        {
#if DEBUG
            Debug.Log("--- PrepareDropdowns");
#endif
            _configToSymbolMap = new Dictionary<string, List<StringListOption>>();
            _configToSymbolToFramesMap = new Dictionary<string, Dictionary<string, List<StringListOption>>>();
            foreach (var kvp in dialogData_GroupedByConfigName)
            {
                var configName = kvp.Key;
                var config = DynamicBuildingsManager.ConfigMap[configName];
                var originalDef = DynamicBuildingsManager.ConfigToBuildingDefMap[config];

                var availableSymsols = new List<StringListOption>() { new StringListOption(SymbolEmptyOptionText) };
                var framesForSymbols = new Dictionary<string, List<StringListOption>>();
                var availableFrames0 = new List<StringListOption>() { new StringListOption(FrameEmptyOptionText) };
                framesForSymbols.Add(SymbolEmptyOptionText, availableFrames0);
                foreach (var symbol in originalDef.AnimFiles[0].GetData().build.symbols)
                {
                    var symbolName = symbol.hash.ToString();
                    availableSymsols.Add(new StringListOption(symbolName));

                    var availableFrames = new List<StringListOption>() { new StringListOption(FrameEmptyOptionText) };
                    for (var frameIdx = 0; frameIdx < symbol.numFrames; frameIdx++)
                    {
                        availableFrames.Add(new StringListOption(frameIdx.ToString()));
                    }
                    framesForSymbols.Add(symbolName, availableFrames);
                }
                _configToSymbolMap.Add(configName, availableSymsols);
                _configToSymbolToFramesMap.Add(configName, framesForSymbols);
            }
#if DEBUG
            Debug.Log($"--- _symbolsForConfigs:");
            foreach (var c in _symbolsForConfigs)
            {
                Debug.Log($"config: '{c.Key}'");
                foreach (var s in c.Value)
                {
                    Debug.Log($"symbol: '{s}'");
                }
            }
            Debug.Log($"--- _framesForSymbolsForConfigs:");
            foreach (var c in _framesForSymbolsForConfigs)
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
        }

        private PScrollPane GenerateRecordsPanel()
        {
#if DEBUG
            Debug.Log("--- GenerateRecordsPanel");
#endif
            GenerateTitles();

            var dialogData_GroupedByConfigName = _dialogData.GroupBy(r => r.ConfigName).ToDictionary(t => t.Key, y => y.Select(r => r).ToList());

            PrepareDropdowns(dialogData_GroupedByConfigName);

            var gridPanelConfigs = new PGridPanel("DialogBodyGridPanelBlocks") { Margin = new RectOffset(10, 40, 10, 10) };
            gridPanelConfigs.AddColumn(new GridColumnSpec());
            for (var i = 0; i < dialogData_GroupedByConfigName.Keys.Count; i++)
            {
                gridPanelConfigs.AddRow(new GridRowSpec());
            }

            var allBuildingsDict = SettingsManager.ListOfAllBuildings.ToDictionary(x => x.ConfigName, y => y);

            var iRowOuter = -1;
            foreach (var kvp in dialogData_GroupedByConfigName)
            {
                ++iRowOuter;
                var gridPanel = new PGridPanel(DialogBodyGridPanelId);
                gridPanel.AddColumn(new GridColumnSpec(240));
                gridPanel.AddColumn(new GridColumnSpec(80));
                gridPanel.AddColumn(new GridColumnSpec(60));
                gridPanel.AddColumn(new GridColumnSpec(100));
                gridPanel.AddColumn(new GridColumnSpec(60));
                gridPanel.AddColumn(new GridColumnSpec(80));
                gridPanel.AddColumn(new GridColumnSpec(80));
                gridPanel.AddColumn(new GridColumnSpec(80));
                gridPanel.AddColumn(new GridColumnSpec(80));
                gridPanel.AddColumn(new GridColumnSpec(80));
                for (var i = 0; i < kvp.Value.Count; i++)
                {
                    gridPanel.AddRow(new GridRowSpec());
                }
                gridPanelConfigs.AddChild(gridPanel, new GridComponentSpec(iRowOuter, 0));

                int iRow = -1;
                foreach (var entry in kvp.Value)
                {
                    ++iRow;
                    FillRecordsPanel_Block(gridPanel, iRow, entry, allBuildingsDict);
                }
            }

            var scrollBody = new PPanel("ScrollContent")
            {
                Spacing = 10,
                Direction = PanelDirection.Vertical,
                Alignment = TextAnchor.UpperCenter,
                FlexSize = Vector2.right
            };
            scrollBody.AddChild(gridPanelConfigs);

            var scrollPane = new PScrollPane()
            {
                ScrollHorizontal = false,
                ScrollVertical = true,
                Child = scrollBody,
                FlexSize = Vector2.right,
                TrackSize = 20,
                AlwaysShowHorizontal = false,
                AlwaysShowVertical = true
            };

            return scrollPane;
        }

        private static AnimSplittingSettings_Internal MapToInternal(AnimSplittingSettings_Item entry)
        {
            var symbolName = !string.IsNullOrEmpty(entry.SymbolName) ? entry.SymbolName : "bridge";
            var frameIndex = !string.IsNullOrEmpty(entry.FrameIndex) ? int.Parse(entry.FrameIndex) : 0;

            var newEntry = new AnimSplittingSettings_Internal
            {
                ConfigName = entry.ConfigName,
                SymbolName = new KAnimHashedString(symbolName),
                FrameIndex = frameIndex,
                IsActive = entry.IsActive,
                MiddlePart_X = entry.MiddlePart_X,
                MiddlePart_Width = entry.MiddlePart_Width,
                FillingStyle = entry.FillingStyle,
                DoFlipEverySecondIteration = entry.DoFlipEverySecondIteration
            };
            return newEntry;
        }

        private PPanel GenerateDynamicBuildingPreviewPanel(AnimSplittingSettings_Item settings_Item)
        {
#if DEBUG
            Debug.Log("--- GenerateDynamicBuildingPreviewPanel");
#endif
            var settings_Internal = MapToInternal(settings_Item);
            var configName = settings_Internal.ConfigName;
            var dynamicBuildingPreviewPanel = new PPanel("DynamicBuildingPreviewPanel") { Direction = PanelDirection.Vertical };
            var basicInfo = GenerateBasicInfoPanel(settings_Internal);
            dynamicBuildingPreviewPanel.AddChild(basicInfo);
            var sprites = GenerateSpritesToShow(settings_Internal, DesiredBuildingWidthToShow);
            var panelWithCaptions = GenerateCaptionsForSprites(sprites);
            dynamicBuildingPreviewPanel.AddChild(panelWithCaptions);
            var panelWithSprites = GenerateScreenComponentsForSprites(sprites, settings_Internal.DoFlipEverySecondIteration);
            dynamicBuildingPreviewPanel.AddChild(panelWithSprites);
            return dynamicBuildingPreviewPanel;
        }

        private PPanel GenerateBasicInfoPanel(AnimSplittingSettings_Internal settings_Internal)
        {
#if DEBUG
            Debug.Log("--- GenerateBasicInfoPanel");
#endif
            var result = new PPanel()
            {
                Direction = PanelDirection.Horizontal,
                Margin = new RectOffset(0, 0, 10, 10)
            };

            var configName = settings_Internal.ConfigName;

            var config = DynamicBuildingsManager.ConfigMap[configName];
            var originalDef = DynamicBuildingsManager.ConfigToBuildingDefMap[config];
            var symbol = originalDef.AnimFiles.First().GetData().build.GetSymbol(settings_Internal.SymbolName);
            if (symbol == null)
            {
                Debug.Log($"ExtendedBuildingWidth WARNING - GenerateBasicInfoPanel: symbol '{settings_Internal.SymbolName}' not found for config '{configName}'");
                return result;
            }

            var symbolFrame = symbol.GetFrame(settings_Internal.FrameIndex);
            if (symbolFrame.symbolIdx == -1)
            {
                Debug.Log($"ExtendedBuildingWidth WARNING - GenerateBasicInfoPanel: symbolFrame '{settings_Internal.FrameIndex}' not found for config '{configName}' and symbol '{settings_Internal.SymbolName}'");
                return result;
            }

            var texture = originalDef.AnimFiles.First().GetData().build.GetTexture(0);

            var margin = new RectOffset(10, 10, 0, 0);
            var origWidth = ((int)((symbolFrame.uvMax.x - symbolFrame.uvMin.x) * texture.width)).ToString();

            result.AddChild(new PLabel() { Text = $"Original width: {origWidth}", Margin = margin });
            result.AddChild(new PLabel() { Text = $"Dynamic building size: {DesiredBuildingWidthToShow}", Margin = margin });

            result.AddChild(new PButton() { Text = "<", Margin = margin, OnClick = OnClick_MakeSmaller });
            result.AddChild(new PButton() { Text = ">", Margin = margin, OnClick = OnClick_MakeBigger });

            return result;
        }

        private List<Sprite> GenerateSpritesToShow(AnimSplittingSettings_Internal settings_Internal, int desiredBuildingWidthToShow)
        {
            var sprites = new List<Sprite>();
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
                return sprites;
            }
#if DEBUG
            Debug.Log($"animSlicingSettingsItem.FrameIndex='{settings_Internal.FrameIndex}'");
#endif
            var frameIdx = symbol.GetFrameIdx(settings_Internal.FrameIndex);
#if DEBUG
            Debug.Log($"frameIdx='{frameIdx}'");
#endif
            var symbolFrame = symbol.GetFrame(settings_Internal.FrameIndex);
            if (symbolFrame.symbolIdx == -1)
            {
                return sprites;
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
            foreach (var frame in smallerFrames)
            {
                var sprite = DynamicAnimManager.GenerateSpriteForFrame(frame, texture);
                sprites.Add(sprite);
            }

            return sprites;
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
                //Debug.Log($"RectW = {sprites[spriteIdx].rect.width}, W = {outputWidth}, H = {outputHeight}, PPU = {sprites[spriteIdx].pixelsPerUnit}");
                gridPanelWithSprites.AddChild(ppanel, new GridComponentSpec(0, columnIdx));
            }

            return panelWithSprites;
        }

        private PPanel GenerateControlPanel()
        {
#if DEBUG
            Debug.Log("--- GenerateControlPanel");
#endif
            var controlPanel = new PPanel("ControlPanel")
            {
                Direction = PanelDirection.Horizontal,
                Spacing = SpacingInPixels,
                Margin = new RectOffset(10, 10, 10, 10)
            };
            var cbShowTechName = new PCheckBox();
            cbShowTechName.InitialState = ShowTechName ? 1 : 0;
            cbShowTechName.Text = "Show tech names";
            cbShowTechName.OnChecked = OnChecked_ShowTechName;
            controlPanel.AddChild(cbShowTechName);

            var btnAdd = new PButton();
            btnAdd.Text = "Add or remove records";
            btnAdd.OnClick = OnClick_AddRemoveRecords;
            controlPanel.AddChild(btnAdd);

            return controlPanel;
        }

        private List<AnimSplittingSettings> MapDialogToSource(List<AnimSplittingSettings_Item> dialogData)
        {
            var result = new List<AnimSplittingSettings>();
            foreach (var entry in dialogData)
            {
                var sourceRec = new AnimSplittingSettings()
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
                result.Add(sourceRec);
            };
            return result;
        }

        private List<AnimSplittingSettings_Item> MapSourceToDialog(List<AnimSplittingSettings> sourceData)
        {
            var result = new List<AnimSplittingSettings_Item>();
            foreach (var entry in sourceData)
            {
                var newRec = new AnimSplittingSettings_Item()
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
                result.Add(newRec);
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

            var dialogData = MapSourceToDialog(sourceData);
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

        private void OnDialogClosed(string option)
        {
            if (option != DialogOption_Ok)
            {
                return;
            }
            var newRez = MapDialogToSource(_dialogData);
            _modSettings.SetAnimSplittingSettings(newRez);
        }

        private void OnTextChanged_SymbolName(GameObject source, string text)
        {
            if (!TryGetRecordByScreenElementName(source.name, out var record))
            {
                return;
            }
            record.SymbolName = text;
        }

        private void OnTextChanged_FrameIndex(GameObject source, string text)
        {
            if (!TryGetRecordByScreenElementName(source.name, out var record))
            {
                return;
            }
            if (!int.TryParse(text, out var parsed) && !string.IsNullOrEmpty(text))
            {
                return;
            }
            record.FrameIndex = text;
        }

        private void OnTextChanged_TemplatePart_X(GameObject source, string text)
        {
            if (!TryGetRecordByScreenElementName(source.name, out var record))
            {
                return;
            }
            if (!int.TryParse(text, out var parsed))
            {
                return;
            }
            record.MiddlePart_X = parsed;
        }

        private void OnTextChanged_TemplatePart_Width(GameObject source, string text)
        {
            if (!TryGetRecordByScreenElementName(source.name, out var record))
            {
                return;
            }
            if (!int.TryParse(text, out var parsed))
            {
                return;
            }
            record.MiddlePart_Width = parsed;
        }

        private void OnOptionSelected_FillingStyle(GameObject source, StringListOption option)
        {
            if (!TryGetRecordByScreenElementName(source.name, out var record))
            {
                return;
            }
            int index = _fillingStyle_Options.IndexOf(option);
            if (index < 0 || index >= _fillingStyle_Options.Count)
            {
                return;
            }
            record.FillingStyle = (FillingStyle)index;
        }

        private void OnOptionSelected_Symbols(GameObject source, StringListOption option)
        {
            if (!TryGetRecordByScreenElementName(source.name, out var record))
            {
                return;
            }
            int index = _configToSymbolMap[record.ConfigName].IndexOf(option);
            if (index < 0 || index >= _configToSymbolMap[record.ConfigName].Count)
            {
                return;
            }
            var symbolDictKey = _configToSymbolMap[record.ConfigName][index].ToString();
            record.SymbolName = index > 0 ? symbolDictKey : "";
            record.FrameIndex = "";

            foreach (var xx in _gameObjects[record.Guid])
            {
                var go = xx.Value;
                go.SetActive(symbolDictKey == xx.Key);
                if (symbolDictKey == xx.Key)
                {
                    var opt = new StringListOption(FrameEmptyOptionText);
                    PComboBox<StringListOption>.SetSelectedItem(go, opt, false);
                }
            }
        }

        private void OnOptionSelected_Frames(GameObject source, StringListOption option)
        {
            //if (!TryGetRecordByScreenElementName(source.name, out var record))
            //{
            //    return;
            //}
            //var recordGuid = record.Guid;
            var split = source.name.Split('/');
            var recordGuid = Guid.Parse(split[0]);
            var symbolDictKey0 = split[1];
            if (!TryGetRecord(recordGuid, out var record))
            {
                return;
            }

            var symbolDictKey = GetSymbolDictKeyBySymbolName(record.SymbolName);
            var frameDictKey = !string.IsNullOrEmpty(record.FrameIndex) ? record.FrameIndex : FrameEmptyOptionText;
            int index = _configToSymbolToFramesMap[record.ConfigName][symbolDictKey].IndexOf(option);
            if (index < 0 || index >= _configToSymbolToFramesMap[record.ConfigName][symbolDictKey].Count)
            {
                return;
            }
            record.FrameIndex = index > 0 ? (index-1).ToString() : "";
        }

        private string GetSymbolDictKeyBySymbolName(string symbolName)
        {
            return !string.IsNullOrEmpty(symbolName) ? symbolName : SymbolEmptyOptionText;
        }

        private void OnRealize_ComboBox(GameObject source)
        {
            //if (!TryGetRecordByScreenElementName(source.name, out var record))
            //{
            //    return;
            //}
            //var recordGuid = record.Guid;
            //string symbolDictKey ????????????;
            var split = source.name.Split('/');
            var recordGuid = Guid.Parse(split[0]);
            var symbolDictKey = split[1];

            Dictionary<string, GameObject> subDict;
            if (!_gameObjects.TryGetValue(recordGuid, out subDict))
            {
                subDict = new Dictionary<string, GameObject>();
                _gameObjects.Add(recordGuid, subDict);
            }
            subDict.Add(symbolDictKey, source);

            bool activeFlag = false;
            if (TryGetRecord(recordGuid, out var record))
            {
                activeFlag =   recordGuid == record.Guid
                            && symbolDictKey == GetSymbolDictKeyBySymbolName(record.SymbolName);
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
            record.IsActive = (newState == 1);
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
            record.DoFlipEverySecondIteration = (newState == 1);
        }

        private void OnChecked_ShowTechName(GameObject source, int state)
        {
            int newState = (state + 1) % 2;
            PCheckBox.SetCheckState(source, newState);
            ShowTechName = (newState == 1);
        }
        
        private void OnClick_ConfigName(GameObject source)
        {
            if (!TryGetRecordByScreenElementName(source.name, out var record))
            {
                return;
            }
            ActiveRecordInitialized = true;
            ActiveRecordId = GetRecordId(record);
            RebuildBodyAndShow();
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
                SymbolName = "",
                FrameIndex = "",
                FillingStyle = FillingStyle.Stretch,
                IsActive = true,
                MiddlePart_X = 130,
                MiddlePart_Width = 50,
                DoFlipEverySecondIteration = false
            };
            _dialogData.Add(newRecord);
            ActiveRecordInitialized = false;
            ActiveRecordId = default;
            RebuildBodyAndShow();
        }

        private void OnClick_MakeSmaller(GameObject source)
        {
            if (DesiredBuildingWidthToShow - 1 < MinBuildingWidthToShow)
            {
                return;
            }
            DesiredBuildingWidthToShow--;
            RebuildBodyAndShow();
        }

        private void OnClick_MakeBigger(GameObject source)
        {
            if (DesiredBuildingWidthToShow + 1 > MaxBuildingWidthToShow)
            {
                return;
            }
            DesiredBuildingWidthToShow++;
            RebuildBodyAndShow();
        }

        private void OnClick_AddRemoveRecords(GameObject source)
        {
            var dARASS = new Dialog_AddRemoveAnimSlicingSettings(this, _modSettings);
            dARASS.CreateAndShow(null);
        }
    }
}