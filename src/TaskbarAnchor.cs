using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace WinCalendar
{
    internal static class TaskbarAnchor
    {
        private static readonly object Gate = new object();
        private static Rectangle cached;
        private static Task<Rectangle> pending;
        private static DateTime checkedAt;
        internal static Rectangle Current { get { lock (Gate) return cached; } }

        internal static Task<Rectangle> RefreshAsync()
        {
            lock (Gate)
            {
                if (pending != null && !pending.IsCompleted) return pending;
                if (DateTime.UtcNow - checkedAt < TimeSpan.FromSeconds(2)) return Task.FromResult(cached);
                checkedAt = DateTime.UtcNow;
                pending = Task.Run(delegate
                {
                    Rectangle found = Find();
                    lock (Gate) cached = found;
                    return found;
                });
                return pending;
            }
        }

        private static Rectangle Find()
        {
            IntPtr previous = SetThreadDpiAwarenessContext(new IntPtr(-4));
            try
            {
                IntPtr taskbar = FindWindow("Shell_TrayWnd", null);
                if (taskbar == IntPtr.Zero) return Rectangle.Empty;
                AutomationElement root = AutomationElement.FromHandle(taskbar);
                AutomationElementCollection buttons = root.FindAll(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
                foreach (AutomationElement button in buttons)
                {
                    string name = button.Current.Name;
                    if (!IsOwnButtonName(name)) continue;
                    if (button.Current.IsOffscreen) return Rectangle.Empty;
                    System.Windows.Rect bounds = button.Current.BoundingRectangle;
                    return bounds.IsEmpty ? Rectangle.Empty : Rectangle.FromLTRB((int)bounds.Left, (int)bounds.Top, (int)bounds.Right, (int)bounds.Bottom);
                }
            }
            catch (Exception) { return Rectangle.Empty; }
            finally { SetThreadDpiAwarenessContext(previous); }
            return Rectangle.Empty;
        }

        // Windows adds localized text such as "pinned" around the app name.
        // Match the complete product name inside that accessible description.
        internal static bool IsOwnButtonName(string name)
        {
            return name != null && Regex.IsMatch(name, @"(?<![\p{L}\p{N}_])(?:WinCalendar|WinDate)(?![\p{L}\p{N}_])",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr FindWindow(string className, string title);
        [DllImport("user32.dll")] private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
    }
}
