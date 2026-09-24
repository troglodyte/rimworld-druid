using System.Linq;
using Verse;

namespace Druidkin
{
    [StaticConstructorOnStartup]
    public static class DruidkinStartup
    {
        static DruidkinStartup()
        {
            CheckFormUnlocks();
        }

        private static void CheckFormUnlocks()
        {
            var allForms = DefDatabase<DruidkinAnimalFormDef>.AllDefsListForReading;
            var allNodes = DefDatabase<WildShapeNodeDef>.AllDefsListForReading;

            foreach (DruidkinAnimalFormDef form in allForms)
            {
                bool hasUnlock = allNodes.Any(n => n.effects != null && n.effects.Any(e => e.UnlocksForm(form)));
                if (!hasUnlock)
                {
                    Log.Warning($"[Druidkin] Form '{form.defName}' has no WildShapeNodeDef unlocking it and cannot be learned by druids.");
                }
            }
        }
    }
}
