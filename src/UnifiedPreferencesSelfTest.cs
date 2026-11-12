using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace WinCalendar
{
    internal static class UnifiedPreferencesSelfTest
    {
        internal static void Run(string directory, Action<bool, string> check)
        {
            string language = UiText.Language;
            try
            {
                UiText.Language = "ru";
                AppPreferences defaults = new AppPreferences();
                check(defaults.PaydayEnabled && defaults.PaydayFrequency == 2 && defaults.PaydayFirst == 15 && defaults.PaydaySecond == 1,
                    "Payday countdown defaults to enabled with payments on the fifteenth and first");
                check(PaydaySchedule.Next(new DateTime(2026, 11, 12), 2, 15, 1) == new DateTime(2026, 11, 15)
                    && PaydaySchedule.Next(new DateTime(2026, 11, 15), 2, 15, 1) == new DateTime(2026, 11, 15)
                    && PaydaySchedule.Next(new DateTime(2026, 11, 16), 2, 15, 1) == new DateTime(2026, 12, 1),
                    "Payday schedule handles upcoming, same-day and next-month payments");
                check(PaydaySchedule.Next(new DateTime(2027, 2, 20), 1, 31, 1) == new DateTime(2027, 2, 28)
                    && PaydaySchedule.Next(new DateTime(2028, 2, 20), 1, 31, 1) == new DateTime(2028, 2, 29)
                    && PaydaySchedule.Next(new DateTime(2026, 12, 31), 2, 15, 1) == new DateTime(2027, 1, 1),
                    "Payday schedule clamps short months, respects leap years and rolls over the year");
                check(PaydaySchedule.DaysLabel(1) == "1 день" && PaydaySchedule.DaysLabel(3) == "3 дня" && PaydaySchedule.DaysLabel(11) == "11 дней" && PaydaySchedule.DaysLabel(21) == "21 день",
                    "Payday countdown uses correct Russian day forms");
                using (CalendarPopup popup = new CalendarPopup(() => new DateTime(2026, 11, 12), delegate { }))
                {
                    popup.ShowAt(new Point(400, 700), Screen.PrimaryScreen.WorkingArea);
                    PaydayBadge badge = popup.Panel.Controls.OfType<PaydayBadge>().Single();
                    check(badge.Visible && badge.AccessibleName.Contains("3 дня"), "Calendar header shows the default payday countdown to November fifteenth");
                    IntPtr handle = popup.Handle; Size calendarSize = popup.Bounds.Size;
                    popup.Panel.SettingsButton.PerformClick();
                    check(popup.PreferencesVisible && popup.Handle == handle && popup.Bounds.Height > calendarSize.Height,
                        "Gear opens the unified spacious preferences page in the existing window");
                    PreferencesEditor editor = popup.PreferencesPage;
                    Rectangle footer = editor.BackButton.Bounds; editor.ScrollTo(808);
                    check(editor.ScrollOffset > 0 && editor.BackButton.Bounds == footer && editor.LayoutFits,
                        "Unified preferences scroll their sections while keeping footer actions fixed and all controls in bounds");
                    editor.ScrollTo(0);
                    ComboBox chime = CalendarPanel.Descendants(editor).OfType<ComboBox>().Single(c => c.AccessibleName == "Мелодия напоминания");
                    int selection = chime.SelectedIndex;
                    Panel viewport = editor.Controls.OfType<Panel>().Single(c => c.AutoScroll);
                    editor.PointerPosition = () => viewport.PointToScreen(new Point(20, 20));
                    Message wheel = Message.Create(chime.Handle, 0x20a, new IntPtr(-120L << 16), IntPtr.Zero);
                    bool routed = editor.PreFilterMessage(ref wheel);
                    check(routed && chime.SelectedIndex == selection && (SystemInformation.MouseWheelScrollLines == 0 || editor.ScrollOffset > 0),
                        "Scrolling over preferences routes to the page without accidentally changing a sound choice");
                    editor.ScrollTo(566);
                    CalendarPanel.Descendants(editor).OfType<CheckBox>().Single(c => c.Text == "Показывать дни до зарплаты").Checked = false;
                    editor.Controls.OfType<Button>().Single(c => c.Text == "Сохранить").PerformClick();
                    check(!badge.Visible && !popup.PreferencesVisible && popup.Handle == handle && popup.Bounds.Size == calendarSize,
                        "Saving payday visibility applies immediately and restores the same calendar window size");
                    popup.Panel.SetPreferences(defaults); check(badge.Visible, "Payday countdown can be enabled again without restarting");
                }
                string path = Path.Combine(directory, "payday-preferences.bin");
                try
                {
                    PreferenceStore store = new PreferenceStore(path); store.Save(new AppPreferences { PaydayEnabled = false, PaydayFrequency = 1, PaydayFirst = 27 });
                    AppPreferences loaded = store.Load();
                    check(!loaded.PaydayEnabled && loaded.PaydayFrequency == 1 && loaded.PaydayFirst == 27, "Payday visibility and payment dates survive preference persistence");
                }
                finally { if (File.Exists(path)) File.Delete(path); }
                Capture(directory);
            }
            finally { UiText.Language = language; }
        }
        private static void Capture(string directory)
        {
            foreach (int dpi in new[] { 96, 120, 144 })
            foreach (bool dark in new[] { false, true })
            {
                foreach (string locale in new[] { "ru", "en" })
                {
                    UiText.Language = locale;
                    using (CalendarPopup popup = new CalendarPopup(() => new DateTime(2026, 11, 12), delegate { }))
                    {
                        popup.ShowAt(new Point(400, 700), Screen.PrimaryScreen.WorkingArea); popup.OpenPreferences();
                        AppPreferences options = new AppPreferences { Language = locale };
                        options.Calendars.Add(new CalendarChoice { Id = "fixture-a", Name = "Team", Color = "#2563A6" });
                        options.Calendars.Add(new CalendarChoice { Id = "fixture-b", Name = "Personal", Color = "#7950B3" });
                        popup.PreferencesPage.BeginEdit(options); popup.PreferencesPage.SetDpi(dpi, dark); popup.ViewWindow.ClientSize = popup.PreferencesPage.NaturalSize;
                        using (Bitmap sheet = new Bitmap(popup.PreferencesPage.Width, popup.PreferencesPage.Height * 3))
                        using (Graphics g = Graphics.FromImage(sheet))
                        {
                            int[] scroll = { 0, 566, 1260 };
                            for (int i = 0; i < scroll.Length; i++)
                            {
                                popup.PreferencesPage.ScrollTo(scroll[i]);
                                using (Bitmap image = popup.PreferencesPage.CaptureFrame())
                                    g.DrawImageUnscaled(image, 0, i * image.Height);
                            }
                            sheet.Save(Path.Combine(directory, "settings-unified-" + locale + "-" + (dark ? "dark" : "light") + "-" + dpi + ".png"));
                        }
                    }
                }
                using (Bitmap sheet = new Bitmap(688 * dpi / 96 * 2, 380 * dpi / 96 * 4))
                using (Graphics g = Graphics.FromImage(sheet))
                {
                    DateTime[] dates = { new DateTime(2026, 11, 12), new DateTime(2026, 11, 15), new DateTime(2026, 11, 12), new DateTime(2026, 9, 30) };
                    for (int column = 0; column < 2; column++)
                    {
                        UiText.Language = column == 0 ? "ru" : "en";
                        for (int row = 0; row < dates.Length; row++)
                        using (CalendarPanel panel = new CalendarPanel(dates[row]))
                        {
                            panel.SetDpi(dpi); panel.Size = panel.NaturalSize; panel.ApplyTheme(dark); panel.SetPreferences(new AppPreferences { PaydayEnabled = row != 2 });
                            using (Bitmap image = new Bitmap(panel.Width, panel.Height))
                            { panel.DrawToBitmap(image, panel.ClientRectangle); g.DrawImageUnscaled(image, column * image.Width, row * image.Height); }
                        }
                    }
                    sheet.Save(Path.Combine(directory, "payday-calendar-" + (dark ? "dark" : "light") + "-" + dpi + ".png"));
                }
            }
        }
    }
}
