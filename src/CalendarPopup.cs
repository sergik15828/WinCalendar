using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinCalendar
{
    internal sealed class CalendarPopup : IDisposable
    {
        private readonly Func<DateTime> clock;
        private readonly ToolStripControlHost host;
        private readonly CalendarDropDown dropDown;
        private readonly GoogleCalendarClient google;
        private readonly CalendarSync sync;
        private readonly System.Windows.Forms.Timer refreshTimer;
        private CancellationTokenSource login;
        private GoogleSetupDialog setupDialog;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private bool disposed, disconnecting, configuring;
        private Rectangle anchorArea;
        private DateTime dismissedAtAnchor = DateTime.MinValue;
        internal CalendarPanel Panel { get; private set; }
        internal bool Visible { get { return dropDown.Visible; } }
        internal IntPtr Handle { get { return dropDown.Handle; } }
        internal Rectangle Bounds { get { return dropDown.Bounds; } }
        internal GoogleSetupDialog ActiveGoogleSetup { get { return setupDialog; } }

        internal CalendarPopup(Func<DateTime> now, Action openGoogle, bool connectGoogle = false, GoogleCalendarClient client = null)
        {
            clock = now;
            Panel = new CalendarPanel(now());
            host = new ToolStripControlHost(Panel) { AutoSize = false, Size = Panel.Size, Margin = Padding.Empty, Padding = Padding.Empty };
            dropDown = new CalendarDropDown { AutoClose = true, Padding = Padding.Empty, Margin = Padding.Empty, DropShadowEnabled = true };
            dropDown.Items.Add(host);
            Panel.GoogleRequested += delegate { Hide(); openGoogle(); };
            Panel.EventRequested += delegate(string url) { Hide(); Program.OpenCalendarEvent(url); };
            Panel.DismissRequested += delegate { Hide(); };
            refreshTimer = new System.Windows.Forms.Timer { Interval = 120000 };
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
                Panel.RefreshRequested += async delegate { await RefreshEventsAsync(true); };
                Panel.ConnectRequested += async delegate { await ConnectAsync(); };
                Panel.DisconnectRequested += async delegate { await DisconnectAsync(); };
                refreshTimer.Tick += async delegate { await RefreshEventsAsync(false); };
            }
            dropDown.Closed += delegate(object sender, ToolStripDropDownClosedEventArgs args)
            {
                refreshTimer.Stop();
                if ((args.CloseReason == ToolStripDropDownCloseReason.AppClicked || args.CloseReason == ToolStripDropDownCloseReason.AppFocusChange)
                    && anchorArea.Contains(Cursor.Position)) dismissedAtAnchor = DateTime.UtcNow;
            };
        }

        internal void Toggle(Point anchor)
        {
            if (RevealGoogleSetup()) return;
            if (Visible) { Hide(); return; }
            // Taskbar click closes the native popup before the launcher forwards
            // its command. Treat that same click as close rather than reopening.
            if ((DateTime.UtcNow - dismissedAtAnchor).TotalMilliseconds < 1500)
            {
                dismissedAtAnchor = DateTime.MinValue; return;
            }
            ShowAt(anchor, Screen.FromPoint(anchor).WorkingArea);
        }

        internal async void ShowAt(Point anchor, Rectangle work)
        {
            dropDown.Location = anchor;
            Panel.SetDpi((int)GetDpiForWindow(dropDown.Handle));
            Panel.Reset(clock());
            anchorArea = new Rectangle(anchor.X - 28, anchor.Y - 28, 56, 56);
            Rectangle bounds = Place(Panel.NaturalSize, anchor, work);
            host.Size = bounds.Size;
            Panel.Size = bounds.Size;
            dropDown.BackColor = Panel.BackColor;
            dropDown.Show(bounds.Location);
            Panel.FocusSelectedDay();
            refreshTimer.Start();
            if (sync != null && login == null && !disconnecting) await RefreshEventsAsync(false);
        }

        internal static Rectangle Place(Size requested, Point anchor, Rectangle work)
        {
            int width = Math.Min(requested.Width, work.Width);
            int height = Math.Min(requested.Height, work.Height);
            int x = anchor.X - width / 2, y = anchor.Y - height - 10;
            if (anchor.Y <= work.Top) y = work.Top + 10;
            if (anchor.X <= work.Left || anchor.X >= work.Right)
            {
                x = anchor.X <= work.Left ? work.Left + 10 : work.Right - width - 10;
                y = anchor.Y - height / 2;
            }
            x = Math.Max(work.Left, Math.Min(x, work.Right - width));
            y = Math.Max(work.Top, Math.Min(y, work.Bottom - height));
            return new Rectangle(x, y, width, height);
        }

        internal void UpdateToday(DateTime today) { Panel.UpdateToday(today); }
        internal void Hide() { dropDown.Close(ToolStripDropDownCloseReason.CloseCalled); }
        private async Task RefreshEventsAsync(bool force)
        {
            if (sync == null || disposed || login != null || disconnecting || configuring) return;
            DateTime[] days = Panel.State.Days();
            await sync.ShowAsync(days[0], days[days.Length - 1].AddDays(1), TimeZoneInfo.Local, force);
        }

        internal async void ConfigureGoogle() { await ConfigureGoogleAsync(); }
        internal async void ConnectGoogle() { await ConnectAsync(); }

        private bool RevealGoogleSetup()
        {
            if (setupDialog == null || setupDialog.IsDisposed) return false;
            Hide(); setupDialog.Reveal(); return true;
        }

        private async Task<bool> ConfigureGoogleAsync()
        {
            if (RevealGoogleSetup()) return false;
            if (google == null || login != null || disconnecting || configuring || disposed) return false;
            Hide(); sync.Cancel(); configuring = true;
            try
            {
                using (GoogleSetupDialog dialog = new GoogleSetupDialog())
                {
                    setupDialog = dialog;
                    // Leave the dropdown's click/close dispatch before starting
                    // a modal loop, so it cannot deactivate the new dialog.
                    await Task.Yield();
                    if (disposed) return false;
                    if (dialog.ShowDialog() != DialogResult.OK) return false;
                    try { await google.ConfigureAsync(dialog.ClientId, dialog.ClientSecret, lifetime.Token); if (!disposed) sync.Clear(); return !disposed; }
                    catch (Exception error)
                    {
                        GoogleFailure failure = error as GoogleFailure;
                        if (!disposed) MessageBox.Show(failure == null ? "Не удалось сохранить подключение в хранилище Windows." : failure.Message, "WinCalendar");
                        return false;
                    }
                }
            }
            finally { setupDialog = null; configuring = false; }
        }

        private async Task ConnectAsync()
        {
            if (RevealGoogleSetup()) return;
            if (google == null || disposed || disconnecting || configuring) return;
            if (login != null) { login.Cancel(); return; }
            if (!google.Configured) await ConfigureGoogleAsync();
            if (disposed || !google.Configured) return;
            Hide(); sync.Cancel();
            CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token); login = cancellation;
            Panel.SetData(new CalendarViewData { Connection = CalendarConnection.Busy, Message = "Завершите вход в открывшемся браузере." });
            try
            {
                await google.ConnectAsync(cancellation.Token);
                if (!disposed) sync.Clear();
            }
            catch (OperationCanceledException)
            {
                if (!disposed) Panel.SetData(new CalendarViewData { Connection = google.Connected ? CalendarConnection.Connected : CalendarConnection.Disconnected, Message = "Вход отменён или истекло время ожидания. Можно повторить." });
            }
            catch (Exception error)
            {
                GoogleFailure failure = error as GoogleFailure;
                if (!disposed) Panel.SetData(new CalendarViewData { Connection = CalendarConnection.NeedsLogin,
                    Message = failure == null ? "Не удалось войти в Google. Проверьте сеть и настройки клиента." : failure.Message });
            }
            finally
            {
                login = null; cancellation.Dispose();
            }
            if (!disposed && google.Connected) await RefreshEventsAsync(true);
        }

        private async Task DisconnectAsync()
        {
            if (google == null || disposed || disconnecting || login != null) return;
            disconnecting = true; sync.Clear();
            Panel.SetData(new CalendarViewData { Connection = CalendarConnection.Busy, Message = "Отключаем Google…" });
            try
            {
                bool revoked = await google.DisconnectAsync(lifetime.Token);
                if (!disposed)
                {
                    sync.Clear();
                    if (!revoked) Panel.SetData(new CalendarViewData { Message = "Локальный доступ удалён. Отзыв в Google не выполнен: нет связи." });
                }
            }
            catch (Exception) { if (!disposed) Panel.SetData(new CalendarViewData { Connection = CalendarConnection.NeedsLogin, Message = "Не удалось удалить доступ из хранилища Windows. Повторите отключение." }); }
            finally { disconnecting = false; }
        }

        public void Dispose()
        {
            disposed = true; lifetime.Cancel();
            if (setupDialog != null && !setupDialog.IsDisposed) setupDialog.Close();
            if (sync != null) sync.Dispose();
            refreshTimer.Dispose(); dropDown.Dispose();
            if (google != null) google.Dispose();
            lifetime.Dispose();
        }

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr window);

        private sealed class CalendarDropDown : ToolStripDropDown
        {
            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams parameters = base.CreateParams;
                    parameters.ExStyle = (parameters.ExStyle | 0x80) & ~0x40000;
                    return parameters;
                }
            }
        }
    }
}
