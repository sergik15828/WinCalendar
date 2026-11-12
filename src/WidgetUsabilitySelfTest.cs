using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinCalendar
{
    internal static class WidgetUsabilitySelfTest
    {
        internal static async Task RunAsync(string directory, Action<bool, string> check)
        {
            string language = UiText.Language;
            Rectangle savedIcon = TaskbarAnchor.Prototype, savedHit = TaskbarAnchor.PrototypeHitArea;
            bool motion = TaskbarAnchor.ReminderMotion;
            try
            {
                UiText.Language = "en";
                check(!DateTaskbarWindow.HostButtonNeeded(true, false) && !DateTaskbarWindow.HostButtonNeeded(false, true) && DateTaskbarWindow.HostButtonNeeded(false, false),
                    "Widget-only mode does not recreate an application button after unpinning; legacy unpinned mode remains reachable");
                check(UiText.Get("Настройки") == "Settings" && DateText.WeekdayAbbreviation(new DateTime(2026, 11, 12)) == "TH"
                    && new DateTime(2026, 11, 12).ToString("MMMM", DateText.Culture) == "November", "English resources localize interface, dates and weekday icon");
                using (CalendarPopup popup = new CalendarPopup(() => new DateTime(2026, 11, 12, 9, 0, 0), delegate { }))
                {
                    popup.ShowAt(new Point(400, 500), Screen.PrimaryScreen.WorkingArea);
                    popup.Panel.SetData(new CalendarViewData { Connection = CalendarConnection.Connected, HasSnapshot = true, Message = "Google calendars",
                        Events = new System.Collections.Generic.List<CalendarEvent> { new CalendarEvent { Id = "locale-fixture", Title = "Название пользователя", CalendarName = "Личный календарь",
                            Start = "2026-11-12T10:00:00+03:00", End = "2026-11-12T11:00:00+03:00", Url = "https://calendar.google.com/calendar/event?eid=fixture" } } });
                    check(popup.Panel.Controls.OfType<Label>().Any(c => c.Text == "Meetings") && popup.Panel.EventList.Items[0].ToString().Contains("Название пользователя"),
                        "English controls preserve user-provided meeting titles");
                    UiText.Language = "ru"; UiText.Apply(popup.Panel); popup.Panel.SetPreferences(new AppPreferences());
                    bool russian = popup.Panel.Controls.OfType<Label>().Any(c => c.Text == "Встречи");
                    UiText.Language = "en"; UiText.Apply(popup.Panel); popup.Panel.SetPreferences(new AppPreferences { Language = "en" });
                    check(russian && popup.Panel.Controls.OfType<Label>().Any(c => c.Text == "Meetings"), "Existing calendar controls switch languages both ways without recreating the window");
                }
                AppPreferences options = new AppPreferences { Language = "en" };
                string path = Path.Combine(directory, "language-preferences.bin");
                try { PreferenceStore store = new PreferenceStore(path); store.Save(options); check(store.Load().Language == "en", "Interface language survives preference persistence"); }
                finally { if (File.Exists(path)) File.Delete(path); }
                using (Form host = new Form { AutoScaleMode = AutoScaleMode.None })
                using (PreferencesEditor editor = new PreferencesEditor())
                {
                    host.Controls.Add(editor); host.ClientSize = editor.Size; host.Show(); editor.BeginEdit(options); editor.ShowPage(3);
                    AppPreferences saved = null; editor.SaveRequested += value => saved = value;
                    ComboBox selector = CalendarPanel.Descendants(editor).OfType<ComboBox>().Single(c => c.AccessibleName == "Language / Язык");
                    selector.SelectedIndex = 0;
                    CalendarPanel.Descendants(editor).OfType<CheckBox>().Single(c => c.Text == "Widget beside weather").Checked = false;
                    CalendarPanel.Descendants(editor).OfType<RadioButton>().Single(c => c.Text == "2 minutes before").Checked = true;
                    editor.Controls.OfType<Button>().Single(b => b.Text == "Save").PerformClick();
                    check(saved != null && saved.Language == "ru" && saved.WeatherWidgetChange == false && saved.ReminderTiming == 2,
                        "Language and widget switch collect explicit save choices without changing live state");
                    check(!Json.Serialize(saved).Contains("WeatherWidgetChange"), "Legacy widget flag remains the single persistent source for widget visibility");
                }
                Rectangle work = Screen.PrimaryScreen.WorkingArea;
                Rectangle hover = new Rectangle(work.Left + 160, work.Bottom, 200, 55);
                TaskbarAnchor.PrototypeHitArea = hover; TaskbarAnchor.Prototype = new Rectangle(hover.Left, hover.Top, 55, 55);
                Point pointer = new Point(hover.Right - 5, hover.Top + 15); DateTime now = DateTime.UtcNow;
                using (TaskbarMeetingTip tip = new TaskbarMeetingTip(null, () => pointer, () => now))
                {
                    tip.UpdateTip("In 5 min · Design review", true); now = now.AddSeconds(1); tip.UpdateTip("In 5 min · Design review", true);
                    check(tip.Visible, "Next meeting tooltip responds over the text end of the full widget");
                    Rectangle position = tip.Bounds; pointer.X -= 75; tip.UpdateTip("In 5 min · Design review", true);
                    check(tip.Bounds == position && tip.Height < 64 * 120 / 96, "Compact tooltip stays anchored while the pointer moves inside the widget");
                    using (Bitmap image = new Bitmap(tip.Width, tip.Height)) { tip.DrawToBitmap(image, tip.ClientRectangle); image.Save(Path.Combine(directory, "widget-compact-tooltip.png")); }
                    tip.UpdateTip(null, false); await Task.Delay(120);
                    check(!tip.Visible, "Acknowledgement disables tooltip observation so it cannot reopen behind the calendar");
                }
                TaskbarAnchor.ReminderMotion = true;
                using (Bitmap bright = WeatherWidgetRenderer.Draw(new DateTime(2026, 11, 12), 160, 44, true, false, true, 0))
                using (Bitmap dim = WeatherWidgetRenderer.Draw(new DateTime(2026, 11, 12), 160, 44, true, false, true, 0.85))
                {
                    bool textUnchanged = true;
                    for (int y = 0; y < bright.Height; y++) for (int x = 46; x < 160; x++) if (bright.GetPixel(x, y) != dim.GetPixel(x, y)) textUnchanged = false;
                    int header = WeatherWidgetRenderer.ReminderHeadroom + 14;
                    check(bright.GetPixel(22, header).A == dim.GetPixel(22, header).A && bright.GetPixel(22, header).A == 255 && textUnchanged,
                        "Reminder keeps the symbol fully opaque and date text unchanged with no dimming");
                }
                using (Bitmap resting = WeatherWidgetRenderer.Draw(new DateTime(2026, 11, 12), 160, 44, true, false, true, 0))
                using (Bitmap hop = WeatherWidgetRenderer.Draw(new DateTime(2026, 11, 12), 160, 44, true, false, true, 0.14))
                using (Bitmap secondHop = WeatherWidgetRenderer.Draw(new DateTime(2026, 11, 12), 160, 44, true, false, true, 0.375))
                using (Bitmap thirdHop = WeatherWidgetRenderer.Draw(new DateTime(2026, 11, 12), 160, 44, true, false, true, 0.53))
                    check(FirstInk(resting, 19) - FirstInk(hop, 19) >= 23 && FirstInk(resting, 19) - FirstInk(secondHop, 19) >= 9
                        && FirstInk(resting, 19) - FirstInk(thirdHop, 19) >= 2,
                        "Dock-style reminder has one high jump and two diminishing rebounds without moving text or hit target");
                using (Bitmap normal = WeatherWidgetRenderer.Draw(new DateTime(2026, 11, 12), 160, 44, true, false))
                using (Bitmap alert = WeatherWidgetRenderer.Draw(new DateTime(2026, 11, 12), 160, 44, true, false, true, 0.14))
                {
                    bool aligned = true;
                    for (int y = 0; y < normal.Height; y++) for (int x = 46; x < normal.Width; x++)
                        if (normal.GetPixel(x, y) != alert.GetPixel(x, y + WeatherWidgetRenderer.ReminderHeadroom)) aligned = false;
                    check(aligned, "Transparent bounce headroom preserves the exact global date and weekday pixels");
                }
                TaskbarAnchor.ReminderMotion = false;
                using (Font font = WeatherWidgetRenderer.DateFont())
                { File.WriteAllText(Path.Combine(directory, "widget-font.txt"), "Requested=Segoe UI Semibold\r\nResolved=" + font.Name + "\r\nStyle=" + font.Style); check(font.Name == "Segoe UI Semibold", "Date renderer resolves the selected Semibold font without falling back"); }
                using (Bitmap first = WeatherWidgetRenderer.Draw(new DateTime(2026, 11, 12), 160, 44, true, false, true, 0))
                using (Bitmap second = WeatherWidgetRenderer.Draw(new DateTime(2026, 11, 12), 160, 44, true, false, true, 0.5))
                    check(first.GetPixel(22, 14) == second.GetPixel(22, 14), "Reduced motion keeps a static reminder instead of blinking");
                Capture(directory);
            }
            finally { UiText.Language = language; TaskbarAnchor.Prototype = savedIcon; TaskbarAnchor.PrototypeHitArea = savedHit; TaskbarAnchor.ReminderMotion = motion; }
        }

        private static void Capture(string directory)
        {
            foreach (int dpi in new[] { 96, 120, 144 })
            foreach (bool dark in new[] { false, true })
            {
                UiText.Language = "en";
                using (CalendarPopup popup = new CalendarPopup(() => new DateTime(2026, 11, 12, 9, 0, 0), delegate { }))
                using (Bitmap sheet = new Bitmap(688 * dpi / 96, 380 * dpi / 96 * 3))
                using (Graphics g = Graphics.FromImage(sheet))
                {
                    g.Clear(dark ? Color.FromArgb(32, 32, 32) : Color.FromArgb(250, 250, 250));
                    popup.ShowAt(new Point(400, 500), Screen.PrimaryScreen.WorkingArea);
                    popup.Panel.SetDpi(dpi); popup.ViewWindow.ClientSize = popup.Panel.NaturalSize; popup.Panel.ApplyTheme(dark);
                    Tile(g, popup.Panel, 0, dpi);
                    popup.Panel.ShowSettings(true); Tile(g, popup.Panel, 1, dpi);
                    popup.Panel.Visible = false; popup.Editor.Visible = true; popup.Editor.BringToFront(); popup.Editor.SetDpi(dpi, dark); Tile(g, popup.Editor, 2, dpi);
                    sheet.Save(Path.Combine(directory, "english-core-" + (dark ? "dark" : "light") + "-" + dpi + ".png"));
                }
                using (Bitmap sheet = new Bitmap(480 * dpi / 96, (WeatherWidgetRenderer.Height + WeatherWidgetRenderer.ReminderHeadroom) * 2 * dpi / 96))
                using (Graphics g = Graphics.FromImage(sheet))
                {
                    g.Clear(dark ? Color.FromArgb(32, 32, 32) : Color.FromArgb(243, 243, 243)); TaskbarAnchor.ReminderMotion = true;
                    double[] phases = { 0, 0.14, 0.85 };
                    for (int row = 0; row < 2; row++) for (int phase = 0; phase < phases.Length; phase++)
                    {
                        UiText.Language = row == 0 ? "ru" : "en";
                        using (Bitmap frame = WeatherWidgetRenderer.Draw(new DateTime(2026, 11, 12), 160 * dpi / 96, 44 * dpi / 96, dark, false, true, phases[phase]))
                            g.DrawImageUnscaled(frame, phase * frame.Width, row * frame.Height);
                    }
                    sheet.Save(Path.Combine(directory, "widget-blink-" + (dark ? "dark" : "light") + "-" + dpi + ".png"));
                }
            }
        }
        private static void Tile(Graphics g, Control view, int index, int dpi)
        {
            using (Bitmap image = new Bitmap(view.Width, view.Height))
            { view.DrawToBitmap(image, view.ClientRectangle); g.DrawImageUnscaled(image, 0, index * 380 * dpi / 96); }
        }
        private static int FirstInk(Bitmap image, int x)
        { for (int y = 0; y < image.Height; y++) if (image.GetPixel(x, y).A > 50) return y; return image.Height; }
    }
}
