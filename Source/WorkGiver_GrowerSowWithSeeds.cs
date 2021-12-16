using System;
using RimWorld;
using Verse;
using Verse.AI;
using System.Linq;

namespace SeedsPleaseLite
{
    public class WorkGiver_GrowerSowWithSeeds : WorkGiver_GrowerSow
    {
        const int SEEDS_TO_CARRY = 25;

    	public override Job JobOnCell(Pawn pawn, IntVec3 c, bool forced = false)
        {
            Job job = base.JobOnCell(pawn, c, forced);

            // plant has seeds, if there is a seed return a job, otherwise prevent it. Seeds with no category are forbidden.
            ThingDef seed = job?.plantDefToSow?.blueprintDef;
            if (!seed?.thingCategories.NullOrEmpty() ?? false)
            {
                Map map = pawn.Map;

                // Clear the area
                foreach (IntVec3 cell in c.GetZone(map)?.cells ?? Enumerable.Empty<IntVec3>())
                {
                    foreach (Thing thing in map.thingGrid.ThingsListAtFast(cell))
                    {
                        if (thing.def != job.plantDefToSow && thing.def.BlocksPlanting(true) && pawn.CanReserve(thing) && !thing.IsForbidden(pawn))
                        {
                            if (thing.def.category == ThingCategory.Plant) return new Job(JobDefOf.CutPlant, thing);
                            if (thing.def.EverHaulable) return HaulAIUtility.HaulAsideJobFor(pawn, thing);
                        }
                    }
                }

                //Predicate filtering the kind of seed allowed
                Predicate<Thing> predicate = (Thing tempThing) =>
                    !ForbidUtility.IsForbidden (tempThing, pawn.Faction)
                    && ForbidUtility.InAllowedArea(tempThing.Position, pawn)
                    && PawnLocalAwareness.AnimalAwareOf (pawn, tempThing)
                    && ReservationUtility.CanReserve (pawn, tempThing, 1);

                //Find the instance on the map to go fetch
                Thing bestSeedThingForSowing = GenClosest.ClosestThingReachable (
                    c, map, ThingRequest.ForDef(seed),
                    PathEndMode.ClosestTouch, TraverseParms.For (pawn, Danger.Deadly, TraverseMode.ByPawn, false), 9999,
                    predicate);

                if (bestSeedThingForSowing != null)
                {
                    return new Job(ResourceBank.JobDefOf.SowWithSeeds, c, bestSeedThingForSowing)
                    {
                        plantDefToSow = job.plantDefToSow,
                        count = SEEDS_TO_CARRY
                    };
                }
                return null;
            }

            return job;
        }
    }
}