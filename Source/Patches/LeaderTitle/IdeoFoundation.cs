using HarmonyLib;
using RimWorld;

namespace NonBinaryGender.Patches
{
    [HarmonyPatch]
    public static class IdeoFoundationPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(IdeoFoundation), nameof(IdeoFoundation.GenerateLeaderTitle))]
        public static void GenerateLeaderTitlePostfix(ref IdeoFoundation __instance)
        {
            Ideo ideo = __instance.ideo;
            ideo.SetTitleFor(ideo.leaderTitleMale);
        }
    }
}
