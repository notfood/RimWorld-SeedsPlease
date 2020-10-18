using UnityEngine;
using Verse;

namespace SeedsPlease
{
    public class SeedsPleaseSettings : ModSettings
    {
        public static bool placeSeedsInInventory = true;

        public void DoWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.CheckboxLabeled(ResourceBank.StringSettingsPlaceSeedsInInventory, ref placeSeedsInInventory, ResourceBank.StringSettingsPlaceSeedsInInventoryTooltip);

            listing.End();
            Mod.GetSettings<SeedsPleaseSettings>().Write();
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref placeSeedsInInventory, "pleaseSeedsInInventory", true);
        }
    }
}
