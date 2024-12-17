using HarmonyLib;
using RimWorld;
using System.Reflection;
using UnityEngine;
using Verse;

namespace NonBinaryGender.Patches
{
    [HarmonyPatch]
    public static class SiblingPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PawnRelationWorker_Sibling), nameof(PawnRelationWorker_Sibling.GenerationChance))]
        public static bool GenerationChancePrefix(Pawn generated, Pawn other, PawnGenerationRequest request, ref float __result, PawnRelationWorker_Sibling __instance)
        {
            if (!ChildRelationUtility.XenotypesCompatible(generated, other))
            {
                return true;
            }
            //Look at the potential sibling's parents
            if (other.GetParent((Gender)3) is Pawn parent)
            {
                float num = 1f;
                Pawn father = other.GetFather();
                Pawn mother = other.GetMother();
                if (father is null && mother is null)
                {
                    num = ChildRelationUtility.ChanceOfBecomingChildOf(generated, parent, null, request, null, null);
                }
                else if (father is not null)
                {
                    num = ChildRelationUtility.ChanceOfBecomingChildOf(generated, father, parent, request, null, null);
                }
                else if (mother is not null)
                {
                    num = ChildRelationUtility.ChanceOfBecomingChildOf(generated, parent, mother, request, null, null);
                }

                float ageDifference = Mathf.Abs(generated.ageTracker.AgeChronologicalYearsFloat - other.ageTracker.AgeChronologicalYearsFloat);
                float ageFactor = 1f;
                if (ageDifference > 40f)
                {
                    ageFactor = 0.02f;
                }
                else if (ageDifference > 10f)
                {
                    ageFactor = 0.65f;
                }
                __result = num * ageFactor * __instance.BaseGenerationChanceFactor(generated, other, request);
                return false;
            }
            return true;
        }

        static MethodInfo GenerateParent = AccessTools.Method(typeof(PawnRelationWorker_Sibling), "GenerateParent");

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PawnRelationWorker_Sibling), nameof(PawnRelationWorker_Sibling.CreateRelation))]
        public static bool CreateRelationPrefix(Pawn generated, Pawn other, ref PawnGenerationRequest request)
        {
            if (other.GetParent((Gender)3) is Pawn parent)
            {
                bool tryMakeSpouses = Rand.Value < 0.85f;
                bool madeNewParent = false;
                Pawn existingMother = other.GetMother();
                Pawn existingFather = other.GetFather();
                if (LovePartnerRelationUtility.HasAnyLovePartner(parent) || (existingMother != null && LovePartnerRelationUtility.HasAnyLovePartner(existingMother)) || (existingFather != null && LovePartnerRelationUtility.HasAnyLovePartner(existingFather)))
                {
                    tryMakeSpouses = false;
                }

                if (existingMother is null && existingFather is null)
                {
                    Gender newParentGender = Rand.Bool ? Gender.Female : Gender.Male;
                    Pawn newParent = (Pawn)GenerateParent.Invoke(null, [generated, other, newParentGender, request, tryMakeSpouses]);
                    if (newParent.gender == Gender.Female)
                    {
                        other.SetMother(newParent);
                    }
                    else
                    {
                        other.SetFather(newParent);
                    }
                    madeNewParent = true;
                }

                generated.SetMother(other.GetMother());
                generated.SetFather(other.GetFather());
                generated.SetParent(parent);
                if (madeNewParent)
                {
                    Pawn otherParent = other.GetMother() ?? other.GetFather();
                    if (!otherParent.story.traits.HasTrait(TraitDefOf.Gay) && !otherParent.story.traits.HasTrait(TraitDefOf.Bisexual))
                    {
                        parent.relations.AddDirectRelation(PawnRelationDefOf.ExLover, otherParent);
                    }
                    else if (tryMakeSpouses)
                    {
                        parent.relations.AddDirectRelation(PawnRelationDefOf.Spouse, otherParent);
                        //Not sure about who's name to use
                        if (parent.Name is NameTriple name)
                        {
                            PawnGenerationRequest request2 = default;
                            SpouseRelationUtility.ResolveNameForSpouseOnGeneration(ref request2, parent);
                            string text = name.Last;
                            string text2 = null;
                            if (request2.FixedLastName != null)
                            {
                                text = request2.FixedLastName;
                            }
                            if (request2.FixedBirthName != null)
                            {
                                text2 = request2.FixedBirthName;
                            }
                            if (parent.story != null && (name.Last != text || parent.story.birthLastName != text2))
                            {
                                parent.story.birthLastName = text2;
                            }
                        }
                    }
                    else
                    {
                        LovePartnerRelationUtility.GiveRandomExLoverOrExSpouseRelation(parent, otherParent);
                    }
                }
                AccessTools.Method(typeof(PawnRelationWorker_Sibling), "ResolveMyName").Invoke(null, [request, generated]);
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PawnRelationWorker_Sibling), "ResolveMyName")]
        public static void ResolveMyNamePrefix(ref PawnGenerationRequest request, Pawn generated)
        {
            if (generated.GetParent((Gender)3) is Pawn parent)
            {
                if (request.FixedLastName == null && ChildRelationUtility.ChildWantsNameOfAnyParent(generated))
                {
                    if (parent.Name is NameTriple name)
                    {
                        request.SetFixedLastName(name.Last);
                    }
                }
            }
        }
    }
}
