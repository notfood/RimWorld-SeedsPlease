using System;
using System.Text;
using System.Collections.Generic;
using System.Collections;
using System.Linq;


using RimWorld;
using Verse;

namespace SeedsPlease
{
	public class Seed : ThingWithComps
	{
		public override IEnumerable<StatDrawEntry> SpecialDisplayStats {
			get {
				SeedDef seedDef = def as SeedDef;
				if (seedDef != null) {
					StatDrawEntry[] extraStats = new StatDrawEntry[] { 
						new StatDrawEntry (StatCategoryDefOf.StuffStatFactors, ResourceBank.StringHarvestMultiplier , seedDef.seed.harvestFactor.ToString ("F2"), 4),
						new StatDrawEntry (StatCategoryDefOf.StuffStatFactors, ResourceBank.StringSeedMultiplier, seedDef.seed.seedFactor.ToString ("F2"), 3),
						new StatDrawEntry (StatCategoryDefOf.StuffStatFactors, ResourceBank.StringSeedBaseChance, seedDef.seed.baseChance.ToString ("F2") + " %", 2),
						new StatDrawEntry (StatCategoryDefOf.StuffStatFactors, ResourceBank.StringSeedExtraChance, seedDef.seed.extraChance.ToString ("F2") + " %", 1),
					};
					return base.SpecialDisplayStats.Concat (extraStats);
				}

				return base.SpecialDisplayStats;
			}
		}
	}
}

