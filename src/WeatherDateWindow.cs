using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WinCalendar
{
    // Experimental companion window; never modifies Explorer or its widgets.
    internal sealed class WeatherDateWindow : Form
    {
        private readonly Timer timer;
        private readonly Func<DateTime> clock;
        private readonly Action open;
        private readonly Stopwatch reminderClock = new Stopwatch();
        private bool locating, hover;
        private DateTime lastLocate, paintedDate;
        private Rectangle renderedBounds;
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

        internal WeatherDateWindow(Func<DateTime> now, Action clicked)
        {
            clock = now; open = clicked;
            FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; TopMost = true;
            StartPosition = FormStartPosition.Manual; AutoScaleMode = AutoScaleMode.None;
            Cursor = Cursors.Hand;
            AccessibleName = UiText.Get("Дата рядом с погодой — открыть WinCalendar"); AccessibleRole = AccessibleRole.PushButton;
            timer = new Timer { Interval = 500 };
            timer.Tick += delegate { RefreshPlacement(); };
            timer.Start();
        }

        internal async void RefreshPlacement()
        {
            if (IsDisposed) return;
            bool attention = TaskbarAnchor.ReminderActive && TaskbarAnchor.ReminderMotion && Visible;
            timer.Interval = attention ? 33 : DateTime.UtcNow < settleUntil ? 50 : 500;
            if (!IsEnabled) { placement = null; HideDate(); return; }
            if (!locating && DateTime.UtcNow - lastLocate > TimeSpan.FromSeconds(2))
            {
                locating = true; lastLocate = DateTime.UtcNow;
                WeatherPlacement previous = placement;
                try { placement = await System.Threading.Tasks.Task.Run(() => WeatherPlacement.Find(previous)); }
                finally { locating = false; }
                if (IsDisposed) return;
                if (!IsEnabled) { HideDate(); return; }
            }
            if (placement == null || !WeatherPlacement.TaskbarExposed(placement)) { HideDate(); return; }
            Rectangle target = placement.DateBounds;
            RenderAt(target);
        }
        internal void RenderAt(Rectangle target)
        {
            TaskbarAnchor.Prototype = new Rectangle(target.Left + (int)Math.Round(target.Height * 2 / 44.0), target.Top, target.Height, target.Height);
            TaskbarAnchor.PrototypeHitArea = target;
            bool dark = WeatherWidgetRenderer.DarkTaskbar();
            bool reminder = TaskbarAnchor.ReminderActive;
            if (reminder && !reminderClock.IsRunning) reminderClock.Restart();
            if (!reminder) reminderClock.Reset();
            double phase = reminder && TaskbarAnchor.ReminderMotion ? reminderClock.Elapsed.TotalMilliseconds % 2600 / 2600 : 0;
            if (!Visible || renderedBounds != target || paintedDate != clock().Date || renderedDark != dark || renderedHover != hover || renderedLanguage != UiText.Language || renderedReminder != reminder || (reminder && phase != renderedPhase))
            {
                paintedDate = clock().Date; renderedBounds = target; renderedDark = dark; renderedHover = hover;
                renderedReminder = reminder; renderedPhase = phase;
                renderedLanguage = UiText.Language;
                SetProp(Handle, "WinCalendar.WeatherReminderActive", new IntPtr(reminder ? 1 : 0));
                SetProp(Handle, "WinCalendar.WeatherReminderMotion", new IntPtr(TaskbarAnchor.ReminderMotion ? 1 : 0));
                SetProp(Handle, "WinCalendar.WeatherFrames", new IntPtr(++renderedFrames));
                AccessibleName = paintedDate.ToString("dddd, d MMMM yyyy", DateText.Culture) + UiText.Get(" — открыть WinCalendar");
                using (Bitmap bitmap = WeatherWidgetRenderer.Draw(paintedDate, target.Width, target.Height, dark, hover, reminder, phase))
                    if (!ReminderOverlay.Present(Handle, bitmap, new Point(target.Left, target.Top - (bitmap.Height - target.Height)))) { HideDate(); return; }
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
            if (e.Button != MouseButtons.Left || !ClientRectangle.Contains(e.Location)) return;
            settleUntil = DateTime.UtcNow.AddSeconds(1); timer.Interval = 50;
            open(); KeepAboveTaskbar();
            if (!IsDisposed && IsHandleCreated) BeginInvoke(new Action(KeepAboveTaskbar));
        }
        protected override void OnHandleCreated(EventArgs e)
        { base.OnHandleCreated(e); SetProp(Handle, "WinCalendar.WeatherDate", new IntPtr(Process.GetCurrentProcess().Id)); }
        protected override void WndProc(ref Message m)
        { if (m.Msg == 0x21) { m.Result = new IntPtr(3); return; } base.WndProc(ref m); }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { timer.Dispose(); HideDate(); }
            base.Dispose(disposing);
        }
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int w, int height, uint flags);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool SetProp(IntPtr h, string name, IntPtr value);
    }
}
