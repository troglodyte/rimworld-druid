using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Leatherworks.HarmonyPatches
{
    /// <summary>
    /// Studded recipes consume leather and metal. Vanilla may pick the metal as the
    /// dominant ingredient (and so the stuff); force the leather back and record the metal on the comp.
    /// </summary>
    [HarmonyPatch(typeof(GenRecipe), nameof(GenRecipe.MakeRecipeProducts))]
    public static class Patch_MakeRecipeProducts
    {
        public static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result, List<Thing> ingredients)
        {
            foreach (Thing product in __result)
            {
                CompStuddedMetal comp = product.TryGetComp<CompStuddedMetal>();
                if (comp != null && ingredients != null)
                    ApplyIngredients(product, comp, ingredients);
                yield return product;
            }
        }

        private static void ApplyIngredients(Thing product, CompStuddedMetal comp, List<Thing> ingredients)
        {
            ThingDef metal = ingredients.Select(t => t.def).FirstOrDefault(CompStuddedMetal.IsStudMetal);
            if (metal != null)
                comp.metal = metal;

            ThingDef leather = ingredients.Select(t => t.def)
                .FirstOrDefault(d => d.IsStuff && d.stuffProps.categories.Contains(StuffCategoryDefOf.Leathery));
            if (leather != null && product.Stuff != leather)
            {
                product.SetStuffDirect(leather);
                product.HitPoints = product.MaxHitPoints;
            }
        }
    }
}
