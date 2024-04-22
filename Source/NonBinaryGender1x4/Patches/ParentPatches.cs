using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace NonBinaryGender.Patches
{
#if v1_5
    [HarmonyPatch]
    public static class ParentPatches
    {
        static MethodInfo GetParent = AccessTools.Method(typeof(ParentRelationUtility), "GetParent");
        static MethodInfo GetFather = AccessTools.Method(typeof(ParentRelationUtility), "GetFather");
        static MethodInfo GetMother = AccessTools.Method(typeof(ParentRelationUtility), "GetMother");
        static MethodInfo ResolveMyName = AccessTools.Method(typeof(PawnRelationWorker_Parent), "ResolveMyName");

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

        //generated is the child, other is the potential parent
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PawnRelationWorker_Parent), nameof(PawnRelationWorker_Parent.GenerationChance))]
        public static bool GenerationChancePrefix(Pawn generated, Pawn other, PawnGenerationRequest request, ref float __result, PawnRelationWorker_Parent __instance)
        {
            if (other.IsEnby())
            {
                Pawn spouse = other.GetFirstSpouseOfOppositeGender();
                float num;
                if (spouse != null)
                {
                    //Put the gendered spouse in the correct slot and put the enby in the other
                    num = ChildRelationUtility.ChanceOfBecomingChildOf(generated, spouse.gender == Gender.Male ? spouse : other, spouse.gender == Gender.Female ? spouse : other, request, null, null);
                }
                else
                {
                    //Don't think it matters where they go if it's only them
                    num = ChildRelationUtility.ChanceOfBecomingChildOf(generated, other, null, request, null, null);
                }
                if (ModsConfig.BiotechActive && request.Context == PawnGenerationContext.PlayerStarter && generated.DevelopmentalStage.Juvenile())
                {
                    num *= 10f;
                }
                __result = num * __instance.BaseGenerationChanceFactor(generated, other, request);
                return false;
            }
            return true;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(ChildRelationUtility), nameof(ChildRelationUtility.ChanceOfBecomingChildOf))]
        public static IEnumerable<CodeInstruction> GetParentTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            //Replace the gender specific methods with GetParent using whatever the provided pawn's gender is
            foreach (CodeInstruction code in instructions)
            {
                if (code.Calls(GetFather))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return HelperExtensions.LoadField(InfoHelper.genderField);
                    yield return HelperExtensions.Call(GetParent);
                }
                else if (code.Calls(GetMother))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return HelperExtensions.LoadField(InfoHelper.genderField);
                    yield return HelperExtensions.Call(GetParent);
                }
                else
                {
                    yield return code;
                }
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(ChildRelationUtility), nameof(ChildRelationUtility.ChanceOfBecomingChildOf))]
        public static IEnumerable<CodeInstruction> GenderCheckTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            //Replace != Gender.Male with == Gender.Female and != Gender.Female with == Gender.Male
            bool genderFound = false;
            bool firstGender = false;
            bool secondGender = false;
            foreach (CodeInstruction code in instructions)
            {
                //We want to do our stuff at every other gender check
                if (code.LoadsField(InfoHelper.genderField))
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

        //The original will just do nothing if other is non-binary, so we do it ourselves
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PawnRelationWorker_Parent), nameof(PawnRelationWorker_Parent.CreateRelation))]
        public static bool CreateRelationPrefix(Pawn generated, Pawn other, ref PawnGenerationRequest request)
        {
            if (other.IsEnby())
            {
                Log.Message($"Attempting to make {other.LabelShort} {generated.LabelShort}'s parent");
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

        public static void SetParent(this Pawn pawn, Pawn newParent)
        {
            Pawn parent = (Pawn)GetParent.Invoke(null, [pawn, (Gender)3]);
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
#endif
}
