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

            Gene_Druid gene = parent.pawn?.genes?.GetFirstGeneOfType<Gene_Druid>();
            int duration = gene != null ? gene.ShiftDurationTicks(Props.durationTicks) : Props.durationTicks;

            Find.WindowStack.Add(new Dialog_ChooseAnimalForm(parent.pawn, duration, this));
        }

        public void Notify_SuccessfulTransform()
        {
            Gene_Druid gene = parent.pawn?.genes?.GetFirstGeneOfType<Gene_Druid>();
            int baseCooldown = parent.def.cooldownTicksRange.min;
            int finalCooldown = gene != null ? gene.CooldownTicks(baseCooldown) : baseCooldown;
            parent.StartCooldown(finalCooldown);
        }
    }
}
