using System.Collections.Generic;
using Verse;

namespace Druidkin
{
    /// One shape a druid can take. A form is a single PawnKindDef reference plus a
    /// difficulty knob, so forms added by animal mods work without new code and
    /// without a hand-authored stat table that would drift from the animal's own.
    public class DruidkinAnimalFormDef : Def
    {
        public PawnKindDef pawnKind;

        /// Scales everything derived from the animal - melee power, move speed and
        /// durability - so a druid's bear is worth more than a wild bear.
        public float statBoostFactor = 1f;

        /// The animal's ThingDef, which is where tools, stats and health scale live.
        public ThingDef Race => pawnKind?.race;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (pawnKind == null)
            {
                yield return "pawnKind is null";
                yield break;
            }

            if (Race == null)
            {
                yield return $"pawnKind {pawnKind.defName} has no race";
            }
            else if (Race.race == null)
            {
                yield return $"race {Race.defName} has no RaceProperties, so it is not a pawn";
            }

            if (statBoostFactor <= 0f)
            {
                yield return $"statBoostFactor must be positive, got {statBoostFactor}";
            }
        }
    }
}
