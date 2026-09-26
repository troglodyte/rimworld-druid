using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Druidkin
{
    /// Upgrades druids from saves made before the talent tree. Those saves hold the wild
    /// shape gene as a plain Gene: RimWorld only writes a Class attribute for subclasses,
    /// so on load the gene comes back without level, XP or learned nodes. Swapping it for
    /// a fresh gene waits until FinalizeInit, when loading and cross-references are done.
    public class GameComponent_DruidGeneMigration : GameComponent
    {
        public GameComponent_DruidGeneMigration(Game game)
        {
        }

        public override void FinalizeInit()
        {
            foreach (Pawn pawn in PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead.ToList())
            {
                if (pawn.genes == null)
                {
                    continue;
                }

                List<Gene> stale = pawn.genes.GenesListForReading
                    .Where(g => g.def.geneClass == typeof(Gene_Druid) && !(g is Gene_Druid))
                    .ToList();

                foreach (Gene gene in stale)
                {
                    bool xenogene = pawn.genes.IsXenogene(gene);
                    pawn.genes.RemoveGene(gene);
                    pawn.genes.AddGene(gene.def, xenogene);
                    Log.Message($"[Druidkin] Upgraded {pawn.LabelShort}'s {gene.def.defName} gene to the talent tree.");
                }
            }
        }
    }
}
