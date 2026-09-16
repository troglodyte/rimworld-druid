using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Druidkin
{
    public static class WildShapeUtility
    {
        /// True if this pawn is a druid currently wearing an animal form.
        /// Single source of truth for "is this a shifted druid?" - used by the
        /// gizmo injection and by every animal-designator guard.
        /// DIAG: one-line dump of everything that decides whether a pawn reads as a
        /// colonist. Called either side of the revert respawn so we can see which
        /// step drops it.
        public static void LogPawnState(string stage, Pawn p)
        {
            string guest = "n/a";
            try { guest = p.guest?.GuestStatus.ToString() ?? "null"; } catch { guest = "threw"; }

            Log.Message($"[Druidkin][DIAG] {stage}: name={p.LabelShort} " +
                        $"faction={p.Faction?.Name ?? "NULL"} " +
                        $"isColonist={p.IsColonist} isFreeColonist={p.IsFreeColonist} " +
                        $"hostFaction={p.HostFaction?.Name ?? "none"} guestStatus={guest} " +
                        $"inWorldPawns={Find.WorldPawns.Contains(p)} spawned={p.Spawned}");
        }

        public static bool IsShifted(Pawn pawn)
        {
            return pawn?.health?.hediffSet?.GetFirstHediffOfDef(DruidkinDefOf.Druidkin_WildShapeForm) is Hediff_WildShapeForm;
        }

        public static bool TryTransform(Pawn pawn, DruidkinAnimalFormDef form, int durationTicks, out string failReason)
        {
            failReason = null;

            if (!pawn.Spawned)
            {
                failReason = "Druidkin_MustBeSpawned".Translate();
                return false;
            }

            if (pawn.Dead || pawn.Downed)
            {
                failReason = "Druidkin_MustBeHealthy".Translate();
                return false;
            }

            if (pawn.health.hediffSet.HasHediff(DruidkinDefOf.Druidkin_WildShapeForm))
            {
                failReason = "Druidkin_AlreadyShifted".Translate();
                return false;
            }

            Map map = pawn.Map;
            IntVec3 position = pawn.Position;
            Faction faction = pawn.Faction;

            List<Thing> storedGear = StripGear(pawn);

            PawnGenerationRequest request = new PawnGenerationRequest(
                form.pawnKind,
                faction,
                PawnGenerationContext.NonPlayer,
                tile: -1,
                forceGenerateNewPawn: true,
                allowDead: false,
                allowDowned: false,
                canGeneratePawnRelations: false,
                colonistRelationChanceFactor: 0f,
                forceAddFreeWarmLayerIfNeeded: false,
                allowFood: true,
                allowAddictions: false);

            Pawn animalPawn = PawnGenerator.GeneratePawn(request);
            animalPawn.Name = pawn.Name;

            TransferNeeds(pawn, animalPawn);
            ApplyResidualWounds(pawn, animalPawn);

            GenSpawn.Spawn(animalPawn, position, map);
            TrainFully(animalPawn, pawn);

            // SPIKE: animals have no drafter by default, so drafting is impossible.
            // Giving the shifted pawn one is half the story - the other half is the
            // drafted branch grafted onto the Animal think tree (see Patches/).
            if (faction == Faction.OfPlayer && animalPawn.drafter == null)
            {
                animalPawn.drafter = new Pawn_DraftController(animalPawn);
            }

            Hediff_WildShapeForm hediff = (Hediff_WildShapeForm)HediffMaker.MakeHediff(DruidkinDefOf.Druidkin_WildShapeForm, animalPawn);
            hediff.originalPawn = pawn;
            hediff.storedGear = storedGear;
            hediff.ticksRemaining = durationTicks;
            animalPawn.health.AddHediff(hediff);

            LogPawnState("shift/before-passtoworld", pawn);
            pawn.DeSpawn(DestroyMode.Vanish);
            Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
            LogPawnState("shift/after-passtoworld", pawn);

            if (faction == Faction.OfPlayer)
            {
                Find.Selector.ClearSelection();
                Find.Selector.Select(animalPawn);
            }

            return true;
        }

        public static void RevertToHuman(Pawn animalPawn)
        {
            Hediff_WildShapeForm hediff = animalPawn.health?.hediffSet?
                .GetFirstHediffOfDef(DruidkinDefOf.Druidkin_WildShapeForm) as Hediff_WildShapeForm;

            if (hediff?.originalPawn == null)
            {
                return;
            }

            Pawn originalPawn = hediff.originalPawn;
            List<Thing> storedGear = hediff.storedGear ?? new List<Thing>();
            Map map = animalPawn.Map;
            IntVec3 position = animalPawn.Position;

            TransferNeeds(animalPawn, originalPawn);

            LogPawnState("revert/before-despawn", originalPawn);

            animalPawn.DeSpawn(DestroyMode.Vanish);

            Find.WorldPawns.RemovePawn(originalPawn);
            LogPawnState("revert/after-removepawn", originalPawn);

            GenSpawn.Spawn(originalPawn, position, map);
            LogPawnState("revert/after-spawn", originalPawn);

            RestoreGear(originalPawn, storedGear);

            Find.WorldPawns.PassToWorld(animalPawn, PawnDiscardDecideMode.Discard);

            if (originalPawn.Faction == Faction.OfPlayer)
            {
                Find.Selector.ClearSelection();
                Find.Selector.Select(originalPawn);
            }
        }

        /// Pulls weapon, worn apparel and carried inventory off the pawn without
        /// dropping them on the ground, so they can be handed back on revert.
        private static List<Thing> StripGear(Pawn pawn)
        {
            List<Thing> stored = new List<Thing>();

            if (pawn.equipment != null)
            {
                foreach (ThingWithComps eq in pawn.equipment.AllEquipmentListForReading.ToList())
                {
                    pawn.equipment.Remove(eq);
                    stored.Add(eq);
                }
            }

            if (pawn.apparel != null)
            {
                foreach (Apparel ap in pawn.apparel.WornApparel.ToList())
                {
                    pawn.apparel.Remove(ap);
                    stored.Add(ap);
                }
            }

            if (pawn.inventory != null)
            {
                foreach (Thing item in pawn.inventory.innerContainer.ToList())
                {
                    pawn.inventory.innerContainer.Remove(item);
                    stored.Add(item);
                }
            }

            return stored;
        }

        private static void RestoreGear(Pawn pawn, List<Thing> storedGear)
        {
            foreach (Thing thing in storedGear)
            {
                if (thing is Apparel apparel)
                {
                    pawn.apparel.Wear(apparel, dropReplacedApparel: false);
                }
                else if (thing is ThingWithComps equipment && equipment.def.IsWeapon)
                {
                    pawn.equipment.AddEquipment(equipment);
                }
                else if (!pawn.inventory.innerContainer.TryAdd(thing))
                {
                    GenPlace.TryPlaceThing(thing, pawn.Position, pawn.Map, ThingPlaceMode.Near);
                }
            }
        }

        private static void TransferNeeds(Pawn from, Pawn to)
        {
            Need fromFood = from.needs?.food;
            Need toFood = to.needs?.food;
            if (fromFood != null && toFood != null)
            {
                toFood.CurLevelPercentage = fromFood.CurLevelPercentage;
            }

            Need fromRest = from.needs?.rest;
            Need toRest = to.needs?.rest;
            if (fromRest != null && toRest != null)
            {
                toRest.CurLevelPercentage = fromRest.CurLevelPercentage;
            }
        }

        /// Instead of trying to map individual injuries between two very different
        /// bodies, carry over overall health loss as a single proxy wound.
        private static void ApplyResidualWounds(Pawn from, Pawn to)
        {
            float missingHealthPct = 1f - from.health.summaryHealth.SummaryHealthPercent;
            if (missingHealthPct <= 0.01f)
            {
                return;
            }

            Hediff wound = HediffMaker.MakeHediff(DruidkinDefOf.Druidkin_ResidualWounds, to);
            wound.Severity = missingHealthPct;
            to.health.AddHediff(wound);
        }

        private static void TrainFully(Pawn animalPawn, Pawn owner)
        {
            if (animalPawn.training == null)
            {
                return;
            }

            foreach (TrainableDef trainable in DefDatabase<TrainableDef>.AllDefsListForReading)
            {
                if (animalPawn.training.CanAssignToTrain(trainable).Accepted)
                {
                    animalPawn.training.Train(trainable, owner, complete: true);
                }
            }
        }
    }
}
