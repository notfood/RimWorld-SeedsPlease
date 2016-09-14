using System;
using System.Collections.Generic;
using System.Linq;

using RimWorld;
using Verse;

namespace SeedsPlease
{
	public class SeedDef : ThingDef
	{
		/*
		static SeedDef() {
			Log.Message ("hmmm");

			var inject = new Dictionary<string, int> () {
				"Caravan_Outlander_BulkGoods", new StockGenerator_Tag() {tradetag}
			};

			var traders = (
				from modpack in LoadedModManager.RunningMods 
				from trader in modpack.AllDefs
				where inject.Keys.Contains(trader.defName)
				select trader
			);

			foreach (var trader in traders) {
				Log.Message (">" + x);


			}

			Log.Message("! " + DefDatabase<TraderKindDef>.DefCount);
		}*/

		public SeedProperties seed;
		public new ThingDef plant;
		public ThingDef harvest;

		public override void ResolveReferences() {
			base.ResolveReferences ();

			if (plant != null && plant.blueprintDef == null) {
				plant.blueprintDef = this;

				if (harvest != null) {
					plant.plant.harvestedThingDef = harvest;
				} else {
					harvest = plant.plant.harvestedThingDef;
				}
			}


			//MarketValue = RoundUpToNearestHundredth(BaseMarketValue * UtilityValue) * (harvestYield / growDays) * (SeedsProduced * SeedChance * (1 + ExtraChance)) * (21 - sowMinSkill)

			//UtilityValue = 1 + (IsColonistIngestible * 0.1) + (IsAnimalIngestible * 0.05) + (craftingRecipesCount * 0.01) + (IsMedicinal * 0.1) 
			/*
			float utility = 1f;
			if (plant.plant.harvestedThingDef.ingestible != null) {
				if (plant.plant.harvestedThingDef.ingestible.HumanEdible) {
					utility += 0.1f;
				}
				if (plant.plant.harvestedThingDef.ingestible.nutrition > 0f){
					utility += 0.05f;
				}
			}

			var recipes = (
				from recipe in DefDatabase<RecipeDef>.AllDefs
				where recipe.IsIngredient(plant.plant.harvestedThingDef)
				select recipe
			);

			int recipeCount = recipes.Count ();

			utility += recipeCount * 0.01f;

			var medicinalRecipes = (
				from recipe in recipes
				where recipe.defName == ""
				select recipe
			);

			int medicinalRecipesCount = medicinalRecipes.Count ();

			if (medicinalRecipesCount > 0) {
				utility += 0.1f;
			}*/
			/*
			float marketValue = plant.plant.harvestedThingDef.BaseMarketValue * utility;

			if (plant.plant.growDays > 0) {
				marketValue *= plant.plant.harvestYield / plant.plant.growDays;
			}
			marketValue *= seed.seedFactor * seed.baseChance * (1 + seed.extraChance) * (21 - plant.plant.sowMinSkill);

			BaseMarketValue = Mathf.Round(marketValue);

			Log.Message ("> " + plant + " has " + BaseMarketValue + " seed value for " + utility + " utility");



			*/

			/*if (BaseMarketValue == 0) {
			if (plant.plant.harvestedThingDef.ingestible != null) {
				BaseMarketValue = 2f;
			} else {
				BaseMarketValue = 2.5f;
			}
			BaseMarketValue *= plant.plant.harvestedThingDef.BaseMarketValue * plant.plant.harvestYield * seed.harvestFactor;
			//BaseMarketValue *= (5 - plant.plant.growDays) / 5;
			BaseMarketValue *= (1 + plant.plant.sowMinSkill) / 5f;

			Log.Message (plant + " Harvest: " + plant.plant.harvestedThingDef.BaseMarketValue);
			Log.Message (plant + " Yield: " + plant.plant.harvestYield);
			Log.Message (plant + " Grow: " + plant.plant.growDays);
			Log.Message (plant + " Skill: " + plant.plant.sowMinSkill);
			Log.Warning (plant + " " + BaseMarketValue);
		}*/
		}
	}
}