using System;
using System.Collections.Generic;
using Klei;
using KSerialization;
using UnityEngine;

namespace UVCleaner
{
	[SerializationConfig(MemberSerialization.OptIn)]
	public class UVCleaner : KMonoBehaviour, IGameObjectEffectDescriptor, ISim200ms
	{
		public void Sim200ms(float dt)
		{
			if (this.operational != null && !this.operational.IsOperational)
			{
				this.operational.SetActive(false, false);
				return;
			}
			this.UpdateState();
		}

		protected override void OnPrefabInit()
		{
			base.OnPrefabInit();
			base.Subscribe<UVCleaner>(-592767678, UVCleaner.OnOperationalChangedDelegate);
			base.Subscribe<UVCleaner>(824508782, UVCleaner.OnActiveChangedDelegate);
		}

		protected override void OnSpawn()
		{
			base.OnSpawn();
			this.waterOutputCell = this.building.GetUtilityOutputCell();
			//this.CreateNewReactable();
		}

		//protected override void OnCleanUp()
		//{
		//	if (this.reactable != null)
		//	{
		//		this.reactable.Cleanup();
		//		this.reactable = null;
		//	}
		//}

		//public void CreateNewReactable()
		//{
		//	this.reactable = new SunburnReactable(this);
		//}

		private void UpdateState()
		{
			bool value = this.consumer.IsSatisfied;
			byte idx = SimUtil.DiseaseInfo.Invalid.idx;
			using (List<GameObject>.Enumerator enumerator = this.storage.items.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					PrimaryElement primaryElement;
					float mass;
					if (enumerator.Current.TryGetComponent<PrimaryElement>(out primaryElement) && (mass = primaryElement.Mass) > 0f && primaryElement.Element.IsLiquid)
					{
						byte b = primaryElement.DiseaseIdx;
						int diseaseCount = primaryElement.DiseaseCount;
						int num = Mathf.RoundToInt(0.050000012f * (float)diseaseCount);
						if (b == idx || (float)num < 250f)
						{
							num = 0;
							b = idx;
						}
						float num2 = Game.Instance.liquidConduitFlow.AddElement(this.waterOutputCell, primaryElement.ElementID, mass, primaryElement.Temperature, b, num);
						value = true;
						primaryElement.KeepZeroMassObject = true;
						int num3 = Mathf.RoundToInt((float)diseaseCount * (num2 / mass));
						primaryElement.Mass = mass - num2;
						primaryElement.ModifyDiseaseCount(-num3, "UVCleaner.UpdateState");
						break;
					}
				}
			}
			this.operational.SetActive(value, false);
			this.UpdateStatus();
		}

		private static void OnOperationalChanged(UVCleaner component, object data)
		{
			if (component.operational.IsOperational)
			{
				component.UpdateState();
			}
		}

		private static void OnActiveChanged(UVCleaner component, object data)
		{
			component.UpdateStatus();
		}

		private void UpdateStatus()
		{
			if (!this.operational.IsActive)
			{
				if (this.statusHandle != Guid.Empty)
				{
					this.statusHandle = this.selectable.RemoveStatusItem(this.statusHandle, false);
				}
				return;
			}
			if (this.statusHandle == Guid.Empty)
			{
				this.statusHandle = this.selectable.SetStatusItem(Db.Get().StatusItemCategories.Main, Db.Get().BuildingStatusItems.Working, null);
				return;
			}
			this.selectable.ReplaceStatusItem(this.statusHandle, Db.Get().BuildingStatusItems.Working, null);
		}

		public List<Descriptor> GetDescriptors(GameObject go)
		{
			return UVCleaner.EMPTY_LIST;
		}

		private static readonly List<Descriptor> EMPTY_LIST = new List<Descriptor>();

		public const float GERM_REMOVAL = 0.95f;

		public const float MIN_GERMS_PER_KG = 50f;

		//private SunburnReactable reactable;

		[MyCmpReq]
		private BuildingComplete building;

		[MyCmpReq]
		private ConduitConsumer consumer;

		[MyCmpReq]
		private KSelectable selectable;

		[MyCmpReq]
		internal Operational operational;

		[MyCmpReq]
		private Storage storage;

		private int waterOutputCell = -1;

		private Guid statusHandle;

		private static readonly EventSystem.IntraObjectHandler<UVCleaner> OnOperationalChangedDelegate = new EventSystem.IntraObjectHandler<UVCleaner>(new Action<UVCleaner, object>(UVCleaner.OnOperationalChanged));

		private static readonly EventSystem.IntraObjectHandler<UVCleaner> OnActiveChangedDelegate = new EventSystem.IntraObjectHandler<UVCleaner>(new Action<UVCleaner, object>(UVCleaner.OnActiveChanged));
	}
}