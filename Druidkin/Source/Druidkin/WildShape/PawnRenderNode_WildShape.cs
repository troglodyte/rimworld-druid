using Verse;

namespace Druidkin
{
    /// Draws the animal a druid is currently wearing.
    ///
    /// This has to be a node class rather than a plain texPath in XML for the same
    /// reason the hediff overrides CurStage: the form is chosen at runtime, and XML
    /// cannot name an animal that has not been picked yet.
    ///
    /// The node only draws the animal. Hiding the human underneath is a separate
    /// concern and lives in Patch_PawnRenderTree_AdjustParms, because suppression
    /// happens at draw time against the whole tree rather than per node.
    public class PawnRenderNode_WildShape : PawnRenderNode
    {
        public PawnRenderNode_WildShape(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree)
        {
        }

        public override Graphic GraphicFor(Pawn pawn)
        {
            // Null while unshifted is not an error path so much as a harmless one: the
            // node is created from the hediff and so should not outlive it, but a null
            // graphic simply draws nothing rather than throwing every frame.
            return WildShapeUtility.GetShiftHediff(pawn)?.form?.BodyGraphicData?.Graphic;
        }
    }
}
