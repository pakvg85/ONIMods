using PeterHan.PLib.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ExtendedBuildingWidth
{
    public class Dialog_AddRemoveRecords
    {
        private class AddRemoveDialog_Item
        {
            public string TechName { get; set; }
            public int IsChecked { get; set; }
            public string Caption { get; set; }
        }

        private class PPanelWithClearableChildren : PPanel
        {
            public PPanelWithClearableChildren(string name) : base(name) { }
            public void ClearChildren() => base.children.Clear();
        }

        private readonly List<AddRemoveDialog_Item> _dialogData = new List<AddRemoveDialog_Item>();
        private readonly List<AddRemoveDialog_Item> _dialogData_Filtered = new List<AddRemoveDialog_Item>();
        private readonly List<AddRemoveDialog_Item> _dialogData_Filtered_OnPage = new List<AddRemoveDialog_Item>();
        private readonly Dictionary<string, AddRemoveDialog_Item> _modifiedItems = new Dictionary<string, AddRemoveDialog_Item>();

        const string DialogOption_Ok = "ok";
        const string DialogOption_Cancel = "cancel";
        const int LeftOffset = 12;
        const int RightOffset = 12;
        const int TopOffset = 7;
        const int BottomOffset = 7;
        const int SpacingInPixels = 7;

        private PDialog _addRemoveDialog_PDialog = null;
        private PPanel _addRemoveDialog_BodyPanel = null;
        private PPanelWithClearableChildren _addRemoveDialog_BodyPanelContents = null;
        private KScreen _componentScreen_AddRemoveRecordsDialog = null;

        private int GetPageNumberByLine(int lineNumber) => (lineNumber - 1) / _recordsPerPage + 1;
        const int MinAvailablePageNumber = 1;
        private int MaxAvailablePageNumber => GetPageNumberByLine(_dialogData_Filtered.Count);

        private int _currentPage = 1;
        private int _recordsPerPage = 30;
        private string _filterText = string.Empty;
        public bool ShowTechName = false;

        private readonly Dialog_EditConfigJson _dialog_EditConfigJson;

        public Dialog_AddRemoveRecords(Dialog_EditConfigJson dialog_EditConfigJson)
        {
            _dialog_EditConfigJson = dialog_EditConfigJson;
        }

        public void CreateAndShow(object obj)
        {
            var dialog = new PDialog("AddOrDeleteRecords")
            {
                Title = "Add New Records",
                DialogClosed = OnDialogClosed,
                Size = new Vector2 { x = 800, y = 600 },
                MaxSize = new Vector2 { x = 800, y = 600 },
                SortKey = 300.0f
            }.AddButton(DialogOption_Ok, "OK", null, PUITuning.Colors.ButtonPinkStyle)
            .AddButton(DialogOption_Cancel, "CANCEL", null, PUITuning.Colors.ButtonBlueStyle);

            _componentScreen_AddRemoveRecordsDialog = null;
            _addRemoveDialog_PDialog = dialog;
            _addRemoveDialog_BodyPanel = dialog.Body;
            _addRemoveDialog_BodyPanelContents = null;
            _filterText = string.Empty;
            ShowTechName = _dialog_EditConfigJson.ShowTechName;
            _modifiedItems.Clear();

            _currentPage = MinAvailablePageNumber;

            RebuildAndShow(showFirstTime: true);
        }

        private void RebuildAndShow(bool showFirstTime = false)
        {
            if (!showFirstTime)
            {
                _componentScreen_AddRemoveRecordsDialog.Deactivate();
            }
            if (showFirstTime)
            {
                GenerateInitialData();
            }

            ClearContents();
            GenerateFilteredData();
            GenerateControlPanel();
            GenerateRecordsPanel();

            _componentScreen_AddRemoveRecordsDialog = null;
            var isBuilt = _addRemoveDialog_PDialog.Build().TryGetComponent<KScreen>(out _componentScreen_AddRemoveRecordsDialog);
            if (isBuilt)
            {
                _componentScreen_AddRemoveRecordsDialog.Activate();
            }
        }

        private void ClearContents()
        {
            if (_addRemoveDialog_BodyPanelContents == null)
            {
                _addRemoveDialog_BodyPanelContents = new PPanelWithClearableChildren("AddRemoveDialog_RecordsPanel");
                _addRemoveDialog_BodyPanel.AddChild(_addRemoveDialog_BodyPanelContents);
            }

            _addRemoveDialog_BodyPanelContents.ClearChildren();
        }

        /// <summary>
        /// We cannot simply add all records from 'SettingsManager.ListOfAllBuildings', because there could be non-existing entries
        /// in config.json that should be shown.
        /// </summary>
        private void GenerateInitialData()
        {
            _dialogData.Clear();

            var allBuildings = SettingsManager.ListOfAllBuildings;
            var allBuildings_Dict = allBuildings.ToDictionary(x => x.TechName, y => y);
            var checkedTechNames = _dialog_EditConfigJson.GetTechNames();
            var techNames_Sorted = new SortedSet<string>(checkedTechNames);
            var uncheckedTechNames = allBuildings.Where(x => !techNames_Sorted.Contains(x.TechName)).Select(x => x.TechName).ToList();

            foreach (var techName in checkedTechNames)
            {
                string caption = string.Empty;
                if (allBuildings_Dict.TryGetValue(techName, out var dictEntry))
                {
                    caption = dictEntry.Caption;
                }
                if (string.IsNullOrEmpty(caption))
                {
                    caption = techName;
                }

                var rec = new AddRemoveDialog_Item()
                {
                    IsChecked = 1,
                    TechName = techName,
                    Caption = caption
                };
                _dialogData.Add(rec);
            }

            foreach (var techName in uncheckedTechNames)
            {
                string caption = string.Empty;
                if (allBuildings_Dict.TryGetValue(techName, out var dictEntry))
                {
                    caption = dictEntry.Caption;
                }
                if (string.IsNullOrEmpty(caption))
                {
                    caption = techName;
                }

                var rec = new AddRemoveDialog_Item()
                {
                    IsChecked = 0,
                    TechName = techName,
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
                if (!string.IsNullOrEmpty(_filterText))
                {
                    if ((string.IsNullOrEmpty(entry.TechName) || !entry.TechName.ToLower().Contains(_filterText.ToLower()))
                        && (string.IsNullOrEmpty(entry.Caption) || !entry.Caption.ToLower().Contains(_filterText.ToLower()))
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
                var sortedList = _dialogData_Filtered.OrderBy(x => x.TechName).OrderByDescending(y => y.IsChecked).ToList();
                _dialogData_Filtered.Clear();
                _dialogData_Filtered.AddRange(sortedList);
            }

            _dialogData_Filtered_OnPage.Clear();
            int iterScreenField = -1;
            foreach (var entry in _dialogData_Filtered)
            {
                iterScreenField++;

                int currPage = GetPageNumberByLine(iterScreenField);
                if (currPage != _currentPage)
                {
                    continue;
                }
                _dialogData_Filtered_OnPage.Add(entry);
            }
        }

        private void GenerateControlPanel()
        {
            var cbShowTechName = new PCheckBox() { Margin = new RectOffset(LeftOffset, RightOffset, TopOffset, BottomOffset) };
            cbShowTechName.InitialState = ShowTechName ? 1 : 0;
            cbShowTechName.Text = "Show tech names";
            cbShowTechName.OnChecked += OnChecked_ShowTechName;

            var lbLinesPerPage = new PLabel("LinesPerPageLabel");
            lbLinesPerPage.Text = "Page size:";

            var txtLinesPerPage = new PTextField("LinesPerPageTxt");
            txtLinesPerPage.Text = _recordsPerPage.ToString();
            txtLinesPerPage.MinWidth = 50;
            txtLinesPerPage.OnTextChanged += OnTextChanged_LinesPerPage;

            var lbFilter = new PLabel("FilterRecordsLabel");
            lbFilter.Text = "Filter:";

            var txtFilter = new PTextField("TextFilterRecords");
            txtFilter.Text = _filterText;
            txtFilter.MinWidth = 200;
            txtFilter.OnTextChanged += OnTextChanged_Filter;

            var btnRefresh = new PButton("BtnFilterRecords") { Margin = new RectOffset(LeftOffset, RightOffset, TopOffset, BottomOffset) };
            btnRefresh.Text = "Refresh";
            btnRefresh.OnClick += OnClick_Refresh;

            var addRemoveDialogSettingsPanel = new PPanel("AddRemoveDialogSettingsPanel") { Direction = PanelDirection.Horizontal, Spacing = SpacingInPixels };
            addRemoveDialogSettingsPanel.AddChild(cbShowTechName);
            addRemoveDialogSettingsPanel.AddChild(lbLinesPerPage);
            addRemoveDialogSettingsPanel.AddChild(txtLinesPerPage);
            addRemoveDialogSettingsPanel.AddChild(lbFilter);
            addRemoveDialogSettingsPanel.AddChild(txtFilter);
            addRemoveDialogSettingsPanel.AddChild(btnRefresh);
            _addRemoveDialog_BodyPanelContents.AddChild(addRemoveDialogSettingsPanel);

            var pageButtonsPanel = new PPanel("PageButtonsPanel")
            {
                Direction = PanelDirection.Horizontal,
                Spacing = SpacingInPixels,
                FlexSize = Vector2.right
            };

            var prevPageBtn = new PButton("BtnPrevPage") { Margin = new RectOffset(LeftOffset, RightOffset, TopOffset, BottomOffset) };
            prevPageBtn.Text = "< Back";
            prevPageBtn.OnClick += OnClick_PrevPage;
            var nextPageBtn = new PButton("BtnNextPage") { Margin = new RectOffset(LeftOffset, RightOffset, TopOffset, BottomOffset) };
            nextPageBtn.Text = "Next >";
            nextPageBtn.OnClick += OnClick_NextPage;

            if (MaxAvailablePageNumber > 1)
            {
                pageButtonsPanel.AddChild(prevPageBtn);
            }

            bool tooMuchPages = false;
            for (int i = 1; i <= MaxAvailablePageNumber; i++)
            {
                if (
                       _currentPage <= 3 && (i <= 6 || i == MaxAvailablePageNumber)
                    || _currentPage > 3 && _currentPage < MaxAvailablePageNumber - 3 && (i == MinAvailablePageNumber || i == MaxAvailablePageNumber || Math.Abs(i - _currentPage) < 3)
                    || _currentPage >= MaxAvailablePageNumber - 3 && (i == MinAvailablePageNumber || i >= MaxAvailablePageNumber - 6 + 1)
                )
                {
                    tooMuchPages = false;
                }
                else
                {
                    if (!tooMuchPages)
                    {
                        var lbTooMuch = new PLabel();
                        lbTooMuch.Text = "...";
                        pageButtonsPanel.AddChild(lbTooMuch);
                        tooMuchPages = true;
                    }
                    continue;
                }

                var pageNumberBtn = new PButton("BtnPage_" + i.ToString())
                {
                    Margin = new RectOffset(LeftOffset, RightOffset, TopOffset, BottomOffset)
                };
                pageNumberBtn.Text = i.ToString();
                pageNumberBtn.OnClick += OnClick_PageNumber;
                if (i == _currentPage)
                {
                    pageNumberBtn.Color = new ColorStyleSetting() {
                        hoverColor = Color.magenta,
                        inactiveColor = Color.magenta
                    };
                }
                pageButtonsPanel.AddChild(pageNumberBtn);
            }

            if (MaxAvailablePageNumber > 1)
            {
                pageButtonsPanel.AddChild(nextPageBtn);
            }

            _addRemoveDialog_BodyPanelContents.AddChild(pageButtonsPanel);
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

                var lCheckbox = new PCheckBox(name: entry.TechName);
                lCheckbox.InitialState = entry.IsChecked;

                if (!ShowTechName)
                {
                    lCheckbox.Text = entry.Caption;
                    lCheckbox.ToolTip = entry.TechName;
                }
                else
                {
                    lCheckbox.Text = entry.TechName;
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
            _addRemoveDialog_BodyPanelContents.AddChild(scrollPane);
        }

        private void OnChecked_RecordItem(GameObject realized, int state)
        {
            int newState = (state + 1) % 2;
            PCheckBox.SetCheckState(realized, newState);
            var checkButton = realized.GetComponentInChildren<MultiToggle>();
            var techName = checkButton.name;

            var record = _dialogData_Filtered_OnPage.Where(x => x.TechName == checkButton.name).FirstOrDefault();

            if (!_modifiedItems.ContainsKey(techName))
            {
                _modifiedItems.Add(techName, record);
            }
            _modifiedItems[techName].IsChecked = newState;
        }

        private void OnDialogClosed(string option)
        {
            if (option != DialogOption_Cancel)
            {
                _dialog_EditConfigJson.ShowTechName = ShowTechName;
                var addRemoveRecords = new List<Tuple<string, bool>>();
                foreach (var entry in _modifiedItems.Values)
                {
                    addRemoveRecords.Add(new Tuple<string, bool>(entry.TechName, (entry.IsChecked == 1)));
                }
                _dialog_EditConfigJson.ApplyChanges(addRemoveRecords);
            }

            _dialog_EditConfigJson.RebuildBodyAndShow();
        }

        private void OnTextChanged_LinesPerPage(GameObject source, string text)
        {
            if (int.TryParse(text, out var count))
            {
                _recordsPerPage = count;
            }
        }

        private void OnChecked_ShowTechName(GameObject source, int state)
        {
            int newState = (state + 1) % 2;
            PCheckBox.SetCheckState(source, newState);
            ShowTechName = (newState == 1);

            _currentPage = MinAvailablePageNumber;

            RebuildAndShow();
        }

        private void OnClick_PageNumber(GameObject source)
        {
            try
            {
                int parsedInt = 0;
                if (int.TryParse(source.name.Split('_')[1], out parsedInt))
                {
                    _currentPage = parsedInt;
                }

                RebuildAndShow();
            }
            catch (Exception e)
            {
                Debug.Log("OnPageNumberClick ERROR " + e.Message);
            }
        }

        private void OnClick_PrevPage(GameObject source)
        {
            if (_currentPage <= MinAvailablePageNumber)
            {
                return;
            }

            _currentPage--;

            RebuildAndShow();
        }

        private void OnClick_NextPage(GameObject source)
        {
            if (_currentPage >= MaxAvailablePageNumber)
            {
                return;
            }

            _currentPage++;

            RebuildAndShow();
        }

        private void OnTextChanged_Filter(GameObject source, string text)
        {
            _filterText = text;
        }

        private void OnClick_Refresh(GameObject source)
        {
            _currentPage = MinAvailablePageNumber;

            RebuildAndShow();
        }
    }
}