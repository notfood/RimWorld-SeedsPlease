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

		public override void ResolveReferences() {
			base.ResolveReferences ();

			if (plant != null && plant.blueprintDef == null) {
				plant.blueprintDef = this;

				if (harvest != null) {
					plant.plant.harvestedThingDef = harvest;
				} else {
					harvest = plant.plant.harvestedThingDef;
				}
			}

			if (BaseMarketValue == 0) {
				var harvestedThingDef = plant.plant.harvestedThingDef;

				var value = harvestedThingDef.BaseMarketValue * plant.plant.harvestYield;

				if (plant.plant.blockAdjacentSow) {
					value /= 9f;
				}

				if (harvestedThingDef == ThingDefOf.WoodLog) {
					value *= 2f;
				} else if (harvestedThingDef.IsAddictiveDrug) {
					value *= 3f;
				} else if (harvestedThingDef.IsDrug) {
					value *= 2f;
				} else if (harvestedThingDef.IsMedicine) {
					value *= 1.5f;
				}

				value *= Mathf.Lerp(0.8f, 1.6f, (float) plant.plant.sowMinSkill / 20f);

				BaseMarketValue = Mathf.Ceil(value / 5f) * 5f;

				#if(DEBUG)
				Log.Message ("\t" + plant + " => " + BaseMarketValue);
				#endif
			}

		}
	}
}