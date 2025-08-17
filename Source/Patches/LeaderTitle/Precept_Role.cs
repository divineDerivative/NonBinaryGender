using HarmonyLib;
using RimWorld;
using Verse;

namespace NonBinaryGender.Patches
{
    [HarmonyPatch(typeof(Precept_Role), nameof(Precept_Role.LabelForPawn))]
    public static class Precept_Role_LabelForPawn
    {
        public static bool Prefix(ref string __result, Pawn p, PreceptDef ___def, Ideo ___ideo)
        {
            if (___def.leaderRole && p.IsEnby())
            {
                __result = ___ideo.GetTitleFor();
                return false;
            }

            return true;
        }
    }
}
