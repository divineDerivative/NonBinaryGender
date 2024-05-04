using HarmonyLib;
using PawnEditor;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace NonBinaryGender.Patches
{
    public static class PawnEditorPatches
    {
        public static void PatchPE(this Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(TabWorker_Bio_Humanlike), "DoButtons"), transpiler: new HarmonyMethod(AccessTools.Method(typeof(PawnEditorPatches), nameof(DoButtonsTranspiler))));
            harmony.Patch(AccessTools.Method(typeof(Dialog_AppearanceEditor), "DoLeftSection"), transpiler: new HarmonyMethod(AccessTools.Method(typeof(PawnEditorPatches), nameof(DoLeftSectionTranspiler))));
            harmony.Patch(AccessTools.Method(typeof(Dialog_AppearanceEditor), "IsAllowed", [typeof(HeadTypeDef), typeof(Pawn)]), transpiler: new HarmonyMethod(AccessTools.Method(typeof(PawnEditorPatches), nameof(HeadTypeTranspiler))));
        }

        public static IEnumerable<CodeInstruction> GenderButtonTranspiler(IEnumerable<CodeInstruction> instructions, OpCode loadPawn, Type type = null)
        {
            bool malefound = false;
            foreach (CodeInstruction code in instructions)
            {
                yield return code;
                if (code.LoadsConstant("Male"))
                {
                    malefound = true;
                }
                if (malefound && code.Calls(typeof(List<FloatMenuOption>), nameof(List<FloatMenuOption>.Add)))
                {
                    yield return new CodeInstruction(OpCodes.Nop);
                    yield return new CodeInstruction(OpCodes.Dup);
                    yield return new CodeInstruction(loadPawn);
                    if (type != null)
                    {
                        yield return CodeInstruction.LoadField(type, "pawn");
                    }
                    yield return CodeInstruction.Call(typeof(PawnEditorPatches), nameof(DoButtonsHelper));
                    yield return code;
                    malefound = false;
                }
            }
        }

        public static IEnumerable<CodeInstruction> DoButtonsTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return GenderButtonTranspiler(instructions, OpCodes.Ldarg_2);
        }

        public static IEnumerable<CodeInstruction> DoLeftSectionTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return GenderButtonTranspiler(instructions, OpCodes.Ldarg_0, typeof(Dialog_AppearanceEditor));
        }

        private static FloatMenuOption DoButtonsHelper(Pawn pawn)
        {
            return new FloatMenuOption("NBLabel".Translate().CapitalizeFirst(), delegate
            {
                TabWorker_Bio_Humanlike.SetGender(pawn, (Gender)3);
            }, EnbyUtility.NonBinaryIcon, Color.white);
        }

        public static IEnumerable<CodeInstruction> HeadTypeTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            Label label = ilg.DefineLabel();
            bool insert = false;
            foreach (CodeInstruction code in instructions)
            {
                if (insert)
                {
                    yield return new CodeInstruction(OpCodes.Beq, label);
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return CodeInstruction.Call(typeof(EnbyUtility), nameof(EnbyUtility.IsEnby), [typeof(Pawn)]);
                    yield return new CodeInstruction(OpCodes.Ret);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_1).WithLabels(label);
                    insert = false;
                }
                else
                {
                    yield return code;
                }

                if (code.LoadsField(InfoHelper.genderField))
                {
                    insert = true;
                }
            }
        }
    }
}
