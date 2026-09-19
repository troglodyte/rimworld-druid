using HarmonyLib;
using RimWorld;
using Verse;

namespace Druidkin.HarmonyPatches
{
    /// Hides the human while the animal is being drawn.
    ///
    /// Vanilla has no way to say "render this humanlike as something else" - Anomaly's
    /// mutant hediffs only ever add overlays on top of a human that still draws. What
    /// it does have is the skip-flag mechanism apparel uses to hide hair under a
    /// helmet, and that generalises: a flagged node is dropped before it draws, and it
    /// takes its children with it.
    ///
    /// Body and Head are therefore sufficient for the whole human. In the Humanlike
    /// render tree the ApparelBody node is a child of Body and ApparelHead a child of
    /// Head, and hair, beard, eyes and tattoos all hang off Head, so every one of them
    /// goes when those two do. The Carried node is a sibling at the root and survives,
    /// which is what we want: a hauled thing should stay visible.
    [HarmonyPatch(typeof(PawnRenderTree), "AdjustParms")]
    public static class Patch_PawnRenderTree_AdjustParms
    {
        public static void Postfix(PawnRenderTree __instance, ref PawnDrawParms parms)
        {
            if (!WildShapeUtility.IsShifted(__instance.pawn))
            {
                return;
            }

            // RenderSkipFlagDef converts implicitly to the ulong bitmask parms carries.
            parms.skipFlags |= DruidkinDefOf.Body;
            parms.skipFlags |= RenderSkipFlagDefOf.Head;
        }
    }
}
