using PeterHan.PLib.Core;
using PeterHan.PLib.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static STRINGS.UI.UISIDESCREENS;

namespace ExtendedBuildingWidth
{
    public class Dialog_EditAnimSlicingSettings
    {
        private class EditAnimSlicingSettings_Item
        {
            public string TechName { get; set; }
            public bool IsActive { get; set; }
            public int MiddlePart_X { get; set; }
            public int MiddlePart_Width { get; set; }
            public FillingMethod FillingMethod { get; set; }
            public bool DoFlipEverySecondTime { get; set; }
        }

        /// <summary>
        /// Clone of PPanel with ability to flip its image by X
        /// </summary>
        private class PPanelWithFlippableImage : PPanel
        {
            public bool FlipByX { get; set; }

            private GameObject Build(Vector2 size, bool dynamic, bool flipByX)
            {
                GameObject gameObject = PUIElements.CreateUI(null, base.Name);

                SetImage(gameObject);

                var imageChild = gameObject.GetComponent<UnityEngine.UI.Image>();

                if (flipByX)
                {
                    var transform = imageChild.rectTransform();
                    var scale = Vector3.one;
                    scale.x = -1.0f;
                    transform.localScale = scale;
                    //float rot = 0.0f;
                    //transform.Rotate(new Vector3(0.0f, 0.0f, rot));
                }

                foreach (IUIComponent child in children)
                {
                    GameObject gameObject2 = child.Build();
                    gameObject2.SetParent(gameObject);
                    PUIElements.SetAnchors(gameObject2, PUIAnchoring.Stretch, PUIAnchoring.Stretch);
                }

                BoxLayoutGroup boxLayoutGroup = gameObject.AddComponent<BoxLayoutGroup>();
                boxLayoutGroup.Params = new BoxLayoutParams
                {
                    Direction = Direction,
                    Alignment = Alignment,
                    Spacing = Spacing,
                    Margin = base.Margin
                };
                if (!dynamic)
                {
                    boxLayoutGroup.LockLayout();
                    gameObject.SetMinUISize(size);
                }

                boxLayoutGroup.flexibleWidth = base.FlexSize.x;
                boxLayoutGroup.flexibleHeight = base.FlexSize.y;
                InvokeRealize(gameObject);
                return gameObject;
            }

            public override GameObject Build()
            {
                return Build(default(Vector2), DynamicSize, FlipByX);
            }
        }

        private readonly List<EditAnimSlicingSettings_Item> _dialogData = new List<EditAnimSlicingSettings_Item>();

        private PPanel _dialogBodyChild = null;
        private PPanel _animSlicingDialogBody = null;
        private KScreen _componentScreenEditConfig = null;
        private PDialog _animSlicing_PDialog = null;
        private PPanel _dynamicBuildingPreviewPanel = null;

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

        private readonly ModSettings _modSettings;

        private List<StringListOption> groups = new List<StringListOption>()
        {
            new StringListOption("Stretch"),
            new StringListOption("Repeat")
        };

        public bool ShowTechName { get; set; } = false;
        public string CurrentConfigName { get; set; } = string.Empty;

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
                Size = new Vector2 { x = 800, y = 600 },
                MaxSize = new Vector2 { x = 800, y = 600 },
                SortKey = 200.0f
            }.AddButton(DialogOption_Ok, "OK", null, PUITuning.Colors.ButtonPinkStyle)
            .AddButton(DialogOption_Cancel, "CANCEL", null, PUITuning.Colors.ButtonBlueStyle);

            GenerateInitialData();

            _animSlicing_PDialog = dialog;
            _animSlicingDialogBody = dialog.Body;

            RebuildBodyAndShow(showFirstTime: true);
        }

        internal void RebuildBodyAndShow(bool showFirstTime = false)
        {
            if (!showFirstTime)
            {
                _componentScreenEditConfig.Deactivate();
            }

            ClearContents();
            GenerateRecordsPanel();
            GenerateControlPanel();
            GenerateDynamicBuildingPreviewPanel();

            _animSlicingDialogBody.AddChild(_dialogBodyChild);

            _componentScreenEditConfig = null;
            var isBuilt = _animSlicing_PDialog.Build().TryGetComponent<KScreen>(out _componentScreenEditConfig);
            if (isBuilt)
            {
                _componentScreenEditConfig.Activate();
            }
        }

        public List<string> GetTechNames()
        {
            return _dialogData.Select(d => d.TechName).ToList();
        }

        //public void ApplyChanges(ICollection<Tuple<string, bool>> modifiedRecords)
        //{
        //    foreach (var entry in modifiedRecords)
        //    {
        //        var existingRecord = _dialogData.FirstOrDefault(x => x.TechName == entry.first);

        //        if (!entry.second)
        //        {
        //            if (existingRecord != null)
        //            {
        //                _dialogData.Remove(existingRecord);
        //            }
        //        }
        //        else if (entry.second)
        //        {
        //            if (existingRecord == null)
        //            {
        //                var newRec = new EditAnimSlicingSettings_Item()
        //                {
        //                    TechName = entry.first,
        //                    MiddleFrame_X = 130,
        //                    MiddleFrame_Width = 50,
        //                    ExtensionStyle = ExtensionStyle.Stretch
        //                };
        //                _dialogData.Add(newRec);
        //            }
        //        }
        //    }
        //}

        private void ClearContents()
        {
            if (_dialogBodyChild != null)
            {
                _animSlicingDialogBody.RemoveChild(_dialogBodyChild);
            }
            _dialogBodyChild = new PPanel("DialogBodyChild");
        }

        private void GenerateRecordsPanel()
        {
            var tableTitlesPanel = new PGridPanel(DialogBodyGridPanelId) { Margin = new RectOffset(10, 40, 10, 0) };
            tableTitlesPanel.AddColumn(new GridColumnSpec(350));
            tableTitlesPanel.AddColumn(new GridColumnSpec(80));
            tableTitlesPanel.AddColumn(new GridColumnSpec(80));
            tableTitlesPanel.AddColumn(new GridColumnSpec(80));
            tableTitlesPanel.AddColumn(new GridColumnSpec(80));
            tableTitlesPanel.AddColumn(new GridColumnSpec(80));
            tableTitlesPanel.AddRow(new GridRowSpec());
            tableTitlesPanel.AddRow(new GridRowSpec());

            int iRow = 0;
            var lbConfigName = new PLabel();
            lbConfigName.Text = "Config Name";
            tableTitlesPanel.AddChild(lbConfigName, new GridComponentSpec(iRow, 0));
            var lbIsActive = new PLabel();
            lbIsActive.Text = "Enabled";
            tableTitlesPanel.AddChild(lbIsActive, new GridComponentSpec(iRow, 1));
            var lbStretchKoef = new PLabel();
            lbStretchKoef.Text = $"{MiddlePartAlias}";
            tableTitlesPanel.AddChild(lbStretchKoef, new GridComponentSpec(iRow, 2));
            var lbStretchKoef2 = new PLabel();
            lbStretchKoef2.Text = "Filling Method";
            tableTitlesPanel.AddChild(lbStretchKoef2, new GridComponentSpec(iRow + 1, 2));
            var lbMiddlePosX = new PLabel();
            lbMiddlePosX.Text = $"{MiddlePartAlias}";
            tableTitlesPanel.AddChild(lbMiddlePosX, new GridComponentSpec(iRow, 3));
            var lbMiddlePosX2 = new PLabel();
            lbMiddlePosX2.Text = "Position X";
            tableTitlesPanel.AddChild(lbMiddlePosX2, new GridComponentSpec(iRow + 1, 3));
            var lbMiddleWidth = new PLabel();
            lbMiddleWidth.Text = $"{MiddlePartAlias}";
            tableTitlesPanel.AddChild(lbMiddleWidth, new GridComponentSpec(iRow, 4));
            var lbMiddleWidth2 = new PLabel();
            lbMiddleWidth2.Text = "Width";
            tableTitlesPanel.AddChild(lbMiddleWidth2, new GridComponentSpec(iRow + 1, 4));
            var lbFlip2ndTime = new PLabel();
            lbFlip2ndTime.Text = "Flip Every";
            tableTitlesPanel.AddChild(lbFlip2ndTime, new GridComponentSpec(iRow, 5));
            var lbFlip2ndTime2 = new PLabel();
            lbFlip2ndTime2.Text = "Second Time";
            tableTitlesPanel.AddChild(lbFlip2ndTime2, new GridComponentSpec(iRow + 1, 5));

            _dialogBodyChild.AddChild(tableTitlesPanel);

            var gridPanel = new PGridPanel(DialogBodyGridPanelId) { Margin = new RectOffset(10, 40, 10, 10) };
            gridPanel.AddColumn(new GridColumnSpec(350));
            gridPanel.AddColumn(new GridColumnSpec(80));
            gridPanel.AddColumn(new GridColumnSpec(80));
            gridPanel.AddColumn(new GridColumnSpec(80));
            gridPanel.AddColumn(new GridColumnSpec(80));
            gridPanel.AddColumn(new GridColumnSpec(80));

            foreach (var entry in _dialogData)
            {
                gridPanel.AddRow(new GridRowSpec());
            }

            var dict = SettingsManager.ListOfAllBuildings.ToDictionary(x => x.TechName, y => y);

            iRow = -1;
            foreach (var entry in _dialogData)
            {
                iRow++;

                string configCaption = string.Empty;
                if (dict.TryGetValue(entry.TechName, out var buildingDescription))
                {
                    configCaption = buildingDescription.Caption;
                }
                if (string.IsNullOrEmpty(configCaption))
                {
                    configCaption = entry.TechName;
                }

                var bn = new PButton(entry.TechName);
                bn.OnClick = OnConfigNameButton_Click;
                bn.Color = new ColorStyleSetting()
                {
                    hoverColor = Color.clear,
                    inactiveColor = Color.clear
                };
                if (!ShowTechName)
                {
                    bn.Text = configCaption;
                    bn.ToolTip = entry.TechName;
                }
                else
                {
                    bn.Text = entry.TechName;
                    bn.ToolTip = configCaption;
                }
                gridPanel.AddChild(bn, new GridComponentSpec(iRow, 0) { Alignment = TextAnchor.MiddleLeft });

                var iA = new PCheckBox(entry.TechName);
                iA.InitialState = entry.IsActive ? 1 : 0;
                iA.ToolTip = IsActive_Tooltip;
                iA.OnChecked = OnChecked_IsActive;
                gridPanel.AddChild(iA, new GridComponentSpec(iRow, 1));

                var eSt = new PComboBox<StringListOption>(entry.TechName)
                {
                    Content = groups,
                    InitialItem = groups[(int)entry.FillingMethod],
                    OnOptionSelected = On_StringListOption_Selected,
                    ToolTip = ExpansionStyleCombobox_Tooltip
                };
                eSt.MinWidth = 60;
                gridPanel.AddChild(eSt, new GridComponentSpec(iRow, 2));

                var mfX = new PTextField(entry.TechName);
                mfX.Text = entry.MiddlePart_X.ToString();
                mfX.ToolTip = MiddlePartX_Tooltip;
                mfX.OnTextChanged += OnTextChanged_TemplatePart_X;
                mfX.MinWidth = 60;
                gridPanel.AddChild(mfX, new GridComponentSpec(iRow, 3));

                var mfW = new PTextField(entry.TechName);
                mfW.Text = entry.MiddlePart_Width.ToString();
                mfW.ToolTip = MiddlePartWidth_Tooltip;
                mfW.OnTextChanged += OnTextChanged_TemplatePart_Width;
                mfW.MinWidth = 60;
                gridPanel.AddChild(mfW, new GridComponentSpec(iRow, 4));

                var df = new PCheckBox(entry.TechName);
                df.InitialState = entry.DoFlipEverySecondTime ? 1 : 0;
                df.ToolTip = DoFlipEverySecondIteration_Tooltip;
                df.OnChecked = OnChecked_DoFlipEverySecondIteration;
                gridPanel.AddChild(df, new GridComponentSpec(iRow, 5));
            }

            var scrollBody = new PPanel("ScrollContent")
            {
                Spacing = 10,
                Direction = PanelDirection.Vertical,
                Alignment = TextAnchor.UpperCenter,
                FlexSize = Vector2.right
            };
            scrollBody.AddChild(gridPanel);

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

            _dialogBodyChild.AddChild(scrollPane);
        }

        const int DesiredBuildingWidthToShow = 7; // ToDo: make it as a slider?
        const int MaxBuildingWidthToShow = 8;
        const int SymbolToShowIndex = 0;
        const int SymbolFrameToShowIndex = 0;

        private void GenerateDynamicBuildingPreviewPanel()
        {
            if (string.IsNullOrEmpty(CurrentConfigName) || !DynamicBuildingsManager.ConfigMap.ContainsKey(CurrentConfigName))
            {
                return;
            }

            _dynamicBuildingPreviewPanel = new PPanel("DynamicBuildingPreviewPanel") { Direction = PanelDirection.Vertical };

            var sprites = GenerateSpritesToShow(CurrentConfigName, DesiredBuildingWidthToShow);

            var basicInfo = GenerateBasicInfoPanel();
            _dynamicBuildingPreviewPanel.AddChild(basicInfo);

            var gridPanelWithCaptions = GenerateCaptionsForSprites(sprites);
            _dynamicBuildingPreviewPanel.AddChild(gridPanelWithCaptions);

            var animSlicingSettings = _modSettings.GetAnimSplittingSettingsList().ToDictionary(x => x.ConfigName, y => y);
            var animSlicingSettingsItem = animSlicingSettings[CurrentConfigName];
            var gridPanelWithSprites = GenerateScreenComponentsForSprites(sprites, animSlicingSettingsItem.DoFlipEverySecondIteration);
            _dynamicBuildingPreviewPanel.AddChild(gridPanelWithSprites);

            _dialogBodyChild.AddChild(_dynamicBuildingPreviewPanel);
        }

        private PPanel GenerateBasicInfoPanel()
        {
            var config = DynamicBuildingsManager.ConfigMap[CurrentConfigName];
            var originalDef = DynamicBuildingsManager.ConfigToBuildingDefMap[config];
            var symbol = originalDef.AnimFiles.FirstOrDefault().GetData().build.GetSymbolByIndex(SymbolToShowIndex);
            var symbolFrame = symbol.GetFrame(SymbolFrameToShowIndex);
            var texture = originalDef.AnimFiles.FirstOrDefault().GetData().build.GetTexture(0);
            var labelInfo = new PLabel();
            labelInfo.Text = "Original width: " + ((int)((symbolFrame.uvMax.x - symbolFrame.uvMin.x) * texture.width)).ToString();
            var result = new PPanel()
            {
                Margin = new RectOffset(0, 0, 10, 10)
            };
            result.AddChild(labelInfo);
            return result;
        }

        private List<Sprite> GenerateSpritesToShow(string configName, int desiredBuildingWidthToShow)
        {
            var config = DynamicBuildingsManager.ConfigMap[configName];
            var extendableConfigSettings = _modSettings.GetExtendableConfigSettingsList().ToDictionary(x => x.ConfigName, y => y);
            var extendableConfigSettingsItem = extendableConfigSettings[configName];
            var animSlicingSettings = _modSettings.GetAnimSplittingSettingsList().ToDictionary(x => x.ConfigName, y => y);
            var animSlicingSettingsItem = animSlicingSettings[configName];

            var originalDef = DynamicBuildingsManager.ConfigToBuildingDefMap[config];
            var texture = originalDef.AnimFiles.FirstOrDefault().GetData().build.GetTexture(0);
            int textureWidth = texture.width;
            var dynamicBuildingWidthToShow = Math.Min(Math.Min(desiredBuildingWidthToShow, extendableConfigSettingsItem.MaxWidth), MaxBuildingWidthToShow);
            int widthInCellsDelta = dynamicBuildingWidthToShow - originalDef.WidthInCells;
            var symbol = originalDef.AnimFiles.FirstOrDefault().GetData().build.GetSymbolByIndex(SymbolToShowIndex);
            var symbolFrame = symbol.GetFrame(SymbolFrameToShowIndex);

            var smallerFrames = DynamicAnimManager.SplitFrameIntoParts(
                symbolFrame,
                textureWidth,
                widthInCellsDelta,
                middle_X: animSlicingSettingsItem.MiddlePart_X,
                middle_Width: animSlicingSettingsItem.MiddlePart_Width,
                fillingMethod: animSlicingSettingsItem.FillingMethod,
                doFlipEverySecondIteration: animSlicingSettingsItem.DoFlipEverySecondIteration
            );
            var sprites = new List<Sprite>();
            foreach (var frame in smallerFrames)
            {
                var sprite = DynamicAnimManager.GenerateSpriteForFrame(frame, texture);
                sprites.Add(sprite);
            }

            return sprites;
        }

        private PGridPanel GenerateCaptionsForSprites(List<Sprite> sprites)
        {
            var gridPanelWithSprites = new PGridPanel() { DynamicSize = false };
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

                var label = new PLabel() {  DynamicSize = false };
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

            return gridPanelWithSprites;
        }

        private PGridPanel GenerateScreenComponentsForSprites(List<Sprite> sprites, bool doFlipEverySecondIteration)
        {
            var gridPanelWithSprites = new PGridPanel() { DynamicSize = false };
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
                ppanel.BackColor = Color.white;

                int outputWidth = (int)(sprites[spriteIdx].rect.width / sprites[spriteIdx].pixelsPerUnit);
                int outputHeight = (int)(sprites[spriteIdx].rect.height);
                //Debug.Log($"RectW = {sprites[spriteIdx].rect.width}, W = {outputWidth}, H = {outputHeight}, PPU = {sprites[spriteIdx].pixelsPerUnit}");
                gridPanelWithSprites.AddChild(ppanel, new GridComponentSpec(0, columnIdx));
            }

            return gridPanelWithSprites;
        }

        private void GenerateControlPanel()
        {
            var controlPanel = new PPanel("ControlPanel")
            {
                Direction = PanelDirection.Horizontal,
                Spacing = SpacingInPixels,
                Margin = new RectOffset(10, 10, 10, 10)
            };
            var cbShowTechName = new PCheckBox();
            cbShowTechName.InitialState = ShowTechName ? 1 : 0;
            cbShowTechName.Text = "Show tech names";
            cbShowTechName.OnChecked += OnChecked_ShowTechName;
            controlPanel.AddChild(cbShowTechName);

            _dialogBodyChild.AddChild(controlPanel);
        }

        private void GenerateInitialData()
        {
            _dialogData.Clear();
            var srcData = _modSettings.GetAnimSplittingSettingsList();
            if (srcData.Count == 0)
            {
                return;
            }    

            foreach (var entry in srcData)
            {
                var rec = new EditAnimSlicingSettings_Item()
                {
                    TechName = entry.ConfigName,
                    IsActive = entry.IsActive,
                    MiddlePart_X = entry.MiddlePart_X,
                    MiddlePart_Width = entry.MiddlePart_Width,
                    FillingMethod = entry.FillingMethod,
                    DoFlipEverySecondTime = entry.DoFlipEverySecondIteration
                };
                _dialogData.Add(rec);
            }

            var firstRecord = srcData.FirstOrDefault();
            CurrentConfigName = firstRecord.ConfigName;
        }

        private void OnDialogClosed(string option)
        {
            if (option == DialogOption_Cancel)
            {
                return;
            }

            var newRez = new List<AnimSplittingSettings>();
            foreach (var entry in _dialogData)
            {
                var rec = new AnimSplittingSettings()
                {
                    ConfigName = entry.TechName,
                    IsActive = entry.IsActive,
                    MiddlePart_X = entry.MiddlePart_X,
                    MiddlePart_Width = entry.MiddlePart_Width,
                    FillingMethod = entry.FillingMethod,
                    DoFlipEverySecondIteration = entry.DoFlipEverySecondTime
                };
                newRez.Add(rec);
            }

            _modSettings.SetAnimSplittingSettings(newRez);
        }

        private void OnTextChanged_TemplatePart_X(GameObject source, string text)
        {
            var record = _dialogData.Where(x => x.TechName == source.name).FirstOrDefault();
            if (record == null)
            {
                return;
            }

            if (int.TryParse(text, out var parsed))
            {
                record.MiddlePart_X = parsed;
                CurrentConfigName = record.TechName;
                RebuildBodyAndShow();
            }
        }

        private void OnTextChanged_TemplatePart_Width(GameObject source, string text)
        {
            var record = _dialogData.Where(x => x.TechName == source.name).FirstOrDefault();
            if (record == null)
            {
                return;
            }
            if (int.TryParse(text, out var parsed))
            {
                record.MiddlePart_Width = parsed;
                CurrentConfigName = record.TechName;
                RebuildBodyAndShow();
            }
        }

        private void On_StringListOption_Selected(GameObject source, StringListOption option)
        {
            int index = groups.IndexOf(option);

            if (index >= 0 && index < groups.Count)
            {
                var record = _dialogData.Where(x => x.TechName == source.name).FirstOrDefault();
                if (record == null)
                {
                    return;
                }
                record.FillingMethod = (FillingMethod)index;
                CurrentConfigName = record.TechName;
                RebuildBodyAndShow();
            }
        }

        private void OnChecked_IsActive(GameObject source, int state)
        {
            int newState = (state + 1) % 2;
            PCheckBox.SetCheckState(source, newState);
            var checkButton = source.GetComponentInChildren<MultiToggle>();
            var techName = checkButton.name;

            var record = _dialogData.Where(x => x.TechName == checkButton.name).FirstOrDefault();

            record.IsActive = (newState == 1);
            CurrentConfigName = record.TechName;
            RebuildBodyAndShow();
        }

        private void OnChecked_DoFlipEverySecondIteration(GameObject source, int state)
        {
            int newState = (state + 1) % 2;
            PCheckBox.SetCheckState(source, newState);
            var checkButton = source.GetComponentInChildren<MultiToggle>();
            var techName = checkButton.name;

            var record = _dialogData.Where(x => x.TechName == checkButton.name).FirstOrDefault();

            record.DoFlipEverySecondTime = (newState == 1);
            CurrentConfigName = record.TechName;
            RebuildBodyAndShow();
        }

        private void OnChecked_ShowTechName(GameObject source, int state)
        {
            int newState = (state + 1) % 2;
            PCheckBox.SetCheckState(source, newState);
            ShowTechName = (newState == 1);

            RebuildBodyAndShow();
        }
        
        private void OnConfigNameButton_Click(GameObject source)
        {
            var techName = source.name;

            CurrentConfigName = techName;
            RebuildBodyAndShow();
        }
    }
}