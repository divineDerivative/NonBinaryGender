using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Reflection;

namespace NonBinaryGender.Patches
{
    [HarmonyPatch]
    public static class NameMaker_Patches
    {
        [HarmonyPrefix]
        public static bool Prefix(Gender gender, ref RulePackDef __result, Def __instance)
        {
            if (gender.IsEnby())
            {
                if (__instance.GetModExtension<EnbyNames>() is EnbyNames extension && extension.nameMakerEnby != null)
                {
                    __result = extension.nameMakerEnby;
                }
                return false;
            }

            return true;
        }

        //There are four places that can have name makers for humanlike pawns. Backstories do not have gender specific name makers, so they do not need to be intercepted.
        public static IEnumerable<MethodInfo> TargetMethods()
        {
            yield return AccessTools.Method(typeof(PawnKindDef), nameof(PawnKindDef.GetNameMaker));
            yield return AccessTools.Method(typeof(XenotypeDef), nameof(XenotypeDef.GetNameMaker));
            yield return AccessTools.Method(typeof(CultureDef), nameof(CultureDef.GetPawnNameMaker));
        }
    }
}
