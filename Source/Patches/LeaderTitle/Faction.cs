using HarmonyLib;
using RimWorld;
using Verse;

namespace NonBinaryGender.Patches
{
    [HarmonyPatch(typeof(Faction), nameof(Faction.LeaderTitle), MethodType.Getter)]
    public static class Faction_LeaderTitle
    {
        public static bool Prefix(ref string __result, Pawn ___leader, FactionIdeosTracker ___ideos)
        {
            if (___leader != null && ___leader.gender.IsEnby())
            {
                if (___ideos?.PrimaryIdeo != null)
                {
                    __result = ___ideos.PrimaryIdeo.GetTitleFor();
                    return false;
                }
            }

            return true;
        }
    }
}
