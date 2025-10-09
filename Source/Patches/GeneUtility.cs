using RimWorld;
using Verse;
using HarmonyLib;
using System.Collections.Generic;

namespace NonBinaryGender.Patches
{
    [HarmonyPatch]
    public static class GeneUtility_Patches
    {
        public static readonly HashSet<BodyTypeDef> tmpBodyTypes = AccessTools.StaticFieldRefAccess<HashSet<BodyTypeDef>>(typeof(PawnGenerator),"tmpBodyTypes");
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GeneUtility), nameof(GeneUtility.ToBodyType))]
        public static void ToBodyTypePostfix(GeneticBodyType bodyType, Pawn pawn, ref BodyTypeDef __result)
        {
            if (forGenesChanged && bodyType == GeneticBodyType.Standard)
            {
                __result = Rand.Bool ? BodyTypeDefOf.Female : BodyTypeDefOf.Male;
            }
        }
        
        static bool forGenesChanged = false;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Pawn_GeneTracker), "Notify_GenesChanged")]
        public static void Notify_GenesChangedPrefix(Pawn_GeneTracker __instance)
        {
            if (__instance.pawn.IsEnby())
            {
                forGenesChanged = true;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Pawn_GeneTracker), "Notify_GenesChanged")]
        public static void Notify_GenesChangedPostfix(Pawn_GeneTracker __instance)
        {
            forGenesChanged = false;
        }
    }
}
