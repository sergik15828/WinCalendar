using System;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace WinCalendar
{
    // A normal native taskbar button, backed by a permanently minimized Form.
    // No WPF window or custom taskbar-group identity is involved.
    internal sealed class DateTaskbarWindow : Form
    {
        private readonly bool integrateWithShell;
        private readonly Func<DateTime> now;
        private readonly Timer timer;
        private readonly ContextMenuStrip menu;
        private readonly ToolStripMenuItem startup;
        private readonly uint taskbarCreatedMessage;
        private Icon ownedIcon;
        private Icon smallIcon;
        private bool retryIcon;
        private CalendarPopup calendar;
        internal DateTime CurrentDate { get; private set; }
        internal CalendarPopup Calendar { get { return calendar; } }

        protected override bool ShowWithoutActivation { get { return true; } }

        internal DateTaskbarWindow(bool shell, Func<DateTime> clock)
        {
            integrateWithShell = shell;
            now = clock;
            Size = new Size(128, 128);
            Rectangle work = SystemInformation.WorkingArea;
            Location = new Point(work.Left + 12, work.Bottom - Height - 12);
            StartPosition = FormStartPosition.Manual;
            WindowState = FormWindowState.Minimized;
            ShowInTaskbar = !shell || !ShellIntegration.HasPinnedShortcut();
            MaximizeBox = false;
            taskbarCreatedMessage = RegisterWindowMessage("TaskbarCreated");
            menu = new ContextMenuStrip();
            menu.Items.Add("Открыть Google Календарь", null, delegate { Program.OpenGoogleCalendar(); });
            menu.Items.Add("Настроить подключение Google…", null, delegate { calendar.ConfigureGoogle(); });
            menu.Items.Add(new ToolStripSeparator());
            startup = new ToolStripMenuItem("Запускать с Windows") { CheckOnClick = true };
            startup.Click += delegate { ShellIntegration.StartsWithWindows = startup.Checked; };
            menu.Items.Add(startup);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Выйти", null, delegate { Close(); });
            timer = new Timer();
            timer.Tick += delegate
            {
                RefreshDate(false);
                if (ShowInTaskbar && ShellIntegration.HasPinnedShortcut()) ShowInTaskbar = false;
                ScheduleNextCheck();
            };
            Resize += delegate
            {
                if (WindowState != FormWindowState.Minimized) WindowState = FormWindowState.Minimized;
            };
            RefreshDate(true);
            calendar = new CalendarPopup(now, Program.OpenGoogleCalendar, integrateWithShell);
            if (integrateWithShell)
            {
                SystemEvents.TimeChanged += OnTimeChanged;
                SystemEvents.PowerModeChanged += OnPowerChanged;
                ScheduleNextCheck();
                timer.Start();
            }
        }

        internal bool RefreshDate(bool force)
        {
            DateTime date = now().Date;
            bool changed = date != CurrentDate;
            if (!changed && !force && !retryIcon) return false;
            byte[] icon = CalendarIcon.Create(date);
            Icon replacement;
            using (MemoryStream stream = new MemoryStream(icon))
            using (Icon loaded = new Icon(stream)) replacement = (Icon)loaded.Clone();
            Icon previous = ownedIcon;
            Icon previousSmall = smallIcon;
            ownedIcon = replacement;
            smallIcon = new Icon(replacement, SystemInformation.SmallIconSize);
            Icon = replacement;
            UpdateNativeIcons();
            if (previous != null) previous.Dispose();
            if (previousSmall != null) previousSmall.Dispose();
            CurrentDate = date;
            if (calendar != null) calendar.UpdateToday(date);
            Text = date.ToString("dddd, d MMMM yyyy", DateText.Russian) + " — WinCalendar";
            if (integrateWithShell)
            {
                try
                {
                    CalendarIcon.Save(ShellIntegration.IconPath, icon);
                    ShellIntegration.RefreshPinnedIcon(ShellIntegration.IconPath);
                    retryIcon = false;
                }
                catch (IOException) { IconNeedsRetry(); }
                catch (UnauthorizedAccessException) { IconNeedsRetry(); }
            }
            return changed;
        }

        protected override void OnHandleCreated(EventArgs args)
        {
            base.OnHandleCreated(args);
            SetProp(Handle, Program.WindowMarker, new IntPtr(Process.GetCurrentProcess().Id));
            UpdateNativeIcons();
        }

        protected override void OnHandleDestroyed(EventArgs args)
        {
            RemoveProp(Handle, Program.WindowMarker);
            base.OnHandleDestroyed(args);
        }

        private void UpdateNativeIcons()
        {
            if (!IsHandleCreated || ownedIcon == null || smallIcon == null) return;
            SetClassLongPtr(Handle, -14, ownedIcon.Handle); // GCLP_HICON
            SetClassLongPtr(Handle, -34, smallIcon.Handle); // GCLP_HICONSM
            SendMessage(Handle, 0x0080, new IntPtr(1), ownedIcon.Handle); // WM_SETICON, ICON_BIG
            SendMessage(Handle, 0x0080, IntPtr.Zero, smallIcon.Handle); // ICON_SMALL
        }

        private void IconNeedsRetry()
        {
            retryIcon = true;
            Text += " (не удалось обновить закреплённый значок)";
        }

        private void ScheduleNextCheck()
        {
            DateTime local = now();
            double milliseconds = (local.Date.AddDays(1) - local).TotalMilliseconds + 100;
            timer.Interval = (int)Math.Max(250, Math.Min(30000, milliseconds));
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == 0x0112)
            {
                long command = message.WParam.ToInt64() & 0xfff0;
                if (command == 0xf120 || command == 0xf030)
                {
                    if (integrateWithShell && command == 0xf120)
                    {
                        ToggleCalendar();
                    }
                    return;
                }
            }
            if (message.Msg == (int)Program.CommandMessage)
            {
                int command = message.WParam.ToInt32();
                if (command == Program.ExitCommand) BeginInvoke(new Action(Close));
                else if (command == Program.StartupOnCommand || command == Program.StartupOffCommand)
                {
                    if (integrateWithShell) ShellIntegration.StartsWithWindows = command == Program.StartupOnCommand;
                }
                else if (command == Program.MenuCommand && integrateWithShell) ShowMenu();
                else if (command == Program.GoogleSetupCommand && integrateWithShell) calendar.ConfigureGoogle();
                else if (command == Program.OpenCalendarCommand && integrateWithShell) ToggleCalendar();
                else RefreshDate(false);
                return;
            }
            if (integrateWithShell && message.Msg == (int)taskbarCreatedMessage) RefreshDate(true);
            base.WndProc(ref message);
        }

        private void ShowMenu()
        {
            calendar.Hide();
            startup.Checked = ShellIntegration.StartsWithWindows;
            menu.Show(Cursor.Position);
        }

        internal void ToggleCalendar()
        {
            RefreshDate(false);
            calendar.Toggle(Cursor.Position);
        }

        protected override void OnFormClosed(FormClosedEventArgs args)
        {
            timer.Stop();
            timer.Dispose();
            menu.Dispose();
            calendar.Dispose();
            if (ownedIcon != null) ownedIcon.Dispose();
            if (smallIcon != null) smallIcon.Dispose();
            if (integrateWithShell)
            {
                SystemEvents.TimeChanged -= OnTimeChanged;
                SystemEvents.PowerModeChanged -= OnPowerChanged;
            }
            base.OnFormClosed(args);
        }

        private void DispatchRefresh()
        {
            if (IsHandleCreated && !IsDisposed)
                BeginInvoke(new Action(delegate { RefreshDate(false); ScheduleNextCheck(); }));
        }
        private void OnTimeChanged(object sender, EventArgs args) { DispatchRefresh(); }
        private void OnPowerChanged(object sender, PowerModeChangedEventArgs args)
        {
            if (args.Mode == PowerModes.Resume) DispatchRefresh();
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern uint RegisterWindowMessage(string name);
        [DllImport("user32.dll", EntryPoint = "SetClassLongPtrW")]
        private static extern IntPtr SetClassLongPtr(IntPtr window, int index, IntPtr value);
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool SetProp(IntPtr window, string name, IntPtr value);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr RemoveProp(IntPtr window, string name);
    }
}
