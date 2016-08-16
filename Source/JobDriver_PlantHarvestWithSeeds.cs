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
			ToilFailConditions.FailOnDestroyedNullOrForbidden<JobDriver_PlantHarvestWithSeeds> (this, TargetIndex.A);

			yield return Toils_Reserve.Reserve (TargetIndex.A, 1);
			yield return Toils_Goto.GotoThing (TargetIndex.A, PathEndMode.Touch);
			yield return HarvestSeedsToil();
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
							MoteThrower.ThrowText ((actor.DrawPos + plant.DrawPos) / 2, "HarvestFailed".Translate (), 220);
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

									GenPlace.TryPlaceThing (seeds, actor.Position, ThingPlaceMode.Near);
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
								GenPlace.TryPlaceThing (thing, actor.Position, ThingPlaceMode.Near, null);
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

			toil.WithEffect ("Harvest", TargetIndex.A);
			toil.WithProgressBar (TargetIndex.A, () => workDone / Plant.def.plant.harvestWork, true, -0.5f);
			toil.WithSustainer (() => Plant.def.plant.soundHarvesting);

			return toil;
		}
	}
}
