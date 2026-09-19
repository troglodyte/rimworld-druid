using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Druidkin
{
    /// The whole of a druid's transformed state. The colonist is never substituted for
    /// another pawn, so this hediff is the only thing that knows a shift is happening:
    /// removing it is sufficient to end the transformation, by any route.
    public class Hediff_WildShapeForm : HediffWithComps
    {
        public DruidkinAnimalFormDef form;
        public List<Thing> storedGear = new List<Thing>();
        public int ticksRemaining;

        /// Per-form numbers cannot come from XML, because they are derived from whichever
        /// animal was chosen. Built once on demand and dropped whenever the form changes,
        /// since CurStage is consulted constantly during stat calculation.
        private HediffStage cachedStage;

        public override HediffStage CurStage => cachedStage ?? (cachedStage = BuildStage());

        public override string LabelInBrackets => form?.LabelCap ?? base.LabelInBrackets;

        public override bool ShouldRemove => false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref form, "form");
            Scribe_Collections.Look(ref storedGear, "storedGear", LookMode.Deep);
            Scribe_Values.Look(ref ticksRemaining, "ticksRemaining", 0);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (storedGear == null)
                {
                    storedGear = new List<Thing>();
                }

                // Both are derived from defs alone, so rebuilding after load reproduces
                // them exactly rather than needing to be saved.
                RefreshDerivedState();
            }
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            RefreshDerivedState();
        }

        /// The comps are built before the form is known, so the form has to tell them
        /// once it is set - on shift, and again after a save is loaded.
        private void RefreshDerivedState()
        {
            cachedStage = null;
            this.TryGetComp<HediffComp_WildShapeVerbs>()?.RefreshTools();
        }

        public override void Tick()
        {
            base.Tick();

            if (ticksRemaining <= 0)
            {
                return;
            }

            ticksRemaining--;
            if (ticksRemaining <= 0)
            {
                // Removal is the single exit: PostRemoved hands the gear back whether the
                // shift ended on the timer or on the gizmo.
                pawn.health.RemoveHediff(this);
            }
        }

        public override void PostRemoved()
        {
            base.PostRemoved();
            ReturnGear();
        }

        /// A corpse keeps its hediffs, so without this the druid's gear would be sealed
        /// inside a hediff that never gets removed.
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit)
        {
            base.Notify_PawnDied(dinfo, culprit);
            ReturnGear();
        }

        public void TakeGear(List<Thing> gear)
        {
            storedGear = gear ?? new List<Thing>();
        }

        private void ReturnGear()
        {
            if (storedGear.NullOrEmpty())
            {
                return;
            }

            List<Thing> gear = storedGear;
            storedGear = new List<Thing>();

            bool canWear = pawn != null && !pawn.Dead && pawn.apparel != null && pawn.equipment != null;

            foreach (Thing thing in gear)
            {
                if (thing == null || thing.Destroyed)
                {
                    continue;
                }

                if (canWear && thing is Apparel apparel)
                {
                    pawn.apparel.Wear(apparel, dropReplacedApparel: false);
                }
                else if (canWear && thing is ThingWithComps equipment && thing.def.IsWeapon)
                {
                    pawn.equipment.AddEquipment(equipment);
                }
                else if (pawn?.inventory == null || !pawn.inventory.innerContainer.TryAdd(thing))
                {
                    DropOrDiscard(thing);
                }
            }
        }

        private void DropOrDiscard(Thing thing)
        {
            Map map = pawn?.MapHeld;
            if (map == null || !GenPlace.TryPlaceThing(thing, pawn.PositionHeld, map, ThingPlaceMode.Near))
            {
                thing.Destroy();
            }
        }

        /// Everything a form shares with every other form stays in XML; everything that
        /// depends on which animal was chosen is computed here and layered on top.
        private HediffStage BuildStage()
        {
            HediffStage fromXml = def.stages.NullOrEmpty() ? new HediffStage() : def.stages[0];

            HediffStage stage = new HediffStage
            {
                becomeVisible = fromXml.becomeVisible,
                capMods = fromXml.capMods,
                disabledWorkTags = fromXml.disabledWorkTags,
                statOffsets = new List<StatModifier>(),
                statFactors = new List<StatModifier>()
            };

            ThingDef race = form?.Race;
            if (race?.race == null || pawn?.def?.race == null)
            {
                return stage;
            }

            float boost = Mathf.Max(0.01f, form.statBoostFactor);

            // Read the animal's and the colonist's own species values off their ThingDefs
            // rather than off the pawn. Asking the pawn for a stat here would re-enter
            // stat calculation, which is what consults CurStage in the first place.
            float ownSpeed = pawn.def.GetStatValueAbstract(StatDefOf.MoveSpeed);
            float animalSpeed = race.GetStatValueAbstract(StatDefOf.MoveSpeed);
            if (ownSpeed > 0f && animalSpeed > 0f)
            {
                stage.statFactors.Add(new StatModifier
                {
                    stat = StatDefOf.MoveSpeed,
                    value = animalSpeed * boost / ownSpeed
                });
            }

            // Body part hit points are fixed when a pawn is generated, from the race's
            // health scale, so no hediff can raise them afterwards. Scaling incoming
            // damage by the same ratio is the reachable equivalent of a tougher body.
            float ownHealth = pawn.def.race.baseHealthScale;
            float animalHealth = race.race.baseHealthScale;
            if (ownHealth > 0f && animalHealth > 0f)
            {
                stage.statFactors.Add(new StatModifier
                {
                    stat = StatDefOf.IncomingDamageFactor,
                    value = ownHealth / (animalHealth * boost)
                });
            }

            return stage;
        }
    }
}
