using RimWorld;
using Verse;

namespace NonBinaryGender
{
    public class EnbyInfo : DefModExtension
    {
        [MustTranslate]
        public string labelEnby;

        public ThoughtDef diedThoughtEnby;
        public ThoughtDef lostThoughtEnby;
        public ThoughtDef killedThoughtEnby;
    }
    
    public class EnbyNames : DefModExtension
    {
        public RulePackDef nameMakerEnby;
    }
}
