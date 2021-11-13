using RimWorld;
using Verse;

namespace SeedsPleaseLite
{
    public static class ResourceBank
    {
        public static readonly string[] knownPrefixes = new string[] {
            "VG_Plant", "VGP_", "RC2_", "RC_Plant", "TKKN_Plant", "TKKN_", "TM_", "Ogre_AdvHyd_", "Plant_", "WildPlant", "Wild", "Plant", "tree", "Tree"
        };

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