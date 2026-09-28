using NUnit.Framework;

namespace Druidkin.Tests
{
    [TestFixture]
    public class WeaponCarryoverTests
    {
        [Test]
        public void DamageFactorOffset_ZeroOrNegativeDps_ReturnsZero()
        {
            Assert.That(WeaponCarryover.DamageFactorOffset(0f, 0.5f), Is.EqualTo(0f));
            Assert.That(WeaponCarryover.DamageFactorOffset(-10f, 0.5f), Is.EqualTo(0f));
        }

        [Test]
        public void DamageFactorOffset_ZeroOrNegativeFraction_ReturnsZero()
        {
            Assert.That(WeaponCarryover.DamageFactorOffset(15f, 0f), Is.EqualTo(0f));
            Assert.That(WeaponCarryover.DamageFactorOffset(15f, -0.5f), Is.EqualTo(0f));
        }

        [Test]
        public void DamageFactorOffset_StandardBenchmark_CalculatesExpectedOffsets()
        {
            // At 20 DPS benchmark and 0.50 fraction:
            // 20 DPS weapon -> (20 / 20) * 0.50 = +0.50 (50% bonus)
            Assert.That(WeaponCarryover.DamageFactorOffset(20f, 0.5f), Is.EqualTo(0.5f).Within(0.0001f));

            // 10 DPS weapon (e.g. mace) -> (10 / 20) * 0.50 = +0.25 (25% bonus)
            Assert.That(WeaponCarryover.DamageFactorOffset(10f, 0.5f), Is.EqualTo(0.25f).Within(0.0001f));

            // 12.5 DPS weapon (e.g. steel longsword) -> (12.5 / 20) * 0.50 = +0.3125 (31.25% bonus)
            Assert.That(WeaponCarryover.DamageFactorOffset(12.5f, 0.5f), Is.EqualTo(0.3125f).Within(0.0001f));

            // 6 DPS weapon (e.g. club) -> (6 / 20) * 0.50 = +0.15 (15% bonus)
            Assert.That(WeaponCarryover.DamageFactorOffset(6f, 0.5f), Is.EqualTo(0.15f).Within(0.0001f));
        }

        [Test]
        public void DamageFactorOffset_CustomBenchmark_ScalesCorrectly()
        {
            // Custom benchmark of 10 DPS:
            // 10 DPS at 0.50 fraction -> (10 / 10) * 0.50 = +0.50
            Assert.That(WeaponCarryover.DamageFactorOffset(10f, 0.5f, referenceDps: 10f), Is.EqualTo(0.5f).Within(0.0001f));

            // Safe fallback against non-positive benchmark (no division by zero)
            Assert.That(WeaponCarryover.DamageFactorOffset(10f, 0.5f, referenceDps: 0f), Is.GreaterThan(0f));
        }

        [Test]
        public void Hediff_WildShapeForm_TakeWeaponBonus_SetsAndClamps()
        {
            var hediff = new Hediff_WildShapeForm();
            Assert.That(hediff.CarriedMeleeDamageFactor, Is.EqualTo(0f));

            hediff.TakeWeaponBonus(0.35f);
            Assert.That(hediff.CarriedMeleeDamageFactor, Is.EqualTo(0.35f).Within(0.0001f));

            hediff.TakeWeaponBonus(-0.1f);
            Assert.That(hediff.CarriedMeleeDamageFactor, Is.EqualTo(0f));
        }
    }
}
