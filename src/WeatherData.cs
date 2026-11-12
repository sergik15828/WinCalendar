using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace WinCalendar
{
    internal sealed class WeatherLocation
    {
        public string Name { get; set; }
        public string Region { get; set; }
        public string Country { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        internal bool Valid { get { return !String.IsNullOrWhiteSpace(Name) && !Double.IsNaN(Latitude) && !Double.IsNaN(Longitude) && Math.Abs(Latitude) <= 90 && Math.Abs(Longitude) <= 180; } }
        internal string Key { get { return Latitude.ToString("R", CultureInfo.InvariantCulture) + "," + Longitude.ToString("R", CultureInfo.InvariantCulture); } }
        public override string ToString() { return String.Join(", ", new[] { Name, Region, Country }.Where(s => !String.IsNullOrEmpty(s)).Distinct()); }
    }
    internal sealed class WeatherDay
    {
        public DateTime Date { get; set; }
        public double Low { get; set; }
        public double High { get; set; }
        public int Code { get; set; }
    }
    internal sealed class WeatherSnapshot
    {
        public string LocationKey { get; set; }
        public DateTime ObservedUtc { get; set; }
        public double Temperature { get; set; }
        public double FeelsLike { get; set; }
        public double Humidity { get; set; }
        public double Wind { get; set; }
        public int Code { get; set; }
        public bool IsDay { get; set; }
        public WeatherDay[] Days { get; set; }
    }
    internal sealed class WeatherView
    {
        internal WeatherLocation Location;
        internal WeatherSnapshot Snapshot;
        internal bool Loading, Failed;
        internal bool Stale { get { return Snapshot != null && (Failed || DateTime.UtcNow - Snapshot.ObservedUtc > TimeSpan.FromMinutes(45)); } }
        internal bool Usable { get { return Snapshot != null && DateTime.UtcNow - Snapshot.ObservedUtc <= TimeSpan.FromDays(1); } }
        internal string Temperature { get { return Usable ? Degrees(Snapshot.Temperature) : "—"; } }
        internal static string Degrees(double number) { return Math.Round(number, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture) + "°"; }
        internal string Status { get { return Location == null ? UiText.Get("Выбрать город") : Loading && Snapshot == null ? UiText.Get("Загрузка…") : !Usable ? UiText.Get("Нет данных") : Stale ? UiText.Get("Устарело") : Location.Name; } }
    }
    internal sealed class WeatherClient : IDisposable
    {
        private readonly HttpClient http;
        internal WeatherClient(HttpMessageHandler handler = null)
        {
            // This csc-built Framework app can inherit legacy TLS defaults.
            // Scope modern TLS to weather without changing Google networking or certificate validation.
            http = new HttpClient(handler ?? new HttpClientHandler { AllowAutoRedirect = false, SslProtocols = System.Security.Authentication.SslProtocols.Tls12 })
                { Timeout = TimeSpan.FromSeconds(12), MaxResponseContentBufferSize = 262144 };
        }
        internal async Task<WeatherLocation[]> SearchAsync(string query, string language, CancellationToken cancellation)
        {
            string body = await ReadAsync("https://geocoding-api.open-meteo.com/v1/search?name=" + Uri.EscapeDataString(query.Trim()) + "&count=8&language=" + (language == "en" ? "en" : "ru") + "&format=json", cancellation).ConfigureAwait(false);
            return ParseLocations(body);
        }
        internal static WeatherLocation[] ParseLocations(string body)
        {
            Dictionary<string, object> root = Json.Object(body); object list;
            if (!root.TryGetValue("results", out list)) return new WeatherLocation[0];
            return ((System.Collections.IEnumerable)list).Cast<object>().Select(value =>
            {
                Dictionary<string, object> item = (Dictionary<string, object>)value;
                return new WeatherLocation { Name = Json.Text(item, "name"), Region = Json.Text(item, "admin1"), Country = Json.Text(item, "country"), Latitude = Number(item, "latitude"), Longitude = Number(item, "longitude") };
            }).Where(item => item.Valid).Take(8).ToArray();
        }
        internal async Task<WeatherSnapshot> FetchAsync(WeatherLocation location, CancellationToken cancellation)
        {
            string url = "https://api.open-meteo.com/v1/forecast?latitude=" + location.Latitude.ToString("R", CultureInfo.InvariantCulture)
                + "&longitude=" + location.Longitude.ToString("R", CultureInfo.InvariantCulture)
                + "&current=temperature_2m,apparent_temperature,relative_humidity_2m,wind_speed_10m,weather_code,is_day"
                + "&daily=temperature_2m_max,temperature_2m_min,weather_code&forecast_days=3&temperature_unit=celsius&wind_speed_unit=ms&timezone=auto&timeformat=unixtime";
            return Parse(await ReadAsync(url, cancellation).ConfigureAwait(false), location.Key);
        }
        private async Task<string> ReadAsync(string url, CancellationToken cancellation)
        {
            using (HttpResponseMessage response = await http.GetAsync(url, cancellation).ConfigureAwait(false))
            { response.EnsureSuccessStatusCode(); return await response.Content.ReadAsStringAsync().ConfigureAwait(false); }
        }
        internal static WeatherSnapshot Parse(string body, string key)
        {
            Dictionary<string, object> root = Json.Object(body), current = (Dictionary<string, object>)root["current"];
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            WeatherSnapshot result = new WeatherSnapshot { LocationKey = key, ObservedUtc = epoch.AddSeconds(Number(current, "time")), Temperature = Number(current, "temperature_2m"), FeelsLike = Number(current, "apparent_temperature"), Humidity = Number(current, "relative_humidity_2m"), Wind = Number(current, "wind_speed_10m"), Code = (int)Number(current, "weather_code"), IsDay = Number(current, "is_day") == 1, Days = new WeatherDay[0] };
            if (result.Temperature < -100 || result.Temperature > 70 || result.Humidity < 0 || result.Humidity > 100 || result.Wind < 0 || result.Wind > 150 || result.Code < 0 || result.Code > 99) throw new FormatException("Invalid weather data");
            object daily;
            if (root.TryGetValue("daily", out daily))
            {
                var day = (Dictionary<string, object>)daily;
                double[] dates = Values(day, "time"), low = Values(day, "temperature_2m_min"), high = Values(day, "temperature_2m_max"), codes = Values(day, "weather_code");
                int count = Math.Min(3, Math.Min(Math.Min(dates.Length, low.Length), Math.Min(high.Length, codes.Length)));
                double offset = Number(root, "utc_offset_seconds");
                result.Days = Enumerable.Range(0, count).Select(i => new WeatherDay { Date = epoch.AddSeconds(dates[i] + offset).Date, Low = low[i], High = high[i], Code = (int)codes[i] }).ToArray();
            }
            return result;
        }
        private static double[] Values(Dictionary<string, object> data, string key)
        { return ((System.Collections.IEnumerable)data[key]).Cast<object>().Select(value => ConvertNumber(value)).ToArray(); }
        private static double Number(Dictionary<string, object> data, string key) { return ConvertNumber(data[key]); }
        private static double ConvertNumber(object value)
        { if (value == null || value is bool || value is string) throw new FormatException("Missing weather value"); double number = Convert.ToDouble(value, CultureInfo.InvariantCulture); if (Double.IsNaN(number) || Double.IsInfinity(number)) throw new FormatException("Invalid weather value"); return number; }
        public void Dispose() { http.Dispose(); }
    }
    internal sealed class WeatherService : IDisposable
    {
        private readonly WeatherClient client;
        private readonly string cache;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private bool enabled, running, disposed;
        private int revision;
        private DateTime next;
        internal WeatherView View { get; private set; }
        internal event Action Changed;
        internal WeatherService(string cachePath, WeatherClient source = null) { cache = cachePath; client = source ?? new WeatherClient(); View = new WeatherView(); }
        internal void Configure(AppPreferences preferences)
        {
            WeatherLocation location = preferences.WeatherCity;
            if (enabled == preferences.WeatherEnabled && (location == null ? View.Location == null : View.Location != null && location.Key == View.Location.Key && location.Name == View.Location.Name)) return;
            enabled = preferences.WeatherEnabled; revision++; next = DateTime.MinValue;
            View = new WeatherView { Location = location, Loading = enabled && location != null };
            if (enabled && location != null && File.Exists(cache))
                try
                {
                    byte[] plain = ProtectedData.Unprotect(File.ReadAllBytes(cache), null, DataProtectionScope.CurrentUser);
                    try { WeatherSnapshot saved = new JavaScriptSerializer().Deserialize<WeatherSnapshot>(Encoding.UTF8.GetString(plain)); if (saved != null && saved.LocationKey == location.Key) View.Snapshot = saved; }
                    finally { Array.Clear(plain, 0, plain.Length); }
                }
                catch (Exception) { }
            Publish();
        }
        internal async Task RefreshAsync()
        {
            if (disposed || !enabled || View.Location == null || running || DateTime.UtcNow < next) return;
            running = true; int request = revision; WeatherLocation location = View.Location;
            View.Loading = true; Publish();
            try
            {
                WeatherSnapshot snapshot = await client.FetchAsync(location, lifetime.Token);
                if (disposed || request != revision) return;
                if (snapshot.ObservedUtc > DateTime.UtcNow.AddMinutes(30) || snapshot.ObservedUtc < DateTime.UtcNow.AddHours(-3)) throw new FormatException("Outdated weather response");
                View.Snapshot = snapshot; View.Failed = false; next = DateTime.UtcNow.AddMinutes(15);
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(cache)); byte[] plain = Encoding.UTF8.GetBytes(Json.Serialize(snapshot));
                    try { byte[] encrypted = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser); string temp = cache + ".tmp"; File.WriteAllBytes(temp, encrypted); if (File.Exists(cache)) File.Replace(temp, cache, null); else File.Move(temp, cache); }
                    finally { Array.Clear(plain, 0, plain.Length); }
                }
                catch (Exception) { }
            }
            catch (Exception) { if (!disposed && request == revision) { View.Failed = true; next = DateTime.UtcNow.AddMinutes(5); } }
            finally { running = false; if (!disposed && request == revision) { View.Loading = false; Publish(); } }
        }
        private void Publish() { if (Changed != null) Changed(); }
        public void Dispose() { disposed = true; lifetime.Cancel(); client.Dispose(); lifetime.Dispose(); }
    }
}
