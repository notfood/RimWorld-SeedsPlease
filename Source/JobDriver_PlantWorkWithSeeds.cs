using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace SeedsPlease
{
    public abstract class JobDriver_PlantWorkWithSeeds : JobDriver_PlantWork
    {
        const TargetIndex targetCellIndex = TargetIndex.A;

        float workDone;

        protected override IEnumerable<Toil> MakeNewToils ()
        {
            yield return Toils_JobTransforms.MoveCurrentTargetIntoQueue (targetCellIndex);
            yield return Toils_Reserve.ReserveQueue (targetCellIndex);

            var init = Toils_JobTransforms.ClearDespawnedNullOrForbiddenQueuedTargets (targetCellIndex);

            yield return init;
            yield return Toils_JobTransforms.SucceedOnNoTargetInQueue (targetCellIndex);
            yield return Toils_JobTransforms.ExtractNextTargetFromQueue (targetCellIndex);

            var clear = Toils_JobTransforms.ClearDespawnedNullOrForbiddenQueuedTargets (targetCellIndex);
            yield return Toils_Goto.GotoThing (targetCellIndex, PathEndMode.Touch).JumpIfDespawnedOrNullOrForbidden (targetCellIndex, clear);

            yield return HarvestSeedsToil ();
            yield return PlantWorkDoneToil ();
            yield return Toils_Jump.JumpIfHaveTargetInQueue (targetCellIndex, init);
        }

        Toil HarvestSeedsToil ()
        {
            var toil = new Toil ();
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.initAction = Init;
            toil.tickAction = delegate {
                var actor = toil.actor;
                var plant = Plant;

                if (actor.skills != null) {
                    actor.skills.Learn (SkillDefOf.Plants, xpPerTick);
                }

                workDone += actor.GetStatValue (StatDefOf.PlantWorkSpeed, true);
                if (workDone >= plant.def.plant.harvestWork) {
                    if (plant.def.plant.harvestedThingDef != null) {
                        if (actor.RaceProps.Humanlike && plant.def.plant.harvestFailable && Rand.Value > actor.GetStatValue(StatDefOf.PlantHarvestYield)) {
                            MoteMaker.ThrowText ((actor.DrawPos + plant.DrawPos) / 2f, actor.Map, ResourceBank.StringTextMoteHarvestFailed, 3.65f);
                        } else {
                            int plantYield = plant.YieldNow();

                            var harvestedThingDef = MakeSeedsAndGetHarvestedThing(plant, actor, ref plantYield);

                            if (plantYield > 0)
                            {
                                var thing = ThingMaker.MakeThing(harvestedThingDef);
                                thing.stackCount = plantYield;
                                if (actor.Faction != Faction.OfPlayer && !actor.IsPrisonerOfColony)
                                {
                                    thing.SetForbidden(true);
                                }

                                GenPlace.TryPlaceThing(thing, actor.Position, actor.Map, ThingPlaceMode.Near);
                                actor.records.Increment(RecordDefOf.PlantsHarvested);
                            }
                        }
                    }

                    plant.def.plant.soundHarvestFinish.PlayOneShot(actor);
                    plant.PlantCollected();
                    workDone = 0;
                    ReadyForNextToil();
                }
            };

            toil.FailOnDespawnedNullOrForbidden (targetCellIndex);
            toil.WithEffect (EffecterDefOf.Harvest, targetCellIndex);
            toil.WithProgressBar (targetCellIndex, () => workDone / Plant.def.plant.harvestWork, true);
            toil.PlaySustainerOrSound (() => Plant.def.plant.soundHarvesting);

            return toil;
        }

        private static ThingDef MakeSeedsAndGetHarvestedThing(Plant plant, Pawn actor, ref int plantYield)
        {
            if (!(plant.def.blueprintDef is SeedDef seedDef) || seedDef.thingCategories.NullOrEmpty())
                return plant.def.plant.harvestedThingDef;

            var minGrowth = plant.def.plant.harvestMinGrowth;

            float parameter = minGrowth < 0.9f ? Mathf.InverseLerp(minGrowth, 0.9f, plant.Growth)
                : minGrowth < plant.Growth ? 1f : 0f;

            parameter = Mathf.Min(parameter, 1f);

            if (seedDef.seed.seedFactor > 0 && Rand.Value < seedDef.seed.baseChance * parameter)
            {
                var count = Rand.Value < seedDef.seed.extraChance ? 2 : 1;
                MakeSeeds(seedDef, count, actor);
            }

            plantYield = Mathf.RoundToInt(plantYield * seedDef.seed.harvestFactor);

            return seedDef.harvest;
        }

        private static void MakeSeeds(SeedDef seedDef, int count, Pawn actor)
        {
            Thing seeds = ThingMaker.MakeThing(seedDef);
            seeds.stackCount = Mathf.RoundToInt(seedDef.seed.seedFactor * count);

            if (!SeedsPleaseSettings.placeSeedsInInventory || actor.WorkTypeIsDisabled(WorkTypeDefOf.Hauling))
            {
                GenPlace.TryPlaceThing(seeds, actor.Position, actor.Map, ThingPlaceMode.Near);
                return;
            }

            var addedToInventory = actor.inventory.innerContainer.TryAdd(seeds);
            if (!addedToInventory)
            {
                GenPlace.TryPlaceThing(seeds, actor.Position, actor.Map, ThingPlaceMode.Near);
            }
        }

        public static void TryUnloadSeeds(Pawn pawn)
        {
            var hauledSeeds = pawn.inventory.innerContainer.Where(thing => thing.def is SeedDef);

            if (pawn.Faction != Faction.OfPlayerSilentFail || !pawn.RaceProps.Humanlike
                || pawn.carryTracker.CarriedThing is Corpse
                || !hauledSeeds.Any())
            {
                return;
            }

            var job = new Job(ResourceBank.JobDefOf.UnloadSeeds, pawn);
            pawn.jobs.jobQueue.EnqueueFirst(job, JobTag.Misc);
        }
    }
}
