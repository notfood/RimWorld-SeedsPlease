using Verse;
using RimWorld;
using HarmonyLib;
using System.Linq;
using System.Collections.Generic;
 
namespace SeedsPleaseLite
{
    [HarmonyPatch(typeof(ThingSetMaker_ResourcePod), "PossiblePodContentsDefs")]
    static class Patch_PossiblePodContentsDefs
    {
        private static void Postfix(ref IEnumerable<ThingDef> __result)
        {
            //Continue if passing a 33% odds chance
            if (Rand.Chance(0.33f)) return;
            
            //If it fails, pick again, this time filtering out any seeds
            __result = from d in DefDatabase<ThingDef>.AllDefs
			where d.category == ThingCategory.Item && d.tradeability.TraderCanSell() && d.equipmentType == EquipmentType.None && d.BaseMarketValue >= 1f && d.BaseMarketValue < 40f && !d.HasComp(typeof(CompHatcher)) && !d.HasModExtension<Seed>()
			select d;
        }
    }
}