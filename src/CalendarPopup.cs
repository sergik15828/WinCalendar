using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinCalendar
{
    // Calendar, settings and client editor share one toolwindow. No modal loops
    // or nested native dropdowns compete for activation or taskbar ownership.
    internal sealed class CalendarPopup : IDisposable
    {
        private readonly Func<DateTime> clock;
        private readonly CalendarFlyout window;
        private readonly GoogleClientEditor editor;
        private readonly GoogleCalendarClient google;
        private readonly CalendarSync sync;
        private readonly bool shell;
        private int visibilityRevision;
        private readonly System.Windows.Forms.Timer refreshTimer, closeTimer;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private CancellationTokenSource login;
        private ReminderCoordinator reminders;
        private Rectangle anchorArea;
        private DateTime closedAtPin = DateTime.MinValue;
        private bool disposed, disconnecting, configuring, editorVisible, loginAfterSave;
        internal event EventHandler ExitRequested;
        internal CalendarPanel Panel { get; private set; }
        internal bool Visible { get { return window.Visible; } }
        internal IntPtr Handle { get { return window.Handle; } }
        internal Rectangle Bounds { get { return window.Bounds; } }
        internal GoogleClientEditor Editor { get { return editor; } }
        internal bool EditorVisible { get { return editorVisible; } }
        internal Form ViewWindow { get { return window; } }

        internal CalendarPopup(Func<DateTime> now, Action openGoogle, bool connectGoogle = false, GoogleCalendarClient client = null)
        {
            clock = now; shell = connectGoogle;
            Panel = new CalendarPanel(now());
            editor = new GoogleClientEditor { Dock = DockStyle.Fill, Visible = false };
            window = new CalendarFlyout { Text = "WinCalendar", StartPosition = FormStartPosition.Manual, ClientSize = Panel.Size };
            Panel.Dock = DockStyle.Fill;
            window.Controls.Add(Panel); window.Controls.Add(editor);
            Panel.GoogleRequested += delegate { Hide(); openGoogle(); };
            Panel.EventRequested += delegate(string url) { Hide(); Program.OpenCalendarEvent(url); };
            Panel.DismissRequested += delegate { Hide(); };
            Panel.ConfigureRequested += delegate { ConfigureGoogle(); };
            Panel.PreviewRequested += delegate { Hide(); if (reminders != null) reminders.Preview(); };
            Panel.ExitRequested += delegate { if (ExitRequested != null) ExitRequested(this, EventArgs.Empty); };
            Panel.StartupRequested += delegate(bool enabled) { if (shell) ShellIntegration.StartsWithWindows = enabled; };
            Panel.SettingsOpened += delegate { if (shell) Panel.SetStartup(ShellIntegration.StartsWithWindows); };
            editor.CancelRequested += delegate { ShowCalendarSettings(); };
            editor.SaveRequested += async delegate { await SaveClientAsync(); };
            refreshTimer = new System.Windows.Forms.Timer { Interval = 120000 };
            closeTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            closeTimer.Tick += delegate { closeTimer.Stop(); closedAtPin = DateTime.UtcNow; Hide(); };
            window.DismissRequested += delegate { Hide(); };
            window.Deactivate += delegate
            {
                // Tooltip creation can temporarily change activation. Inspect
                // the settled foreground window before dismissing the flyout.
                int revision = visibilityRevision;
                window.BeginInvoke(new Action(delegate
                {
                    if (disposed || !window.Visible || revision != visibilityRevision) return;
                    uint process; GetWindowThreadProcessId(GetForegroundWindow(), out process);
                    if (process == (uint)System.Diagnostics.Process.GetCurrentProcess().Id) return;
                    if (anchorArea.Contains(Cursor.Position) && (Control.MouseButtons & MouseButtons.Left) != 0) closeTimer.Start();
                    else Hide();
                }));
            };
            window.Activated += delegate { visibilityRevision++; closeTimer.Stop(); };
            window.VisibleChanged += delegate
            {
                if (window.Visible) refreshTimer.Start(); else { refreshTimer.Stop(); closeTimer.Stop(); }
            };
            if (connectGoogle || client != null)
            {
                try
                {
                    google = client ?? new GoogleCalendarClient(new WindowsCredentialStore());
                    sync = new CalendarSync(google, new CalendarCache(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinCalendar", "GoogleCache")));
                    sync.Changed += Panel.SetData;
                }
                catch (Exception) { Panel.SetData(new CalendarViewData { Message = "Хранилище Windows недоступно. Повторите запуск приложения." }); }
                Panel.RangeChanged += async delegate { await RefreshEventsAsync(false); };
                Panel.RefreshRequested += async delegate { await RefreshEventsAsync(true); if (reminders != null) await reminders.RefreshAsync(true); };
                Panel.ConnectRequested += async delegate { await ConnectAsync(); };
                Panel.DisconnectRequested += async delegate { await DisconnectAsync(); };
                refreshTimer.Tick += async delegate { await RefreshEventsAsync(false); };
            }
        }

        internal void StartBackgroundServices()
        {
            if (!shell || google == null || reminders != null || disposed) return;
            reminders = new ReminderCoordinator(google, lifetime.Token);
            reminders.Start();
        }

        internal void Toggle(Point anchor)
        {
            if (reminders != null) reminders.Acknowledge();
            if (Visible) { Hide(); return; }
            if ((DateTime.UtcNow - closedAtPin).TotalMilliseconds < 800) { closedAtPin = DateTime.MinValue; return; }
            ShowAt(anchor, Screen.FromPoint(anchor).WorkingArea);
        }

        internal async void ShowAt(Point anchor, Rectangle work)
        {
            if (disposed) return;
            visibilityRevision++; closeTimer.Stop();
            Rectangle pin = shell ? TaskbarAnchor.Current : Rectangle.Empty;
            if (!pin.IsEmpty) { anchor = new Point(pin.Left + pin.Width / 2, pin.Top + pin.Height / 2); work = Screen.FromRectangle(pin).WorkingArea; }
            anchorArea = pin.IsEmpty ? new Rectangle(anchor.X - 28, anchor.Y - 28, 56, 56) : pin;
            window.Location = anchor;
            int dpi = (int)GetDpiForWindow(window.Handle);
            Panel.SetDpi(dpi); editor.SetDpi(dpi);
            if (!editorVisible) Panel.Reset(clock());
            Rectangle bounds = Place(Panel.NaturalSize, anchor, work);
            window.Bounds = bounds;
            if (!window.Visible) window.Show();
            window.Reveal();
            if (editorVisible) editor.Select(); else Panel.FocusSelectedDay();
            if (sync != null && login == null && !disconnecting) await RefreshEventsAsync(false);
        }

        internal static Rectangle Place(Size requested, Point anchor, Rectangle work)
        {
            int width = Math.Min(requested.Width, work.Width), height = Math.Min(requested.Height, work.Height);
            int x = anchor.X - Math.Min(width / 2, requested.Width / 4), y = anchor.Y - height - 10;
            if (anchor.Y >= work.Bottom) y = work.Bottom - height - 8;
            if (anchor.Y <= work.Top) y = work.Top + 10;
            if (anchor.X <= work.Left || anchor.X >= work.Right)
            { x = anchor.X <= work.Left ? work.Left + 10 : work.Right - width - 10; y = anchor.Y - height / 2; }
            return new Rectangle(Math.Max(work.Left, Math.Min(x, work.Right - width)), Math.Max(work.Top, Math.Min(y, work.Bottom - height)), width, height);
        }

        internal void UpdateToday(DateTime today) { Panel.UpdateToday(today); }
        internal void Hide() { visibilityRevision++; closeTimer.Stop(); window.Hide(); }

        internal void ShowCalendarSettings()
        {
            editorVisible = false; editor.Visible = false; Panel.Visible = true;
            if (!window.Visible) ShowAt(Cursor.Position, Screen.FromPoint(Cursor.Position).WorkingArea);
            Panel.ShowSettings(true);
            if (shell) Panel.SetStartup(ShellIntegration.StartsWithWindows);
            window.Reveal();
        }

        private async Task RefreshEventsAsync(bool force)
        {
            if (sync == null || disposed || login != null || disconnecting || configuring) return;
            DateTime[] days = Panel.State.Days();
            await sync.ShowAsync(days[0], days[days.Length - 1].AddDays(1), TimeZoneInfo.Local, force);
        }

        internal void ConfigureGoogle() { OpenClientEditor(false); }
        internal async void ConnectGoogle() { await ConnectAsync(); }
        internal void PreviewReminder() { StartBackgroundServices(); Hide(); if (reminders != null) reminders.Preview(); }

        private void OpenClientEditor(bool connectAfter)
        {
            if (disposed || login != null || disconnecting || configuring) return;
            if (google == null) { ShowCalendarSettings(); return; }
            if (!editorVisible) { editor.BeginEdit(google.ConfiguredClientId); loginAfterSave = connectAfter; }
            editorVisible = true;
            if (!window.Visible) ShowAt(Cursor.Position, Screen.FromPoint(Cursor.Position).WorkingArea);
            Panel.Visible = false; editor.Visible = true; editor.BringToFront();
            window.Reveal(); editor.Select();
        }

        private async Task SaveClientAsync()
        {
            if (google == null || configuring || disposed) return;
            configuring = true; editor.SetBusy(true); sync.Cancel();
            if (reminders != null) reminders.Pause();
            try
            {
                await google.ConfigureAsync(editor.ClientId, editor.ClientSecret, lifetime.Token);
                if (disposed) return;
                sync.Clear(); editor.Clear();
                if (reminders != null) reminders.ResetAccount();
                ShowCalendarSettings();
            }
            catch (Exception error)
            {
                GoogleFailure failure = error as GoogleFailure;
                if (!disposed) editor.ShowError(failure == null ? "Не удалось сохранить данные в хранилище Windows." : failure.Message);
                return;
            }
            finally { configuring = false; if (!disposed) editor.SetBusy(false); }
            if (!disposed && loginAfterSave) { loginAfterSave = false; await ConnectAsync(); }
            else if (!disposed && google.Connected) { await RefreshEventsAsync(true); if (reminders != null) await reminders.RefreshAsync(true); }
        }

        private async Task ConnectAsync()
        {
            if (google == null || disposed || disconnecting || configuring) return;
            if (editorVisible) { if (!window.Visible) ShowAt(Cursor.Position, Screen.FromPoint(Cursor.Position).WorkingArea); window.Reveal(); return; }
            if (login != null) { login.Cancel(); return; }
            if (!google.Configured) { OpenClientEditor(true); return; }
            Hide(); sync.Cancel();
            if (reminders != null) reminders.Pause();
            CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token); login = cancellation;
            Panel.SetData(new CalendarViewData { Connection = CalendarConnection.Busy, Message = "Завершите вход в открывшемся браузере." });
            try
            {
                await google.ConnectAsync(cancellation.Token);
                if (!disposed) { sync.Clear(); if (reminders != null) { reminders.ResetAccount(); await reminders.RefreshAsync(true); } }
            }
            catch (OperationCanceledException)
            {
                if (!disposed) Panel.SetData(new CalendarViewData { Connection = google.Connected ? CalendarConnection.Connected : CalendarConnection.Disconnected, Message = "Вход отменён. Можно повторить." });
            }
            catch (Exception error)
            {
                GoogleFailure failure = error as GoogleFailure;
                if (!disposed) Panel.SetData(new CalendarViewData { Connection = CalendarConnection.NeedsLogin,
                    Message = failure == null ? "Не удалось войти в Google. Проверьте сеть и настройки." : failure.Message });
            }
            finally { login = null; cancellation.Dispose(); }
            if (!disposed && google.Connected)
            { await RefreshEventsAsync(true); if (reminders != null) await reminders.RefreshAsync(true); }
        }

        private async Task DisconnectAsync()
        {
            if (google == null || disposed || disconnecting || login != null) return;
            disconnecting = true; sync.Clear();
            if (reminders != null) reminders.ResetAccount();
            Panel.SetData(new CalendarViewData { Connection = CalendarConnection.Busy, Message = "Отключаем Google…" });
            try
            {
                bool revoked = await google.DisconnectAsync(lifetime.Token);
                if (!disposed) { sync.Clear(); if (reminders != null) reminders.ResetAccount(); if (!revoked) Panel.SetData(new CalendarViewData { Message = "Локальный доступ удалён. Отзыв в Google не выполнен: нет связи." }); }
            }
            catch (Exception) { if (!disposed) Panel.SetData(new CalendarViewData { Connection = CalendarConnection.NeedsLogin, Message = "Не удалось удалить доступ. Повторите отключение." }); }
            finally { disconnecting = false; }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true; lifetime.Cancel();
            if (reminders != null) reminders.Dispose();
            if (sync != null) sync.Dispose();
            refreshTimer.Dispose(); closeTimer.Dispose(); window.Dispose();
            if (google != null) google.Dispose(); lifetime.Dispose();
        }

        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
        [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr window);

        private sealed class CalendarFlyout : Form
        {
            internal event EventHandler DismissRequested;
            internal CalendarFlyout()
            {
                AutoScaleMode = AutoScaleMode.None; FormBorderStyle = FormBorderStyle.None;
                ShowInTaskbar = false; MaximizeBox = MinimizeBox = false;
            }
            protected override CreateParams CreateParams
            {
                get { CreateParams value = base.CreateParams; value.ExStyle = (value.ExStyle | 0x80) & ~0x40000; value.ClassStyle |= 0x20000; return value; }
            }
            protected override void OnHandleCreated(EventArgs args)
            {
                base.OnHandleCreated(args);
                SetProp(Handle, "WinCalendar.Flyout", new IntPtr(System.Diagnostics.Process.GetCurrentProcess().Id));
                int rounded = 2; DwmSetWindowAttribute(Handle, 33, ref rounded, sizeof(int));
            }
            protected override void OnHandleDestroyed(EventArgs args)
            { RemoveProp(Handle, "WinCalendar.Flyout"); base.OnHandleDestroyed(args); }
            internal void Reveal() { BringToFront(); Activate(); SetForegroundWindow(Handle); }
            protected override bool ProcessCmdKey(ref Message message, Keys keys)
            {
                if (keys == Keys.Escape) { if (DismissRequested != null) DismissRequested(this, EventArgs.Empty); return true; }
                return base.ProcessCmdKey(ref message, keys);
            }
            protected override void OnFormClosing(FormClosingEventArgs args)
            {
                if (args.CloseReason == CloseReason.UserClosing) { args.Cancel = true; Hide(); return; }
                base.OnFormClosing(args);
            }
            [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr window);
            [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool SetProp(IntPtr window, string name, IntPtr value);
            [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool RemoveProp(IntPtr window, string name);
            [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
        }
    }
}
