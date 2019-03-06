﻿using System;
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
    [DesignerCategory("code")]
    public partial class SkyView : Control
    {
        private Point pOld;
        private Point pNew;

        public SkyView()
        {
            InitializeComponent();
            DoubleBuffered = true;
            Cursor = Cursors.Cross;
        }

        [Description("Gets or sets ISkyMap object to be rendered in the control.")]
        private ISkyMap mSkyMap = null;
        public ISkyMap SkyMap
        {
            get { return mSkyMap; }
            set
            {
                mSkyMap = value;
                if (mSkyMap != null)
                {
                    mSkyMap.Width = Width;
                    mSkyMap.Height = Height;
                    mSkyMap.OnInvalidate += InvalidateWithDoEvents;
                }
            }
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            if (DesignMode || SkyMap == null)
            {
                pe.Graphics.DrawString("SkyView", SystemFonts.DefaultFont, Brushes.White, 10, 10);
                System.Diagnostics.Trace.WriteLine("OnPaint");
            }
            else
            {
                SkyMap.Render(pe.Graphics);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (SkyMap != null)
            {
                SkyMap.Width = Width;
                SkyMap.Height = Height;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            bool shift = (ModifierKeys & Keys.Shift) != Keys.None;

            if (e.Button == MouseButtons.Left && !shift)
            {
                pOld.X = e.X;
                pOld.Y = e.Y;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            SkyMap.Antialias = true;
            Invalidate();
            pOld = Point.Empty;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            Select();

            if (SkyMap != null)
            {
                bool shift = (ModifierKeys & Keys.Shift) != Keys.None;

                if (e.Button == MouseButtons.Left && !shift)
                {
                    if (pOld == Point.Empty)
                    {
                        pOld = new Point(e.X, e.Y);
                    }

                    pNew.X = e.X;
                    pNew.Y = e.Y;
                    double dx = pNew.X - pOld.X;
                    double dy = pNew.Y - pOld.Y;

                    SkyMap.Antialias = Math.Sqrt(dx * dx + dy * dy) < 30;

                    double maxSize = Math.Max(SkyMap.Width, SkyMap.Height);
                    double f = maxSize / (SkyMap.ViewAngle * 2);

                    if (Math.Abs(SkyMap.Center.Altitude) < 30 || SkyMap.ViewAngle > 80)
                    {
                        SkyMap.Center.Azimuth = (SkyMap.Center.Azimuth - dx / f + 360) % 360;
                    }
                    else
                    {
                        CrdsHorizontal cpNew = SkyMap.Projection.Invert(pNew);
                        CrdsHorizontal cpOld = SkyMap.Projection.Invert(pOld);
                        double da = Math.Abs(cpNew.Azimuth - cpOld.Azimuth);
                        da = Math.Abs(da) * Math.Sign(dx);
                        SkyMap.Center.Azimuth -= da;
                        SkyMap.Center.Azimuth %= 360;
                    }

                    SkyMap.Center.Altitude += dy / f;

                    if (SkyMap.Center.Altitude > 90) SkyMap.Center.Altitude = 90;
                    if (SkyMap.Center.Altitude < -90) SkyMap.Center.Altitude = -90;

                    if (double.IsNaN(SkyMap.Center.Azimuth))
                    {
                        SkyMap.Center.Azimuth = 0;
                    }

                    pOld.X = pNew.X;
                    pOld.Y = pNew.Y;

                    Invalidate();
                }
            }
        }

        public void Zoom(int delta)
        {
            double v = SkyMap.ViewAngle;

            if (delta < 0)
            {
                v *= 1.1;
            }
            else
            {
                v /= 1.1;
            }

            if (v >= 90)
            {
                v = 90;
            }
            if (v < 1.0 / 1024.0)
            {
                v = 1.0 / 1024.0;
            }

            SkyMap.ViewAngle = v;
            Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if (SkyMap != null)
            {
                Zoom(e.Delta);
            }
        }

        private void InvalidateWithDoEvents()
        {
            Invalidate();
            Application.DoEvents();
        }
    }
}
