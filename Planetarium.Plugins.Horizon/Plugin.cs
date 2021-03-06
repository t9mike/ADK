﻿using Planetarium.Config;
using Planetarium.Types;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planetarium.Plugins.Horizon
{
    public class Plugin : AbstractPlugin
    {
        public Plugin(ISettings settings)
        {
            #region Settings

            AddSetting(new SettingItem("Ground", true, "Grids"));
            AddSetting(new SettingItem("HorizonLine", true, "Grids"));
            AddSetting(new SettingItem("LabelCardinalDirections", true, "Grids", s => s.Get<bool>("HorizonLine")));

            AddSetting(new SettingItem("ColorCardinalDirections", Color.FromArgb(0x00, 0x99, 0x99), "Colors"));
            AddSetting(new SettingItem("ColorHorizon", Color.FromArgb(0xC8, 0x00, 0x40, 0x00), "Colors"));

            #endregion Settings

            AddToolbarItem(new ToolbarToggleButton("Settings.Ground", "IconGround", new SimpleBinding(settings, "Ground"), "Grids"));

            ExportResourceDictionaries("Images.xaml");
        }
    }
}
