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
            //Method name check at the end there isn't great. Hopefully just a temp solution until something better can be thought up
            if (__instance.def.blueprintDef != null && __instance.def.blueprintDef.HasModExtension<Seed>() && !__instance.def.blueprintDef.thingCategories.NullOrEmpty() && new StackFrame(2).GetMethod().Name.Contains("MakeNewToils"))
            {
                //Check if this plant is mature enough to yield a seed
                if (__instance.Growth < __instance.def.plant.harvestMinGrowth) return;

                var seedDef = __instance.def.blueprintDef;
                var seedComp = seedDef.GetModExtension<Seed>();;

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