using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;

namespace ExtendedBuildingWidth
{
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