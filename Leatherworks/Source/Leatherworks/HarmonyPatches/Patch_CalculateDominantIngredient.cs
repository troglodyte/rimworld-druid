using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Leatherworks.HarmonyPatches
{
    /// <summary>
    /// Vanilla picks a random stuff ingredient, weighted by stack count, as the dominant one, so a
    /// studded recipe could make the item (and its unfinished thing) out of the metal. Always pick the leather.
    /// </summary>
    [HarmonyPatch(typeof(Toils_Recipe), "CalculateDominantIngredient")]
    public static class Patch_CalculateDominantIngredient
    {
        public static void Postfix(Job job, List<Thing> ingredients, ref Thing __result)
        {
            if (!StudIngredients.MakesStuddedApparel(job?.RecipeDef))
                return;
            List<Thing> pool = (job.GetTarget(TargetIndex.B).Thing as UnfinishedThing)?.ingredients ?? ingredients;
            Thing leather = StudIngredients.FindLeather(pool, StuffCategoryDefOf.Leathery);
            if (leather != null)
                __result = leather;
        }
    }
}
