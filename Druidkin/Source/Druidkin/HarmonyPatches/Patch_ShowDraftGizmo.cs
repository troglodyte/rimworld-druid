using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Druidkin.HarmonyPatches
{
    /// Vanilla already knows how to build the draft toggle - Pawn_DraftController
    /// has its own (internal) GetGizmos, gated behind ShowDraftGizmo. Rather than
    /// duplicating that gizmo, we just open the gate for a shifted druid and let
    /// the vanilla path do the work.
    [HarmonyPatch(typeof(Pawn_DraftController), nameof(Pawn_DraftController.ShowDraftGizmo), MethodType.Getter)]
    public static class Patch_ShowDraftGizmo
    {
        // DIAG: this getter runs every frame while gizmos are built, so only shout once
        // per pawn. If nothing from here ever reaches the log, vanilla is not consulting
        // ShowDraftGizmo for a non-humanlike pawn at all, and the gate is somewhere else.
        private static readonly HashSet<int> Announced = new HashSet<int>();

        public static void Postfix(Pawn ___pawn, ref bool __result)
        {
            if (___pawn == null || !WildShapeUtility.IsShifted(___pawn))
            {
                return;
            }

            if (Announced.Add(___pawn.thingIDNumber))
            {
                Log.Message($"[Druidkin][DIAG] ShowDraftGizmo consulted for {___pawn.LabelShort}: " +
                            $"vanillaResult={__result} drafterNull={___pawn.drafter == null} -> forcing true");
            }

            __result = true;
        }
    }
}
