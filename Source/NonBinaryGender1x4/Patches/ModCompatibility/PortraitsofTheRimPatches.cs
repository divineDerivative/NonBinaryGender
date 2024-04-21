using HarmonyLib;
using PortraitsOfTheRim;
using RimWorld;
using System;
using System.Reflection;
using Verse;

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

        public static void MatchesPrefix(Type portrait, ref Type portraitElementDef)
        {
            Pawn pawn = (Pawn)AccessTools.Field(typeof(Portrait), nameof(Portrait.pawn)).GetValue(portrait);
            Requirements requirements = (Requirements)AccessTools.Field(typeof(PortraitElementDef), nameof(PortraitElementDef.requirements)).GetValue(portraitElementDef);
            if (pawn.IsEnby())
            {
                if (requirements.body != null && requirements.gender.HasValue)
                {
                    if (requirements.body == GeneticBodyType.Standard)
                    {
                        switch (requirements.gender)
                        {
                            case Gender.Male:
                                bodyTypeDef = BodyTypeDefOf.Male;
                                break;
                            case Gender.Female:
                                bodyTypeDef = BodyTypeDefOf.Female;
                                break;
                        }
                        oldGender = requirements.gender;
                        requirements.gender = null;
                    }
                }
                forMatches = true;
            }
        }

        public static void MatchesPostfix(ref Type portraitElementDef)
        {
            forMatches = false;
            bodyTypeDef = null;
            if (oldGender.HasValue)
            {
                Requirements requirements = (Requirements)AccessTools.Field(typeof(PortraitElementDef), nameof(PortraitElementDef.requirements)).GetValue(portraitElementDef);
                requirements.gender = oldGender;
                oldGender = null;
            }
        }

        public static void PatchPortraits(this Harmony harmony)
        {
            MethodInfo Matches = AccessTools.Method(typeof(Requirements), nameof(Requirements.Matches), [typeof(Portrait), typeof(PortraitElementDef)]);
            harmony.Patch(Matches, prefix: new HarmonyMethod(typeof(PortraitsofTheRimPatches), nameof(MatchesPrefix)), postfix: new HarmonyMethod(typeof(PortraitsofTheRimPatches), nameof(MatchesPostfix)));
            MethodInfo ToBodyType = AccessTools.Method(typeof(GeneUtility), nameof(GeneUtility.ToBodyType));
            harmony.Patch(ToBodyType, prefix: new HarmonyMethod(typeof(PortraitsofTheRimPatches), nameof(ToBodyTypePrefix)));
        }
    }
}
