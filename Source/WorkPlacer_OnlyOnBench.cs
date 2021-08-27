using Verse;
using RimWorld;
using System.Collections.Generic;

namespace SeedsPlease
{
	public class WorkPlacer_OnlyOnBench : PlaceWorker
	{
		public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
		{		
			//Try to determine if this is a workbench that deals with food
			IEnumerable<Thing> building = map.thingGrid.ThingsAt(loc);
			foreach (Thing thinghere in building)
			{
				if (thinghere?.def.thingClass == typeof(Building_WorkTable_HeatPush) && thinghere.def.building.isMealSource) return true;
			}

			return new AcceptanceReport("Must be placed on a stove's surface.");
		}

		public WorkPlacer_OnlyOnBench()
		{
		}
	}
}
