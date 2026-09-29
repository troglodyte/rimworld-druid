using System.Linq;
using RimWorld;
using Verse;

namespace Leatherworks
{
    public class CompProperties_StuddedMetal : CompProperties
    {
        public CompProperties_StuddedMetal()
        {
            compClass = typeof(CompStuddedMetal);
        }
    }

    /// <summary>Remembers which metal the studs are made of. Crafting sets it; anything else rolls one.</summary>
    public class CompStuddedMetal : ThingComp
    {
        public ThingDef metal;

        public static bool IsStudMetal(ThingDef def)
        {
            return def != null && def.IsStuff && def.stuffProps.categories.Contains(StuffCategoryDefOf.Metallic);
        }

        public override void PostPostMake()
        {
            base.PostPostMake();
            if (metal == null)
                metal = RandomMetal();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Defs.Look(ref metal, "studMetal");
            if (Scribe.mode == LoadSaveMode.PostLoadInit && metal == null)
                metal = RandomMetal();
        }

        public override string TransformLabel(string label)
        {
            return StudLabel.Format(label, metal?.label);
        }

        public override string CompInspectStringExtra()
        {
            return metal == null ? null : "LW_StudMetal".Translate(metal.LabelCap);
        }

        private static ThingDef RandomMetal()
        {
            return DefDatabase<ThingDef>.AllDefs.Where(IsStudMetal)
                .RandomElementByWeightWithFallback(d => d == ThingDefOf.Steel ? 10f : 1f, ThingDefOf.Steel);
        }
    }
}
