using System.Collections.Generic;
using Druidkin.HarmonyPatches;
using NUnit.Framework;
using RimWorld;

namespace Druidkin.Tests
{
    [TestFixture]
    public class ShiftedEatingTests
    {
        private static ThoughtDef Thought(string defName, float mood)
        {
            return new ThoughtDef
            {
                defName = defName,
                stages = new List<ThoughtStage> { new ThoughtStage { baseMoodEffect = mood } }
            };
        }

        [Test]
        public void RemoveBadThoughts_DropsNegativeAndKeepsPositive()
        {
            ThoughtDef ateCorpse = Thought("AteCorpse", -6f);
            ThoughtDef ateRaw = Thought("AteRawFood", -7f);
            ThoughtDef fineMeal = Thought("AteFineMeal", 5f);
            var thoughts = new List<FoodUtility.ThoughtFromIngesting>
            {
                new FoodUtility.ThoughtFromIngesting { thought = ateCorpse },
                new FoodUtility.ThoughtFromIngesting { thought = fineMeal },
                new FoodUtility.ThoughtFromIngesting { thought = ateRaw }
            };

            ShiftedEating.RemoveBadThoughts(thoughts);

            Assert.That(thoughts.Count, Is.EqualTo(1));
            Assert.That(thoughts[0].thought, Is.SameAs(fineMeal));
        }

        [Test]
        public void IsBad_HandlesThoughtWithoutStages()
        {
            Assert.That(ShiftedEating.IsBad(new ThoughtDef { defName = "NoStages" }), Is.False);
            Assert.That(ShiftedEating.IsBad(null), Is.False);
        }
    }
}
