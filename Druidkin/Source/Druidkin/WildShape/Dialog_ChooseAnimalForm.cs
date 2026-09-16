using RimWorld;
using UnityEngine;
using Verse;

namespace Druidkin
{
    public class Dialog_ChooseAnimalForm : Window
    {
        private readonly Pawn pawn;
        private readonly int durationTicks;

        public override Vector2 InitialSize => new Vector2(400f, 500f);

        public Dialog_ChooseAnimalForm(Pawn pawn, int durationTicks)
        {
            this.pawn = pawn;
            this.durationTicks = durationTicks;
            doCloseX = true;
            absorbInputAroundWindow = true;
            forcePause = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 40f), "Druidkin_ChooseForm".Translate());
            Text.Font = GameFont.Small;

            float y = 45f;
            foreach (DruidkinAnimalFormDef form in DefDatabase<DruidkinAnimalFormDef>.AllDefsListForReading)
            {
                Rect rowRect = new Rect(0f, y, inRect.width, 32f);
                if (Widgets.ButtonText(rowRect, form.LabelCap))
                {
                    if (WildShapeUtility.TryTransform(pawn, form, durationTicks, out string failReason))
                    {
                        Close();
                    }
                    else
                    {
                        Messages.Message(failReason, MessageTypeDefOf.RejectInput, historical: false);
                    }
                }

                y += 36f;
            }
        }
    }
}
