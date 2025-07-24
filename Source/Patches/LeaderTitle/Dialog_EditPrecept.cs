using static NonBinaryGender.Logger;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using UnityEngine;
using Verse;

namespace NonBinaryGender.Patches
{
    [HarmonyPatch]
    public static class Dialog_EditPrecept_Patches
    {
        private static string enbyLabel;
        private static bool adjust = false;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Dialog_EditPrecept), "UpdateWindowHeight")]
        public static void UpdateWindowHeightPostfix(Precept ___precept)
        {
            if (___precept.def.leaderRole)
            {
                adjust = true;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Dialog_EditPrecept), nameof(Dialog_EditPrecept.InitialSize), MethodType.Getter)]
        public static void InitialSizePrefix(ref float ___windowHeight, float ___EditFieldHeight)
        {
            if (adjust)
            {
                ___windowHeight += 10f + ___EditFieldHeight;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Dialog_EditPrecept), nameof(Dialog_EditPrecept.InitialSize), MethodType.Getter)]
        public static void InitialSizePostfix()
        {
            adjust = false;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Dialog_EditPrecept), nameof(Dialog_EditPrecept.DoWindowContents))]
        public static IEnumerable<CodeInstruction> DoWindowContentsTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            bool firstFemaleFound = false;
            bool secondFemaleFound = false;
            Label? oldLabel = null;
            Label newLabel = ilg.DefineLabel();
            List<CodeInstruction> codes = instructions.ToList();
            int insertIndex = -1;
            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction code = codes[i];
                if (!firstFemaleFound && code.StoresField(typeof(Dialog_EditPrecept), "newPreceptNameFemale"))
                {
                    firstFemaleFound = true;
                }
                else if (firstFemaleFound && code.StoresField(typeof(Dialog_EditPrecept), "newPreceptNameFemale"))
                {
                    secondFemaleFound = true;
                }
                if (firstFemaleFound && !secondFemaleFound && code.Branches(out _))
                {
                    code.operand = newLabel;
                }
                else if (firstFemaleFound && secondFemaleFound && code.Branches(out oldLabel))
                {
                    insertIndex = i;
                    code.operand = newLabel;
                    break;
                }
            }
            if (insertIndex > -1)
            {
                //This is just loading the correct variables to pass to the helper
                List<CodeInstruction> stuff =
                [
                    new CodeInstruction(OpCodes.Ldarga_S, 1).WithLabels(newLabel),
                    CodeInstruction.LoadField(typeof(Dialog_EditPrecept), "EditFieldHeight"),
                    CodeInstruction.LoadField(typeof(Dialog_EditPrecept), "ValidSymbolRegex"),
                    new CodeInstruction(OpCodes.Ldloc, 1),
                    new CodeInstruction(OpCodes.Ldloc, 2),
                    new CodeInstruction(OpCodes.Ldloca_S, 3),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    CodeInstruction.LoadField(typeof(Dialog_EditPrecept), "precept", true),
                    CodeInstruction.Call(typeof(Dialog_EditPrecept_Patches), nameof(EnbyLabel)),
                    new CodeInstruction(OpCodes.Br, oldLabel),
                ];
                codes.InsertRange(insertIndex + 1, stuff);
            }
            else
            {
                LogUtil.Error("Unable to find entry point for DoWindowContentsTranspiler");
            }
            //This takes care of returning the adjusted instructions and the simple addition of saving the newly generated name to the enby field
            foreach (CodeInstruction code in codes)
            {
                yield return code;
                if (code.IsLdloc() && code.operand is LocalBuilder localB && localB.LocalIndex == 66)
                {
                    yield return CodeInstruction.StoreField(typeof(Dialog_EditPrecept_Patches), nameof(enbyLabel));
                    yield return code;
                }
            }
        }

        private static void EnbyLabel(ref Rect rect, float EditFieldHeight, Regex ValidSymbolRegex, float num, float num2, ref float num3, ref Precept precept)
        {
            Widgets.Label(new Rect(rect.x, num3, num2, EditFieldHeight), "LeaderTitle".Translate() + " (" + ((Gender)3).GetLabel() + ")");
            WorldComp_EnbyLeaderTitle comp = Find.World.GetComponent<WorldComp_EnbyLeaderTitle>();
            if (enbyLabel == string.Empty)
            {
                enbyLabel = comp.TitlesPerIdeo[precept.ideo];
            }
            enbyLabel = Widgets.TextField(new Rect(num, num3, num2, EditFieldHeight), enbyLabel, 99999, ValidSymbolRegex);
            num3 += EditFieldHeight + 10f;
            if (enbyLabel.Length > 32)
            {
                Messages.Message("PreceptNameTooLong".Translate(32), null, MessageTypeDefOf.RejectInput, historical: false);
                enbyLabel = enbyLabel.Substring(0, 32);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Dialog_EditPrecept), "ApplyChanges")]
        public static void Postfix(Precept ___precept)
        {
            WorldComp_EnbyLeaderTitle comp = Find.World.GetComponent<WorldComp_EnbyLeaderTitle>();
            comp.TitlesPerIdeo[___precept.ideo] = enbyLabel;
            enbyLabel = string.Empty;
        }
    }
}
