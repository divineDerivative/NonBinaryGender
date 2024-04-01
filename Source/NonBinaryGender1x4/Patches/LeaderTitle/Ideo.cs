using HarmonyLib;
using RimWorld;
using Verse;
using System;
using System.Collections.Generic;

namespace NonBinaryGender.Patches
{
    public static class IdeoPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Ideo), nameof(Ideo.CopyTo))]
        public static void CopyToPostfix(Ideo ideo, ref Ideo __instance)
        {
            WorldComp_EnbyLeaderTitle comp = Find.World.GetComponent<WorldComp_EnbyLeaderTitle>();
            string oldName = comp.TitlesPerIdeo[__instance];
            comp.TitlesPerIdeo.Add(ideo, oldName);
        }
    }
}