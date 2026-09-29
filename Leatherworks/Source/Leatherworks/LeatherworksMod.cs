using HarmonyLib;
using Verse;

namespace Leatherworks
{
    public class LeatherworksMod : Mod
    {
        public LeatherworksMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("throwingfish.leatherworks");
            harmony.PatchAll();
        }
    }
}
