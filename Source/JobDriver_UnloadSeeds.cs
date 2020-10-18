using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using System.Linq;

namespace SeedsPlease
{
    /// <summary>
    /// Taken from PickUpAndHaul
    /// https://github.com/Mehni/PickUpAndHaul/blob/master/Source/PickUpAndHaul/JobDriver_UnloadYourHauledInventory.cs
    /// </summary>
    public class JobDriver_UnloadSeeds : JobDriver
    {
        private int countToDrop = -1;
        private int unloadDuration = 3;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref countToDrop, "countToDrop", -1);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
            => true;

        /// <summary>
        /// Find spot, reserve spot, pull thing out of inventory, go to spot, drop stuff, repeat.
        /// </summary>
        /// <returns></returns>
        protected override IEnumerable<Toil> MakeNewToils()
        {
            var carriedSeeds = pawn.inventory.innerContainer.Where(t => t.def is SeedDef).ToList();

            Toil wait = Toils_General.Wait(unloadDuration);
            Toil celebrate = Toils_General.Wait(unloadDuration);

            yield return wait;
            Toil findSpot = new Toil
            {
                initAction = () =>
                {
                    ThingCount unloadableThing = FirstUnloadableThing(pawn);

                    if (unloadableThing.Count == 0 && carriedSeeds.Count == 0)
                        EndJobWith(JobCondition.Succeeded);

                    if (unloadableThing.Count != 0)
                    {
                        //StoragePriority currentPriority = StoreUtility.StoragePriorityAtFor(pawn.Position, unloadableThing.Thing);
                        if (!StoreUtility.TryFindStoreCellNearColonyDesperate(unloadableThing.Thing, pawn, out IntVec3 c))
                        {
                            pawn.inventory.innerContainer.TryDrop(unloadableThing.Thing, ThingPlaceMode.Near, unloadableThing.Thing.stackCount, out Thing _);
                            EndJobWith(JobCondition.Succeeded);
                        }
                        else
                        {
                            job.SetTarget(TargetIndex.A, unloadableThing.Thing);
                            job.SetTarget(TargetIndex.B, c);
                            countToDrop = unloadableThing.Thing.stackCount;
                        }
                    }
                }
            };
            yield return findSpot;

            yield return Toils_Reserve.Reserve(TargetIndex.B);

            yield return new Toil
            {
                initAction = delegate
                {
                    Thing thing = job.GetTarget(TargetIndex.A).Thing;
                    if (thing == null || !pawn.inventory.innerContainer.Contains(thing))
                    {
                        carriedSeeds.Remove(thing);
                        pawn.jobs.curDriver.JumpToToil(wait);
                        return;
                    }
                    if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) || !thing.def.EverStorable(false))
                    {
                        pawn.inventory.innerContainer.TryDrop(thing, ThingPlaceMode.Near, countToDrop, out thing);
                        EndJobWith(JobCondition.Succeeded);
                        carriedSeeds.Remove(thing);
                    }
                    else
                    {
                        pawn.inventory.innerContainer.TryTransferToContainer(thing, pawn.carryTracker.innerContainer, countToDrop, out thing);
                        job.count = countToDrop;
                        job.SetTarget(TargetIndex.A, thing);
                        carriedSeeds.Remove(thing);
                    }
                    thing.SetForbidden(false, false);
                }
            };

            Toil carryToCell = Toils_Haul.CarryHauledThingToCell(TargetIndex.B);
            yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.Touch);
            yield return carryToCell;
            yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.B, carryToCell, true);

            //If the original cell is full, PlaceHauledThingInCell will set a different TargetIndex resulting in errors on yield return Toils_Reserve.Release.
            //We still gotta release though, mostly because of Extended Storage.
            Toil releaseReservation = new Toil
            {
                initAction = () =>
                {
                    if (pawn.Map.reservationManager.ReservedBy(job.targetB, pawn, pawn.CurJob))
                        pawn.Map.reservationManager.Release(job.targetB, pawn, pawn.CurJob);
                }
            };
            yield return releaseReservation;
            yield return Toils_Jump.Jump(wait);
            yield return celebrate;
        }

        private static ThingCount FirstUnloadableThing(Pawn pawn)
        {
            var carriedSeeds = pawn.inventory.innerContainer.Where(t => t.def is SeedDef).ToList();

            //find the overlap.
            IEnumerable<Thing> potentialThingsToUnload =
                from t in pawn.inventory.innerContainer
                where carriedSeeds.Contains(t)
                select t;

            foreach (Thing thing in carriedSeeds.OrderBy(t => t.def.FirstThingCategory?.index))
            {
                //merged partially picked up stacks get a different thingID in inventory
                if (!potentialThingsToUnload.Contains(thing))
                {
                    ThingDef stragglerDef = thing.def;
                    //we have no method of grabbing the newly generated thingID. This is the solution to that.
                    IEnumerable<Thing> dirtyStragglers =
                        from straggler in pawn.inventory.innerContainer
                        where straggler.def == stragglerDef
                        select straggler;

                    carriedSeeds.Remove(thing);

                    foreach (Thing dirtyStraggler in dirtyStragglers)
                        return new ThingCount(dirtyStraggler, dirtyStraggler.stackCount);
                }
                return new ThingCount(thing, thing.stackCount);
            }
            return default;
        }
    }
}