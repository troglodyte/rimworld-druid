using Verse;

namespace Druidkin
{
    /// Holds every tuning knob for wild shape progression on the gene def.
    /// Keeping all numbers here lets balance adjustments happen in XML without
    /// recompiling or touching C# logic.
    public class WildShapeProgressionExtension : DefModExtension
    {
        public int pointsPerLevel = 1;
        public int maxLevel = 20;

        /// XP required to advance from level x to x+1.
        public SimpleCurve xpToNextLevel;

        /// Skill level to multiplier on each form's statBoostFactor.
        /// Level 10 is anchored at 1.00 so unscaled forms retain their hand-tuned baselines.
        public SimpleCurve masteryFactorByLevel;

        /// XP earned for a full day (60,000 ticks) spent shifted.
        public float xpPerDayShifted = 600f;

        /// XP earned per point of damage dealt while shifted to pawns outside the druid's faction.
        public float xpPerDamageDealt = 4f;

        /// XP earned per point of damage taken while shifted.
        public float xpPerDamageTaken = 2f;

        /// Fraction of Animals skill XP gained that is mirrored to wild shape progression.
        public float animalsXpFraction = 0.5f;
    }
}
