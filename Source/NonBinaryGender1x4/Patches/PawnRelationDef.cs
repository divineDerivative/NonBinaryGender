using HarmonyLib;
using RimWorld;
using Verse;

namespace NonBinaryGender.Patches
{
    [HarmonyPatch]
    public static class PawnRelationDef_Patches
    {
        //This returns the correct label for a relationship
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PawnRelationDef), nameof(PawnRelationDef.GetGenderSpecificLabel))]
        public static bool GetGenderSpecificLabelPatch(ref string __result, PawnRelationDef __instance, Pawn pawn)
        {
            if (pawn.IsEnby() && __instance.GetModExtension<EnbyInfo>() is EnbyInfo extension && extension.labelEnby != null)
            {
                __result = extension.labelEnby;
                return false;
            }
            return true;
        }

        //These return the correct thought for each event
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PawnRelationDef), nameof(PawnRelationDef.GetGenderSpecificKilledThought))]
        public static bool GetGenderSpecificKilledThoughtPatch(ref ThoughtDef __result, PawnRelationDef __instance, Pawn killed)
        {
            if (killed.IsEnby() && __instance.GetModExtension<EnbyInfo>() is EnbyInfo extension && extension.killedThoughtEnby != null)
            {
                __result = extension.killedThoughtEnby;
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PawnRelationDef), nameof(PawnRelationDef.GetGenderSpecificLostThought))]
        public static bool GetGenderSpecificLostThoughtPatch(ref ThoughtDef __result, PawnRelationDef __instance, Pawn killed)
        {
            if (killed.IsEnby() && __instance.GetModExtension<EnbyInfo>() is EnbyInfo extension && extension.lostThoughtEnby != null)
            {
                __result = extension.lostThoughtEnby;
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PawnRelationDef), nameof(PawnRelationDef.GetGenderSpecificDiedThought))]
        public static bool GetGenderSpecificDiedThoughtPatch(ref ThoughtDef __result, PawnRelationDef __instance, Pawn killed)
        {
            if (killed.IsEnby() && __instance.GetModExtension<EnbyInfo>() is EnbyInfo extension && extension.diedThoughtEnby != null)
            {
                __result = extension.diedThoughtEnby;
                return false;
            }
            return true;
        }
    }
}
