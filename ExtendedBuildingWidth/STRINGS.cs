namespace ExtendedBuildingWidth
{
    public class STRINGS
    {
        public class UI
        {
            public class MODSETTINGS
            {
                public static LocString BUTTON_STARTDIALOG_CONFIGMAIN = "Main settings";
                public static LocString BUTTON_STARTDIALOG_ANIMSLICING = "Visual output settings";
                public static LocString BUTTON_CREATEALLBUILDINGNAMES = "Create list of all in-game buildings";
                public static LocString BUTTON_CREATEALLBUILDINGNAMES_TOOLTIP = "Create a text file with all available buildings in the game";
            }

            public class MOD
            {
                public static LocString ACTION_TITLE_GETSMALLERBUILDING = "Get smaller building";
                public static LocString ACTION_TITLE_GETBIGGERBUILDING = "Get bigger building";
            }

            public class DIALOG_COMMON_STR
            {
                public static LocString BUTTON_OK = "OK";
                public static LocString BUTTON_CANCEL = "CANCEL";
                public static LocString CHECKBOX_SHOWTECHNAMES = "Show tech names";
            }

            public class DIALOG_EDIT_MAINSETTINGS
            {
                public static LocString DIALOG_TITLE = "Main settings";
                public static LocString LABEL_CONFIGNAME = "Config name";
                public static LocString GRIDCOLUMN_MAXWIDTH1 = "Max";
                public static LocString GRIDCOLUMN_MAXWIDTH2 = "width";
                public static LocString GRIDCOLUMN_STRETCHKOEF1 = "Stretch";
                public static LocString GRIDCOLUMN_STRETCHKOEF2 = "koef";
                public static LocString BUTTON_STARTDIALOGADDREMOVE = "Add / remove records";
            }

            public class DIALOG_EDIT_ANIMSLICINGSETTINGS
            {
                public static LocString DIALOG_TITLE = "Visual output settings";
                public static LocString GRIDCOLUMN_CONFIGNAME = "Config name";
                public static LocString GRIDCOLUMN_CONFIGNAME_TOOLTIP = "";
                public static LocString GRIDCOLUMN_OPENFIELDSFOREDITING1 = "Edit";
                public static LocString GRIDCOLUMN_OPENFIELDSFOREDITING2 = "";
                public static LocString GRIDCOLUMN_OPENFIELDSFOREDITING_TOOLTIP = "";
                public static LocString GRIDCOLUMN_ADDREC = "+";
                public static LocString GRIDCOLUMN_ADDREC_TOOLTIP = "";
                public static LocString GRIDCOLUMN_DELREC = "-";
                public static LocString GRIDCOLUMN_DELREC_TOOLTIP = "";
                public static LocString GRIDCOLUMN_SYMBOL1 = "Symbol";
                public static LocString GRIDCOLUMN_SYMBOL2 = "";
                public static LocString GRIDCOLUMN_SYMBOL_TOOLTIP = "";
                public static LocString GRIDCOLUMN_FRAME1 = "Frame";
                public static LocString GRIDCOLUMN_FRAME2 = "";
                public static LocString GRIDCOLUMN_FRAME_TOOLTIP = "";
                public static LocString GRIDCOLUMN_ISACTIVE1 = "Enabled";
                public static LocString GRIDCOLUMN_ISACTIVE2 = "";
                public static LocString GRIDCOLUMN_ISACTIVE_TOOLTIP1 = "Checked: dynamic buildings of this config will be drawn according to these settings.";
                public static LocString GRIDCOLUMN_ISACTIVE_TOOLTIP2 = "Unchecked: dynamic buildings of this config will be drawn old style (fully stretched from left to right).";
                public static LocString GRIDCOLUMN_FILLINGSTYLE1 = "Template's";
                public static LocString GRIDCOLUMN_FILLINGSTYLE2 = "filling style";
                public static LocString GRIDCOLUMN_FILLINGSTYLE_TOOLTIP1 = "Stretch: Template will be drawn stretched between left and right parts.";
                public static LocString GRIDCOLUMN_FILLINGSTYLE_TOOLTIP2 = "Repeat: Template will be drawn repeatedly from left part to right part.";
                public static LocString GRIDCOLUMN_MIDDLEXPOS1 = "Template's";
                public static LocString GRIDCOLUMN_MIDDLEXPOS2 = "pos.X";
                public static LocString GRIDCOLUMN_MIDDLEXPOS_TOOLTIP = "Pos.X (in pixels) in the original texture (PNG file), where the Template starts.";
                public static LocString GRIDCOLUMN_MIDDLEWIDTH1 = "Template's";
                public static LocString GRIDCOLUMN_MIDDLEWIDTH2 = "width";
                public static LocString GRIDCOLUMN_MIDDLEWIDTH_TOOLTIP = "Width (in pixels) in the original texture (PNG file), that defines Template's width.";
                public static LocString GRIDCOLUMN_FLIP1 = "Flip every";
                public static LocString GRIDCOLUMN_FLIP2 = "second time";
                public static LocString GRIDCOLUMN_FLIP_TOOLTIP = "When 'Repeat' Filling style is chosen: Template will be flipped horizontally every second time it is drawn.";
                public static LocString GRIDCOLUMN_PREVIEW1 = "Preview";
                public static LocString GRIDCOLUMN_PREVIEW2 = "";
                public static LocString GRIDCOLUMN_PREVIEW_TOOLTIP = "";
                public static LocString BUTTON_OPENFIELDSFOREDITING = "edit";
                public static LocString BUTTON_ADDREC = "+";
                public static LocString BUTTON_DELREC = "-";
                public static LocString BUTTON_PREVIEW = "preview";
                public static LocString LABEL_ORIGXPOS = "Original frame's pos.X:";
                public static LocString LABEL_ORIGWIDTH = "Original frame's width:";
                public static LocString LABEL_DYNAMICSIZE = "Dynamic building size:";
                public static LocString BUTTON_PREVIEWMAKESMALLER = "<";
                public static LocString BUTTON_PREVIEWMAKEBIGGER = ">";
                public static LocString LABEL_PREVIEWLEFTPART = "Left part";
                public static LocString LABEL_PREVIEWRIGHTPART = "Right part";
                public static LocString LABEL_PREVIEWMIDDLEPART = "Template";
                public static LocString CHECKBOX_SYMBOLFRAMEDROPDOWNS = "Show Dropdowns for Symbols and Frames";
                public static LocString BUTTON_STARTDIALOGADDREMOVE = "Add / remove records";
            }

            public class DIALOG_ADDREMOVE_CONFIGMAIN
            {
                public static LocString DIALOG_TITLE = "Add / remove records";
                public static LocString LABEL_PAGESIZE = "Page size:";
                public static LocString LABEL_FILTER = "Filter:";
                public static LocString BUTTON_REFRESH = "Refresh";
                public static LocString BUTTON_PREVPAGE = "< Back";
                public static LocString LABEL_PERIODS = "...";
                public static LocString BUTTON_NEXTPAGE = "Next >";
            }

            public class DIALOG_ADDREMOVE_ANIMSLICING
            {
                public static LocString DIALOG_TITLE = "Add / remove records";
            }
        }
    }
}