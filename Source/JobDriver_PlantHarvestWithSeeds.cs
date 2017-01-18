using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace SeedsPlease
{
	public class JobDriver_PlantHarvestWithSeeds : JobDriver_PlantHarvest
	{
		private float workDone;

		protected override IEnumerable<Toil> MakeNewToils ()
		{
			yield return Toils_JobTransforms.MoveCurrentTargetIntoQueue (TargetIndex.A);
			yield return Toils_Reserve.ReserveQueue (TargetIndex.A, 1);

			var init = Toils_JobTransforms.ClearDespawnedNullOrForbiddenQueuedTargets (TargetIndex.A);

			yield return init;
			yield return Toils_JobTransforms.ExtractNextTargetFromQueue (TargetIndex.A);

			var clear = Toils_JobTransforms.ClearDespawnedNullOrForbiddenQueuedTargets (TargetIndex.A);
			yield return Toils_Goto.GotoThing (TargetIndex.A, PathEndMode.Touch).JumpIfDespawnedOrNullOrForbidden (TargetIndex.A, clear);

			yield return HarvestSeedsToil();
			yield return Toils_Jump.JumpIfHaveTargetInQueue (TargetIndex.A, init);
			yield return Toils_General.RemoveDesignationsOnThing (TargetIndex.A, DesignationDefOf.HarvestPlant);
			yield break;
		}

		private Toil HarvestSeedsToil () {
			Toil toil = new Toil ();
			toil.defaultCompleteMode = ToilCompleteMode.Never;
			toil.tickAction = delegate {
				Pawn actor = toil.actor;
				Plant plant = Plant;

				if (actor.skills != null) {
					actor.skills.Learn (SkillDefOf.Growing, xpPerTick);
				}

				workDone += actor.GetStatValue (StatDefOf.PlantWorkSpeed, true);
				if (workDone >= plant.def.plant.harvestWork) {
					if (plant.def.plant.harvestedThingDef != null) {
						if (actor.RaceProps.Humanlike && plant.def.plant.harvestFailable && Rand.Value < actor.GetStatValue (StatDefOf.HarvestFailChance, true)) {
							MoteMaker.ThrowText ((actor.DrawPos + plant.DrawPos) / 2, actor.Map, "HarvestFailed".Translate (), 3.65f);
						} else {
							int plantYield = plant.YieldNow ();

							ThingDef harvestedThingDef;

							SeedDef seedDef = plant.def.blueprintDef as SeedDef;
							if (seedDef != null) {
								float parameter = Mathf.Max (Mathf.InverseLerp (plant.def.plant.harvestMinGrowth, 1.2f, plant.Growth), 1f);

								if (seedDef.seed.seedFactor > 0 && Rand.Value < seedDef.seed.baseChance * parameter) {
									int count;
									if (Rand.Value < seedDef.seed.extraChance) {
										count = 2;
									} else {
										count = 1;
									}

									Thing seeds = ThingMaker.MakeThing (seedDef, null);
									seeds.stackCount = Mathf.RoundToInt(seedDef.seed.seedFactor * count);

									GenPlace.TryPlaceThing (seeds, actor.Position, actor.Map, ThingPlaceMode.Near);
								}

								plantYield = Mathf.RoundToInt(plantYield * seedDef.seed.harvestFactor);

								harvestedThingDef = seedDef.harvest;
							} else {
								harvestedThingDef = plant.def.plant.harvestedThingDef;
							}

							if (plantYield > 0) {
								Thing thing = ThingMaker.MakeThing (harvestedThingDef, null);
								thing.stackCount = plantYield;
								if (actor.Faction != Faction.OfPlayer) {
									thing.SetForbidden (true, true);
								}
								GenPlace.TryPlaceThing (thing, actor.Position, actor.Map, ThingPlaceMode.Near, null);
							}

							actor.records.Increment (RecordDefOf.PlantsHarvested);
						}
					}
					plant.def.plant.soundHarvestFinish.PlayOneShot (actor);
					plant.PlantCollected ();
					workDone = 0;
					ReadyForNextToil ();
					return;
				}
			};

			toil.FailOnDespawnedNullOrForbidden (TargetIndex.A);
			toil.WithEffect (EffecterDefOf.Harvest, TargetIndex.A);
			toil.WithProgressBar (TargetIndex.A, () => workDone / Plant.def.plant.harvestWork, true, -0.5f);
			toil.PlaySustainerOrSound (() => Plant.def.plant.soundHarvesting);

			return toil;
		}
	}
}
