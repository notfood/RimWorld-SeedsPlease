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
			var job = base.JobOnCell (pawn, c);

			// plant has seeds, if there is a seed return a job, otherwise prevent it.
			if (job != null && job.plantDefToSow != null && job.plantDefToSow.blueprintDef != null) {

				// Don't start until the zone is clear from blockers we can cut/move
				var zone = c.GetZone (pawn.Map);
				if (zone != null) {
					foreach (var thing in zone.AllContainedThings) {
						if (thing.def != job.plantDefToSow && thing.def.BlockPlanting && pawn.CanReserve (thing) && !thing.IsForbidden (pawn)) {
							if (thing.def.category == ThingCategory.Plant) {
								return new Job (JobDefOf.CutPlant, thing);
							} else if (thing.def.EverHaulable) {
								return HaulAIUtility.HaulAsideJobFor (pawn, thing);
							}
						}
					}
				}

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
