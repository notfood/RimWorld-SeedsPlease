using Verse;
using HarmonyLib;
using System.Linq;
using System.Reflection;

namespace SeedsPleaseLite
{
    [HarmonyPatch]
    static class Patch_Achtung
    {
        static MethodBase target;

        static bool Prepare()
        {
            var mod = LoadedModManager.RunningMods.FirstOrDefault(m => m.Name == "Achtung!");
            if (mod == null) {
                return false;
            }

            var type = mod.assemblies.loadedAssemblies
                        .FirstOrDefault(a => a.GetName().Name == "AchtungMod")?
                        .GetType("AchtungMod.JobDriver_SowAll");

            if (type == null) {
                Log.Warning("SeedsPlease :: Can't patch Achtung! No JobDriver_SowAll");

                return false;
            }

            target = AccessTools.DeclaredMethod(type, "CanStart");
            if (target == null) {
                Log.Warning("SeedsPlease :: Can't patch Achtung! No JobDriver_SowAll.CanStart");

                return false;
            }

            return true;
        }

        static MethodBase TargetMethod()
        {
            return target;
        }

        static bool Prefix()
        {
            return false;
        }
    }
}