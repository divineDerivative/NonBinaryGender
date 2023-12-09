using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace NonBinaryGender.Patches
{
    [HarmonyPatch]
    public static class Pawn_StoryTracker_Patches
    {
        //This allows selection of head types regardless of gender
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Pawn_StoryTracker), nameof(Pawn_StoryTracker.TryGetRandomHeadFromSet))]
        public static bool TryGetRandomHeadFromSetPatch(ref bool __result, Pawn ___pawn, ref HeadTypeDef ___headType, IEnumerable<HeadTypeDef> options)
        {
            if (___pawn.IsEnby())
            {
                Rand.PushState(___pawn.thingIDNumber);
                bool result = options.Where((HeadTypeDef h) => CanUseHeadType(h, ___pawn)).TryRandomElementByWeight((HeadTypeDef x) => x.selectionWeight, out ___headType);
                Rand.PopState();
                __result = result;
                return false;
            }
            return true;
        }

        //This replicates a local method used in the original, just with the gender stuff removed
        private static bool CanUseHeadType(HeadTypeDef head, Pawn pawn)
        {
            if (ModsConfig.BiotechActive && !head.requiredGenes.NullOrEmpty())
            {
                if (pawn.genes == null)
                {
                    return false;
                }
                foreach (GeneDef requiredGene in head.requiredGenes)
                {
                    if (!pawn.genes.HasGene(requiredGene))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
