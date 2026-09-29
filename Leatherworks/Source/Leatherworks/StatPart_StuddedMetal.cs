using RimWorld;
using Verse;

namespace Leatherworks
{
    /// <summary>Adds a share of the stud metal's armor power to ArmorRating_Sharp / ArmorRating_Blunt.</summary>
    public class StatPart_StuddedMetal : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (TryGetBonus(req, out float bonus, out _))
                val += bonus;
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (!TryGetBonus(req, out float bonus, out ThingDef metal))
                return null;
            return "LW_StudBonus".Translate(metal.LabelCap, bonus.ToStringByStyle(ToStringStyle.FloatTwo, ToStringNumberSense.Offset));
        }

        private bool TryGetBonus(StatRequest req, out float bonus, out ThingDef metal)
        {
            bonus = 0f;
            metal = req.Thing?.TryGetComp<CompStuddedMetal>()?.metal;
            StatDef power = StuffPowerStat();
            if (metal == null || power == null)
                return false;
            bonus = StudMath.Bonus(metal.GetStatValueAbstract(power), StudMath.StudFactor);
            return true;
        }

        private StatDef StuffPowerStat()
        {
            if (parentStat == StatDefOf.ArmorRating_Sharp) return StatDefOf.StuffPower_Armor_Sharp;
            if (parentStat == StatDefOf.ArmorRating_Blunt) return StatDefOf.StuffPower_Armor_Blunt;
            return null;
        }
    }
}
