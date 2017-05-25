using System;
using System.Collections.Generic;

using RimWorld;
using Verse;
using Verse.AI;

namespace SeedsPlease
{
	public class WorkGiver_GrowerSowWithSeeds : WorkGiver_GrowerSow
	{
		public override Job JobOnCell (Pawn pawn, IntVec3 c)
		{
			Job job = base.JobOnCell (pawn, c);
			if (job != null && job.plantDefToSow != null && job.plantDefToSow.blueprintDef != null) {
				Predicate<Thing> predicate = (Thing tempThing) =>
					!ForbidUtility.IsForbidden (tempThing, pawn.Faction)
					&& PawnLocalAwareness.AnimalAwareOf (pawn, tempThing)
					&& ReservationUtility.CanReserve (pawn, tempThing, 1);

				Thing bestSeedThingForSowing = GenClosest.ClosestThingReachable (
					c, pawn.Map, ThingRequest.ForDef (job.plantDefToSow.blueprintDef), 
					PathEndMode.ClosestTouch, TraverseParms.For (pawn, Danger.Deadly, TraverseMode.ByPawn, false), 9999,
	                predicate);
				
				if (bestSeedThingForSowing != null) {
					return new Job (LocalJobDefOf.SowWithSeeds, c, bestSeedThingForSowing) {
						plantDefToSow = job.plantDefToSow,
						count = 25
					};
				}
				return null;
			}

			return job;
		}
	}
}
