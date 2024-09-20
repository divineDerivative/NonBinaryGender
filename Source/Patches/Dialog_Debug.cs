using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using HarmonyLib;
using System.Reflection;

namespace NonBinaryGender.Patches
{
    [HarmonyPatch]
    internal static class Dialog_Debug_Patches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Dialog_Debug), nameof(Dialog_Debug.TrySetupNodeGraph))]
        public static bool TrySetupNodeGraphPrefix(ref DebugActionNode ___rootNode, ref Dictionary<DebugTabMenuDef, DebugActionNode> ___roots)
        {
            ___rootNode = new DebugActionNode("Root");
            var allDefs = DefDatabase<DebugTabMenuDef>.AllDefsListForReading;
            foreach(var def in allDefs)
            {
                ___roots.Add(def, DebugTabMenu.CreateMenu(def,null,___rootNode).InitActions(___rootNode));
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(DebugTabMenu), nameof(DebugTabMenu.CreateMenu))]
        public static bool CreateMenuPrefix(DebugTabMenuDef def, Dialog_Debug dialog, DebugActionNode root, ref DebugTabMenu __result)
        {
            __result = (DebugTabMenu)Activator.CreateInstance(def.menuClass, def, dialog, root);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(DebugTabMenu_Actions), nameof(DebugTabMenu.InitActions))]
        public static bool InitActionsPrefix(DebugActionNode absRoot, ref DebugActionNode ___myRoot, DebugTabMenu_Actions __instance, ref DebugActionNode ___moreActionsNode, ref DebugActionNode __result)
        {
            MethodInfo GenerateCacheForMethod = AccessTools.Method(typeof(DebugTabMenu_Actions), "GenerateCacheForMethod");
            ___myRoot = new DebugActionNode("Actions");
            absRoot.AddChild(___myRoot);
            ___moreActionsNode = new DebugActionNode("Show more actions")
            {
                category = "More debug actions",
                displayPriority = -999999
            };
            ___myRoot.AddChild(___moreActionsNode);
            foreach (Type allType in GenTypes.AllTypes)
            {
                MethodInfo[] methods = allType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (MethodInfo methodInfo in methods)
                {
                    try
                    {
                        if (methodInfo.TryGetAttribute<DebugActionAttribute>(out var customAttribute))
                        {
                            GenerateCacheForMethod.Invoke(__instance, new object[] { methodInfo, customAttribute });
                        }
                        if (!methodInfo.TryGetAttribute<DebugActionYielderAttribute>(out var _))
                        {
                            continue;
                        }
                        foreach (DebugActionNode item in (IEnumerable<DebugActionNode>)methodInfo.Invoke(null, null))
                        {
                            ___myRoot.AddChild(item);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Found it, type: " + allType.Name + " method: " + methodInfo.Name);
                    }
                }
            }
            ___myRoot.TrySort();
            __result = ___myRoot;
            return false;
        }
    }
}
