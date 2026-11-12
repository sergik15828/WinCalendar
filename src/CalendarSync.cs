using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WinCalendar
{
    internal enum CalendarConnection { Disconnected, Connected, Busy, NeedsLogin }

    internal sealed class CalendarViewData
    {
        internal List<CalendarEvent> Events = new List<CalendarEvent>();
        internal string Message;
        internal bool Loading, HasSnapshot, LoadError;
        internal DateTime? UpdatedUtc;
        internal CalendarConnection Connection;
    }

    // All publications occur on the caller's WinForms synchronization context.
    // Only the newest range may update the view, even if a cancelled request
    // completes after a month switch or account disconnection.
    internal sealed class CalendarSync : IDisposable
    {
        private readonly ICalendarSource source;
        private readonly CalendarCache cache;
        private readonly Dictionary<string, CalendarSnapshot> memory = new Dictionary<string, CalendarSnapshot>();
        private CancellationTokenSource pending;
        private int generation;
        private bool disposed;
        private DateTime retryAfter;
        internal event Action<CalendarViewData> Changed;
        internal CalendarViewData Current { get; private set; }

        internal CalendarSync(ICalendarSource source, CalendarCache cache) { this.source = source; this.cache = cache; Current = new CalendarViewData(); }

        internal async Task ShowAsync(DateTime first, DateTime end, TimeZoneInfo zone, bool force)
        {
            if (disposed) return;
            Cancel(); int request = generation;
            if (!source.Connected)
            {
                if (Current.Connection == CalendarConnection.NeedsLogin) { Publish(Current); return; }
                Publish(new CalendarViewData { Connection = CalendarConnection.Disconnected, Message = "Подключите Google, чтобы видеть встречи." }); return;
            }
            string identity = source.CacheIdentity;
            string rangeIdentity = identity + ":range:" + end.ToString("yyyyMMdd");
            string key = rangeIdentity + first.ToString("o") + zone.Id;
            CalendarSnapshot snapshot;
            if (!memory.TryGetValue(key, out snapshot)) snapshot = cache.Read(rangeIdentity, first, zone);
            bool fresh = snapshot != null && DateTime.UtcNow - snapshot.UpdatedUtc < TimeSpan.FromMinutes(2);
            bool canLoad = force || (!fresh && DateTime.UtcNow >= retryAfter);
            Publish(FromSnapshot(snapshot, canLoad, null));
            if (!canLoad) return;
            CancellationTokenSource cancellation = new CancellationTokenSource(); pending = cancellation;
            try
            {
                List<CalendarEvent> events = await source.LoadAsync(first, end, zone, cancellation.Token);
                if (disposed || request != generation || identity != source.CacheIdentity) return;
                snapshot = new CalendarSnapshot { UpdatedUtc = DateTime.UtcNow, Events = events };
                memory[key] = snapshot;
                bool saved = cache.Write(rangeIdentity, first, zone, snapshot);
                retryAfter = DateTime.MinValue;
                Publish(FromSnapshot(snapshot, false, saved ? null : "Встречи обновлены; локальный кеш недоступен."));
            }
            catch (OperationCanceledException)
            {
                if (!disposed && request == generation && !cancellation.IsCancellationRequested)
                {
                    retryAfter = DateTime.UtcNow.AddMinutes(2);
                    CalendarViewData timedOut = FromSnapshot(snapshot, false, "Превышено время ожидания. Повторите обновление.");
                    timedOut.LoadError = true; Publish(timedOut);
                }
            }
            catch (Exception error)
            {
                if (disposed || request != generation) return;
                retryAfter = DateTime.UtcNow.AddMinutes(2);
                GoogleFailure google = error as GoogleFailure;
                string message = google != null ? google.Message : error is HttpRequestException ? "Нет связи с Google. Проверьте интернет." : "Не удалось загрузить встречи. Повторите обновление.";
                CalendarViewData data = FromSnapshot(snapshot, false, message);
                data.LoadError = true;
                if (google != null && google.NeedsLogin) data.Connection = CalendarConnection.NeedsLogin;
                Publish(data);
            }
            finally { if (pending == cancellation) pending = null; cancellation.Dispose(); }
        }

        private static CalendarViewData FromSnapshot(CalendarSnapshot snapshot, bool loading, string message)
        {
            return new CalendarViewData { Connection = CalendarConnection.Connected, Events = snapshot == null ? new List<CalendarEvent>() : snapshot.Events,
                HasSnapshot = snapshot != null, UpdatedUtc = snapshot == null ? (DateTime?)null : snapshot.UpdatedUtc, Loading = loading,
                Message = message ?? (loading ? "Обновление встреч…" : snapshot == null ? "Обновление будет повторено позже." : "Календари Google") };
        }

        internal void Cancel()
        {
            generation++;
            if (pending != null) { pending.Cancel(); pending = null; }
        }

        internal void Clear()
        {
            Cancel(); memory.Clear(); retryAfter = DateTime.MinValue;
            try { cache.Clear(); } catch (System.IO.IOException) { } catch (UnauthorizedAccessException) { }
            Publish(new CalendarViewData { Connection = CalendarConnection.Disconnected, Message = "Google отключён. Локальные встречи удалены." });
        }

        private void Publish(CalendarViewData data) { Current = data; if (Changed != null) Changed(data); }
        public void Dispose() { disposed = true; Cancel(); }
    }
}
