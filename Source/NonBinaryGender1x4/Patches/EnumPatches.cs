using System;
using Verse;
using HarmonyLib;
using System.Reflection;

namespace NonBinaryGender.Patches
{
    [HarmonyPatch]
    public static class Enum_Patches
    {
        //This has been used by HAR to parse graphic file names
        //This is also used when saving the gender to the save file
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Enum), nameof(Enum.GetName))]
        public static bool EnumGetNamePatch(Type enumType, object value, ref string __result)
        {
            if (enumType == typeof(Gender))
            {
                if (Convert.ToInt32(value) == 3)
                {
                    __result = "Enby";
                    return false;
                }
            }
            return true;
        }

        //This is no longer needed since it uses data from GetCachedValuesAndNames
        //[HarmonyPatch(typeof(Enum), nameof(Enum.GetNames))]
        //[HarmonyPostfix]
        //public static void EnumGetNamesPatch(Type enumType, ref string[] __result)
        //{
        //    if (enumType == typeof(Gender))
        //    {
        //        int length = __result.Length;
        //        string[] newArray = new string[length + 1];
        //        for (int i = 0; i < length; i++)
        //        {
        //            newArray[i] = __result[i];
        //        }
        //        newArray[length] = "Enby";
        //        __result = newArray;
        //    }
        //}

        //This is no longer needed since it uses data from GetCachedValuesAndNames
        //So far this has been used for generating deities
        //[HarmonyPatch(typeof(Enum), nameof(Enum.GetValues))]
        //[HarmonyPostfix]
        //public static void EnumGetValuesPatch(Type enumType, ref Array __result)
        //{
        //    if (enumType == typeof(Gender))
        //    {
        //        int length = __result.Length;
        //        Gender enby = (Gender)length;
        //        Array newArray = Array.CreateInstance(enumType, length + 1);
        //        for (int i = 0; i < length; i++)
        //        {
        //            newArray.SetValue(__result.GetValue(i), i);
        //        }
        //        newArray.SetValue(enby, length);
        //        __result = newArray;
        //    }
        //}

        private static Type valuesAndNames = AccessTools.TypeByName("ValuesAndNames");

        private static object genderCache;

        //This enables "Enby" to be read from the save file and turned into 3
        //It also causes Enum.GetValues && Enum.GetNames to return 'correct' data without needing additional patches
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Enum), "GetCachedValuesAndNames")]
        public static void GetCachedValuesAndNamesPatch(ref object __result, Type enumType, bool getNames)
        {
            if (enumType == typeof(Gender))
            {
                if (genderCache == null)
                {
                    ConstructorInfo ctor = AccessTools.Constructor(valuesAndNames, parameters: new Type[] { typeof(ulong[]), typeof(string[]) });
                    ulong[] values = (ulong[])AccessTools.Field(valuesAndNames, "Values").GetValue(__result);
                    string[] names = (string[])AccessTools.Field(valuesAndNames, "Names").GetValue(__result);
                    int length = values.Length;
                    ulong[] newValues = new ulong[length + 1];
                    string[] newNames = new string[length + 1];
                    for (int i = 0; i < length; i++)
                    {
                        newValues[i] = values[i];
                        newNames[i] = names[i];
                    }
                    newValues[length] = (uint)length;
                    newNames[length] = "Enby";
                    genderCache = ctor.Invoke(new object[] { newValues, newNames });
                }
                __result = genderCache;
            }
        }
    }
}