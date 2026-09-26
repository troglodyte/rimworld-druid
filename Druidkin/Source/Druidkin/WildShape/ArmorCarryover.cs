using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Druidkin
{
    /// Armor a druid wore into a shift keeps protecting them, at a fraction of its worth.
    /// Worn apparel protects per body part, but an animal form has no apparel slots, so
    /// the gear is collapsed into one overall rating the same way vanilla's Gear tab does
    /// and applied as a flat armor stat offset for the length of the shift.
    public static class ArmorCarryover
    {
        private static readonly StatDef[] ArmorStats =
        {
            StatDefOf.ArmorRating_Sharp,
            StatDefOf.ArmorRating_Blunt,
            StatDefOf.ArmorRating_Heat
        };

        /// Must run before the gear is stripped: afterwards worn apparel is mixed in with
        /// carried items, and spare armor in a pack should not count.
        public static Dictionary<StatDef, float> FromWornApparel(Pawn pawn, float fraction)
        {
            Dictionary<StatDef, float> result = new Dictionary<StatDef, float>();
            List<Apparel> worn = pawn?.apparel?.WornApparel;
            List<BodyPartRecord> allParts = pawn?.RaceProps?.body?.AllParts;
            if (fraction <= 0f || worn.NullOrEmpty() || allParts.NullOrEmpty())
            {
                return result;
            }

            foreach (StatDef stat in ArmorStats)
            {
                List<(float, IList<float>)> parts = new List<(float, IList<float>)>(allParts.Count);
                foreach (BodyPartRecord part in allParts)
                {
                    List<float> layers = new List<float>();
                    foreach (Apparel apparel in worn)
                    {
                        if (apparel.def.apparel.CoversBodyPart(part))
                        {
                            layers.Add(apparel.GetStatValue(stat));
                        }
                    }
                    parts.Add((part.coverageAbs, layers));
                }

                float bonus = OverallArmor(parts) * fraction;
                if (bonus > 0f)
                {
                    result[stat] = bonus;
                }
            }

            return result;
        }

        /// Vanilla's overall armor from ITab_Pawn_Gear, minus the pawn's natural armor:
        /// each part blocks 1 - product(1 - armor / 2) over the layers covering it,
        /// weighted by the part's share of the body, and the total is scaled back to 0-2.
        public static float OverallArmor(IList<(float coverage, IList<float> layers)> parts)
        {
            float blocked = 0f;
            foreach ((float coverage, IList<float> layers) in parts)
            {
                float passes = 1f;
                foreach (float armor in layers)
                {
                    passes *= 1f - Mathf.Clamp01(armor / 2f);
                }
                blocked += coverage * (1f - passes);
            }

            return Mathf.Clamp(blocked * 2f, 0f, 2f);
        }
    }
}
