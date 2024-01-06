using System;
using Verse;
using HarmonyLib;
using System.Reflection;
using PortraitsOfTheRim;
using RimWorld;

namespace NonBinaryGender
{
    public static class PortraitsofTheRimPatches
    {
        public static bool forMatches = false;
        private static BodyTypeDef bodyTypeDef;
        private static Gender? oldGender;
        //The vanilla uses of ToBodyType is to assign a body type, but Matches uses it to compare to the existing body type
        //So for standard genetic body we just want to return the body type that they have
        public static bool ToBodyTypePrefix(GeneticBodyType bodyType, Pawn pawn, ref BodyTypeDef __result)
        {
            if (forMatches && pawn.IsEnby() && bodyType == GeneticBodyType.Standard)
            {
                __result = bodyTypeDef;
                return false;
            }
            return true;
        }

        public static void MatchesPrefix(Portrait portrait, ref PortraitElementDef portraitElementDef)
        {
            if (portrait.pawn.IsEnby())
            {
                if (portraitElementDef.requirements.body != null && portraitElementDef.requirements.gender.HasValue)
                {
                    if (portraitElementDef.requirements.body == GeneticBodyType.Standard)
                    {
                        switch (portraitElementDef.requirements.gender)
                        {
                            case Gender.Male:
                                bodyTypeDef = BodyTypeDefOf.Male;
                                break;
                            case Gender.Female:
                                bodyTypeDef = BodyTypeDefOf.Female;
                                break;
                        }
                        oldGender = portraitElementDef.requirements.gender;
                        portraitElementDef.requirements.gender = null;
                    }
                }
                forMatches = true;
            }
        }

        public static void MatchesPostfix(Portrait portrait, ref PortraitElementDef portraitElementDef, BoolReport __result)
        {
            forMatches = false;
            bodyTypeDef = null;
            if (oldGender.HasValue)
            {
                portraitElementDef.requirements.gender = oldGender;
                oldGender = null;
            }
        }

        public static void PatchPortraits(this Harmony harmony)
        {
            MethodInfo Matches = AccessTools.Method(typeof(Requirements), nameof(Requirements.Matches), new Type[] { typeof(Portrait), typeof(PortraitElementDef) });
            harmony.Patch(Matches, prefix: new HarmonyMethod(typeof(PortraitsofTheRimPatches), nameof(MatchesPrefix)), postfix: new HarmonyMethod(typeof(PortraitsofTheRimPatches), nameof(MatchesPostfix)));
            MethodInfo ToBodyType = AccessTools.Method(typeof(GeneUtility), nameof(GeneUtility.ToBodyType));
            harmony.Patch(ToBodyType, prefix: new HarmonyMethod(typeof(PortraitsofTheRimPatches), nameof(ToBodyTypePrefix)));
        }
    }
}
