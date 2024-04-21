using AlienRace;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace NonBinaryGender
{
    public static class HARPatches
    {
        public static bool IsAlien(this Pawn pawn)
        {
            return pawn.def is ThingDef_AlienRace;
        }

        public static void PatchHAR(this Harmony harmony)
        {
            harmony.Patch(typeof(HarmonyPatches).GetMethod(nameof(HarmonyPatches.GenerateRandomAgePrefix)), transpiler: new HarmonyMethod(typeof(HARPatches).GetMethod(nameof(GenerateRandomAgePrefixTranspiler))));
        }

        //Decide what, if any, adjustments should be made to head offsets based on HAR settings
        //This is a mess and it probably only works in the viera use case because that's what I used to figure it out
        //There's too many different things involved in HAR calculating the offsets, and I would have to make assumptions about why certain offsets were used
        //I'm going to leave this for now but likely I'll just fix races on a case by case basis with xml patches
        public static Vector2 HeadOffsetHelper(Pawn pawn, Gender bodyGender, Vector2 previous)
        {
            //I'm only going to use the basic offsets, anything directional or conditional will be left alone for now
            //Maybe I should check for body type offsets?
            //HAR takes the supplied offsets, does stuff based on settings in the life stage, then assigns the new offset to the life stage
            //So we only look at the life stage stuff
            //((But where do the body type offsets go?) ((They're inside head(Female)OffsetSpecific))
            if (pawn.ageTracker.CurLifeStageRace is LifeStageAgeAlien ageAlien)
            {
                //For the viera use case, it's the draw size that seems to matter
                //So I think we check if the draw sizes are the same
                Vector2 maleSize = ageAlien.customDrawSize;
                Vector2 femaleSize = ageAlien.customFemaleDrawSize;
                Vector2 maleOffset = ageAlien.headOffset;
                Vector2 femaleOffset = ageAlien.headFemaleOffset;
                //If they're the same, then we can do the below
                if (maleSize == femaleSize)
                {
                    //So we actually only care about applying the female offset, because the male one would have been applied by default
                    if (bodyGender == Gender.Female)
                    {
                        //If these are the same the result is zero so we're good
                        return femaleOffset - maleOffset;
                    }
                }
                //If they're not the same, the male size is what will be in effect.
                //A female head on a female body gets a head offset from the body and from the race. We're assuming the race adjustment will be to account for the different draw sizes
                //A male head on a female body gets the head offset for a female body, and the race offset for a male. We've already swapped the body def offset, here we're only concerned with the race offset. So, the body will be the male size and should get the male offset, which it already has
                else
                {
                    //So, male head/female body gets left alone
                    if (bodyGender == Gender.Male)
                    {
                        //A male head on a male body gets a head offset from the body and from the race. Again assuming the race adjustment is to account for the size difference
                        //A female head on a male body gets the head offset for a male body and the race offset for a male. We've already swapped the body def offset, we only care about the race offset.
                        //The body should be the male size and should get the male offset, which it already has?
                        //Alright, we're just going to have to assume that the race author is following best practices and their head graphics are drawn over the vanilla texture
                        //Which means the gender specific offset is to make their custom head fit on the differently sized bodies
                        //So it doesn't matter what gender the head is I think, just what gender the body is
                        //So for a male body, we only want to apply the race's male offset
                        //Which means we reverse the swap done in the main method
                        return Vector2.zero - previous;
                    }
                }
            }
            return Vector2.zero;
        }

        //This happens after gender has been assigned normally, but HAR uses it to reroll based on race settings, so we need to insert our stuff here too
        public static IEnumerable<CodeInstruction> GenerateRandomAgePrefixTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            MethodInfo HasValue = AccessTools.PropertyGetter(typeof(float?), "HasValue");
            Label? label = new Label();
            Label myLabel = ilg.DefineLabel();
            Label jumpPoint = ilg.DefineLabel();

            List<CodeInstruction> codes = instructions.ToList();

            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction code = codes[i];
                //We want the result of our new section to jump here, so add a label we can reference
                if (code.StoresField(InfoHelper.genderField))
                {
                    code.labels.Add(jumpPoint);
                    break;
                }
            }

            bool start = false;
            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction code = codes[i];
                yield return code;

                //We want to jump to the new section if maleGenderProbability is not null, so swap the label
                if (code.Calls(HasValue) && codes[i + 1].Branches(out label))
                {
                    codes[i + 1].operand = myLabel;
                    start = true;
                }
                //We insert our section after the ret
                if (start && code.opcode == OpCodes.Ret)
                {
                    //if (Rand.Value >= NonBinaryGenderMod.settings.enbyChance)
                    yield return new CodeInstruction(OpCodes.Call, InfoHelper.RandValue).WithLabels(myLabel);
                    yield return CodeInstruction.LoadField(typeof(NonBinaryGenderMod), nameof(NonBinaryGenderMod.settings));
                    yield return CodeInstruction.LoadField(typeof(Settings), nameof(Settings.enbyChance));
                    //Go to the old section if above is true
                    yield return new CodeInstruction(OpCodes.Bge_S, label);
                    //Put pawn and 3 on the stack
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_3);
                    //Then jump to storing the value to pawn.gender
                    yield return new CodeInstruction(OpCodes.Br_S, jumpPoint);
                    start = false;
                }
            }

        }
    }
}