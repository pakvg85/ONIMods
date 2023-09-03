using HarmonyLib;

namespace UVCleaner
{
	[HarmonyPatch(typeof(Db), "Initialize")]
	public static class Patch_Db
	{
		public static void Postfix()
		{
			AddBuildingToTech("MedicineIII", "UVCleaner");
		}

		public static void AddBuildingToTech(string tech, string buildingid)
		{
			Db.Get().Techs.Get(tech).unlockedItemIDs.Add(buildingid);
		}
	}
}