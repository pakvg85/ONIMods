using PeterHan.PLib.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ExtendedBuildingWidth
{
    public class Dialog_AddRemoveAnimSlicingSettings
    {
        private class AddRemoveDialog_Item
        {
            public string ConfigName { get; set; }
            public int IsChecked { get; set; }
            public string Caption { get; set; }
        }

        private PDialog _pDialog = null;
        private PPanel _dialogBody = null;
        private PPanelWithClearableChildren _dialogBodyChild = null;
        private KScreen _componentScreen = null;
        private readonly List<AddRemoveDialog_Item> _dialogData = new List<AddRemoveDialog_Item>();
        private readonly Dictionary<string, AddRemoveDialog_Item> _modifiedItems = new Dictionary<string, AddRemoveDialog_Item>();
        private readonly Dialog_EditAnimSlicingSettings _dialog_Parent;
        private readonly ModSettings _modSettings;

        const string DialogOption_Ok = "ok";
        const string DialogOption_Cancel = "cancel";
        const int LeftOffset = 12;
        const int RightOffset = 12;
        const int TopOffset = 7;
        const int BottomOffset = 7;
        const int SpacingInPixels = 7;

        public Dialog_AddRemoveAnimSlicingSettings(Dialog_EditAnimSlicingSettings dialog_Parent, ModSettings modSettings)
        {
            _dialog_Parent = dialog_Parent;
            _modSettings = modSettings;
        }

        public bool ShowTechName { get; set; } = false;

        public void CreateAndShow(object obj)
        {
            var dialog = new PDialog("AddRemoveAnimSlicingSettings")
            {
                Title = "Add New Records",
                DialogClosed = OnDialogClosed,
                Size = new Vector2 { x = 1000, y = 700 },
                MaxSize = new Vector2 { x = 1000, y = 700 },
                SortKey = 300.0f
            }.AddButton(DialogOption_Ok, "OK", null, PUITuning.Colors.ButtonPinkStyle)
            .AddButton(DialogOption_Cancel, "CANCEL", null, PUITuning.Colors.ButtonBlueStyle);

            _componentScreen = null;
            _pDialog = dialog;
            _dialogBody = dialog.Body;
            _dialogBodyChild = null;
            ShowTechName = _dialog_Parent.ShowTechName;
            _modifiedItems.Clear();

            RebuildAndShow(showFirstTime: true);
        }

        private void RebuildAndShow(bool showFirstTime = false)
        {
            if (!showFirstTime)
            {
                _componentScreen.Deactivate();
            }
            if (showFirstTime)
            {
                GenerateInitialData();
            }

            ClearContents();
            GenerateControlPanel();
            GenerateRecordsPanel();

            _componentScreen = null;
            var isBuilt = _pDialog.Build().TryGetComponent<KScreen>(out _componentScreen);
            if (isBuilt)
            {
                _componentScreen.Activate();
            }
        }

        private void ClearContents()
        {
            if (_dialogBodyChild == null)
            {
                _dialogBodyChild = new PPanelWithClearableChildren("AddRemoveDialog_RecordsPanel");
                _dialogBody.AddChild(_dialogBodyChild);
            }

            _dialogBodyChild.ClearChildren();
        }

        /// <summary>
        /// We cannot simply add all records from 'SettingsManager.ListOfAllBuildings', because there could be non-existing entries
        /// in config.json that should be shown.
        /// </summary>
        private void GenerateInitialData()
        {
            _dialogData.Clear();

            var sourceData = _modSettings.GetExtendableConfigSettingsList();

            var allBuildings = SettingsManager.ListOfAllBuildings;
            var dict = allBuildings.ToDictionary(x => x.ConfigName, y => y);
            var checkedConfigNames = _dialog_Parent.GetConfigNames();
            var configNames_Sorted = new SortedSet<string>(checkedConfigNames);
            var uncheckedConfigNames = sourceData.Where(x => !configNames_Sorted.Contains(x.ConfigName)).Select(x => x.ConfigName).ToList();
            //var uncheckedConfigNames = sourceData.Select(x => x.ConfigName).ToList();

            foreach (var configName in checkedConfigNames)
            {
                string caption = string.Empty;
                if (dict.TryGetValue(configName, out var dictEntry))
                {
                    caption = dictEntry.Caption;
                }
                if (string.IsNullOrEmpty(caption))
                {
                    caption = configName;
                }

                var rec = new AddRemoveDialog_Item()
                {
                    IsChecked = 1,
                    ConfigName = configName,
                    Caption = caption
                };
                _dialogData.Add(rec);
            }

            foreach (var configName in uncheckedConfigNames)
            {
                string caption = string.Empty;
                if (dict.TryGetValue(configName, out var dictEntry))
                {
                    caption = dictEntry.Caption;
                }
                if (string.IsNullOrEmpty(caption))
                {
                    caption = configName;
                }

                var rec = new AddRemoveDialog_Item()
                {
                    IsChecked = 0,
                    ConfigName = configName,
                    Caption = caption
                };
                _dialogData.Add(rec);
            }
        }

        private void GenerateControlPanel()
        {
            var addRemoveDialogSettingsPanel = new PPanel("AddRemoveDialogSettingsPanel") { Direction = PanelDirection.Horizontal, Spacing = SpacingInPixels };

            var cbShowTechName = new PCheckBox() { Margin = new RectOffset(LeftOffset, RightOffset, TopOffset, BottomOffset) };
            cbShowTechName.InitialState = ShowTechName ? 1 : 0;
            cbShowTechName.Text = "Show tech names";
            cbShowTechName.OnChecked = OnChecked_ShowTechName;
            addRemoveDialogSettingsPanel.AddChild(cbShowTechName);

            _dialogBodyChild.AddChild(addRemoveDialogSettingsPanel);
        }

        private void GenerateRecordsPanel()
        {
            var scrollBody = new PPanel("ScrollContent")
            {
                Spacing = 10,
                Direction = PanelDirection.Vertical,
                Alignment = TextAnchor.UpperCenter,
                FlexSize = Vector2.right
            };
            foreach (var entry in _dialogData)
            {
                var contents = new PGridPanel("Entries") { FlexSize = Vector2.right };

                contents.AddRow(new GridRowSpec());
                contents.AddColumn(new GridColumnSpec(700));

                var lCheckbox = new PCheckBox(name: entry.ConfigName);
                lCheckbox.InitialState = entry.IsChecked;
                if (!ShowTechName)
                {
                    lCheckbox.Text = entry.Caption;
                    lCheckbox.ToolTip = entry.ConfigName;
                }
                else
                {
                    lCheckbox.Text = entry.ConfigName;
                    lCheckbox.ToolTip = entry.Caption;
                }
                lCheckbox.OnChecked = OnChecked_RecordItem;
                contents.AddChild(lCheckbox, new GridComponentSpec(0, 0) { Alignment = TextAnchor.MiddleLeft });

                scrollBody.AddChild(contents);
            }

            var scrollPane = new PScrollPane()
            {
                ScrollHorizontal = false,
                ScrollVertical = true,
                Child = scrollBody,
                FlexSize = Vector2.right,
                TrackSize = 20,
                AlwaysShowHorizontal = false,
                AlwaysShowVertical = false
            };
            _dialogBodyChild.AddChild(scrollPane);
        }

        private void OnDialogClosed(string option)
        {
            if (option != DialogOption_Ok)
            {
                return;
            }
            _dialog_Parent.ShowTechName = ShowTechName;
            var addRemoveRecords = new List<System.Tuple<string, bool>>();
            foreach (var entry in _modifiedItems.Values)
            {
                addRemoveRecords.Add(new System.Tuple<string, bool>(entry.ConfigName, (entry.IsChecked == PCheckBox.STATE_CHECKED)));
            }
            _dialog_Parent.ApplyChanges(addRemoveRecords);
            _dialog_Parent.RebuildDataPanel();
        }

        private bool TryGetRecord(string name, out AddRemoveDialog_Item record)
        {
            if (!_dialogData.Any(x => x.ConfigName == name))
            {
                record = null;
                return false;
            }
            record = _dialogData.Where(x => x.ConfigName == name).First();
            return true;
        }

        private void OnChecked_RecordItem(GameObject source, int state)
        {
            int newState = (state + 1) % 2;
            PCheckBox.SetCheckState(source, newState);
            var checkButton = source.GetComponentInChildren<MultiToggle>();

            if (!TryGetRecord(checkButton.name, out var record))
            {
                return;
            }
            if (!_modifiedItems.ContainsKey(record.ConfigName))
            {
                _modifiedItems.Add(record.ConfigName, record);
            }
            _modifiedItems[record.ConfigName].IsChecked = newState;
        }

        private void OnChecked_ShowTechName(GameObject source, int state)
        {
            int newState = (state + 1) % 2;
            PCheckBox.SetCheckState(source, newState);
            ShowTechName = (newState == PCheckBox.STATE_CHECKED);
            RebuildAndShow();
        }
    }
}