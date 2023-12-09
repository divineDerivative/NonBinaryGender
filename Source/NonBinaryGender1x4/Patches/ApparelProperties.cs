using RimWorld;
using Verse;
using HarmonyLib;

namespace NonBinaryGender.Patches
{
    [HarmonyPatch]
    public static class ApparelProperties_Patches
    {
        //Makes them not care about the gender of apparel when deciding what to wear, whether to be upset about wearing the 'wrong' gender, and generating apparel for new pawns
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ApparelProperties), nameof(ApparelProperties.CorrectGenderForWearing))]
        public static bool CorrectGenderForWearingPrefix(Gender wearerGender, ref bool __result)
        {
            if (wearerGender.IsEnby())
            {
                __result = true;
                return false;
            }
            return true;
        }
    }
}
