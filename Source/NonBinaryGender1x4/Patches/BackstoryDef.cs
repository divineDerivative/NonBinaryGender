using HarmonyLib;
using RimWorld;
using Verse;

namespace NonBinaryGender.Patches
{
    [HarmonyPatch]
    public static class BackstoryDef_Patches
    {
        //This picks randomly between the male and female body types on the backstory, instead of defaulting to male
        [HarmonyPrefix]
        [HarmonyPatch(typeof(BackstoryDef), nameof(BackstoryDef.BodyTypeFor))]
        public static bool BodyTypeForPrefix(Gender g, ref BodyTypeDef __result, BodyTypeDef ___bodyTypeGlobal, BodyTypeDef ___bodyTypeFemale, BodyTypeDef ___bodyTypeMale)
        {
            if (g.IsEnby() && ___bodyTypeGlobal == null)
            {
                __result = Rand.Value < 0.5f ? ___bodyTypeFemale : ___bodyTypeMale;
                return false;
            }
            return true;
        }
    }
}
