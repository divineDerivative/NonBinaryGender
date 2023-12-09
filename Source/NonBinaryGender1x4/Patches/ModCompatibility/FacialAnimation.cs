using System;
using Verse;
using HarmonyLib;
using FacialAnimation;
using System.Reflection;
using System.Linq;

namespace NonBinaryGender.Patches
{
    public static class FacialAnimation_Patches
    {
        public static void PatchFacialAnimation(this Harmony harmony)
        {
            MethodInfo targetMethod = null;
            foreach (Type t in typeof(NL_SelectPartWindow).GetNestedTypes(AccessTools.all).Where((Type t) => t.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() != null))
            {
                foreach (MethodInfo method in t.GetMethods(AccessTools.all))
                {
                    if (method.ReturnType == typeof(bool) && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(Verse.HeadTypeDef))
                    {
                        if (targetMethod != null)
                        {
                            Log.Error("Multiple matching methods found: " + targetMethod.Name + " and " + method.Name);
                        }
                        targetMethod = method;
                    }
                }
            }

            MethodInfo patchMethod = typeof(FacialAnimation_Patches).GetMethod(nameof(DrawAnimationPawnParamVanillaHeadTypeDefPredicatePrefix));
            harmony.Patch(targetMethod, prefix: new HarmonyMethod(patchMethod));
        }

        //This causes the correct head types to populate the float menu in the editor window, instead of being empty and causing an error
        public static bool DrawAnimationPawnParamVanillaHeadTypeDefPredicatePrefix(Verse.HeadTypeDef x, Pawn ___pawn, ref bool __result)
        {
            if (___pawn.IsEnby())
            {
                //This just excludes skull/stump since some races have none gendered heads
                string path = x.graphicPath.ToLower();
                __result = !path.Contains("skull") && !path.Contains("stump");
                return false;
            }
            return true;
        }
    }
}