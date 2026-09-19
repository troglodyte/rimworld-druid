using System.Collections.Generic;
using RimWorld;
using System.Linq;
using Verse;

namespace Druidkin
{
    /// Entry and exit for wild shape. The druid is the same Pawn object throughout -
    /// never despawned, never regenerated, never passed to the world - so there is no
    /// faction, spawn or world-pawn state to save and put back.
    public static class WildShapeUtility
    {
        public static Hediff_WildShapeForm GetShiftHediff(Pawn pawn)
        {
            return pawn?.health?.hediffSet?
                .GetFirstHediffOfDef(DruidkinDefOf.Druidkin_WildShapeForm) as Hediff_WildShapeForm;
        }

        public static bool IsShifted(Pawn pawn)
        {
            return GetShiftHediff(pawn) != null;
        }

        public static bool TryTransform(Pawn pawn, DruidkinAnimalFormDef form, int durationTicks, out string failReason)
        {
            failReason = null;

            if (pawn == null || form == null)
            {
                failReason = "Druidkin_MustBeSpawned".Translate();
                return false;
            }

            if (!pawn.Spawned)
            {
                failReason = "Druidkin_MustBeSpawned".Translate();
                return false;
            }

            if (pawn.Dead || pawn.Downed)
            {
                failReason = "Druidkin_MustBeHealthy".Translate();
                return false;
            }

            if (IsShifted(pawn))
            {
                failReason = "Druidkin_AlreadyShifted".Translate();
                return false;
            }

            Hediff_WildShapeForm hediff =
                (Hediff_WildShapeForm)HediffMaker.MakeHediff(DruidkinDefOf.Druidkin_WildShapeForm, pawn);

            // Both must be set before the hediff is added: its stage and its melee tools
            // are derived from the form, and adding it is what triggers that derivation.
            hediff.form = form;
            hediff.ticksRemaining = durationTicks;
            hediff.TakeGear(StripGear(pawn));

            pawn.health.AddHediff(hediff);

            // Whatever the druid was doing is very likely work they can no longer do.
            pawn.jobs?.StopAll();

            return true;
        }

        public static void Revert(Pawn pawn)
        {
            Hediff_WildShapeForm hediff = GetShiftHediff(pawn);
            if (hediff == null)
            {
                return;
            }

            // Removal is the only exit. Gear comes back from PostRemoved, so the gizmo
            // and the expiring timer take exactly the same path.
            pawn.health.RemoveHediff(hediff);
            pawn.jobs?.StopAll();
        }

        /// Takes weapon, worn apparel and carried items off the druid without dropping
        /// them, so the hediff can hand them back when the shift ends.
        private static List<Thing> StripGear(Pawn pawn)
        {
            List<Thing> stored = new List<Thing>();

            if (pawn.equipment != null)
            {
                foreach (ThingWithComps equipment in pawn.equipment.AllEquipmentListForReading.ToList())
                {
                    pawn.equipment.Remove(equipment);
                    stored.Add(equipment);
                }
            }

            if (pawn.apparel != null)
            {
                foreach (Apparel apparel in pawn.apparel.WornApparel.ToList())
                {
                    pawn.apparel.Remove(apparel);
                    stored.Add(apparel);
                }
            }

            if (pawn.inventory != null)
            {
                foreach (Thing item in pawn.inventory.innerContainer.ToList())
                {
                    pawn.inventory.innerContainer.Remove(item);
                    stored.Add(item);
                }
            }

            return stored;
        }
    }
}
