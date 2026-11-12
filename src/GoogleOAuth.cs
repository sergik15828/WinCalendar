using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace WinCalendar
{
    internal sealed class OAuthCode
    {
        internal string Code, Verifier, RedirectUri;
    }

    internal static class GoogleOAuth
    {
        internal static string RandomValue()
        {
            byte[] bytes = new byte[32];
            using (RandomNumberGenerator random = RandomNumberGenerator.Create()) random.GetBytes(bytes);
            return Base64Url(bytes);
        }

        internal static string Challenge(string verifier)
        {
            using (SHA256 hash = SHA256.Create()) return Base64Url(hash.ComputeHash(Encoding.ASCII.GetBytes(verifier)));
        }

        private static string Base64Url(byte[] bytes) { return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_'); }

        internal static string AuthorizationUrl(string id, string redirect, string state, string verifier)
        {
            Dictionary<string, string> fields = new Dictionary<string, string> { { "client_id", id }, { "redirect_uri", redirect },
                { "response_type", "code" }, { "scope", GoogleCalendarClient.AuthorizationScopes }, { "state", state },
                { "code_challenge", Challenge(verifier) }, { "code_challenge_method", "S256" },
                { "access_type", "offline" }, { "prompt", "consent select_account" } };
            StringBuilder url = new StringBuilder("https://accounts.google.com/o/oauth2/v2/auth?");
            foreach (KeyValuePair<string, string> item in fields) url.Append(Uri.EscapeDataString(item.Key)).Append('=').Append(Uri.EscapeDataString(item.Value)).Append('&');
            return url.ToString().TrimEnd('&');
        }

        internal static async Task<OAuthCode> AuthorizeAsync(string id, CancellationToken cancellation, Action<string> launchBrowser = null)
        {
            string state = RandomValue(), verifier = RandomValue();
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            string redirect = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port + "/oauth2/callback";
            try
            {
                string url = AuthorizationUrl(id, redirect, state, verifier);
                if (launchBrowser == null) Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                else launchBrowser(url);
                using (CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
                {
                    timeout.CancelAfter(TimeSpan.FromMinutes(4));
                    while (true)
                    {
                        Task<TcpClient> accept = listener.AcceptTcpClientAsync();
                        if (await Task.WhenAny(accept, Task.Delay(Timeout.Infinite, timeout.Token)).ConfigureAwait(false) != accept)
                            throw new OperationCanceledException(timeout.Token);
                        using (TcpClient client = await accept.ConfigureAwait(false))
                        using (NetworkStream stream = client.GetStream())
                        {
                            string request = await ReadHeadersAsync(stream, timeout.Token).ConfigureAwait(false);
                            string[] parts = request.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0].Split(' ');
                            Uri callback;
                            bool valid = parts.Length == 3 && parts[0] == "GET" && parts[1].StartsWith("/oauth2/callback?", StringComparison.Ordinal)
                                && Uri.TryCreate(redirect.Substring(0, redirect.IndexOf("/oauth2", StringComparison.Ordinal)) + parts[1], UriKind.Absolute, out callback);
                            if (!valid) { await ReplyAsync(stream, false, timeout.Token).ConfigureAwait(false); continue; }
                            callback = new Uri(redirect.Substring(0, redirect.IndexOf("/oauth2", StringComparison.Ordinal)) + parts[1]);
                            var query = HttpUtility.ParseQueryString(callback.Query);
                            if (!ValidateState(state, query["state"])) { await ReplyAsync(stream, false, timeout.Token).ConfigureAwait(false); continue; }
                            string code = query["code"];
                            await ReplyAsync(stream, !String.IsNullOrEmpty(code), timeout.Token).ConfigureAwait(false);
                            if (!String.IsNullOrEmpty(query["error"]) || String.IsNullOrEmpty(code)) throw new GoogleFailure(UiText.Get("Вход отменён или доступ к календарю не разрешён."), true);
                            return new OAuthCode { Code = code, Verifier = verifier, RedirectUri = redirect };
                        }
                    }
                }
            }
            finally { listener.Stop(); }
        }

        internal static bool ValidateState(string expected, string received)
        {
            if (received == null || expected.Length != received.Length) return false;
            int difference = 0;
            for (int i = 0; i < expected.Length; i++) difference |= expected[i] ^ received[i];
            return difference == 0;
        }

        private static async Task<string> ReadHeadersAsync(NetworkStream stream, CancellationToken cancellation)
        {
            using (CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
            {
                timeout.CancelAfter(TimeSpan.FromSeconds(5));
                byte[] buffer = new byte[8192];
                int count = 0;
                while (count < buffer.Length)
                {
                    Task<int> pending = stream.ReadAsync(buffer, count, buffer.Length - count, timeout.Token);
                    if (await Task.WhenAny(pending, Task.Delay(Timeout.Infinite, timeout.Token)).ConfigureAwait(false) != pending)
                        throw new OperationCanceledException(timeout.Token);
                    int read = await pending.ConfigureAwait(false);
                    if (read == 0) break;
                    count += read;
                    string text = Encoding.ASCII.GetString(buffer, 0, count);
                    if (text.Contains("\r\n\r\n")) return text;
                }
                throw new GoogleFailure(UiText.Get("Не удалось завершить вход в браузере. Повторите попытку."), true);
            }
        }

        private static Task ReplyAsync(Stream stream, bool success, CancellationToken cancellation)
        {
            string html = "<!doctype html><meta charset=\"utf-8\"><title>WinCalendar</title><p>"
                + (success ? UiText.Get("Ответ Google получен. Можно закрыть эту вкладку и открыть WinCalendar.") : UiText.Get("Запрос входа не подтверждён. Вернитесь в WinCalendar и повторите вход.")) + "</p>";
            byte[] body = Encoding.UTF8.GetBytes(html);
            byte[] head = Encoding.ASCII.GetBytes("HTTP/1.1 " + (success ? "200 OK" : "400 Bad Request") + "\r\nContent-Type: text/html; charset=utf-8\r\nCache-Control: no-store\r\nContent-Security-Policy: default-src 'none'\r\nContent-Length: " + body.Length + "\r\nConnection: close\r\n\r\n");
            byte[] response = new byte[head.Length + body.Length]; head.CopyTo(response, 0); body.CopyTo(response, head.Length);
            return stream.WriteAsync(response, 0, response.Length, cancellation);
        }
    }
}
