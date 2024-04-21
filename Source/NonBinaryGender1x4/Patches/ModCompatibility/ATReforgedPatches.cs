using ATReforged;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace NonBinaryGender.Patches
{
    public static class ATReforgedPatches
    {
        public static void PatchATR(this Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(ATRCore_Utils), nameof(ATRCore_Utils.GenerateGender)), transpiler: new HarmonyMethod(typeof(ATReforgedPatches), nameof(GenerateGenderranspiler)));
        }

        public static IEnumerable<CodeInstruction> GenerateGenderranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            FieldInfo androidsPickGenders = AccessTools.Field(typeof(ATReforgedCore_Settings), nameof(ATReforgedCore_Settings.androidsPickGenders));
            Label? label = new Label();
            Label myLabel = ilg.DefineLabel();
            List<CodeInstruction> codes = instructions.ToList();
            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction code = codes[i];
                CodeInstruction nextCode = codes[i + 1];

                if (code.LoadsField(androidsPickGenders) && nextCode.Branches(out label))
                {
                    break;
                }
            }

            foreach (CodeInstruction code in instructions)
            {
                if (code.labels.Contains((Label)label))
                {
                    yield return CodeInstruction.LoadField(typeof(NonBinaryGenderMod), nameof(NonBinaryGenderMod.settings)).MoveLabelsFrom(code);
                    yield return CodeInstruction.LoadField(typeof(Settings), nameof(Settings.enbyChance));
                    yield return CodeInstruction.Call(typeof(Rand), nameof(Rand.Chance), [typeof(float)]);
                    yield return new CodeInstruction(OpCodes.Brfalse, myLabel);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_3);
                    yield return new CodeInstruction(OpCodes.Ret);
                    yield return code.WithLabels(myLabel);
                }
                else
                {
                    yield return code;
                }
            }
        }
    }
}
