using Verse;
using Verse.AI;

namespace Druidkin
{
    /// Gate for the drafted branch we graft onto the shared vanilla "Animal" think tree.
    ///
    /// The branch sits in a tree every animal in the game walks, so this has to be
    /// false for all of them except a shifted druid the player has actually drafted.
    /// Ordinary animals fall straight through to their normal behaviour.
    public class ThinkNode_ConditionalWildShapedDrafted : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn)
        {
            return pawn.drafter != null
                   && pawn.drafter.Drafted
                   && WildShapeUtility.IsShifted(pawn);
        }
    }
}
