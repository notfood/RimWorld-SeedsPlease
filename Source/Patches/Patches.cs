using Verse;
using RimWorld;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace SeedsPleaseLite
{
    //This patch controls the dropping of seeds upon harvest
    [HarmonyPatch(typeof(Plant), nameof(Plant.PlantCollected))]
	public class Patch_PlantCollected
	{
        public static void Prefix(ref Plant __instance, Pawn by)
        {
            if ((__instance.def.blueprintDef?.HasModExtension<Seed>() ?? false) && !__instance.def.blueprintDef.thingCategories.NullOrEmpty() && __instance.Growth >= __instance.def.plant.harvestMinGrowth)
            {
                ThingDef seedDef = __instance.def.blueprintDef;
                Seed seedDefX = seedDef.GetModExtension<Seed>();

                //Roll to check if a seed is gotten
                if (seedDefX.seedFactor > 0 && Rand.Chance(seedDefX.baseChance))
                {
                    //Try for a bonus seed
                    int count = Rand.Chance(seedDefX.extraChance) ? 2 : 1;

                    Thing newSeeds = ThingMaker.MakeThing(seedDef, null);
                    newSeeds.stackCount = Mathf.RoundToInt(seedDefX.seedFactor * count * SeedsPleaseLite.ModSettings_SeedsPleaseLite.seedFactorModifier);

                    GenPlace.TryPlaceThing(newSeeds, by.Position, by.Map, ThingPlaceMode.Near);

                    if (by.Faction != Faction.OfPlayer) newSeeds.SetForbidden(true);
                }
            }
	    }
    }

    //This is responsible for determining which crops show up on the list when you configue a grow zone
    [HarmonyPatch (typeof(Command_SetPlantToGrow), nameof(Command_SetPlantToGrow.IsPlantAvailable))]
    static class Patch_IsPlantAvailable
    {
        public static void Postfix(ThingDef plantDef, Map map, ref bool __result)
        {
            if (__result && (plantDef?.blueprintDef?.HasModExtension<Seed>() ?? false))
            {
                __result = map.listerThings.ThingsOfDef(plantDef.blueprintDef).Count > 0;
            }
        }
    }

    //This patches the random resource drop pod event to reduce odds of it being seeds since seeds can overwhelm the loot table
    [HarmonyPatch(typeof(ThingSetMaker_ResourcePod), nameof(ThingSetMaker_ResourcePod.PossiblePodContentsDefs))]
    static class Patch_PossiblePodContentsDefs
    {
        private static IEnumerable<ThingDef> Postfix(IEnumerable<ThingDef> values)
        {
            //Continue if passing a 33% odds chance
            if (Rand.Chance(0.33f)) foreach (ThingDef value in values) yield return value;
            //If it fails, pick again, this time filtering out any seeds
            else
            {
                var dd = DefDatabase<ThingDef>.AllDefsListForReading.Where
                (d => 
                    d.category == ThingCategory.Item && 
                    d.tradeability.TraderCanSell() && 
                    d.equipmentType == EquipmentType.None && 
                    d.BaseMarketValue >= 1f && 
                    d.BaseMarketValue < 40f && 
                    !d.HasComp(typeof(CompHatcher)) && 
                    !d.HasModExtension<Seed>()
                );
                foreach (ThingDef def in dd) yield return def;
            }
        }
    }

    //This patchs traders to adjust their stock generation so they won't try to sell seeds you can't even grow
    [HarmonyPatch(typeof(StockGenerator_Tag), nameof(StockGenerator_Tag.GenerateThings))]
    static class Patch_GenerateThings
    {
        private static void Prefix(ref List<ThingDef> ___excludedThingDefs)
        {
            if (SeedsPleaseLite.ModSettings_SeedsPleaseLite.noUselessSeeds) 
            {
                //Get a list of wild plants that grow in player's map(s)
                List<ThingDef> wildBiomePlants = new List<ThingDef>();
                foreach (Map map in Current.Game.Maps)
                {
                    if (map.IsPlayerHome) map.Biome.wildPlants.ForEach(x => wildBiomePlants.Add(x.plant));
                }
                
                //Get a list of seeds that are sensitive to biome restrictions
                var seeds = DefDatabase<ThingDef>.AllDefsListForReading.Where
                (x => 
                    x.HasModExtension<Seed>() && 
                    x.GetModExtension<Seed>().sources.Any(y => y.plant.mustBeWildToSow && y.plant.purpose != PlantPurpose.Beauty)
                );

                //Of those seeds, determine which ones are useless and add them to the excluded defs list
                foreach (ThingDef seed in seeds)
                {
                    
                    if (!wildBiomePlants.Intersect(seed.GetModExtension<Seed>().sources).Any())
                    {
                        if (___excludedThingDefs == null) ___excludedThingDefs = new List<ThingDef>();
                        ___excludedThingDefs.Add(seed);
                    }
                }
            }
        }
    }
}