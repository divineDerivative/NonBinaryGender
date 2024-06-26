﻿using HarmonyLib;
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
        [HarmonyPatch(typeof(PawnRelationWorker_Child), nameof(PawnRelationWorker_Child.InRelation))]
        public static void ChildPostfix(Pawn me, Pawn other, ref bool __result)
        {
            //If it's already true we don't need to do anything
            if (!__result && me != other)
            {
                //In case they're both enby, we need to compare them both
                __result = other.GetParents().Where(p => p.IsEnby()).Any(p => p == me);
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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PawnRelationWorker_NephewOrNiece), nameof(PawnRelationWorker_NephewOrNiece.InRelation))]
        public static void NephewOrNiecePostfix(Pawn me, Pawn other, ref bool __result)
        {
            if (!__result && me != other)
            {
                __result = InRelationViaParents(other, me, PawnRelationDefOf.Sibling.Worker) || InRelationViaParents(other, me, PawnRelationDefOf.HalfSibling.Worker);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PawnRelationWorker_Grandchild), nameof(PawnRelationWorker_Grandchild.InRelation))]
        public static void GrandchildPostfix(Pawn me, Pawn other, ref bool __result)
        {
            if (!__result && me != other)
            {
                __result = InRelationViaParents(other, me, PawnRelationDefOf.Child.Worker);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PawnRelationWorker_Cousin), nameof(PawnRelationWorker_Cousin.InRelation))]
        public static void CousinPostfix(Pawn me, Pawn other, ref bool __result)
        {
            if (!__result && me != other)
            {
                __result = InRelationViaParents(other, me, PawnRelationDefOf.UncleOrAunt.Worker);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PawnRelationWorker_CousinOnceRemoved), nameof(PawnRelationWorker_CousinOnceRemoved.InRelation))]
        public static void CousinOnceRemovedPostfix(Pawn me, Pawn other, ref bool __result)
        {
            if (!__result && me != other)
            {
                __result = InRelationViaParents(other, me, PawnRelationDefOf.Cousin.Worker) || InRelationViaParents(other, me, PawnRelationDefOf.GranduncleOrGrandaunt.Worker);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PawnRelationWorker_GrandnephewOrGrandniece), nameof(PawnRelationWorker_GrandnephewOrGrandniece.InRelation))]
        public static void GrandnephewOrGrandniecePostfix(Pawn me, Pawn other, ref bool __result)
        {
            if (!__result && me != other)
            {
                __result = InRelationViaParents(other, me, PawnRelationDefOf.NephewOrNiece.Worker);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PawnRelationWorker_GranduncleOrGrandaunt), nameof(PawnRelationWorker_GranduncleOrGrandaunt.InRelation))]
        public static void GranduncleOrGrandauntPostfix(Pawn me, Pawn other, ref bool __result)
        {
            if (!__result && me != other)
            {
                __result = InRelationViaParents(other, me, PawnRelationDefOf.GreatGrandparent.Worker);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PawnRelationWorker_GreatGrandchild), nameof(PawnRelationWorker_GreatGrandchild.InRelation))]
        public static void GreatGrandchildPostfix(Pawn me, Pawn other, ref bool __result)
        {
            if (!__result && me != other)
            {
                __result = InRelationViaParents(other, me, PawnRelationDefOf.Grandchild.Worker);
            }
        }

        //Prefix because we don't just need to look at their enby parent's enby parents, but binary parents of their enby parents and enby parents of their binary parents
        //So might as well just look at them all and be done
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PawnRelationWorker_SecondCousin), nameof(PawnRelationWorker_SecondCousin.InRelation))]
        public static bool SecondCousinPrefix(Pawn me, Pawn other, ref bool __result)
        {
            if (me != other)
            {
                foreach (Pawn parent in other.GetParents())
                {
                    if (InRelationViaParents(parent, me, PawnRelationDefOf.GranduncleOrGrandaunt.Worker, false))
                    {
                        __result = true;
                        return false;
                    }
                }
            }
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PawnRelationWorker_Stepparent), nameof(PawnRelationWorker_Stepparent.InRelation))]
        public static void StepparentPostfix(Pawn me, Pawn other, ref bool __result)
        {
            if (!__result && me != other)
            {
                //Make sure they're not already a parent
                if (!PawnRelationDefOf.Parent.Worker.InRelation(me, other))
                {
                    __result = InRelationViaParents(me, other, PawnRelationDefOf.Spouse.Worker);
                }
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
