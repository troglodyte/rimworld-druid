using RimWorld;
using Verse;

namespace Druidkin
{
    public class HediffCompProperties_WildShapeXp : HediffCompProperties
    {
        public HediffCompProperties_WildShapeXp()
        {
            compClass = typeof(HediffComp_WildShapeXp);
        }
    }

    /// Awards wild shape progression XP for time spent transformed and damage taken while shifted.
    public class HediffComp_WildShapeXp : HediffComp
    {
        private const int IntervalTicks = 250;
        private const float TicksPerDay = 60000f;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (Pawn == null || !Pawn.IsHashIntervalTick(IntervalTicks))
            {
                return;
            }

            Gene_Druid gene = Pawn.genes?.GetFirstGeneOfType<Gene_Druid>();
            if (gene?.Progression != null)
            {
                float xp = gene.Progression.xpPerDayShifted * IntervalTicks / TicksPerDay;
                gene.GainXp(xp);
            }
        }

        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);

            if (totalDamageDealt <= 0f || Pawn == null)
            {
                return;
            }

            Gene_Druid gene = Pawn.genes?.GetFirstGeneOfType<Gene_Druid>();
            if (gene?.Progression != null && gene.Progression.xpPerDamageTaken > 0f)
            {
                gene.GainXp(gene.Progression.xpPerDamageTaken * totalDamageDealt);
            }
        }
    }
}
