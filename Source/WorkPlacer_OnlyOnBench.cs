using Verse;
using RimWorld;
using System.Collections.Generic;

namespace SeedsPleaseLite
{
	public class WorkPlacer_OnlyOnBench : PlaceWorker
	{
		public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
		{		
			//Try to determine if this is a workbench that deals with food
			IEnumerable<Thing> building = map.thingGrid.ThingsAt(loc);
			foreach (var thinghere in building)
			{
				if (thinghere.def.building != null && thinghere.def.building.isMealSource && thinghere.def.thingClass != typeof(Building_NutrientPasteDispenser)) return true;
			}

			return new AcceptanceReport("Must be placed on a stove's surface.");
		}

		public WorkPlacer_OnlyOnBench()
		{
		}
	}
}
