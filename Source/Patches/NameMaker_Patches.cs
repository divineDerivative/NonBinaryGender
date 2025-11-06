using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using static NonBinaryGender.Logger;

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
                //if there is only the main name maker, I don't need to do anything. I only need to interfere if there are separate male and female makers
                bool hasSeparateMakers = false;
                RulePackDef maleMaker = null;
                RulePackDef femaleMaker = null;

                //Have to check each def type separately
                if (__instance is PawnKindDef pawnKindDef)
                {
                    if (pawnKindDef.nameMakerFemale is not null)
                    {
                        hasSeparateMakers = true;
                        maleMaker = pawnKindDef.nameMaker;
                        femaleMaker = pawnKindDef.nameMakerFemale;
                    }
                }
                else if (__instance is XenotypeDef xenotypeDef)
                {
                    if (xenotypeDef.nameMakerFemale is not null)
                    {
                        hasSeparateMakers = true;
                        maleMaker = xenotypeDef.nameMaker;
                        femaleMaker = xenotypeDef.nameMakerFemale;
                    }
                }
                else if (__instance is CultureDef cultureDef)
                {
                    if (cultureDef.pawnNameMakerFemale is not null)
                    {
                        hasSeparateMakers = true;
                        maleMaker = cultureDef.pawnNameMaker;
                        femaleMaker = cultureDef.pawnNameMakerFemale;
                    }
                }
                else
                {
                    LogUtil.Error($"Tried to find name makers on an unknown Def type: {__instance.GetType()}");
                    return true;
                }

                if (hasSeparateMakers)
                {
                    //Grab enby maker if it exists
                    RulePackDef enbyMaker = __instance.GetModExtension<EnbyNames>()?.nameMakerEnby;
                    Gender resultGender;

                    if (NonBinaryGenderMod.settings.neutralNames == GenderNeutralNameOption.None)
                    {
                        //don't use the enby maker, pick between male or female
                        resultGender = Rand.Bool ? Gender.Male : Gender.Female;
                    }
                    else if (NonBinaryGenderMod.settings.neutralNames == GenderNeutralNameOption.Only)
                    {
                        //just use the enby maker, if it exists
                        if (enbyMaker is not null)
                        {
                            resultGender = (Gender)3;
                        }
                        //otherwise, randomly pick between male or female
                        else
                        {
                            resultGender = Rand.Bool ? Gender.Male : Gender.Female;
                        }
                    }
                    else
                    {
                        //randomly pick between all three genders
                        int num = Rand.RangeInclusive(1, 3);
                        resultGender = num switch
                        {
                            1 => Gender.Male,
                            2 => Gender.Female,
                            _ => (Gender)3,
                        };
                    }

                    __result = resultGender switch
                    {
                        Gender.Male   => maleMaker,
                        Gender.Female => femaleMaker,
                        _             => enbyMaker,
                    };
                    return false;
                }
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
