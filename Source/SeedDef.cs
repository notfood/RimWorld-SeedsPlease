using System.Collections.Generic;

using UnityEngine;
using RimWorld;
using Verse;
using System.Xml.Linq;

namespace SeedsPlease
{
    public class SeedDef : ThingDef
    {
        public SeedProperties seed;
        public new ThingDef plant;
        public ThingDef harvest;
        public List<ThingDef> sources = new List<ThingDef> ();

        static float AssignMarketValueFromHarvest(ThingDef thingDef)
        {
            var harvestedThingDef = thingDef.plant.harvestedThingDef;
            if (harvestedThingDef == null)
            {
                return 50f;
            }

            float factor = thingDef.plant.harvestYield / thingDef.plant.growDays + thingDef.plant.growDays / thingDef.plant.harvestYield;
            float value = harvestedThingDef.BaseMarketValue * factor * 2.5f;

            if (thingDef.plant.blockAdjacentSow)
            {
                value *= 9f;
            }

            if (harvestedThingDef == ThingDefOf.WoodLog)
            {
                value *= 0.2f;
            }
            else if (harvestedThingDef.IsAddictiveDrug)
            {
                value *= 1.3f;
            }
            else if (harvestedThingDef.IsDrug)
            {
                value *= 1.2f;
            }
            else if (harvestedThingDef.IsMedicine)
            {
                value *= 1.1f;
            }

            value *= Mathf.Lerp(0.8f, 1.6f, thingDef.plant.sowMinSkill / 20f);

            return Mathf.Round(value * 100f) / 100f;
        }

        public override void ResolveReferences ()
        {
            base.ResolveReferences ();

            if (plant == null || plant.blueprintDef != null) {
                return;
            }

            if (plant.plant != null && plant.plant.Sowable) {
                plant.blueprintDef = this;
            } else {
                Log.Warning ("SeedsPlease :: " + plant.defName + " is not a sowable plant.");
                plant = null;
                return;
            }

            if (harvest != null) {
                plant.plant.harvestedThingDef = harvest;
            } else {
                harvest = plant.plant.harvestedThingDef;
            }

            if (BaseMarketValue <= 0 && harvest != null) {
                BaseMarketValue = AssignMarketValueFromHarvest(plant);
            }

            foreach (var p in sources) {
                if (p.plant == null) {
                    Log.Warning ("SeedsPlease :: " + p.defName + " is not a plant.");
                    continue;
                }

                p.blueprintDef = this;
            }

#if DEBUG
			Log.Message ("\t" + plant + " => " + BaseMarketValue);
#endif
        }

        public static void AddMissingSeeds() {
            foreach (var thingDef in DefDatabase<ThingDef>.AllDefs) {
                if (thingDef.plant == null) {
                    continue;
                }
                if (thingDef.blueprintDef != null)
                {
                    continue;
                }
                if (!thingDef.plant.Sowable) {
                    continue;
                }
                if (thingDef.plant.harvestedThingDef == null)  {
                    continue;
                }

                AddMissingSeed(thingDef);
            }
        }

        static void AddMissingSeed(ThingDef thingDef)
        {
            string name = thingDef.defName;
            foreach (string prefix in SeedsPleaseMod.knownPrefixes)
            {
                name = name.Replace(prefix, "");
            }
            name = name.CapitalizeFirst();

            var template = ResourceBank.ThingDefOf.Seed_Potato;
            var seed = new SeedDef()
            {
                defName = "Seed_" + name,
                label = name.ToLower() + " seeds",
                plant = thingDef,
                harvest = thingDef.plant.harvestedThingDef,
                BaseMarketValue = AssignMarketValueFromHarvest(thingDef),
                stackLimit = template.stackLimit,
                seed = new SeedProperties() { harvestFactor = 1f, seedFactor = 1f, baseChance = 0.95f, extraChance = 0.15f },
                tradeTags = template.tradeTags,
                thingCategories = template.thingCategories,
                soundDrop = template.soundDrop,
                soundInteract = template.soundInteract,
                statBases = template.statBases,
                graphicData = template.graphicData,
                description = template.description,
                thingClass = template.thingClass,
                pathCost = template.pathCost,
                rotatable = template.rotatable,
                drawGUIOverlay = template.drawGUIOverlay,
                alwaysHaulable = template.alwaysHaulable,
                comps = template.comps,
                altitudeLayer = template.altitudeLayer,
                selectable = template.selectable,
                useHitPoints = template.useHitPoints,
                resourceReadoutPriority = template.resourceReadoutPriority,
                category = template.category,
            };
            seed.ResolveReferences();
            thingDef.blueprintDef = seed;
            DefDatabase<SeedDef>.Add(seed);

            var stringBuilder = new System.Text.StringBuilder();

            stringBuilder.Append("SeedsPlease :: ");
            stringBuilder.Append(thingDef.defName);
            stringBuilder.Append(" (");
            stringBuilder.Append(thingDef.modContentPack.IsCoreMod ? "Patched" : thingDef.modContentPack.Name);
            stringBuilder.Append(") is missing a SeedDef. Autogenerated as ");
            stringBuilder.AppendLine(seed.defName);
            stringBuilder.AppendLine();

            var seedXml =
                new XElement("SeedsPlease.SeedDef", new XAttribute("ParentName", "SeedBase"),
                             new XElement("defName", seed.defName),
                             new XElement("label", seed.label),
                             new XElement("plant", thingDef.defName));
            stringBuilder.AppendLine(seedXml.ToString());

            if (thingDef.plant.harvestedThingDef == ThingDefOf.WoodLog) {
                Log.Warning(stringBuilder.ToString());

                return;
            }

            float yieldCount = Mathf.Max(Mathf.Round(thingDef.plant.harvestYield / 3f), 4f);
            var ingredient = new IngredientCount();
            ingredient.filter.SetAllow(thingDef.plant.harvestedThingDef, true);
            ingredient.SetBaseCount(yieldCount);

            var recipe = new RecipeDef()
            {
                defName = "ExtractSeed_" + name,
                label = "extract " + name.ToLower() + " seeds",
                description = "Extract seeds from " + thingDef.plant.harvestedThingDef.defName.Replace("Raw", ""),
                ingredients = new List<IngredientCount>() { ingredient },
                defaultIngredientFilter = ingredient.filter,
                fixedIngredientFilter = ingredient.filter,
                products = new List<ThingDefCountClass>() {
                    new ThingDefCountClass() { thingDef = seed, count = 3 }
                },
                researchPrerequisite = thingDef.researchPrerequisites?.FirstOrFallback(),
                workAmount = 600f,
                workSkill = SkillDefOf.Cooking,
                effectWorking = EffecterDefOf.Vomit,
                workSpeedStat = StatDefOf.EatingSpeed,
                jobString = "Extracting seeds."
            };
            DefDatabase<RecipeDef>.Add(recipe);
            ResourceBank.ThingDefOf.PlantProcessingTable.recipes.Add(recipe);

            var recipeXml =
                new XElement("RecipeDef", new XAttribute("ParentName", "ExtractSeed"),
                             new XElement("defName", recipe.defName),
                             new XElement("label", recipe.label),
                             new XElement("description", recipe.description),
                             new XElement("ingredients",
                                          new XElement("li",
                                                       new XElement("filter",
                                                                    new XElement("thingDefs",
                                                                                 new XElement("li", thingDef.plant.harvestedThingDef.defName))),
                                                       new XElement("count", yieldCount))),
                             new XElement("fixedIngredientFilter",
                                          new XElement("thingDefs",
                                                       new XElement("li", thingDef.plant.harvestedThingDef.defName))),
                             new XElement("products",
                                          new XElement(seed.defName, 3)));

            stringBuilder.AppendLine();
            stringBuilder.AppendLine(recipeXml.ToString());

            Log.Warning(stringBuilder.ToString());
        }
    }
}