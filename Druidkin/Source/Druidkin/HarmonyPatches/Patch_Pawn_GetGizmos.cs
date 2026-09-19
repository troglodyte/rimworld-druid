using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;

namespace Druidkin.HarmonyPatches
{
    /// Adds a "revert to human" command to a shifted druid. The wild shape ability
    /// itself is unavailable while shifted, so the way back has to be its own gizmo.
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Patch_Pawn_GetGizmos
    {
        public static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            if (!WildShapeUtility.IsShifted(__instance))
            {
                return;
            }

            Pawn pawn = __instance;
            Command_Action revert = new Command_Action
            {
                defaultLabel = "Druidkin_RevertToHuman".Translate(),
                defaultDesc = "Druidkin_RevertToHumanDesc".Translate(),
                action = () => WildShapeUtility.Revert(pawn)
            };

            __result = __result.Concat(new Gizmo[] { revert });
        }
    }
}
