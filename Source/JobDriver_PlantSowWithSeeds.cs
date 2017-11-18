using System;
using System.Collections.Generic;

using RimWorld;
using Verse;
using Verse.AI;

namespace SeedsPlease
{
    public class JobDriver_PlantSowWithSeeds : JobDriver
    {
        const TargetIndex targetCellIndex = TargetIndex.A;

        const TargetIndex seedsTargetIndex = TargetIndex.B;

        float sowWorkDone;

        public override string GetReport ()
        {
            string text = JobDefOf.Sow.reportString;
            if (job.plantDefToSow != null) {
                text = text.Replace ("TargetA", GenLabel.ThingLabel (job.plantDefToSow, null, 1));
            } else {
                text = text.Replace ("TargetA", "area");
            }
            return text;
        }

        public override void ExposeData ()
        {
            base.ExposeData ();
            Scribe_Values.Look (ref sowWorkDone, "sowWorkDone", 0f, false);
        }

        protected override IEnumerable<Toil> MakeNewToils ()
        {
            this.FailOnDespawnedNullOrForbidden (TargetIndex.A);

            yield return Toils_Reserve.Reserve (TargetIndex.A, 1);

            var reserveSeeds = ReserveSeedsIfWillPlantWholeStack ();
            yield return reserveSeeds;

            yield return Toils_Goto.GotoThing (TargetIndex.B, PathEndMode.ClosestTouch)
                .FailOnDespawnedNullOrForbidden (TargetIndex.B)
                .FailOnSomeonePhysicallyInteracting (TargetIndex.B);

            yield return Toils_Haul.StartCarryThing (TargetIndex.B, false, false)
                .FailOnDestroyedNullOrForbidden (TargetIndex.B);

            Toils_Haul.CheckForGetOpportunityDuplicate (reserveSeeds, TargetIndex.B, TargetIndex.None, false, null);

            var toil = Toils_Goto.GotoCell (TargetIndex.A, PathEndMode.Touch);
            yield return toil;
            yield return SowSeedToil ();
            yield return Toils_Reserve.Release (TargetIndex.A);
            yield return TryToSetAdditionalPlantingSite ();
            yield return Toils_Reserve.Reserve (TargetIndex.A, 1);
            yield return Toils_Jump.Jump (toil);
        }

        Toil ReserveSeedsIfWillPlantWholeStack ()
        {
            return new Toil {
                initAction = delegate {
                    if (pawn.Faction == null) {
                        return;
                    }
                    var thing = job.GetTarget (TargetIndex.B).Thing;
                    if (pawn.carryTracker.CarriedThing == thing) {
                        return;
                    }
                    if (job.count >= thing.stackCount) {
                        pawn.Reserve (thing, job, 1);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant,
                atomicWithPrevious = true
            };
        }

        Toil SowSeedToil ()
        {
            var toil = new Toil ();
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.initAction = delegate {
                var actor = toil.actor;
                if (IsActorCarryingAppropriateSeed (actor, job.plantDefToSow)) {

                    var plant = (Plant)GenSpawn.Spawn (job.plantDefToSow, TargetLocA, actor.Map);
                    plant.Growth = 0;
                    plant.sown = true;

                    job.targetC = plant;

                    actor.Reserve (job.targetC, job, 1);

                    sowWorkDone = 0;
                } else {
                    EndJobWith (JobCondition.Incompletable);
                }
            };
            toil.tickAction = delegate {
                var actor = toil.actor;

                var plant = (Plant)job.targetC.Thing;

                if (actor.skills != null) {
                    actor.skills.Learn (SkillDefOf.Growing, 0.22f);
                }

                if (plant.LifeStage != PlantLifeStage.Sowing) {
                    Log.Error (this + " getting sowing work while not in Sowing life stage.");
                }

                sowWorkDone += StatExtension.GetStatValue (actor, StatDefOf.PlantWorkSpeed, true);

                if (sowWorkDone >= plant.def.plant.sowWork) {

                    if (!IsActorCarryingAppropriateSeed (actor, job.plantDefToSow)) {
                        EndJobWith (JobCondition.Incompletable);

                        return;
                    }

                    if (actor.carryTracker.CarriedThing.stackCount <= 1) {
                        actor.carryTracker.CarriedThing.Destroy (DestroyMode.Cancel);
                    } else {
                        actor.carryTracker.CarriedThing.stackCount--;
                    }

                    if (actor.story.traits.HasTrait (TraitDefOf.GreenThumb)) {
                        actor.needs.mood.thoughts.memories.TryGainMemory (ThoughtDefOf.GreenThumbHappy, null);
                    }

                    plant.Growth = 0.05f;

                    plant.Map.mapDrawer.MapMeshDirty (plant.Position, MapMeshFlag.Things);

                    actor.records.Increment (RecordDefOf.PlantsSown);

                    ReadyForNextToil ();
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.FailOnDespawnedNullOrForbidden (TargetIndex.A);
            toil.WithEffect (EffecterDefOf.Sow, TargetIndex.A);
            toil.WithProgressBar (TargetIndex.A, () => sowWorkDone / job.plantDefToSow.plant.sowWork, true, -0.5f);
            toil.PlaySustainerOrSound (() => SoundDefOf.Interact_Sow);
            toil.AddFinishAction (delegate {
                var actor = toil.actor;

                var thing = job.targetC.Thing;
                if (thing != null) {
                    var plant = (Plant)thing;
                    if (sowWorkDone < plant.def.plant.sowWork && !thing.Destroyed) {
                        thing.Destroy (DestroyMode.Vanish);
                    }

                    actor.Map.reservationManager.Release (job.targetC, actor, job);

                    job.targetC = null;
                }
            });
            return toil;
        }

        Toil TryToSetAdditionalPlantingSite ()
        {
            var toil = new Toil ();
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            toil.initAction = delegate {
                Pawn actor = toil.actor;

                IntVec3 intVec;
                if (IsActorCarryingAppropriateSeed (actor, job.plantDefToSow) && GetNearbyPlantingSite (job.GetTarget (TargetIndex.A).Cell, actor.Map, out intVec)) {
                    job.SetTarget (TargetIndex.A, intVec);

                    return;
                }

                EndJobWith (JobCondition.Incompletable);
            };

            return toil;
        }

        bool GetNearbyPlantingSite (IntVec3 originPos, Map map, out IntVec3 newSite)
        {
            Predicate<IntVec3> validator = (IntVec3 tempCell) => IsCellOpenForSowingPlantOfType (tempCell, map, job.plantDefToSow)
                && ReservationUtility.CanReserveAndReach (GetActor (), tempCell, PathEndMode.Touch, DangerUtility.NormalMaxDanger (GetActor ()), 1);

            return CellFinder.TryFindRandomCellNear (originPos, map, 2, validator, out newSite);
        }

        static bool IsCellOpenForSowingPlantOfType (IntVec3 cell, Map map, ThingDef plantDef)
        {
            var playerSetPlantForCell = GetPlayerSetPlantForCell (cell, map);
            if (playerSetPlantForCell == null || !playerSetPlantForCell.CanAcceptSowNow ()) {
                return false;
            }

            var plantDefToGrow = playerSetPlantForCell.GetPlantDefToGrow ();
            if (plantDefToGrow == null || plantDefToGrow != plantDef) {
                return false;
            }

            if (cell.GetPlant (map) != null) {
                return false;
            }

            if (GenPlant.AdjacentSowBlocker (plantDefToGrow, cell, map) != null) {
                return false;
            }

            foreach (Thing current in map.thingGrid.ThingsListAt (cell)) {
                if (current.def.BlockPlanting) {
                    return false;
                }
            }
            return (plantDefToGrow.CanEverPlantAt (cell, map) && GenPlant.GrowthSeasonNow (cell, map));
        }

        static IPlantToGrowSettable GetPlayerSetPlantForCell (IntVec3 cell, Map map)
        {
            var plantToGrowSettable = cell.GetEdifice (map) as IPlantToGrowSettable;
            if (plantToGrowSettable == null) {
                plantToGrowSettable = (map.zoneManager.ZoneAt (cell) as IPlantToGrowSettable);
            }
            return plantToGrowSettable;
        }

        static bool IsActorCarryingAppropriateSeed (Pawn pawn, ThingDef thingDef)
        {
            if (pawn.carryTracker == null) {
                return false;
            }

            var carriedThing = pawn.carryTracker.CarriedThing;
            if (carriedThing == null || carriedThing.stackCount < 1) {
                return false;
            }

            if (thingDef.blueprintDef != carriedThing.def) {
                return false;
            }

            return true;
        }

        public override bool TryMakePreToilReservations ()
        {
            return pawn.Reserve (job.targetA, job, 1, -1, null);
        }
    }
}
