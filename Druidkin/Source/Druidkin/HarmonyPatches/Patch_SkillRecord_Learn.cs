using HarmonyLib;
using RimWorld;
using Verse;

namespace Druidkin.HarmonyPatches
{
    /// Awards wild shape progression XP when a druid handles animals.
    /// Covers taming, training, milking, shearing, slaughtering and any other
    /// modded work that trains Animals.
    [HarmonyPatch(typeof(SkillRecord), nameof(SkillRecord.Learn))]
    public static class Patch_SkillRecord_Learn
    {
        public static void Postfix(SkillRecord __instance, float xp, bool direct, bool ignoreLearnRate)
        {
            if (xp <= 0f)
            {
                return;
            }

            if (__instance?.def != SkillDefOf.Animals)
            {
                return;
            }

            Pawn pawn = __instance.Pawn;
            if (pawn == null)
            {
                return;
            }

            Gene_Druid gene = pawn.genes?.GetFirstGeneOfType<Gene_Druid>();
            if (gene?.Progression != null && gene.Progression.animalsXpFraction > 0f)
            {
                gene.GainXp(xp * gene.Progression.animalsXpFraction);
            }
        }
    }
}
