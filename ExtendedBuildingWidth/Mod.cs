using HarmonyLib;
using KMod;
using PeterHan.PLib.Actions;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using static ExtendedBuildingWidth.STRINGS.UI;

namespace ExtendedBuildingWidth
{
    /// <summary>
    /// How this mod works:
    /// 1) When the game is just launched (before Main Menu is shown), method GeneratedBuildings.LoadGeneratedBuildings is called.
    ///    Standard BuildingDef objects are instantinated there based on all IBuildingConfig implementations.
    ///    Additional BuildingDefs with extended width are created dynamically in Postfix of that method.
    /// 2) When a save file is loaded (or new game started), player is able to change width of particular buildings.
    ///    To do so, first a building must be selected from Build Menu (for example, a Gas Bridge), and then ALT+X / ALT+C pressed to switch widths.
    ///    The list of buildings that could be extended is defined in ModSettings class.
    ///    Key listening is handled in Prefix of Patch_BuildTool_OnKeyDown.
    /// </summary>
    public class Mod : UserMod2
    {
        public static PAction PAction_GetSmallerBuilding;

        public static PAction PAction_GetBiggerBuilding;

        public override void OnLoad(Harmony harmony)
        {
            PUtil.InitLibrary();

            var options = new POptions();
            options.RegisterOptions(this, typeof(ModSettings));

            var actionManager = new PActionManager();
            PAction_GetSmallerBuilding = actionManager.CreateAction(
                "ExtendedBuildingWidth.GetSmallerBuilding",
                MOD.ACTION_TITLE_GETSMALLERBUILDING,
                new PKeyBinding(KKeyCode.X, Modifier.Alt)
            );
            PAction_GetBiggerBuilding = actionManager.CreateAction(
                "ExtendedBuildingWidth.GetBiggerBuilding",
                MOD.ACTION_TITLE_GETBIGGERBUILDING,
                new PKeyBinding(KKeyCode.C, Modifier.Alt)
            );

            base.OnLoad(harmony);
        }
    }
}