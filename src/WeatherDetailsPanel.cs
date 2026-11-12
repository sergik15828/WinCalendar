using System;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;

namespace WinCalendar
{
    internal sealed class WeatherDetailsPanel : UserControl
    {
        private readonly Button back, settings, provider, yandex;
        private readonly Action<string> open;
        private readonly CalendarSymbolButton close;
        private WeatherView view = new WeatherView();
        private int dpi = 96;
        private bool? dark;
        private Font body;
        internal event Action BackRequested, SettingsRequested, CloseRequested;
        internal WeatherDetailsPanel(Action<string> navigate = null)
        {
            open = navigate ?? Program.OpenBrowser;
            DoubleBuffered = true; AutoScaleMode = AutoScaleMode.None; Size = new Size(688, 380);
            back = Button(UiText.Get("Назад"), 20, 328, 80);
            settings = Button(UiText.Get("Выбрать город"), 116, 328, 160);
            yandex = Button(UiText.Get("Яндекс Погода"), 292, 328, 196);
            provider = Button(UiText.Get("Данные: Open-Meteo"), 504, 328, 164);
            back.Click += delegate { if (BackRequested != null) BackRequested(); };
            settings.Click += delegate { if (SettingsRequested != null) SettingsRequested(); };
            provider.Click += delegate { open("https://open-meteo.com/"); };
            yandex.Click += delegate { string url = YandexLink(view.Location); if (url != null) open(url); };
            close = new CalendarSymbolButton { Bounds = new Rectangle(640, 16, 32, 32), IsClose = true, AccessibleName = UiText.Get("Закрыть календарь") };
            close.Click += delegate { if (CloseRequested != null) CloseRequested(); }; Controls.Add(close);
            SetView(view); SetDpi(96);
        }
        private Button Button(string text, int x, int y, int width)
        { Button button = new Button { Text = text, Bounds = new Rectangle(x, y, width, 34), FlatStyle = FlatStyle.Flat }; button.FlatAppearance.BorderSize = 0; Controls.Add(button); return button; }
        internal void SetView(WeatherView value)
        {
            view = value ?? new WeatherView();
            yandex.Enabled = view.Location != null && view.Location.Valid;
            AccessibleName = UiText.Get("Погода") + ", " + (view.Location == null ? view.Status : view.Location.ToString()) + ", " + view.Temperature
                + (view.Usable ? ", " + WeatherSymbols.Description(view.Snapshot.Code) + ", " + UiText.Get("Ощущается") + " " + WeatherView.Degrees(view.Snapshot.FeelsLike) + ", " + UiText.Get("Ветер") + " " + view.Snapshot.Wind.ToString("0.0", DateText.Culture) + " " + UiText.Get("м/с") + ", " + UiText.Get("Влажность") + " " + view.Snapshot.Humidity.ToString("0") + "%" : "")
                + (view.Stale ? ", " + UiText.Get("Устарело") : "")
                + (view.Usable ? "; " + String.Join("; ", (view.Snapshot.Days ?? new WeatherDay[0]).Select(day => day.Date.ToString("d MMM", DateText.Culture) + ": " + WeatherView.Degrees(day.High) + " / " + WeatherView.Degrees(day.Low) + ", " + WeatherSymbols.Description(day.Code))) : "");
            Invalidate();
        }
        internal void SetDpi(int value, bool? isDark = null)
        {
            Font previous = body;
            if (body == null || dpi != value) body = new Font("Segoe UI", 12 * value / 96f, FontStyle.Regular, GraphicsUnit.Pixel);
            dpi = value; dark = isDark;
            back.Bounds = Scaled(20, 328, 80, 34); settings.Bounds = Scaled(116, 328, 160, 34); yandex.Bounds = Scaled(292, 328, 196, 34); provider.Bounds = Scaled(504, 328, 164, 34); close.Bounds = Scaled(640, 16, 32, 32);
            CalendarPalette palette = CalendarPalette.Create(dark); BackColor = palette.Surface; close.Palette = palette;
            foreach (Button button in Controls.OfType<Button>())
            { button.BackColor = palette.Surface; button.ForeColor = palette.Accent; button.FlatAppearance.MouseOverBackColor = palette.Hover; button.Font = body; }
            if (previous != null && previous != body) previous.Dispose();
            Invalidate();
        }
        private Rectangle Scaled(int x, int y, int width, int height)
        { return new Rectangle(x * dpi / 96, y * dpi / 96, width * dpi / 96, height * dpi / 96); }
        internal static string YandexLink(WeatherLocation location)
        {
            return location == null || !location.Valid ? null : "https://yandex.ru/pogoda/?lat=" + location.Latitude.ToString("R", System.Globalization.CultureInfo.InvariantCulture)
                + "&lon=" + location.Longitude.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "&name=" + Uri.EscapeDataString(location.ToString());
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); Graphics g = e.Graphics; g.ScaleTransform(dpi / 96f, dpi / 96f); g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            CalendarPalette p = CalendarPalette.Create(dark);
            DrawText(g, UiText.Get("Погода"), 20, 16, 580, 30, 22, p.Ink, true);
            DrawText(g, view.Location == null ? UiText.Get("Выберите город в параметрах") : view.Location.ToString(), 20, 54, 630, 28, 14, p.Secondary);
            WeatherSymbols.Draw(g, new RectangleF(20, 94, 64, 64), view.Usable ? view.Snapshot.Code : 3, !view.Usable || view.Snapshot.IsDay, p);
            DrawText(g, view.Temperature + (view.Usable ? "C" : ""), 100, 89, 210, 64, 46, p.Ink, true);
            DrawText(g, view.Usable ? WeatherSymbols.Description(view.Snapshot.Code) : view.Status, 20, 163, 310, 25, 14, p.Ink);
            DrawText(g, view.Snapshot == null ? UiText.Get("Поиск города — без геолокации") : (view.Stale ? UiText.Get("Устарело") + " · " : UiText.Get("Обновлено") + " · ") + view.Snapshot.ObservedUtc.ToLocalTime().ToString(view.Stale ? "d MMM, HH:mm" : "HH:mm", DateText.Culture), 20, 190, 630, 24, 12, p.Secondary);
            if (view.Usable)
            {
                WeatherSnapshot s = view.Snapshot;
                Metric(g, UiText.Get("Ощущается"), WeatherView.Degrees(s.FeelsLike) + "C", 96, p);
                Metric(g, UiText.Get("Ветер"), s.Wind.ToString("0.0", DateText.Culture) + " " + UiText.Get("м/с"), 130, p);
                Metric(g, UiText.Get("Влажность"), s.Humidity.ToString("0") + "%", 164, p);
                WeatherDay[] days = s.Days ?? new WeatherDay[0];
                for (int i = 0; i < Math.Min(3, days.Length); i++)
                {
                    int x = 20 + i * 220; WeatherDay day = days[i];
                    DrawText(g, day.Date.ToString("ddd, d MMM", DateText.Culture), x, 235, 200, 22, 12, p.Secondary);
                    WeatherSymbols.Draw(g, new RectangleF(x, 264, 28, 28), day.Code, true, p);
                    DrawText(g, WeatherView.Degrees(day.High) + " / " + WeatherView.Degrees(day.Low), x + 40, 263, 160, 30, 18, p.Ink, true);
                }
            }
        }
        private static void Metric(Graphics g, string title, string value, int y, CalendarPalette p)
        { DrawText(g, title, 360, y, 150, 26, 13, p.Secondary); DrawText(g, value, 520, y, 140, 26, 16, p.Ink, true); }
        private static void DrawText(Graphics g, string text, int x, int y, int width, int height, float size, Color color, bool bold = false)
        {
            using (Font font = new Font(bold ? "Segoe UI Semibold" : "Segoe UI", size, FontStyle.Regular, GraphicsUnit.Pixel))
            using (Brush brush = new SolidBrush(color))
            using (StringFormat format = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
                g.DrawString(text, font, brush, new RectangleF(x, y, width, height), format);
        }
        protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing && body != null) { body.Dispose(); body = null; } }
    }
}
