using System;
using PeterHan.PLib.Core;
using TUNING;
using UnityEngine;

namespace UVCleaner
{
	public sealed class UVCleanerConfig : IBuildingConfig
	{
		public override BuildingDef CreateBuildingDef()
		{
			BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef("UVCleaner", 3, 3, "uvcleaner_kanim", 100, 120f, BUILDINGS.CONSTRUCTION_MASS_KG.TIER3, MATERIALS.REFINED_METALS, 1600f, BuildLocationRule.OnFloor, BUILDINGS.DECOR.NONE, NOISE_POLLUTION.NOISY.TIER2, 0.2f);
			PGameUtils.CopySoundsToAnim("uvcleaner_kanim", "waterpurifier_kanim");
			BuildingTemplates.CreateElectricalBuildingDef(buildingDef);
			buildingDef.EnergyConsumptionWhenActive = 320f;
			buildingDef.SelfHeatKilowattsWhenActive = 6f;
			buildingDef.InputConduitType = ConduitType.Liquid;
			buildingDef.OutputConduitType = ConduitType.Liquid;
			buildingDef.Floodable = false;
			buildingDef.LogicInputPorts = LogicOperationalController.CreateSingleInputPortList(new CellOffset(1, 1));
			buildingDef.PowerInputOffset = new CellOffset(1, 0);
			buildingDef.UtilityInputOffset = new CellOffset(0, 0);
			buildingDef.PermittedRotations = PermittedRotations.FlipH;
			buildingDef.ViewMode = OverlayModes.LiquidConduits.ID;
			buildingDef.OverheatTemperature = 348.15f;
			return buildingDef;
		}

		public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
		{
			go.AddOrGet<LoopingSounds>();
			ConduitConsumer conduitConsumer = go.AddOrGet<ConduitConsumer>();
			conduitConsumer.conduitType = ConduitType.Liquid;
			conduitConsumer.consumptionRate = 5f;
			Storage storage = BuildingTemplates.CreateDefaultStorage(go, false);
			storage.showInUI = true;
			storage.capacityKg = 2f * conduitConsumer.consumptionRate;
			storage.SetDefaultStoredItemModifiers(Storage.StandardInsulatedStorage);
			go.AddOrGet<UVCleaner>();
		}

		public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
		{
			LightShapePreview lightShapePreview = go.AddOrGet<LightShapePreview>();
			lightShapePreview.lux = UVCleanerConfig.LUX;
			lightShapePreview.radius = (float)UVCleanerConfig.RADIUS;
			lightShapePreview.shape = global::LightShape.Circle;
		}

		public override void DoPostConfigureComplete(GameObject go)
		{
			go.AddOrGet<LogicOperationalController>();
			go.AddOrGetDef<PoweredActiveController.Def>();
			Light2D light2D = go.AddOrGet<Light2D>();
			light2D.overlayColour = LIGHT2D.FLOORLAMP_OVERLAYCOLOR;
			light2D.Color = new Color(0.47058824f, 0.39215687f, 0.47058824f);
			light2D.Range = (float)UVCleanerConfig.RADIUS;
			light2D.Angle = 2.6f;
			light2D.Direction = LIGHT2D.FLOORLAMP_DIRECTION;
			light2D.Offset = new Vector2(0.05f, 2.5f);
			light2D.shape = global::LightShape.Cone;
			light2D.drawOverlay = false;
			light2D.Lux = UVCleanerConfig.LUX;
			go.AddOrGetDef<LightController.Def>();
		}

		// Token: 0x0400027F RID: 639
		public const string ID = "UVCleaner";

		// Token: 0x04000280 RID: 640
		public static LocString DISPLAY_NAME = "UV Cleaner";

		// Token: 0x04000281 RID: 641
		public static LocString DESCRIPTION = "The sun is a deadly laser, blindingly bright and prone to inducing sunburn. Naturally, some duplicants decided to bottle it for water sanitization purposes.";

		// Token: 0x04000282 RID: 642
		public static LocString EFFECT = "Removes almost all Germs from liquids. Emits UV radiation while running, which might burn Duplicants that get too close.";

		// Token: 0x04000283 RID: 643
		public static int LUX = 500;

		// Token: 0x04000284 RID: 644
		public static int RADIUS = 3;
	}
}