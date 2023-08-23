using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;

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
        public override void OnLoad(Harmony harmony)
        {
            PUtil.InitLibrary();

            var options = new POptions();
            options.RegisterOptions(this, typeof(ModSettings));

            base.OnLoad(harmony);
        }
    }
}