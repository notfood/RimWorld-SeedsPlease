using Verse;
using RimWorld;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace SeedsPleaseLite
{
    [HarmonyPatch(typeof(Plant), nameof(Plant.PlantCollected))]
	public class Patch_PlantCollected
	{
        public static void Prefix(ref Plant __instance, Pawn by)
        {
            //Method name check at the end there isn't great. Hopefully just a temp solution until something better can be thought up
            if (__instance.def.blueprintDef != null && __instance.def.blueprintDef.HasModExtension<Seed>() && !__instance.def.blueprintDef.thingCategories.NullOrEmpty() && __instance.Growth >= __instance.def.plant.harvestMinGrowth)
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

    [HarmonyPatch (typeof(Command_SetPlantToGrow), nameof(Command_SetPlantToGrow.IsPlantAvailable))]
    static class Patch_Command_SetPlantToGrow
    {
        public static void Postfix(ThingDef plantDef, Map map, ref bool __result)
        {
            //This is responsible for determining which crops show up on the list when you configue a grow zone
            if (__result && plantDef != null && plantDef.blueprintDef != null && plantDef.blueprintDef.HasModExtension<Seed>())
            {
                __result = map.listerThings.ThingsOfDef(plantDef.blueprintDef).Count > 0;
            }
        }
    }

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

    [HarmonyPatch(typeof(StockGenerator_Tag), nameof(StockGenerator_Tag.GenerateThings))]
    static class Patch_GenerateThings
    {
        private static void Prefix(ref List<ThingDef> ___excludedThingDefs)
        {
            if (SeedsPleaseLite.ModSettings_SeedsPleaseLite.noUselessSeeds) 
            {
                List<ThingDef> wildBiomePlants = new List<ThingDef>();
                foreach (Map map in Current.Game.Maps)
                {
                    if (map.IsPlayerHome) map.Biome.wildPlants.ForEach(x => wildBiomePlants.Add(x.plant));
                }
                
                var seeds = DefDatabase<ThingDef>.AllDefsListForReading.Where
                (x => 
                    x.HasModExtension<Seed>() && 
                    x.GetModExtension<Seed>().sources.Any(y => y.plant.mustBeWildToSow && y.plant.purpose != PlantPurpose.Beauty)
                );
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