using HarmonyLib;
using PeterHan.PLib.Lighting;

namespace UVCleaner
{
	public static class Patch_GeneratedBuildings
	{
		//public static ILightShape uvlight;

		[HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
		public static class GeneratedBuildings_LoadGeneratedBuildings_Patch
		{
			public static void Prefix()
			{
				AddBuildingStrings("UVCleaner", UVCleanerConfig.DISPLAY_NAME, UVCleanerConfig.DESCRIPTION, UVCleanerConfig.EFFECT);
				ModUtil.AddBuildingToPlanScreen("Medical", "UVCleaner", "cleaning", "WashSink", ModUtil.BuildingOrdering.After);
			}
		}

		public static void AddBuildingStrings(string id, string name, string desc, string effect)
		{
			string str = id.ToUpperInvariant();
			Strings.Add(new string[]
			{
				"STRINGS.BUILDINGS.PREFABS." + str + ".NAME", STRINGS.UI.FormatAsLink(name, id)
			});
			Strings.Add(new string[] {
				"STRINGS.BUILDINGS.PREFABS." + str + ".DESC", desc
			});
			Strings.Add(new string[]
			{
				"STRINGS.BUILDINGS.PREFABS." + str + ".EFFECT", effect
			});
		}
	}
}