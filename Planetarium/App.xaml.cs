﻿using ADK;
using Ninject;
using Planetarium.Config;
using Planetarium.Logging;
using Planetarium.Renderers;
using Planetarium.Types;
using Planetarium.Types.Localization;
using Planetarium.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;

namespace Planetarium
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IKernel kernel = new StandardKernel();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ViewManager.SetImplementation(new DefaultViewManager(t => kernel.Get(t)));

            var splashVM = new SplashScreenVM();
            ViewManager.ShowWindow(splashVM);

            //in order to ensure the UI stays responsive, we need to
            //do the work on a different thread
            Task.Factory.StartNew(() =>
            {
                ConfigureContainer(splashVM);

                Dispatcher.Invoke(() =>
                {
                    ViewManager.ShowWindow<MainVM>();
                    splashVM.Close();
                });
            });

            Dispatcher.UnhandledException += (s, ea) =>
            {
                string message = $"An unhandled exception occurred:\n\n{ea.Exception.Message}\nStack trace:\n\n{ea.Exception.StackTrace}";
                Trace.TraceError(message);
                ViewManager.ShowMessageBox("Error", message, MessageBoxButton.OK);
                ea.Handled = true;                
            };
        }

        protected override void OnExit(ExitEventArgs e)
        {
            CursorsHelper.SetSystemCursors();
            base.OnExit(e);
        }

        private void SetColorSchema(ColorSchema schema)
        {
            Current.Resources.MergedDictionaries[0].Source = new Uri($@"pack://application:,,,/Planetarium.Types;component/Themes/Colors{(schema == ColorSchema.Red ? "Red" : "Default")}.xaml");
            if (schema == ColorSchema.Red)
            {
                CursorsHelper.SetCustomCursors();
            }
            else
            {
                CursorsHelper.SetSystemCursors();
            }
        }

        private void SetLanguage(string language)
        {            
            var locale = Text.GetLocales().FirstOrDefault(loc => loc.Name == language);
            if (locale != null)
            {
                Text.SetLocale(locale);
            }
        }

        private void ConfigureContainer(IProgress<string> progress)
        {
            // use single logger for whole application
            kernel.Bind<Logger>().ToSelf().InSingletonScope();
            kernel.Get<Logger>();

            Debug.WriteLine("Configuring application container...");

            kernel.Bind<ISettings, Settings>().To<Settings>().InSingletonScope();

            kernel.Bind<ISky, Sky>().To<Sky>().InSingletonScope();
            kernel.Bind<ISkyMap, SkyMap>().To<SkyMap>().InSingletonScope();

            kernel.Bind<SettingsConfig>().ToSelf().InSingletonScope();
            kernel.Bind<ToolbarButtonsConfig>().ToSelf().InSingletonScope();
            kernel.Bind<ContextMenuItemsConfig>().ToSelf().InSingletonScope();

            SettingsConfig settingsConfig = kernel.Get<SettingsConfig>();
            ToolbarButtonsConfig toolbarButtonsConfig = kernel.Get<ToolbarButtonsConfig>();
            ContextMenuItemsConfig contextMenuItemsConfig = kernel.Get<ContextMenuItemsConfig>();
            ICollection<AbstractPlugin> plugins = new List<AbstractPlugin>();

            // TODO: consider more proper way to load plugins
            string homeFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            IEnumerable<string> pluginPaths = Directory.EnumerateFiles(homeFolder, "*.dll");

            progress.Report("Loading plugins");

            foreach (string path in pluginPaths)
            {
                try
                {
                    Assembly.LoadFrom(path);
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Unable to load plugin assembly with path {path}. {ex})");
                }
            }

            // collect all plugins implementations
            // TODO: to support plugin system, we need to load assemblies 
            // from the specific directory and search for plugin there
            Type[] pluginTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(AbstractPlugin).IsAssignableFrom(t) && !t.IsAbstract)
                .ToArray();

            // collect all calculators types
            Type[] calcTypes = pluginTypes
                .SelectMany(p => AbstractPlugin.Calculators(p))
                .ToArray();

            foreach (Type calcType in calcTypes)
            {
                var types = new[] { calcType }.Concat(calcType.GetInterfaces()).ToArray();
                kernel.Bind(types).To(calcType).InSingletonScope();
            }

            // collect all renderers types
            Type[] rendererTypes = pluginTypes
                .SelectMany(p => AbstractPlugin.Renderers(p))
                .ToArray();

            foreach (Type rendererType in rendererTypes)
            {
                var types = new[] { rendererType }.Concat(rendererType.GetInterfaces()).ToArray();
                kernel.Bind(rendererType).ToSelf().InSingletonScope();
            }

            // collect all event provider implementations
            Type[] eventProviderTypes = pluginTypes
                .SelectMany(p => AbstractPlugin.AstroEventProviders(p))
                .ToArray();

            foreach (Type eventProviderType in eventProviderTypes)
            {
                kernel.Bind(eventProviderType).ToSelf().InSingletonScope();
            }

            foreach (Type pluginType in pluginTypes)
            {
                progress.Report($"Creating plugin {pluginType}");

                kernel.Bind(pluginType).ToSelf().InSingletonScope();
                var plugin = kernel.Get(pluginType) as AbstractPlugin;

                // add settings configurations
                settingsConfig.AddRange(plugin.SettingItems);

                // add configured toolbar buttons
                toolbarButtonsConfig.AddRange(plugin.ToolbarItems);

                // add configured context menu items
                contextMenuItemsConfig.AddRange(plugin.ContextMenuItems);

                plugins.Add(plugin);
            }

            // Default rendering order for BaseRenderer descendants.
            settingsConfig.Add(new SettingItem("RenderingOrder", new RenderingOrder(), "Rendering", typeof(RenderersListSettingControl)));

            var settings = kernel.Get<ISettings>();

            // set settings defaults 
            foreach (SettingItem item in settingsConfig)
            {
                settings.Set(item.Name, item.DefaultValue);
            }

            settings.Save("Defaults");

            progress.Report($"Loading settings");

            settings.Load();

            SetLanguage(settings.Get<string>("Language"));
            SetColorSchema(settings.Get<ColorSchema>("Schema"));

            SkyContext context = new SkyContext(
                new Date(DateTime.Now).ToJulianEphemerisDay(),
                new CrdsGeographical(settings.Get<CrdsGeographical>("ObserverLocation")));

            progress.Report($"Creating calculators");

            var calculators = calcTypes
                .Select(c => kernel.Get(c))
                .Cast<BaseCalc>()
                .ToArray();

            progress.Report($"Creating event providers");

            var eventProviders = eventProviderTypes
                .Select(c => kernel.Get(c))
                .Cast<BaseAstroEventsProvider>()
                .ToArray();

            progress.Report($"Creating renderers");

            var renderers = rendererTypes.Select(r => kernel.Get(r)).Cast<BaseRenderer>().ToArray();

            kernel.Get<Sky>().Initialize(context, calculators, eventProviders);

            kernel.Get<SkyMap>().Initialize(context, renderers);

            Debug.Write("Application container has been configured.");

            progress.Report($"Initializing shell");

            settings.SettingValueChanged += (settingName, value) =>
            {
                if (settingName == "Schema")
                {
                    SetColorSchema((ColorSchema)value);
                }
                else if (settingName == "Language")
                {
                    SetLanguage((string)value);
                }
            };
        }
    }
}
