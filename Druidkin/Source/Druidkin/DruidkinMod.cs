using HarmonyLib;
using Verse;

namespace Druidkin
{
    public class DruidkinMod : Mod
    {
        public DruidkinMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("throwingfish.druidkin");
            harmony.PatchAll();
        }
    }
}
