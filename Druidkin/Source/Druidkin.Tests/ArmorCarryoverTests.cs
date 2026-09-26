using System.Collections.Generic;
using NUnit.Framework;

namespace Druidkin.Tests
{
    [TestFixture]
    public class ArmorCarryoverTests
    {
        [Test]
        public void OverallArmor_NoApparelIsZero()
        {
            var parts = new List<(float, IList<float>)>
            {
                (0.5f, new List<float>()),
                (0.5f, new List<float>())
            };

            Assert.That(ArmorCarryover.OverallArmor(parts), Is.EqualTo(0f));
        }

        [Test]
        public void OverallArmor_FullCoverageMatchesSingleLayer()
        {
            var parts = new List<(float, IList<float>)>
            {
                (0.6f, new List<float> { 1f }),
                (0.4f, new List<float> { 1f })
            };

            Assert.That(ArmorCarryover.OverallArmor(parts), Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void OverallArmor_WeightsByCoverageAndStacksLayers()
        {
            // Part A: two layers at 1.0 each block 1 - 0.5 * 0.5 = 0.75.
            // Part B: uncovered. Overall = 2 * (0.5 * 0.75) = 0.75.
            var parts = new List<(float, IList<float>)>
            {
                (0.5f, new List<float> { 1f, 1f }),
                (0.5f, new List<float>())
            };

            Assert.That(ArmorCarryover.OverallArmor(parts), Is.EqualTo(0.75f).Within(0.0001f));
        }

        [Test]
        public void OverallArmor_CapsAtTwo()
        {
            var parts = new List<(float, IList<float>)>
            {
                (1f, new List<float> { 5f })
            };

            Assert.That(ArmorCarryover.OverallArmor(parts), Is.EqualTo(2f).Within(0.0001f));
        }
    }
}
