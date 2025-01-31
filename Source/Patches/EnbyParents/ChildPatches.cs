using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace NonBinaryGender.Patches
{
    [HarmonyPatch]
    public static class ChildPatches
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PawnRelationWorker_Child), nameof(PawnRelationWorker_Child.GenerationChance))]
        [HarmonyBefore(["eth0net.AnimalHemogen"])]
        public static IEnumerable<CodeInstruction> GenerationChanceTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            Label? biotechLabel = new();
            Label myLabel = ilg.DefineLabel();
            List<CodeInstruction> codes = instructions.ToList();
            for (int i = 0; i < instructions.Count(); i++)
            {
                CodeInstruction code = codes[i];
                CodeInstruction nextCode = codes[i + 1];
                if (code.LoadsConstant(2) && nextCode.Branches(out biotechLabel))
                {
                    nextCode.operand = myLabel;
                    break;
                }
            }

            foreach (CodeInstruction code in instructions)
            {
                if (code.Calls(AccessTools.PropertyGetter(typeof(ModsConfig), nameof(ModsConfig.BiotechActive))))
                {
                    //Make the female section jump to the Biotech section
                    yield return new CodeInstruction(OpCodes.Br_S, biotechLabel);
                    //Check for non-binary
                    yield return new CodeInstruction(OpCodes.Ldarg_1).WithLabels(myLabel);
                    yield return HelperExtensions.LoadField(InfoHelper.genderField);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_3);
                    //Jump to Biotech section if not enby
                    yield return new CodeInstruction(OpCodes.Bne_Un, biotechLabel);
                    //Load generated, other, and request
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Ldarg_3);
                    yield return CodeInstruction.Call(typeof(ChildPatches), nameof(GenerationChanceHelper));
                    //Store result to num
                    yield return new CodeInstruction(OpCodes.Stloc_0);
                }
                yield return code;
            }
        }

        private static float GenerationChanceHelper(Pawn generated, Pawn other, PawnGenerationRequest request)
        {
            Pawn otherParent = other.GetMother() ?? other.GetFather();
            if (otherParent is null)
            {
                return ChildRelationUtility.ChanceOfBecomingChildOf(other, generated, null, null, request, null);
            }
            if (otherParent.gender == Gender.Male)
            {
                return ChildRelationUtility.ChanceOfBecomingChildOf(other, otherParent, generated, null, null, request);
            }
            return ChildRelationUtility.ChanceOfBecomingChildOf(other, generated, otherParent, null, request, null);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PawnRelationWorker_Child), nameof(PawnRelationWorker_Child.CreateRelation))]
        public static bool CreateRelationPrefix(Pawn generated, Pawn other, ref PawnGenerationRequest request)
        {
            if (generated.IsEnby())
            {
                other.SetParent(generated);
                //Using my method due to the difference in GetMother and GetFather between game versions
                Pawn mother = other.GetParent(Gender.Female);
                Pawn father = other.GetParent(Gender.Male);
                //ResolveMyName gets called in ResolveOtherParent if another parent exists, so we need to call it here if they don't
                if (mother == null && father == null)
                {
                    ResolveMyName.Invoke(null, [request, other, mother ?? father]);
                }
                else
                {
                    if (mother is not null)
                    {
                        ResolveOtherParent(mother, generated, other, ref request);
                    }
                    if (father is not null)
                    {
                        ResolveOtherParent(father, generated, other, ref request);
                    }
                }
                return false;
            }
            return true;
        }

        static MethodInfo ResolveMyName = AccessTools.Method(typeof(PawnRelationWorker_Child), "ResolveMyName");
        private static void ResolveOtherParent(Pawn existingParent, Pawn generatedParent, Pawn child, ref PawnGenerationRequest request)
        {
            ResolveMyName.Invoke(null, [request, child, existingParent]);
            //Next is an orientation check. It should probably just be not straight/asexual yeah?
            if (!existingParent.story.traits.HasTrait(TraitDefOf.Gay) && !existingParent.story.traits.HasTrait(TraitDefOf.Bisexual))
            {
                generatedParent.relations.AddDirectRelation(PawnRelationDefOf.ExLover, existingParent);
            }
            else if (Rand.Value < 0.85f && !LovePartnerRelationUtility.HasAnyLovePartner(existingParent))
            {
                generatedParent.relations.AddDirectRelation(PawnRelationDefOf.Spouse, existingParent);
                SpouseRelationUtility.ResolveNameForSpouseOnGeneration(ref request, generatedParent);
            }
            else
            {
                LovePartnerRelationUtility.GiveRandomExLoverOrExSpouseRelation(generatedParent, existingParent);
            }
        }
    }
}
