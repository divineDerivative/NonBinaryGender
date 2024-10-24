using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using DivineFramework;

namespace NonBinaryGender
{
    public class Settings : ModSettings
    {
        public float enbyChance = 0.1f;
        public GenderNeutralNameOption neutralNames = GenderNeutralNameOption.None;

        public static bool HARActive = false;
        internal SettingsHandler<Settings> handler = new();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref enbyChance, "enbyChance", 0.1f);
            Scribe_Values.Look(ref neutralNames, "neutralNames", GenderNeutralNameOption.None);
        }

        internal void SetUpHandler()
        {
            handler.RegisterNewRow().AddLabel(() => "Enby.EnbyChance".Translate() + (int)(enbyChance * 100f) + "%");
            handler.RegisterNewRow()
                .AddElement(NewElement.Slider<float>()
                .WithReference(this, "enbyChance", enbyChance)
                .MinMax(0f, 1f), "ChanceSlider");
            UIContainer buttonRow = handler.RegisterNewRow("ButtonRow", 2f);
            buttonRow.AddLabel("Enby.GenderNeutralNames".Translate)
                .WithTooltip("Enby.GenderNeutralNamesTooltip".Translate);
            buttonRow.AddElement(NewElement.Button(GenderNeutralButtonAction)
                .WithLabel(() => neutralNames.ToString()));
            handler.RegisterNewRow().AddLabel(() => Names.OptionTooltip(neutralNames, true));

            handler.Initialize();
        }

        private void GenderNeutralButtonAction()
        {
            List<FloatMenuOption> options = [];
            foreach (GenderNeutralNameOption value in Enum.GetValues(typeof(GenderNeutralNameOption)))
            {
                options.Add(new FloatMenuOption(Enum.GetName(typeof(GenderNeutralNameOption), value), delegate
                {
                    neutralNames = value;
                })
                {
                    tooltip = Names.OptionTooltip(value, false)
                });
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }
    }

    internal class NonBinaryGenderMod : Mod
    {
        public static Settings settings;

        public NonBinaryGenderMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<Settings>();
            ModManagement.RegisterMod("NonBinaryGenderModName", typeof(NonBinaryGenderMod).Assembly.GetName().Name, new("0.2.1.0"), "[NonBinaryGender]", () => true);
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

            if (!settings.handler.Initialized)
            {
                settings.handler.width = list.ColumnWidth;
                settings.SetUpHandler();
            }
            settings.handler.Draw(list);

            list.End();
        }
    }
}
