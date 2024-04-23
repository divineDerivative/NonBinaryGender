using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace NonBinaryGender
{
    [StaticConstructorOnStartup]
    public static class EnbyUtility
    {
        public static bool IsEnby(this Pawn pawn) => (int)pawn.gender == 3;
        public static bool IsEnby(this Gender gender) => (int)gender == 3;
        public static readonly Texture2D NonBinaryIcon = ContentFinder<Texture2D>.Get("UI/Gender/NonBinary", true);
        public static readonly Texture2D NonBinaryButton = ContentFinder<Texture2D>.Get("UI/Gender/NonBinaryButton", true);
        public static MethodInfo GetParentMethod = AccessTools.Method(typeof(ParentRelationUtility), "GetParent");

        public static Pawn GetParent(this Pawn pawn, Gender gender)
        {
            if (GetParentMethod != null)
            {
                return (Pawn)GetParentMethod.Invoke(null, [pawn, gender]);
            }
            if (!pawn.RaceProps.IsFlesh)
            {
                return null;
            }
            if (pawn.relations == null)
            {
                return null;
            }
            foreach (DirectPawnRelation relation in pawn.relations.DirectRelations)
            {
                if (relation.def == PawnRelationDefOf.Parent && relation.otherPawn.gender == gender)
                {
                    return relation.otherPawn;
                }
            }
            return null;
        }
    }

    public static class InfoHelper
    {
        public static MethodInfo RandValue = typeof(Rand).GetProperty("Value").GetGetMethod();
        public static FieldInfo genderField = AccessTools.Field(typeof(Pawn), nameof(Pawn.gender));
    }

    public enum GenderNeutralNameOption
    {
        None,
        Only,
        Add
    }

    public static class HelperExtensions
    {
        public static int LocalIndex(this CodeInstruction code)
        {
            if (code.opcode == OpCodes.Ldloc_0 || code.opcode == OpCodes.Stloc_0) return 0;
            else if (code.opcode == OpCodes.Ldloc_1 || code.opcode == OpCodes.Stloc_1) return 1;
            else if (code.opcode == OpCodes.Ldloc_2 || code.opcode == OpCodes.Stloc_2) return 2;
            else if (code.opcode == OpCodes.Ldloc_3 || code.opcode == OpCodes.Stloc_3) return 3;
            else if (code.opcode == OpCodes.Ldloc_S || code.opcode == OpCodes.Ldloc) return Convert.ToInt32(code.operand);
            else if (code.opcode == OpCodes.Stloc_S || code.opcode == OpCodes.Stloc) return Convert.ToInt32(code.operand);
            else if (code.opcode == OpCodes.Ldloca_S || code.opcode == OpCodes.Ldloca) return Convert.ToInt32(code.operand);
            else throw new ArgumentException("Instruction is not a load or store", nameof(code));
        }

        public static CodeInstruction LoadField(FieldInfo fieldInfo, bool useAddress = false)
        {
            if (fieldInfo is null)
            {
                throw new ArgumentException($"fieldInfo is null");
            }
            return new CodeInstruction((!useAddress) ? (fieldInfo.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld) : (fieldInfo.IsStatic ? OpCodes.Ldsflda : OpCodes.Ldflda), fieldInfo);
        }

        public static CodeInstruction Call(this MethodInfo methodInfo)
        {
            if (methodInfo is null)
            {
                throw new ArgumentException($"methodInfo is null");
            }

            return new CodeInstruction(OpCodes.Call, methodInfo);
        }

        public static bool LoadsField(this CodeInstruction code, Type type, string name)
        {
            FieldInfo field = AccessTools.Field(type, name);
            return code.LoadsField(field);
        }

        public static bool StoresField(this CodeInstruction code, Type type, string name)
        {
            FieldInfo field = AccessTools.Field(type, name);
            return code.StoresField(field);
        }

        public static bool Calls(this CodeInstruction code, Type type, string name)
        {
            MethodInfo method = AccessTools.Method(type, name);
            return code.Calls(method);
        }
    }
}
