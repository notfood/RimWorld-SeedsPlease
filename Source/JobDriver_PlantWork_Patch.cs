using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace SeedsPlease
{
    [HarmonyPatch]
    static class JobDriver_PlantWork_Patch
    {
        static MethodBase TargetMethod()
        {
            string inner = "<>c__DisplayClass9_0";
            string method = "<MakeNewToils>b__1";
            
            return AccessTools.Method(AccessTools.Inner(typeof(JobDriver_PlantWork), inner), method);
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> e)
        {
            MethodInfo yieldSeeds = AccessTools.Method(typeof(JobDriver_PlantWork_Patch), nameof(YieldSeeds));
            MethodInfo yieldNow = AccessTools.Method(typeof(Plant), nameof(Plant.YieldNow));

            MethodInfo makeThing = AccessTools.Method(typeof(ThingMaker), nameof(ThingMaker.MakeThing));
            MethodInfo makeOtherThing = AccessTools.Method(typeof(JobDriver_PlantWork_Patch), nameof(MakeOtherThing));

            var insts = new List<CodeInstruction>(e);
            foreach (var inst in insts)
            {
                if (object.Equals(yieldNow, inst.operand))
                {
                    yield return inst;
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                    yield return new CodeInstruction(OpCodes.Call, yieldSeeds);
                }
                else if (object.Equals(makeThing, inst.operand))
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                    yield return new CodeInstruction(OpCodes.Call, makeOtherThing);
                    yield return new CodeInstruction(OpCodes.Ldnull);
                    yield return inst;
                }
                else
                {
                    yield return inst;
                }
            }
        }

        static int YieldSeeds(int yield, Pawn actor, Plant plant)
        {
            if (plant.def.blueprintDef is SeedDef seedDef && !seedDef.thingCategories.NullOrEmpty() ) {
                var minGrowth = plant.def.plant.harvestMinGrowth;

                float parameter;
                if (minGrowth < 0.9f) {
                    parameter = Mathf.InverseLerp (minGrowth, 0.9f, plant.Growth);
                } else if (minGrowth < plant.Growth) {
                    parameter = 1f;
                } else {
                    parameter = 0f;
                }
                parameter = Mathf.Min (parameter, 1f);

                if (seedDef.seed.seedFactor > 0 && Rand.Value < seedDef.seed.baseChance * parameter) {
                    int count;
                    if (Rand.Value < seedDef.seed.extraChance) {
                        count = 2;
                    } else {
                        count = 1;
                    }

                    Thing seeds = ThingMaker.MakeThing (seedDef, null);
                    seeds.stackCount = Mathf.RoundToInt (seedDef.seed.seedFactor * count);
                    if (actor.Faction != Faction.OfPlayer)
                    {
                        seeds.SetForbidden(true);
                    }

                    GenPlace.TryPlaceThing (seeds, actor.Position, actor.Map, ThingPlaceMode.Near);
                }

                yield = Mathf.RoundToInt (yield * seedDef.seed.harvestFactor);
            }

            return yield;
        }

        static ThingDef MakeOtherThing(ThingDef def, ThingDef stuff, Plant plant)
        {
            if (plant.def.blueprintDef is SeedDef seedDef && seedDef.harvest != null) {
                return seedDef.harvest;
            }

            return def;
        }
    }
}