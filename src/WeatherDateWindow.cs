using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WinCalendar
{
    // Nonactivating date/weather companion; Explorer continues to own the taskbar.
    internal sealed class WeatherDateWindow : Form
    {
        private readonly Timer timer;
        private readonly Func<DateTime> clock;
        private readonly Action open;
        private readonly Action<WeatherView> openWeather, updateWeather;
        private readonly WeatherService weather;
        private bool showWeather = true;
        private int weatherRevision, renderedWeatherRevision = -1;
        private int layoutRevision;
        private readonly Stopwatch reminderClock = new Stopwatch();
        private bool locating, hover;
        private DateTime lastLocate, paintedDate;
        private Rectangle renderedBounds;
        private Size renderedFrame;
        private bool? renderedDark;
        private bool renderedHover;
        private double renderedPhase = -1;
        private bool renderedReminder;
        private string renderedLanguage;
        private int renderedFrames;
        private DateTime settleUntil;
        private int hiddenTransitions;
        private WeatherPlacement placement;
        private static string FlagPath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinCalendar", "WeatherDatePrototype.enabled"); } }
        internal static bool IsEnabled
        {
            get { return File.Exists(FlagPath); }
            set
            {
                if (value) { Directory.CreateDirectory(Path.GetDirectoryName(FlagPath)); File.WriteAllText(FlagPath, "prototype-v1"); }
                else if (File.Exists(FlagPath)) File.Delete(FlagPath);
            }
        }
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams
        { get { CreateParams p = base.CreateParams; p.ExStyle = (p.ExStyle | 0x08080080) & ~0x40000; return p; } }

        internal WeatherDateWindow(Func<DateTime> now, Action clicked, Action<WeatherView> weatherClicked = null, Action<WeatherView> weatherUpdated = null)
        {
            clock = now; open = clicked; openWeather = weatherClicked; updateWeather = weatherUpdated;
            weather = new WeatherService(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinCalendar", "Weather.bin"));
            weather.Changed += delegate { weatherRevision++; if (updateWeather != null) updateWeather(weather.View); };
            FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; TopMost = true;
            StartPosition = FormStartPosition.Manual; AutoScaleMode = AutoScaleMode.None;
            Cursor = Cursors.Hand;
            AccessibleName = UiText.Get("Дата рядом с погодой — открыть WinCalendar"); AccessibleRole = AccessibleRole.PushButton;
            timer = new Timer { Interval = 500 };
            timer.Tick += delegate { RefreshPlacement(); };
            timer.Start();
        }
        internal void Configure(AppPreferences preferences)
        { if (showWeather != preferences.WeatherEnabled) { placement = null; layoutRevision++; } showWeather = preferences.WeatherEnabled; weather.Configure(preferences); weatherRevision++; lastLocate = DateTime.MinValue; }
        private async void RefreshWeather() { await weather.RefreshAsync(); }

        internal async void RefreshPlacement()
        {
            if (IsDisposed) return;
            bool attention = TaskbarAnchor.ReminderActive && TaskbarAnchor.ReminderMotion && Visible;
            timer.Interval = attention ? 33 : DateTime.UtcNow < settleUntil ? 50 : 500;
            if (!IsEnabled) { placement = null; HideDate(); return; }
            RefreshWeather();
            if (!locating && DateTime.UtcNow - lastLocate > TimeSpan.FromSeconds(2))
            {
                locating = true; lastLocate = DateTime.UtcNow;
                WeatherPlacement previous = placement;
                int request = layoutRevision;
                int logicalWidth = showWeather ? WeatherWidgetRenderer.CombinedWidth : WeatherWidgetRenderer.Width;
                try { WeatherPlacement found = await System.Threading.Tasks.Task.Run(() => WeatherPlacement.Find(previous, true, logicalWidth)); if (request == layoutRevision) placement = found; }
                finally { locating = false; }
                if (request != layoutRevision) return;
                if (IsDisposed) return;
                if (!IsEnabled) { HideDate(); return; }
            }
            if (placement == null || !WeatherPlacement.TaskbarExposed(placement)) { HideDate(); return; }
            Rectangle target = placement.DateBounds;
            SetProp(Handle, "WinCalendar.SystemWeatherVisible", new IntPtr(placement.SystemWeatherVisible ? 1 : 0));
            RenderAt(target);
        }
        internal void RenderAt(Rectangle target)
        {
            TaskbarAnchor.Prototype = new Rectangle(target.Left + (int)Math.Round(target.Height * 2 / 44.0), target.Top, target.Height, target.Height);
            TaskbarAnchor.PrototypeHitArea = new Rectangle(target.Left, target.Top, (int)Math.Round(WeatherWidgetRenderer.Width * target.Height / 44.0), target.Height);
            bool dark = WeatherWidgetRenderer.DarkTaskbar();
            bool reminder = TaskbarAnchor.ReminderActive;
            if (reminder && !reminderClock.IsRunning) reminderClock.Restart();
            if (!reminder) reminderClock.Reset();
            double phase = reminder && TaskbarAnchor.ReminderMotion ? reminderClock.Elapsed.TotalMilliseconds % 2600 / 2600 : 0;
            if (!Visible || renderedBounds != target || paintedDate != clock().Date || renderedDark != dark || renderedHover != hover || renderedLanguage != UiText.Language || renderedReminder != reminder || renderedWeatherRevision != weatherRevision || (reminder && phase != renderedPhase))
            {
                paintedDate = clock().Date; renderedBounds = target; renderedDark = dark; renderedHover = hover;
                renderedReminder = reminder; renderedPhase = phase;
                renderedLanguage = UiText.Language;
                renderedWeatherRevision = weatherRevision;
                SetProp(Handle, "WinCalendar.WeatherReminderActive", new IntPtr(reminder ? 1 : 0));
                SetProp(Handle, "WinCalendar.WeatherBaseHeight", new IntPtr(target.Height));
                SetProp(Handle, "WinCalendar.WeatherCombined", new IntPtr(showWeather ? 1 : 0));
                SetProp(Handle, "WinCalendar.WeatherReminderMotion", new IntPtr(TaskbarAnchor.ReminderMotion ? 1 : 0));
                SetProp(Handle, "WinCalendar.WeatherFrames", new IntPtr(++renderedFrames));
                AccessibleName = paintedDate.ToString("dddd, d MMMM yyyy", DateText.Culture) + UiText.Get(" — открыть WinCalendar");
                if (showWeather) AccessibleName += "; " + UiText.Get("Погода") + ": " + weather.View.Temperature + ", " + weather.View.Status;
                using (Bitmap bitmap = WeatherWidgetRenderer.Draw(paintedDate, target.Width, target.Height, dark, hover, reminder, phase, weather.View, showWeather))
                {
                    if (!ReminderOverlay.Present(Handle, bitmap, new Point(target.Left, target.Top - (bitmap.Height - target.Height)))) { HideDate(); return; }
                    renderedFrame = bitmap.Size;
                }
            }
            if (!Visible) Show();
            KeepAboveTaskbar();
        }
        internal void KeepAboveTaskbar()
        { if (!IsDisposed && IsHandleCreated && Visible) SetWindowPos(Handle, new IntPtr(-1), 0, 0, 0, 0, 0x0213); }
        private void HideDate()
        {
            TaskbarAnchor.Prototype = Rectangle.Empty;
            TaskbarAnchor.PrototypeHitArea = Rectangle.Empty;
            if (Visible) { hiddenTransitions++; SetProp(Handle, "WinCalendar.WeatherHideCount", new IntPtr(hiddenTransitions)); Hide(); }
        }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; if (Visible) RenderAt(renderedBounds); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; if (Visible) RenderAt(renderedBounds); }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            // UpdateLayeredWindow resizes the native surface; WinForms can keep
            // the old client size while reminder headroom is visible.
            if (e.Button != MouseButtons.Left || !new Rectangle(Point.Empty, renderedFrame).Contains(e.Location)) return;
            settleUntil = DateTime.UtcNow.AddSeconds(1); timer.Interval = 50;
            if (showWeather && e.X >= WeatherWidgetRenderer.Width * renderedBounds.Height / 44.0 && openWeather != null) openWeather(weather.View);
            else open();
            KeepAboveTaskbar();
            if (!IsDisposed && IsHandleCreated) BeginInvoke(new Action(KeepAboveTaskbar));
        }
        protected override void OnHandleCreated(EventArgs e)
        { base.OnHandleCreated(e); SetProp(Handle, "WinCalendar.WeatherDate", new IntPtr(Process.GetCurrentProcess().Id)); }
        protected override void WndProc(ref Message m)
        { if (m.Msg == 0x1a) lastLocate = DateTime.MinValue; if (m.Msg == 0x21) { m.Result = new IntPtr(3); return; } base.WndProc(ref m); }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { timer.Dispose(); weather.Dispose(); HideDate(); }
            base.Dispose(disposing);
        }
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int w, int height, uint flags);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool SetProp(IntPtr h, string name, IntPtr value);
    }
}
