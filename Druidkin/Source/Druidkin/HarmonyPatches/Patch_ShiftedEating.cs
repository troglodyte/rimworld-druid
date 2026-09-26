using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Druidkin.HarmonyPatches
{
    /// A shifted druid eats as the animal: corpses, raw meat and a meal off the floor
    /// carry no bad memories. Vanilla hands out eating moodlets along three paths, so
    /// each is patched below. The negative filter also feeds food choice, which scores
    /// options by these thoughts, so a shifted druid stops avoiding corpses.
    public static class ShiftedEating
    {
        /// The shifted pawn inside Thing.Ingested right now, if any. Ingestion runs to
        /// completion on the main thread, so a single slot is enough.
        internal static Pawn eater;

        public static bool IsBad(ThoughtDef thought)
        {
            return thought?.stages != null
                && thought.stages.Count > 0
                && thought.stages[0] != null
                && thought.stages[0].baseMoodEffect < 0f;
        }

        public static void RemoveBadThoughts(List<FoodUtility.ThoughtFromIngesting> thoughts)
        {
            thoughts.RemoveAll(t => IsBad(t.thought));
        }
    }

    /// Ate corpse, raw food, insect meat, cannibalism and similar.
    [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.ThoughtsFromIngesting))]
    public static class Patch_FoodUtility_ThoughtsFromIngesting
    {
        public static void Postfix(Pawn ingester, List<FoodUtility.ThoughtFromIngesting> __result)
        {
            if (__result != null && WildShapeUtility.IsShifted(ingester))
            {
                ShiftedEating.RemoveBadThoughts(__result);
            }
        }
    }

    /// Ate without a table, which the ingest toil adds on its own.
    [HarmonyPatch(typeof(MemoryThoughtHandler), nameof(MemoryThoughtHandler.TryGainMemory),
        new Type[] { typeof(Thought_Memory), typeof(Pawn) })]
    public static class Patch_MemoryThoughtHandler_TryGainMemory
    {
        public static bool Prefix(MemoryThoughtHandler __instance, Thought_Memory newThought)
        {
            return newThought?.def != ThoughtDefOf.AteWithoutTable
                || !WildShapeUtility.IsShifted(__instance.pawn);
        }
    }

    /// Ideology food precepts react to history events recorded while eating. Marks the
    /// span of Thing.Ingested so those events can be dropped for a shifted eater.
    [HarmonyPatch(typeof(Thing), nameof(Thing.Ingested))]
    public static class Patch_Thing_Ingested
    {
        public static void Prefix(Pawn ingester)
        {
            ShiftedEating.eater = WildShapeUtility.IsShifted(ingester) ? ingester : null;
        }

        public static void Finalizer()
        {
            ShiftedEating.eater = null;
        }
    }

    [HarmonyPatch(typeof(HistoryEventsManager), nameof(HistoryEventsManager.RecordEvent))]
    public static class Patch_HistoryEventsManager_RecordEvent
    {
        public static bool Prefix()
        {
            return ShiftedEating.eater == null;
        }
    }
}
