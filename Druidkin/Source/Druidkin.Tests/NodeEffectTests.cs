using System.Collections.Generic;
using Druidkin;
using NUnit.Framework;
using RimWorld;
using Verse;

namespace Druidkin.Tests
{
    [TestFixture]
    public class NodeEffectTests
    {
        [Test]
        public void UnlockFormEffect_MatchesOnlyTargetForm()
        {
            var wolf = new DruidkinAnimalFormDef { defName = "Form_Wolf" };
            var bear = new DruidkinAnimalFormDef { defName = "Form_Bear" };

            var effect = new NodeEffect_UnlockForm { form = wolf };

            Assert.That(effect.UnlocksForm(wolf), Is.True);
            Assert.That(effect.UnlocksForm(bear), Is.False);
            Assert.That(effect.UnlocksForm(null), Is.False);
        }

        [Test]
        public void PerkEffect_DurationAndCooldownModifiers()
        {
            var perk = new NodeEffect_Perk
            {
                shiftDurationFactor = 1.5f,
                cooldownFactor = 0.5f
            };

            float duration = 60000f;
            perk.ModifyShiftDuration(ref duration);
            Assert.That(duration, Is.EqualTo(90000f));

            float cd = 2500f;
            perk.ModifyCooldown(ref cd);
            Assert.That(cd, Is.EqualTo(1250f));
        }

        [Test]
        public void FormUpgrade_AppliesOffsetOnlyWhenFormMatches()
        {
            var wolf = new DruidkinAnimalFormDef { defName = "Form_Wolf" };
            var bear = new DruidkinAnimalFormDef { defName = "Form_Bear" };
            var moveSpeedStat = new StatDef { defName = "MoveSpeed" };

            var upgrade = new NodeEffect_FormUpgrade
            {
                form = wolf,
                stat = moveSpeedStat,
                offset = 0.60f
            };

            var stage = new HediffStage
            {
                statOffsets = new List<StatModifier>(),
                statFactors = new List<StatModifier>()
            };

            // Applying to Bear should do nothing
            upgrade.ApplyToStage(bear, stage);
            Assert.That(stage.statOffsets, Is.Empty);

            // Applying to Wolf should add StatModifier
            upgrade.ApplyToStage(wolf, stage);
            Assert.That(stage.statOffsets.Count, Is.EqualTo(1));
            Assert.That(stage.statOffsets[0].stat, Is.EqualTo(moveSpeedStat));
            Assert.That(stage.statOffsets[0].value, Is.EqualTo(0.60f));
        }

        [Test]
        public void FormUpgrade_AppliesFactorOnlyWhenFormMatches()
        {
            var wolf = new DruidkinAnimalFormDef { defName = "Form_Wolf" };
            var armorStat = new StatDef { defName = "ArmorRating_Sharp" };

            var upgrade = new NodeEffect_FormUpgrade
            {
                form = wolf,
                stat = armorStat,
                factor = 1.25f
            };

            var stage = new HediffStage
            {
                statOffsets = new List<StatModifier>(),
                statFactors = new List<StatModifier>()
            };

            upgrade.ApplyToStage(wolf, stage);
            Assert.That(stage.statFactors.Count, Is.EqualTo(1));
            Assert.That(stage.statFactors[0].stat, Is.EqualTo(armorStat));
            Assert.That(stage.statFactors[0].value, Is.EqualTo(1.25f));
        }

        [Test]
        public void FormUpgrade_WhenFormIsNull_AppliesOffsetToAllForms()
        {
            var wolf = new DruidkinAnimalFormDef { defName = "Form_Wolf" };
            var bear = new DruidkinAnimalFormDef { defName = "Form_Bear" };
            var moveSpeedStat = new StatDef { defName = "MoveSpeed" };

            var upgrade = new NodeEffect_FormUpgrade
            {
                form = null,
                stat = moveSpeedStat,
                offset = 0.50f
            };

            var wolfStage = new HediffStage
            {
                statOffsets = new List<StatModifier>(),
                statFactors = new List<StatModifier>()
            };
            upgrade.ApplyToStage(wolf, wolfStage);
            Assert.That(wolfStage.statOffsets.Count, Is.EqualTo(1));
            Assert.That(wolfStage.statOffsets[0].stat, Is.EqualTo(moveSpeedStat));
            Assert.That(wolfStage.statOffsets[0].value, Is.EqualTo(0.50f));

            var bearStage = new HediffStage
            {
                statOffsets = new List<StatModifier>(),
                statFactors = new List<StatModifier>()
            };
            upgrade.ApplyToStage(bear, bearStage);
            Assert.That(bearStage.statOffsets.Count, Is.EqualTo(1));
            Assert.That(bearStage.statOffsets[0].stat, Is.EqualTo(moveSpeedStat));
            Assert.That(bearStage.statOffsets[0].value, Is.EqualTo(0.50f));
        }

        [Test]
        public void FormUpgrade_WhenFormIsNull_AppliesFactorToAllForms()
        {
            var wolf = new DruidkinAnimalFormDef { defName = "Form_Wolf" };
            var bear = new DruidkinAnimalFormDef { defName = "Form_Bear" };
            var armorStat = new StatDef { defName = "ArmorRating_Sharp" };

            var upgrade = new NodeEffect_FormUpgrade
            {
                form = null,
                stat = armorStat,
                factor = 1.30f
            };

            var wolfStage = new HediffStage
            {
                statOffsets = new List<StatModifier>(),
                statFactors = new List<StatModifier>()
            };
            upgrade.ApplyToStage(wolf, wolfStage);
            Assert.That(wolfStage.statFactors.Count, Is.EqualTo(1));
            Assert.That(wolfStage.statFactors[0].stat, Is.EqualTo(armorStat));
            Assert.That(wolfStage.statFactors[0].value, Is.EqualTo(1.30f));

            var bearStage = new HediffStage
            {
                statOffsets = new List<StatModifier>(),
                statFactors = new List<StatModifier>()
            };
            upgrade.ApplyToStage(bear, bearStage);
            Assert.That(bearStage.statFactors.Count, Is.EqualTo(1));
            Assert.That(bearStage.statFactors[0].stat, Is.EqualTo(armorStat));
            Assert.That(bearStage.statFactors[0].value, Is.EqualTo(1.30f));
        }
    }
}
