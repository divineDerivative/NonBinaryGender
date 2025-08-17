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
        private static bool isWindowOpen = false;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Dialog_EditPrecept), nameof(Dialog_EditPrecept.DoWindowContents))]
        // Sets isWindowOpen to true if the precept is a leader role, so we only mess with stuff for that specific precept
        public static void DoWindowContentsPrefix(Precept ___precept)
        {
            if (___precept.def.leaderRole)
            {
                isWindowOpen = true;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Dialog_EditPrecept), "UpdateWindowHeight")]
        // We only want to adjust the window height if it's a leader role precept
        public static void UpdateWindowHeightPostfix(Precept ___precept)
        {
            if (___precept.def.leaderRole)
            {
                adjust = true;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Dialog_EditPrecept), nameof(Dialog_EditPrecept.InitialSize), MethodType.Getter)]
        // Increases the initial window height if adjust is true. This is used in various places to determine the size of the window in conjunction with other variables in Dialog_EditPrecept
        public static void InitialSizePrefix(ref float ___windowHeight, float ___EditFieldHeight)
        {
            if (adjust)
            {
                ___windowHeight += 10f + ___EditFieldHeight;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Dialog_EditPrecept), nameof(Dialog_EditPrecept.InitialSize), MethodType.Getter)]
        // Resets adjust to false, so we're only adjusting the window height when we need to
        public static void InitialSizePostfix()
        {
            adjust = false;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Dialog_EditPrecept), nameof(Dialog_EditPrecept.DoWindowContents))]
        //Adds an extra text field for the enby leader title and saves the result to enbyLabel
        public static IEnumerable<CodeInstruction> DoWindowContentsTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            CodeMatcher matcher = new(instructions, ilg);

            // Find first store to newPreceptNameFemale
            matcher.MatchStartForward(CodeMatch.StoresField(AccessTools.Field(typeof(Dialog_EditPrecept), "newPreceptNameFemale")));

            // Find branch after first store and redirect to new label
            matcher.MatchStartForward(CodeMatch.Branches());
            Label newLabel = ilg.DefineLabel();
            matcher.Operand = newLabel;

            // Find second store to newPreceptNameFemale
            matcher.MatchStartForward(CodeMatch.StoresField(AccessTools.Field(typeof(Dialog_EditPrecept), "newPreceptNameFemale")));

            // Find branch after second store and redirect to new label, save old label
            matcher.MatchStartForward(CodeMatch.Branches());
            Label oldLabel = (Label)matcher.Operand;
            matcher.Operand = newLabel;

            // Insert call to EnbyLabel after the second branch
            matcher.Advance(1);
            matcher.InsertAndAdvance(
                //Set this as the jump to point for the female sections
                new CodeInstruction(OpCodes.Ldarga_S, 1).WithLabels(newLabel),
                //Load necessary fields and locals for the EnbyLabel method
                CodeInstruction.LoadField(typeof(Dialog_EditPrecept), "EditFieldHeight"),
                CodeInstruction.LoadField(typeof(Dialog_EditPrecept), "ValidSymbolRegex"),
                new CodeInstruction(OpCodes.Ldloc, 1),
                new CodeInstruction(OpCodes.Ldloc, 2),
                new CodeInstruction(OpCodes.Ldloca_S, 3),
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.LoadField(typeof(Dialog_EditPrecept), "precept", true),
                CodeInstruction.Call(typeof(Dialog_EditPrecept_Patches), nameof(EnbyLabel)),
                // After finishing, jump to the old label to continue normal execution
                new CodeInstruction(OpCodes.Br, oldLabel));
            // When a new name is being generated, store it in enbyLabel as well
            matcher.MatchEndForward(CodeMatch.Calls(AccessTools.Method(typeof(Precept), nameof(Precept.GenerateNewName))));
            //Insert after the call to GenerateNewName
            matcher.Advance(1);
            matcher.Insert(new CodeInstruction(OpCodes.Dup),
                CodeInstruction.StoreField(typeof(Dialog_EditPrecept_Patches), "enbyLabel"));

            return matcher.InstructionEnumeration();
        }

        //Helper method to draw the enby leader title text field and handle input
        private static void EnbyLabel(ref Rect rect, float EditFieldHeight, Regex ValidSymbolRegex, float num, float num2, ref float num3, ref Precept precept)
        {
            Widgets.Label(new(rect.x, num3, num2, EditFieldHeight), "LeaderTitle".Translate() + " (" + ((Gender)3).GetLabel() + ")");
            WorldComp_EnbyLeaderTitle comp = Find.World.GetComponent<WorldComp_EnbyLeaderTitle>();

            if (enbyLabel == string.Empty)
            {
                enbyLabel = comp.TitlesPerIdeo[precept.ideo];
            }

            enbyLabel = Widgets.TextField(new(num, num3, num2, EditFieldHeight), enbyLabel, 99999, ValidSymbolRegex);
            num3 += EditFieldHeight + 10f;

            if (enbyLabel.Length > 32)
            {
                Messages.Message("PreceptNameTooLong".Translate(32), null, MessageTypeDefOf.RejectInput, false);
                enbyLabel = enbyLabel.Substring(0, 32);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Dialog_EditPrecept), "ApplyChanges")]
        //Saves the enby leader title to the world component when changes are applied
        public static void ApplyChangesPrefix(Precept ___precept)
        {
            if (___precept.def.leaderRole)
            {
                WorldComp_EnbyLeaderTitle comp = Find.World.GetComponent<WorldComp_EnbyLeaderTitle>();
                comp.TitlesPerIdeo[___precept.ideo] = enbyLabel;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Window), "Close")]
        //Resets enbyLabel if Dialog_EditPrecept is the window being closed
        public static void ClosePostfix()
        {
            if (isWindowOpen)
            {
                enbyLabel = string.Empty;
                isWindowOpen = false;
            }
        }
    }
}
