using DivineFramework;
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
    public static class ParentPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SpouseRelationUtility), nameof(SpouseRelationUtility.GetFirstSpouseOfOppositeGender))]
        //Used in the generation of parent relationships
        public static bool GetFirstSpouseOfOppositeGenderPrefix(Pawn pawn, ref Pawn __result)
        {
            if (pawn.IsEnby())
            {
                foreach (Pawn spouse in pawn.GetSpouses(true))
                {
                    //Only one parent should be non-binary
                    if (spouse.gender is Gender.Male or Gender.Female)
                    {
                        __result = spouse;
                        return false;
                    }
                }
            }
            return true;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PawnRelationWorker_Parent), nameof(PawnRelationWorker_Parent.GenerationChance))]
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
                    yield return new CodeInstruction(OpCodes.Ldarg_2).WithLabels(myLabel);
                    yield return HelperExtensions.LoadField(InfoHelper.genderField);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_3);
                    //Jump to Biotech section if not enby
                    yield return new CodeInstruction(OpCodes.Bne_Un, biotechLabel);
                    //Load generated, other, and request
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Ldarg_3);
                    yield return CodeInstruction.Call(typeof(ParentPatches), nameof(GenerationChanceHelper));
                    //Store result to num
                    yield return new CodeInstruction(OpCodes.Stloc_0);
                }
                yield return code;
            }
        }

        private static float GenerationChanceHelper(Pawn generated, Pawn other, PawnGenerationRequest request)
        {
            Pawn spouse = other.GetFirstSpouseOfOppositeGender();
            if (spouse is null)
            {
                return ChildRelationUtility.ChanceOfBecomingChildOf(generated, other, null, null, request, null);
            }
            if (spouse.gender == Gender.Male)
            {
                return ChildRelationUtility.ChanceOfBecomingChildOf(generated, spouse, other, null, null, request);
            }
            return ChildRelationUtility.ChanceOfBecomingChildOf(generated, other, spouse, null, request, null);
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(ChildRelationUtility), nameof(ChildRelationUtility.ChanceOfBecomingChildOf))]
        [HarmonyBefore(["eth0net.AnimalHemogen"])]
        public static IEnumerable<CodeInstruction> GenderCheckTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            //Replace != Gender.Male with == Gender.Female and != Gender.Female with == Gender.Male
            bool genderFound = false;
            bool firstGender = false;
            bool secondGender = false;
            foreach (CodeInstruction code in instructions)
            {
                //We want to do our stuff at every other gender check
                if (code.LoadsField(InfoHelper.genderField) || code.LoadsField(InfoHelper.genderField, true))
                {
                    genderFound = !genderFound;
                }

                if (genderFound)
                {
                    //Switch the gender
                    if (!firstGender && code.LoadsConstant(1))
                    {
                        code.opcode = OpCodes.Ldc_I4_2;
                    }
                    else if (!secondGender && code.LoadsConstant(2))
                    {
                        code.opcode = OpCodes.Ldc_I4_1;
                    }
                    //Change from equal to unequal
                    if (!firstGender && code.Branches(out _))
                    {
                        code.opcode = OpCodes.Bne_Un;
                        firstGender = true;
                    }
                    else if (!secondGender && code.Branches(out _))
                    {
                        code.opcode = OpCodes.Bne_Un;
                        secondGender = true;
                    }
                }
                yield return code;
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(ChildRelationUtility), nameof(ChildRelationUtility.ChanceOfBecomingChildOf))]
        public static IEnumerable<CodeInstruction> GetParentTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            //Replace the gender specific methods with GetParent using whatever the provided pawn's gender is
            foreach (CodeInstruction code in instructions)
            {
                if (code.Calls(typeof(ParentRelationUtility), "GetFather"))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return HelperExtensions.LoadField(InfoHelper.genderField);
                    yield return CodeInstruction.Call(typeof(EnbyUtility), nameof(EnbyUtility.GetParent));
                }
                else if (code.Calls(typeof(ParentRelationUtility), "GetMother"))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return HelperExtensions.LoadField(InfoHelper.genderField);
                    yield return CodeInstruction.Call(typeof(EnbyUtility), nameof(EnbyUtility.GetParent));
                }
                else
                {
                    yield return code;
                }
            }
        }

        static MethodInfo ResolveMyName = AccessTools.Method(typeof(PawnRelationWorker_Parent), "ResolveMyName");

        //The original will just do nothing if other is non-binary, so we do it ourselves
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PawnRelationWorker_Parent), nameof(PawnRelationWorker_Parent.CreateRelation))]
        public static bool CreateRelationPrefix(Pawn generated, Pawn other, ref PawnGenerationRequest request)
        {
            if (other.IsEnby())
            {
                generated.SetParent(other);
                Pawn spouse = other.GetFirstSpouseOfOppositeGender();
                if (spouse != null)
                {
                    if (spouse.gender == Gender.Male)
                    {
                        generated.SetFather(spouse);
                    }
                    if (spouse.gender == Gender.Female)
                    {
                        generated.SetMother(spouse);
                    }
                    ResolveMyName.Invoke(null, [request, generated]);
                }
                return false;
            }
            return true;
        }

        //Might need to patch ResolveMyName
        //Might need to patch TryToShareChildrenForGeneratedLovePartner
    }

    [HarmonyPatch]
    public static class SetParent_Patches
    {
        internal static bool skip = false;
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(ParentRelationUtility), nameof(ParentRelationUtility.SetMother));
            yield return AccessTools.Method(typeof(ParentRelationUtility), nameof(ParentRelationUtility.SetFather));
        }

        public static void Prefix()
        {
            skip = true;
        }

        public static void Postfix()
        {
            skip = false;
        }

        /// <summary>
        /// Sets <paramref name="newParent"/> as the non-binary parent of <paramref name="pawn"/>. Will remove an existing non-binary parent first.
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="newParent"></param>
        public static void SetParent(this Pawn pawn, Pawn newParent)
        {
            if (newParent != null && !newParent.IsEnby())
            {
                LogUtil.Warning($"Tried to set {newParent} with gender {newParent.gender} as {pawn}'s non-binary parent.");
                return;
            }
            Pawn parent = pawn.GetParent((Gender)3);
            if (parent != newParent)
            {
                if (parent != null)
                {
                    pawn.relations.RemoveDirectRelation(PawnRelationDefOf.Parent, parent);
                }
                if (newParent != null)
                {
                    pawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, newParent);
                }
            }
        }
    }

    [HarmonyPatch]
    public static class GetParent_Patches
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(ParentRelationUtility), nameof(ParentRelationUtility.GetMother));
            yield return AccessTools.Method(typeof(ParentRelationUtility), nameof(ParentRelationUtility.GetFather));
        }

        [HarmonyPostfix]
        public static void Postfix(Pawn pawn, ref Pawn __result)
        {
            //We don't want to interfere if this is being called from within SetMother or SetFather
            if (!SetParent_Patches.skip)
            {
                __result ??= pawn.GetParent((Gender)3);
            }
        }
    }
}
