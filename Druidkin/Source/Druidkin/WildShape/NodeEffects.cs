using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Druidkin
{
    /// Abstract base for polymorphic effects attached to a WildShapeNodeDef.
    /// Callers never inspect an effect's concrete type; they invoke hooks,
    /// so new behaviours can be added without modifying the gene, hediff or UI.
    public abstract class NodeEffect
    {
        public virtual bool UnlocksForm(DruidkinAnimalFormDef form) => false;

        public virtual void ApplyToStage(DruidkinAnimalFormDef form, HediffStage stage) { }

        public virtual void ModifyShiftDuration(ref float ticks) { }

        public virtual void ModifyCooldown(ref float ticks) { }

        public virtual void OnRevert(Pawn pawn) { }
    }

    /// Grants access to shift into a specific animal form.
    public class NodeEffect_UnlockForm : NodeEffect
    {
        public DruidkinAnimalFormDef form;

        public override bool UnlocksForm(DruidkinAnimalFormDef form) => this.form == form;
    }

    /// Enhances a specific animal form with a stat offset or factor while transformed.
    public class NodeEffect_FormUpgrade : NodeEffect
    {
        public DruidkinAnimalFormDef form;
        public StatDef stat;
        public float offset = 0f;
        public float factor = 1f;

        public override void ApplyToStage(DruidkinAnimalFormDef form, HediffStage stage)
        {
            if (this.form != form || stat == null || stage == null)
            {
                return;
            }

            if (offset != 0f)
            {
                if (stage.statOffsets == null)
                {
                    stage.statOffsets = new List<StatModifier>();
                }
                stage.statOffsets.Add(new StatModifier { stat = stat, value = offset });
            }

            if (factor != 1f)
            {
                if (stage.statFactors == null)
                {
                    stage.statFactors = new List<StatModifier>();
                }
                stage.statFactors.Add(new StatModifier { stat = stat, value = factor });
            }
        }
    }

    /// Druid-wide perk altering shift duration, cooldown, or health restored on reverting.
    public class NodeEffect_Perk : NodeEffect
    {
        public float shiftDurationFactor = 1f;
        public float cooldownFactor = 1f;
        public float healOnRevert = 0f;

        public override void ModifyShiftDuration(ref float ticks)
        {
            ticks *= shiftDurationFactor;
        }

        public override void ModifyCooldown(ref float ticks)
        {
            ticks *= cooldownFactor;
        }

        public override void OnRevert(Pawn pawn)
        {
            if (healOnRevert <= 0f || pawn?.health?.hediffSet == null)
            {
                return;
            }

            float remainingToHeal = healOnRevert;
            List<Hediff_Injury> injuries = new List<Hediff_Injury>();
            pawn.health.hediffSet.GetHediffs(ref injuries, h => h.CanHealNaturally() || h is Hediff_Injury);

            foreach (Hediff_Injury injury in injuries)
            {
                if (remainingToHeal <= 0f)
                {
                    break;
                }

                float healAmount = Mathf.Min(remainingToHeal, injury.Severity);
                injury.Heal(healAmount);
                remainingToHeal -= healAmount;
            }
        }
    }
}
