﻿using ADK.Demo.Calculators;
using ADK.Demo.Renderers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ADK.Demo
{
    public partial class FormMain : Form
    {
        private Sky sky;

        public FormMain()
        {
            InitializeComponent();

            sky = new Sky();
            sky.Calculators.Add(new CelestialGridCalc(sky));
            sky.Calculators.Add(new BordersCalc(sky));
            sky.Calculators.Add(new StarsCalc(sky));
            sky.Calculators.Add(new SolarCalc(sky));
            sky.Calculators.Add(new LunarCalc(sky));

            sky.Initialize();
            sky.Calculate();

            ISkyMap map = new SkyMap();
            map.Renderers.Add(new BordersRenderer(sky, map));
            map.Renderers.Add(new CelestialGridRenderer(sky, map));
            map.Renderers.Add(new StarsRenderer(sky, map));
            map.Renderers.Add(new SolarSystemRenderer(sky, map));
            //map.Renderers.Add(new GroundRenderer(sky, map));
            map.Initialize();

            skyView.SkyMap = map;
        }

        private void skyView_MouseMove(object sender, MouseEventArgs e)
        {
            var hor = skyView.SkyMap.Projection.Invert(e.Location);

            var eq = hor.ToEquatorial(sky.GeoLocation, sky.SiderealTime);

            // precessional elements for converting from current to B1875 epoch
            var p1875 = Precession.ElementsFK5(sky.JulianDay, Date.EPOCH_B1875);

            // Equatorial coordinates for B1875 epoch
            CrdsEquatorial eq1875 = Precession.GetEquatorialCoordinates(eq, p1875);

            Text = 
                hor.ToString() + " / " +
                eq.ToString() + " / " +
                skyView.SkyMap.ViewAngle + " / " +
                Constellations.FindConstellation(eq1875);
        }

        private void skyView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.D)
            {
                sky.JulianDay += 1;
                sky.Calculate();
                skyView.Invalidate();
            }
            else if (e.KeyCode == Keys.A)
            {
                sky.JulianDay -= 1;
                sky.Calculate();
                skyView.Invalidate();
            }
            else if (e.KeyCode == Keys.S)
            {
                sky.JulianDay = Date.Now.ToJulianEphemerisDay();
                sky.Calculate();
                skyView.Invalidate();
            }
        }
    }
}
