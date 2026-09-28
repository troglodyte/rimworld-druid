using RimWorld;
using UnityEngine;
using Verse;

namespace Druidkin
{
    /// Melee weapons a druid wielded into a shift empower their animal attacks.
    /// Animal forms have no weapon slots, so the weapon's melee combat effectiveness
    /// (DPS) is converted into a MeleeDamageFactor stat offset for the duration of the shift.
    public static class WeaponCarryover
    {
        public const float DefaultReferenceMeleeDps = 20f;

        /// Pure calculation converting weapon DPS and fraction into a MeleeDamageFactor offset.
        public static float DamageFactorOffset(float weaponDps, float fraction, float referenceDps = DefaultReferenceMeleeDps)
        {
            if (weaponDps <= 0f || fraction <= 0f)
            {
                return 0f;
            }

            float benchmark = Mathf.Max(0.001f, referenceDps);
            return (weaponDps / benchmark) * fraction;
        }

        /// Must run before the gear is stripped: afterwards equipped weapons are
        /// in storedGear rather than held in hands.
        public static float FromEquippedWeapon(Pawn pawn, float fraction, float referenceDps = DefaultReferenceMeleeDps)
        {
            if (fraction <= 0f || pawn?.equipment == null)
            {
                return 0f;
            }

            ThingWithComps primary = pawn.equipment.Primary;
            if (primary == null || primary.def == null || !primary.def.IsMeleeWeapon)
            {
                return 0f;
            }

            float dps = primary.GetStatValue(StatDefOf.MeleeWeapon_AverageDPS);
            return DamageFactorOffset(dps, fraction, referenceDps);
        }
    }
}
