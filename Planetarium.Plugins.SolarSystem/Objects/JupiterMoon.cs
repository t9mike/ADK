﻿using ADK;
using Planetarium.Types.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planetarium.Objects
{
    public class JupiterMoon : SizeableCelestialObject
    {
        public JupiterMoon(int number)
        {
            Number = number;
        }

        /// <summary>
        /// Apparent equatorial coordinates of the Galilean moon
        /// </summary>
        public CrdsEquatorial Equatorial { get; set; }

        /// <summary>
        /// Planetocentric rectangular coordinates of the Galilean moon
        /// </summary>
        public CrdsRectangular Rectangular { get; set; }

        /// <summary>
        /// Planetocentric rectangular coordinates of the Galilean moon, as seen from Sun
        /// </summary>
        public CrdsRectangular RectangularS { get; set; }

        /// <summary>
        /// Longitude of central meridian
        /// </summary>
        public double CM { get; set; }

        /// <summary>
        /// Name of the Galilean moon
        /// </summary>
        public string Name => Text.Get($"JupiterMoon.{Number}.Name");

        /// <summary>
        /// Name of moon shadow
        /// </summary>
        public string ShadowName => Text.Get($"JupiterMoon.{Number}.Shadow");

        /// <summary>
        /// Number of the Galilean moon (1 to 4)
        /// </summary>
        public int Number { get; private set; }

        /// <summary>
        /// Apparent magnitude
        /// </summary>
        public float Magnitude { get; set; }

        /// <summary>
        /// Gets Galilean moon names
        /// </summary>
        public override string[] Names => new[] { Name };

        public bool IsEclipsedByJupiter
        {
            get
            {
                return
                    RectangularS.Z > 0 && RectangularS.X * RectangularS.X + RectangularS.Y * RectangularS.Y * 1.14784224788 <= 1.1;
            }
        }
    }
}
