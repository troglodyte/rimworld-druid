using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Druidkin.HarmonyPatches
{
    /// A shifted druid sits in the Animals tab looking exactly like livestock, so every
    /// designator that acts on owned animals has to refuse it.
    ///
    /// Vanilla has four animal designators. Hunt and Tame already reject player-faction
    /// animals on their own, so only these two need a guard - the list is the one place
    /// that changes if that stops being true (or if a mod adds another designator).
    [HarmonyPatch]
    public static class Patch_AnimalDesignators
    {
        private static readonly Type[] GuardedDesignators =
        {
            typeof(Designator_Slaughter),
            typeof(Designator_ReleaseAnimalToWild),
        };

        public static IEnumerable<MethodBase> TargetMethods()
        {
            HashSet<MethodBase> seen = new HashSet<MethodBase>();

            foreach (Type type in GuardedDesignators)
            {
                // Prefer the subclass's own override. Fall back to the inherited base
                // method if it doesn't declare one - patching the base is broader than
                // we need, but the postfix only ever fires for a shifted druid, so the
                // extra coverage is harmless. The set keeps us from handing Harmony the
                // same MethodBase twice when two designators share a base.
                MethodBase method = AccessTools.DeclaredMethod(type, "CanDesignateThing")
                                    ?? AccessTools.Method(type, "CanDesignateThing");

                if (method == null)
                {
                    Log.Warning($"[Druidkin] No CanDesignateThing found on {type.Name}; shifted druids are NOT protected from it.");
                    continue;
                }

                if (seen.Add(method))
                {
                    yield return method;
                }
            }
        }

        public static void Postfix(Thing t, ref AcceptanceReport __result)
        {
            if (!__result.Accepted)
            {
                return;
            }

            if (t is Pawn pawn && WildShapeUtility.IsShifted(pawn))
            {
                __result = "Druidkin_NotLivestock".Translate();
            }
        }
    }
}
