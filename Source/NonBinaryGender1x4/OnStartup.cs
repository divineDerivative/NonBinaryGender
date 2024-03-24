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
                harmony.PatchCE();
            }
            if (ModsConfig.IsActive("erdelf.humanoidalienraces") || ModsConfig.IsActive("erdelf.humanoidalienraces.dev"))
            {
                harmony.PatchHAR();
                Settings.HARActive = true;
                if (ModsConfig.IsActive("Killathon.MechHumanlikes.AndroidTiers"))
                {
                    harmony.PatchATR();
                }
            }
            if (ModsConfig.IsActive("Nals.FacialAnimation"))
            {
                harmony.PatchFacialAnimation();
            }
            if (ModsConfig.IsActive("TwoPenny.PortraitsOfTheRim"))
            {
                harmony.PatchPortraits();
            }
            //Add names if needed
            if (NonBinaryGenderMod.settings.neutralNames != GenderNeutralNameOption.None)
            {
                Names.AddUnisexNames();
            }
        }
    }
}