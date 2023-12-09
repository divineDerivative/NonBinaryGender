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
        [HarmonyPatch(typeof(PawnStyleItemChooser), nameof(PawnStyleItemChooser.StyleItemChoiceLikelihoodFor))]
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
                if (ModsConfig.BiotechActive && pawn.genes != null && styleItem.requiredGene != null && !pawn.genes.HasGene(styleItem.requiredGene))
                {
                    __result = 0f;
                    return false;
                }
                __result = 60f;
                return false;
            }
            return true;
        }
    }
}
