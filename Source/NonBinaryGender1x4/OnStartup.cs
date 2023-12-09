using HarmonyLib;
using NonBinaryGender.Patches;
using Verse;

namespace NonBinaryGender
{
    [StaticConstructorOnStartup]
    internal static class OnStartup
    {
        static OnStartup()
        {
            Harmony harmony = new Harmony("divineDerivative.NonBinaryGender");
            harmony.PatchAll();
            if (ModsConfig.IsActive("void.charactereditor"))
            {
                CharacterEditorPatches.PatchCE(harmony);
            }
            if (ModsConfig.IsActive("erdelf.humanoidalienraces") || ModsConfig.IsActive("erdelf.humanoidalienraces.dev"))
            {
                HARPatches.PatchHAR(harmony);
                Settings.HARActive = true;
            }
            if (ModsConfig.IsActive("Nals.FacialAnimation"))
            {
                FacialAnimation_Patches.PatchFacialAnimation(harmony);
            }
            //Add names if needed
            if (NonBinaryGenderMod.settings.neutralNames != GenderNeutralNameOption.None)
            {
                Names.AddUnisexNames();
            }
        }
    }
}