using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WinCalendar
{
    // A tiny click-through composited accent. The pinned shortcut and its ICO
    // stay untouched; no Explorer cache churn or running-app taskbar underline.
    internal sealed class ReminderOverlay : Form
    {
        private readonly System.Windows.Forms.Timer animation;
        private readonly Action playSound;
        private readonly ReminderChime chime;
        private readonly Stopwatch clock = new Stopwatch();
        private DateTime anchorChecked;
        private Rectangle locatedPin;
        private bool active, refreshing, motion = true;
        internal bool Active { get { return active; } }
        protected override bool ShowWithoutActivation { get { return true; } }

        internal ReminderOverlay() : this(null) { }

        internal ReminderOverlay(Action notificationSound)
        {
            if (notificationSound == null) { chime = new ReminderChime(); playSound = chime.Play; }
            else playSound = notificationSound;
            AutoScaleMode = AutoScaleMode.None; FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false; TopMost = true; StartPosition = FormStartPosition.Manual;
            animation = new System.Windows.Forms.Timer { Interval = 33 };
            animation.Tick += delegate { Render(); };
        }

        protected override CreateParams CreateParams
        {
            get { CreateParams value = base.CreateParams; value.ExStyle = (value.ExStyle | 0x080800a0) & ~0x40000; return value; }
        }

        protected override void OnHandleCreated(EventArgs args)
        {
            base.OnHandleCreated(args);
            SetProp(Handle, "WinCalendar.ReminderAccent", new IntPtr(Process.GetCurrentProcess().Id));
        }
        protected override void OnHandleDestroyed(EventArgs args)
        { RemoveProp(Handle, "WinCalendar.ReminderAccent"); base.OnHandleDestroyed(args); }

        internal void SetActive(bool enabled)
        {
            if (active == enabled) return;
            active = enabled;
            if (!enabled) { animation.Stop(); Hide(); return; }
            try { playSound(); }
            catch (InvalidOperationException) { }
            int enabledMotion;
            motion = SystemParametersInfo(0x1042, 0, out enabledMotion, 0) && enabledMotion != 0 && !SystemInformation.HighContrast;
            animation.Interval = motion ? 33 : 1000;
            clock.Restart(); animation.Start(); Render();
        }

        private async void RefreshAnchor()
        {
            refreshing = true;
            try { await TaskbarAnchor.RefreshAsync(); }
            finally { refreshing = false; }
        }

        private void Render()
        {
            if (!active || IsDisposed) return;
            if (!refreshing && DateTime.UtcNow - anchorChecked > TimeSpan.FromSeconds(2))
            { anchorChecked = DateTime.UtcNow; RefreshAnchor(); }
            Rectangle pin = TaskbarAnchor.Current;
            if (pin.IsEmpty) { Hide(); return; }
            if (locatedPin != pin)
            { Location = new Point(pin.Left + pin.Width / 2 - Width / 2, pin.Top + pin.Height / 2 - Height / 2); locatedPin = pin; }
            int dpi = (int)GetDpiForWindow(Handle), size = (int)Math.Round(48 * dpi / 96.0);
            Point at = new Point(pin.Left + pin.Width / 2 - size / 2, pin.Top + pin.Height / 2 - size / 2);
            using (Bitmap bitmap = DrawFrame(size, motion ? clock.Elapsed.TotalMilliseconds % 1800 / 1800 : 0.35))
            {
                if (!Present(bitmap, at)) { Hide(); return; }
            }
            if (!Visible) Show();
            // Explorer raises the taskbar again during hover and activation.
            // Keep the click-through accent above it without taking focus.
            SetWindowPos(Handle, new IntPtr(-1), 0, 0, 0, 0, 0x0213);
        }

        internal static Bitmap DrawFrame(int size, double phase)
        {
            Bitmap bitmap = new Bitmap(size, size, PixelFormat.Format32bppPArgb);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent); graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.ScaleTransform(size / 48f, size / 48f);
                double breathe = (1 - Math.Cos(phase * 2 * Math.PI)) / 2;
                Color accent = SystemInformation.HighContrast ? SystemColors.Highlight : Color.FromArgb(196, 43, 54);
                float spread = (float)(3 * breathe);
                using (GraphicsPath outline = Rounded(8 - spread, 8 - spread, 32 + 2 * spread, 32 + 2 * spread, 6 + spread))
                using (Pen pen = new Pen(Color.FromArgb((int)(65 + 175 * breathe), accent), (float)(1.25 + 1.25 * breathe))) graphics.DrawPath(pen, outline);
                float radius = (float)(3 + 1.5 * breathe), ripple = (float)(5 + 6 * phase);
                using (Pen ring = new Pen(Color.FromArgb((int)(150 * (1 - phase)), accent), 1.4f))
                    graphics.DrawEllipse(ring, 36 - ripple, 12 - ripple, 2 * ripple, 2 * ripple);
                using (Brush fill = new SolidBrush(Color.FromArgb((int)(180 + 75 * breathe), accent)))
                    graphics.FillEllipse(fill, 36 - radius, 12 - radius, 2 * radius, 2 * radius);
                using (Pen border = new Pen(Color.FromArgb(235, Color.White), 0.9f))
                    graphics.DrawEllipse(border, 36 - radius, 12 - radius, 2 * radius, 2 * radius);
            }
            return bitmap;
        }

        private static GraphicsPath Rounded(float x, float y, float width, float height, float radius)
        {
            GraphicsPath path = new GraphicsPath(); float d = radius * 2;
            path.AddArc(x, y, d, d, 180, 90); path.AddArc(x + width - d, y, d, d, 270, 90);
            path.AddArc(x + width - d, y + height - d, d, d, 0, 90); path.AddArc(x, y + height - d, d, d, 90, 90); path.CloseFigure(); return path;
        }

        private bool Present(Bitmap bitmap, Point at)
        {
            IntPtr screen = GetDC(IntPtr.Zero), memory = CreateCompatibleDC(screen), image = IntPtr.Zero, previous = IntPtr.Zero;
            try
            {
                image = bitmap.GetHbitmap(Color.FromArgb(0)); previous = SelectObject(memory, image);
                NativePoint position = new NativePoint { X = at.X, Y = at.Y }, source = new NativePoint();
                NativeSize size = new NativeSize { Width = bitmap.Width, Height = bitmap.Height };
                Blend blend = new Blend { Operation = 0, Alpha = 255, Format = 1 };
                return UpdateLayeredWindow(Handle, screen, ref position, ref size, memory, ref source, 0, ref blend, 2);
            }
            finally
            {
                if (previous != IntPtr.Zero) SelectObject(memory, previous);
                if (image != IntPtr.Zero) DeleteObject(image);
                if (memory != IntPtr.Zero) DeleteDC(memory);
                if (screen != IntPtr.Zero) ReleaseDC(IntPtr.Zero, screen);
            }
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == 0x84) { message.Result = new IntPtr(-1); return; }
            if (message.Msg == 0x21) { message.Result = new IntPtr(3); return; }
            base.WndProc(ref message);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { animation.Dispose(); if (chime != null) chime.Dispose(); }
            base.Dispose(disposing);
        }

        [StructLayout(LayoutKind.Sequential)] private struct NativePoint { internal int X, Y; }
        [StructLayout(LayoutKind.Sequential)] private struct NativeSize { internal int Width, Height; }
        [StructLayout(LayoutKind.Sequential, Pack = 1)] private struct Blend { internal byte Operation, Flags, Alpha, Format; }
        [DllImport("user32.dll")] private static extern bool SystemParametersInfo(uint action, uint parameter, out int value, uint flags);
        [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr window);
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool SetProp(IntPtr window, string name, IntPtr value);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool RemoveProp(IntPtr window, string name);
        [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr window);
        [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr window, IntPtr dc);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr dc);
        [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr dc, IntPtr value);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr value);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool UpdateLayeredWindow(IntPtr window, IntPtr screen, ref NativePoint position,
            ref NativeSize size, IntPtr source, ref NativePoint origin, uint key, ref Blend blend, uint flags);
    }
}
