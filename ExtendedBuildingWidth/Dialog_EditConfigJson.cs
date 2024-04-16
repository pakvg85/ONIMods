using PeterHan.PLib.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ExtendedBuildingWidth
{
    public class Dialog_EditConfigJson
    {
        private class EditConfigDialog_Item
        {
            public string TechName { get; set; }
            public int MinWidth { get; set; }
            public int MaxWidth { get; set; }
            public float AnimStretchModifier { get; set; }
        }

        private readonly List<EditConfigDialog_Item> _dialogData = new List<EditConfigDialog_Item>();

        private PPanel _dialogBodyChild = null;
        private PPanel _configJsonDialogBody = null;
        private KScreen _componentScreenEditConfig = null;
        private PDialog _editConfigJson_PDialog = null;

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
                Title = "Edit Config Json",
                DialogClosed = OnDialogClosed,
                Size = new Vector2 { x = 800, y = 600 },
                MaxSize = new Vector2 { x = 800, y = 600 },
                SortKey = 200.0f
            }.AddButton(DialogOption_Ok, "OK", null, PUITuning.Colors.ButtonPinkStyle)
            .AddButton(DialogOption_Cancel, "CANCEL", null, PUITuning.Colors.ButtonBlueStyle);

            GenerateData();

            _editConfigJson_PDialog = dialog;
            _configJsonDialogBody = dialog.Body;

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

            _configJsonDialogBody.AddChild(_dialogBodyChild);

            _componentScreenEditConfig = null;
            var isBuilt = _editConfigJson_PDialog.Build().TryGetComponent<KScreen>(out _componentScreenEditConfig);
            if (isBuilt)
            {
                _componentScreenEditConfig.Activate();
            }
        }

        public List<string> GetTechNames()
        {
            return _dialogData.Select(d => d.TechName).ToList();
        }

        public void ApplyChanges(ICollection<Tuple<string, bool>> modifiedRecords)
        {
            foreach (var entry in modifiedRecords)
            {
                var existingRecord = _dialogData.FirstOrDefault(x => x.TechName == entry.first);

                if (!entry.second)
                {
                    if (existingRecord != null)
                    {
                        _dialogData.Remove(existingRecord);
                    }
                }
                else if (entry.second)
                {
                    if (existingRecord == null)
                    {
                        var newRec = new EditConfigDialog_Item()
                        {
                            TechName = entry.first,
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
                _configJsonDialogBody.RemoveChild(_dialogBodyChild);
            }
            _dialogBodyChild = new PPanel("DialogBodyChild");
        }

        private void GenerateRecordsPanel()
        {
            var tableTitlesPanel = new PGridPanel("EditConfigJsonDialogBody") { Margin = new RectOffset(10, 40, 10, 0) };
            tableTitlesPanel.AddColumn(new GridColumnSpec(440));
            tableTitlesPanel.AddColumn(new GridColumnSpec(80));
            tableTitlesPanel.AddColumn(new GridColumnSpec(80));
            tableTitlesPanel.AddColumn(new GridColumnSpec(100));
            tableTitlesPanel.AddRow(new GridRowSpec());

            int iRow = 0;
            var lbConfigName = new PLabel();
            lbConfigName.Text = "Config name";
            tableTitlesPanel.AddChild(lbConfigName, new GridComponentSpec(iRow, 0));
            var lbMinWidth = new PLabel();
            lbMinWidth.Text = "Min width";
            tableTitlesPanel.AddChild(lbMinWidth, new GridComponentSpec(iRow, 1));
            var lbMaxWidth = new PLabel();
            lbMaxWidth.Text = "Max width";
            tableTitlesPanel.AddChild(lbMaxWidth, new GridComponentSpec(iRow, 2));
            var lbStretchKoef = new PLabel();
            lbStretchKoef.Text = "Stretch koef";
            tableTitlesPanel.AddChild(lbStretchKoef, new GridComponentSpec(iRow, 3));

            _dialogBodyChild.AddChild(tableTitlesPanel);

            var gridPanel = new PGridPanel("EditConfigJsonDialogBody") { Margin = new RectOffset(10, 40, 10, 10) };
            gridPanel.AddColumn(new GridColumnSpec(440));
            gridPanel.AddColumn(new GridColumnSpec(80));
            gridPanel.AddColumn(new GridColumnSpec(80));
            gridPanel.AddColumn(new GridColumnSpec(100));

            foreach (var entry in _dialogData)
            {
                gridPanel.AddRow(new GridRowSpec());
            }

            var dict = SettingsManager.ListOfAllBuildings.ToDictionary(x => x.TechName, y => y);

            iRow = -1;
            foreach (var entry in _dialogData)
            {
                iRow++;
                var bn = new PLabel(entry.TechName);

                if (!ShowTechName)
                {
                    bn.Text = !string.IsNullOrEmpty(dict[entry.TechName].Caption) ? dict[entry.TechName].Caption : entry.TechName;
                    bn.ToolTip = entry.TechName;
                }
                else
                {
                    bn.Text = entry.TechName;
                    bn.ToolTip = !string.IsNullOrEmpty(dict[entry.TechName].Caption) ? dict[entry.TechName].Caption : entry.TechName;
                }

                gridPanel.AddChild(bn, new GridComponentSpec(iRow, 0) { Alignment = TextAnchor.MiddleLeft });

                var minW = new PTextField(entry.TechName);
                minW.Text = entry.MinWidth.ToString();
                minW.OnTextChanged += OnTextChanged_MinWidth;
                minW.MinWidth = 60;
                gridPanel.AddChild(minW, new GridComponentSpec(iRow, 1));

                var maxW = new PTextField(entry.TechName);
                maxW.Text = entry.MaxWidth.ToString();
                maxW.OnTextChanged += OnTextChanged_MaxWidth;
                maxW.MinWidth = 60;
                gridPanel.AddChild(maxW, new GridComponentSpec(iRow, 2));

                var strMdf = new PTextField(entry.TechName);
                strMdf.Text = entry.AnimStretchModifier.ToString();
                strMdf.OnTextChanged += OnTextChanged_AnimStretchModifier;
                strMdf.MinWidth = 90;
                gridPanel.AddChild(strMdf, new GridComponentSpec(iRow, 3));
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
            var cbShowTechName = new PCheckBox();
            cbShowTechName.InitialState = ShowTechName ? 1 : 0;
            cbShowTechName.Text = "Show tech names";
            cbShowTechName.OnChecked += OnChecked_ShowTechName;
            controlPanel.AddChild(cbShowTechName);
            var btnAdd = new PButton();
            btnAdd.Text = "Add or remove records";
            btnAdd.OnClick += OnClick_AddRemoveRecords;
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
                    TechName = entry.ConfigName,
                    MinWidth = entry.MinWidth,
                    MaxWidth = entry.MaxWidth,
                    AnimStretchModifier = entry.AnimStretchModifier
                };
                _dialogData.Add(rec);
            }
        }

        private void OnDialogClosed(string option)
        {
            if (option == DialogOption_Cancel)
            {
                return;
            }

            var newRez = new List<ExtendableConfigSettings>();
            foreach (var entry in _dialogData)
            {
                var rec = new ExtendableConfigSettings()
                {
                    ConfigName = entry.TechName,
                    MinWidth = entry.MinWidth,
                    MaxWidth = entry.MaxWidth,
                    AnimStretchModifier = entry.AnimStretchModifier
                };
                newRez.Add(rec);
            }

            _modSettings.SetExtendableConfigSettings(newRez);
        }

        private void OnTextChanged_MinWidth(GameObject source, string text)
        {
            var record = _dialogData.Where(x => x.TechName == source.name).FirstOrDefault();
            if (record == null)
            {
                return;
            }

            if (int.TryParse(text, out var parsed))
            {
                record.MinWidth = parsed;
            }
        }

        private void OnTextChanged_MaxWidth(GameObject source, string text)
        {
            var record = _dialogData.Where(x => x.TechName == source.name).FirstOrDefault();
            if (record == null)
            {
                return;
            }
            if (int.TryParse(text, out var parsed))
            {
                record.MaxWidth = parsed;
            }
        }

        private void OnTextChanged_AnimStretchModifier(GameObject source, string text)
        {
            var record = _dialogData.Where(x => x.TechName == source.name).FirstOrDefault();
            if (record == null)
            {
                return;
            }
            if (float.TryParse(text, out var parsed))
            {
                record.AnimStretchModifier = parsed;
            }
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
            ShowTechName = (newState == 1);

            RebuildBodyAndShow();
        }
    }
}