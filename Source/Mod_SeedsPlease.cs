using HarmonyLib;
using Verse;

namespace SeedsPleaseLite
{
    public class Mod_SeedsPlease : Mod
    {
        public Mod_SeedsPlease(ModContentPack content) : base(content)
        {
            new Harmony(this.Content.PackageIdPlayerFacing).PatchAll();
            LongEventHandler.QueueLongEvent(() => SeedsPleaseUtility.Setup(), "SeedsPleaseLite.Setup", false, null);
        }
    }
}