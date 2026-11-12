using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinCalendar
{
    internal static class WeatherSelfTest
    {
        private static WeatherLocation City(string name = "Berlin", double latitude = 52.52)
        { return new WeatherLocation { Name = name, Country = "Germany", Latitude = latitude, Longitude = 13.405 }; }
        private const string Cities = "{\"results\":[{\"name\":\"Paris\",\"country\":\"France\",\"admin1\":\"Ile-de-France\",\"latitude\":48.85,\"longitude\":2.35},{\"name\":\"Paris\",\"country\":\"United States\",\"latitude\":33.66,\"longitude\":-95.55}]}";
        private static string Body(double temperature = 7.4)
        {
            double time = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            return Json.Serialize(new Dictionary<string, object> {
                { "current", new Dictionary<string, object> { { "time", time }, { "temperature_2m", temperature }, { "apparent_temperature", 4.2 }, { "relative_humidity_2m", 72 }, { "wind_speed_10m", 3.4 }, { "weather_code", 2 }, { "is_day", 1 } } },
                { "utc_offset_seconds", 3600 },
                { "daily", new Dictionary<string, object> { { "time", new[] { 1794438000, 1794524400, 1794610800 } }, { "temperature_2m_min", new[] { 2.1, 1.5, 3.0 } }, { "temperature_2m_max", new[] { 8.4, 9.2, 7.8 } }, { "weather_code", new[] { 2, 61, 3 } } } }
            });
        }
        private static WeatherView Sample(int code = 2, bool day = true)
        {
            WeatherSnapshot snapshot = WeatherClient.Parse(Body(), City().Key); snapshot.Code = code; snapshot.IsDay = day;
            return new WeatherView { Location = City(), Snapshot = snapshot };
        }
        internal static async Task RunAsync(string directory, Action<bool, string> check)
        {
            string language = UiText.Language, folder = Path.Combine(directory, "weather-check-" + Guid.NewGuid().ToString("N"));
            try
            {
                UiText.Language = "ru";
                WeatherLocation[] cities = WeatherClient.ParseLocations("{\"results\":[{\"name\":\"Paris\",\"country\":\"France\",\"admin1\":\"Ile-de-France\",\"latitude\":48.85,\"longitude\":2.35},{\"name\":\"Paris\",\"country\":\"United States\",\"latitude\":33.66,\"longitude\":-95.55}]}");
                check(cities.Length == 2 && cities[0].ToString().Contains("France") && cities[0].Key != cities[1].Key,
                    "City search distinguishes identically named locations by region and country");
                check(WeatherClient.ParseLocations("{}").Length == 0, "An empty city search does not invent a default location");
                WeatherSnapshot parsed = WeatherClient.Parse(Body(), City().Key);
                check(parsed.Temperature == 7.4 && parsed.FeelsLike == 4.2 && parsed.Humidity == 72 && parsed.Wind == 3.4 && parsed.Days.Length == 3 && parsed.Days[0].Date.Hour == 0,
                    "Weather parser retains current details and three local forecast dates");
                bool invalid = false; try { WeatherClient.Parse(Body().Replace("7.4", "null"), City().Key); } catch (FormatException) { invalid = true; }
                check(invalid, "Missing temperatures are rejected instead of displaying a false zero");
                string url = null;
                using (WeatherClient client = new WeatherClient(new Handler((request, token) => { url = request.RequestUri.AbsoluteUri; return Response(Body()); })))
                {
                    await client.FetchAsync(City(), CancellationToken.None);
                    check(url.Contains("latitude=52.52") && url.Contains("wind_speed_unit=ms") && url.Contains("forecast_days=3") && !url.Contains("apikey"),
                        "Weather requests use invariant coordinates, explicit wind units and no API credential");
                }
                string cache = Path.Combine(folder, "weather.bin");
                TaskCompletionSource<HttpResponseMessage> pending = new TaskCompletionSource<HttpResponseMessage>(); int requests = 0;
                using (WeatherService service = new WeatherService(cache, new WeatherClient(new Handler((request, token) => ++requests == 1 ? pending.Task : Response(Body(2.5))))))
                {
                    service.Configure(new AppPreferences { WeatherCity = City() }); Task first = service.RefreshAsync();
                    service.Configure(new AppPreferences { WeatherCity = City("Other city", 48.1) });
                    pending.SetResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Body()) }); await first;
                    check(service.View.Snapshot == null && service.View.Location.Name == "Other city", "An old city's in-flight response cannot overwrite a newly selected city");
                    await service.RefreshAsync();
                    check(service.View.Snapshot != null && service.View.Snapshot.Temperature == 2.5, "New city fetches after the cancelled selection's response settles");
                    await service.RefreshAsync(); check(requests == 2, "Background weather refreshes are throttled rather than fetching on every widget frame");
                }
                using (WeatherService offline = new WeatherService(cache, new WeatherClient(new Handler((request, token) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))))))
                {
                    offline.Configure(new AppPreferences { WeatherCity = City("Other city", 48.1) }); await offline.RefreshAsync();
                    check(offline.View.Usable && offline.View.Stale && offline.View.Snapshot.Temperature == 2.5, "Offline weather retains its own encrypted city cache and explicitly marks it stale");
                    offline.Configure(new AppPreferences { WeatherCity = City("A third city", 39) });
                    check(offline.View.Snapshot == null, "Cached weather from another city is never reused");
                }
                var store = new PreferenceStore(Path.Combine(folder, "preferences.bin")); store.Save(new AppPreferences { WeatherEnabled = false, WeatherCity = City() });
                check(!store.Load().WeatherEnabled && store.Load().WeatherCity.Key == City().Key, "Weather visibility and selected city persist across restarts");
                using (Form browserHost = new Form())
                using (WeatherDetailsPanel browserPanel = new WeatherDetailsPanel(navigateUrl => browserHost.Tag = navigateUrl))
                {
                    browserHost.Controls.Add(browserPanel); browserHost.Show(); browserPanel.SetView(Sample());
                    Button yandex = browserPanel.Controls.OfType<Button>().Single(b => b.Text == UiText.Get("Яндекс Погода")); yandex.PerformClick();
                    string link = browserHost.Tag as string;
                    check(link != null && new Uri(link).Host == "yandex.ru" && link.Contains("lat=52.52") && link.Contains("lon=13.405") && link.Contains("name=Berlin%2C%20Germany"),
                        "Yandex Weather action opens the selected city's escaped coordinates and name without an API key");
                    browserHost.Tag = null; browserPanel.SetView(new WeatherView()); yandex.PerformClick();
                    check(!yandex.Enabled && browserHost.Tag == null, "Yandex navigation waits for a selected city instead of guessing a location");
                }
                using (Form host = new Form { AutoScaleMode = AutoScaleMode.None })
                using (PreferencesEditor settings = SearchEditor())
                {
                    host.Controls.Add(settings); host.ClientSize = settings.NaturalSize; host.Show(); settings.BeginEdit(new AppPreferences()); settings.ShowPage(5);
                    await SearchAsync(settings, "Paris");
                    ListBox results = Results(settings); results.SelectedIndex = 1;
                    settings.ShowPage(0); settings.ShowPage(5); AppPreferences saved = null; settings.SaveRequested += value => saved = value;
                    settings.Controls.OfType<Button>().Single(b => b.Text == UiText.Get("Сохранить")).PerformClick();
                    check(saved != null && saved.WeatherCity != null && saved.WeatherCity.Country == "United States",
                        "City search selection survives tab switches and is collected only when saving preferences");
                    await SearchAsync(settings, "missing"); check(Results(settings).Items.Count == 0 && CalendarPanel.Descendants(settings).OfType<Label>().Any(l => l.Text == UiText.Get("Город не найден. Уточните название.")), "City search displays an explicit no-results state");
                    await SearchAsync(settings, "offline"); check(CalendarPanel.Descendants(settings).OfType<Label>().Any(l => l.Text == UiText.Get("Не удалось найти город. Повторите поиск.")), "City search displays retry guidance after a network error");
                }
                check(WeatherPlacement.PlaceStandalone(new Rectangle(-1920, 0, 1920, 48), new Rectangle[0], 320, 44).Left < -1500
                    && WeatherPlacement.PlaceStandalone(new Rectangle(0, 0, 1920, 48), new[] { new Rectangle(0, 0, 500, 48) }, 320, 44).IsEmpty,
                    "Standalone widget replaces the weather area safely without overlapping taskbar buttons");
                Rectangle previousAnchor = TaskbarAnchor.Prototype, previousHit = TaskbarAnchor.PrototypeHitArea;
                bool previousActive = TaskbarAnchor.ReminderActive, previousMotion = TaskbarAnchor.ReminderMotion;
                try
                {
                    TaskbarAnchor.ReminderActive = TaskbarAnchor.ReminderMotion = true; int clicks = 0;
                    using (WeatherDateWindow widget = new WeatherDateWindow(() => new DateTime(2026, 11, 12), delegate { clicks++; }))
                    {
                        widget.Configure(new AppPreferences { WeatherEnabled = false }); widget.ClientSize = new Size(160, 44);
                        widget.RenderAt(new Rectangle(100, 100, 160, 44));
                        IntPtr point = new IntPtr((60 << 16) | 80);
                        SendMessage(widget.Handle, 0x201, new IntPtr(1), point); SendMessage(widget.Handle, 0x202, IntPtr.Zero, point);
                        check(clicks == 1, "A date click inside the expanded native reminder surface works beyond the original client height");
                    }
                }
                finally { TaskbarAnchor.Prototype = previousAnchor; TaskbarAnchor.PrototypeHitArea = previousHit; TaskbarAnchor.ReminderActive = previousActive; TaskbarAnchor.ReminderMotion = previousMotion; }
                using (CalendarPopup popup = new CalendarPopup(() => new DateTime(2026, 11, 12), delegate { }))
                {
                    popup.ShowAt(new Point(500, 800), Screen.PrimaryScreen.WorkingArea); IntPtr handle = popup.Handle;
                    popup.ShowWeather(Sample());
                    check(popup.Handle == handle && popup.WeatherDetails.Visible && !popup.Panel.Visible && popup.WeatherDetails.AccessibleName.Contains("72%"),
                        "Weather click opens detailed conditions in the existing flyout with accessible values");
                    popup.Hide(); popup.Toggle(new Point(500, 800));
                    check(popup.Visible && popup.Panel.Visible && !popup.WeatherDetails.Visible, "Date click immediately opens calendar after a hidden weather page without an extra click");
                    popup.ShowWeather(Sample()); popup.ShowWeather(Sample()); popup.Toggle(new Point(500, 800));
                    check(popup.Visible && popup.Panel.Visible && !popup.WeatherDetails.Visible, "Closing weather with its own button does not leave it selected for the next date click");
                    popup.ShowWeather(Sample());
                    popup.WeatherDetails.Controls.OfType<Button>().Single(b => b.Text == "Выбрать город").PerformClick();
                    check(popup.PreferencesVisible && popup.PreferencesPage.SelectedPage == 5 && !popup.WeatherDetails.Visible && popup.Handle == handle,
                        "Weather details link directly to the city settings tab without a second window");
                    popup.ShowCalendar(); check(popup.Panel.Visible && !popup.WeatherDetails.Visible && popup.Handle == handle, "Calendar remains reachable after weather settings");
                }
                await CaptureAsync(directory);
            }
            finally { UiText.Language = language; if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        }
        private static Task<HttpResponseMessage> Response(string body) { return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) }); }
        [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr parameter, IntPtr position);
        private static PreferencesEditor SearchEditor()
        {
            return new PreferencesEditor(new WeatherClient(new Handler((request, token) => request.RequestUri.Query.Contains("offline")
                ? Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)) : Response(request.RequestUri.Query.Contains("missing") ? "{}" : Cities))));
        }
        private static ListBox Results(PreferencesEditor editor) { return CalendarPanel.Descendants(editor).OfType<ListBox>().Single(c => c.AccessibleName == UiText.Get("Найденные города")); }
        private static async Task SearchAsync(PreferencesEditor editor, string query)
        {
            CalendarPanel.Descendants(editor).OfType<TextBox>().Single(c => c.AccessibleName == UiText.Get("Поиск города")).Text = query;
            Button search = CalendarPanel.Descendants(editor).OfType<Button>().Single(b => b.Text == UiText.Get("Найти")); search.PerformClick();
            for (int i = 0; i < 200 && !search.Enabled; i++) await Task.Delay(10);
            if (!search.Enabled) throw new InvalidOperationException("Synthetic city search timed out");
        }
        private sealed class Handler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply;
            internal Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> callback) { reply = callback; }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) { return reply(request, cancellationToken); }
        }
        private static async Task CaptureAsync(string directory)
        {
            foreach (int dpi in new[] { 96, 120, 144 })
            foreach (bool dark in new[] { false, true })
            {
                int width = WeatherWidgetRenderer.CombinedWidth * dpi / 96, height = 44 * dpi / 96;
                using (Bitmap sheet = new Bitmap(width * 2, height * 10))
                using (Graphics g = Graphics.FromImage(sheet))
                {
                    g.Clear(dark ? Color.FromArgb(32, 32, 32) : Color.FromArgb(243, 243, 243));
                    for (int column = 0; column < 2; column++)
                    for (int row = 0; row < 10; row++)
                    {
                        UiText.Language = column == 0 ? "ru" : "en";
                        WeatherView view = row == 6 ? new WeatherView() : Sample(row == 3 ? 61 : row == 4 ? 73 : row == 5 ? 95 : row == 2 ? 3 : 0, row != 1);
                        if (row == 7) { view.Snapshot = null; view.Loading = true; }
                        if (row == 8) { view.Failed = true; view.Snapshot.ObservedUtc = DateTime.UtcNow.AddHours(-2); }
                        if (row == 9) { view.Snapshot = null; view.Failed = true; }
                        using (Bitmap frame = WeatherWidgetRenderer.Draw(new DateTime(2026, 11, 12), width, height, dark, false, false, 0, view, true)) g.DrawImageUnscaled(frame, width * column, height * row);
                    }
                    sheet.Save(Path.Combine(directory, "combined-weather-" + (dark ? "dark" : "light") + "-" + dpi + ".png"));
                }
                int panelWidth = 688 * dpi / 96, panelHeight = 380 * dpi / 96;
                int prefsWidth = 720 * dpi / 96, prefsHeight = 460 * dpi / 96;
                using (Bitmap searchSheet = new Bitmap(prefsWidth * 2, prefsHeight * 3))
                using (Graphics searchGraphics = Graphics.FromImage(searchSheet))
                {
                    for (int column = 0; column < 2; column++)
                    for (int row = 0; row < 3; row++)
                    using (Form host = new Form { AutoScaleMode = AutoScaleMode.None })
                    {
                        UiText.Language = column == 0 ? "ru" : "en";
                        using (PreferencesEditor editor = SearchEditor())
                        {
                            host.Controls.Add(editor); host.ClientSize = new Size(prefsWidth, prefsHeight); host.Show(); editor.BeginEdit(new AppPreferences()); editor.SetDpi(dpi, dark); editor.Size = editor.NaturalSize; editor.ShowPage(5);
                            await SearchAsync(editor, row == 0 ? "Paris" : row == 1 ? "missing" : "offline"); if (row == 0) Results(editor).SelectedIndex = 1;
                            using (Bitmap frame = editor.CaptureFrame()) searchGraphics.DrawImageUnscaled(frame, column * frame.Width, row * frame.Height);
                        }
                    }
                    searchSheet.Save(Path.Combine(directory, "weather-search-" + (dark ? "dark" : "light") + "-" + dpi + ".png"));
                }
                using (Bitmap modes = new Bitmap(prefsWidth * 2, prefsHeight * 2))
                using (Graphics modeGraphics = Graphics.FromImage(modes))
                {
                    for (int column = 0; column < 2; column++)
                    for (int row = 0; row < 2; row++)
                    using (Form host = new Form { AutoScaleMode = AutoScaleMode.None })
                    {
                        UiText.Language = column == 0 ? "ru" : "en";
                        using (PreferencesEditor editor = new PreferencesEditor())
                        {
                            host.Controls.Add(editor); host.ClientSize = new Size(prefsWidth, prefsHeight); host.Show(); editor.BeginEdit(new AppPreferences { WeatherCity = City() }); editor.SetDpi(dpi, dark); editor.Size = editor.NaturalSize; editor.ShowPage(5);
                            CheckBox mode = CalendarPanel.Descendants(editor).OfType<CheckBox>().Single(c => c.Text == UiText.Get("Показывать погоду справа от даты"));
                            mode.Checked = false; if (row == 0) mode.Checked = true;
                            using (Bitmap frame = editor.CaptureFrame()) modeGraphics.DrawImageUnscaled(frame, column * frame.Width, row * frame.Height);
                        }
                    }
                    modes.Save(Path.Combine(directory, "weather-modes-" + (dark ? "dark" : "light") + "-" + dpi + ".png"));
                }
                using (Bitmap sheet = new Bitmap(panelWidth * 2, panelHeight * 3))
                using (Graphics g = Graphics.FromImage(sheet))
                {
                    for (int column = 0; column < 2; column++)
                    for (int row = 0; row < 3; row++)
                    using (Form host = new Form { AutoScaleMode = AutoScaleMode.None, ClientSize = new Size(panelWidth, panelHeight) })
                    {
                        UiText.Language = column == 0 ? "ru" : "en";
                        using (WeatherDetailsPanel panel = new WeatherDetailsPanel { Dock = DockStyle.Fill })
                        {
                            host.Controls.Add(panel); host.Show(); panel.SetDpi(dpi, dark); WeatherView view = row == 1 ? new WeatherView() : Sample();
                            if (row == 2) { view.Failed = true; view.Snapshot.ObservedUtc = DateTime.UtcNow.AddHours(-2); }
                            panel.SetView(view);
                            using (Bitmap frame = new Bitmap(panelWidth, panelHeight)) { panel.DrawToBitmap(frame, panel.ClientRectangle); g.DrawImageUnscaled(frame, panelWidth * column, panelHeight * row); }
                        }
                    }
                    sheet.Save(Path.Combine(directory, "weather-details-" + (dark ? "dark" : "light") + "-" + dpi + ".png"));
                }
            }
        }
    }
}
