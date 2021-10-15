using Verse;
using RimWorld;
using HarmonyLib;
using UnityEngine;

namespace SeedsPleaseLite
{
    [HarmonyPatch(typeof(Plant), "PlantCollected")]
	public class Patch_PlantCollected
	{
        public static void Prefix(ref Plant __instance, Pawn by)
        {
            //Method name check at the end there isn't great. Hopefully just a temp solution until something better can be thought up
            if (__instance.def.blueprintDef != null && __instance.def.blueprintDef.HasModExtension<Seed>() && !__instance.def.blueprintDef.thingCategories.NullOrEmpty() && __instance.Growth >= __instance.def.plant.harvestMinGrowth)
            {
                var seedDef = __instance.def.blueprintDef;
                var seedDefX = seedDef.GetModExtension<Seed>();

                //Roll to check if a seed is gotten
                if (seedDefX.seedFactor > 0 && Rand.Chance(seedDefX.baseChance))
                {
                    //Try for a bonus seed
                    int count = (Rand.Chance(seedDefX.extraChance)) ? 2 : 1;

                    Thing newSeeds = ThingMaker.MakeThing(seedDef, null);
                    newSeeds.stackCount = Mathf.RoundToInt(seedDefX.seedFactor * count);

                    GenPlace.TryPlaceThing (newSeeds, by.Position, by.Map, ThingPlaceMode.Near);

                    if (by.Faction != Faction.OfPlayer) newSeeds.SetForbidden(true);
                }
            }
	    }
    }
}