using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using Verse;
using System.Xml.Linq;
using System.Linq;
using System.Text;

namespace SeedsPlease
{
    public class SeedDef : ThingDef
    {
        public SeedProperties seed;
        public ThingDef harvest;
        public List<ThingDef> sources = new List<ThingDef> ();
        public bool extractable = true;
		
        [Unsaved]
        public new ThingDef plant;
		
        static float AssignMarketValueFromHarvest(ThingDef thingDef)
        {
            var harvestedThingDef = thingDef.plant.harvestedThingDef;
            if (harvestedThingDef == null)
            {
                return 0.5f;
			}
			
            float factor = thingDef.plant.harvestYield / thingDef.plant.growDays + thingDef.plant.growDays / thingDef.plant.harvestYield;
            float value = harvestedThingDef.BaseMarketValue * factor * 2.5f;
			
            if (thingDef.plant.blockAdjacentSow)
            {
                value *= 1.5f;
			}
			
            int cnt = thingDef.plant.wildBiomes?.Count() ?? 0;
            if (cnt > 1)
            {
                value *= Mathf.Pow(0.95f, cnt);
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
			
            if (value > 25f)
            {
                value = 24.99f;
			}
			
            return Mathf.Round(value * 100f) / 100f;
		}
		
        public override void ResolveReferences ()
        {
            base.ResolveReferences ();
			
            foreach (var p in sources)
            {
                if (p.plant == null)
                {
                    Log.Warning("SeedsPlease :: " + p.defName + " is not a plant.");
                    continue;
				}
				
                p.blueprintDef = this;
				
                if (plant == null && p.plant.Sowable)
                {
                    plant = p;
				}
			}
			
            if (plant == null) {
                Log.Warning("SeedsPlease :: " + defName + " has no sowable plant.");
				
                return;
			}
			
            if (plant.blueprintDef == null)
            {
                plant.blueprintDef = this;
			}
			
            if (harvest != null) {
                plant.plant.harvestedThingDef = harvest;
				} else {
                harvest = plant.plant.harvestedThingDef;
			}
			
            if (BaseMarketValue <= 0f && harvest != null) {
                BaseMarketValue = AssignMarketValueFromHarvest(plant);
			}
			
			#if DEBUG
				Log.Message ($"{plant} {harvest?.BaseMarketValue} => {BaseMarketValue}");
			#endif
		}
		
        public static bool AddMissingSeeds(StringBuilder report) {
            // Linq narrow down the def database. Plants we've already manually done fail the blueprint check to filter out
            var dd = DefDatabase<ThingDef>.AllDefs.Where(x => x.plant != null && x.blueprintDef == null && x.plant.Sowable && x.plant.harvestedThingDef != null).ToList();
            bool isAnyMissing = false;
            foreach (var thingDef in dd) {
                isAnyMissing = true;
                AddMissingSeed(report, thingDef);
			}
            return isAnyMissing;
		}
		
        static void AddMissingSeed(StringBuilder report, ThingDef thingDef)
        {
            string name = thingDef.defName;
            foreach (string prefix in SeedsPleaseMod.knownPrefixes)
            {
                name = name.Replace(prefix, "");
			}
            name = name.CapitalizeFirst();
			
            report.AppendLine();
            report.Append("<!-- SeedsPlease :: ");
            report.Append(thingDef.defName);
            report.Append(" (");
            report.Append(thingDef.modContentPack.IsCoreMod ? "Patched" : thingDef.modContentPack.PackageId);
            report.Append(") ");
			
            SeedDef seed = DefDatabase<SeedDef>.GetNamedSilentFail("Seed_" + name);
            if (seed == null)
            {
                var template = ResourceBank.ThingDefOf.Seed_Psychoid;
				
                seed = new SeedDef()
                {
                    defName = "Seed_" + name,
                    label = name.ToLower() + " seeds",
                    plant = thingDef,
                    harvest = thingDef.plant.harvestedThingDef,
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
                    uiIcon = template.uiIcon,
                    uiIconColor = template.uiIconColor,
                    ingestible = template.ingestible,
                    descriptionHyperlinks = new List<DefHyperlink>() { thingDef }
				};
				
                seed.BaseMarketValue = AssignMarketValueFromHarvest(thingDef);
				
                foreach(var category in seed.thingCategories) {
                    category.childThingDefs.Add(seed);
				}
				
                DefDatabase<ThingDef>.Add(seed);
                DefDatabase<SeedDef>.Add(seed);
				
                seed.ResolveReferences();
				
                report.Append("Autogenerated as ");
			}
            else
            {
                seed.sources.Add(thingDef);
				
                report.Append("Inserted to ");
			}
			
            report.Append(seed.defName);
            report.AppendLine("-->");
            report.AppendLine();
			
            var seedXml =
            new XElement("SeedsPlease.SeedDef", new XAttribute("ParentName", "SeedBase"),
				new XElement("defName", seed.defName),
				new XElement("label", seed.label),
                new XElement("descriptionHyperlinks", 
                new XElement("ThingDef",thingDef)),
				new XElement("sources",
				new XElement("li", thingDef.defName)));
				
				report.AppendLine(seedXml.ToString());
				
				if (thingDef.plant.harvestedThingDef.IsStuff) {
					return;
				}
				
				float yieldCount = Mathf.Max(Mathf.Round(thingDef.plant.harvestYield / 3f), 4f);
		}

        public static void AddButchery()
        {
            var defDatabase = DefDatabase<SeedDef>.AllDefs.Where(x => x.extractable == true).ToList();
            var se = ResourceBank.ThingCategoryDefOf.SeedExtractable;

            //Iterate through the seed database
            foreach (var seed in defDatabase)
            {
                //Iterate through the sources within each seed
                foreach (ThingDef source in seed.sources)
                {
                    if (source.plant.harvestedThingDef == null) continue;
                    var thisProduce = DefDatabase<ThingDef>.GetNamed(source.plant.harvestedThingDef.defName);
                    if (thisProduce == null) continue;
                    //We don't add butchery things to non-produce harvests like wood.
                    if (thisProduce.IsIngestible == false && thisProduce.devNote != "extractable") continue;
                    //Add butchery product values. Butchering this produce renders this seed
                    if (thisProduce.butcherProducts == null) {
                        thisProduce.butcherProducts = new List<ThingDefCountClass>();;
                    }
                    var seedToAdd = new ThingDefCountClass(seed,seed.seed.extractionValue);
                    //Make the produce drop this seed when processed
                    if (thisProduce.butcherProducts.Count == 0){
                        thisProduce.butcherProducts.Add(seedToAdd);
                    
                    }
                    //Give warning, or ignore if the seed is the same (which would happen if an alt plant exists like for example wild healroot)
                    else if (thisProduce.butcherProducts[0].thingDef != seed) Log.Warning("Warning: the seed " + seed.defName + " wants to be extracted from " + thisProduce.defName + " but this produce already contains seeds for " + thisProduce.butcherProducts[0].thingDef.defName + ". This will need to be resolved manually, please report.");
                    //Add category
                    if (!thisProduce.thingCategories.Contains(se)) {
                        thisProduce.thingCategories.Add(se);
                        if (!se.childThingDefs.Contains(thisProduce)) {
                            se.childThingDefs.Add(thisProduce);
                        }
                    }
                }
            }
            se.ResolveReferences();
            DefDatabase<RecipeDef>.GetNamed("ExtractSeeds").ResolveReferences();
        }
	}
}