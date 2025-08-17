using RimWorld;
using System.Collections.Generic;
using Verse;

namespace NonBinaryGender
{
    public static class PreceptUtility
    {
        public static WorldComp_EnbyLeaderTitle GetWorldComp() => Find.World.GetComponent<WorldComp_EnbyLeaderTitle>();

        public static string GetTitleFor(this Ideo ideo)
        {
            WorldComp_EnbyLeaderTitle comp = GetWorldComp();
            //I might want to call GenerateLeaderTitle instead of returning null
            return comp.TitlesPerIdeo.GetValueOrDefault(ideo);
        }

        public static void SetTitleFor(this Ideo ideo, string title)
        {
            WorldComp_EnbyLeaderTitle comp = GetWorldComp();
            comp.TitlesPerIdeo[ideo] = title;
        }
    }
}
