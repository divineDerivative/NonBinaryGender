using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;

namespace NonBinaryGender.Patches
{
    [HarmonyPatch]
    public static class RelationWorker_Patches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PawnRelationWorker_Sibling), nameof(PawnRelationWorker_Sibling.InRelation))]
        public static bool SiblingPrefix(Pawn me, Pawn other, ref bool __result)
        {
            //We're just going to check if they have at least two shared parents
            //Not using == 2 because there are shenanigans that can give someone three parents
            if (me == other)
            {
                __result = false;
                return false;
            }
            int sharedParents = 0;
            foreach (Pawn pawn in me.GetParents())
            {
                if (other.GetParents().Contains(pawn))
                {
                    sharedParents++;
                }
            }
            __result = sharedParents > 1;
            return false;
        }

#if v1_5
        //Add check for shared non-binary parent
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PawnRelationWorker_HalfSibling), nameof(PawnRelationWorker_HalfSibling.InRelation))]
        public static IEnumerable<CodeInstruction> HalfSiblingTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            Label? returnTrue = new();
            List<CodeInstruction> codes = instructions.ToList();
            for (int i = 0; i < instructions.Count(); i++)
            {
                CodeInstruction code = codes[i];
                CodeInstruction nextCode = codes[i + 1];
                if (code.Calls(typeof(ParentRelationUtility), nameof(ParentRelationUtility.HasSameMother)) && nextCode.Branches(out returnTrue))
                {
                    break;
                }
            }

            bool FatherFound = false;
            foreach (CodeInstruction code in instructions)
            {
                if (FatherFound && code.Branches(out _))
                {
                    yield return new CodeInstruction(OpCodes.Brtrue_S, returnTrue);
                    //me.HasSameParent(other, (Gender)3)
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_3);
                    yield return CodeInstruction.Call(typeof(ParentRelationUtility), "HasSameParent");
                    FatherFound = false;
                }
                if (code.Calls(typeof(ParentRelationUtility), nameof(ParentRelationUtility.HasSameFather)))
                {
                    FatherFound = true;
                }

                yield return code;
            }
        }
#endif

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PawnRelationWorker_UncleOrAunt), nameof(PawnRelationWorker_UncleOrAunt.InRelation))]
        public static void UncleOrAuntPostfix(Pawn me, Pawn other, ref bool __result)
        {
            if (!__result && me != other)
            {
                __result = InRelationViaParents(me, other, PawnRelationDefOf.Sibling.Worker, false) || InRelationViaParents(me, other, PawnRelationDefOf.HalfSibling.Worker, false);
            }
        }

        /// <summary>
        /// Checks if any of <paramref name="first"/>'s parents have a <paramref name="worker"/> relationship with <paramref name="second"/>
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <param name="worker"></param>
        /// <param name="enbyOnly">Whether we only look at non-binary parents</param>
        /// <returns></returns>
        public static bool InRelationViaParents(Pawn first, Pawn second, PawnRelationWorker worker, bool enbyOnly = true)
        {
            IEnumerable<Pawn> parents = first.GetParents();
            if (enbyOnly)
            {
                parents = parents.Where(p => p.IsEnby());
            }
            foreach (Pawn parent in parents)
            {
                if (parent == first || parent == second)
                {
                    continue;
                }
                if (worker.InRelation(second, parent))
                {
                    return true;
                }
            }
            return false;
        }

        public static IEnumerable<Pawn> GetParents(this Pawn pawn)
        {
            if (!pawn.RaceProps.IsFlesh)
            {
                yield break;
            }
            if (pawn.relations == null)
            {
                yield break;
            }
            foreach (DirectPawnRelation relation in pawn.relations.DirectRelations)
            {
                if (relation.def == PawnRelationDefOf.Parent)
                {
                    yield return relation.otherPawn;
                }
            }
        }
    }
}
