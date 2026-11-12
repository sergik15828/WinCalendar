using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinCalendar
{
    internal static class GoogleSelfTest
    {
        // All identities, responses and credentials here are synthetic fixtures.
        // Tests never read the installed app's real Google credentials or events.
        internal static async Task RunAsync(string directory, Action<bool, string> check)
        {
            check(GoogleOAuth.Challenge("dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk") == "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM", "PKCE matches the RFC 7636 SHA-256 test vector");
            check(GoogleOAuth.RandomValue() != GoogleOAuth.RandomValue(), "Each OAuth attempt has independent randomness");
            check(GoogleOAuth.ValidateState("state-fixture", "state-fixture") && !GoogleOAuth.ValidateState("state-fixture", "different") && !GoogleOAuth.ValidateState("state-fixture", null), "OAuth callback rejects wrong or missing state");
            string authorization = Uri.UnescapeDataString(GoogleOAuth.AuthorizationUrl("fixture.apps.googleusercontent.com", "http://127.0.0.1:12345/oauth2/callback", "state-fixture", "verifier-fixture"));
            check(authorization.Contains("code_challenge_method=S256") && authorization.Contains("access_type=offline") && authorization.Contains("calendar.events.readonly") && !authorization.Contains("client_secret"), "Browser authorization requests only calendar read access with PKCE and offline refresh");
            check(authorization.Contains("calendar.calendarlist.readonly") && GoogleCalendarClient.HasRequiredScopes(GoogleCalendarClient.AuthorizationScopes)
                && !GoogleCalendarClient.HasRequiredScopes(GoogleCalendarClient.Scope), "Additional calendars require a read-only calendar-list grant and legacy grants need renewed consent");
            Task simulatedBrowser = null;
            bool wrongStateRejected = false, returnPageShown = false;
            string challenge = null;
            OAuthCode received = await GoogleOAuth.AuthorizeAsync("fixture.apps.googleusercontent.com", CancellationToken.None, delegate(string url)
            {
                var query = System.Web.HttpUtility.ParseQueryString(new Uri(url).Query);
                string redirect = query["redirect_uri"], state = query["state"];
                challenge = query["code_challenge"];
                simulatedBrowser = Task.Run(async delegate
                {
                    using (HttpClient local = new HttpClient(new HttpClientHandler { UseProxy = false }) { Timeout = TimeSpan.FromSeconds(5) })
                    {
                        using (HttpResponseMessage wrong = await local.GetAsync(redirect + "?state=wrong&code=fixture-code")) wrongStateRejected = wrong.StatusCode == HttpStatusCode.BadRequest;
                        using (HttpResponseMessage right = await local.GetAsync(redirect + "?state=" + Uri.EscapeDataString(state) + "&code=fixture-code"))
                            returnPageShown = right.StatusCode == HttpStatusCode.OK && (await right.Content.ReadAsStringAsync()).Contains("WinCalendar");
                    }
                });
            });
            await simulatedBrowser;
            check(wrongStateRejected && returnPageShown && received.Code == "fixture-code" && GoogleOAuth.Challenge(received.Verifier) == challenge,
                "Real loopback listener rejects wrong state, accepts the browser callback and retains PKCE verifier");
            using (CancellationTokenSource cancel = new CancellationTokenSource(50))
            {
                bool cancelled = false;
                try { await GoogleOAuth.AuthorizeAsync("fixture.apps.googleusercontent.com", cancel.Token, delegate { }); }
                catch (OperationCanceledException) { cancelled = true; }
                check(cancelled, "Cancelled browser authorization releases the loopback listener promptly");
            }
            check(CalendarPage.SafeGoogleLink("https://calendar.google.com/calendar/event?eid=fixture") != null
                && CalendarPage.SafeGoogleLink("https://www.google.com/calendar/event?eid=fixture") != null
                && CalendarPage.SafeGoogleLink("https://evil.example/calendar") == null
                && CalendarPage.SafeGoogleLink("file:///C:/test.exe") == null
                && CalendarPage.SafeGoogleLink("https://www.google.com/url?q=test") == null, "Event navigation accepts only Google Calendar HTTPS links");

            TimeZoneInfo zone = TimeZoneInfo.CreateCustomTimeZone("Fixture UTC+3", TimeSpan.FromHours(3), "Fixture UTC+3", "Fixture UTC+3");
            CalendarEvent midnight = new CalendarEvent { Start = "2026-11-11T22:30:00Z", End = "2026-11-11T23:30:00Z" };
            check(midnight.OccursOn(new DateTime(2026, 11, 12), zone) && !midnight.OccursOn(new DateTime(2026, 11, 11), zone), "Timed events use the local day across UTC midnight");
            CalendarEvent span = new CalendarEvent { Start = "2026-11-12T23:00:00+03:00", End = "2026-11-13T00:00:00+03:00" };
            check(span.OccursOn(new DateTime(2026, 11, 12), zone) && !span.OccursOn(new DateTime(2026, 11, 13), zone), "Midnight event end is exclusive");
            CalendarEvent allDay = new CalendarEvent { AllDay = true, Start = "2026-11-12", End = "2026-11-14", Title = "Синтетическое событие на весь день" };
            check(allDay.OccursOn(new DateTime(2026, 11, 12), zone) && allDay.OccursOn(new DateTime(2026, 11, 13), zone) && !allDay.OccursOn(new DateTime(2026, 11, 14), zone), "Multiday all-day events preserve dates and exclusive end");
            check(allDay.TimeLabel(new DateTime(2026, 11, 12), zone) == "Весь день", "All-day events have a distinct time label");

            string firstPage = "{\"nextPageToken\":\"page2\",\"items\":[{\"id\":\"timed\",\"summary\":\"Синтетическая встреча\",\"htmlLink\":\"https://calendar.google.com/calendar/event?eid=fixture\",\"start\":{\"dateTime\":\"2026-11-12T10:00:00+03:00\"},\"end\":{\"dateTime\":\"2026-11-12T11:00:00+03:00\"}},{\"id\":\"cancelled\",\"status\":\"cancelled\"}]}";
            string secondPage = "{\"items\":[{\"id\":\"all-day\",\"start\":{\"date\":\"2026-11-13\"},\"end\":{\"date\":\"2026-11-14\"}}]}";
            check(CalendarPage.Parse(firstPage).Events.Count == 1 && CalendarPage.Parse(secondPage).Events.Single().Title == "Без названия", "API parsing skips cancellations and names untitled events");

            MemoryCredentials store = SessionStore();
            int tokenRequests = 0, eventRequests = 0;
            FixtureHandler handler = new FixtureHandler(async delegate(HttpRequestMessage request)
            {
                if (request.RequestUri.AbsolutePath == "/token")
                {
                    tokenRequests++;
                    string body = await request.Content.ReadAsStringAsync();
                    check(body.Contains("grant_type=refresh_token") && body.Contains("refresh_token=fixture-refresh"), "Refresh grant stays in the HTTPS request body");
                    return Response(HttpStatusCode.OK, "{\"access_token\":\"fixture-access\",\"expires_in\":3600}");
                }
                if (request.RequestUri.AbsolutePath.EndsWith("/calendarList", StringComparison.Ordinal))
                    return Response(HttpStatusCode.OK, "{\"items\":[{\"id\":\"primary\",\"summary\":\"Synthetic primary\",\"accessRole\":\"owner\"}]}");
                eventRequests++;
                check(request.Headers.Authorization != null && request.Headers.Authorization.Scheme == "Bearer", "Calendar API receives bearer access in its header");
                string url = Uri.UnescapeDataString(request.RequestUri.AbsoluteUri);
                check(url.Contains("singleEvents=true") && url.Contains("orderBy=startTime") && url.Contains("timeMin=") && url.Contains("timeMax="), "Requests expand recurring instances and bound the displayed date range");
                if (eventRequests == 1) return Response(HttpStatusCode.Unauthorized, "{}");
                return Response(HttpStatusCode.OK, url.Contains("pageToken=page2") ? secondPage : firstPage);
            });
            using (GoogleCalendarClient client = new GoogleCalendarClient(store, handler))
            {
                List<CalendarEvent> events = await client.LoadAsync(new DateTime(2026, 10, 26), new DateTime(2026, 12, 7), zone, CancellationToken.None);
                check(events.Count == 2 && eventRequests == 3 && tokenRequests == 2, "401 refreshes once, then all event pages are loaded");
                check(client.Connected && client.CacheIdentity == "fixture-account:all-calendars-reminders-v2", "Stored session restores with a separate cache namespace for all calendars");
            }
            await TestAdditionalCalendarsAsync(check, zone, directory);
            using (GoogleCalendarClient expired = new GoogleCalendarClient(SessionStore(), new FixtureHandler(delegate { return Task.FromResult(Response(HttpStatusCode.BadRequest, "{\"error\":\"invalid_grant\"}")); })))
            {
                bool rejected = false;
                try { await expired.LoadAsync(new DateTime(2026, 11, 1), new DateTime(2026, 12, 1), zone, CancellationToken.None); }
                catch (GoogleFailure failure) { rejected = failure.NeedsLogin; }
                check(rejected && !expired.Connected, "Revoked refresh access becomes a sign-in state without a retry loop");
            }
            MemoryCredentials disconnectStore = SessionStore();
            using (GoogleCalendarClient disconnect = new GoogleCalendarClient(disconnectStore, new FixtureHandler(delegate { throw new HttpRequestException(); })))
            {
                bool revoked = await disconnect.DisconnectAsync(CancellationToken.None);
                check(!revoked && !disconnect.Connected && disconnectStore.Read("WinCalendar/Google/Session") == null, "Disconnect removes local session even when Google is offline");
            }
            MemoryCredentials configurationStore = SessionStore();
            using (GoogleCalendarClient configured = new GoogleCalendarClient(configurationStore, new FixtureHandler(delegate { throw new HttpRequestException(); })))
            {
                await configured.ConfigureAsync("new-fixture.apps.googleusercontent.com", "", CancellationToken.None);
                check(configured.Configured && !configured.Connected && configured.CacheIdentity == null && configurationStore.Read("WinCalendar/Google/Session") == null,
                    "Changing OAuth client removes the previous account session before reconnecting");
            }

            string cachePath = Path.Combine(directory, "google-cache-test-" + Guid.NewGuid().ToString("N"));
            CalendarCache cache = new CalendarCache(cachePath);
            try
            {
                DateTime first = new DateTime(2026, 10, 26);
                CalendarSnapshot snapshot = new CalendarSnapshot { UpdatedUtc = DateTime.UtcNow.AddHours(-1), Events = new List<CalendarEvent> { allDay } };
                check(cache.Write("fixture-account", first, zone, snapshot), "DPAPI cache can persist a snapshot");
                string ciphertext = Encoding.UTF8.GetString(File.ReadAllBytes(Directory.GetFiles(cachePath, "*.bin").Single()));
                check(!ciphertext.Contains(allDay.Title), "Private event titles are not plaintext in the disk cache");
                check(cache.Read("fixture-account", first, zone).Events.Single().Title == allDay.Title && cache.Read("other-account", first, zone) == null, "Cache round-trips and separates accounts");
                cache.Write("fixture-account:range:" + first.AddDays(42).ToString("yyyyMMdd"), first, zone, snapshot);
                FixtureSource offline = new FixtureSource { Load = delegate { throw new HttpRequestException(); } };
                using (CalendarSync sync = new CalendarSync(offline, cache))
                {
                    await sync.ShowAsync(first, first.AddDays(42), zone, false);
                    check(sync.Current.HasSnapshot && sync.Current.Events.Count == 1 && sync.Current.Message.Contains("Нет связи"), "Offline state keeps cached events instead of reporting an empty calendar");
                    int calls = offline.Calls;
                    await sync.ShowAsync(first, first.AddDays(42), zone, false);
                    check(offline.Calls == calls, "Network failures back off automatic refreshes");
                }
                TaskCompletionSource<List<CalendarEvent>> slow = new TaskCompletionSource<List<CalendarEvent>>(), newer = new TaskCompletionSource<List<CalendarEvent>>();
                int requestIndex = 0;
                FixtureSource racing = new FixtureSource { Load = delegate { return ++requestIndex == 1 ? slow.Task : newer.Task; } };
                using (CalendarSync sync = new CalendarSync(racing, cache))
                {
                    Task a = sync.ShowAsync(first, first.AddDays(42), zone, true);
                    Task b = sync.ShowAsync(first.AddDays(35), first.AddDays(77), zone, true);
                    newer.SetResult(new List<CalendarEvent> { allDay }); await b;
                    slow.SetResult(new List<CalendarEvent>()); await a;
                    check(sync.Current.Events.Count == 1, "Late previous-month response cannot overwrite the selected month's data");
                    sync.Clear();
                    check(sync.Current.Connection == CalendarConnection.Disconnected && sync.Current.Events.Count == 0 && Directory.GetFiles(cachePath).Length == 0, "Disconnect clears events, memory and encrypted cache");
                }
            }
            finally { if (Directory.Exists(cachePath)) Directory.Delete(cachePath, true); }

            WindowsCredentialStore nativeStore = new WindowsCredentialStore();
            string credentialName = "WinCalendar/SelfTest/" + Guid.NewGuid().ToString("N");
            try
            {
                nativeStore.Write(credentialName, "synthetic-test-value");
                check(nativeStore.Read(credentialName) == "synthetic-test-value", "Windows Credential Manager round-trips an isolated synthetic credential");
            }
            finally { nativeStore.Delete(credentialName); }
            check(nativeStore.Read(credentialName) == null, "Synthetic credential is removed after testing");
            await TestSetupRecoveryAsync(check, directory);
            CaptureEvents(directory, check);
            await VerifyTooltipHoverAsync(check);
        }

        private static async Task TestAdditionalCalendarsAsync(Action<bool, string> check, TimeZoneInfo zone, string directory)
        {
            string calendarPage = "{\"nextPageToken\":\"list2\",\"items\":[{\"id\":\"primary\",\"summary\":\"Synthetic primary\",\"accessRole\":\"owner\"},"
                + "{\"id\":\"hidden\",\"hidden\":true,\"accessRole\":\"reader\"},{\"id\":\"busy\",\"accessRole\":\"freeBusyReader\"}]}";
            string additional = "{\"items\":[{\"id\":\"team+one@example.test\",\"summary\":\"Synthetic shared\",\"accessRole\":\"reader\",\"selected\":false},"
                + "{\"id\":\"team-two\",\"summaryOverride\":\"Synthetic team\",\"summary\":\"Original\",\"accessRole\":\"writer\"}]}";
            int eventCalls = 0, listCalls = 0;
            using (GoogleCalendarClient client = new GoogleCalendarClient(SessionStore(), new FixtureHandler(delegate(HttpRequestMessage request)
            {
                string path = Uri.UnescapeDataString(request.RequestUri.AbsolutePath);
                if (path == "/token") return Task.FromResult(Response(HttpStatusCode.OK, "{\"access_token\":\"fixture-access\",\"scope\":\"" + GoogleCalendarClient.AuthorizationScopes + "\",\"expires_in\":3600}"));
                if (path.EndsWith("/calendarList", StringComparison.Ordinal))
                { listCalls++; return Task.FromResult(Response(HttpStatusCode.OK, request.RequestUri.Query.Contains("pageToken=list2") ? additional : calendarPage)); }
                eventCalls++;
                if (path.Contains("/primary/")) return Task.FromResult(Response(HttpStatusCode.OK, "{\"items\":[]}"));
                check(!path.Contains("/hidden/") && !path.Contains("/busy/"), "Hidden and free-busy-only calendars are not queried for event details");
                return Task.FromResult(Response(HttpStatusCode.OK, "{\"items\":[{\"id\":\"same-id\",\"summary\":\"Synthetic meeting\",\"htmlLink\":\"https://calendar.google.com/calendar/event?eid=fixture\",\"start\":{\"dateTime\":\"2026-11-12T10:00:00+03:00\"},\"end\":{\"dateTime\":\"2026-11-12T11:00:00+03:00\"}}]}"));
            })))
            {
                List<CalendarEvent> events = await client.LoadAsync(new DateTime(2026, 11, 1), new DateTime(2026, 12, 1), zone, CancellationToken.None);
                check(listCalls == 2 && eventCalls == 3 && events.Count == 2 && events.All(e => e.OccursOn(new DateTime(2026, 11, 12), zone)),
                    "Empty primary calendar does not hide today's meetings in paginated additional calendars");
                check(events.Select(e => e.CalendarId).Distinct().Count() == 2 && events.Any(e => e.CalendarName == "Synthetic team"),
                    "Equal event IDs in different calendars remain distinct and retain their source names");
            }
            int oldGrantCalls = 0;
            using (GoogleCalendarClient legacy = new GoogleCalendarClient(SessionStore(), new FixtureHandler(delegate(HttpRequestMessage request)
            {
                oldGrantCalls++;
                return Task.FromResult(Response(HttpStatusCode.OK, "{\"access_token\":\"fixture-access\",\"scope\":\"" + GoogleCalendarClient.Scope + "\",\"expires_in\":3600}"));
            })))
            using (CalendarSync sync = new CalendarSync(legacy, new CalendarCache(Path.Combine(directory, "unused-permission-cache"))))
            {
                await sync.ShowAsync(new DateTime(2026, 11, 1), new DateTime(2026, 12, 1), zone, true);
                check(sync.Current.Connection == CalendarConnection.NeedsLogin && sync.Current.LoadError && sync.Current.Message.Contains("список календарей"),
                    "Legacy event-only session requests calendar-list consent instead of presenting an empty day");
                await sync.ShowAsync(new DateTime(2026, 11, 1), new DateTime(2026, 12, 1), zone, false);
                check(oldGrantCalls == 1 && sync.Current.Connection == CalendarConnection.NeedsLogin,
                    "Reopening preserves the permission explanation without retrying an obsolete grant");
            }
        }

        private static async Task TestSetupRecoveryAsync(Action<bool, string> check, string directory)
        {
            GoogleCalendarClient client = new GoogleCalendarClient(new MemoryCredentials(), new FixtureHandler(delegate { throw new HttpRequestException(); }));
            using (CalendarPopup popup = new CalendarPopup(delegate { return new DateTime(2026, 11, 12); }, delegate { }, false, client))
            {
                for (int round = 0; round < 2; round++)
                {
                    popup.ShowAt(new Point(50, 50), Screen.PrimaryScreen.WorkingArea);
                    IntPtr original = popup.Handle;
                    popup.Panel.SettingsButton.PerformClick();
                    check(popup.Panel.SettingsVisible && popup.Handle == original, "Gear opens settings inside the same flyout, round " + round);
                    popup.Panel.ConnectButton.PerformClick();
                    await Task.Delay(20);
                    check(popup.EditorVisible && popup.Handle == original && popup.Visible, "Sign-in opens client editor without another window or modal loop, round " + round);
                    TextBox id = popup.Editor.Controls.OfType<TextBox>().Single(t => !t.UseSystemPasswordChar);
                    id.Text = "fixture-unsaved.apps.googleusercontent.com";
                    popup.Hide(); popup.Toggle(new Point(50, 50));
                    check(popup.EditorVisible && popup.Visible && popup.Handle == original && id.Text == "fixture-unsaved.apps.googleusercontent.com",
                        "Reopening restores the inline editor and preserves unsaved input, round " + round);
                    check(popup.Editor.Controls.Cast<Control>().All(c => c.Font.GetHeight() > 0), "Reopening at the same DPI retains valid editor fonts, round " + round);
                    popup.ConfigureGoogle();
                    check(popup.Handle == original && popup.EditorVisible, "Repeated setup command reuses one flyout, round " + round);
                    if (round == 0)
                        using (Bitmap bitmap = new Bitmap(popup.Editor.Width, popup.Editor.Height))
                        { popup.Editor.DrawToBitmap(bitmap, popup.Editor.ClientRectangle); bitmap.Save(Path.Combine(directory, "google-setup-recovered.png"), ImageFormat.Png); }
                    popup.Editor.CancelButton.PerformClick();
                    check(!popup.EditorVisible && popup.Panel.SettingsVisible, "Editor back returns to settings without closing another window, round " + round);
                    popup.Panel.SettingsButton.PerformClick();
                    check(!popup.Panel.SettingsVisible, "Settings close returns to calendar, round " + round);
                    popup.ViewWindow.Close();
                    check(!popup.Visible && !popup.ViewWindow.IsDisposed, "Closing the flyout keeps its reusable window alive, round " + round);
                }
            }
        }

        private static async Task VerifyTooltipHoverAsync(Action<bool, string> check)
        {
            using (CalendarPopup popup = new CalendarPopup(delegate { return new DateTime(2026, 11, 12); }, delegate { }, false))
            {
                popup.ShowAt(new Point(400, 500), Screen.PrimaryScreen.WorkingArea);
                popup.Panel.SetData(new CalendarViewData { Connection = CalendarConnection.Connected, HasSnapshot = true,
                    Events = new List<CalendarEvent> { new CalendarEvent { Id = "hover-fixture", Title = "Синтетическая встреча для проверки подсказки", AllDay = true,
                        Start = "2026-11-12", End = "2026-11-13", Url = "https://calendar.google.com/calendar/event?eid=hover-fixture" } } });
                CalendarEventList list = popup.Panel.EventList;
                bool tipShown = false; int tipCount = 0;
                var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                ToolTip tip = (ToolTip)typeof(CalendarEventList).GetField("tooltip", flags).GetValue(list);
                tip.Popup += delegate { tipShown = true; tipCount++; };
                var move = typeof(CalendarEventList).GetMethod("OnMouseMove", flags);
                move.Invoke(list, new object[] { new MouseEventArgs(MouseButtons.None, 0, list.Width / 2, 30, 0) });
                for (int attempt = 0; attempt < 25 && !tipShown; attempt++) await Task.Delay(100);
                check(tipShown && popup.Visible && list.Cursor == Cursors.Hand,
                    "Native meeting tooltip appears without dismissing calendar or losing card hover");
                int redraws = 0;
                InvalidateEventHandler trackRedraw = delegate { redraws++; };
                list.Invalidated += trackRedraw;
                for (int x = list.Width / 2; x < list.Width / 2 + 20; x++)
                    move.Invoke(list, new object[] { new MouseEventArgs(MouseButtons.None, 0, x, 30, 0) });
                list.Invalidated -= trackRedraw;
                await Task.Delay(750);
                check(redraws == 0 && tipCount == 1 && popup.Visible,
                    "Moving within a meeting does not repaint the list or restart its visible tooltip");
                move.Invoke(list, new object[] { new MouseEventArgs(MouseButtons.None, 0, list.Width / 2, 0, 0) });
                check(list.Cursor == Cursors.Default && list.HitCard(new Point(list.Width / 2, 0)) == -1,
                    "Pointer leaves the card in a row gap without keeping its clickable cursor");
                popup.Panel.CloseButton.PerformClick();
                check(!popup.Visible && !popup.ViewWindow.IsDisposed, "Header close button hides calendar while preserving its background host");
            }
        }

        private static void CaptureEvents(string directory, Action<bool, string> check)
        {
            DateTime today = new DateTime(2026, 11, 12);
            List<CalendarEvent> events = new List<CalendarEvent>();
            events.Add(new CalendarEvent { Id = "fixture1", CalendarName = "Команда", Title = "Планирование недели — синтетический пример", Url = "https://calendar.google.com/calendar/event?eid=fixture1",
                Start = CalendarEvent.Boundary(today.AddHours(10), TimeZoneInfo.Local).ToString("o"), End = CalendarEvent.Boundary(today.AddHours(11), TimeZoneInfo.Local).ToString("o") });
            events.Add(new CalendarEvent { Id = "fixture2", Title = "Обсуждение макета и следующих шагов проекта — длинное синтетическое название встречи", Url = "https://calendar.google.com/calendar/event?eid=fixture2",
                Start = CalendarEvent.Boundary(today.AddHours(15), TimeZoneInfo.Local).ToString("o"), End = CalendarEvent.Boundary(today.AddHours(16), TimeZoneInfo.Local).ToString("o") });
            events.Add(new CalendarEvent { Id = "fixture3", Title = "День подготовки — синтетический пример", AllDay = true, Start = "2026-11-12", End = "2026-11-14", Url = "https://calendar.google.com/calendar/event?eid=fixture3" });
            using (CalendarPopup popup = new CalendarPopup(delegate { return today; }, delegate { }, false,
                new GoogleCalendarClient(new MemoryCredentials(), new FixtureHandler(delegate { throw new HttpRequestException(); }))))
            {
                popup.ShowAt(new Point(50, 50), Screen.PrimaryScreen.WorkingArea); Application.DoEvents();
                CalendarViewData data = new CalendarViewData { Connection = CalendarConnection.Connected, Events = events, HasSnapshot = true, UpdatedUtc = DateTime.UtcNow, Message = "Календари Google" };
                popup.Panel.SetData(data);
                check(popup.Panel.EventList.Items.Count == 3 && popup.Panel.Controls.OfType<CalendarDayButton>().Count(b => b.HasEvents) == 2, "Selected day's list and calendar dots use the same events");
                string opened = null;
                // Test the list's action without invoking the production browser handler.
                using (CalendarEventList list = new CalendarEventList())
                {
                    list.SetEvents(events, today, TimeZoneInfo.Local, CalendarPalette.Create(false), 96);
                    list.OpenRequested += delegate(string url) { opened = url; };
                    list.Open(0);
                    check(opened == "https://calendar.google.com/calendar/event?eid=fixture3", "Clicking a meeting dispatches its Google Calendar event link");
                    foreach (int scale in new[] { 96, 120, 144 })
                    {
                        list.Size = new Size(312 * scale / 96, 278 * scale / 96);
                        list.SetEvents(events, today, TimeZoneInfo.Local, CalendarPalette.Create(false), scale);
                        list.CreateControl();
                        Rectangle row = list.GetItemRectangle(0);
                        check(list.HitCard(new Point(row.Width / 2, row.Top + row.Height / 2)) == 0
                            && list.HitCard(new Point(row.Width / 2, row.Top)) == -1
                            && list.HitCard(new Point(row.Width / 2, row.Bottom - 1)) == -1
                            && list.HitCard(new Point(row.Left, row.Top + 3 * scale / 96)) == -1
                            && list.HitCard(new Point(-1, 10)) == -1,
                            "Meeting hit target follows rounded card and excludes gaps at " + scale + " DPI");
                    }

                }
                foreach (int dpi in new[] { 96, 120, 144 })
                foreach (bool dark in new[] { false, true })
                {
                    popup.Panel.SetDpi(dpi); popup.ViewWindow.ClientSize = popup.Panel.NaturalSize; popup.Panel.ApplyTheme(dark); Application.DoEvents();
                    using (Bitmap bitmap = new Bitmap(popup.Panel.Width, popup.Panel.Height))
                    {
                        popup.Panel.DrawToBitmap(bitmap, popup.Panel.ClientRectangle);
                        for (int row = 0; row < 3; row++)
                        {
                            Rectangle item = popup.Panel.EventList.GetItemRectangle(row);
                            int scale = dpi;
                            Rectangle titleArea = new Rectangle(popup.Panel.EventList.Left + 14 * scale / 96,
                                popup.Panel.EventList.Top + item.Top + 30 * scale / 96, 270 * scale / 96, 33 * scale / 96);
                            Color ink = CalendarPalette.Create(dark).Ink;
                            int inkPixels = 0;
                            for (int y = titleArea.Top; y < titleArea.Bottom; y++)
                            for (int x = titleArea.Left; x < titleArea.Right; x++)
                            {
                                Color pixel = bitmap.GetPixel(x, y);
                                if (Math.Abs(pixel.R - ink.R) < 35 && Math.Abs(pixel.G - ink.G) < 35 && Math.Abs(pixel.B - ink.B) < 35) inkPixels++;
                            }
                            check(inkPixels > 20, "Buffered meeting title remains visible in row " + row + " at " + dpi + " DPI, dark=" + dark);
                        }
                        bitmap.Save(Path.Combine(directory, "google-events-" + (dark ? "dark" : "light") + "-" + dpi + ".png"), ImageFormat.Png);
                    }
                    check(popup.Panel.Controls.Cast<Control>().All(c => popup.Panel.ClientRectangle.Contains(c.Bounds)), "Events panel controls fit at " + dpi + " DPI in " + (dark ? "dark" : "light"));
                }
                popup.Panel.SetDpi(96); popup.ViewWindow.ClientSize = popup.Panel.NaturalSize; Application.DoEvents();
                popup.Panel.SetData(new CalendarViewData { Connection = CalendarConnection.Connected, Events = events, HasSnapshot = true, UpdatedUtc = DateTime.UtcNow.AddHours(-1), Message = "Нет связи с Google. Показаны сохранённые встречи." });
                using (Bitmap bitmap = new Bitmap(popup.Panel.Width, popup.Panel.Height))
                { popup.Panel.DrawToBitmap(bitmap, popup.Panel.ClientRectangle); bitmap.Save(Path.Combine(directory, "google-events-offline.png"), ImageFormat.Png); }
                popup.Panel.Controls.OfType<CalendarDayButton>().Single(b => (DateTime)b.Tag == today.AddDays(2)).PerformClick();
                check(popup.Panel.EventList.Items.Count == 0 && popup.Panel.State.Today == today, "Choosing a day without meetings clears the list while retaining today's date");
                Capture(popup.Panel, directory, "google-events-empty.png");
                popup.Panel.TodayButton.PerformClick();
                popup.Panel.SetData(new CalendarViewData { Connection = CalendarConnection.Connected, Loading = true, Message = "Обновление встреч…" });
                check(!popup.Panel.RefreshButton.Enabled && popup.Panel.EventList.Items.Count == 0, "Initial loading is distinct from an empty day and disables duplicate refresh");
                Capture(popup.Panel, directory, "google-events-loading.png");
                popup.Panel.SetData(new CalendarViewData { Connection = CalendarConnection.NeedsLogin, Events = events, HasSnapshot = true, UpdatedUtc = DateTime.UtcNow.AddHours(-1), Message = "Доступ Google истёк или отозван. Войдите снова." });
                check(popup.Panel.ConnectButton.Text == "Войти в Google" && popup.Panel.DisconnectButton.Enabled, "Expired access offers sign-in and local disconnection");
                Capture(popup.Panel, directory, "google-events-signin.png");
                popup.Panel.SetData(new CalendarViewData { Connection = CalendarConnection.Busy, Message = "Завершите вход в открывшемся браузере." });
                check(popup.Panel.ConnectButton.Text == "Отмена входа" && !popup.Panel.DisconnectButton.Enabled, "Pending browser authorization can be cancelled");
                Capture(popup.Panel, directory, "google-events-authorizing.png");
                popup.Panel.SetData(new CalendarViewData { Connection = CalendarConnection.NeedsLogin, HasSnapshot = true, LoadError = true,
                    Message = "Нужно разрешение на список календарей. Войдите в Google." });
                check(!popup.Panel.Controls.OfType<Label>().Any(label => label.Text == "На этот день встреч нет."),
                    "Missing calendar-list permission does not claim that the selected day has no meetings");
                Capture(popup.Panel, directory, "google-events-permission.png");
                popup.Panel.SetData(new CalendarViewData { Connection = CalendarConnection.Connected, HasSnapshot = true, LoadError = true,
                    Message = "Нет связи с Google. Проверьте интернет." });
                check(!popup.Panel.Controls.OfType<Label>().Any(label => label.Text == "На этот день встреч нет."),
                    "An empty cached snapshot plus a refresh failure is not presented as a verified empty day");
                Capture(popup.Panel, directory, "google-events-empty-error.png");
                popup.Panel.SetData(new CalendarViewData { Connection = CalendarConnection.Connected, LoadError = true,
                    Message = "Нет связи с Google. Проверьте интернет." });
                check(!popup.Panel.Controls.OfType<Label>().Any(label => label.Text.Contains("сохранённые данные")),
                    "A first-load failure without a snapshot does not claim cached data are shown");
                Capture(popup.Panel, directory, "google-events-initial-error.png");
                popup.ConfigureGoogle();
                check(popup.EditorVisible && popup.Editor.Controls.OfType<TextBox>().Any(t => t.UseSystemPasswordChar), "Inline client editor masks its secret field");
                popup.Editor.SetDpi(120); popup.ViewWindow.ClientSize = new Size(CalendarPanel.LogicalWidth * 120 / 96, CalendarPanel.LogicalHeight * 120 / 96);
                using (Bitmap bitmap = new Bitmap(popup.Editor.Width, popup.Editor.Height))
                { popup.Editor.DrawToBitmap(bitmap, popup.Editor.ClientRectangle); bitmap.Save(Path.Combine(directory, "google-setup.png"), ImageFormat.Png); }
                foreach (int dpi in new[] { 96, 120, 144 })
                foreach (bool dark in new[] { false, true })
                {
                    popup.Editor.SetDpi(dpi, dark); popup.ViewWindow.ClientSize = new Size(CalendarPanel.LogicalWidth * dpi / 96, CalendarPanel.LogicalHeight * dpi / 96);
                    check(popup.Editor.Controls.Cast<Control>().All(c => c.Font.GetHeight() > 0), "Changing editor theme retains usable fonts at " + dpi + " DPI");
                    using (Bitmap bitmap = new Bitmap(popup.Editor.Width, popup.Editor.Height))
                    { popup.Editor.DrawToBitmap(bitmap, popup.Editor.ClientRectangle); bitmap.Save(Path.Combine(directory, "calendar-client-" + (dark ? "dark" : "light") + "-" + dpi + ".png"), ImageFormat.Png); }
                    check(popup.Editor.Controls.Cast<Control>().All(c => popup.Editor.ClientRectangle.Contains(c.Bounds)), "Inline client editor fits at " + dpi + " DPI");
                }
                popup.ShowCalendarSettings();
                foreach (int dpi in new[] { 96, 120, 144 })
                foreach (bool dark in new[] { false, true })
                {
                    popup.Panel.SetDpi(dpi); popup.ViewWindow.ClientSize = popup.Panel.NaturalSize; popup.Panel.ApplyTheme(dark); popup.Panel.ShowSettings(true);
                    Capture(popup.Panel, directory, "calendar-settings-" + (dark ? "dark" : "light") + "-" + dpi + ".png");
                    check(CalendarPanel.Descendants(popup.Panel).All(c => c.Parent.ClientRectangle.Contains(c.Bounds)), "Settings controls fit at " + dpi + " DPI");
                }

            }
        }

        private static void Capture(CalendarPanel panel, string directory, string name)
        {
            using (Bitmap bitmap = new Bitmap(panel.Width, panel.Height))
            { panel.DrawToBitmap(bitmap, panel.ClientRectangle); bitmap.Save(Path.Combine(directory, name), ImageFormat.Png); }
        }

        private static HttpResponseMessage Response(HttpStatusCode status, string json) { return new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") }; }
        private static MemoryCredentials SessionStore()
        {
            MemoryCredentials store = new MemoryCredentials();
            store.Write("WinCalendar/Google/DesktopClient", "{\"client_id\":\"fixture.apps.googleusercontent.com\"}");
            store.Write("WinCalendar/Google/Session", "{\"refresh_token\":\"fixture-refresh\",\"cache_id\":\"fixture-account\"}");
            return store;
        }
        private sealed class MemoryCredentials : ICredentialStore
        {
            private readonly Dictionary<string, string> values = new Dictionary<string, string>();
            public string Read(string key) { string value; return values.TryGetValue(key, out value) ? value : null; }
            public void Write(string key, string value) { values[key] = value; }
            public void Delete(string key) { values.Remove(key); }
        }
        private sealed class FixtureHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> respond;
            internal FixtureHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> callback) { respond = callback; }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation) { return respond(request); }
        }
        private sealed class FixtureSource : ICalendarSource
        {
            internal int Calls;
            internal Func<Task<List<CalendarEvent>>> Load;
            public bool Connected { get { return true; } }
            public string CacheIdentity { get { return "fixture-account"; } }
            public Task<List<CalendarEvent>> LoadAsync(DateTime first, DateTime end, TimeZoneInfo zone, CancellationToken cancellation) { Calls++; return Load(); }
        }
    }
}
