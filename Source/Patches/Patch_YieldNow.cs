using Verse;
using RimWorld;
using HarmonyLib;
using UnityEngine;
using System.Diagnostics;

namespace SeedsPleaseLite
{
    [HarmonyPatch(typeof(Plant), "YieldNow")]
	public class Patch_YieldNow
	{
        public static void Postfix(ref Plant __instance)
        {
            //The stack track check at the end is a dirty hack to test and check if this YieldNow() function is being used by toils and not just as a shortcut to calculate yields that some mods like Colony Manager do.
            //A more proper solution is still under investigation, but this'll do for the moment.
            if (__instance.def.blueprintDef != null && __instance.def.blueprintDef.HasComp(typeof(CompSeed)) && !__instance.def.blueprintDef.thingCategories.NullOrEmpty() && (new StackTrace().GetFrame(2).GetMethod().Name).Contains("MakeNewToils"))
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