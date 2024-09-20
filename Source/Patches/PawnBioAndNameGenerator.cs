using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;

namespace NonBinaryGender.Patches
{
    [HarmonyPatch]
    public static class PawnBioAndNameGenerator_Patches
    {
        //This makes it pick only solid names with a gender possibility of Either for enby pawns
        //Do I want to try to change the behavior based on the neutral names settings?
        //That will make it more complicated
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PawnBioAndNameGenerator), nameof(PawnBioAndNameGenerator.TryGetRandomUnusedSolidName))]
        public static IEnumerable<CodeInstruction> TryGetRandomUnusedSolidNameTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            List<CodeInstruction> codes = instructions.ToList();

            bool firstBranch = false;
            bool secondBranch = false;
            Label? storeSecondList = new Label();
            CodeInstruction checkCode = default;
            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction code = codes[i];

                if (code.opcode == OpCodes.Ldarg_0)
                {
                    firstBranch = true;
                }

                if (firstBranch && secondBranch && code.Branches(out storeSecondList))
                {
                    break;
                }
                else if (firstBranch && code.Branches(out _))
                {
                    checkCode = code;
                    secondBranch = true;
                }
            }

            Label myFirstLabel = ilg.DefineLabel();
            foreach (CodeInstruction code in codes)
            {
                if (code.operand is Label bah && bah == (Label)storeSecondList)
                {
                    Label mySecondLabel = ilg.DefineLabel();
                    code.operand = mySecondLabel;
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldc_I4_2).WithLabels(myFirstLabel);
                    yield return CodeInstruction.Call(typeof(PawnNameDatabaseSolid), nameof(PawnNameDatabaseSolid.GetListForGender));
                    yield return new CodeInstruction(OpCodes.Br_S, storeSecondList).WithLabels(mySecondLabel);
                }
                else
                {
                    yield return code;
                }
                if (code == checkCode)
                {
                    //Insert a check for enby then jump to the right spot
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_3);
                    yield return new CodeInstruction(OpCodes.Beq_S, myFirstLabel);
                }
            }
        }

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(PawnBioAndNameGenerator), nameof(PawnBioAndNameGenerator.TryGetRandomUnusedSolidName))]
        public static void TryGetRandomUnusedSolidNameLogging(Gender gender, NameTriple __result)
        {
            if (gender.IsEnby())
            {
                GenderPossibility possibility = default;
                if (PawnNameDatabaseSolid.GetListForGender(GenderPossibility.Either).Contains(__result))
                {
                    possibility = GenderPossibility.Either;
                }
                else if (PawnNameDatabaseSolid.GetListForGender(GenderPossibility.Male).Contains(__result))
                {
                    possibility = GenderPossibility.Male;
                }
                else if (PawnNameDatabaseSolid.GetListForGender(GenderPossibility.Female).Contains(__result))
                {
                    possibility = GenderPossibility.Female;
                }
                Log.Warning($"Solid name \'{__result}\' chosen for enby pawn with gender possibility of {possibility}");
            }
        }

        //Limits solid bios to those with a gender possibility of either if the neutral names setting is only
        //Otherwise default behavior lets all gender possibilities apply
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PawnBioAndNameGenerator), "IsBioUseable")]
        public static void IsBioUseablePostfix(PawnBio bio, Gender gender, ref bool __result)
        {
            if (__result && gender.IsEnby() && NonBinaryGenderMod.settings.neutralNames == GenderNeutralNameOption.Only && bio.gender != GenderPossibility.Either)
            {
                __result = false;
            }
        }

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(PawnBioAndNameGenerator), "TryGetRandomUnusedSolidBioFor")]
        public static void TryGetRandomUnusedSolidBioForLogging(Gender gender, PawnBio result, ref bool __result)
        {
            if (__result && gender.IsEnby())
            {
                Log.Warning("Solid bio \'" + result.name + "\' chosen for enby pawn with gender possibility of " + result.gender);
            }
        }
    }
}
