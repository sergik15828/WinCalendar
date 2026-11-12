using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WinCalendar
{
    internal sealed class TaskbarMeetingTip : Form
    {
        private readonly Label text;
        private DateTime entered;
        private bool waiting;
        private Font tipFont;
        private int dpi;
        private readonly Func<Rectangle> locate;
        private readonly Func<Point> pointer;
        private readonly Func<DateTime> clock;
        protected override bool ShowWithoutActivation { get { return true; } }
        internal TaskbarMeetingTip(Func<Rectangle> anchor = null, Func<Point> cursor = null, Func<DateTime> now = null)
        {
            locate = anchor ?? delegate { return TaskbarAnchor.Current; };
            pointer = cursor ?? delegate { return Cursor.Position; };
            clock = now ?? delegate { return DateTime.UtcNow; };
            AutoScaleMode = AutoScaleMode.None; FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; TopMost = true; StartPosition = FormStartPosition.Manual;
            text = new Label { Dock = DockStyle.Fill, AutoEllipsis = true, AutoSize = false };
            Controls.Add(text);
        }
        protected override CreateParams CreateParams
        { get { CreateParams p = base.CreateParams; p.ExStyle = (p.ExStyle | 0x080000a0) & ~0x40000; p.ClassStyle |= 0x20000; return p; } }
        internal void UpdateTip(string message, bool enabled)
        {
            Rectangle pin = locate();
            if (!enabled || String.IsNullOrEmpty(message) || pin.IsEmpty || !pin.Contains(pointer()))
            { waiting = false; Hide(); return; }
            if (!waiting) { waiting = true; entered = clock(); }
            if (clock() - entered < TimeSpan.FromMilliseconds(650)) return;
            Screen monitor = Screen.FromRectangle(pin);
            Rectangle work = monitor.WorkingArea;
            if (!IsHandleCreated || Screen.FromPoint(Location).DeviceName != monitor.DeviceName || !Visible)
            { Hide(); Location = new Point(pin.Left, Math.Max(work.Top, work.Bottom - 80)); }
            int actualDpi = (int)GetDpiForWindow(Handle);
            if (actualDpi != dpi)
            {
                Font previous = tipFont; dpi = actualDpi;
                tipFont = new Font("Segoe UI", 9.5f * dpi / 72f, FontStyle.Regular, GraphicsUnit.Pixel); text.Font = tipFont;
                text.Padding = new Padding(12 * dpi / 96, 8 * dpi / 96, 12 * dpi / 96, 8 * dpi / 96);
                if (previous != null) previous.Dispose();
            }
            CalendarPalette palette = CalendarPalette.Create(null); BackColor = text.BackColor = palette.EventSurface; text.ForeColor = palette.Ink;
            if (text.Text != message) text.Text = message;
            int width = Math.Min(360 * dpi / 96, work.Width), height = 64 * dpi / 96;
            Bounds = new Rectangle(Math.Max(work.Left, Math.Min(pin.Left, work.Right - width)), Math.Max(work.Top, work.Bottom - height - 8), width, height);
            if (!Visible) Show();
            SetWindowPos(Handle, new IntPtr(-1), 0, 0, 0, 0, 0x0213);
        }
        protected override void Dispose(bool disposing)
        { base.Dispose(disposing); if (disposing && tipFont != null) tipFont.Dispose(); }
        [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr window);
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
        protected override void WndProc(ref Message message)
        {
            if (message.Msg == 0x84) { message.Result = new IntPtr(-1); return; }
            if (message.Msg == 0x21) { message.Result = new IntPtr(3); return; }
            base.WndProc(ref message);
        }
    }
}
