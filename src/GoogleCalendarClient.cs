using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace WinCalendar
{
    internal sealed class GoogleFailure : Exception
    {
        internal readonly bool NeedsLogin;
        internal GoogleFailure(string message, bool needsLogin) : base(message) { NeedsLogin = needsLogin; }
    }

    internal interface ICalendarSource
    {
        bool Connected { get; }
        string CacheIdentity { get; }
        Task<List<CalendarEvent>> LoadAsync(DateTime first, DateTime end, TimeZoneInfo zone, CancellationToken cancellation);
    }

    internal sealed class GoogleCalendarClient : ICalendarSource, IDisposable
    {
        internal const string Scope = "https://www.googleapis.com/auth/calendar.events.readonly";
        internal const string CalendarListScope = "https://www.googleapis.com/auth/calendar.calendarlist.readonly";
        internal const string AuthorizationScopes = Scope + " " + CalendarListScope;
        private const string ClientCredential = "WinCalendar/Google/DesktopClient";
        private const string SessionCredential = "WinCalendar/Google/Session";
        private readonly ICredentialStore credentials;
        private readonly HttpClient http;
        private readonly SemaphoreSlim tokenLock = new SemaphoreSlim(1, 1);
        private string clientId, clientSecret, refreshToken, accessToken, cacheIdentity;
        private DateTime accessExpires;
        private bool needsLogin;
        internal bool Configured { get { return !String.IsNullOrEmpty(clientId); } }
        internal string ConfiguredClientId { get { return clientId; } }
        public bool Connected { get { return !String.IsNullOrEmpty(refreshToken) && !needsLogin; } }
        public string CacheIdentity { get { return cacheIdentity == null ? null : cacheIdentity + ":all-calendars-reminders-v2"; } }

        internal GoogleCalendarClient(ICredentialStore store) : this(store, new HttpClientHandler { AllowAutoRedirect = false }) { }
        internal GoogleCalendarClient(ICredentialStore store, HttpMessageHandler handler)
        {
            credentials = store;
            http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(25) };
            string config = store.Read(ClientCredential);
            if (config != null)
            {
                Dictionary<string, object> data = Json.Object(config);
                clientId = Json.Text(data, "client_id"); clientSecret = Json.Text(data, "client_secret");
            }
            string session = store.Read(SessionCredential);
            if (session != null)
            {
                Dictionary<string, object> data = Json.Object(session);
                refreshToken = Json.Text(data, "refresh_token"); cacheIdentity = Json.Text(data, "cache_id");
            }
        }

        internal async Task ConfigureAsync(string id, string secret, CancellationToken cancellation)
        {
            id = id.Trim();
            if (id.Length > 512 || !id.EndsWith(".apps.googleusercontent.com", StringComparison.Ordinal) || id.Any(c => Char.IsWhiteSpace(c)))
                throw new GoogleFailure("Нужен Client ID OAuth-клиента типа Desktop app из Google Cloud.", false);
            await tokenLock.WaitAsync(cancellation).ConfigureAwait(false);
            try
            {
                if (clientId == id && String.IsNullOrEmpty(secret)) secret = clientSecret;
                if (clientId == id && clientSecret == secret) return;
                credentials.Delete(SessionCredential); ClearMemory();
                credentials.Write(ClientCredential, Json.Serialize(new Dictionary<string, object> { { "client_id", id }, { "client_secret", secret } }));
                clientId = id; clientSecret = secret;
            }
            finally { tokenLock.Release(); }
        }

        internal async Task ConnectAsync(CancellationToken cancellation)
        {
            if (!Configured) throw new GoogleFailure("Сначала настройте OAuth-клиент Google.", false);
            await tokenLock.WaitAsync(cancellation).ConfigureAwait(false);
            try
            {
                OAuthCode code = await GoogleOAuth.AuthorizeAsync(clientId, cancellation).ConfigureAwait(false);
                Dictionary<string, string> fields = ClientFields();
                fields.Add("grant_type", "authorization_code"); fields.Add("code", code.Code);
                fields.Add("redirect_uri", code.RedirectUri); fields.Add("code_verifier", code.Verifier);
                Dictionary<string, object> token = await TokenAsync(fields, cancellation).ConfigureAwait(false);
                string refresh = Json.Text(token, "refresh_token");
                if (String.IsNullOrEmpty(refresh)) throw new GoogleFailure("Google не выдал доступ для фоновой работы. Повторите вход и разрешите чтение календаря.", true);
                string identity = Guid.NewGuid().ToString("N");
                credentials.Write(SessionCredential, Json.Serialize(new Dictionary<string, object> { { "refresh_token", refresh }, { "cache_id", identity } }));
                refreshToken = refresh; cacheIdentity = identity; needsLogin = false;
                SetAccess(token);
            }
            finally { tokenLock.Release(); }
        }

        internal async Task<bool> DisconnectAsync(CancellationToken cancellation)
        {
            await tokenLock.WaitAsync(cancellation).ConfigureAwait(false);
            try
            {
                string token = refreshToken;
                credentials.Delete(SessionCredential); ClearMemory();
                if (String.IsNullOrEmpty(token)) return true;
                try
                {
                    using (HttpResponseMessage response = await http.PostAsync("https://oauth2.googleapis.com/revoke",
                        new FormUrlEncodedContent(new Dictionary<string, string> { { "token", token } }), cancellation).ConfigureAwait(false))
                        return response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.BadRequest;
                }
                catch (HttpRequestException) { return false; }
                catch (OperationCanceledException) { return false; }
            }
            finally { tokenLock.Release(); }
        }

        private void ClearMemory() { refreshToken = accessToken = cacheIdentity = null; accessExpires = DateTime.MinValue; needsLogin = false; }

        private Dictionary<string, string> ClientFields()
        {
            Dictionary<string, string> fields = new Dictionary<string, string> { { "client_id", clientId } };
            if (!String.IsNullOrEmpty(clientSecret)) fields.Add("client_secret", clientSecret);
            return fields;
        }

        private async Task<Dictionary<string, object>> TokenAsync(Dictionary<string, string> fields, CancellationToken cancellation)
        {
            using (HttpResponseMessage response = await http.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(fields), cancellation).ConfigureAwait(false))
            {
                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    string code = null;
                    try { code = Json.Text(Json.Object(body), "error"); } catch (ArgumentException) { }
                    if (code == "invalid_grant") { needsLogin = true; accessToken = null; throw new GoogleFailure("Доступ Google истёк или отозван. Войдите снова.", true); }
                    if (code == "invalid_client" || code == "unauthorized_client") throw new GoogleFailure("Google не принял OAuth-клиент. Проверьте Client ID и Client secret в настройках.", true);
                    throw new GoogleFailure("Не удалось получить доступ Google. Повторите вход позже.", true);
                }
                Dictionary<string, object> result = Json.Object(body);
                string granted = Json.Text(result, "scope");
                if (granted != null && !HasRequiredScopes(granted))
                {
                    needsLogin = true;
                    throw new GoogleFailure("Нужно разрешение на список календарей. Нажмите «Войти в Google» и подтвердите доступ.", true);
                }
                return result;
            }
        }

        private void SetAccess(Dictionary<string, object> token)
        {
            accessToken = Json.Text(token, "access_token");
            if (String.IsNullOrEmpty(accessToken)) throw new GoogleFailure("Google не вернул доступ. Повторите вход.", true);
            object seconds;
            int duration = token.TryGetValue("expires_in", out seconds) ? Convert.ToInt32(seconds, CultureInfo.InvariantCulture) : 3600;
            accessExpires = DateTime.UtcNow.AddSeconds(Math.Max(0, duration - 60));
        }

        private async Task<string> AccessAsync(CancellationToken cancellation)
        {
            await tokenLock.WaitAsync(cancellation).ConfigureAwait(false);
            try
            {
                if (!Connected) throw new GoogleFailure("Войдите в Google, чтобы видеть встречи.", true);
                if (!String.IsNullOrEmpty(accessToken) && DateTime.UtcNow < accessExpires) return accessToken;
                Dictionary<string, string> fields = ClientFields();
                fields.Add("grant_type", "refresh_token"); fields.Add("refresh_token", refreshToken);
                Dictionary<string, object> token = await TokenAsync(fields, cancellation).ConfigureAwait(false);
                SetAccess(token);
                string rotated = Json.Text(token, "refresh_token");
                if (!String.IsNullOrEmpty(rotated))
                {
                    credentials.Write(SessionCredential, Json.Serialize(new Dictionary<string, object> { { "refresh_token", rotated }, { "cache_id", cacheIdentity } }));
                    refreshToken = rotated;
                }
                return accessToken;
            }
            finally { tokenLock.Release(); }
        }

        internal static bool HasRequiredScopes(string granted)
        {
            HashSet<string> scopes = new HashSet<string>(granted.Split(' '));
            bool all = scopes.Contains("https://www.googleapis.com/auth/calendar") || scopes.Contains("https://www.googleapis.com/auth/calendar.readonly");
            return all || (scopes.Contains(Scope) && scopes.Contains(CalendarListScope));
        }

        internal static string EventsUrl(DateTime first, DateTime end, TimeZoneInfo zone, string page, string calendarId = "primary")
        {
            string url = "https://www.googleapis.com/calendar/v3/calendars/" + Uri.EscapeDataString(calendarId) + "/events?singleEvents=true&orderBy=startTime&showDeleted=false&maxResults=250&timeZone=UTC"
                + "&timeMin=" + Uri.EscapeDataString(CalendarEvent.Boundary(first, zone).ToString("o", CultureInfo.InvariantCulture))
                + "&timeMax=" + Uri.EscapeDataString(CalendarEvent.Boundary(end, zone).ToString("o", CultureInfo.InvariantCulture))
                + "&fields=" + Uri.EscapeDataString("nextPageToken,defaultReminders,items(id,status,summary,htmlLink,start,end,reminders)");
            if (!String.IsNullOrEmpty(page)) url += "&pageToken=" + Uri.EscapeDataString(page);
            return url;
        }

        public async Task<List<CalendarEvent>> LoadAsync(DateTime first, DateTime end, TimeZoneInfo zone, CancellationToken cancellation)
        {
            List<CalendarEvent> events = new List<CalendarEvent>();
            List<GoogleCalendarEntry> calendars = await ListCalendarsAsync(cancellation).ConfigureAwait(false);
            foreach (GoogleCalendarEntry calendar in calendars)
            {
                List<CalendarEvent> items = await LoadCalendarAsync(calendar, first, end, zone, cancellation).ConfigureAwait(false);
                foreach (CalendarEvent item in items) { item.CalendarId = calendar.Id; item.CalendarName = calendar.Name; item.CalendarTimeZone = calendar.TimeZone; }
                events.AddRange(items);
            }
            return events.GroupBy(e => new { e.CalendarId, Key = e.Id ?? e.Start + e.Title }).Select(group => group.First()).ToList();
        }

        internal async Task<List<GoogleCalendarEntry>> ListCalendarsAsync(CancellationToken cancellation)
        {
            List<GoogleCalendarEntry> calendars = new List<GoogleCalendarEntry>();
            HashSet<string> pages = new HashSet<string>();
            string page = null;
            do
            {
                string url = "https://www.googleapis.com/calendar/v3/users/me/calendarList?minAccessRole=reader&maxResults=250&showHidden=false&showDeleted=false"
                    + "&fields=" + Uri.EscapeDataString("nextPageToken,items(id,summary,summaryOverride,accessRole,hidden,deleted,timeZone,defaultReminders)");
                if (!String.IsNullOrEmpty(page)) url += "&pageToken=" + Uri.EscapeDataString(page);
                GoogleCalendarList parsed = GoogleCalendarList.Parse(await ReadJsonAsync(url, true, cancellation).ConfigureAwait(false));
                calendars.AddRange(parsed.Calendars); page = parsed.NextPage;
                if (!String.IsNullOrEmpty(page) && !pages.Add(page)) throw new GoogleFailure("Не удалось получить полный список календарей. Повторите обновление.", false);
            } while (!String.IsNullOrEmpty(page));
            if (calendars.Count == 0) throw new GoogleFailure("Google не вернул доступных календарей. Проверьте аккаунт или войдите снова.", true);
            return calendars.GroupBy(c => c.Id).Select(group => group.First()).ToList();
        }

        private async Task<List<CalendarEvent>> LoadCalendarAsync(GoogleCalendarEntry calendar, DateTime first, DateTime end, TimeZoneInfo zone, CancellationToken cancellation)
        {
            List<CalendarEvent> events = new List<CalendarEvent>();
            HashSet<string> pages = new HashSet<string>();
            string page = null;
            do
            {
                cancellation.ThrowIfCancellationRequested();
                string url = EventsUrl(first, end, zone, page, calendar.Id);
                CalendarPage parsed = CalendarPage.Parse(await ReadJsonAsync(url, false, cancellation).ConfigureAwait(false), calendar.DefaultReminders);
                events.AddRange(parsed.Events); page = parsed.NextPage;
                if (!String.IsNullOrEmpty(page) && !pages.Add(page)) throw new GoogleFailure("Google вернул неполный список. Повторите обновление.", false);
            } while (!String.IsNullOrEmpty(page));
            return events.GroupBy(e => e.Id ?? e.Start + e.Title).Select(group => group.First()).ToList();
        }

        private async Task<string> ReadJsonAsync(string url, bool calendarList, CancellationToken cancellation)
        {
            for (int attempt = 0; attempt < 2; attempt++)
            {
                string access = await AccessAsync(cancellation).ConfigureAwait(false);
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access);
                    using (HttpResponseMessage response = await http.SendAsync(request, cancellation).ConfigureAwait(false))
                    {
                        if (response.StatusCode == HttpStatusCode.Unauthorized && attempt == 0) { accessExpires = DateTime.MinValue; continue; }
                        if (response.StatusCode == HttpStatusCode.Unauthorized) { needsLogin = true; throw new GoogleFailure("Доступ к Google отозван. Войдите снова.", true); }
                        if (response.StatusCode == HttpStatusCode.Forbidden && calendarList)
                        {
                            needsLogin = true;
                            throw new GoogleFailure("Нужно разрешение на список календарей. Нажмите «Войти в Google» и подтвердите доступ.", true);
                        }
                        if (response.StatusCode == HttpStatusCode.Forbidden) throw new GoogleFailure("Google запретил чтение одного из календарей. Проверьте разрешения аккаунта.", false);
                        if ((int)response.StatusCode == 429) throw new GoogleFailure("Google временно ограничил запросы. Обновление повторится позже.", false);
                        if (!response.IsSuccessStatusCode) throw new GoogleFailure("Google Calendar временно недоступен. Попробуйте позже.", false);
                        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    }
                }
            }
            throw new GoogleFailure("Не удалось обновить доступ Google. Войдите снова.", true);
        }

        public void Dispose() { http.Dispose(); accessToken = refreshToken = clientSecret = null; }
    }
}
