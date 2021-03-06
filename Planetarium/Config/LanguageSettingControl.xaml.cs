﻿using Planetarium.Types.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Planetarium.Config
{
    /// <summary>
    /// Interaction logic for LanguageSettingControl.xaml
    /// </summary>
    public partial class LanguageSettingControl : UserControl
    {
        public LanguageSettingControl()
        {
            InitializeComponent();
            cmbCultures.ItemsSource = Text.GetLocales();
        }
    }
}
