using PeterHan.PLib.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ExtendedBuildingWidth.STRINGS.UI;

namespace ExtendedBuildingWidth
{
    public class Dialog_EditConfigJson
    {
        private class EditConfigDialog_Item
        {
            public string ConfigName { get; set; }
            public int MinWidth { get; set; }
            public int MaxWidth { get; set; }
            public float AnimStretchModifier { get; set; }
        }

        private PDialog _pDialog = null;
        private PPanel _dialogBody = null;
        private PPanel _dialogBodyChild = null;
        private KScreen _componentScreen = null;
        private readonly List<EditConfigDialog_Item> _dialogData = new List<EditConfigDialog_Item>();

        const string DialogOption_Ok = "ok";
        const string DialogOption_Cancel = "cancel";
        const int SpacingInPixels = 7;

        public bool ShowTechName = false;

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
                Size = new Vector2 { x = 1000, y = 700 },
                MaxSize = new Vector2 { x = 1000, y = 700 },
                SortKey = 200.0f
            }.AddButton(DialogOption_Ok, DIALOG_COMMON_STR.BUTTON_OK, null, PUITuning.Colors.ButtonPinkStyle)
            .AddButton(DialogOption_Cancel, DIALOG_COMMON_STR.BUTTON_CANCEL, null, PUITuning.Colors.ButtonBlueStyle);

            GenerateData();

            _pDialog = dialog;
            _dialogBody = dialog.Body;

            RebuildBodyAndShow(showFirstTime: true);
        }

        internal void RebuildBodyAndShow(bool showFirstTime = false)
        {
            if (!showFirstTime)
            {
                _componentScreen.Deactivate();
            }

            ClearContents();
            GenerateRecordsPanel();
            GenerateControlPanel();

            _dialogBody.AddChild(_dialogBodyChild);

            _componentScreen = null;
            var isBuilt = _pDialog.Build().TryGetComponent<KScreen>(out _componentScreen);
            if (isBuilt)
            {
                _componentScreen.Activate();
            }
        }

        public List<string> GetConfigNames()
        {
            return _dialogData.Select(d => d.ConfigName).ToList();
        }

        public void ApplyChanges(ICollection<System.Tuple<string, bool>> modifiedRecords)
        {
            foreach (var entry in modifiedRecords)
            {
                var configName = entry.Item1;
                bool doAddNewRecord = entry.Item2;
                bool hasRecordsWithThisConfig = TryGetRecord(configName, out var existingRecord);

                if (!doAddNewRecord)
                {
                    if (hasRecordsWithThisConfig)
                    {
                        _dialogData.Remove(existingRecord);
                    }
                }
                else
                {
                    if (!hasRecordsWithThisConfig)
                    {
                        var newRec = new EditConfigDialog_Item()
                        {
                            ConfigName = configName,
                            MinWidth = SettingsManager.DefaultMinWidth,
                            MaxWidth = SettingsManager.DefaultMaxWidth,
                            AnimStretchModifier = SettingsManager.DefaultAnimStretchModifier
                        };
                        _dialogData.Add(newRec);
                    }
                }
            }
        }

        private void ClearContents()
        {
            if (_dialogBodyChild != null)
            {
                _dialogBody.RemoveChild(_dialogBodyChild);
            }
            _dialogBodyChild = new PPanel("DialogBodyChild");
        }

        private void GenerateRecordsPanel()
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
            tableTitlesPanel.AddChild(new PLabel() { Text = DIALOG_EDIT_MAINSETTINGS.GRIDCOLUMN_MAXWIDTH1 }, new GridComponentSpec(iRow, ++iCol));
            tableTitlesPanel.AddChild(new PLabel() { Text = DIALOG_EDIT_MAINSETTINGS.GRIDCOLUMN_MAXWIDTH2 }, new GridComponentSpec(iRow + 1, iCol));
            tableTitlesPanel.AddChild(new PLabel() { Text = DIALOG_EDIT_MAINSETTINGS.GRIDCOLUMN_STRETCHKOEF1 }, new GridComponentSpec(iRow, ++iCol));
            tableTitlesPanel.AddChild(new PLabel() { Text = DIALOG_EDIT_MAINSETTINGS.GRIDCOLUMN_STRETCHKOEF2 }, new GridComponentSpec(iRow + 1, iCol));

            _dialogBodyChild.AddChild(tableTitlesPanel);

            var gridPanel = new PGridPanel("EditConfigJsonGridPanel") { Margin = new RectOffset(10, 40, 10, 10) };
            gridPanel.AddColumn(new GridColumnSpec(440));
            gridPanel.AddColumn(new GridColumnSpec(80));
            gridPanel.AddColumn(new GridColumnSpec(100));
            foreach (var entry in _dialogData)
            {
                gridPanel.AddRow(new GridRowSpec());
            }

            iRow = -1;
            foreach (var entry in _dialogData)
            {
                ++iRow;
                iCol = -1;

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

        private void GenerateControlPanel()
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
                OnChecked = OnChecked_ShowTechName
            };
            controlPanel.AddChild(cbShowTechName);

            var btnAdd = new PButton()
            {
                Text = DIALOG_EDIT_MAINSETTINGS.BUTTON_STARTDIALOGADDREMOVE,
                OnClick = OnClick_AddRemoveRecords,
            };
            controlPanel.AddChild(btnAdd);

            _dialogBodyChild.AddChild(controlPanel);
        }

        private void GenerateData()
        {
            _dialogData.Clear();
            var confList = _modSettings.GetExtendableConfigSettingsList();
            foreach (var entry in confList)
            {
                var rec = new EditConfigDialog_Item()
                {
                    ConfigName = entry.ConfigName,
                    MinWidth = entry.MinWidth,
                    MaxWidth = entry.MaxWidth,
                    AnimStretchModifier = entry.AnimStretchModifier
                };
                _dialogData.Add(rec);
            }
        }

        private bool TryGetRecord(string name, out EditConfigDialog_Item record)
        {
            if (!_dialogData.Any(x => x.ConfigName == name))
            {
                record = null;
                return false;
            }
            record = _dialogData.Where(x => x.ConfigName == name).First();
            return true;
        }

        private void OnDialogClosed(string option)
        {
            if (option != DialogOption_Ok)
            {
                return;
            }

            var newRez = new List<ExtendableConfigSettings>();
            foreach (var entry in _dialogData)
            {
                var rec = new ExtendableConfigSettings()
                {
                    ConfigName = entry.ConfigName,
                    MinWidth = entry.MinWidth,
                    MaxWidth = entry.MaxWidth,
                    AnimStretchModifier = entry.AnimStretchModifier
                };
                newRez.Add(rec);
            }

            _modSettings.SetExtendableConfigSettings(newRez);
        }

        private void OnTextChanged_MaxWidth(GameObject source, string text)
        {
            if (!TryGetRecord(source.name, out var record))
            {
                return;
            }
            if (!int.TryParse(text, out var parsed))
            {
                return;
            }
            record.MaxWidth = parsed;
        }

        private void OnTextChanged_AnimStretchModifier(GameObject source, string text)
        {
            if (!TryGetRecord(source.name, out var record))
            {
                return;
            }
            if (!float.TryParse(text, out var parsed))
            {
                return;
            }
            record.AnimStretchModifier = parsed;
        }

        private void OnClick_AddRemoveRecords(GameObject source)
        {
            var dARR = new Dialog_AddRemoveRecords(this);
            dARR.CreateAndShow(null);
        }

        private void OnChecked_ShowTechName(GameObject source, int state)
        {
            int newState = (state + 1) % 2;
            PCheckBox.SetCheckState(source, newState);
            ShowTechName = (newState == PCheckBox.STATE_CHECKED);
            RebuildBodyAndShow();
        }
    }
}