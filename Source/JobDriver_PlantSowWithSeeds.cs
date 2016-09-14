using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace SeedsPlease
{
	public class JobDriver_PlantSowWithSeeds : JobDriver
	{
		private const TargetIndex targetCellIndex = TargetIndex.A;

		private const TargetIndex seedsTargetIndex = TargetIndex.B;

		private float sowWorkDone;

		public override string GetReport ()
		{
			string text = JobDefOf.Sow.reportString;
			if (base.CurJob.plantDefToSow != null) {
				text = text.Replace ("TargetA", GenLabel.ThingLabel (base.CurJob.plantDefToSow, null, 1));
			}
			else {
				text = text.Replace ("TargetA", "area");
			}
			return text;
		}

		public override void ExposeData ()
		{
			base.ExposeData ();
			Scribe_Values.LookValue<float> (ref this.sowWorkDone, "sowWorkDone", 0, false);
		}

		protected override IEnumerable<Toil> MakeNewToils ()
		{
			ToilFailConditions.FailOnBurningImmobile<JobDriver_PlantSowWithSeeds> (this, TargetIndex.A);
			ToilFailConditions.FailOnDestroyedOrNull<JobDriver_PlantSowWithSeeds> (this, TargetIndex.B);
			ToilFailConditions.FailOnForbidden<JobDriver_PlantSowWithSeeds> (this, TargetIndex.B);

			yield return Toils_Reserve.Reserve (TargetIndex.A, 1);
			yield return Toils_Reserve.Reserve (TargetIndex.B, 1);
			yield return Toils_Reserve.ReserveQueue (TargetIndex.B, 1);
			yield return Toils_Goto.GotoThing (TargetIndex.B, PathEndMode.ClosestTouch);
			yield return Toils_Haul.StartCarryThing (TargetIndex.B);
			Toil toil = Toils_Goto.GotoCell (TargetIndex.A, PathEndMode.Touch);
			yield return toil;
			yield return SowSeedToil();
			yield return Toils_Reserve.Release (TargetIndex.A);
			yield return TryToSetAdditionalPlantingSite ();
			yield return Toils_Reserve.Reserve (TargetIndex.A, 1);
			yield return Toils_Jump.Jump (toil);
			yield break;
		}

		private Toil SowSeedToil ()
		{
			Toil toil = new Toil ();
			toil.defaultCompleteMode = ToilCompleteMode.Never;
			toil.initAction = delegate {
				Pawn actor = toil.actor;
				Job job = CurJob;
				if ( IsActorCarryingAppropriateSeed (actor, job.plantDefToSow) ) {
					
					Plant plant = (Plant) GenSpawn.Spawn (job.plantDefToSow, TargetLocA);
					plant.Growth = 0;
					plant.sown = true;

					job.targetC = plant;

					Find.Reservations.Reserve(actor, job.targetC, 1);

					sowWorkDone = 0;
				} else {
					EndJobWith (JobCondition.Incompletable);
				}
			};
			toil.tickAction = delegate {
				Pawn actor = toil.actor;
				Job job = actor.jobs.curJob;

				TargetInfo target = job.targetC;

				Plant plant = (Plant) target.Thing;

				if (actor.skills != null) {
					actor.skills.Learn (SkillDefOf.Growing, 0.154f);
				}

				if (plant.LifeStage != PlantLifeStage.Sowing) {
					Log.Error (this + " getting sowing work while not in Sowing life stage.");
				}

				this.sowWorkDone += StatExtension.GetStatValue (actor, StatDefOf.PlantWorkSpeed, true);

				if (sowWorkDone >= plant.def.plant.sowWork) {
					
					if ( !IsActorCarryingAppropriateSeed (actor, job.plantDefToSow) ) {
						EndJobWith (JobCondition.Incompletable);

						return;
					}

					if (actor.carrier.CarriedThing.stackCount <= 1) {
						actor.carrier.CarriedThing.Destroy (DestroyMode.Cancel);
					}
					else {
						actor.carrier.CarriedThing.stackCount--;
					}
						
					if (actor.story.traits.HasTrait (TraitDefOf.GreenThumb)) {
						actor.needs.mood.thoughts.memories.TryGainMemoryThought (ThoughtDefOf.GreenThumbHappy, null);
				    }

					plant.Growth = 0.05f;

					Find.MapDrawer.MapMeshDirty (plant.Position, MapMeshFlag.Things);

					actor.records.Increment (RecordDefOf.PlantsSown);

					ReadyForNextToil();
				}
			};
			toil.defaultCompleteMode = ToilCompleteMode.Never;
			toil.FailOnDespawnedNullOrForbidden (TargetIndex.A);
			toil.WithEffect ("Sow", TargetIndex.A);
			toil.WithProgressBar (TargetIndex.A, () => sowWorkDone / CurJob.plantDefToSow.plant.sowWork, true, -0.5f);
			toil.PlaySustainerOrSound (() => SoundDefOf.Interact_Sow);
			toil.AddFinishAction (delegate {
				Pawn actor = toil.actor;
				Job job = actor.jobs.curJob;

				Thing thing = job.targetC.Thing;
				if (thing != null) {
					Plant plant = (Plant) thing;
					if (sowWorkDone < plant.def.plant.sowWork && !thing.Destroyed) {
						thing.Destroy (DestroyMode.Vanish);
					}

					Find.Reservations.Release(job.targetC, actor);
					job.targetC = null;
				}
			});
			return toil;
		}

		private Toil TryToSetAdditionalPlantingSite ()
		{
			Toil toil = new Toil ();
			toil.defaultCompleteMode = ToilCompleteMode.Instant;
			toil.initAction = delegate {
				Pawn actor = toil.actor;
				Job job = actor.jobs.curJob;

				IntVec3 intVec;
				if ( IsActorCarryingAppropriateSeed (actor, job.plantDefToSow) && GetNearbyPlantingSite (job.GetTarget (TargetIndex.A).Cell, out intVec) ) {
					job.SetTarget (TargetIndex.A, intVec);

					return;
				}

				EndJobWith (JobCondition.Incompletable);
			};

			return toil;
		}

		private bool GetNearbyPlantingSite (IntVec3 originPos, out IntVec3 newSite)
		{
			Predicate<IntVec3> validator = (IntVec3 tempCell) => IsCellOpenForSowingPlantOfType (tempCell, CurJob.plantDefToSow) 
				&& ReservationUtility.CanReserveAndReach (GetActor (), tempCell, PathEndMode.Touch, DangerUtility.NormalMaxDanger (GetActor ()), 1);

			return CellFinder.TryFindRandomCellNear (originPos, 5, validator, out newSite);
		}

		private static bool IsCellOpenForSowingPlantOfType (IntVec3 cell, ThingDef plantDef)
		{
			IPlantToGrowSettable playerSetPlantForCell = GetPlayerSetPlantForCell (cell);
			if (playerSetPlantForCell == null || !playerSetPlantForCell.CanAcceptSowNow ()) {
				return false;
			}

			ThingDef plantDefToGrow = playerSetPlantForCell.GetPlantDefToGrow ();
			if (plantDefToGrow == null || plantDefToGrow != plantDef) {
				return false;
			}

			if (GridsUtility.GetPlant (cell) != null) {
				return false;
			}

			if (GenPlant.AdjacentSowBlocker (plantDefToGrow, cell) != null) {
				return false;
			}

			foreach (Thing current in Find.ThingGrid.ThingsListAt (cell)) {
				if (current.def.BlockPlanting) {
					return false;
				}
			}
			return (GenPlant.CanEverPlantAt (plantDefToGrow, cell) && GenPlant.GrowthSeasonNow (cell));
		}

		private static IPlantToGrowSettable GetPlayerSetPlantForCell (IntVec3 cell)
		{
			IPlantToGrowSettable plantToGrowSettable = GridsUtility.GetEdifice (cell) as IPlantToGrowSettable;
			if (plantToGrowSettable == null) {
				plantToGrowSettable = (Find.ZoneManager.ZoneAt (cell) as IPlantToGrowSettable);
			}
			return plantToGrowSettable;
		}

		private static bool IsActorCarryingAppropriateSeed (Pawn pawn, ThingDef thingDef)
		{
			if (pawn.carrier == null) {
				return false;
			}

			Thing carriedThing = pawn.carrier.CarriedThing;
			if (carriedThing == null || carriedThing.stackCount < 1) {
				return false;
			}

			if (thingDef.blueprintDef != carriedThing.def) {
				return false;
			}

			return true;
		}
	}
}
