using System.Collections.Generic;
using System.Reflection;
using Druidkin;
using NUnit.Framework;
using Verse;

namespace Druidkin.Tests
{
    [TestFixture]
    public class ProgressionTests
    {
        private WildShapeProgressionExtension progressionExt;
        private GeneDef geneDef;

        [SetUp]
        public void Setup()
        {
            progressionExt = new WildShapeProgressionExtension
            {
                maxLevel = 20,
                pointsPerLevel = 1,
                xpToNextLevel = new SimpleCurve
                {
                    new CurvePoint(0, 1000f),
                    new CurvePoint(19, 8000f)
                },
                masteryFactorByLevel = new SimpleCurve
                {
                    new CurvePoint(0, 0.80f),
                    new CurvePoint(10, 1.00f),
                    new CurvePoint(20, 1.30f)
                },
                xpPerDayShifted = 600f,
                xpPerDamageDealt = 4f,
                xpPerDamageTaken = 2f,
                animalsXpFraction = 0.5f
            };

            geneDef = new GeneDef
            {
                defName = "Druidkin_WildShape",
                modExtensions = new List<DefModExtension> { progressionExt }
            };
        }

        private Gene_Druid CreateDruid(int level = 0, float xp = 0f)
        {
            var gene = new Gene_Druid { def = geneDef };
            typeof(Gene_Druid).GetField("level", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(gene, level);
            typeof(Gene_Druid).GetField("xp", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(gene, xp);
            return gene;
        }

        [Test]
        public void ExtensionDefaults_MatchDesignSpec()
        {
            var ext = new WildShapeProgressionExtension();
            Assert.That(ext.maxLevel, Is.EqualTo(20));
            Assert.That(ext.pointsPerLevel, Is.EqualTo(1));
            Assert.That(ext.xpPerDayShifted, Is.EqualTo(600f));
            Assert.That(ext.xpPerDamageDealt, Is.EqualTo(4f));
            Assert.That(ext.xpPerDamageTaken, Is.EqualTo(2f));
            Assert.That(ext.animalsXpFraction, Is.EqualTo(0.5f));
            Assert.That(ext.armorCarryoverFraction, Is.EqualTo(0.75f));
            Assert.That(ext.weaponDamageCarryoverFraction, Is.EqualTo(0.5f));
            Assert.That(ext.referenceMeleeDps, Is.EqualTo(20f));
        }

        [Test]
        public void MasteryFactor_AnchoredAndInterpolatedCorrectly()
        {
            var gene = CreateDruid(level: 0);
            Assert.That(gene.MasteryFactor, Is.EqualTo(0.80f));

            typeof(Gene_Druid).GetField("level", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(gene, 5);
            Assert.That(gene.MasteryFactor, Is.EqualTo(0.90f).Within(0.001f));

            typeof(Gene_Druid).GetField("level", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(gene, 10);
            Assert.That(gene.MasteryFactor, Is.EqualTo(1.00f));

            typeof(Gene_Druid).GetField("level", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(gene, 15);
            Assert.That(gene.MasteryFactor, Is.EqualTo(1.15f).Within(0.001f));

            typeof(Gene_Druid).GetField("level", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(gene, 20);
            Assert.That(gene.MasteryFactor, Is.EqualTo(1.30f));
        }

        [Test]
        public void XpThreshold_EvaluatesCorrectlyAcrossLevels()
        {
            var gene = CreateDruid(level: 0);
            Assert.That(gene.XpToNextLevel, Is.EqualTo(1000f));

            typeof(Gene_Druid).GetField("level", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(gene, 19);
            Assert.That(gene.XpToNextLevel, Is.EqualTo(8000f));
        }

        [Test]
        public void GainXp_BelowThreshold_AccruesWithoutLeveling()
        {
            var gene = CreateDruid(level: 0, xp: 0f);
            gene.GainXp(400f);

            Assert.That(gene.Level, Is.EqualTo(0));
            Assert.That(gene.Xp, Is.EqualTo(400f));
            Assert.That(gene.UnspentPoints, Is.EqualTo(0));
        }

        [Test]
        public void GainXp_MeetsThreshold_LevelsUpAndRollsOverXp()
        {
            var gene = CreateDruid(level: 0, xp: 800f);
            // Threshold for level 0 is 1000. Adding 300 should reach 1100 -> level 1, 100 leftover
            gene.GainXp(300f);

            Assert.That(gene.Level, Is.EqualTo(1));
            Assert.That(gene.Xp, Is.EqualTo(100f));
            Assert.That(gene.UnspentPoints, Is.EqualTo(1));
        }

        [Test]
        public void GainXp_MultipleLevelsGainedInSingleCall()
        {
            var gene = CreateDruid(level: 0, xp: 0f);
            // Threshold level 0: 1000
            // Threshold level 1: 1000 + (7000 / 19) = ~1368.42
            // Giving 3000 XP covers both levels 0 and 1
            gene.GainXp(3000f);

            Assert.That(gene.Level, Is.GreaterThanOrEqualTo(2));
            Assert.That(gene.UnspentPoints, Is.EqualTo(gene.Level));
        }

        [Test]
        public void GainXp_ReachingMaxLevel_CapsAtMaxLevelAndZeroesXp()
        {
            var gene = CreateDruid(level: 19, xp: 7500f);
            gene.GainXp(1000f); // exceeds 8000 threshold

            Assert.That(gene.Level, Is.EqualTo(20));
            Assert.That(gene.Xp, Is.EqualTo(0f));

            // Further XP gain at cap does nothing
            gene.GainXp(5000f);
            Assert.That(gene.Level, Is.EqualTo(20));
            Assert.That(gene.Xp, Is.EqualTo(0f));
        }

        [Test]
        public void UnspentPoints_StrictlyDerivedFromLevelAndPurchases()
        {
            var gene = CreateDruid(level: 5);
            Assert.That(gene.UnspentPoints, Is.EqualTo(5));

            var node1 = new WildShapeNodeDef { defName = "TestNode1", cost = 2 };
            var node2 = new WildShapeNodeDef { defName = "TestNode2", cost = 1 };

            gene.Learn(node1);
            Assert.That(gene.UnspentPoints, Is.EqualTo(3));

            gene.Learn(node2);
            Assert.That(gene.UnspentPoints, Is.EqualTo(2));
        }
    }
}
