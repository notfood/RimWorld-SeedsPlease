using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using RimWorld;
using Verse;

namespace SeedsPlease
{
	public class SeedDef : ThingDef
	{
		public SeedProperties seed;
		public new ThingDef plant;
		public ThingDef harvest;

		public override void ResolveReferences ()
		{
			base.ResolveReferences ();

			if (plant == null || plant.blueprintDef != null) {
				return;
			}

			if (plant.plant.Sowable) {
				plant.blueprintDef = this;
			} else {
				Log.Warning ("SeedsPlease :: " + plant.defName + " is not sowable");
				plant = null;
				return;
			}

			if (harvest != null) {
				plant.plant.harvestedThingDef = harvest;
			} else {
				harvest = plant.plant.harvestedThingDef;
			}

			if (BaseMarketValue == 0) {
				var harvestedThingDef = harvest;

				var value = harvestedThingDef.BaseMarketValue * (plant.plant.harvestYield / plant.plant.growDays + plant.plant.growDays / plant.plant.harvestYield) * 2.5f;

				if (plant.plant.blockAdjacentSow) {
					value *= 9f;
				}

				if (harvestedThingDef == ThingDefOf.WoodLog) {
					value *= 0.2f;
				} else if (harvestedThingDef.IsAddictiveDrug) {
					value *= 1.3f;
				} else if (harvestedThingDef.IsDrug) {
					value *= 1.2f;
				} else if (harvestedThingDef.IsMedicine) {
					value *= 1.1f;
				}

				value *= Mathf.Lerp (0.8f, 1.6f, (float)plant.plant.sowMinSkill / 20f);

				BaseMarketValue = Mathf.Round (value * 100f) / 100f;
			}

			#if DEBUG
			Log.Message ("\t" + plant + " => " + BaseMarketValue);
			#endif
		}
	}
}