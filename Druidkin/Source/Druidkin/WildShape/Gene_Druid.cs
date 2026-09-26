using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Druidkin
{
    /// Holds and scribes level, XP and learned talent nodes for a druid.
    /// This is the single source of truth and entry point for wild shape progression.
    public class Gene_Druid : Gene
    {
        private int level = 0;
        private float xp = 0f;
        private List<WildShapeNodeDef> learned = new List<WildShapeNodeDef>();

        public WildShapeProgressionExtension Progression =>
            def?.GetModExtension<WildShapeProgressionExtension>();

        public int Level => level;

        public float Xp => xp;

        public int MaxLevel => Progression?.maxLevel ?? 20;

        public float XpToNextLevel =>
            Progression?.xpToNextLevel != null
                ? Progression.xpToNextLevel.Evaluate(level)
                : 1000f;

        public float MasteryFactor =>
            Progression?.masteryFactorByLevel != null
                ? Progression.masteryFactorByLevel.Evaluate(level)
                : 1f;

        /// Unspent points are strictly derived, never stored, preventing drift.
        public int UnspentPoints
        {
            get
            {
                int pointsPerLevel = Progression?.pointsPerLevel ?? 1;
                int totalEarned = pointsPerLevel * level;
                int totalSpent = 0;
                if (learned != null)
                {
                    for (int i = 0; i < learned.Count; i++)
                    {
                        totalSpent += learned[i]?.cost ?? 0;
                    }
                }
                return totalEarned - totalSpent;
            }
        }

        public IReadOnlyList<WildShapeNodeDef> LearnedNodes =>
            learned ?? (IReadOnlyList<WildShapeNodeDef>)System.Array.Empty<WildShapeNodeDef>();

        public override void PostAdd()
        {
            base.PostAdd();
            if (learned == null)
            {
                learned = new List<WildShapeNodeDef>();
            }
            AutoLearnFreeNodes();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref level, "level", 0);
            Scribe_Values.Look(ref xp, "xp", 0f);
            Scribe_Collections.Look(ref learned, "learned", LookMode.Def);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (learned == null)
                {
                    learned = new List<WildShapeNodeDef>();
                    AutoLearnFreeNodes();
                }
                else
                {
                    // Clean up any missing def references
                    learned.RemoveAll(n => n == null);
                }
            }
        }

        /// Nodes with cost 0 and minLevel 0 (such as the Rat form) are learned automatically
        /// when a druid is born or loaded without progression state.
        public void AutoLearnFreeNodes()
        {
            if (learned == null)
            {
                learned = new List<WildShapeNodeDef>();
            }

            foreach (WildShapeNodeDef node in DefDatabase<WildShapeNodeDef>.AllDefsListForReading)
            {
                if (node.cost == 0 && node.minLevel == 0 && !learned.Contains(node))
                {
                    learned.Add(node);
                }
            }
        }

        public void GainXp(float amount)
        {
            if (amount <= 0f || level >= MaxLevel)
            {
                return;
            }

            xp += amount;
            while (level < MaxLevel)
            {
                float needed = XpToNextLevel;
                if (xp < needed)
                {
                    break;
                }

                xp -= needed;
                level++;

                if (Current.ProgramState == ProgramState.Playing && pawn != null)
                {
                    int pointsGained = Progression?.pointsPerLevel ?? 1;
                    TaggedString message = pointsGained == 1
                        ? "Druidkin_LevelUpSingle".Translate(pawn.Named("PAWN"), level)
                        : "Druidkin_LevelUpPlural".Translate(pawn.Named("PAWN"), level, pointsGained);

                    Messages.Message(message, pawn, MessageTypeDefOf.PositiveEvent);
                }
            }

            if (level >= MaxLevel)
            {
                xp = 0f;
            }
        }

        public bool Knows(DruidkinAnimalFormDef form)
        {
            if (form == null || learned == null)
            {
                return false;
            }

            for (int i = 0; i < learned.Count; i++)
            {
                WildShapeNodeDef node = learned[i];
                if (node?.effects == null)
                {
                    continue;
                }

                for (int j = 0; j < node.effects.Count; j++)
                {
                    if (node.effects[j].UnlocksForm(form))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static string SafeTranslate(string key, NamedArgument arg)
        {
            if (LanguageDatabase.activeLanguage == null)
            {
                return $"{key}({arg.arg})";
            }

            return key.Translate(arg).Resolve();
        }

        private static string SafeTranslate(string key)
        {
            if (LanguageDatabase.activeLanguage == null)
            {
                return key;
            }

            return key.Translate().Resolve();
        }

        public bool CanLearn(WildShapeNodeDef node, out string reason)
        {
            reason = null;
            if (node == null)
            {
                reason = SafeTranslate("Druidkin_NodeInvalid");
                return false;
            }

            if (learned != null && learned.Contains(node))
            {
                reason = SafeTranslate("Druidkin_NodeAlreadyLearned");
                return false;
            }

            if (level < node.minLevel)
            {
                reason = SafeTranslate("Druidkin_NodeRequiresLevel", node.minLevel);
                return false;
            }

            if (!node.prerequisites.NullOrEmpty())
            {
                for (int i = 0; i < node.prerequisites.Count; i++)
                {
                    WildShapeNodeDef prereq = node.prerequisites[i];
                    if (prereq != null && (learned == null || !learned.Contains(prereq)))
                    {
                        reason = SafeTranslate("Druidkin_NodeRequiresPrereq", prereq.LabelCap);
                        return false;
                    }
                }
            }

            if (UnspentPoints < node.cost)
            {
                reason = SafeTranslate("Druidkin_NodeNotEnoughPoints", node.cost);
                return false;
            }

            return true;
        }

        public bool Learn(WildShapeNodeDef node)
        {
            if (!CanLearn(node, out string _))
            {
                return false;
            }

            if (learned == null)
            {
                learned = new List<WildShapeNodeDef>();
            }

            learned.Add(node);
            return true;
        }

        public int ShiftDurationTicks(int baseTicks)
        {
            float ticks = baseTicks;
            if (learned != null)
            {
                for (int i = 0; i < learned.Count; i++)
                {
                    WildShapeNodeDef node = learned[i];
                    if (node?.effects == null)
                    {
                        continue;
                    }

                    for (int j = 0; j < node.effects.Count; j++)
                    {
                        node.effects[j].ModifyShiftDuration(ref ticks);
                    }
                }
            }
            return Mathf.RoundToInt(ticks);
        }

        public int CooldownTicks(int baseTicks)
        {
            float ticks = baseTicks;
            if (learned != null)
            {
                for (int i = 0; i < learned.Count; i++)
                {
                    WildShapeNodeDef node = learned[i];
                    if (node?.effects == null)
                    {
                        continue;
                    }

                    for (int j = 0; j < node.effects.Count; j++)
                    {
                        node.effects[j].ModifyCooldown(ref ticks);
                    }
                }
            }
            return Mathf.RoundToInt(ticks);
        }

        public void NotifyReverted()
        {
            if (learned == null)
            {
                return;
            }

            for (int i = 0; i < learned.Count; i++)
            {
                WildShapeNodeDef node = learned[i];
                if (node?.effects == null)
                {
                    continue;
                }

                for (int j = 0; j < node.effects.Count; j++)
                {
                    node.effects[j].OnRevert(pawn);
                }
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            // Vanilla Gene.GetGizmos returns null, not an empty sequence.
            IEnumerable<Gizmo> baseGizmos = base.GetGizmos();
            if (baseGizmos != null)
            {
                foreach (Gizmo g in baseGizmos)
                {
                    yield return g;
                }
            }

            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: +1000 Wild Shape XP",
                    action = () => GainXp(1000f)
                };

                yield return new Command_Action
                {
                    defaultLabel = "DEV: +1 Wild Shape Level",
                    action = () =>
                    {
                        if (level < MaxLevel)
                        {
                            float needed = XpToNextLevel - xp;
                            GainXp(Mathf.Max(1f, needed));
                        }
                    }
                };

                yield return new Command_Action
                {
                    defaultLabel = "DEV: Reset Druid",
                    action = () =>
                    {
                        level = 0;
                        xp = 0f;
                        learned?.Clear();
                        AutoLearnFreeNodes();
                    }
                };
            }
        }
    }
}
