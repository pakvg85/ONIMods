using PeterHan.PLib.Core;
using PeterHan.PLib.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ExtendedBuildingWidth.STRINGS.UI;

namespace ExtendedBuildingWidth
{
    public class Dialog_EditConfigJson
    {
        private GameObject _dataPanelParentGo = null;
        private GameObject _dataPanelGo = null;
        private readonly List<ExtendableConfigSettings_Gui> _dialogMainData = new List<ExtendableConfigSettings_Gui>();

        const string DialogOption_Ok = "ok";
        const string DialogOption_Cancel = "cancel";
        const int SpacingInPixels = 7;

        public bool ShowTechName { get; set; } = false;
        public bool ActiveRecordInitialized { get; set; } = false;
        public string ActiveRecordId { get; set; } = default;

        private readonly ModSettings _modSettings;

        public Dialog_EditConfigJson(ModSettings modSettings)
        {
            _modSettings = modSettings;
        }

        public void CreateAndShow(object obj)
        {
            var dialog = new PDialog("EditConfigJsonDialog")
            {
                Title = DIALOG_EDIT_MAINSETTINGS.DIALOG_TITLE,
                DialogClosed = OnDialogClosed,
                Size = new Vector2 { x = 1280, y = 900 },
                MaxSize = new Vector2 { x = 1280, y = 900 },
                SortKey = 200.0f
            }.AddButton(DialogOption_Ok, DIALOG_COMMON_STR.BUTTON_OK, null, PUITuning.Colors.ButtonPinkStyle)
            .AddButton(DialogOption_Cancel, DIALOG_COMMON_STR.BUTTON_CANCEL, null, PUITuning.Colors.ButtonBlueStyle);

            GenerateInitialData();

            dialog.Body.DynamicSize = true;
            dialog.Body.FlexSize = Vector2.one;
            dialog.Body.Margin = new RectOffset(10, 10, 10, 10);
            dialog.Body.Alignment = TextAnchor.UpperLeft;
            //dialog.Body.BackColor = Color.white;

            var gridPanel = new PGridPanel()
            {
                //BackColor = Color.black,
                DynamicSize = true,
                FlexSize = Vector2.one // does matter - so the scroll slider will be at far right
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
                DynamicSize = true,
                FlexSize = Vector2.one // does matter - if not set, then contents will be centered
            };
            gridPanel.AddChild(headerPanelParent, new GridComponentSpec(0, 0));

            // contents should lean to left border
            // and scroll slider should lean to the right border
            var dataPanelParent = new PPanel("DataPanelParent")
            {
                //Alignment = ... // doesn't matter - contents are stretched by ScrollPane
                //BackColor = Color.magenta,
                DynamicSize = true,
                FlexSize = Vector2.one // does matter - so the scroll slider will be at far right
            };
            dataPanelParent.OnRealize += (realized) => { _dataPanelParentGo = realized; };
            gridPanel.AddChild(dataPanelParent, new GridComponentSpec(1, 0));

            // contents should be centered
            var controlPanelParent = new PPanel("ControlPanelParent")
            {
                //Alignment = ... FlexSize = ... // doesn't matter - contents will be centered anyway
            };
            gridPanel.AddChild(controlPanelParent, new GridComponentSpec(2, 0));

            var titlesPanel = GenerateTitles();
            //titlesPanel.FlexSize = ... ; // should not be stretched - contents should lean to the left
            //titlesPanel.BackColor = Color.gray;
            headerPanelParent.AddChild(titlesPanel);
            var dataPanel = GenerateDataPanel();
            //dataPanel.BackColor = Color.yellow;
            // dataPanel.FlexSize = Vector2.one; // should not be stretched - contents should lean to the left
            var dataPanelWithScroll = CreateScrollForPanel(dataPanel);
            dataPanelWithScroll.FlexSize = Vector2.one; // does matter - so the scroll slider will be at far right
            dataPanelWithScroll.OnRealize += (realized) => { _dataPanelGo = realized; };
            dataPanelParent.AddChild(dataPanelWithScroll);
            var controlPanel = GenerateControlPanel();
            //controlPanel.BackColor = Color.blue;
            controlPanelParent.AddChild(controlPanel);

            dialog.Show();
        }

        private PScrollPane CreateScrollForPanel(IUIComponent child)
        {
            var scrollPane = new PScrollPane()
            {
                Child = child,
                TrackSize = 20,
                ScrollHorizontal = false,
                ScrollVertical = true,
                AlwaysShowHorizontal = false,
                AlwaysShowVertical = false
            };
            return scrollPane;
        }

        public List<string> GetConfigNames()
        {
            return _dialogMainData.Select(d => d.ConfigName).ToList();
        }

        private PGridPanel GenerateTitles()
        {
            var tableTitlesPanel = new PGridPanel("EditConfigJsonTitlesPanel") { Margin = new RectOffset(10, 40, 10, 0) };
            tableTitlesPanel.AddColumn(new GridColumnSpec(440));
            tableTitlesPanel.AddColumn(new GridColumnSpec(80));
            tableTitlesPanel.AddColumn(new GridColumnSpec(100));
            tableTitlesPanel.AddRow(new GridRowSpec());
            tableTitlesPanel.AddRow(new GridRowSpec());

            int iRow = 0;
            int iCol = -1;
            tableTitlesPanel.AddChild(new PLabel() { Text = DIALOG_EDIT_MAINSETTINGS.LABEL_CONFIGNAME }, new GridComponentSpec(iRow, ++iCol));
            tableTitlesPanel.AddChild(new PLabel() { Text = DIALOG_EDIT_MAINSETTINGS.GRIDCOLUMN_MAXWIDTH1, ToolTip = DIALOG_EDIT_MAINSETTINGS.GRIDCOLUMN_MAXWIDTH_TOOLTIP }, new GridComponentSpec(iRow, ++iCol));
            tableTitlesPanel.AddChild(new PLabel() { Text = DIALOG_EDIT_MAINSETTINGS.GRIDCOLUMN_MAXWIDTH2, ToolTip = DIALOG_EDIT_MAINSETTINGS.GRIDCOLUMN_MAXWIDTH_TOOLTIP }, new GridComponentSpec(iRow + 1, iCol));
            tableTitlesPanel.AddChild(new PLabel() { Text = DIALOG_EDIT_MAINSETTINGS.GRIDCOLUMN_STRETCHKOEF1, ToolTip = DIALOG_EDIT_MAINSETTINGS.GRIDCOLUMN_STRETCHKOEF_TOOLTIP }, new GridComponentSpec(iRow, ++iCol));
            tableTitlesPanel.AddChild(new PLabel() { Text = DIALOG_EDIT_MAINSETTINGS.GRIDCOLUMN_STRETCHKOEF2, ToolTip = DIALOG_EDIT_MAINSETTINGS.GRIDCOLUMN_STRETCHKOEF_TOOLTIP }, new GridComponentSpec(iRow + 1, iCol));

            return tableTitlesPanel;
        }

        private PGridPanel GenerateDataPanel()
        {
            var gridPanel = new PGridPanel("EditConfigJsonGridPanel") { Margin = new RectOffset(10, 40, 10, 10) };
            gridPanel.AddColumn(new GridColumnSpec(440));
            gridPanel.AddColumn(new GridColumnSpec(80));
            gridPanel.AddColumn(new GridColumnSpec(100));
            foreach (var entry in _dialogMainData)
            {
                gridPanel.AddRow(new GridRowSpec());
            }

            int iRow = -1;
            foreach (var entry in _dialogMainData)
            {
                ++iRow;
                int iCol = -1;

                string configCaption = string.Empty;
                if (SettingsManager.AllBuildingsMap.TryGetValue(entry.ConfigName, out var buildingDescription))
                {
                    configCaption = buildingDescription.Caption;
                }
                if (string.IsNullOrEmpty(configCaption))
                {
                    configCaption = entry.ConfigName;
                }
                var bn = new PLabel(entry.ConfigName);
                if (!ShowTechName)
                {
                    bn.Text = configCaption;
                    bn.ToolTip = entry.ConfigName;
                }
                else
                {
                    bn.Text = entry.ConfigName;
                    bn.ToolTip = configCaption;
                }
                gridPanel.AddChild(bn, new GridComponentSpec(iRow, ++iCol) { Alignment = TextAnchor.MiddleLeft });

                var maxW = new PTextField(entry.ConfigName)
                {
                    Text = entry.MaxWidth.ToString(),
                    OnTextChanged = OnTextChanged_MaxWidth,
                    MinWidth = 60
                };
                gridPanel.AddChild(maxW, new GridComponentSpec(iRow, ++iCol));

                var strMdf = new PTextField(entry.ConfigName)
                {
                    Text = entry.AnimStretchModifier.ToString(),
                    OnTextChanged = OnTextChanged_AnimStretchModifier,
                    MinWidth = 90
                };
                gridPanel.AddChild(strMdf, new GridComponentSpec(iRow, ++iCol));
            }

            return gridPanel;
        }

        private PPanel GenerateControlPanel()
        {
            var controlPanel = new PPanel("ControlPanel") {
                Direction = PanelDirection.Horizontal,
                Spacing = SpacingInPixels,
                Margin = new RectOffset(10, 10, 10, 10)
            };
            var cbShowTechName = new PCheckBox()
            {
                InitialState = ShowTechName ? 1 : 0,
                Text = DIALOG_COMMON_STR.CHECKBOX_SHOWTECHNAMES,
                ToolTip = DIALOG_COMMON_STR.CHECKBOX_SHOWTECHNAMES_TOOLTIP1 + Environment.NewLine + DIALOG_COMMON_STR.CHECKBOX_SHOWTECHNAMES_TOOLTIP2,
                OnChecked = OnChecked_ShowTechName
            };
            controlPanel.AddChild(cbShowTechName);

            var btnAdd = new PButton()
            {
                Text = DIALOG_EDIT_MAINSETTINGS.BUTTON_STARTDIALOGADDREMOVE,
                OnClick = OnClick_AddRemoveRecords,
            };
            controlPanel.AddChild(btnAdd);

            return controlPanel;
        }

        private void GenerateInitialData()
        {
            _dialogMainData.Clear();
            var confList = _modSettings.GetExtendableConfigSettingsList();
            foreach (var entry in confList)
            {
                var rec = DataMapper.SourceToGui(entry);
                _dialogMainData.Add(rec);
            }
        }

        internal void RebuildDataPanel()
        {
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

        public bool TryGetRecord(string name, out ExtendableConfigSettings_Gui record)
        {
            if (!_dialogMainData.Any(x => x.ConfigName == name))
            {
                record = null;
                return false;
            }
            record = _dialogMainData.Where(x => x.ConfigName == name).First();
            return true;
        }

        public static ExtendableConfigSettings_Gui NewDefaultRecord(string configName)
        {
            var newRec = new ExtendableConfigSettings_Gui()
            {
                ConfigName = configName,
                MinWidth = SettingsManager.DefaultMinWidth,
                MaxWidth = SettingsManager.DefaultMaxWidth,
                AnimStretchModifier = SettingsManager.DefaultAnimStretchModifier
            };
            return newRec;
        }

        private void OnDialogClosed(string option)
        {
            if (option != DialogOption_Ok)
            {
                return;
            }

            var newRez = new List<ExtendableConfigSettings>();
            foreach (var entry in _dialogMainData)
            {
                var rec = DataMapper.GuiToSource(entry);
                newRez.Add(rec);
            }

            _modSettings.SetExtendableConfigSettings(newRez);
        }

        private void OnTextChanged_MaxWidth(GameObject source, string text)
        {
            if (   !TryGetRecord(source.name, out var record)
                || !int.TryParse(text, out var parsed)
                )
            {
                return;
            }
            record.MaxWidth = parsed;
        }

        private void OnTextChanged_AnimStretchModifier(GameObject source, string text)
        {
            if (!TryGetRecord(source.name, out var record)
                || !int.TryParse(text, out var parsed)
                )
            {
                return;
            }
            record.AnimStretchModifier = parsed;
        }

        private void OnClick_AddRemoveRecords(GameObject source)
        {
            var dARR = new Dialog_AddRemoveRecords(this, _dialogMainData);
            dARR.CreateAndShow(null);
        }

        private void OnChecked_ShowTechName(GameObject source, int state)
        {
            int newState = (state + 1) % 2;
            PCheckBox.SetCheckState(source, newState);
            ShowTechName = (newState == PCheckBox.STATE_CHECKED);
            RebuildDataPanel();
        }
    }
}