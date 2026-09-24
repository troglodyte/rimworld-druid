using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Druidkin
{
    /// One purchasable node in the wild shape talent tree.
    public class WildShapeNodeDef : Def
    {
        public int cost = 1;
        public int minLevel;
        public List<WildShapeNodeDef> prerequisites;
        public Vector2 treePosition;
        public List<NodeEffect> effects;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (effects.NullOrEmpty())
            {
                yield return $"WildShapeNodeDef {defName} has no effects";
            }

            if (cost < 0)
            {
                yield return $"WildShapeNodeDef {defName} has negative cost: {cost}";
            }

            if (HasPrerequisiteCycle())
            {
                yield return $"WildShapeNodeDef {defName} has a prerequisite cycle";
            }
        }

        private bool HasPrerequisiteCycle()
        {
            HashSet<WildShapeNodeDef> visited = new HashSet<WildShapeNodeDef>();
            return DetectCycle(this, visited);
        }

        private static bool DetectCycle(WildShapeNodeDef current, HashSet<WildShapeNodeDef> visited)
        {
            if (current == null)
            {
                return false;
            }

            if (!visited.Add(current))
            {
                return true;
            }

            if (current.prerequisites != null)
            {
                foreach (WildShapeNodeDef prereq in current.prerequisites)
                {
                    if (DetectCycle(prereq, visited))
                    {
                        return true;
                    }
                }
            }

            visited.Remove(current);
            return false;
        }
    }
}
