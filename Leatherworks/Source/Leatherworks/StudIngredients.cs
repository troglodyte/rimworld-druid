using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Leatherworks
{
    public static class StudIngredients
    {
        public static Thing FindLeather(IEnumerable<Thing> ingredients, StuffCategoryDef leathery)
        {
            return ingredients?.FirstOrDefault(t => t.def.IsStuff && t.def.stuffProps.categories.Contains(leathery));
        }

        public static bool MakesStuddedApparel(RecipeDef recipe)
        {
            return recipe?.products != null
                && recipe.products.Any(p => p.thingDef.HasComp(typeof(CompStuddedMetal)));
        }
    }
}
