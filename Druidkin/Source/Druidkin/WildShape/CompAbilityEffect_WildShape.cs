using RimWorld;
using Verse;

namespace Druidkin
{
    public class CompProperties_AbilityWildShape : CompProperties_AbilityEffect
    {
        public int durationTicks = 60000;

        public CompProperties_AbilityWildShape()
        {
            compClass = typeof(CompAbilityEffect_WildShape);
        }
    }

    public class CompAbilityEffect_WildShape : CompAbilityEffect
    {
        public new CompProperties_AbilityWildShape Props => (CompProperties_AbilityWildShape)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Find.WindowStack.Add(new Dialog_ChooseAnimalForm(parent.pawn, Props.durationTicks));
        }
    }
}
