using RimWorld;
using Verse;

namespace Druidkin
{
    [DefOf]
    public static class DruidkinDefOf
    {
        public static HediffDef Druidkin_WildShapeForm;

        static DruidkinDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DruidkinDefOf));
        }
    }
}
