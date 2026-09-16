using System.Collections.Generic;
using Verse;

namespace Druidkin
{
    /// Lives on the animal-formed pawn. Tracks the suspended human pawn underneath,
    /// their stashed gear, and how much longer the shift lasts.
    public class Hediff_WildShapeForm : HediffWithComps
    {
        public Pawn originalPawn;
        public List<Thing> storedGear = new List<Thing>();
        public int ticksRemaining;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref originalPawn, "originalPawn");
            Scribe_Collections.Look(ref storedGear, "storedGear", LookMode.Deep);
            Scribe_Values.Look(ref ticksRemaining, "ticksRemaining", 0);

            if (Scribe.mode == LoadSaveMode.LoadingVars && storedGear == null)
            {
                storedGear = new List<Thing>();
            }
        }

        public override void Tick()
        {
            base.Tick();

            if (ticksRemaining <= 0)
            {
                return;
            }

            ticksRemaining--;
            if (ticksRemaining <= 0 && pawn.Spawned)
            {
                WildShapeUtility.RevertToHuman(pawn);
            }
        }
    }
}
