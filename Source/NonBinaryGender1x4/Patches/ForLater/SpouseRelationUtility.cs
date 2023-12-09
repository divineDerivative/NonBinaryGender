using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using HarmonyLib;

namespace NonBinaryGender.Patches
{
    [HarmonyPatch]
    internal class SpouseRelationUtility_Patches
    {
        //This is only used when generating parents, so only matters if I make changes allowing non-binary parents
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SpouseRelationUtility), nameof(SpouseRelationUtility.GetFirstSpouseOfOppositeGender))]
        public static bool GetFirstSpouseOfOppositeGenderPrefix(Pawn pawn, ref Pawn __result)
        {
            if (pawn.IsEnby())
            {

            }
            return true;
        }
    }
}
