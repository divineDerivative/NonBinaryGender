using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NonBinaryGender
{
    public class Settings : ModSettings
    {
        public float enbyChance = 0.1f;
        public GenderNeutralNameOption neutralNames = GenderNeutralNameOption.None;

        public static bool HARActive = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref enbyChance, "enbyChance", 0.1f);
            Scribe_Values.Look(ref neutralNames, "neutralNames", GenderNeutralNameOption.None);
        }
    }

    internal class NonBinaryGenderMod : Mod
    {
        public static Settings settings;

        public NonBinaryGenderMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<Settings>();
        }

        public override string SettingsCategory()
        {
            return "NonBinaryGenderModName".Translate();
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            if (settings.neutralNames != GenderNeutralNameOption.None && !Names.alreadyAdded)
            {
                Names.AddUnisexNames();
            }
        }

        public override void DoSettingsWindowContents(Rect canvas)
        {
            Listing_Standard list = new()
            {
                ColumnWidth = (canvas.width / 2f) - 17f
            };
            list.Begin(canvas);

            list.Label("Enby.EnbyChance".Translate() + (int)(settings.enbyChance * 100f) + "%");
            settings.enbyChance = list.Slider(settings.enbyChance, 0f, 1f);

            if (list.ButtonTextLabeled("Enby.GenderNeutralNames".Translate(), settings.neutralNames.ToString(), tooltip: "Enby.GenderNeutralNamesTooltip".Translate()))
            {
                List<FloatMenuOption> options = [];
                foreach (GenderNeutralNameOption value in Enum.GetValues(typeof(GenderNeutralNameOption)))
                {
                    options.Add(new FloatMenuOption(Enum.GetName(typeof(GenderNeutralNameOption), value), delegate
                    {
                        settings.neutralNames = value;
                    })
                    {
                        tooltip = Names.OptionTooltip(value, false)
                    });
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            list.Label(Names.OptionTooltip(settings.neutralNames, true));

            list.End();
        }
    }
}
