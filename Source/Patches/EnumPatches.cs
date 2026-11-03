using HarmonyLib;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Verse;

namespace NonBinaryGender.Patches
{
    public static class Enum_Patches
    {

        private static object genderCache;
        public static void TryInjectGenderEnumInfo()
        {
            try
            {
                Type runtimeType = AccessTools.TypeByName("System.RuntimeType");

                MethodInfo ReplaceCacheEntry = AccessTools.Method(runtimeType, "ReplaceCacheEntry");
                if (ReplaceCacheEntry == null) throw new MissingMethodException("System.RuntimeType", "ReplaceCacheEntry<T>(...)");

                MethodInfo GetEnumInfo = AccessTools.Method(typeof(Enum), "GetEnumInfo");

                Type RuntimeTypeCache = AccessTools.Inner(runtimeType, "RuntimeTypeCache");
                if (RuntimeTypeCache == null) throw new MissingMemberException("System.RuntimeType.RuntimeTypeCache");

                Type EnumInfoGeneric = AccessTools.Inner(typeof(Enum),"EnumInfo`1");
                if (EnumInfoGeneric == null) throw new MissingMemberException("System.RuntimeType.RuntimeTypeCache.EnumInfo`1");

                Type EnumInfoByte = EnumInfoGeneric.MakeGenericType(typeof(byte));
                MethodInfo GetEnumInfoByte = GetEnumInfo.MakeGenericMethod(typeof(byte));
                object enumInfo = GetEnumInfoByte.Invoke(null, [ typeof(Gender), true, ]);
                if (enumInfo == null) throw new InvalidOperationException("Enum.GetEnumInfo<byte>(Gender) returned null.");

                FieldInfo valuesField = AccessTools.Field(EnumInfoByte, "Values");
                FieldInfo namesField = AccessTools.Field(EnumInfoByte, "Names");
                if (valuesField == null) throw new MissingFieldException(EnumInfoByte.FullName, "Values");
                if (namesField == null) throw new MissingFieldException(EnumInfoByte.FullName, "Names");

                byte[] oldValues = valuesField.GetValue(enumInfo) as byte[];
                string[] oldNames = namesField.GetValue(enumInfo) as string[];
                if (oldValues == null) throw new InvalidCastException("EnumInfo.Values was not byte[].");
                if (oldNames == null) throw new InvalidCastException("EnumInfo.Names was not string[].");
                if (oldValues.Length != oldNames.Length) throw new InvalidOperationException("EnumInfo.Values and Names length mismatch.");

                if (oldValues.Contains((byte)3) && oldNames.Contains("Enby"))
                {
                    KeepStrongCacheRef(runtimeType, typeof(Gender));
                    return;
                }

                byte[] newValues = new byte[oldValues.Length + 1];
                string[] newNames = new string[oldNames.Length + 1];
                Array.Copy(oldValues, newValues, oldValues.Length);
                Array.Copy(oldNames, newNames, oldNames.Length);
                newValues[oldValues.Length] = 3;
                newNames[oldNames.Length] = "Enby";

                ConstructorInfo enumInfoCtor = AccessTools.Constructor(EnumInfoByte, [typeof(bool), typeof(byte[]), typeof(string[])]);
                if (enumInfoCtor == null) throw new MissingMethodException(EnumInfoByte.FullName, ".ctor(bool, byte[], string[], ...)");

                object[] ctorArgs = [false, newValues, newNames];

                object newEnumInfo = enumInfoCtor.Invoke(ctorArgs);
                MethodInfo replaceCacheEntry = ReplaceCacheEntry.MakeGenericMethod(EnumInfoByte);
                replaceCacheEntry.Invoke(typeof(Gender), [ newEnumInfo ]);

                KeepStrongCacheRef(runtimeType, typeof(Gender));
            }
            catch (Exception e)
            {
                Log.Error($"[NonBinaryGender] Failed to inject Gender enum cache for CoreCLR: {e}");
            }
        }

        private static void KeepStrongCacheRef(Type runtimeType, Type enumType)
        {
            // RuntimeType cache is weakly held; keep a strong root so injected EnumInfo survives GC.
            PropertyInfo cacheProperty = AccessTools.Property(runtimeType, "Cache"); 
            if (cacheProperty == null) throw new MissingMemberException(runtimeType.FullName, "Cache");
            object cache = cacheProperty.GetValue(enumType);
            if (cache == null) throw new InvalidOperationException("RuntimeType.Cache returned null.");
            
            if (cache is IEnumerable enumerable)
            {
                genderCache = enumerable.Cast<object>().ToArray();
            }
            else
            {
                genderCache = cache;
            }
        }
    }
}