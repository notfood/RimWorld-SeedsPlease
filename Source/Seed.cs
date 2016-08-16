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
						new StatDrawEntry (StatCategoryDefOf.StuffStatFactors, "Harvest amount factor", seedDef.seed.harvestFactor.ToString ("F2"), 4),
						new StatDrawEntry (StatCategoryDefOf.StuffStatFactors, "Seed return factor", seedDef.seed.seedFactor.ToString ("F2"), 3),
						new StatDrawEntry (StatCategoryDefOf.StuffStatFactors, "Chance for seeds", seedDef.seed.baseChance.ToString ("F2") + " %", 2),
						new StatDrawEntry (StatCategoryDefOf.StuffStatFactors, "Chance for extra seeds", seedDef.seed.extraChance.ToString ("F2") + " %", 1),
					};
					return base.SpecialDisplayStats.Concat (extraStats);
				}

				return base.SpecialDisplayStats;
			}
		}
	}
}

