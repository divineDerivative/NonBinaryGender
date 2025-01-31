using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PawnRelationWorker_HalfSibling), nameof(PawnRelationWorker_HalfSibling.InRelation))]
        public static void HalfSiblingPostfix(Pawn me, Pawn other, ref bool __result)
        {
            //If it's already true we don't need to do anything
            if (!__result && me != other)
            {
                //But we do still need to check if they're full siblings first
                if (!PawnRelationDefOf.Sibling.Worker.InRelation(me, other))
                {
                    //Have to check against all enby parents because HasSameParent will only grab one; if one of them has two enby parents, it might not grab the shared one
                    IEnumerable<Pawn> myParents = me.GetParents().Where(p => p.IsEnby());
                    IEnumerable<Pawn> yourParents = other.GetParents().Where(p => p.IsEnby());

                    __result = myParents.SharesElementWith(yourParents);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PawnRelationWorker_UncleOrAunt), nameof(PawnRelationWorker_UncleOrAunt.InRelation))]
        public static void UncleOrAuntPostfix(Pawn me, Pawn other, ref bool __result)
        {
            if (!__result && me != other)
            {
                __result = InRelationViaParents(me, other, PawnRelationDefOf.Sibling.Worker) || InRelationViaParents(me, other, PawnRelationDefOf.HalfSibling.Worker);
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
