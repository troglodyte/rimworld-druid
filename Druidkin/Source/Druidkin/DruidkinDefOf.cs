using RimWorld;
using Verse;

namespace Druidkin
{
    [DefOf]
    public static class DruidkinDefOf
    {
        public static HediffDef Druidkin_WildShapeForm;

        /// Core defines a Body render skip flag, but vanilla's own RenderSkipFlagDefOf
        /// lists every flag except that one, so it has to be resolved here instead.
        public static RenderSkipFlagDef Body;

        static DruidkinDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DruidkinDefOf));
        }
    }
}
