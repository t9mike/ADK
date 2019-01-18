﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADK
{
    /// <summary>
    /// Contains methods for calculating appearance parameters of celestial bodies
    /// </summary>
    public static class Appearance
    {
        /// <summary>
        /// Gets geocentric elongation angle of the celestial body
        /// </summary>
        /// <param name="sun">Ecliptical geocentrical coordinates of the Sun</param>
        /// <param name="body">Ecliptical geocentrical coordinates of the body</param>
        /// <returns>Geocentric elongation angle, in degrees, from -180 to 180.
        /// Negative sign means western elongation, positive eastern.
        /// </returns>
        /// <remarks>
        /// AA(II), formula 48.2
        /// </remarks>
        // TODO: tests
        public static double Elongation(CrdsEcliptical sun, CrdsEcliptical body)
        {
            double beta = Angle.ToRadians(body.Beta);
            double lambda = Angle.ToRadians(body.Lambda);
            double lambda0 = Angle.ToRadians(sun.Lambda);

            double s = sun.Lambda;
            double b = body.Lambda;

            if (Math.Abs(s - b) > 180)
            {
                if (s < b)
                {
                    s += 360;
                }
                else
                {
                    b += 360;
                }
            }

            return Math.Sign(b - s) * Angle.ToDegrees(Math.Acos(Math.Cos(beta) * Math.Cos(lambda - lambda0)));
        }

        /// <summary>
        /// Calculates phase angle of celestial body
        /// </summary>
        /// <param name="psi">Geocentric elongation of the body.</param>
        /// <param name="R">Distance Earth-Sun, in any units</param>
        /// <param name="Delta">Distance Earth-body, in the same units</param>
        /// <returns>Phase angle, in degrees, from 0 to 180</returns>
        /// <remarks>
        /// AA(II), formula 48.3.
        /// </remarks>
        /// TODO: tests
        public static double PhaseAngle(double psi, double R, double Delta)
        {
            psi = Angle.ToRadians(Math.Abs(psi));
            double phaseAngle = Angle.ToDegrees(Math.Atan(R * Math.Sin(psi) / (Delta - R * Math.Cos(psi))));
            if (phaseAngle < 0) phaseAngle += 180;
            return phaseAngle;
        }

        /// <summary>
        /// Gets phase value (illuminated fraction of the disk).
        /// </summary>
        /// <param name="phaseAngle">Phase angle of celestial body, in degrees.</param>
        /// <returns>Illuminated fraction of the disk, from 0 to 1.</returns>
        /// <remarks>
        /// AA(II), formula 48.1
        /// </remarks>
        // TODO: tests
        public static double Phase(double phaseAngle)
        {
            return (1 + Math.Cos(Angle.ToRadians(phaseAngle))) / 2;
        }


        // TODO: not finished yet
        public static RTS RiseTransitSet(CrdsEquatorial[] eq, CrdsGeographical location, double deltaT, double theta0, double h0)
        {
            if (eq.Length != 3)
                throw new ArgumentException("Number of equatorial coordinates in the array should be equal to 3.");

            double[] alpha = new double[3];
            double[] delta = new double[3];
            for (int i = 0; i < 3; i++)
            {
                alpha[i] = eq[i].Alpha;
                delta[i] = eq[i].Delta;
            }

            double cosH0 = (Math.Sin(Angle.ToRadians(h0)) - Math.Sin(Angle.ToRadians(location.Latitude)) * Math.Sin(Angle.ToRadians(delta[1]))) /
                (Math.Cos(Angle.ToRadians(location.Latitude)) * Math.Cos(Angle.ToRadians(delta[1])));

            if (Math.Abs(cosH0) >= 1)
            {
                throw new Exception("Circumpolar");
            }

            double H0 = Angle.ToDegrees(Math.Acos(cosH0));

            double[] m = new double[3];

            m[0] = (alpha[1] + location.Longitude - theta0) / 360;
            m[1] = m[0] - H0 / 360;
            m[2] = m[0] + H0 / 360;

            for (int i = 0; i < 3; i++)
            {
                if (m[i] >= 1) m[i] -= 1;
                if (m[i] < 0) m[i] += 1;
            }

            Angle.NormalizeAngles(alpha);
            Angle.NormalizeAngles(delta);

            double[] x = new double[] { 0, 0.5, 1 };

            for (int i = 0; i < 3; i++)
            {
                double deltaM;

                do
                {
                    double theta = Angle.To360(theta0 + 360.985647 * m[i]) - 180;

                    double n = m[i] + deltaT / 86400;

                    double a = Angle.To360(Interpolation.Lagrange(x, alpha, n));
                    double d = Interpolation.Lagrange(x, delta, n);

                    var eq0 = new CrdsEquatorial(a, d);

                    double H = Angle.To360(Coordinates.HourAngle(theta, location.Longitude, a));
                    if (H > 180) H -= 360;


                    var h = eq0.ToHorizontal(location, theta);



                    // transit
                    if (i == 0)
                    {
                        deltaM = -H / 360;
                    }
                    else
                    {
                        deltaM = (h.Altitude - h0) / (360 * Math.Cos(Angle.ToRadians(d)) * Math.Cos(Angle.ToRadians(location.Latitude) * Math.Sin(Angle.ToRadians(H))));
                    }

                    m[i] += deltaM;

                }
                while (Math.Abs(deltaM * 24 * 60) > 1);

            }

            return new RTS()
            {
                Transit = m[0],
                Rise = m[1],
                Set = m[2]
            };
        }

        public static RTS RiseTransitSet2(CrdsEquatorial[] eq, CrdsGeographical location, double deltaT, double theta0, double h0)
        {
            if (eq.Length != 3)
                throw new ArgumentException("Number of equatorial coordinates in the array should be equal to 3.");

            double[] alpha = new double[3];
            double[] delta = new double[3];
            for (int i = 0; i < 3; i++)
            {
                alpha[i] = eq[i].Alpha;
                delta[i] = eq[i].Delta;
            }

            Angle.NormalizeAngles(alpha);
            Angle.NormalizeAngles(delta);

            double[] x = new double[] { 0, 0.5, 1 };
           
            List<CrdsHorizontal> hor = new List<CrdsHorizontal>();

            for (int i=0; i<=24; i++)
            {
                double a = Interpolation.Lagrange(x, alpha, i / 24.0);
                double d = Interpolation.Lagrange(x, delta, i / 24.0);

                CrdsEquatorial eqP = new CrdsEquatorial(Angle.To360(a), d);
                CrdsHorizontal h = eqP.ToHorizontal(location, Angle.To360(theta0 + i / 24.0 * 360));
                h.Altitude += h0;
                hor.Add(h);                
            }

            var maxH = hor.OrderBy(f => f.Altitude).Last();
            var minH = hor.OrderBy(f => f.Altitude).First();

            int maxAltIndex = hor.IndexOf(maxH);
            int minAltIndex = hor.IndexOf(minH);


            if (maxAltIndex + 1 > 24)
            {
                maxAltIndex -= 1;
            }
            if (maxAltIndex - 1 < 0)
            {
                maxAltIndex += 1;
            }

            var result = new RTS();

            // transit:
            result.Transit = Double.NaN;
            
            double n = SolveParabola(Math.Sin(Angle.ToRadians(hor[maxAltIndex - 1].Azimuth)), Math.Sin(Angle.ToRadians(hor[maxAltIndex].Azimuth)), Math.Sin(Angle.ToRadians(hor[maxAltIndex + 1].Azimuth)));
            if (!Double.IsNaN(n))
            {
                result.Transit = (maxAltIndex - 1 + n / 24.0) / 24.0;  
            }

            return result;
        }

        private static double SolveParabola(double y1, double y2, double y3)
        {
            double a = 2 * y1 - 4 * y2 + 2 * y3;
            double b = -3 * y1 + 4 * y2 - y3;
            double c = y1;

            double D = Math.Sqrt(b * b - 4 * a * c);

            double x1 = (-b - D) / (2 * a);
            double x2 = (-b + D) / (2 * a);

            if (x1 >= 0 && x1 <= 1) return x1;
            if (x2 >= 0 && x2 <= 1) return x2;

            return Double.NaN;
        }
    }
}
