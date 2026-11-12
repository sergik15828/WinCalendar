using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WinCalendar
{
    internal sealed class CalendarPopup : IDisposable
    {
        private readonly Func<DateTime> clock;
        private readonly ToolStripControlHost host;
        private readonly CalendarDropDown dropDown;
        private Rectangle anchorArea;
        private DateTime dismissedAtAnchor = DateTime.MinValue;
        internal CalendarPanel Panel { get; private set; }
        internal bool Visible { get { return dropDown.Visible; } }
        internal IntPtr Handle { get { return dropDown.Handle; } }
        internal Rectangle Bounds { get { return dropDown.Bounds; } }

        internal CalendarPopup(Func<DateTime> now, Action openGoogle)
        {
            clock = now;
            Panel = new CalendarPanel(now());
            host = new ToolStripControlHost(Panel) { AutoSize = false, Size = Panel.Size, Margin = Padding.Empty, Padding = Padding.Empty };
            dropDown = new CalendarDropDown { AutoClose = true, Padding = Padding.Empty, Margin = Padding.Empty, DropShadowEnabled = true };
            dropDown.Items.Add(host);
            Panel.GoogleRequested += delegate { Hide(); openGoogle(); };
            Panel.DismissRequested += delegate { Hide(); };
            dropDown.Closed += delegate(object sender, ToolStripDropDownClosedEventArgs args)
            {
                if ((args.CloseReason == ToolStripDropDownCloseReason.AppClicked || args.CloseReason == ToolStripDropDownCloseReason.AppFocusChange)
                    && anchorArea.Contains(Cursor.Position)) dismissedAtAnchor = DateTime.UtcNow;
            };
        }

        internal void Toggle(Point anchor)
        {
            if (Visible) { Hide(); return; }
            // Taskbar click closes the native popup before the launcher forwards
            // its command. Treat that same click as close rather than reopening.
            if ((DateTime.UtcNow - dismissedAtAnchor).TotalMilliseconds < 1500)
            {
                dismissedAtAnchor = DateTime.MinValue; return;
            }
            ShowAt(anchor, Screen.FromPoint(anchor).WorkingArea);
        }

        internal void ShowAt(Point anchor, Rectangle work)
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
        public void Dispose() { dropDown.Dispose(); }

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
