using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;

namespace Leatherworks.HarmonyPatches
{
    /// <summary>Records the metal consumed by a studded recipe on the product's comp.</summary>
    [HarmonyPatch(typeof(GenRecipe), nameof(GenRecipe.MakeRecipeProducts))]
    public static class Patch_MakeRecipeProducts
    {
        public static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result, List<Thing> ingredients)
        {
            foreach (Thing product in __result)
            {
                CompStuddedMetal comp = product.TryGetComp<CompStuddedMetal>();
                if (comp != null && ingredients != null)
                    ApplyIngredients(comp, ingredients);
                yield return product;
            }
        }

        private static void ApplyIngredients(CompStuddedMetal comp, List<Thing> ingredients)
        {
            ThingDef metal = ingredients.Select(t => t.def).FirstOrDefault(CompStuddedMetal.IsStudMetal);
            if (metal != null)
                comp.metal = metal;
        }
    }
}
