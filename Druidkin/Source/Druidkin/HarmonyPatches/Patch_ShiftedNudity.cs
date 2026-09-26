using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Druidkin.HarmonyPatches
{
    /// A shifted druid is an animal, so clothing rules do not apply to them: no
    /// "uncovered groin/chest" or "body covered" moodlets from Ideology nudity precepts,
    /// no opinion penalties from others judging their exposure, and no vanilla "naked"
    /// thought. Each vanilla worker decides in its own override, so those are patched.
    public static class ShiftedNudity
    {
        internal static readonly string[] PreceptWorkers =
        {
            "ThoughtWorker_Precept_AnyBodyPartButGroinCovered",
            "ThoughtWorker_Precept_AnyBodyPartButHairOrFaceCovered",
            "ThoughtWorker_Precept_AnyBodyPartCovered",
            "ThoughtWorker_Precept_FaceCovered",
            "ThoughtWorker_Precept_GroinChestHairOrFaceUncovered",
            "ThoughtWorker_Precept_GroinChestOrHairUncovered",
            "ThoughtWorker_Precept_GroinOrChestUncovered",
            "ThoughtWorker_Precept_GroinUncovered"
        };

        internal static IEnumerable<MethodBase> Declared(IEnumerable<string> typeNames, string method)
        {
            foreach (string name in typeNames)
            {
                MethodInfo target = AccessTools.DeclaredMethod(AccessTools.TypeByName("RimWorld." + name), method);
                if (target != null)
                {
                    yield return target;
                }
            }
        }
    }

    /// The shifted pawn's own clothing thoughts.
    [HarmonyPatch]
    public static class Patch_NudityThought_Self
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (MethodBase m in ShiftedNudity.Declared(ShiftedNudity.PreceptWorkers, "ShouldHaveThought"))
            {
                yield return m;
            }
            yield return AccessTools.DeclaredMethod(typeof(ThoughtWorker_PsychologicallyNude), "CurrentStateInternal");
        }

        public static bool Prefix(Pawn __0, ref ThoughtState __result)
        {
            if (!WildShapeUtility.IsShifted(__0))
            {
                return true;
            }
            __result = ThoughtState.Inactive;
            return false;
        }
    }

    /// Other pawns' opinion of how a shifted pawn is dressed.
    [HarmonyPatch]
    public static class Patch_NudityThought_Social
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            return ShiftedNudity.Declared(ShiftedNudity.PreceptWorkers.Select(n => n + "_Social"), "ShouldHaveThought");
        }

        public static bool Prefix(Pawn __1, ref ThoughtState __result)
        {
            if (!WildShapeUtility.IsShifted(__1))
            {
                return true;
            }
            __result = ThoughtState.Inactive;
            return false;
        }
    }
}
