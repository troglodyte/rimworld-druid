using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Leatherworks.Tests
{
    [TestFixture]
    public class ContentValidationTests
    {
        private static readonly string[] Pieces = { "Jerkin", "Cap", "Greaves" };

        private string modRoot;
        private List<XElement> thingDefs;
        private List<XElement> recipeDefs;

        [SetUp]
        public void Setup()
        {
            // Climb up from bin/Debug/net8.0/ to Leatherworks/
            DirectoryInfo dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null && dir.Name != "Leatherworks" && dir.Name != "Source")
                dir = dir.Parent;
            if (dir?.Name == "Source")
                dir = dir.Parent;
            Assert.That(dir, Is.Not.Null, "Could not locate Leatherworks mod directory");
            modRoot = dir!.FullName;

            thingDefs = LoadDefs("ThingDefs/Apparel_Leatherworks.xml", "ThingDef");
            recipeDefs = LoadDefs("RecipeDefs/Recipes_Leatherworks.xml", "RecipeDef");
        }

        private List<XElement> LoadDefs(string relPath, string element)
        {
            string path = Path.Combine(modRoot, "Defs", relPath);
            Assert.That(File.Exists(path), Is.True, $"File not found: {path}");
            return XDocument.Load(path).Root!.Elements(element).ToList();
        }

        private XElement Named(IEnumerable<XElement> defs, string defName)
        {
            XElement def = defs.FirstOrDefault(d => d.Element("defName")?.Value == defName);
            Assert.That(def, Is.Not.Null, $"Missing def {defName}");
            return def!;
        }

        [Test]
        public void AllDefNames_UseLwPrefix()
        {
            foreach (var def in thingDefs.Concat(recipeDefs))
            {
                string name = def.Element("defName")?.Value ?? def.Attribute("Name")?.Value;
                Assert.That(name, Does.StartWith("LW_"), $"{name} must use the LW_ prefix");
            }
        }

        [TestCaseSource(nameof(Pieces))]
        public void PlainPiece_HasParent_Multiplier_AndRecipeMaker(string piece)
        {
            XElement def = Named(thingDefs, $"LW_Apparel_Leather{piece}");
            Assert.That(def.Attribute("ParentName")?.Value, Is.EqualTo($"LW_{piece}Base"));
            Assert.That(def.Element("recipeMaker"), Is.Not.Null);
            Assert.That(def.Element("comps"), Is.Null, "Plain pieces have no stud comp");

            XElement parent = thingDefs.First(d => d.Attribute("Name")?.Value == $"LW_{piece}Base");
            float mult = float.Parse(parent.Element("statBases")!.Element("StuffEffectMultiplierArmor")!.Value);
            Assert.That(mult, Is.GreaterThan(0f));
        }

        [TestCaseSource(nameof(Pieces))]
        public void StuddedPiece_HasComp_NoRecipeMaker_AndExactlyOneTwoSlotRecipe(string piece)
        {
            string defName = $"LW_Apparel_StuddedLeather{piece}";
            XElement def = Named(thingDefs, defName);
            Assert.That(def.Attribute("ParentName")?.Value, Is.EqualTo($"LW_{piece}Base"));
            Assert.That(def.Element("recipeMaker"), Is.Null);
            Assert.That(def.Element("comps")?.Elements("li")
                .Any(li => li.Attribute("Class")?.Value == "Leatherworks.CompProperties_StuddedMetal"), Is.True);

            var recipes = recipeDefs.Where(r => r.Element("products")?.Element(defName) != null).ToList();
            Assert.That(recipes.Count, Is.EqualTo(1), $"{defName} needs exactly one recipe");
            var slots = recipes[0].Element("ingredients")!.Elements("li").ToList();
            Assert.That(slots.Count, Is.EqualTo(2));
            Assert.That(slots[0].Element("filter")!.Element("categories")!.Elements("li").Select(e => e.Value),
                Does.Contain("Leathers"));
            Assert.That(slots[1].Element("filter")!.Element("stuffCategoriesToAllow")!.Elements("li").Select(e => e.Value),
                Does.Contain("Metallic"));
        }

        [Test]
        public void StatPartPatch_SitsBetweenStuffAndQuality()
        {
            string path = Path.Combine(modRoot, "Patches", "StatParts_Leatherworks.xml");
            XElement li = XDocument.Load(path).Descendants("li")
                .Single(e => e.Attribute("Class")?.Value == "Leatherworks.StatPart_StuddedMetal");
            float priority = float.Parse(li.Element("priority")!.Value);
            Assert.That(priority, Is.GreaterThan(0f).And.LessThan(100f),
                "Must run after StatPart_Stuff (100) and before StatPart_Quality (0)");
        }
    }
}
