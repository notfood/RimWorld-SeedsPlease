using HarmonyLib;
using Verse;
using UnityEngine;
using static SeedsPleaseLite.ModSettings_SeedsPleaseLite;

namespace SeedsPleaseLite
{
    public class Mod_SeedsPlease : Mod
    {
        public Mod_SeedsPlease(ModContentPack content) : base(content)
        {
            new Harmony(this.Content.PackageIdPlayerFacing).PatchAll();
            base.GetSettings<ModSettings_SeedsPleaseLite>();
            LongEventHandler.QueueLongEvent(() => SeedsPleaseUtility.Setup(), null, false, null);
        }

        public override void DoSettingsWindowContents(Rect inRect)
		{
			Listing_Standard options = new Listing_Standard();
			options.Begin(inRect);
            options.Label("SPL.RequiresRestart".Translate());
            options.GapLine();
			options.Label("SPL.Settings.MarketValueModifier".Translate("100%", "20%", "500%") + marketValueModifier.ToStringPercent(), -1f, "SPL.Settings.MarketValueModifier.Desc".Translate());
			marketValueModifier = options.Slider(marketValueModifier, 0.2f, 5f);

            options.Label("SPL.Settings.SeedExtractionModifier".Translate("100%", "20%", "500%") + extractionModifier.ToStringPercent(), -1f, "SPL.Settings.SeedExtractionModifier.Desc".Translate("4"));
			extractionModifier = options.Slider(extractionModifier, 0.2f, 5f);

            options.Label("SPL.Settings.SeedFactorModifier".Translate("100%", "20%", "500%") + seedFactorModifier.ToStringPercent(), -1f, "SPL.Settings.SeedFactorModifier.Desc".Translate("1"));
			seedFactorModifier = options.Slider(seedFactorModifier, 0.2f, 5f);

			options.CheckboxLabeled("SPL.Settings.NoUselessSeeds".Translate(), ref noUselessSeeds, "SPL.Settings.NoUselessSeeds.Desc".Translate());
			
			options.End();
			base.DoSettingsWindowContents(inRect);
		}

		public override string SettingsCategory()
		{
			return "Seeds Please: Lite";
		}

		public override void WriteSettings()
		{
			base.WriteSettings();
		}
    }

    public class ModSettings_SeedsPleaseLite : ModSettings
	{
		public override void ExposeData()
		{
			Scribe_Values.Look<float>(ref marketValueModifier, "marketValueModifier", 1f, false);
            Scribe_Values.Look<float>(ref extractionModifier, "extractionModifier", 1f, false);
            Scribe_Values.Look<float>(ref seedFactorModifier, "seedFactorModifier", 1f, false);
			Scribe_Values.Look<bool>(ref noUselessSeeds, "noUselessSeeds", true, false);
			
			base.ExposeData();
		}

		public static float marketValueModifier = 1f, extractionModifier = 1f, seedFactorModifier = 1f;
		public static bool noUselessSeeds = true;
	}
}