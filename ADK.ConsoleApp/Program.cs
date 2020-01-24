using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace ADK.ConsoleApp
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            TestSeasons();
        }

        private static void TestSeasons()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<table border=1 cellspacing=3 cellpadding=3><tr><th>Year</th><th>Season</th><th>Local</th><th>UTC</th></tr>");
            foreach (int year in new[] { 2019, 2020, 2021 })
            {
                foreach (Season season in Enum.GetValues(typeof(Season)))
                {
                    var adate = SolarEphem.SeasonDate(year, season);
                    var date = adate.ToDateTimeUTC();
                    Console.WriteLine($"{year} {season}: {date} UTC {date.ToLocalTime()} Local");
                    sb.AppendLine($"<tr><td>{year}</td><td>{season}</td><td>{date.ToLocalTime()}</td><td>{date}</td></tr>");
                }
            }
            sb.AppendLine("</table>");
            File.WriteAllText("/Users/mmuegel/Mike/Projects/Sundial App/Tests/ADK-Equinox.html",
                sb.ToString());
        }
    }
}
