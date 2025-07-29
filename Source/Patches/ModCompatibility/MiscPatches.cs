using System;
using HarmonyLib;
using NonBinaryGender.Patches;

namespace NonBinaryGender
{
    public static class MiscPatches
    {
        public static void PatchRationalRomance(this Harmony harmony)
        {
            //Apply my patches to RR's destructive prefix for ChanceOfBecomingChildOf
            var genderPatch = AccessTools.Method(typeof(ParentPatches), nameof(ParentPatches.GenderCheckTranspiler));
            var parentPatch = AccessTools.Method(typeof(ParentPatches), nameof(ParentPatches.GetParentTranspiler));
            harmony.Patch(AccessTools.Method(Type.GetType("RationalRomance_Code.ChildRelationUtility_ChanceOfBecomingChildOf, Rainbeau's Rational Romance"), "Prefix"), transpiler: new HarmonyMethod(genderPatch));
            harmony.Patch(AccessTools.Method(Type.GetType("RationalRomance_Code.ChildRelationUtility_ChanceOfBecomingChildOf, Rainbeau's Rational Romance"), "Prefix"), transpiler: new HarmonyMethod(parentPatch));
        }
    }
}
