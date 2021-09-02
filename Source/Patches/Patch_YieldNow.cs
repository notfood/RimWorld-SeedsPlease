using Verse;
using RimWorld;
using HarmonyLib;
using UnityEngine;

namespace SeedsPleaseLite
{
    [HarmonyPatch(typeof(Plant), "YieldNow")]
	public class Patch_YieldNow
	{
		public static void Postfix(ref Plant __instance)
        {
            if (__instance.def.blueprintDef != null && __instance.def.blueprintDef.HasComp(typeof(CompSeed)) && !__instance.def.blueprintDef.thingCategories.NullOrEmpty() )
            {
                //Check if this plant is mature enough to yield a seed
                if (__instance.Growth < __instance.def.plant.harvestMinGrowth) return;

                var seedDef = __instance.def.blueprintDef;
                var seedComp = seedDef.GetCompProperties<CompProperties_Seed>();

                //Roll to check if a seed is gotten
                if (seedComp.seedFactor > 0 && Rand.Chance(seedComp.baseChance))
                {
                    //Try for a bonus seed
                    int count = (Rand.Chance(seedComp.extraChance)) ? 2 : 1;

                    Thing newSeeds = ThingMaker.MakeThing(seedDef, null);
                    newSeeds.stackCount = Mathf.RoundToInt(seedComp.seedFactor * count);

                    GenPlace.TryPlaceThing (newSeeds, __instance.Position, __instance.Map, ThingPlaceMode.Near);
                }
            }
	    }
    }
}