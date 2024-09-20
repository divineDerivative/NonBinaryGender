using HarmonyLib;
using UnityEngine;
using Verse;

namespace NonBinaryGender.Patches
{
    [HarmonyPatch]
    public static class GenderUtility_Patches
    {
        //These return the correct translation key for grammar resolution
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GenderUtility), nameof(GenderUtility.GetLabel))]
        public static bool GetLabelPatch(ref string __result, Gender gender)
        {
            if (gender.IsEnby())
            {
                __result = "NBLabel".Translate();
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GenderUtility), nameof(GenderUtility.GetPronoun))]
        public static bool GetPronounPatch(ref string __result, Gender gender)
        {
            if (gender.IsEnby())
            {
                __result = "NBProthey".Translate();
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GenderUtility), nameof(GenderUtility.GetPossessive))]
        public static bool GetPossessivePatch(ref string __result, Gender gender)
        {
            if (gender.IsEnby())
            {
                __result = "NBProtheir".Translate();
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GenderUtility), nameof(GenderUtility.GetObjective))]
        public static bool GetObjectivePatch(ref string __result, Gender gender)
        {
            if (gender.IsEnby())
            {
                __result = "NBProthemObj".Translate();
                return false;
            }
            return true;
        }

        //Displays the non-binary gender icon in various places
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GenderUtility), nameof(GenderUtility.GetIcon))]
        public static bool GetIconPatch(ref Texture2D __result, Gender gender)
        {
            if (gender.IsEnby())
            {
                __result = EnbyUtility.NonBinaryIcon;
                return false;
            }
            return true;
        }
    }
}
