using HarmonyLib;
using RimWorld;

namespace NonBinaryGender.Patches
{
    public static class IdeoPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Ideo), nameof(Ideo.CopyTo))]
        public static void CopyToPostfix(Ideo ideo, ref Ideo __instance)
        {
            WorldComp_EnbyLeaderTitle comp = PreceptUtility.GetWorldComp();
            //Using GetTitleFor and SetTitleFor would cause the comp to be retrieved twice, so we just do it manually here
            string oldName = comp.TitlesPerIdeo[__instance];
            comp.TitlesPerIdeo.Add(ideo, oldName);
        }
    }
}
