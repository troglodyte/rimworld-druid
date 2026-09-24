using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Druidkin
{
    public class Dialog_ChooseAnimalForm : Window
    {
        private readonly Pawn pawn;
        private readonly int durationTicks;
        private readonly CompAbilityEffect_WildShape sourceComp;

        public override Vector2 InitialSize => new Vector2(420f, 520f);

        public Dialog_ChooseAnimalForm(Pawn pawn, int durationTicks, CompAbilityEffect_WildShape sourceComp = null)
        {
            this.pawn = pawn;
            this.durationTicks = durationTicks;
            this.sourceComp = sourceComp;
            doCloseX = true;
            absorbInputAroundWindow = true;
            forcePause = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 36f), "Druidkin_ChooseForm".Translate());

            Gene_Druid gene = pawn?.genes?.GetFirstGeneOfType<Gene_Druid>();
            float y = 38f;

            if (gene != null)
            {
                Text.Font = GameFont.Small;
                GUI.color = new Color(0.85f, 0.85f, 0.85f);
                string summary = "Druidkin_ChooseFormSummary".Translate(
                    gene.Level,
                    gene.MasteryFactor.ToStringPercent());
                Widgets.Label(new Rect(0f, y, inRect.width, 26f), summary);
                GUI.color = Color.white;
                y += 28f;
            }

            Widgets.DrawLineHorizontal(0f, y, inRect.width);
            y += 10f;

            // Only forms the druid knows are displayed in the picker
            List<DruidkinAnimalFormDef> forms = DefDatabase<DruidkinAnimalFormDef>.AllDefsListForReading
                .Where(f => gene == null || gene.Knows(f))
                .ToList();

            if (forms.Count == 0)
            {
                Text.Font = GameFont.Small;
                GUI.color = Color.gray;
                Widgets.Label(new Rect(0f, y, inRect.width, 32f), "Druidkin_NoFormsUnlocked".Translate());
                GUI.color = Color.white;
                return;
            }

            Text.Font = GameFont.Small;
            foreach (DruidkinAnimalFormDef form in forms)
            {
                Rect rowRect = new Rect(0f, y, inRect.width, 34f);
                if (Widgets.ButtonText(rowRect, form.LabelCap))
                {
                    if (WildShapeUtility.TryTransform(pawn, form, durationTicks, out string failReason))
                    {
                        sourceComp?.Notify_SuccessfulTransform();
                        Close();
                    }
                    else
                    {
                        Messages.Message(failReason, MessageTypeDefOf.RejectInput, historical: false);
                    }
                }

                if (!form.description.NullOrEmpty())
                {
                    TooltipHandler.TipRegion(rowRect, form.description);
                }

                y += 38f;
            }
        }
    }
}
