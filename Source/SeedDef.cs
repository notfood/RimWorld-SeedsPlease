using System;

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
		}
	}
}