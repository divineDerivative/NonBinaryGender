using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace NonBinaryGender.Patches
{
    [HarmonyPatch]
    public static class NameBank_Patches
    {
        //This picks the appropriate set of name banks depending on mod settings
        [HarmonyPrefix]
        [HarmonyPatch(typeof(NameBank), "NamesFor")]
        public static bool NamesForPatch(NameBank __instance, ref List<string> __result, PawnNameSlot slot, Gender gender, List<string>[,] ___names)
        {
            if (gender.IsEnby())
            {
                __result = NonBinaryGenderMod.settings.neutralNames switch
                {
                    GenderNeutralNameOption.Only => ___names[(int)Gender.None, (int)slot],
                    GenderNeutralNameOption.Add => ___names[(int)Gender.Male, (int)slot].Concat(___names[(int)Gender.Female, (int)slot]).Concat(___names[(int)Gender.None, (int)slot]).ToList(),
                    _ => ___names[(int)Gender.Male, (int)slot].Concat(___names[(int)Gender.Female, (int)slot]).ToList(),
                };
                return false;
            }
            return true;
        }
    }
}