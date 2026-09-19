using HarmonyLib;
using RimWorld;
using Verse;

namespace Druidkin.HarmonyPatches
{
    /// A shifted druid has paws, not hands. Their own gear is stashed on the hediff at
    /// shift time, but nothing otherwise stops the player ordering a bear to pick up a
    /// rifle, so the equip itself is refused for as long as the shift lasts.
    [HarmonyPatch(typeof(EquipmentUtility), nameof(EquipmentUtility.CanEquip))]
    [HarmonyPatch(new[] { typeof(Thing), typeof(Pawn), typeof(string), typeof(bool) },
        new[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal })]
    public static class Patch_CanEquip
    {
        public static void Postfix(Pawn pawn, ref bool __result, ref string cantReason)
        {
            if (!__result || !WildShapeUtility.IsShifted(pawn))
            {
                return;
            }

            __result = false;
            cantReason = "Druidkin_CannotEquipWhileShifted".Translate();
        }
    }
}
