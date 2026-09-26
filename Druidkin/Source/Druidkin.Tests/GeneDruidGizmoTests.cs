using System.Linq;
using NUnit.Framework;

namespace Druidkin.Tests
{
    [TestFixture]
    public class GeneDruidGizmoTests
    {
        // Vanilla Gene.GetGizmos returns null rather than an empty sequence. Enumerating
        // it unguarded threw inside the pawn's gizmo list and hid every button.
        [Test]
        public void GetGizmos_ToleratesNullFromBaseGene()
        {
            var gene = new Gene_Druid();

            Assert.DoesNotThrow(() => gene.GetGizmos().ToList());
        }
    }
}
