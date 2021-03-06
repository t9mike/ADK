﻿using ADK;
using Planetarium.Calculators;
using Planetarium.Config;
using Planetarium.Objects;
using Planetarium.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Planetarium.ViewModels
{
    public class MainVM : ViewModelBase
    {
        private readonly ISky sky;
        private readonly ISkyMap map;
        private readonly ISettings settings;

        public bool FullScreen { get; private set; }
        public string MapEquatorialCoordinatesString { get; private set; }
        public string MapHorizontalCoordinatesString { get; private set; }
        public string MapConstellationNameString { get; private set; }
        public string MapViewAngleString { get; private set; }
        public string DateString { get; private set; }

        public Command<Key> MapKeyDownCommand { get; private set; }
        public Command<int> ZoomCommand { get; private set; }
        public Command<PointF> MapDoubleClickCommand { get; private set; }
        public Command<PointF> MapRightClickCommand { get; private set; }
        public Command SetDateCommand { get; private set; }
        public Command SelectLocationCommand { get; private set; }
        public Command SearchObjectCommand { get; private set; }
        public Command<PointF> CenterOnPointCommand { get; private set; }
        public Command<CelestialObject> GetObjectInfoCommand { get; private set; }
        public Command<CelestialObject> GetObjectEphemerisCommand { get; private set; }
        public Command CalculatePhenomenaCommand { get; private set; }
        public Command<CelestialObject> MotionTrackCommand { get; private set; }
        public Command<CelestialObject> LockOnObjectCommand { get; private set; }
        public Command<PointF> MeasureToolCommand { get; private set; }
        public Command<CelestialObject> CenterOnObjectCommand { get; private set; }
        public Command ClearObjectsHistoryCommand { get; private set; }
        public Command ChangeSettingsCommand { get; private set; }

        private ContextMenuItemsConfig contextMenuItemsConfig;
        public ObservableCollection<MenuItemVM> ContextMenuItems { get; private set; } = new ObservableCollection<MenuItemVM>();
        public ObservableCollection<MenuItemVM> SelectedObjectsMenuItems { get; private set; } = new ObservableCollection<MenuItemVM>();
        public string SelectedObjectName { get; private set; }

        public ObservableCollection<ToolbarItem> ToolbarItems { get; private set; } = new ObservableCollection<ToolbarItem>();

        public class MenuItemVM
        {
            public bool IsCheckable { get; set; } = false;
            public bool IsChecked { get; set; }
            public bool IsEnabled { get; set; } = true;
            public string Header { get; set; }
            public ICommand Command { get; set; }
            public object CommandParameter { get; set; }
            public ObservableCollection<MenuItemVM> SubItems { get; set; }
        }

        public class ObservableUniqueItemsCollection<T> : ObservableCollection<T>
        {
            public new void Add(T item)
            {
                base.Add(item);
            }
        }

        public PointF SkyMousePosition
        {
            set
            {
                var hor = map.Projection.Invert(value);
                var eq = hor.ToEquatorial(sky.Context.GeoLocation, sky.Context.SiderealTime);

                MapEquatorialCoordinatesString = eq.ToString();
                MapHorizontalCoordinatesString = hor.ToString();
                MapConstellationNameString = Constellations.FindConstellation(eq, sky.Context.JulianDay);
                MapViewAngleString = Formatters.AngularDiameter.Format(map.ViewAngle);

                NotifyPropertyChanged(
                    nameof(MapEquatorialCoordinatesString),
                    nameof(MapHorizontalCoordinatesString),
                    nameof(MapConstellationNameString),
                    nameof(MapViewAngleString));
            }
        }

        public bool DateTimeSync
        {
            get { return sky.DateTimeSync; }
            set { sky.DateTimeSync = value; }
        }

        public MainVM(ISky sky, ISkyMap map, ISettings settings, ToolbarButtonsConfig toolbarButtonsConfig, ContextMenuItemsConfig contextMenuItemsConfig)
        {
            this.sky = sky;
            this.map = map;
            this.settings = settings;

            sky.Calculate();

            MapKeyDownCommand = new Command<Key>(MapKeyDown);
            ZoomCommand = new Command<int>(Zoom);
            MapDoubleClickCommand = new Command<PointF>(MapDoubleClick);
            MapRightClickCommand = new Command<PointF>(MapRightClick);
            SetDateCommand = new Command(SetDate);
            SelectLocationCommand = new Command(SelectLocation);
            SearchObjectCommand = new Command(SearchObject);
            CenterOnPointCommand = new Command<PointF>(CenterOnPoint);
            GetObjectInfoCommand = new Command<CelestialObject>(GetObjectInfo);
            GetObjectEphemerisCommand = new Command<CelestialObject>(GetObjectEphemeris);
            CalculatePhenomenaCommand = new Command(CalculatePhenomena);
            LockOnObjectCommand = new Command<CelestialObject>(LockOnObject);
            CenterOnObjectCommand = new Command<CelestialObject>(CenterOnObject);
            ClearObjectsHistoryCommand = new Command(ClearObjectsHistory);
            ChangeSettingsCommand = new Command(ChangeSettings);

            sky.Context.ContextChanged += Sky_ContextChanged;
            sky.Calculated += () => map.Invalidate();
            sky.DateTimeSyncChanged += () => NotifyPropertyChanged(nameof(DateTimeSync));
            map.SelectedObjectChanged += Map_SelectedObjectChanged;
            map.ViewAngleChanged += Map_ViewAngleChanged;
            settings.SettingValueChanged += (s, v) => map.Invalidate();

            Sky_ContextChanged();
            Map_SelectedObjectChanged(map.SelectedObject);
            Map_ViewAngleChanged(map.ViewAngle);
         
            var toolbarGroups = toolbarButtonsConfig.GroupBy(b => b.Group);
            foreach (var group in toolbarGroups)
            {
                foreach (var button in group)
                {
                    ToolbarItems.Add(button);
                }
                ToolbarItems.Add(new ToolbarSeparator());
            }

            this.contextMenuItemsConfig = contextMenuItemsConfig;            
        }

        private void Sky_ContextChanged()
        {
            var months = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedMonthNames.Take(12).ToArray();
            var d = new Date(sky.Context.JulianDay, sky.Context.GeoLocation.UtcOffset);
            DateString = $"{(int)d.Day:00} {months[d.Month - 1]} {d.Year} {d.Hour:00}:{d.Minute:00}:{d.Second:00}";
            NotifyPropertyChanged(nameof(DateString));
        }

        private void Map_ViewAngleChanged(double viewAngle)
        {
            MapViewAngleString = Formatters.AngularDiameter.Format(map.ViewAngle);
            NotifyPropertyChanged(nameof(MapViewAngleString));
        }

        private void Map_SelectedObjectChanged(CelestialObject body)
        {
            if (body != null)
            {
                SelectedObjectName = body.Names.First();

                if (!SelectedObjectsMenuItems.Any())
                {          
                    SelectedObjectsMenuItems.Add(new MenuItemVM()
                    {
                        Header = "Clear all",
                        Command = ClearObjectsHistoryCommand
                    });                    
                    SelectedObjectsMenuItems.Add(null);
                }

                var existingItem = SelectedObjectsMenuItems.FirstOrDefault(i => body.Equals(i?.CommandParameter));
                if (existingItem != null)
                {
                    SelectedObjectsMenuItems.Remove(existingItem);
                }

                SelectedObjectsMenuItems.Insert(2, new MenuItemVM()
                {
                    Command = CenterOnObjectCommand,
                    CommandParameter = body,
                    Header = SelectedObjectName
                });

                // 10 items of history + "clear all" + separator
                if (SelectedObjectsMenuItems.Count > 13)
                {
                    SelectedObjectsMenuItems.RemoveAt(0);
                }
            }
            else
            {
                SelectedObjectName = "<No object>";
            }

            NotifyPropertyChanged(nameof(SelectedObjectName));
        }

        private void Zoom(int delta)
        {
            map.ViewAngle *= Math.Pow(1.1, -delta / 120);
        }

        private void MapKeyDown(Key key)
        {
            // "+" = Zoom In
            if (key == Key.Add)
            {
                Zoom(1);
            }
            // "-" = Zoom Out
            else if (key == Key.Subtract)
            {
                Zoom(-1);
            }
            // "D" = [D]ate
            else if (key == Key.D)
            {
                SetDate();
            }
            // "A" = [A]dd
            else if (key == Key.A)
            {
                sky.Context.JulianDay += 1;
                sky.Calculate();
            }
            // "S" = [S]ubtract
            else if (key == Key.S)
            {
                sky.Context.JulianDay -= 1;
                sky.Calculate();
            }
            // "O" = [O]ptions
            else if (key == Key.O)
            {
                ChangeSettings();
            }
            // "I" = [I]nfo
            else if (key == Key.I)
            {
                GetObjectInfo(map.SelectedObject);
            }
            // "F12" = Full Screen On
            else if (key == Key.F12)
            {
                SetFullScreen(true);
            }
            // "Esc" = Full Screen Off
            else if (key == Key.Escape)
            {
                SetFullScreen(false);
            }
            // "F" = [F]ind
            else if (key == Key.F)
            {
                SearchObject();
            }
            // "E" = [E]phemerides
            else if (key == Key.E)
            {
                GetObjectEphemeris(map.SelectedObject);
            }
            // "P" = [P]henomena
            else if (key == Key.P)
            {
                CalculatePhenomena();
            }
            // "L" = [L]ocation
            else if (key == Key.L)
            {
                SelectLocation();
            }
            // "T" = [T]rack
            //else if (key == Key.T)
            //{
            //    MotionTrack(map.SelectedObject);
            //}
        }

        private void MapDoubleClick(PointF point)
        {
            map.SelectedObject = map.FindObject(point);
            map.Invalidate();
            GetObjectInfo(map.SelectedObject);
        }

        private void MapRightClick(PointF point)
        {
            map.SelectedObject = map.FindObject(point);
            map.Invalidate();

            ContextMenuItems.Clear();
            
            ContextMenuItems.Add(new MenuItemVM()
            {
                Header = "Info",
                Command = MapDoubleClickCommand,
                CommandParameter = point,
                IsEnabled = map.SelectedObject != null
            });

            ContextMenuItems.Add(null);

            ContextMenuItems.Add(new MenuItemVM()
            {
                Header = "Center",
                Command = CenterOnPointCommand,
                CommandParameter = point
            });
            ContextMenuItems.Add(new MenuItemVM()
            {
                Header = "Search object...",
                Command = SearchObjectCommand
            });
            ContextMenuItems.Add(new MenuItemVM()
            {
                Header = "Go to point..."
            });

            ContextMenuItems.Add(null);

            ContextMenuItems.Add(new MenuItemVM()
            {
                Header = "Ephemerides",
                IsEnabled = map.SelectedObject != null && sky.GetEphemerisCategories(map.SelectedObject).Any(),
                Command = GetObjectEphemerisCommand,
                CommandParameter = map.SelectedObject
            });

            // dynamic menu items from plugins
            foreach (var configItem in contextMenuItemsConfig)
            {
                ContextMenuItems.Add(new MenuItemVM()
                {
                    Header = configItem.Text,
                    IsEnabled = configItem.EnabledCondition(),
                    IsCheckable = configItem.CheckedCondition != null,
                    IsChecked = configItem.CheckedCondition != null ? configItem.CheckedCondition() : false,
                    Command = new Command(configItem.Action)
                });
            }
            
            ContextMenuItems.Add(null);

            ContextMenuItems.Add(new MenuItemVM()
            {
                Header = map.LockedObject != null ? (map.SelectedObject != null && map.SelectedObject != map.LockedObject ? "Lock" : "Unlock") : "Lock",
                IsEnabled = map.LockedObject != null || map.SelectedObject != null,
                Command = LockOnObjectCommand,
                CommandParameter = map.SelectedObject
            });
            
            NotifyPropertyChanged(nameof(ContextMenuItems));
        }

        private async void GetObjectEphemeris(CelestialObject body)
        {
            var es = ViewManager.CreateViewModel<EphemerisSettingsVM>();
            es.SelectedBody = body;
            es.JulianDayFrom = sky.Context.JulianDay;
            es.JulianDayTo = sky.Context.JulianDay + 30;
            if (ViewManager.ShowDialog(es) ?? false)
            {
                var tokenSource = new CancellationTokenSource();
                var progress = new Progress<double>();

                ViewManager.ShowProgress("Please wait", "Calculating ephemerides...", tokenSource, progress);

                var ephem = await Task.Run(() => sky.GetEphemerides(
                    es.SelectedBody,
                    es.JulianDayFrom,
                    es.JulianDayTo,
                    es.Step.TotalDays,
                    es.Categories,
                    tokenSource.Token,
                    progress
                ));

                if (!tokenSource.IsCancellationRequested)
                {
                    tokenSource.Cancel();
                    var vm = ViewManager.CreateViewModel<EphemerisVM>();
                    vm.SetData(es.SelectedBody, es.JulianDayFrom, es.JulianDayTo, es.Step, ephem);
                    ViewManager.ShowWindow(vm);
                }
            }
        }

        private async void CalculatePhenomena()
        {
            var ps = ViewManager.CreateViewModel<PhenomenaSettingsVM>();
            ps.JulianDayFrom = sky.Context.JulianDay;
            ps.JulianDayTo = sky.Context.JulianDay + 30;
            if (ViewManager.ShowDialog(ps) ?? false)
            {
                var tokenSource = new CancellationTokenSource();

                ViewManager.ShowProgress("Please wait", "Calculating phenomena...", tokenSource);

                var events = await Task.Run(() => sky.GetEvents(
                        ps.JulianDayFrom,
                        ps.JulianDayTo,
                        ps.Categories,
                        tokenSource.Token));
               
                if (!tokenSource.IsCancellationRequested)
                {
                    tokenSource.Cancel();
                    var vm = ViewManager.CreateViewModel<PhenomenaVM>();
                    vm.SetEvents(events);
                    if (ViewManager.ShowDialog(vm) ?? false)
                    {
                        sky.SetDate(vm.JulianDay);                        
                        if (vm.Body != null) 
                        {
                            map.GoToObject(vm.Body, TimeSpan.Zero);
                        }
                    }
                }
            }    
        }

        private void SearchObject()
        {
            CelestialObject body = ViewManager.ShowSearchDialog();
            if (body != null)
            {
                CenterOnObject(body);
            }
        }

        private void ChangeSettings()
        {
            ViewManager.ShowDialog<SettingsVM>();
        }

        private void CenterOnObject(CelestialObject body)
        {
            if (settings.Get<bool>("Ground") && body.Horizontal.Altitude <= 0)
            {
                if (ViewManager.ShowMessageBox("Question", "The object is under horizon at the moment. Do you want to switch off displaying the ground?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    settings.Set("Ground", false);
                }
                else
                {
                    return;
                }
            }

            if (map.LockedObject != null && map.LockedObject != body)
            {
                if (ViewManager.ShowMessageBox("Question", "The map is locked on different celestial body. Do you want to unlock the map?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    map.LockedObject = null;
                }
                else
                {
                    return;
                }
            }
            
            map.GoToObject(body, TimeSpan.FromSeconds(1));
        }

        private void LockOnObject(CelestialObject body)
        {
            if (map.LockedObject != body)
            {
                map.LockedObject = body;
            }
            else
            {
                map.LockedObject = null;
            }
            map.Invalidate();
        }

        private void ClearObjectsHistory()
        {
            SelectedObjectsMenuItems.Clear();
        }

        private void CenterOnPoint(PointF point)
        {
            map.Center.Set(map.Projection.Invert(point));
            map.Invalidate();
        }

        private void GetObjectInfo(CelestialObject body)
        {
            if (body != null)
            {
                var info = sky.GetInfo(body);
                if (info != null)
                {
                    var vm = new ObjectInfoVM(info);
                    if (ViewManager.ShowDialog(vm) ?? false)
                    {
                        sky.SetDate(vm.JulianDay);
                        map.GoToObject(body, TimeSpan.Zero);
                    }
                }
            }
        }

        private void SetDate()
        {
            double? jd = ViewManager.ShowDateDialog(sky.Context.JulianDay, sky.Context.GeoLocation.UtcOffset);
            if (jd != null)
            {
                sky.SetDate(jd.Value);
            }
        }

        private void SelectLocation()
        {
            var vm = ViewManager.CreateViewModel<LocationVM>();       
            if (ViewManager.ShowDialog(vm) ?? false)
            {
                sky.Context.GeoLocation = new CrdsGeographical(vm.ObserverLocation);
                settings.Set("ObserverLocation", vm.ObserverLocation);
                settings.Save();
                sky.Calculate();
            }            
        }

        private void SetFullScreen(bool isFullScreen)
        {
            FullScreen = isFullScreen;
            NotifyPropertyChanged(nameof(FullScreen));
        }
    }
}
