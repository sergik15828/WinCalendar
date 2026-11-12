using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinCalendar
{
    internal sealed class ReminderCoordinator : IDisposable
    {
        private readonly GoogleCalendarClient source;
        private readonly CalendarSync sync;
        private readonly CalendarCache cache;
        private readonly System.Windows.Forms.Timer tick, refresh;
        private readonly ReminderOverlay overlay;
        private readonly string stateDirectory;
        private readonly CancellationToken lifetime;
        private ReminderEngine engine;
        private List<CalendarEvent> events = new List<CalendarEvent>();
        private string identity;
        private DateTime? lastSnapshotBoundary;
        private bool disposed, suspended, preview, hasSnapshot, loading, reloadAfterCurrent;

        internal ReminderCoordinator(GoogleCalendarClient source, CancellationToken cancellation)
        {
            this.source = source; lifetime = cancellation;
            string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinCalendar");
            stateDirectory = Path.Combine(root, "ReminderState");
            cache = new CalendarCache(Path.Combine(root, "ReminderCache"));
            sync = new CalendarSync(source, cache);
            sync.Changed += delegate(CalendarViewData data)
            {
                if (!data.HasSnapshot || suspended || disposed) return;
                events = data.Events; hasSnapshot = true;
                if (engine != null && source.Connected)
                {
                    DateTime after = lastSnapshotBoundary ?? engine.LastCheckUtc;
                    engine.Evaluate(events, after);
                    lastSnapshotBoundary = data.UpdatedUtc.HasValue && data.UpdatedUtc.Value > after ? data.UpdatedUtc : after;
                }
                Evaluate();
            };
            overlay = new ReminderOverlay();
            tick = new System.Windows.Forms.Timer { Interval = 1000 };
            refresh = new System.Windows.Forms.Timer { Interval = 120000 };
            tick.Tick += delegate { Evaluate(); };
            refresh.Tick += async delegate { if (!suspended) await RefreshAsync(true); };
        }

        internal async void Start()
        {
            tick.Start(); refresh.Start();
            WarmAnchor();
            if (!disposed) await RefreshAsync(false);
        }

        private static async void WarmAnchor() { await TaskbarAnchor.RefreshAsync(); }

        internal async Task RefreshAsync(bool force)
        {
            if (disposed || lifetime.IsCancellationRequested) return;
            if (loading) { if (force) reloadAfterCurrent = true; return; }
            if (!force && suspended) return;
            if (!source.Connected) { overlay.SetActive(preview); return; }
            suspended = false;
            EnsureAccount();
            loading = true;
            try
            {
                DateTime first = DateTime.Today.AddDays(-1);
                await sync.ShowAsync(first, DateTime.Today.AddDays(35), TimeZoneInfo.Local, force);
            }
            finally { loading = false; }
            if (reloadAfterCurrent && !disposed)
            { reloadAfterCurrent = false; await RefreshAsync(true); }
        }

        private void EnsureAccount()
        {
            string account = source.CacheIdentity;
            if (account == identity && engine != null) return;
            identity = account; events.Clear(); hasSnapshot = false; lastSnapshotBoundary = null;
            engine = new ReminderEngine(stateDirectory, account, delegate { return DateTimeOffset.Now; });
        }

        private void Evaluate()
        {
            if (disposed) return;
            if (!suspended && source.Connected && engine != null && hasSnapshot) engine.Evaluate(events);
            overlay.SetActive(preview || (!suspended && source.Connected && engine != null && engine.Active));
        }

        internal void Acknowledge()
        {
            preview = false;
            if (engine == null && source.Connected && !suspended) EnsureAccount();
            if (engine != null) engine.Acknowledge(); overlay.SetActive(false);
        }
        internal void Preview() { preview = true; overlay.SetActive(true); }
        internal void Pause() { suspended = true; reloadAfterCurrent = false; sync.Cancel(); overlay.SetActive(false); }

        internal void ResetAccount()
        {
            suspended = true; preview = false; reloadAfterCurrent = false; sync.Clear(); events.Clear(); hasSnapshot = false; engine = null; identity = null; lastSnapshotBoundary = null;
            overlay.SetActive(false);
            try
            {
                cache.Clear();
                if (Directory.Exists(stateDirectory)) foreach (string file in Directory.GetFiles(stateDirectory, "*.bin*")) File.Delete(file);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true; tick.Dispose(); refresh.Dispose(); sync.Dispose(); overlay.Dispose();
        }
    }
}
