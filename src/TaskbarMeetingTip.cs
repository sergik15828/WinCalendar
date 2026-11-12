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
        private readonly Timer hoverTimer;
        private string message;
        private bool enabled;
        protected override bool ShowWithoutActivation { get { return true; } }
        internal TaskbarMeetingTip(Func<Rectangle> anchor = null, Func<Point> cursor = null, Func<DateTime> now = null)
        {
            locate = anchor ?? delegate { return TaskbarAnchor.HoverArea; };
            pointer = cursor ?? delegate { return Cursor.Position; };
            clock = now ?? delegate { return DateTime.UtcNow; };
            AutoScaleMode = AutoScaleMode.None; FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; TopMost = true; StartPosition = FormStartPosition.Manual;
            DoubleBuffered = true;
            text = new Label { Dock = DockStyle.Fill, AutoEllipsis = true, AutoSize = false };
            Controls.Add(text);
            hoverTimer = new Timer { Interval = 40 }; hoverTimer.Tick += delegate { Observe(); }; hoverTimer.Start();
        }
        protected override CreateParams CreateParams
        { get { CreateParams p = base.CreateParams; p.ExStyle = (p.ExStyle | 0x080000a0) & ~0x40000; return p; } }
        internal void UpdateTip(string message, bool enabled)
        { this.message = message; this.enabled = enabled; Observe(); }
        private void Observe()
        {
            if (IsDisposed) return;
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
                text.Padding = new Padding(8 * dpi / 96, 6 * dpi / 96, 8 * dpi / 96, 6 * dpi / 96);
                if (previous != null) previous.Dispose();
            }
            CalendarPalette palette = CalendarPalette.Create(null); BackColor = text.BackColor = palette.EventSurface; text.ForeColor = palette.Ink;
            if (text.Text != message) text.Text = message;
            int padding = 16 * dpi / 96, maximum = Math.Min(300 * dpi / 96, work.Width);
            Size measured = TextRenderer.MeasureText(message, tipFont, new Size(Math.Max(1, maximum - padding), Int32.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
            int width = Math.Min(maximum, Math.Max(120 * dpi / 96, measured.Width + padding));
            int height = Math.Min(2 * tipFont.Height, measured.Height) + 12 * dpi / 96;
            int anchorX = pin.Width > pin.Height * 2 ? pin.Left + 46 * dpi / 96 : pin.Left;
            Rectangle target = new Rectangle(Math.Max(work.Left, Math.Min(anchorX, work.Right - width)), Math.Max(work.Top, Math.Min(pin.Top, work.Bottom) - height - 6 * dpi / 96), width, height);
            if (Bounds != target) Bounds = target;
            if (!Visible) { Show(); SetWindowPos(Handle, new IntPtr(-1), 0, 0, 0, 0, 0x0213); }
        }
        protected override void Dispose(bool disposing)
        { if (disposing) hoverTimer.Dispose(); base.Dispose(disposing); if (disposing && tipFont != null) tipFont.Dispose(); }
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
