﻿using ADK;
using Planetarium.Config;
using Planetarium.Plugins.SolarSystem.Controls;
using Planetarium.Types;
using Planetarium.Types.Localization;
using System.Drawing;

namespace Planetarium.Plugins.SolarSystem
{
    public class Plugin : AbstractPlugin
    {
        public Plugin(ISettings settings)
        {
            #region Settings

            AddSetting(new SettingItem("Sun", true, "Sun"));
            AddSetting(new SettingItem("SunLabel", true, "Sun", s => s.Get<bool>("Sun")));
            AddSetting(new SettingItem("SunTexture", true, "Sun", s => s.Get<bool>("Sun")));
            AddSetting(new SettingItem("SunTexturePath", "https://soho.nascom.nasa.gov/data/REPROCESSING/Completed/{yyyy}/hmiigr/{yyyy}{MM}{dd}/{yyyy}{MM}{dd}_0000_hmiigr_512.jpg", "Sun", s => s.Get<bool>("Sun") && s.Get<bool>("TextureSun")));

            AddSetting(new SettingItem("Planets", true, "Planets"));
            AddSetting(new SettingItem("UseTextures", true, "Planets", s => s.Get<bool>("Planets")));
            AddSetting(new SettingItem("JupiterMoonsShadowOutline", true, "Planets", s => s.Get<bool>("Planets")));
            AddSetting(new SettingItem("ShowRotationAxis", true, "Planets", s => s.Get<bool>("Planets")));

            AddSetting(new SettingItem("Moon", true, "Moon"));
            AddSetting(new SettingItem("MoonLabel", true, "Moon", s => s.Get<bool>("Moon")));
            AddSetting(new SettingItem("MoonTexture", true, "Moon", s => s.Get<bool>("Moon")));
            AddSetting(new SettingItem("EarthShadowOutline", false, "Moon"));

            AddSetting(new SettingItem("GRSLongitude", new GreatRedSpotSettings()
            {
                Epoch = 2458150.5000179596,
                MonthlyDrift = 2,
                Longitude = 283
            }, "Planets", typeof(GRSSettingControl), s => s.Get<bool>("Planets")));

            // Colors

            AddSetting(new SettingItem("ColorSolarSystemLabel", Color.DimGray, "Colors"));

            #endregion Settings

            AddToolbarItem(new ToolbarToggleButton("Settings.Planets", "IconPlanet", new SimpleBinding(settings, "Planets"), "Objects"));

            ExportResourceDictionaries("Images.xaml");
        }
    }
}
