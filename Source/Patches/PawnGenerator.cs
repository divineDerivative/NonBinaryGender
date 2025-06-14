using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace NonBinaryGender.Patches
{
    [HarmonyPatch]
    public static class PawnGenerator_Patches
    {
        //This makes them bisexual unless they're already asexual. Only works on new pawn generation
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PawnGenerator), "GenerateTraits")]
        public static void GenerateTraitsPatch(Pawn pawn)
        {
            if (pawn.IsEnby())
            {
                if (pawn.story.traits.HasTrait(TraitDefOf.Gay))
                {
                    pawn.story.traits.RemoveTrait(pawn.story.traits.GetTrait(TraitDefOf.Gay));
                }
                if (!pawn.story.traits.HasTrait(TraitDefOf.Asexual) && !pawn.story.traits.HasTrait(TraitDefOf.Bisexual))
                {
                    pawn.story.traits.GainTrait(new Trait(TraitDefOf.Bisexual));
                }
            }
        }

        //This inserts a check against the enby chance when choosing a new pawn's gender
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PawnGenerator), "TryGenerateNewPawnInternal")]
        public static IEnumerable<CodeInstruction> TryGenerateNewPawnInternalTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            MethodInfo RaceProps = typeof(Pawn).GetProperty("RaceProps").GetGetMethod();
            MethodInfo Animal = typeof(RaceProperties).GetProperty("Animal").GetGetMethod();
            Label startMyStuff = generator.DefineLabel();
            bool done = false;

            CodeMatcher matcher = new CodeMatcher(instructions, generator)
                     .MatchEndForward(
            CodeMatch.StoresField(InfoHelper.genderField),
            CodeMatch.Branches()
        );
            Label doneWithGender = (Label)matcher.Operand;

#if !v1_4
            //Get the compiler generated type that stores the pawn
            Type compilerType = null;
            IEnumerable<Type> types = AccessTools.InnerTypes(typeof(PawnGenerator));
            foreach (Type innerType in types)
            {
                FieldInfo[] fields = innerType.GetFields();
                if (fields.Count() == 1 && fields[0].FieldType == typeof(Pawn))
                {
                    MethodInfo[] methods = innerType.GetMethods(AccessTools.all);
                    if (methods.Any(x => x.Name.Contains("TryGenerateNewPawnInternal")))
                    {
                        compilerType = innerType;
                        break;
                    }
                }
            }
#endif
            //Look for Rand.Value being called, then insert our stuff before it
            matcher.MatchStartForward(CodeMatch.Calls(InfoHelper.RandValue))
                //Add our label so it jumps to here if the pawn is an animal or enby was not selected
                .CreateLabel(out Label notAnimalorEnby)
                .Insert(
                    //We check for animals because we don't want non-binary cows
                    //if (!pawn.RaceProps.Animal
                    new CodeInstruction(OpCodes.Ldloc_0).WithLabels(startMyStuff),
#if !v1_4
                    CodeInstruction.LoadField(compilerType, "pawn"),
#endif
                    new CodeInstruction(OpCodes.Callvirt, RaceProps),
                    new CodeInstruction(OpCodes.Callvirt, Animal),
                    new CodeInstruction(OpCodes.Brtrue, notAnimalorEnby),
                    //Check against the enby chance
                    //&& Rand.Value < NonBinaryGenderMod.settings.enbyChance)
                    new CodeInstruction(OpCodes.Call, InfoHelper.RandValue),
                    CodeInstruction.LoadField(typeof(NonBinaryGenderMod), nameof(NonBinaryGenderMod.settings)),
                    CodeInstruction.LoadField(typeof(Settings), nameof(Settings.enbyChance)),
                    new CodeInstruction(OpCodes.Bge_Un, notAnimalorEnby),
                    //pawn.gender = 3;
                    new CodeInstruction(OpCodes.Ldloc_0),
#if !v1_4
                    CodeInstruction.LoadField(compilerType, "pawn"),
#endif
                    new CodeInstruction(OpCodes.Ldc_I4_3),
                    new CodeInstruction(OpCodes.Stfld, InfoHelper.genderField),
                    new CodeInstruction(OpCodes.Br_S, doneWithGender)
                );
            return matcher.InstructionEnumeration();
        }

        //This changes the gender check after the 50% chance to get thin to be another 50% chance to get male or female, instead of defaulting to male
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GetBodyTypeFor))]
        public static IEnumerable<CodeInstruction> GetBodyTypeForTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            Label? oldLabel = new Label();
            Label newLabel = ilg.DefineLabel();
            Label otherLabel = ilg.DefineLabel();
            List<CodeInstruction> codes = instructions.ToList();

            bool randFound = false;
            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction code = codes[i];
                //Look for the call to Rand.Value
                if (code.Calls(InfoHelper.RandValue))
                {
                    randFound = true;
                }
                //Then at the next branch, grab the operand and replace it with our new one
                if (randFound && code.Branches(out oldLabel))
                {
                    code.operand = newLabel;
                    break;
                }
            }

            bool insert = false;
            foreach (CodeInstruction code in instructions)
            {
                //Look for the reference to thin and start the flag
                if (code.LoadsField(AccessTools.Field(typeof(BodyTypeDefOf), nameof(BodyTypeDefOf.Thin))))
                {
                    insert = true;
                }

                yield return code;
                //We want to make sure we insert after the ret
                if (insert && code.opcode == OpCodes.Ret)
                {
                    //Jump here if the thin roll failed
                    //if pawn.gender != 3
                    yield return new CodeInstruction(OpCodes.Ldarg_0).WithLabels(newLabel);
                    yield return CodeInstruction.LoadField(typeof(Pawn), nameof(Pawn.gender));
                    yield return new CodeInstruction(OpCodes.Ldc_I4_3);
                    //Jump to the regular gender check
                    yield return new CodeInstruction(OpCodes.Bne_Un_S, oldLabel);
                    //if Rand.Value >= 0.5f)
                    yield return new CodeInstruction(OpCodes.Call, InfoHelper.RandValue);
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 0.5f);
                    //Jump to the female return
                    yield return new CodeInstruction(OpCodes.Bge_Un_S, otherLabel);
                    //return BodyTypeDefOf.Male
                    yield return CodeInstruction.LoadField(typeof(BodyTypeDefOf), nameof(BodyTypeDefOf.Male));
                    yield return new CodeInstruction(OpCodes.Ret);
                    //return BodyTypeDefOf.Female
                    yield return CodeInstruction.LoadField(typeof(BodyTypeDefOf), nameof(BodyTypeDefOf.Female)).WithLabels(otherLabel);
                    yield return new CodeInstruction(OpCodes.Ret);
                    insert = false;
                }
            }
        }
    }
}