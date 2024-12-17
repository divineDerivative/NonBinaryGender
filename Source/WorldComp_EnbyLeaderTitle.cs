using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;
using Verse.Grammar;

namespace NonBinaryGender
{
    public class WorldComp_EnbyLeaderTitle(World world) : WorldComponent(world)
    {
        public Dictionary<Ideo, string> TitlesPerIdeo = [];
        List<Ideo> tempIdeos;
        List<string> tempTitles;

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref TitlesPerIdeo, "enbyLeaderTitles", LookMode.Reference, LookMode.Value, ref tempIdeos, ref tempTitles);
        }
        //This was for generating a title if the entry was null, but so far that hasn't happened
        //I'll keep this just in case though
        public string GenerateLeaderTitle(Ideo ideo)
        {

#if v1_4
            if (ideo.hiddenIdeoMode)
#else
            if (ideo.classicMode)
#endif
            {
                return PreceptDefOf.IdeoRole_Leader.label;
            }
            if (ideo.culture.leaderTitleMaker == null)
            {
                return null;
            }
            GrammarRequest request = default;
            request.Includes.Add(ideo.culture.leaderTitleMaker);
            for (int i = 0; i < ideo.memes.Count; i++)
            {
                if (ideo.memes[i].generalRules != null)
                {
                    request.IncludesBare.Add(ideo.memes[i].generalRules);
                }
            }
            return NameGenerator.GenerateName(request, null, appendNumberIfNameUsed: false, "r_leaderTitle");
        }
    }
}
