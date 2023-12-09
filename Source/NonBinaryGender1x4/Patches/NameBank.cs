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
                switch (NonBinaryGenderMod.settings.neutralNames)
                {
                    case GenderNeutralNameOption.Only:
                        __result = ___names[(int)Gender.None, (int)slot];
                        break;
                    case GenderNeutralNameOption.Add:
                        __result = ___names[(int)Gender.Male, (int)slot].Concat(___names[(int)Gender.Female, (int)slot]).Concat(___names[(int)Gender.None, (int)slot]).ToList();
                        break;
                    default:
                        __result = ___names[(int)Gender.Male, (int)slot].Concat(___names[(int)Gender.Female, (int)slot]).ToList();
                        break;
                }
                return false;
            }
            return true;
        }
    }
}