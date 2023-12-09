using System.Collections.Generic;
using RimWorld;
using Verse;
using HarmonyLib;
using UnityEngine;

namespace NonBinaryGender.Patches
{
    [HarmonyPatch]
    public static class PawnRenderer_Patches
    {
        public static HashSet<HeadTypeDef> HeadDefs;

        //This adjusts the head offset when a male/female head is used with the 'wrong' body type
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.BaseHeadOffsetAt))]
        public static void BaseHeadOffsetAtPostfix(ref Vector3 __result, Rot4 rotation, Pawn ___pawn)
        {
            //Cache the list of heads we care about
            if (HeadDefs == null)
            {
                HeadDefs = new HashSet<HeadTypeDef>
                {
                    //I'm annoyed that these are so inconsistent
                    DefDatabase<HeadTypeDef>.GetNamed("Female_AveragePointy"),
                    DefDatabase<HeadTypeDef>.GetNamed("Male_AverageNormal"),
                    DefDatabase<HeadTypeDef>.GetNamed("Male_AveragePointy"),
                    DefDatabase<HeadTypeDef>.GetNamed("Male_AverageWide"),
                    DefDatabase<HeadTypeDef>.GetNamed("Male_NarrowNormal"),
                    DefDatabase<HeadTypeDef>.GetNamed("Male_NarrowPointy")
                };
            }

            //We only care about east and west, since the y offset is the same for all default body types
            if (___pawn.IsEnby() && (rotation.AsInt == 1 || rotation.AsInt == 3) && ___pawn.story != null)
            {
                BodyTypeDef bodyType = ___pawn.story.bodyType;
                HeadTypeDef headType = ___pawn.story.headType;

                //We only care about these body types
                if (bodyType == BodyTypeDefOf.Male || bodyType == BodyTypeDefOf.Female)
                {
                    //Calculate the age appropriate offset for each body
                    Vector2 maleOffset = BodyTypeDefOf.Male.headOffset * Mathf.Sqrt(___pawn.ageTracker.CurLifeStage.bodySizeFactor);
                    Vector2 femaleOffset = BodyTypeDefOf.Female.headOffset * Mathf.Sqrt(___pawn.ageTracker.CurLifeStage.bodySizeFactor);
                    //For this part we only care if they have certain human heads, we don't want to accidentally grab a custom head
                    if (HeadDefs.Contains(headType))
                    {
                        //Get the difference between the offset that should be applied and the one that already was
                        float correctOffset = 0f;
                        if (bodyType == BodyTypeDefOf.Male && headType.defName.StartsWith("Female"))
                        {
                            correctOffset = femaleOffset.x - maleOffset.x;
                        }
                        if (bodyType == BodyTypeDefOf.Female && headType.defName.StartsWith("Male"))
                        {
                            correctOffset = maleOffset.x - femaleOffset.x;
                        }
                        //Adjust the x result with the above; add for east, subtract for west
                        __result.x += rotation.AsInt == 1 ? correctOffset : 0f - correctOffset;
                        return;
                    }
                    //Next we look at aliens
                    //Hopefully this doesn't mess stuff up too much
                    if (Settings.HARActive && ___pawn.IsAlien())
                    {
                        //So this as a vector because I don't know what other people's offsets will look like
                        Vector2 correctOffset = Vector2.zero;
                        if (bodyType == BodyTypeDefOf.Male && headType.defName.ToLower().Contains("female"))
                        {
                            //I think we still do this part, since the other offsets go on top of the ones from the body type def
                            correctOffset = femaleOffset - maleOffset;
                            //Do whatever needs to be done to account for HAR offsets
                            correctOffset += HARPatches.HeadOffsetHelper(___pawn, Gender.Male, correctOffset);
                        }
                        if (bodyType == BodyTypeDefOf.Female && headType.defName.ToLower().Contains("male"))
                        {
                            correctOffset = maleOffset - femaleOffset;
                            correctOffset += HARPatches.HeadOffsetHelper(___pawn, Gender.Female, correctOffset);
                        }
                        //Adjust as appropriate
                        __result += rotation.AsInt == 1 ? new Vector3(correctOffset.x, 0f, correctOffset.y) : new Vector3(0f - correctOffset.x, 0f, correctOffset.y);
                        return;
                    }
                }
            }
        }
    }
}
