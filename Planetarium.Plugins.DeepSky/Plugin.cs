﻿using Planetarium.Config;
using Planetarium.Types;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Planetarium.Plugins.DeepSky
{
    public class Plugin : AbstractPlugin
    {
        public Plugin(ISettings settings)
        {
            #region Settings

            AddSetting(new SettingItem("DeepSky", true, "Deep Sky Objects"));
            AddSetting(new SettingItem("DeepSkyLabels", true, "Deep Sky Objects", s => s.Get<bool>("DeepSky")));
            AddSetting(new SettingItem("DeepSkyOutlines", true, "Deep Sky Objects", s => s.Get<bool>("DeepSky")));

            AddSetting(new SettingItem("ColorDeepSkyOutline", Color.FromArgb(50, 50, 50), "Colors"));
            AddSetting(new SettingItem("ColorDeepSkyLabel", Color.FromArgb(0, 64, 128), "Colors"));

            #endregion Settings

            #region Toolbar Integration

            AddToolbarItem(new ToolbarToggleButton("Settings.DeepSky", "IconDeepSky", new SimpleBinding(settings, "DeepSky"), "Objects"));

            #endregion Toolbar Integration

            #region Exports

            ExportResourceDictionaries("Images.xaml");

            #endregion
        }
    }
}
