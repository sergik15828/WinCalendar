using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;
using System.Windows.Forms;

namespace WinCalendar
{
    internal sealed class WeatherPlacement
    {
        internal IntPtr Taskbar;
        internal Rectangle Bar, DateBounds;
        private readonly DateTime validatedAt = DateTime.UtcNow;
        internal static WeatherPlacement Find(WeatherPlacement previousPlacement = null)
        {
            IntPtr previous = SetThreadDpiAwarenessContext(new IntPtr(-4));
            try
            {
                IntPtr bar = FindWindow("Shell_TrayWnd", null); NativeRect native;
                if (bar == IntPtr.Zero || !GetWindowRect(bar, out native)) return null;
                Rectangle bounds = native.Rectangle;
                if (bounds.Width < bounds.Height) return null;
                AutomationElement root = AutomationElement.FromHandle(bar);
                AutomationElementCollection buttons = root.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
                List<Rectangle> occupied = new List<Rectangle>(); Rectangle weather = Rectangle.Empty;
                foreach (AutomationElement button in buttons)
                {
                    if (button.Current.IsOffscreen) continue;
                    System.Windows.Rect r = button.Current.BoundingRectangle;
                    if (r.IsEmpty) continue;
                    Rectangle rect = Rectangle.FromLTRB((int)r.Left, (int)r.Top, (int)r.Right, (int)r.Bottom);
                    if (rect.Width <= 0 || rect.Height <= 0) continue;
                    occupied.Add(rect);
                    if (IsWeather(button.Current.AutomationId) && (weather.IsEmpty || rect.Width > weather.Width)) weather = rect;
                }
                double scale = GetDpiForWindow(bar) / 96.0;
                int height = Math.Min(bounds.Height - 4, (int)Math.Round(WeatherWidgetRenderer.Height * scale));
                int width = (int)Math.Round(WeatherWidgetRenderer.Width * height / (double)WeatherWidgetRenderer.Height);
                Rectangle date = Place(bounds, weather, occupied, width, height);
                if (weather.IsEmpty && CanRetain(previousPlacement) && previousPlacement.Bar == bounds && !occupied.Any(r => r.IntersectsWith(previousPlacement.DateBounds)))
                    return previousPlacement;
                return date.IsEmpty ? null : new WeatherPlacement { Taskbar = bar, Bar = bounds, DateBounds = date };
            }
            catch (Exception) { return CanRetain(previousPlacement) ? previousPlacement : null; }
            finally { SetThreadDpiAwarenessContext(previous); }
        }
        private static bool CanRetain(WeatherPlacement previous)
        { return previous != null && DateTime.UtcNow - previous.validatedAt < TimeSpan.FromSeconds(3) && TaskbarExposed(previous); }
        internal static bool IsWeather(string id)
        {
            // Match shell entry-point identities, never an application's title.
            // A browser tab or a document can itself be named "Weather".
            return new[] { "AugmentedEntryPointButton", "DynamicContent", "DynamicContent2", "WidgetsButton", "Widgets" }
                .Any(known => string.Equals(id, known, StringComparison.OrdinalIgnoreCase));
        }
        internal static Rectangle Place(Rectangle bar, Rectangle weather, IEnumerable<Rectangle> occupied, int size, int height = 0)
        {
            if (height == 0) height = size;
            if (weather.IsEmpty || size < 24 || !bar.Contains(weather)) return Rectangle.Empty;
            int gap = Math.Max(4, height / 8), y = bar.Top + (bar.Height - height) / 2;
            bool leftEdge = weather.Left + weather.Width / 2 < bar.Left + bar.Width / 2;
            foreach (bool right in new[] { leftEdge, !leftEdge })
            {
                int x = right ? weather.Right + gap : weather.Left - gap - size;
                Rectangle candidate = new Rectangle(x, y, size, height);
                if (bar.Contains(candidate) && !occupied.Any(r => r.IntersectsWith(candidate))) return candidate;
            }
            return Rectangle.Empty;
        }
        internal static bool TaskbarExposed(WeatherPlacement placement)
        {
            NativeRect rect;
            if (!IsWindowVisible(placement.Taskbar) || !GetWindowRect(placement.Taskbar, out rect) || rect.Rectangle != placement.Bar) return false;
            Rectangle screen = Screen.FromRectangle(placement.Bar).Bounds;
            if (Rectangle.Intersect(screen, placement.Bar).Height < placement.Bar.Height - 2) return false;
            IntPtr foreground = GetForegroundWindow(); uint owner;
            GetWindowThreadProcessId(foreground, out owner);
            if (owner == (uint)System.Diagnostics.Process.GetCurrentProcess().Id || foreground == placement.Taskbar) return true;
            StringBuilder className = new StringBuilder(128); GetClassName(foreground, className, className.Capacity);
            if (className.ToString() == "Progman" || className.ToString() == "WorkerW") return true;
            if (GetWindowRect(foreground, out rect) && rect.Rectangle.Contains(screen)) return false;
            return true;
        }
        [StructLayout(LayoutKind.Sequential)] private struct NativeRect
        { internal int Left, Top, Right, Bottom; internal Rectangle Rectangle { get { return Rectangle.FromLTRB(Left, Top, Right, Bottom); } } }
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr FindWindow(string className, string title);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr h, out NativeRect rect);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr h);
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr h, out uint process);
        [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr h);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr h, StringBuilder text, int count);
        [DllImport("user32.dll")] private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
    }
}
