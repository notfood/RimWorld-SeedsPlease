using RimWorld;
using Verse;

namespace SeedsPleaseLite
{
    public static class ResourceBank
    {
        [DefOf]
        public static class JobDefOf
        {
            public static JobDef SowWithSeeds;
        }

        [DefOf]
        public static class ThingDefOf
        {
            public static ThingDef Seed_Psychoid;
        }

        [DefOf]
        public static class ThingCategoryDefOf
        {
            public static ThingCategoryDef SeedExtractable;
            public static ThingCategoryDef SeedsCategory;
            
        }

        [DefOf]
        public static class RecipeDefOf
        {
            public static RecipeDef ExtractSeeds;
        }
    }
}