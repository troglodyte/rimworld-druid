namespace Leatherworks.Tests
{
    [TestFixture]
    public class StudMathTests
    {
        [TestCase(0.9f, 0.27f)]   // steel sharp
        [TestCase(1.14f, 0.342f)] // plasteel sharp
        [TestCase(0f, 0f)]
        public void Bonus_IsStuffPowerTimesFactor(float power, float expected)
        {
            Assert.That(StudMath.Bonus(power, StudMath.StudFactor), Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void Label_AppendsMetalInParens()
        {
            Assert.That(StudLabel.Format("bear leather studded jerkin", "plasteel"),
                Is.EqualTo("bear leather studded jerkin (plasteel)"));
        }

        [Test]
        public void Label_NoMetal_Unchanged()
        {
            Assert.That(StudLabel.Format("studded jerkin", null), Is.EqualTo("studded jerkin"));
        }
    }
}
