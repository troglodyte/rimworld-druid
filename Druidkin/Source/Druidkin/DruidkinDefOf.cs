using RimWorld;
using Verse;

namespace Druidkin
{
    [DefOf]
    public static class DruidkinDefOf
    {
        public static HediffDef Druidkin_WildShapeForm;
        public static HediffDef Druidkin_ResidualWounds;

        static DruidkinDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DruidkinDefOf));
        }
    }
}
