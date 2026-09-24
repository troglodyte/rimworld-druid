using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Druidkin;
using NUnit.Framework;
using Verse;

namespace Druidkin.Tests
{
    [TestFixture]
    public class TalentTreeGraphTests
    {
        private GeneDef geneDef;

        [SetUp]
        public void Setup()
        {
            var ext = new WildShapeProgressionExtension
            {
                maxLevel = 20,
                pointsPerLevel = 1
            };

            geneDef = new GeneDef
            {
                defName = "Druidkin_WildShape",
                modExtensions = new List<DefModExtension> { ext }
            };
        }

        private Gene_Druid CreateDruid(int level)
        {
            var gene = new Gene_Druid { def = geneDef };
            typeof(Gene_Druid).GetField("level", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(gene, level);
            return gene;
        }

        [Test]
        public void ConfigErrors_RejectsNodeWithNoEffects()
        {
            var node = new WildShapeNodeDef { defName = "NoEffectsNode", cost = 1 };
            var errors = node.ConfigErrors().ToList();
            Assert.That(errors.Any(e => e.Contains("has no effects")), Is.True);
        }

        [Test]
        public void ConfigErrors_RejectsNegativeCost()
        {
            var node = new WildShapeNodeDef
            {
                defName = "NegativeCostNode",
                cost = -2,
                effects = new List<NodeEffect> { new NodeEffect_UnlockForm() }
            };
            var errors = node.ConfigErrors().ToList();
            Assert.That(errors.Any(e => e.Contains("negative cost")), Is.True);
        }

        [Test]
        public void CycleDetection_DetectsSelfCycle()
        {
            var node = new WildShapeNodeDef
            {
                defName = "SelfCycle",
                effects = new List<NodeEffect> { new NodeEffect_UnlockForm() }
            };
            node.prerequisites = new List<WildShapeNodeDef> { node };

            var errors = node.ConfigErrors().ToList();
            Assert.That(errors.Any(e => e.Contains("prerequisite cycle")), Is.True);
        }

        [Test]
        public void CycleDetection_DetectsMutualCycle()
        {
            var nodeA = new WildShapeNodeDef
            {
                defName = "NodeA",
                effects = new List<NodeEffect> { new NodeEffect_UnlockForm() }
            };
            var nodeB = new WildShapeNodeDef
            {
                defName = "NodeB",
                effects = new List<NodeEffect> { new NodeEffect_UnlockForm() }
            };
            nodeA.prerequisites = new List<WildShapeNodeDef> { nodeB };
            nodeB.prerequisites = new List<WildShapeNodeDef> { nodeA };

            var errorsA = nodeA.ConfigErrors().ToList();
            var errorsB = nodeB.ConfigErrors().ToList();

            Assert.That(errorsA.Any(e => e.Contains("prerequisite cycle")), Is.True);
            Assert.That(errorsB.Any(e => e.Contains("prerequisite cycle")), Is.True);
        }

        [Test]
        public void CycleDetection_DetectsTransitiveCycle()
        {
            var a = new WildShapeNodeDef { defName = "A", effects = new List<NodeEffect> { new NodeEffect_UnlockForm() } };
            var b = new WildShapeNodeDef { defName = "B", effects = new List<NodeEffect> { new NodeEffect_UnlockForm() } };
            var c = new WildShapeNodeDef { defName = "C", effects = new List<NodeEffect> { new NodeEffect_UnlockForm() } };

            a.prerequisites = new List<WildShapeNodeDef> { b };
            b.prerequisites = new List<WildShapeNodeDef> { c };
            c.prerequisites = new List<WildShapeNodeDef> { a };

            Assert.That(a.ConfigErrors().Any(e => e.Contains("prerequisite cycle")), Is.True);
        }

        [Test]
        public void CycleDetection_ValidDiamondDAG_HasNoCycle()
        {
            // Root -> Left, Root -> Right, (Left, Right) -> Merged
            var root = new WildShapeNodeDef { defName = "Root", effects = new List<NodeEffect> { new NodeEffect_UnlockForm() } };
            var left = new WildShapeNodeDef { defName = "Left", prerequisites = new List<WildShapeNodeDef> { root }, effects = new List<NodeEffect> { new NodeEffect_UnlockForm() } };
            var right = new WildShapeNodeDef { defName = "Right", prerequisites = new List<WildShapeNodeDef> { root }, effects = new List<NodeEffect> { new NodeEffect_UnlockForm() } };
            var merged = new WildShapeNodeDef { defName = "Merged", prerequisites = new List<WildShapeNodeDef> { left, right }, effects = new List<NodeEffect> { new NodeEffect_UnlockForm() } };

            Assert.That(merged.ConfigErrors().Any(e => e.Contains("prerequisite cycle")), Is.False);
        }

        [Test]
        public void CanLearn_EnforcesPrerequisitesAndLevel()
        {
            var druid = CreateDruid(level: 1);

            var formA = new DruidkinAnimalFormDef { defName = "FormA" };
            var nodeA = new WildShapeNodeDef
            {
                defName = "NodeA",
                cost = 0,
                minLevel = 0,
                effects = new List<NodeEffect> { new NodeEffect_UnlockForm { form = formA } }
            };

            var formB = new DruidkinAnimalFormDef { defName = "FormB" };
            var nodeB = new WildShapeNodeDef
            {
                defName = "NodeB",
                cost = 1,
                minLevel = 2,
                prerequisites = new List<WildShapeNodeDef> { nodeA },
                effects = new List<NodeEffect> { new NodeEffect_UnlockForm { form = formB } }
            };

            // Druid is level 1, nodeB needs level 2
            Assert.That(druid.CanLearn(nodeB, out string reason), Is.False);
            Assert.That(reason, Does.Contain("Druidkin_NodeRequiresLevel"));

            // Raise to level 2, but nodeA is not learned yet
            typeof(Gene_Druid).GetField("level", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(druid, 2);
            Assert.That(druid.CanLearn(nodeB, out reason), Is.False);
            Assert.That(reason, Does.Contain("Druidkin_NodeRequiresPrereq"));

            // Learn nodeA
            Assert.That(druid.Learn(nodeA), Is.True);
            Assert.That(druid.Knows(formA), Is.True);
            Assert.That(druid.Knows(formB), Is.False);

            // Now nodeB can be learned
            Assert.That(druid.CanLearn(nodeB, out reason), Is.True);
            Assert.That(druid.Learn(nodeB), Is.True);
            Assert.That(druid.Knows(formB), Is.True);

            // NodeB already learned
            Assert.That(druid.CanLearn(nodeB, out reason), Is.False);
            Assert.That(reason, Does.Contain("Druidkin_NodeAlreadyLearned"));
        }

        [Test]
        public void CanLearn_EnforcesPointsRequirement()
        {
            var druid = CreateDruid(level: 2); // 2 points available

            var expensiveNode = new WildShapeNodeDef
            {
                defName = "Expensive",
                cost = 3,
                minLevel = 0,
                effects = new List<NodeEffect> { new NodeEffect_UnlockForm() }
            };

            Assert.That(druid.CanLearn(expensiveNode, out string reason), Is.False);
            Assert.That(reason, Does.Contain("Druidkin_NodeNotEnoughPoints"));

            // Level up to 3 -> now affordable
            typeof(Gene_Druid).GetField("level", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(druid, 3);
            Assert.That(druid.CanLearn(expensiveNode, out reason), Is.True);
            Assert.That(druid.Learn(expensiveNode), Is.True);
            Assert.That(druid.UnspentPoints, Is.EqualTo(0));
        }

        [Test]
        public void ShiftDurationAndCooldown_AggregatesAllLearnedPerks()
        {
            var druid = CreateDruid(level: 10);

            var perk1 = new WildShapeNodeDef
            {
                defName = "Perk1",
                cost = 2,
                effects = new List<NodeEffect> { new NodeEffect_Perk { shiftDurationFactor = 1.5f } }
            };
            var perk2 = new WildShapeNodeDef
            {
                defName = "Perk2",
                cost = 2,
                effects = new List<NodeEffect> { new NodeEffect_Perk { cooldownFactor = 0.5f } }
            };

            druid.Learn(perk1);
            druid.Learn(perk2);

            int baseDuration = 60000;
            int modifiedDuration = druid.ShiftDurationTicks(baseDuration);
            Assert.That(modifiedDuration, Is.EqualTo(90000));

            int baseCooldown = 2500;
            int modifiedCooldown = druid.CooldownTicks(baseCooldown);
            Assert.That(modifiedCooldown, Is.EqualTo(1250));
        }
    }
}
