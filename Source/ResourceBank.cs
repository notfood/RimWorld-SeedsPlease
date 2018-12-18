using RimWorld;
using Verse;

namespace SeedsPlease
{
    public static class ResourceBank
    {
        public static readonly string StringPlantMinFertlity = "FertilityRequirement".Translate ();
        public static readonly string StringHarvestMultiplier = "HarvestMultiplier".Translate ();
        public static readonly string StringSeedMultiplier = "SeedMultiplier".Translate ();
        public static readonly string StringSeedBaseChance = "SeedBaseChance".Translate ();
        public static readonly string StringSeedExtraChance = "SeedExtraChance".Translate ();

        [DefOf]
        public static class JobDefOf
        {
            public static JobDef SowWithSeeds;
        }

        [DefOf]
        public static class ThingDefOf
        {
            public static ThingDef PlantProcessingTable;
            public static ThingDef Seed_Psychoid;
        }
    }
}