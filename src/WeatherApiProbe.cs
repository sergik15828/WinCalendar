using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WinCalendar
{
    internal static class WeatherApiProbe
    {
        internal static async Task<int> RunAsync(string directory)
        {
            Directory.CreateDirectory(directory); string path = Path.Combine(directory, "weather-api-probe.txt");
            try
            {
                using (WeatherClient client = new WeatherClient())
                {
                    WeatherLocation[] results = await client.SearchAsync("Ереван", "ru", CancellationToken.None);
                    if (results.Length == 0) throw new InvalidOperationException("Russian city search returned no results");
                    WeatherSnapshot weather = await client.FetchAsync(results[0], CancellationToken.None);
                    if (weather.Days.Length != 3) throw new InvalidOperationException("Missing three-day forecast");
                    File.WriteAllText(path, "PASS actual .NET weather client, Russian Yerevan search and current/three-day forecast\r\nSearchResults=" + results.Length + "\r\nResolvedCity=" + results[0].ToString());
                    return 0;
                }
            }
            catch (Exception error)
            {
                File.WriteAllText(path, "FAIL " + error.GetType().Name + ": " + error.Message + (error.InnerException == null ? "" : "\r\nInner=" + error.InnerException.GetType().Name + ": " + error.InnerException.Message)); return 1;
            }
        }
    }
}
