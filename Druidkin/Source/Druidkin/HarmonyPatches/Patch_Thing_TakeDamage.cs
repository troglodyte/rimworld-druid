using HarmonyLib;
using RimWorld;
using Verse;

namespace Druidkin.HarmonyPatches
{
    /// Awards progression XP to a shifted druid for damage dealt in combat.
    /// Runs on every damage event in the game, so it returns immediately on
    /// the first failed condition before doing any deeper work.
    [HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
    public static class Patch_Thing_TakeDamage
    {
        public static void Postfix(Thing __instance, DamageInfo dinfo, DamageWorker.DamageResult __result)
        {
            if (__result == null || __result.totalDamageDealt <= 0f)
            {
                return;
            }

            if (!(dinfo.Instigator is Pawn instigator))
            {
                return;
            }

            if (!WildShapeUtility.IsShifted(instigator))
            {
                return;
            }

            if (!(__instance is Pawn target))
            {
                return;
            }

            if (target == instigator)
            {
                return;
            }

            // Outside the druid's faction: stops sparring with colonists or colony animals from becoming an XP farm
            if (instigator.Faction != null && target.Faction == instigator.Faction)
            {
                return;
            }

            Gene_Druid gene = instigator.genes?.GetFirstGeneOfType<Gene_Druid>();
            if (gene?.Progression != null && gene.Progression.xpPerDamageDealt > 0f)
            {
                gene.GainXp(gene.Progression.xpPerDamageDealt * __result.totalDamageDealt);
            }
        }
    }
}
