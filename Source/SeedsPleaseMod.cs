using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using System.Xml.Linq;
using System.Text;

namespace SeedsPleaseLite
{
    public class SeedsPleaseMod : Mod
    {
        //Normal follows the default rules on if a plant has an extractable seed or not. True and false override the results.
        public enum extractable { Normal, True, False }

        internal static readonly List<string> knownPrefixes = new List<string>() {
            "VG_Plant", "VGP_", "RC2_", "RC_Plant", "TKKN_Plant", "TKKN_", "TM_", "Plant_", "WildPlant", "Wild", "Plant", "tree", "Tree"
        };

        public SeedsPleaseMod(ModContentPack content) : base(content)
        {
            new Harmony("rimworld.seedsplease").PatchAll();
        }

        public static float AssignMarketValueFromHarvest(ThingDef thingDef)
        {
            var harvestedThingDef = thingDef.plant.harvestedThingDef;
            
            //Flat rate value if there's no harvested thing
            if (harvestedThingDef == null) return 0.5f;
			
            //Adjust value based on plant's growth cycle and yield
            float factor = thingDef.plant.harvestYield / thingDef.plant.growDays + thingDef.plant.growDays / thingDef.plant.harvestYield;

            //Adjust value based on harvested thing's value
            float value = harvestedThingDef.BaseMarketValue * factor * 2.5f;
			
            //Adjust value if this plant needs space
            if (thingDef.plant.blockAdjacentSow) value *= 1.5f;
			
            //Adjust value if it's a wild plant
            int cnt = thingDef.plant.wildBiomes?.Count() ?? 0;
            if (cnt > 1) value *= Mathf.Pow(0.95f, cnt);
			
            //Value adjusted based on type
            if (harvestedThingDef == ThingDefOf.WoodLog) value *= 0.2f;
            else if (harvestedThingDef.IsAddictiveDrug) value *= 1.3f;
            else if (harvestedThingDef.IsDrug) value *= 1.2f;
            else if (harvestedThingDef.IsMedicine) value *= 1.1f;
			
            //Adjust value based on skill need
            value *= Mathf.Lerp(0.8f, 1.6f, thingDef.plant.sowMinSkill / 20f);
			
            //Cap value
            if (value > 25f) value = 24.99f;
			
            return Mathf.Round(value * 100f) / 100f;
		}

         public static bool AddMissingSeeds(StringBuilder report) {
            // Filter the database. The thing must be a plant, missing a blueprintDef (the seed assigned), be sowable, have a harvested thing, and not have the seedless comp
            var dd = DefDatabase<ThingDef>.AllDefs.Where(x => x.plant != null && x.blueprintDef == null && x.plant.Sowable && x.plant.harvestedThingDef != null && !x.HasComp(typeof(CompSeedless))).ToList();
            foreach (var thingDef in dd) {
                AddMissingSeed(report, thingDef);
			}
            return (dd.Count() > 0) ? true : false;
		}
		
        static void AddMissingSeed(StringBuilder report, ThingDef thingDef)
        {
            string name = thingDef.defName;
            foreach (string prefix in SeedsPleaseMod.knownPrefixes)
            {
                name = name.Replace(prefix, "");
			}
            name = name.CapitalizeFirst();
			
            report.Append("\n<!-- SeedsPlease :: " + thingDef.defName + "(" + (thingDef.modContentPack.IsCoreMod ? "Patched" : thingDef.modContentPack.PackageId) + ")");
			
            var seed = DefDatabase<ThingDef>.GetNamedSilentFail("Seed_" + name);
            if (seed == null)
            {
                var template = ResourceBank.ThingDefOf.Seed_Psychoid;
				
                seed = new ThingDef()
                {
                    defName = "Seed_" + name,
                    label = name.ToLower() + " seeds",
                    stackLimit = template.stackLimit,
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
                    comps = new List<CompProperties>() {new CompProperties_Seed()},
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
                var seedComp = seed.GetCompProperties<CompProperties_Seed>();
                seedComp.plant = thingDef;
                seedComp.harvestOverride = thingDef.plant.harvestedThingDef;
				
                seed.BaseMarketValue = SeedsPleaseMod.AssignMarketValueFromHarvest(thingDef);
				
                foreach(var category in seed.thingCategories) {
                    category.childThingDefs.Add(seed);
				}
				
                //Add the seed to the database and let it resolve its links with other defs
                DefDatabase<ThingDef>.Add(seed);
                ResolveSpecialReferences(seed, seedComp);
				
                report.Append("Autogenerated as ");
			}
            else
            {
                seed.GetCompProperties<CompProperties_Seed>().sources.Add(thingDef);

                report.Append("Inserted to ");
			}
			
            report.Append(seed.defName + "-->\n");
			
            var seedXml =
            new XElement("ThingDef", new XAttribute("ParentName", "SeedBase"),
				new XElement("defName", seed.defName),
				new XElement("label", seed.label),
                new XElement("descriptionHyperlinks", 
                new XElement("ThingDef",thingDef)),
				new XElement("comps",
                    new XElement("li", new XAttribute("Class", "SeedsPleaseLite.CompProperties_Seed"),
                        new XElement("sources",
				            new XElement("li", thingDef.defName)))));
				
				report.AppendLine(seedXml.ToString());
				
				if (thingDef.plant.harvestedThingDef.IsStuff) {
					return;
				}
				
				float yieldCount = Mathf.Max(Mathf.Round(thingDef.plant.harvestYield / 3f), 4f);
		}

        public static void AddButchery()
        {
            var defDatabase = DefDatabase<ThingDef>.AllDefs.Where(x => x.HasComp(typeof(CompSeed)) && x.GetCompProperties<CompProperties_Seed>().extractable != SeedsPleaseMod.extractable.False).ToList();
            var se = ResourceBank.ThingCategoryDefOf.SeedExtractable;

            //Iterate through the seed database
            foreach (var seed in defDatabase)
            {
                var seedComp = seed.GetCompProperties<CompProperties_Seed>();
                //Iterate through the sources within each seed
                foreach (ThingDef source in seedComp.sources)
                {
                    if (source.plant.harvestedThingDef == null) continue;

                    var thisProduce = DefDatabase<ThingDef>.GetNamed(source.plant.harvestedThingDef.defName);
                    if (thisProduce == null) continue;

                    //We don't add butchery things to non-produce harvests like wood.
                    if (thisProduce.IsIngestible == false && seedComp.extractable != SeedsPleaseMod.extractable.True) continue;

                    //Add butchery product values. Butchering this produce renders this seed
                    if (thisProduce.butcherProducts == null) {
                        thisProduce.butcherProducts = new List<ThingDefCountClass>();;
                    }
                    var seedToAdd = new ThingDefCountClass(seed,seedComp.extractionValue);

                    //Make the produce drop this seed when processed
                    if (thisProduce.butcherProducts.Count == 0){
                        thisProduce.butcherProducts.Add(seedToAdd);
                    
                    }

                    //Give warning, or ignore if the seed is the same (which would happen if an alt plant exists like for example wild healroot)
                    else if (thisProduce.butcherProducts[0].thingDef != seed) Log.Warning("Warning: the seed " + seed.defName + " wants to be extracted from "
                     + thisProduce.defName + " but this produce already contains seeds for " + thisProduce.butcherProducts[0].thingDef.defName + 
                     ". This will need to be resolved manually, please report.");

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

        static public void ResolveSpecialReferences (ThingDef thing, CompProperties_Seed props)
        {
            //Check the seed's sources
            foreach (var sourcePlant in props.sources)
            {
                //Validate source is actually a plant
                if (sourcePlant.plant == null)
                {
                    Log.Warning("SeedsPlease :: " + sourcePlant.defName + " is not a plant.");
                    continue;
				}
				
                //Give this plant a blueprint that equals this seed
                sourcePlant.blueprintDef = thing;
				
                //Set plant reference
                if (props.plant == null && sourcePlant.plant.Sowable) props.plant = sourcePlant;
			}
			
            if (props.plant == null) {
                Log.Warning("SeedsPlease :: " + thing.defName + " has no sowable plant.");
                return;
			}
			
            //Set plant's blueprint?
            if (props.plant.blueprintDef == null) props.plant.blueprintDef = thing;
			
            //If using an override, set it on the plant
            if (props.harvestOverride != null) props.plant.plant.harvestedThingDef = props.harvestOverride;
			else props.harvestOverride = props.plant.plant.harvestedThingDef;
			
            //Set the market value
            if (thing.BaseMarketValue <= 0f && props.harvestOverride != null) thing.BaseMarketValue = SeedsPleaseMod.AssignMarketValueFromHarvest(props.plant);
			
			#if DEBUG
				Log.Message ($"{plant} {harvest?.BaseMarketValue} => {BaseMarketValue}");
			#endif
		}
    }
}