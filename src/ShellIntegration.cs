using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using Microsoft.Win32;

namespace WinCalendar
{
    internal static class ShellIntegration
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private static readonly Guid PropertySet = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3");

        internal static string Executable
        {
            get { return System.Reflection.Assembly.GetExecutingAssembly().Location; }
        }

        internal static string IconPath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinCalendar", "WeekdayDate.ico"); }
        }

        internal static string ShortcutPath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "WinCalendar", "WinCalendar.lnk"); }
        }

        internal static string SettingsShortcutPath
        {
            get { return Path.Combine(Path.GetDirectoryName(ShortcutPath), "WinCalendar Settings.lnk"); }
        }

        private static string LegacyExecutable
        {
            get { return Path.Combine(Path.GetDirectoryName(Executable), "WinCalendar.exe"); }
        }

        internal static string StartupCommand
        {
            get { return "\"" + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe") + "\" \"" + StartupShortcutPath + "\""; }
        }

        internal static string StartupShortcutPath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinCalendar", "Startup.lnk"); }
        }

        private static string PreviousStartupCommand
        {
            get { return "\"" + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe") + "\" \"" + Executable + "\""; }
        }

        internal static bool HasPinnedShortcut()
        {
            string pins = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar");
            if (!Directory.Exists(pins)) return false;
            foreach (string shortcut in Directory.GetFiles(pins, "*.lnk"))
            {
                IShellLinkW link = null;
                try
                {
                    link = (IShellLinkW)new ShellLink();
                    ((IPersistFile)link).Load(shortcut, 0);
                    StringBuilder target = new StringBuilder(32768);
                    link.GetPath(target, target.Capacity, IntPtr.Zero, 4);
                    if (String.Equals(target.ToString(), Executable, StringComparison.OrdinalIgnoreCase)) return true;
                }
                catch (COMException) { }
                catch (IOException) { }
                finally { if (link != null) Marshal.FinalReleaseComObject(link); }
            }
            return false;
        }

        internal static void EnsureAppShortcut()
        {
            CalendarIcon.Save(IconPath, CalendarIcon.Create(DateTime.Today));
            CreateAppShortcut(StartupShortcutPath, IconPath, "--background");
            // Preserve an already enabled startup entry when migrating the binary
            // from the calendar prototype to the date-only executable.
            if (!String.Equals(Executable, LegacyExecutable, StringComparison.OrdinalIgnoreCase))
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (key != null)
                    {
                        string existing = key.GetValue("WinCalendar") as string;
                        if (String.Equals(existing, "\"" + LegacyExecutable + "\" --background", StringComparison.OrdinalIgnoreCase)
                            || String.Equals(existing, "\"" + Executable + "\" --background", StringComparison.OrdinalIgnoreCase)
                            || String.Equals(existing, PreviousStartupCommand, StringComparison.OrdinalIgnoreCase))
                            key.SetValue("WinCalendar", StartupCommand);
                    }
                }
            }
            CreateAppShortcut(ShortcutPath, IconPath);
            CreateAppShortcut(SettingsShortcutPath, IconPath, "--menu");
        }

        internal static void CreateAppShortcut(string shortcutPath, string iconPath, string arguments = "")
        {
            bool existed = File.Exists(shortcutPath);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(shortcutPath)));
            IShellLinkW link = (IShellLinkW)new ShellLink();
            try
            {
                link.SetPath(Executable);
                link.SetArguments(arguments);
                link.SetWorkingDirectory(Path.GetDirectoryName(Executable));
                link.SetDescription(UiText.Get("Текущая дата в панели задач"));
                link.SetIconLocation(iconPath, 0);
                ((IPersistFile)link).Save(shortcutPath, true);
            }
            finally { Marshal.FinalReleaseComObject(link); }
            NotifyPath(existed ? 0x00002000u : 0x00000002u, shortcutPath);
        }

        internal static string GetShortcutAppId(string shortcutPath)
        {
            IShellLinkW link = (IShellLinkW)new ShellLink();
            try
            {
                ((IPersistFile)link).Load(shortcutPath, 0);
                return GetString((IPropertyStore)link, 5);
            }
            finally { Marshal.FinalReleaseComObject(link); }
        }

        internal static string GetWindowAppId(IntPtr window)
        {
            Guid iid = typeof(IPropertyStore).GUID;
            IPropertyStore store;
            Marshal.ThrowExceptionForHR(SHGetPropertyStoreForWindow(window, ref iid, out store));
            try
            {
                return GetString(store, 5);
            }
            finally { Marshal.FinalReleaseComObject(store); }
        }

        private static string GetString(IPropertyStore store, uint id)
        {
            PropertyKey key = new PropertyKey { FormatId = PropertySet, Id = id };
            PropVariant value;
            Marshal.ThrowExceptionForHR(store.GetValue(ref key, out value));
            try { return value.Type == 31 ? Marshal.PtrToStringUni(value.Pointer) : null; }
            finally { PropVariantClear(ref value); }
        }

        internal static void RefreshPinnedIcon(string iconPath)
        {
            // Explorer caches the group icon independently from Window.Icon.
            // Like Chromium's profile icon updater, invalidate the icon cache and
            // notify each affected pinned shortcut specifically on Windows 11.
            SHChangeNotify(0x08000000, 0, IntPtr.Zero, IntPtr.Zero); // SHCNE_ASSOCCHANGED
            NotifyPath(0x00002000, iconPath); // SHCNE_UPDATEITEM
            if (File.Exists(ShortcutPath)) NotifyPath(0x00002000, ShortcutPath);
            string pins = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar");
            if (!Directory.Exists(pins)) return;
            foreach (string shortcut in Directory.GetFiles(pins, "*.lnk"))
            {
                IShellLinkW link = null;
                try
                {
                    link = (IShellLinkW)new ShellLink();
                    ((IPersistFile)link).Load(shortcut, 0);
                    StringBuilder target = new StringBuilder(32768);
                    link.GetPath(target, target.Capacity, IntPtr.Zero, 4);
                    string targetPath = Path.GetFullPath(target.ToString());
                    bool migrate = String.Equals(targetPath, LegacyExecutable, StringComparison.OrdinalIgnoreCase)
                        && !String.Equals(Executable, LegacyExecutable, StringComparison.OrdinalIgnoreCase);
                    if (!migrate && !String.Equals(targetPath, Executable, StringComparison.OrdinalIgnoreCase)) continue;
                    // Only this executable's own pin is updated; keep order and identity.
                    StringBuilder existing = new StringBuilder(32768);
                    int index;
                    link.GetIconLocation(existing, existing.Capacity, out index);
                    if (migrate || !String.Equals(existing.ToString(), iconPath, StringComparison.OrdinalIgnoreCase) || index != 0)
                    {
                        // Load the matching shortcut writable only when it needs changing.
                        ((IPersistFile)link).Load(shortcut, 2);
                        if (migrate)
                        {
                            link.SetPath(Executable);
                            IPropertyStore properties = (IPropertyStore)link;
                            for (uint id = 2; id <= 5; id++)
                            {
                                PropertyKey key = new PropertyKey { FormatId = PropertySet, Id = id };
                                PropVariant empty = new PropVariant();
                                Marshal.ThrowExceptionForHR(properties.SetValue(ref key, ref empty));
                            }
                            Marshal.ThrowExceptionForHR(properties.Commit());
                        }
                        link.SetIconLocation(iconPath, 0);
                        ((IPersistFile)link).Save(shortcut, true);
                    }
                    NotifyPath(0x00002000, shortcut);
                }
                catch (COMException) { /* A concurrently removed or inaccessible pin can be retried next day. */ }
                catch (IOException) { }
                catch (ArgumentException) { }
                catch (UnauthorizedAccessException) { }
                finally { if (link != null) Marshal.FinalReleaseComObject(link); }
            }
        }

        private static void NotifyPath(uint change, string path)
        {
            IntPtr text = Marshal.StringToCoTaskMemUni(path);
            try { SHChangeNotify(change, 0x0005, text, IntPtr.Zero); } // SHCNF_PATHW
            finally { Marshal.FreeCoTaskMem(text); }
        }

        internal static bool StartsWithWindows
        {
            get
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey))
                {
                    if (key == null) return false;
                    string value = key.GetValue("WinCalendar") as string;
                    return String.Equals(value, StartupCommand, StringComparison.OrdinalIgnoreCase)
                        || String.Equals(value, PreviousStartupCommand, StringComparison.OrdinalIgnoreCase)
                        || String.Equals(value, "\"" + Executable + "\" --background", StringComparison.OrdinalIgnoreCase);
                }
            }
            set
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKey))
                {
                    if (value)
                    {
                        CreateAppShortcut(StartupShortcutPath, IconPath, "--background");
                        key.SetValue("WinCalendar", StartupCommand);
                    }
                    else key.DeleteValue("WinCalendar", false);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PropertyKey { public Guid FormatId; public uint Id; }
        [StructLayout(LayoutKind.Sequential)]
        private struct Blob { public int Length; public IntPtr Data; }
        [StructLayout(LayoutKind.Explicit)]
        private struct PropVariant
        {
            [FieldOffset(0)] public ushort Type;
            [FieldOffset(8)] public IntPtr Pointer;
            [FieldOffset(8)] public Blob Blob;
        }
        [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPropertyStore
        {
            [PreserveSig] int GetCount(out uint count);
            [PreserveSig] int GetAt(uint index, out PropertyKey key);
            [PreserveSig] int GetValue(ref PropertyKey key, out PropVariant value);
            [PreserveSig] int SetValue(ref PropertyKey key, ref PropVariant value);
            [PreserveSig] int Commit();
        }
        [ComImport, Guid("00021401-0000-0000-C000-000000000046")]
        private class ShellLink { }
        [ComImport, Guid("000214F9-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder path, int maxPath, IntPtr findData, uint flags);
            void GetIDList(out IntPtr itemList);
            void SetIDList(IntPtr itemList);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder text, int max);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string text);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder path, int max);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string path);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder arguments, int max);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string arguments);
            void GetHotkey(out short hotkey);
            void SetHotkey(short hotkey);
            void GetShowCmd(out int command);
            void SetShowCmd(int command);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder path, int max, out int index);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string path, int index);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string path, uint reserved);
            void Resolve(IntPtr window, uint flags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string path);
        }
        [DllImport("shell32.dll")]
        private static extern int SHGetPropertyStoreForWindow(IntPtr window, ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out IPropertyStore store);
        [DllImport("ole32.dll")]
        private static extern int PropVariantClear(ref PropVariant value);
        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(uint change, uint flags, IntPtr item1, IntPtr item2);
    }
}
