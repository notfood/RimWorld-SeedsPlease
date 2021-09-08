using Verse;
using HarmonyLib;
   
namespace SeedsPleaseLite
{
    [HarmonyPatch (typeof(Command_SetPlantToGrow), "IsPlantAvailable")]
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
}