using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;

namespace Leatherworks.Tests
{
    [TestFixture]
    public class StudIngredientsTests
    {
        // ThingDef's constructor loads Unity shaders, so build it uninitialized; FindLeather only reads stuffProps.
        private static ThingDef Stuff(string name, StuffCategoryDef cat)
        {
            var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            def.defName = name;
            def.stuffProps = new StuffProperties { categories = new List<StuffCategoryDef> { cat } };
            return def;
        }

        [Test]
        public void FindLeather_SkipsMetal_ReturnsLeatherThing()
        {
            var leatheryCat = new StuffCategoryDef { defName = "Leathery" };
            var metallicCat = new StuffCategoryDef { defName = "Metallic" };
            var steel = new Thing { def = Stuff("Steel", metallicCat) };
            var bear = new Thing { def = Stuff("Leather_Bear", leatheryCat) };

            Thing found = StudIngredients.FindLeather(new List<Thing> { steel, bear }, leatheryCat);

            Assert.That(found, Is.SameAs(bear));
        }

        [Test]
        public void FindLeather_NoLeather_ReturnsNull()
        {
            var leatheryCat = new StuffCategoryDef { defName = "Leathery" };
            var steel = new Thing { def = Stuff("Steel", new StuffCategoryDef { defName = "Metallic" }) };

            Assert.That(StudIngredients.FindLeather(new List<Thing> { steel }, leatheryCat), Is.Null);
        }
    }
}
