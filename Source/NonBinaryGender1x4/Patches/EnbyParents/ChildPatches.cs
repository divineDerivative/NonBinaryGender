using HarmonyLib;
using RimWorld;
using System.Reflection;
using Verse;

namespace NonBinaryGender.Patches
{
    [HarmonyPatch]
    public static class ChildPatches
    {
        //generated is the parent, other is the potential child
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PawnRelationWorker_Child), nameof(PawnRelationWorker_Child.GenerationChance))]
        public static bool GenerationChancePrefix(Pawn generated, Pawn other, PawnGenerationRequest request, ref float __result, PawnRelationWorker_Child __instance)
        {
            if (generated.IsEnby())
            {
#if v1_5
                if (other.IsDuplicate)
                {
                    return true;
                }
#endif
                if (!ChildRelationUtility.XenotypesCompatible(generated, other))
                {
                    __result = 0f;
                    return false;
                }
                float num;
                if (other.GetMother() != null)
                {
                    num = ChildRelationUtility.ChanceOfBecomingChildOf(other, generated, other.GetMother(), null, request, null);
                }
                else if (other.GetFather() != null)
                {
                    num = ChildRelationUtility.ChanceOfBecomingChildOf(other, other.GetFather(), generated, null, null, request);
                }
                else
                {
                    num = ChildRelationUtility.ChanceOfBecomingChildOf(other, other.GetParent((Gender)3), generated, null, null, request);
                }
                if (ModsConfig.BiotechActive && request.Context == PawnGenerationContext.PlayerStarter && other.DevelopmentalStage.Juvenile())
                {
                    num *= 10f;
                }
                __result = num * __instance.BaseGenerationChanceFactor(generated, other, request);
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PawnRelationWorker_Child), nameof(PawnRelationWorker_Child.CreateRelation))]
        public static bool CreateRelationPrefix(Pawn generated, Pawn other, ref PawnGenerationRequest request)
        {
            if (generated.IsEnby())
            {
                other.SetParent(generated);
                if (other.GetMother() is Pawn mother)
                {
                    ResolveOtherParent(mother, generated, other, ref request);
                }
                if (other.GetFather() is Pawn father)
                {
                    ResolveOtherParent(father, generated, other, ref request);
                }
                return false;
            }
            return true;
        }

        static MethodInfo ResolveMyName = AccessTools.Method(typeof(PawnRelationWorker_Child), "ResolveMyName");
        private static void ResolveOtherParent(Pawn parent, Pawn generated, Pawn child, ref PawnGenerationRequest request)
        {
            ResolveMyName.Invoke(null, [request, child, parent]);
            //Next is an orientation check. It should probably just be not straight/asexual yeah?
            if (!parent.story.traits.HasTrait(TraitDefOf.Gay) && !parent.story.traits.HasTrait(TraitDefOf.Bisexual))
            {
                generated.relations.AddDirectRelation(PawnRelationDefOf.ExLover, parent);
            }
            else if (Rand.Value < 0.85f && !LovePartnerRelationUtility.HasAnyLovePartner(parent))
            {
                generated.relations.AddDirectRelation(PawnRelationDefOf.Spouse, parent);
                SpouseRelationUtility.ResolveNameForSpouseOnGeneration(ref request, generated);
            }
            else
            {
                LovePartnerRelationUtility.GiveRandomExLoverOrExSpouseRelation(generated, parent);
            }
        }
    }
}
