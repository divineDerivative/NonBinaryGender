﻿using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
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
                Dictionary<Ideo, string> list = Find.World.GetComponent<WorldComp_EnbyLeaderTitle>().TitlesPerIdeo;
                if (___ideos != null && ___ideos.PrimaryIdeo != null)
                {
                    __result = list[___ideos.PrimaryIdeo];
                    return false;
                }
            }
            return true;
        }
    }
}
