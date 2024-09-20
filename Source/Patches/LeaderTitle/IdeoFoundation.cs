using HarmonyLib;
using RimWorld;
using Verse;

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
            WorldComp_EnbyLeaderTitle comp = Find.World.GetComponent<WorldComp_EnbyLeaderTitle>();
            string name = ideo.leaderTitleMale;
            if (comp.TitlesPerIdeo.ContainsKey(ideo))
            {
                comp.TitlesPerIdeo[ideo] = name;
            }
            else
            {
                comp.TitlesPerIdeo.Add(ideo, name);
            }
        }
    }
}
