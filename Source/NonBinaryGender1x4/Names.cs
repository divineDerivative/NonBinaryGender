using System.Collections.Generic;
using RimWorld;
using Verse;
using System.IO;

namespace NonBinaryGender
{
    public static class Names
    {
        public static bool alreadyAdded = false;

        public static void AddUnisexNames()
        {
            if (alreadyAdded)
            {
                return;
            }
            NameBank nameBank = PawnNameDatabaseShuffled.BankOf(PawnNameCategory.HumanStandard);
            char sep = Path.DirectorySeparatorChar;
            string raw = GenFile.TextFromRawFile(GenFilePaths.ModsFolderPath + sep + "NonBinaryGender" + sep + "Languages" + sep + "English" + sep + "Names" + sep + "First_None.txt");
            IEnumerable<string> list = GenText.LinesFromString(raw);
            nameBank.AddNames(PawnNameSlot.First, Gender.None, list);
            nameBank.ErrorCheck();
            Log.Message("Gender neutral names added");
            alreadyAdded = true;
        }

        internal static string OptionTooltip(GenderNeutralNameOption value, bool verbose)
        {
            string result;
            switch (value)
            {
                case GenderNeutralNameOption.None:
                    result = "Enby.GenderNeutralNameOptionTooltipNoneBrief".Translate();
                    if (verbose)
                    {
                        result += "Enby.GenderNeutralNameOptionTooltipNoneVerbose".Translate();
                    }
                    break;
                case GenderNeutralNameOption.Only:
                    result = "Enby.GenderNeutralNameOptionTooltipOnlyBrief".Translate();
                    if (verbose)
                    {
                        result += "Enby.GenderNeutralNameOptionTooltipOnlyVerbose".Translate();
                    }
                    break;
                case GenderNeutralNameOption.Add:
                    result = "Enby.GenderNeutralNameOptionTooltipAddBrief".Translate();
                    if (verbose)
                    {
                        result += "Enby.GenderNeutralNameOptionTooltipAddVerbose".Translate();
                    }
                    break;
                default:
                    return null;

            }
            return result;
        }
    }
}
