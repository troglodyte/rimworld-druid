using System.Linq;
using Druidkin.HarmonyPatches;
using NUnit.Framework;

namespace Druidkin.Tests
{
    [TestFixture]
    public class ShiftedNudityTests
    {
        [Test]
        public void SelfPatch_FindsEveryNudityWorker()
        {
            var targets = Patch_NudityThought_Self.TargetMethods().ToList();

            Assert.That(targets, Has.None.Null);
            Assert.That(targets.Count, Is.EqualTo(9));
            Assert.That(targets.All(m => m.GetParameters().Length == 1));
        }

        [Test]
        public void SocialPatch_FindsEveryNudityWorker()
        {
            var targets = Patch_NudityThought_Social.TargetMethods().ToList();

            Assert.That(targets.Count, Is.EqualTo(8));
            Assert.That(targets.All(m => m.GetParameters().Length == 2));
        }
    }
}
