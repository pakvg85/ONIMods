using PeterHan.PLib.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ExtendedBuildingWidth.STRINGS.UI;

namespace ExtendedBuildingWidth
{
    public class Dialog_AddRemoveRecords
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
        private readonly List<AddRemoveDialog_Item> _dialogData_Filtered = new List<AddRemoveDialog_Item>();
        private readonly List<AddRemoveDialog_Item> _dialogData_Filtered_OnPage = new List<AddRemoveDialog_Item>();
        private readonly Dictionary<string, AddRemoveDialog_Item> _modifiedItems = new Dictionary<string, AddRemoveDialog_Item>();
        private readonly Dialog_EditConfigJson _dialog_Parent;
        private readonly List<ExtendableConfigSettings_Gui> _parentDialogData;

        const string DialogOption_Ok = "ok";
        const string DialogOption_Cancel = "cancel";
        const int LeftOffset = 12;
        const int RightOffset = 12;
        const int TopOffset = 7;
        const int BottomOffset = 7;
        const int SpacingInPixels = 7;

        private int GetPageNumberByLine(int lineNumber) => (lineNumber - 1) / RecordsPerPage + 1;
        const int MinAvailablePageNumber = 1;
        private int MaxAvailablePageNumber => GetPageNumberByLine(_dialogData_Filtered.Count);

        public int CurrentPage { get; set; } = 1;
        public int RecordsPerPage { get; set; } = 30;
        public string FilterText { get; set; } = string.Empty;
        public bool ShowTechName { get; set; } = false;

        public Dialog_AddRemoveRecords(Dialog_EditConfigJson dialog_Parent, List<ExtendableConfigSettings_Gui> parentDialogData)
        {
            _dialog_Parent = dialog_Parent;
            _parentDialogData = parentDialogData;
        }

        public void CreateAndShow(object obj)
        {
            var dialog = new PDialog("AddOrDeleteRecords")
            {
                Title = DIALOG_ADDREMOVE_CONFIGMAIN.DIALOG_TITLE,
                DialogClosed = OnDialogClosed,
                Size = new Vector2 { x = 1000, y = 700 },
                MaxSize = new Vector2 { x = 1000, y = 700 },
                SortKey = 300.0f
            }.AddButton(DialogOption_Ok, DIALOG_COMMON_STR.BUTTON_OK, DIALOG_COMMON_STR.BUTTON_OK_TOOLTIP, PUITuning.Colors.ButtonPinkStyle)
            .AddButton(DialogOption_Cancel, DIALOG_COMMON_STR.BUTTON_CANCEL, DIALOG_COMMON_STR.BUTTON_CANCEL_TOOLTIP, PUITuning.Colors.ButtonBlueStyle);

            _componentScreen = null;
            _pDialog = dialog;
            _dialogBody = dialog.Body;
            _dialogBodyChild = null;
            FilterText = string.Empty;
            ShowTechName = _dialog_Parent.ShowTechName;
            _modifiedItems.Clear();

            CurrentPage = MinAvailablePageNumber;

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
            GenerateFilteredData();
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

            var checkedConfigNames = _dialog_Parent.GetConfigNames();
            var configNames_Sorted = new SortedSet<string>(checkedConfigNames);
            var uncheckedConfigNames = SettingsManager.AllBuildingsMap.Keys.Where(x => !configNames_Sorted.Contains(x)).Select(x => x).ToList();

            foreach (var configName in checkedConfigNames)
            {
                string caption = string.Empty;
                if (SettingsManager.AllBuildingsMap.TryGetValue(configName, out var buildingDescription))
                {
                    caption = buildingDescription.Caption;
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
                if (SettingsManager.AllBuildingsMap.TryGetValue(configName, out var buildingDescription))
                {
                    caption = buildingDescription.Caption;
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
         
        private void GenerateFilteredData()
        {
            _dialogData_Filtered.Clear();
            foreach (var entry in _dialogData)
            {
                if (!string.IsNullOrEmpty(FilterText))
                {
                    if ((string.IsNullOrEmpty(entry.ConfigName) || !entry.ConfigName.ToLower().Contains(FilterText.ToLower()))
                        && (string.IsNullOrEmpty(entry.Caption) || !entry.Caption.ToLower().Contains(FilterText.ToLower()))
                       )
                    {
                        continue;
                    }
                }

                _dialogData_Filtered.Add(entry);
            }

            if (!ShowTechName)
            {
                var sortedList = _dialogData_Filtered.OrderBy(x => x.Caption).OrderByDescending(y => y.IsChecked).ToList();
                _dialogData_Filtered.Clear();
                _dialogData_Filtered.AddRange(sortedList);
            }
            else
            {
                var sortedList = _dialogData_Filtered.OrderBy(x => x.ConfigName).OrderByDescending(y => y.IsChecked).ToList();
                _dialogData_Filtered.Clear();
                _dialogData_Filtered.AddRange(sortedList);
            }

            _dialogData_Filtered_OnPage.Clear();
            int iterScreenField = -1;
            foreach (var entry in _dialogData_Filtered)
            {
                iterScreenField++;

                int currPage = GetPageNumberByLine(iterScreenField);
                if (currPage != CurrentPage)
                {
                    continue;
                }
                _dialogData_Filtered_OnPage.Add(entry);
            }
        }

        private void GenerateControlPanel()
        {
            var addRemoveDialogSettingsPanel = new PPanel("AddRemoveDialogSettingsPanel") { Direction = PanelDirection.Horizontal, Spacing = SpacingInPixels };

            var cbShowTechName = new PCheckBox()
            {
                Margin = new RectOffset(LeftOffset, RightOffset, TopOffset, BottomOffset),
                InitialState = ShowTechName ? 1 : 0,
                Text = DIALOG_COMMON_STR.CHECKBOX_SHOWTECHNAMES,
                ToolTip = StringUtils.CombineTooltip(DIALOG_COMMON_STR.CHECKBOX_SHOWTECHNAMES_TOOLTIP1, DIALOG_COMMON_STR.CHECKBOX_SHOWTECHNAMES_TOOLTIP2),
                OnChecked = OnChecked_ShowTechName
            };
            addRemoveDialogSettingsPanel.AddChild(cbShowTechName);

            addRemoveDialogSettingsPanel.AddChild(new PLabel("LinesPerPageLabel") { Text = DIALOG_ADDREMOVE_CONFIGMAIN.LABEL_PAGESIZE });

            var txtLinesPerPage = new PTextField("LinesPerPageTxt")
            {
                Text = RecordsPerPage.ToString(),
                MinWidth = 50,
                OnTextChanged = OnTextChanged_LinesPerPage
            };
            addRemoveDialogSettingsPanel.AddChild(txtLinesPerPage);

            addRemoveDialogSettingsPanel.AddChild(new PLabel("FilterRecordsLabel") { Text = DIALOG_ADDREMOVE_CONFIGMAIN.LABEL_FILTER });

            var txtFilter = new PTextField("TextFilterRecords")
            {
                Text = FilterText,
                MinWidth = 200,
                OnTextChanged = OnTextChanged_Filter
            };
            addRemoveDialogSettingsPanel.AddChild(txtFilter);

            var btnRefresh = new PButton("BtnFilterRecords") { Margin = new RectOffset(LeftOffset, RightOffset, TopOffset, BottomOffset) };
            btnRefresh.Text = DIALOG_ADDREMOVE_CONFIGMAIN.BUTTON_REFRESH;
            btnRefresh.OnClick = OnClick_Refresh;
            addRemoveDialogSettingsPanel.AddChild(btnRefresh);

            _dialogBodyChild.AddChild(addRemoveDialogSettingsPanel);

            var pageButtonsPanel = new PPanel("PageButtonsPanel")
            {
                Direction = PanelDirection.Horizontal,
                Spacing = SpacingInPixels,
                FlexSize = Vector2.right
            };

            if (MaxAvailablePageNumber > 1)
            {
                var prevPageBtn = new PButton("BtnPrevPage") { Margin = new RectOffset(LeftOffset, RightOffset, TopOffset, BottomOffset) };
                prevPageBtn.Text = DIALOG_ADDREMOVE_CONFIGMAIN.BUTTON_PREVPAGE;
                prevPageBtn.OnClick = OnClick_PrevPage;
                pageButtonsPanel.AddChild(prevPageBtn);
            }

            bool tooMuchPages = false;
            for (int i = 1; i <= MaxAvailablePageNumber; i++)
            {
                if (
                       CurrentPage <= 3 && (i <= 6 || i == MaxAvailablePageNumber)
                    || CurrentPage > 3 && CurrentPage < MaxAvailablePageNumber - 3 && (i == MinAvailablePageNumber || i == MaxAvailablePageNumber || Math.Abs(i - CurrentPage) < 3)
                    || CurrentPage >= MaxAvailablePageNumber - 3 && (i == MinAvailablePageNumber || i >= MaxAvailablePageNumber - 6 + 1)
                )
                {
                    tooMuchPages = false;
                }
                else
                {
                    if (!tooMuchPages)
                    {
                        pageButtonsPanel.AddChild(new PLabel() { Text = DIALOG_ADDREMOVE_CONFIGMAIN.LABEL_PERIODS });
                        tooMuchPages = true;
                    }
                    continue;
                }

                var pageNumberBtn = new PButton("BtnPage_" + i.ToString())
                {
                    Margin = new RectOffset(LeftOffset, RightOffset, TopOffset, BottomOffset)
                };
                pageNumberBtn.Text = i.ToString();
                pageNumberBtn.OnClick = OnClick_PageNumber;
                if (i == CurrentPage)
                {
                    pageNumberBtn.Color = ScriptableObject.CreateInstance<ColorStyleSetting>();
                    pageNumberBtn.Color.hoverColor = Color.magenta;
                    pageNumberBtn.Color.inactiveColor = Color.magenta;
                }
                pageButtonsPanel.AddChild(pageNumberBtn);
            }

            if (MaxAvailablePageNumber > 1)
            {
                var nextPageBtn = new PButton("BtnNextPage") { Margin = new RectOffset(LeftOffset, RightOffset, TopOffset, BottomOffset) };
                nextPageBtn.Text = DIALOG_ADDREMOVE_CONFIGMAIN.BUTTON_NEXTPAGE;
                nextPageBtn.OnClick = OnClick_NextPage;
                pageButtonsPanel.AddChild(nextPageBtn);
            }

            _dialogBodyChild.AddChild(pageButtonsPanel);
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
            foreach (var entry in _dialogData_Filtered_OnPage)
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

        private bool TryGetRecord(string name, out AddRemoveDialog_Item record)
        {
            if (!_dialogData_Filtered_OnPage.Any(x => x.ConfigName == name))
            {
                record = null;
                return false;
            }
            record = _dialogData.Where(x => x.ConfigName == name).First();
            return true;
        }

        public void ApplyChanges(ICollection<System.Tuple<string, bool>> modifiedRecords)
        {
            foreach (var entry in modifiedRecords)
            {
                var configName = entry.Item1;
                bool doAddNewRecord = entry.Item2;
                bool hasRecordsWithThisConfig = _dialog_Parent.TryGetRecord(configName, out var existingRecord);

                if (!doAddNewRecord)
                {
                    if (hasRecordsWithThisConfig)
                    {
                        _parentDialogData.Remove(existingRecord);
                    }
                }
                else
                {
                    if (!hasRecordsWithThisConfig)
                    {
                        var newRec = Dialog_EditConfigJson.NewDefaultRecord(configName);
                        _parentDialogData.Add(newRec);
                    }
                }
            }
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
            ApplyChanges(addRemoveRecords);
            _dialog_Parent.RebuildDataPanel();
        }

        private void OnChecked_RecordItem(GameObject source, int state)
        {
            int newState = (state + 1) % 2;
            PCheckBox.SetCheckState(source, newState);
            var checkButton = source.GetComponentInChildren<MultiToggle>();
            var configName = checkButton.name;
            if (!TryGetRecord(configName, out var record))
            {
                return;
            }
            if (!_modifiedItems.ContainsKey(configName))
            {
                _modifiedItems.Add(configName, record);
            }
            _modifiedItems[configName].IsChecked = newState;
        }

        private void OnTextChanged_LinesPerPage(GameObject source, string text)
        {
            if (int.TryParse(text, out var count))
            {
                RecordsPerPage = count;
            }
        }

        private void OnChecked_ShowTechName(GameObject source, int state)
        {
            int newState = (state + 1) % 2;
            PCheckBox.SetCheckState(source, newState);
            ShowTechName = (newState == PCheckBox.STATE_CHECKED);
            CurrentPage = MinAvailablePageNumber;
            RebuildAndShow();
        }

        private void OnClick_PageNumber(GameObject source)
        {
            try
            {
                int parsedInt = 0;
                if (int.TryParse(source.name.Split('_')[1], out parsedInt))
                {
                    CurrentPage = parsedInt;
                }
                RebuildAndShow();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"ExtendedBuildingWidth - OnPageNumberClick");
                Debug.LogWarning(e.ToString());
            }
        }

        private void OnClick_PrevPage(GameObject source)
        {
            if (CurrentPage <= MinAvailablePageNumber)
            {
                return;
            }
            CurrentPage--;
            RebuildAndShow();
        }

        private void OnClick_NextPage(GameObject source)
        {
            if (CurrentPage >= MaxAvailablePageNumber)
            {
                return;
            }
            CurrentPage++;
            RebuildAndShow();
        }

        private void OnTextChanged_Filter(GameObject source, string text)
        {
            FilterText = text;
        }

        private void OnClick_Refresh(GameObject source)
        {
            CurrentPage = MinAvailablePageNumber;

            RebuildAndShow();
        }
    }
}