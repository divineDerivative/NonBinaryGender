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
            if (!PortraitsofTheRimPatches.forMatches && pawn.IsEnby() && bodyType == GeneticBodyType.Standard)
            {
                __result = Rand.Bool ? BodyTypeDefOf.Female : BodyTypeDefOf.Male;
            }
        }
    }
}
