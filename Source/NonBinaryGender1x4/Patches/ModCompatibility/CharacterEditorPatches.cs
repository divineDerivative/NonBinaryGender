using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using HarmonyLib;
using UnityEngine;
using System.Reflection.Emit;
using System.Reflection;
using RimWorld;

namespace NonBinaryGender.Patches
{
    internal static class CharacterEditorPatches
    {
        private static Type CEditor;
        private static Type NameTool;
        private static Type BlockPerson;
        private static Type BlockSocial;
        private static Type DialogChoosePawn;

        internal static void PatchCE(this Harmony harmony)
        {
            //Need to grab these because everything in CE is internal/private
            CEditor = Type.GetType("CharacterEditor.CEditor, CharacterEditor");
            BlockPerson = AccessTools.Inner(AccessTools.Inner(CEditor, "EditorUI"), "BlockPerson");
            BlockSocial = AccessTools.Inner(AccessTools.Inner(CEditor, "EditorUI"), "BlockSocial");
            NameTool = Type.GetType("CharacterEditor.NameTool, CharacterEditor");
            DialogChoosePawn = Type.GetType("CharacterEditor.DialogChoosePawn, CharacterEditor");

            harmony.Patch(AccessTools.Method(BlockPerson, "DrawMainIcons"), transpiler: new HarmonyMethod(typeof(CharacterEditorPatches).GetMethod(nameof(GenderButtonTranspiler))));
            harmony.Patch(AccessTools.Method(Type.GetType("CharacterEditor.HeadTool, CharacterEditor"), "TestHead"), transpiler: new HarmonyMethod(typeof(CharacterEditorPatches).GetMethod(nameof(TestHeadTranspiler))));
            harmony.Patch(AccessTools.Method(BlockSocial, "DrawRelations"), transpiler: new HarmonyMethod(typeof(CharacterEditorPatches).GetMethod(nameof(DrawRelationsTranspiler))));
            harmony.Patch(AccessTools.Method(BlockSocial, "AOnGenderChange"), prefix: new HarmonyMethod(typeof(CharacterEditorPatches).GetMethod(nameof(AOnGenderChangePrefix))));
            harmony.Patch(AccessTools.Method(BlockSocial, "AOnGenderChange2"), prefix: new HarmonyMethod(typeof(CharacterEditorPatches).GetMethod(nameof(AOnGenderChange2Prefix))));
            harmony.Patch(AccessTools.Method(BlockSocial, "AOnGenderChange3"), prefix: new HarmonyMethod(typeof(CharacterEditorPatches).GetMethod(nameof(AOnGenderChange3Prefix))));
            harmony.Patch(AccessTools.Method(BlockSocial, "AOnGenderChange4"), prefix: new HarmonyMethod(typeof(CharacterEditorPatches).GetMethod(nameof(AOnGenderChange4Prefix))));
            harmony.Patch(AccessTools.Method(BlockSocial, "GetLabelSelectedPawn"), postfix: new HarmonyMethod(typeof(CharacterEditorPatches).GetMethod(nameof(GetLabelSelectedPawnPostfix))));
            harmony.Patch(AccessTools.Method(BlockSocial, "GetLabelSelectedPawn2"), postfix: new HarmonyMethod(typeof(CharacterEditorPatches).GetMethod(nameof(GetLabelSelectedPawn2Postfix))));
            harmony.Patch(AccessTools.Method(BlockSocial, "GetLabelSelectedPawn3"), postfix: new HarmonyMethod(typeof(CharacterEditorPatches).GetMethod(nameof(GetLabelSelectedPawn3Postfix))));
            harmony.Patch(AccessTools.Method(BlockSocial, "GetLabelSelectedPawn4"), postfix: new HarmonyMethod(typeof(CharacterEditorPatches).GetMethod(nameof(GetLabelSelectedPawn4Postfix))));
            harmony.Patch(AccessTools.Method(DialogChoosePawn, "DrawTitle"), transpiler: new HarmonyMethod(typeof(CharacterEditorPatches).GetMethod(nameof(DrawTitleTranspiler))));
            Type RelationTool = Type.GetType("CharacterEditor.RelationTool, CharacterEditor");
            harmony.Patch(AccessTools.Method(RelationTool, "RelationLabelDirect"), postfix: new HarmonyMethod(typeof(CharacterEditorPatches).GetMethod(nameof(RelationLabelDirectPostfix))));
            harmony.Patch(AccessTools.Method(RelationTool, "RelationLabelIndirect"), postfix: new HarmonyMethod(typeof(CharacterEditorPatches).GetMethod(nameof(RelationLabelIndirectPostfix))));
        }

        //This makes the gender change to non-binary if the female icon is clicked, and change to male/none if the non-binary icon is clicked
        public static IEnumerable<CodeInstruction> GenderButtonTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            //These labels are named for what they do now, not what they used to do, where the use has changed
            Label doneWithGender = ilg.DefineLabel(); //The start of the section after all the gender stuff is done
            Label? noneBlock = new Label(); //The start of the section that checks if gender == none
            Label? afterEnbyButton = new Label(); //The start of the section after the enby icon is drawn and/or clicked
            Label afterFemaleButton = ilg.DefineLabel(); //The start of the section after the female icon is drawn and/or clicked
            Label enbyBlock = ilg.DefineLabel(); //The start of the section that checks if gender == enby
            Label pawnIsNull = ilg.DefineLabel(); //The place we jump to if the pawn is null
            Label storeEnbyIf = ilg.DefineLabel(); //The place we store the value of (API.Pawn != null && API.Pawn.gender.IsEnby())
            LocalBuilder enbyIf = ilg.DeclareLocal(typeof(bool)); //The local used to store the value of (API.Pawn != null && API.Pawn.gender.IsEnby())
            LocalBuilder wasButtonPressed = ilg.DeclareLocal(typeof(bool)); //The local used to store the value of the button being clicked

            MethodInfo API = AccessTools.PropertyGetter(CEditor, "API");
            MethodInfo getPawn = AccessTools.PropertyGetter(CEditor, "Pawn");
            MethodInfo ButtonImage = AccessTools.Method(typeof(Widgets), "ButtonImage", new Type[] { typeof(Rect), typeof(Texture2D), typeof(bool) });

            List<CodeInstruction> codes = instructions.ToList();

            //This is to find the spot to jump to after the gender stuff is done and add a label that I can reference
            //Copying the label didn't work because, contrary to what the decompiler displays, instructions can have multiple labels, and this one did
            //We start at "bnoclothes" and go backwards until we find ldarg 0
            bool bnoclothes = false;
            for (int i = codes.Count - 1; i > 0; i--)
            {
                CodeInstruction code = codes[i];
                if (code.LoadsConstant("bnoclothes"))
                {
                    bnoclothes = true;
                }
                if (bnoclothes && code.IsLdarg(0))
                {
                    //Add my label, then stop
                    code.labels.Add(doneWithGender);
                    break;
                }
            }

            //This is to find where the original jumps to when (pawn != null & pawn.gender == Gender.Female) is false
            bool femaleCheck = false;
            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction code = codes[i];
                CodeInstruction nextCode = codes[i + 1];
                //Find gender == 2 (which is what Gender.Female resolves to)
                if (code.LoadsField(AccessTools.Field(typeof(Pawn), nameof(Pawn.gender))) && nextCode.LoadsConstant(2))
                {
                    femaleCheck = true;
                }
                //Then find the next branch and save the label
                if (femaleCheck && code.Branches(out noneBlock))
                {
                    //Replace it with a jump to the new section
                    code.operand = enbyBlock;
                    break;
                }
            }

            //This is where I insert my block
            bool bfemaleFound = false;
            bool startInsert = false;
            foreach (CodeInstruction code in codes)
            {

                //Find "bfemale"
                if (code.LoadsConstant("bfemale"))
                {
                    bfemaleFound = true;
                }
                //Then find the next branch and remember the label
                //This is the jump to the else after if (pawn != null && pawn.gender == Gender.Female)
                if (bfemaleFound && code.Branches(out afterEnbyButton))
                {
                    //Replace it with a new label
                    code.operand = afterFemaleButton;
                    bfemaleFound = false;
                    //Signal that the new code should be added on the next iteration
                    startInsert = true;
                }
                yield return code;

                if (startInsert)
                {
                    //This makes clicking on the female icon change the gender to non-binary
                    //API.Pawn.SetPawnGender(3)
                    yield return new CodeInstruction(OpCodes.Call, API);
                    yield return new CodeInstruction(OpCodes.Callvirt, getPawn);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_3);
                    yield return CodeInstruction.Call(NameTool, "SetPawnGender");

                    //I think the nop is just to mark the end of the nested if block
                    //Give this the label we grabbed earlier
                    yield return new CodeInstruction(OpCodes.Nop).WithLabels(afterFemaleButton);
                    //And jump to after the gender section
                    yield return new CodeInstruction(OpCodes.Br, doneWithGender);

                    //if (API.Pawn != null && API.Pawn.gender.IsEnby())
                    //The value of this if is stored as a local which I generated above

                    //API.Pawn != null
                    //This is where we jump to if (pawn != null && pawn.gender == Gender.Female) is false, so give it that label
                    yield return new CodeInstruction(OpCodes.Call, API).WithLabels(enbyBlock);
                    yield return new CodeInstruction(OpCodes.Callvirt, getPawn);
                    //If it's false/null then jump past the next section
                    yield return new CodeInstruction(OpCodes.Brfalse, pawnIsNull);

                    //API.Pawn.gender.IsEnby()
                    yield return new CodeInstruction(OpCodes.Call, API);
                    yield return new CodeInstruction(OpCodes.Callvirt, getPawn);
                    yield return CodeInstruction.LoadField(typeof(Pawn), nameof(Pawn.gender));
                    yield return CodeInstruction.Call(typeof(EnbyUtility), nameof(EnbyUtility.IsEnby), new Type[] { typeof(Gender) });
                    //Jump to storing the value with the result of IsEnby still on the stack
                    yield return new CodeInstruction(OpCodes.Br_S, storeEnbyIf);

                    //Only gets here if the pawn was null, so load a 0 before moving on
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0).WithLabels(pawnIsNull);

                    //Store the value of the if statement
                    yield return new CodeInstruction(OpCodes.Stloc, enbyIf).WithLabels(storeEnbyIf);
                    //Then load the value back
                    yield return new CodeInstruction(OpCodes.Ldloc, enbyIf);
                    //And if false, jump to the if (pawn5 != null && pawn5.gender == Gender.None) section, using a label we grabbed earlier
                    yield return new CodeInstruction(OpCodes.Brfalse, noneBlock);

                    // if (Widgets.ButtonImage(new Rect(x + 132, 259f, 28f, 28f), ContentFinder<Texture2D>.Get(EnbyUtility.NonBinaryIcon)))

                    //Not really sure what the nop is for, but both the original and the decompiled version of my translation had this, so shrug
                    yield return new CodeInstruction(OpCodes.Nop);
                    //(float)x+132, 259f, 28f, 28f
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.LoadField(BlockPerson, "x");
                    yield return new CodeInstruction(OpCodes.Ldc_I4, 132);
                    yield return new CodeInstruction(OpCodes.Add);
                    yield return new CodeInstruction(OpCodes.Conv_R4);
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 259f);
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 28f);
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 28f);
                    //new Rect(values loaded above)
                    yield return new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(Rect), new Type[] { typeof(float), typeof(float), typeof(float), typeof(float) }));
                    yield return CodeInstruction.LoadField(typeof(EnbyUtility), "NonBinaryIcon");
                    yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                    //Widgets.ButtonImage(above Rect, EnbyUtility.NonBinaryIcon)
                    yield return new CodeInstruction(OpCodes.Call, ButtonImage);
                    //Store whether the icon was clicked or not
                    yield return new CodeInstruction(OpCodes.Stloc, wasButtonPressed);
                    //Load it back
                    yield return new CodeInstruction(OpCodes.Ldloc, wasButtonPressed);
                    //If it wasn't clicked, jump to where the female section used to jump to
                    yield return new CodeInstruction(OpCodes.Brfalse_S, afterEnbyButton);
                    startInsert = false;
                    //If it is clicked, we pick up where the original left off
                }
            }
        }

        //This allows heads whose texture names start with male or female to be added to the head type list for enby pawns
        public static IEnumerable<CodeInstruction> TestHeadTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            Label? trueLabel = new Label();
            Label? storeLabel = new Label();

            List<CodeInstruction> codes = instructions.ToList();

            bool firstGender = false;
            bool secondGender = false;
            //This is to change flag = (pawn.gender == Gender.Female && flag2) || (pawn.gender == Gender.Male && flag3);
            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction code = codes[i];
                CodeInstruction nextCode = codes[i + 1];
                //This finds pawn.gender == Gender.Female
                if (code.LoadsField(InfoHelper.genderField) && nextCode.LoadsConstant(2))
                {
                    firstGender = true;
                }
                //Then at the next branch we grab the operand
                //This jumps to a single instruction that loads a true value right before that value is stored to flag
                if (firstGender && code.Branches(out trueLabel))
                {
                    firstGender = false;
                }
                //This finds pawn.gender == Gender.Male
                if (code.LoadsField(InfoHelper.genderField) && nextCode.LoadsConstant(1))
                {
                    secondGender = true;
                }
                //Then at the next branch we grab the operand
                //This jumps straight to storing flag with the result of this section still on the stack
                if (secondGender && code.Branches(out storeLabel))
                {
                    break;
                }
            }

            foreach (CodeInstruction code in codes)
            {
                //Now we find the end of the male section that jumps to storing the value of flag
                if (code.OperandIs(storeLabel))
                {
                    //And instead have it jump to the same place the female section jumps if its true
                    yield return new CodeInstruction(OpCodes.Brtrue_S, trueLabel);
                    //Then we simply add pawn.IsEnby
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.Call(typeof(EnbyUtility), nameof(EnbyUtility.IsEnby), new Type[] { typeof(Pawn) });
                }
                //And continue where we left of, the jump to storing the value of flag, with the result of our section still on the stack
                yield return code;
            }
        }

        //This shows the correct gender icon if pawn is non-binary in the social tab
        public static IEnumerable<CodeInstruction> DrawRelationsTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            //It's just the same thing four times but with different field names, so we'll just iterate through lists
            List<FieldInfo> fields = new List<FieldInfo>
            {
                AccessTools.Field(BlockSocial, "selectedGender"),
                AccessTools.Field(BlockSocial, "selectedGender2"),
                AccessTools.Field(BlockSocial, "selectedGender3"),
                AccessTools.Field(BlockSocial, "selectedGender4"),
            };
            List<Label> firstLabel = new List<Label>
            {
                ilg.DefineLabel(),
                ilg.DefineLabel(),
                ilg.DefineLabel(),
                ilg.DefineLabel()
            };
            List<Label> secondLabel = new List<Label>
            {
                ilg.DefineLabel(),
                ilg.DefineLabel(),
                ilg.DefineLabel(),
                ilg.DefineLabel()
            };
            List<bool> bools = new List<bool>
            {
                false, false, false, false
            };

            int index = 0;
            List<CodeInstruction> codes = instructions.ToList();
            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction code = codes[i];
                CodeInstruction nextCode = (i < codes.Count - 1) ? codes[i + 1] : codes[i];

                if (bools[index] && code.LoadsConstant("bmale") && nextCode.Branches(out _))
                {
                    code.labels.Add(firstLabel[index]);
                    nextCode.labels.Add(secondLabel[index]);
                    bools[index] = false;
                    index++;
                    //Reset once we've done them all to avoid the index out of range error
                    if (index > 3)
                    {
                        index = 0;
                    }
                }

                yield return code;

                if (code.LoadsField(fields[index]) && nextCode.LoadsConstant(2))
                {
                    bools[index] = true;
                }
                if (bools[index] && code.Branches(out _))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return HelperExtensions.LoadField(fields[index]);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                    yield return new CodeInstruction(OpCodes.Beq_S, firstLabel[index]);
                    yield return new CodeInstruction(OpCodes.Ldstr, "UI/Gender/NonBinary");
                    yield return new CodeInstruction(OpCodes.Br_S, secondLabel[index]);
                }
            }
        }

        //These allow non-binary to be selected as a gender
        public static bool AOnGenderChangePrefix(Type __instance, ref Gender ___selectedGender)
        {
            ___selectedGender = ___selectedGender == Gender.Female ? (Gender)3 : ___selectedGender == Gender.Male ? Gender.Female : Gender.Male;
            PropertyInfo SelectedPawn = AccessTools.Property(BlockSocial, "SelectedPawn");
            SelectedPawn.SetValue(__instance, null);
            return false;
        }

        public static bool AOnGenderChange2Prefix(Type __instance, ref Gender ___selectedGender2)
        {
            ___selectedGender2 = ___selectedGender2 == Gender.Female ? (Gender)3 : ___selectedGender2 == Gender.Male ? Gender.Female : Gender.Male;
            PropertyInfo SelectedPawn = AccessTools.Property(BlockSocial, "SelectedPawn2");
            SelectedPawn.SetValue(__instance, null);
            return false;
        }

        public static bool AOnGenderChange3Prefix(Type __instance, ref Gender ___selectedGender3)
        {
            ___selectedGender3 = ___selectedGender3 == Gender.Female ? (Gender)3 : ___selectedGender3 == Gender.Male ? Gender.Female : Gender.Male;
            PropertyInfo SelectedPawn = AccessTools.Property(BlockSocial, "SelectedPawn3");
            SelectedPawn.SetValue(__instance, null);
            return false;
        }

        public static bool AOnGenderChange4Prefix(Type __instance, ref Gender ___selectedGender4)
        {
            ___selectedGender4 = ___selectedGender4 == Gender.Female ? (Gender)3 : ___selectedGender4 == Gender.Male ? Gender.Female : Gender.Male;
            PropertyInfo SelectedPawn = AccessTools.Property(BlockSocial, "SelectedPawn4");
            SelectedPawn.SetValue(__instance, null);
            return false;
        }

        //These change the label for the relationship
        public static void GetLabelSelectedPawnPostfix(PawnRelationDef r, Gender ___selectedGender, ref string __result)
        {
            if (___selectedGender.IsEnby())
            {
                if (r.GetModExtension<EnbyInfo>() is EnbyInfo extension && extension.labelEnby != null)
                {
                    __result = extension.labelEnby;
                }
            }
        }

        public static void GetLabelSelectedPawn2Postfix(PawnRelationDef r, Gender ___selectedGender2, ref string __result)
        {
            if (___selectedGender2.IsEnby())
            {
                if (r.GetModExtension<EnbyInfo>() is EnbyInfo extension && extension.labelEnby != null)
                {
                    __result = extension.labelEnby + __result.Substring(__result.IndexOf("("));
                }
            }
        }

        public static void GetLabelSelectedPawn3Postfix(PawnRelationDef r, Gender ___selectedGender3, ref string __result)
        {
            if (___selectedGender3.IsEnby())
            {
                if (r.GetModExtension<EnbyInfo>() is EnbyInfo extension && extension.labelEnby != null)
                {
                    __result = extension.labelEnby + __result.Substring(__result.IndexOf("("));
                }
            }
        }

        public static void GetLabelSelectedPawn4Postfix(PawnRelationDef r, Gender ___selectedGender4, ref string __result)
        {
            if (___selectedGender4.IsEnby())
            {
                if (r.GetModExtension<EnbyInfo>() is EnbyInfo extension && extension.labelEnby != null)
                {
                    __result = extension.labelEnby + __result.Substring(__result.IndexOf("("));
                }
            }
        }

        //This shows the correct gender icon on the list of pawns to select from
        public static IEnumerable<CodeInstruction> DrawTitleTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            FieldInfo choosenGender = AccessTools.Field(DialogChoosePawn, "choosenGender");
            Label firstLabel = ilg.DefineLabel();
            Label secondLabel = ilg.DefineLabel();

            List<CodeInstruction> codes = instructions.ToList();
            bool insert = false;
            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction code = codes[i];
                CodeInstruction nextCode = codes[Math.Min(i + 1, codes.Count - 1)];

                if (insert && code.LoadsConstant("bfemale") && nextCode.Branches(out _))
                {
                    code.labels.Add(firstLabel);
                    nextCode.labels.Add(secondLabel);
                    insert = false;
                }

                yield return code;

                if (code.LoadsField(choosenGender) && nextCode.LoadsConstant(1))
                {
                    insert = true;
                }
                if (insert && code.Branches(out _))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.LoadField(DialogChoosePawn, "choosenGender");
                    yield return new CodeInstruction(OpCodes.Ldc_I4_2);
                    yield return new CodeInstruction(OpCodes.Beq_S, firstLabel);
                    yield return new CodeInstruction(OpCodes.Ldstr, "UI/Gender/NonBinary");
                    yield return new CodeInstruction(OpCodes.Br_S, secondLabel);
                }
            }
        }

        //These give the correct label to the existing relations
        public static void RelationLabelDirectPostfix(ref string __result, DirectPawnRelation dr)
        {
            if (dr.otherPawn.IsEnby())
            {
                if (dr.def.GetModExtension<EnbyInfo>() is EnbyInfo extension && extension.labelEnby != null)
                {
                    __result = extension.labelEnby + __result.Substring(__result.IndexOf(" "));
                }
            }
        }

        public static void RelationLabelIndirectPostfix(ref string __result, PawnRelationDef prd, Pawn otherPawn)
        {
            if (otherPawn.IsEnby())
            {
                if (prd.GetModExtension<EnbyInfo>() is EnbyInfo extension && extension.labelEnby != null)
                {
                    __result = extension.labelEnby + __result.Substring(__result.IndexOf(" "));
                }
            }
        }
    }
}
