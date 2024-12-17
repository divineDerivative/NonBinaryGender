using HarmonyLib;
using RimWorld;
using Verse;

namespace NonBinaryGender.Patches
{
    [HarmonyPatch]
    public static class PawnStyleItemChooser_Patches
    {
        //This just removes the gender stuff from the original method, used for choosing hair/beard/tattoo based on ideo gender settings
        [HarmonyPrefix]
#if v1_4
        [HarmonyPatch(typeof(PawnStyleItemChooser), nameof(PawnStyleItemChooser.StyleItemChoiceLikelihoodFor))]
#else
        [HarmonyPatch(typeof(PawnStyleItemChooser), nameof(PawnStyleItemChooser.FrequencyFromGender))]
#endif
        public static bool StyleItemChoiceLikelihoodForPatch(ref float __result, StyleItemDef styleItem, Pawn pawn)
        {
            if (pawn.IsEnby())
            {
                //Replicating some vanilla stuff
                if (pawn.Ideo == null)
                {
                    __result = 100f;
                    return false;
                }

#if v1_4
                if (ModsConfig.BiotechActive && pawn.genes != null && styleItem.requiredGene != null && !pawn.genes.HasGene(styleItem.requiredGene))
#else
                if (ModsConfig.BiotechActive && pawn.genes != null && styleItem.requiredGene != null && !pawn.genes.HasActiveGene(styleItem.requiredGene))
#endif
                {
                    __result = 0f;
                    return false;
                }
#if v1_5
                if (ModsConfig.AnomalyActive && pawn.IsMutant && styleItem.requiredMutant != null && pawn.mutant.Def != styleItem.requiredMutant)
                {
                    __result = 0f;
                    return false;
                }
#endif
                __result = 70f;
                return false;
            }
            return true;
        }
    }
}
