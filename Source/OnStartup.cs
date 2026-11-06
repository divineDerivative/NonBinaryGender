using DivineFramework;
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
            Harmony harmony = new("divineDerivative.NonBinaryGender");
            harmony.PatchAll();
#if v1_4
            if (ModsConfig.IsActive("void.charactereditor") || ModsConfig.IsActive("void.charactereditor_steam"))
            {
                harmony.PatchCE();
            }
#endif
            if (ModsConfig.IsActive("erdelf.humanoidalienraces") || ModsConfig.IsActive("erdelf.humanoidalienraces.dev"))
            {
                harmony.PatchHAR();
                Settings.HARActive = true;
                if (ModsConfig.IsActive("Killathon.MechHumanlikes.AndroidTiers") || ModsConfig.IsActive("Killathon.MechHumanlikes.AndroidTiers_steam"))
                {
                    harmony.PatchATR();
                }
            }
            if (ModsConfig.IsActive("Nals.FacialAnimation") || ModsConfig.IsActive("Nals.FacialAnimation_steam"))
            {
                harmony.PatchFacialAnimation();
            }
            if (ModsConfig.IsActive("TwoPenny.PortraitsOfTheRim") || ModsConfig.IsActive("TwoPenny.PortraitsOfTheRim_steam"))
            {
                harmony.PatchPortraits();
            }
            if (ModsConfig.IsActive("ISOREX.PawnEditor") || ModsConfig.IsActive("ISOREX.PawnEditor_steam") || ModsConfig.IsActive("segaswolf.pawneditor.fork"))
            {
                harmony.PatchPE();
            }
            if (ModsConfig.IsActive("Mlie.RFTribalPawnNames"))
            {
#if v1_6
                harmony.Patch(AccessTools.Method("RTPN_NameBank:namesFor"), prefix: new HarmonyMethod(typeof(NameBank_Patches), nameof(NameBank_Patches.NamesForPatch)));
#else
                harmony.Patch(AccessTools.Method("RTPN_NameBank:NamesFor"), prefix: new HarmonyMethod(typeof(NameBank_Patches), nameof(NameBank_Patches.NamesForPatch)));
#endif
            }
            if (ModsConfig.IsActive("Mlie.RationalRomance"))
            {
                harmony.PatchRationalRomance();
            }
            //Add names if needed
            if (NonBinaryGenderMod.settings.neutralNames != GenderNeutralNameOption.None)
            {
                Names.AddUnisexNames();
            }
        }
    }

    internal class Logger : Logging
    {
        public static readonly Logger LogUtil = new Logger();
        private Logger() : base("[NonBinaryGender]", () => true) { }
    }
}