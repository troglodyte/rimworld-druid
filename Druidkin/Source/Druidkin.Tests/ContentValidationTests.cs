using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;

namespace Druidkin.Tests
{
    [TestFixture]
    public class ContentValidationTests
    {
        private string modRoot;

        [SetUp]
        public void Setup()
        {
            // Resolve path to the Druidkin mod directory
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            // Climb up from bin/Debug/net8.0/ to Druidkin/
            DirectoryInfo dir = new DirectoryInfo(baseDir);
            while (dir != null && dir.Name != "Druidkin" && dir.Name != "Source")
            {
                dir = dir.Parent;
            }

            if (dir?.Name == "Source")
            {
                dir = dir.Parent;
            }

            Assert.That(dir, Is.Not.Null, "Could not locate Druidkin mod directory");
            modRoot = dir!.FullName;
        }

        [Test]
        public void StarterContent_HasExactly15Nodes_WithCorrectTotalCost()
        {
            string nodesPath = Path.Combine(modRoot, "Defs", "WildShapeNodeDefs", "WildShapeNodes_Druidkin.xml");
            Assert.That(File.Exists(nodesPath), Is.True, $"File not found: {nodesPath}");

            XDocument doc = XDocument.Load(nodesPath);
            var nodes = doc.Root!.Elements("Druidkin.WildShapeNodeDef").ToList();

            Assert.That(nodes.Count, Is.EqualTo(15), "Must define exactly 15 talent nodes in starter content.");

            // All defNames must start with Druidkin_Node_
            foreach (var node in nodes)
            {
                string defName = node.Element("defName")?.Value;
                Assert.That(defName, Does.StartWith("Druidkin_Node_"), $"Node {defName} must start with 'Druidkin_Node_' prefix.");
            }

            // Total tree cost must equal 24
            int totalCost = nodes.Sum(n => int.Parse(n.Element("cost")?.Value ?? "1"));
            Assert.That(totalCost, Is.EqualTo(24), "Total tree cost must be exactly 24 points.");
        }

        [Test]
        public void StarterContent_FormsAndPrerequisites_MatchDesignSpec()
        {
            string nodesPath = Path.Combine(modRoot, "Defs", "WildShapeNodeDefs", "WildShapeNodes_Druidkin.xml");
            XDocument doc = XDocument.Load(nodesPath);
            var nodes = doc.Root!.Elements("Druidkin.WildShapeNodeDef").ToDictionary(n => n.Element("defName")!.Value);

            // Rat: cost 0, minLevel 0, auto-learned
            Assert.That(int.Parse(nodes["Druidkin_Node_Rat"].Element("cost")!.Value), Is.EqualTo(0));
            Assert.That(int.Parse(nodes["Druidkin_Node_Rat"].Element("minLevel")!.Value), Is.EqualTo(0));

            // Timber wolf: cost 1, level 2, requires Rat
            Assert.That(int.Parse(nodes["Druidkin_Node_TimberWolf"].Element("cost")!.Value), Is.EqualTo(1));
            Assert.That(int.Parse(nodes["Druidkin_Node_TimberWolf"].Element("minLevel")!.Value), Is.EqualTo(2));
            Assert.That(nodes["Druidkin_Node_TimberWolf"].Element("prerequisites")?.Elements("li").Select(e => e.Value), Does.Contain("Druidkin_Node_Rat"));

            // Cougar: cost 1, level 4, requires Timber wolf
            Assert.That(int.Parse(nodes["Druidkin_Node_Cougar"].Element("cost")!.Value), Is.EqualTo(1));
            Assert.That(int.Parse(nodes["Druidkin_Node_Cougar"].Element("minLevel")!.Value), Is.EqualTo(4));
            Assert.That(nodes["Druidkin_Node_Cougar"].Element("prerequisites")?.Elements("li").Select(e => e.Value), Does.Contain("Druidkin_Node_TimberWolf"));

            // Muffalo: cost 1, level 6, requires Rat
            Assert.That(int.Parse(nodes["Druidkin_Node_Muffalo"].Element("cost")!.Value), Is.EqualTo(1));
            Assert.That(int.Parse(nodes["Druidkin_Node_Muffalo"].Element("minLevel")!.Value), Is.EqualTo(6));
            Assert.That(nodes["Druidkin_Node_Muffalo"].Element("prerequisites")?.Elements("li").Select(e => e.Value), Does.Contain("Druidkin_Node_Rat"));

            // Grizzly bear: cost 1, level 8, requires Muffalo
            Assert.That(int.Parse(nodes["Druidkin_Node_GrizzlyBear"].Element("cost")!.Value), Is.EqualTo(1));
            Assert.That(int.Parse(nodes["Druidkin_Node_GrizzlyBear"].Element("minLevel")!.Value), Is.EqualTo(8));
            Assert.That(nodes["Druidkin_Node_GrizzlyBear"].Element("prerequisites")?.Elements("li").Select(e => e.Value), Does.Contain("Druidkin_Node_Muffalo"));

            // Megasloth: cost 2, level 12, requires Grizzly bear
            Assert.That(int.Parse(nodes["Druidkin_Node_Megasloth"].Element("cost")!.Value), Is.EqualTo(2));
            Assert.That(int.Parse(nodes["Druidkin_Node_Megasloth"].Element("minLevel")!.Value), Is.EqualTo(12));
            Assert.That(nodes["Druidkin_Node_Megasloth"].Element("prerequisites")?.Elements("li").Select(e => e.Value), Does.Contain("Druidkin_Node_GrizzlyBear"));
        }

        [Test]
        public void StarterContent_PerksAndUpgrades_MatchDesignSpec()
        {
            string nodesPath = Path.Combine(modRoot, "Defs", "WildShapeNodeDefs", "WildShapeNodes_Druidkin.xml");
            XDocument doc = XDocument.Load(nodesPath);
            var nodes = doc.Root!.Elements("Druidkin.WildShapeNodeDef").ToDictionary(n => n.Element("defName")!.Value);

            // Upgrades all cost 2, require their form, and apply to all forms (no form element in effects)
            string[] forms = { "Rat", "TimberWolf", "Cougar", "Muffalo", "GrizzlyBear", "Megasloth" };
            foreach (string form in forms)
            {
                string upgradeDef = $"Druidkin_Node_Upgrade{form}";
                string formDef = $"Druidkin_Node_{form}";

                Assert.That(nodes.ContainsKey(upgradeDef), Is.True, $"Upgrade node {upgradeDef} missing.");
                Assert.That(int.Parse(nodes[upgradeDef].Element("cost")!.Value), Is.EqualTo(2));
                Assert.That(nodes[upgradeDef].Element("prerequisites")?.Elements("li").Select(e => e.Value), Does.Contain(formDef));
                Assert.That(nodes[upgradeDef].Element("effects")?.Descendants("form").Any(), Is.False,
                    $"Upgrade node {upgradeDef} should not restrict to a single form.");
            }

            // Perks all cost 2
            Assert.That(int.Parse(nodes["Druidkin_Node_Perk_EnduringShape"].Element("cost")!.Value), Is.EqualTo(2));
            Assert.That(int.Parse(nodes["Druidkin_Node_Perk_EnduringShape"].Element("minLevel")!.Value), Is.EqualTo(3));

            Assert.That(int.Parse(nodes["Druidkin_Node_Perk_SwiftReturn"].Element("cost")!.Value), Is.EqualTo(2));
            Assert.That(int.Parse(nodes["Druidkin_Node_Perk_SwiftReturn"].Element("minLevel")!.Value), Is.EqualTo(5));

            Assert.That(int.Parse(nodes["Druidkin_Node_Perk_MendingRevert"].Element("cost")!.Value), Is.EqualTo(2));
            Assert.That(int.Parse(nodes["Druidkin_Node_Perk_MendingRevert"].Element("minLevel")!.Value), Is.EqualTo(10));
        }

        [Test]
        public void GenesXml_HasGeneClassAndProgressionExtension()
        {
            string genesPath = Path.Combine(modRoot, "Defs", "GeneDefs", "Genes_Druidkin.xml");
            XDocument doc = XDocument.Load(genesPath);
            var gene = doc.Root!.Elements("GeneDef").FirstOrDefault(g => g.Element("defName")?.Value == "Druidkin_WildShape");

            Assert.That(gene, Is.Not.Null);
            Assert.That(gene!.Element("geneClass")?.Value, Is.EqualTo("Druidkin.Gene_Druid"));

            var ext = gene.Element("modExtensions")?.Element("li");
            Assert.That(ext, Is.Not.Null);
            Assert.That(ext!.Attribute("Class")?.Value, Is.EqualTo("Druidkin.WildShapeProgressionExtension"));
            Assert.That(ext.Element("pointsPerLevel")?.Value, Is.EqualTo("1"));
            Assert.That(ext.Element("maxLevel")?.Value, Is.EqualTo("20"));
        }

        [Test]
        public void HediffsXml_IncludesXpComp()
        {
            string hediffsPath = Path.Combine(modRoot, "Defs", "HediffDefs", "Hediffs_Druidkin.xml");
            XDocument doc = XDocument.Load(hediffsPath);
            var hediff = doc.Root!.Elements("HediffDef").FirstOrDefault(h => h.Element("defName")?.Value == "Druidkin_WildShapeForm");

            Assert.That(hediff, Is.Not.Null);
            var comp = hediff!.Element("comps")?.Elements("li").FirstOrDefault(c => c.Attribute("Class")?.Value == "Druidkin.HediffCompProperties_WildShapeXp");
            Assert.That(comp, Is.Not.Null);
        }

        [Test]
        public void InspectorTabsPatch_AddsWildShapeTab()
        {
            string patchPath = Path.Combine(modRoot, "Patches", "Human_InspectorTabs.xml");
            XDocument doc = XDocument.Load(patchPath);

            var valueElements = doc.Descendants("value").SelectMany(v => v.Elements("li")).ToList();
            Assert.That(valueElements.Any(e => e.Value == "Druidkin.ITab_Pawn_WildShape"), Is.True);
        }

        [Test]
        public void TranslationKeys_AllExistAndPopulated()
        {
            string keysPath = Path.Combine(modRoot, "Languages", "English", "Keyed", "Druidkin_Keys.xml");
            XDocument doc = XDocument.Load(keysPath);
            var keys = doc.Root!.Elements().ToDictionary(e => e.Name.LocalName, e => e.Value);

            string[] expectedKeys =
            {
                "Druidkin_TabWildShape",
                "Druidkin_Level",
                "Druidkin_MaxLevel",
                "Druidkin_PowerMultiplier",
                "Druidkin_UnspentPointsSingle",
                "Druidkin_UnspentPointsPlural",
                "Druidkin_CategoryForms",
                "Druidkin_CategoryPerks",
                "Druidkin_NodeStatusLearned",
                "Druidkin_NodeCostSingle",
                "Druidkin_NodeCostPlural",
                "Druidkin_RequiresLevelShort",
                "Druidkin_NodeRequiresLevel",
                "Druidkin_NodeRequiresPrereq",
                "Druidkin_NodeClickToLearn",
                "Druidkin_NodeLocked",
                "Druidkin_NodeAlreadyLearned",
                "Druidkin_NodeNotEnoughPoints",
                "Druidkin_ConfirmLearnPrompt",
                "Druidkin_LevelUpSingle",
                "Druidkin_LevelUpPlural",
                "Druidkin_ChooseFormSummary"
            };

            foreach (string expected in expectedKeys)
            {
                Assert.That(keys.ContainsKey(expected), Is.True, $"Key '{expected}' missing from Druidkin_Keys.xml");
                Assert.That(keys[expected], Is.Not.Empty, $"Key '{expected}' must have non-empty translation.");
            }
        }
    }
}
