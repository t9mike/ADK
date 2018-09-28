using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace ADK.Tests
{
    [TestClass]
    public class PlanerCalculatorTests : TestClassBase
    {
        /// <summary>
        /// Number of seconds in 1 degree of arc.
        /// </summary>
        private const double SECONDS_IN_DEGREE = 3600;

        /// <summary>
        /// The data to test VSOP87 implementation are taken from <see href="ftp://ftp.imcce.fr/pub/ephem/planets/vsop87/vsop87.chk" />.
        /// </summary>
        private static List<VSOP87DTestData> testData = new List<VSOP87DTestData>();

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            NumberFormatInfo numericFormat = new NumberFormatInfo();
            numericFormat.NumberDecimalSeparator = ".";

            var lines = ReadLinesFromResource("ADK.Tests.Data.VSOP87.chk", Encoding.ASCII).ToList();

            Regex regexHeader = new Regex(@"^VSOP87D\s+([\w]+)\s*JD([\w\d\.]+).*$");
            Regex regexValues = new Regex(@"^\s*l\s+([-\d.]+)\s+rad\s+b\s+([-\d.]+)\s+rad\s+r\s+([\d.]+)\s+au\s*$");

            for (int i = 0; i < lines.Count; i++)
            {
                string header = lines[i].Trim();
                if (header.StartsWith("VSOP87D"))
                {
                    string[] chunks = regexHeader.Match(header).Groups.Select(g => g.Value).ToArray();

                    Planet planet = GetPlanet(FirstCharToUpper(chunks[1].ToLower()));
                    double jd = Double.Parse(chunks[2], numericFormat);

                    i++;
                    string[] values = regexValues.Match(lines[i]).Groups.Select(g => g.Value).ToArray();

                    double L = AstroUtils.ToDegree(Double.Parse(values[1], numericFormat));
                    double B = AstroUtils.ToDegree(Double.Parse(values[2], numericFormat));
                    double R = Double.Parse(values[3], numericFormat);

                    testData.Add(new VSOP87DTestData(planet, jd, L, B, R));
                }
            }
        }

        [TestMethod]
        public void GetPlanetCoordinatesLP()
        {
            // Example 32.a from AA
            {
                CrdsHeliocentrical crds = PlanetCalculator.GetPlanetCoordinates(Planet.Venus, 2448976.5, highPrecision: false);
                Assert.AreEqual(26.11428, crds.L, 1e-5);
                Assert.AreEqual(-2.62070, crds.B, 1e-5);
                Assert.AreEqual(0.724603, crds.R, 1e-6);
            }

            // Test low-precision implementation from AA2 book
            foreach (VSOP87DTestData testValue in testData)
            {
                CrdsHeliocentrical crds = PlanetCalculator.GetPlanetCoordinates(testValue.Planet, testValue.JDE, highPrecision: false);

                double deltaL = Math.Abs(testValue.L - crds.L) * SECONDS_IN_DEGREE;
                double deltaB = Math.Abs(testValue.B - crds.B) * SECONDS_IN_DEGREE;
                double deltaR = Math.Abs(testValue.R - crds.R);

                // difference in L should be less than 4" of arc
                Assert.IsTrue(deltaL < 4);

                // difference in B should be less than 4" of arc
                Assert.IsTrue(deltaB < 4);

                // difference in R should be less than 1e-3 AU
                Assert.IsTrue(deltaR < 1e-3);
            }
        }

        [TestMethod]
        public void GetPlanetCoordinatesHP()
        {
            // Test high-precision implementation from original VSOP87 theory.
            foreach (VSOP87DTestData testValue in testData)
            {
                CrdsHeliocentrical crds = PlanetCalculator.GetPlanetCoordinates(testValue.Planet, testValue.JDE, highPrecision: true);

                double deltaL = Math.Abs(testValue.L - crds.L) * SECONDS_IN_DEGREE;
                double deltaB = Math.Abs(testValue.B - crds.B) * SECONDS_IN_DEGREE;
                double deltaR = Math.Abs(testValue.R - crds.R);

                // difference in L should be less than 1" of arc
                Assert.IsTrue(deltaL < 1);

                // difference in B should be less than 1" of arc
                Assert.IsTrue(deltaB < 1);

                // difference in R should be less than 1e-5 AU
                Assert.IsTrue(deltaR < 1e-5);
            }
        }

        /// <summary>
        /// Converts first char of string to uppercase
        /// </summary>
        /// <param name="input">Input string</param>
        /// <returns>Returns modified string with first uppercase character</returns>
        private static string FirstCharToUpper(string input)
        {
            switch (input)
            {
                case null: throw new ArgumentNullException(nameof(input));
                case "": throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input));
                default: return input.First().ToString().ToUpper() + input.Substring(1);
            }
        }

        /// <summary>
        /// Gets <see cref="Planet"/> enum value from its string value
        /// </summary>
        /// <param name="value">String value to be prsed as enum</param>
        /// <returns>Enum value</returns>
        private static Planet GetPlanet(string value)
        {
            return (Planet)Enum.Parse(typeof(Planet), value);
        }

        /// <summary>
        /// Used to store test data to validate VSOP87 implementation.
        /// </summary>
        private class VSOP87DTestData
        {
            /// <summary>
            /// Planet
            /// </summary>
            public Planet Planet { get; private set; }

            /// <summary>
            /// Julian Ephemeris Day
            /// </summary>
            public double JDE { get; private set; }

            /// <summary>
            /// Expected heliocentrical longitude
            /// </summary>
            public double L { get; private set; }

            /// <summary>
            /// Expected heliocentrical latitude
            /// </summary>
            public double B { get; private set; }

            /// <summary>
            /// Expected heliocentrical radius vector
            /// </summary>
            public double R { get; private set; }

            /// <summary>
            /// Creates new test data
            /// </summary>
            /// <param name="planet">Planet</param>
            /// <param name="jde">Julian Ephemeris Day</param>
            /// <param name="L">L value</param>
            /// <param name="B">B value</param>
            /// <param name="R">R value</param>
            public VSOP87DTestData(Planet planet, double jde, double L, double B, double R)
            {
                Planet = planet;
                JDE = jde;
                this.L = L;
                this.B = B;
                this.R = R;
            }
        }
    }
}