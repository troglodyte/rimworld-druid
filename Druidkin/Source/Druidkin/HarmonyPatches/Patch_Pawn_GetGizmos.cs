using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;

namespace Druidkin.HarmonyPatches
{
    /// Adds a "revert to human" command to any pawn currently wearing an animal form.
    /// The animal pawn has no gene of its own to hang an ability off, so the command
    /// is injected here instead.
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Patch_Pawn_GetGizmos
    {
        private static readonly HashSet<int> GizmoDiagAnnounced = new HashSet<int>();

        public static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            if (!WildShapeUtility.IsShifted(__instance))
            {
                return;
            }

            Command_Action revert = new Command_Action
            {
                defaultLabel = "Druidkin_RevertToHuman".Translate(),
                defaultDesc = "Druidkin_RevertToHumanDesc".Translate(),
                action = () => WildShapeUtility.RevertToHuman(__instance)
            };

            // DIAG: prove GetGizmos runs for the shifted pawn and report the drafter,
            // so we can tell "never called" apart from "called but vanilla skipped draft".
            if (GizmoDiagAnnounced.Add(__instance.thingIDNumber))
            {
                Log.Message($"[Druidkin][DIAG] Pawn.GetGizmos ran for {__instance.LabelShort}: " +
                            $"drafterNull={__instance.drafter == null} humanlike={__instance.RaceProps.Humanlike} " +
                            $"colonistPlayerControlled={__instance.IsColonistPlayerControlled}");
            }

            __result = __result.Concat(new Gizmo[] { revert });

        }
    }
}
