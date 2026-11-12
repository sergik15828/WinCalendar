using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Microsoft.Win32;

namespace WinCalendar
{
    internal sealed class CalendarPanel : UserControl
    {
        private readonly Label heading;
        private readonly Label weekday;
        private readonly Label month;
        private readonly CalendarArrow previous;
        private readonly CalendarArrow next;
        private readonly Button todayButton;
        private readonly Button googleButton;
        private readonly Label[] weekLabels = new Label[7];
        private readonly CalendarDayButton[] dayButtons = new CalendarDayButton[42];
        private readonly Font headingFont = new Font("Segoe UI", 20, FontStyle.Bold);
        private readonly Font monthFont = new Font("Segoe UI", 12.5f, FontStyle.Bold);
        private readonly Font bodyFont = new Font("Segoe UI", 10);
        private readonly Font smallFont = new Font("Segoe UI", 9);
        private CalendarPalette palette;
        private readonly Dictionary<Control, Rectangle> logicalBounds = new Dictionary<Control, Rectangle>();
        private readonly Dictionary<Control, Font> logicalFonts = new Dictionary<Control, Font>();
        private readonly List<Font> scaledFonts = new List<Font>();
        private int layoutDpi;
        internal Size NaturalSize { get { return new Size(Pixels(344), Pixels(468)); } }
        internal CalendarState State { get; private set; }
        internal event EventHandler GoogleRequested;
        internal event EventHandler DismissRequested;
        internal Button TodayButton { get { return todayButton; } }
        internal Button GoogleButton { get { return googleButton; } }
        internal Button NextButton { get { return next; } }
        internal Button PreviousButton { get { return previous; } }

        internal CalendarPanel(DateTime today)
        {
            // Scale this hosted control explicitly; a dropdown has no Form to
            // coordinate automatic DPI scaling of its host, children and paint.
            AutoScaleMode = AutoScaleMode.None;
            Size = new Size(344, 468);
            AutoScroll = true;
            AutoScrollMinSize = Size;
            Font = bodyFont;
            DoubleBuffered = true;
            AccessibleName = "Календарь";
            State = new CalendarState(today);
            heading = LabelAt(new Rectangle(16, 14, 312, 40), headingFont);
            weekday = LabelAt(new Rectangle(16, 54, 312, 22), bodyFont);
            month = LabelAt(new Rectangle(16, 91, 230, 32), monthFont);
            previous = new CalendarArrow(false) { Bounds = new Rectangle(256, 91, 34, 32), AccessibleName = "Предыдущий месяц", TabIndex = 0 };
            next = new CalendarArrow(true) { Bounds = new Rectangle(294, 91, 34, 32), AccessibleName = "Следующий месяц", TabIndex = 1 };
            previous.Click += delegate { MoveMonth(-1, false); };
            next.Click += delegate { MoveMonth(1, false); };
            Controls.Add(previous); Controls.Add(next);
            string[] names = { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс" };
            for (int column = 0; column < 7; column++)
            {
                int left = 16 + column * 312 / 7;
                int width = (column + 1) * 312 / 7 - column * 312 / 7;
                weekLabels[column] = LabelAt(new Rectangle(left, 130, width, 26), smallFont);
                weekLabels[column].Text = names[column];
                weekLabels[column].TextAlign = ContentAlignment.MiddleCenter;
                for (int row = 0; row < 6; row++)
                {
                    int index = row * 7 + column;
                    CalendarDayButton button = new CalendarDayButton
                    {
                        Bounds = new Rectangle(left + 2, 156 + row * 40 + 2, width - 4, 36),
                        Font = bodyFont, TabIndex = 2, FlatStyle = FlatStyle.Flat
                    };
                    button.Click += delegate { State.Select((DateTime)button.Tag); RenderState(true); };
                    dayButtons[index] = button; Controls.Add(button);
                }
            }
            todayButton = FooterButton("Сегодня", new Rectangle(16, 420, 72, 32), 3);
            todayButton.AccessibleName = "Перейти к сегодняшней дате";
            todayButton.Click += delegate { State.GoToToday(); RenderState(false); };
            googleButton = FooterButton("Открыть Google Calendar", new Rectangle(96, 420, 232, 32), 4);
            googleButton.Click += delegate { if (GoogleRequested != null) GoogleRequested(this, EventArgs.Empty); };
            foreach (Control control in Controls) { logicalBounds.Add(control, control.Bounds); logicalFonts.Add(control, control.Font); }
            SetDpi(96);
            ApplyTheme(null);
        }

        private int Pixels(int value) { return (int)Math.Round(value * layoutDpi / 96.0); }

        internal void SetDpi(int dpi)
        {
            if (layoutDpi == dpi) return;
            layoutDpi = dpi;
            SuspendLayout();
            AutoScrollPosition = Point.Empty;
            Dictionary<Font, Font> replacements = new Dictionary<Font, Font>();
            foreach (Control control in Controls)
            {
                Rectangle bounds = logicalBounds[control];
                control.Bounds = new Rectangle(Pixels(bounds.X), Pixels(bounds.Y), Pixels(bounds.Width), Pixels(bounds.Height));
                Font original = logicalFonts[control], scaled;
                if (!replacements.TryGetValue(original, out scaled))
                {
                    scaled = new Font(original.FontFamily, original.SizeInPoints * dpi / 72f, original.Style, GraphicsUnit.Pixel);
                    replacements.Add(original, scaled);
                }
                control.Font = scaled;
            }
            foreach (Font font in scaledFonts) font.Dispose();
            scaledFonts.Clear(); scaledFonts.AddRange(replacements.Values);
            AutoScrollMinSize = NaturalSize;
            Size = NaturalSize;
            ResumeLayout(); Invalidate();
        }

        private Label LabelAt(Rectangle bounds, Font font)
        {
            Label label = new Label { Bounds = bounds, Font = font, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false };
            Controls.Add(label); return label;
        }

        private Button FooterButton(string text, Rectangle bounds, int tabIndex)
        {
            Button button = new Button { Text = text, Bounds = bounds, Font = bodyFont, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, TabIndex = tabIndex };
            button.FlatAppearance.BorderSize = 0;
            Controls.Add(button); return button;
        }

        internal void Reset(DateTime today)
        {
            State.UpdateToday(today); State.GoToToday(); ApplyTheme(null);
        }

        internal void UpdateToday(DateTime today) { State.UpdateToday(today); RenderState(false); }

        internal void ApplyTheme(bool? dark)
        {
            palette = CalendarPalette.Create(dark);
            BackColor = palette.Surface;
            foreach (Control control in Controls) control.BackColor = palette.Surface;
            heading.ForeColor = month.ForeColor = palette.Ink;
            weekday.ForeColor = palette.Secondary;
            previous.Palette = next.Palette = palette;
            todayButton.ForeColor = palette.Accent;
            todayButton.FlatAppearance.MouseOverBackColor = palette.Hover;
            googleButton.BackColor = palette.Accent; googleButton.ForeColor = palette.OnAccent;
            googleButton.FlatAppearance.MouseOverBackColor = palette.Accent;
            googleButton.FlatAppearance.MouseDownBackColor = palette.Accent;
            for (int i = 0; i < 7; i++) weekLabels[i].ForeColor = i >= 5 ? palette.Weekend : palette.Secondary;
            RenderState(false);
        }

        private void MoveMonth(int amount, bool focus) { State.MoveMonth(amount); RenderState(focus); }

        private void RenderState(bool focus)
        {
            heading.Text = State.Today.ToString("d MMMM", DateText.Russian);
            weekday.Text = DateText.Russian.TextInfo.ToTitleCase(State.Today.ToString("dddd", DateText.Russian));
            month.Text = DateText.Russian.TextInfo.ToTitleCase(State.Month.ToString("MMMM yyyy", DateText.Russian));
            previous.Enabled = State.Month > CalendarState.Minimum;
            next.Enabled = State.Month < new DateTime(9998, 12, 1);
            DateTime[] days = State.Days();
            for (int i = 0; i < days.Length; i++)
            {
                CalendarDayButton button = dayButtons[i];
                DateTime date = days[i];
                button.Tag = date;
                button.Text = date.Day.ToString(DateText.Russian);
                button.Today = date == State.Today;
                button.Selected = date == State.Selected;
                button.InMonth = date.Month == State.Month.Month && date.Year == State.Month.Year;
                button.Weekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
                button.Palette = palette;
                button.TabStop = button.Selected;
                button.Enabled = date >= CalendarState.Minimum && date <= CalendarState.Maximum;
                button.AccessibleName = date.ToString("dddd, d MMMM yyyy", DateText.Russian)
                    + (button.Today ? ", сегодня" : "") + (button.Selected ? ", выбрано" : "");
                button.Invalidate();
                if (focus && button.Selected) button.Focus();
            }
            previous.Invalidate(); next.Invalidate(); Invalidate();
        }

        internal void FocusSelectedDay()
        {
            foreach (CalendarDayButton button in dayButtons) if (button.Selected) { button.Focus(); return; }
        }

        protected override bool ProcessCmdKey(ref Message message, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                if (DismissRequested != null) DismissRequested(this, EventArgs.Empty);
                return true;
            }
            if (keyData == Keys.PageUp || keyData == Keys.PageDown) { MoveMonth(keyData == Keys.PageUp ? -1 : 1, true); return true; }
            if (keyData == Keys.Home) { State.GoToToday(); RenderState(true); return true; }
            bool dayFocused = false;
            foreach (CalendarDayButton button in dayButtons) if (button.Focused) dayFocused = true;
            int delta = keyData == Keys.Left ? -1 : keyData == Keys.Right ? 1 : keyData == Keys.Up ? -7 : keyData == Keys.Down ? 7 : 0;
            if (dayFocused && delta != 0) { State.Select(State.Selected.AddDays(delta)); RenderState(true); return true; }
            return base.ProcessCmdKey(ref message, keyData);
        }

        protected override void OnPaint(PaintEventArgs args)
        {
            base.OnPaint(args);
            int inset = Pixels(16), top = Pixels(408) + AutoScrollPosition.Y;
            using (Pen rule = new Pen(palette.Rule)) args.Graphics.DrawLine(rule, inset, top, Width - inset, top);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) { foreach (Font font in scaledFonts) font.Dispose(); headingFont.Dispose(); monthFont.Dispose(); bodyFont.Dispose(); smallFont.Dispose(); }
        }
    }

    internal sealed class CalendarPalette
    {
        internal Color Surface, Ink, Secondary, Adjacent, Accent, OnAccent, Hover, Weekend, Rule;
        internal static CalendarPalette Create(bool? dark)
        {
            if (SystemInformation.HighContrast) return new CalendarPalette { Surface = SystemColors.Window, Ink = SystemColors.WindowText,
                Secondary = SystemColors.WindowText, Adjacent = SystemColors.WindowText, Accent = SystemColors.Highlight,
                OnAccent = SystemColors.HighlightText, Hover = SystemColors.Control, Weekend = SystemColors.WindowText, Rule = SystemColors.WindowText };
            bool isDark = false;
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                if (key != null) isDark = Object.Equals(key.GetValue("AppsUseLightTheme"), 0);
            isDark = dark ?? isDark;
            return new CalendarPalette { Surface = Hex(isDark ? "202020" : "FAFAFA"), Ink = Hex(isDark ? "F5F5F5" : "202020"),
                Secondary = Hex(isDark ? "BDBDBD" : "626262"), Adjacent = Hex(isDark ? "A0A0A0" : "727272"),
                Accent = Hex(isDark ? "FF8B91" : "C42B36"), OnAccent = Hex(isDark ? "202020" : "FFFFFF"),
                Hover = Hex(isDark ? "383838" : "E8E8E8"), Weekend = Hex(isDark ? "FFB6BB" : "A82330"), Rule = Hex(isDark ? "444444" : "DEDEDE") };
        }
        private static Color Hex(string text) { return ColorTranslator.FromHtml("#" + text); }
    }

    internal sealed class CalendarDayButton : Button
    {
        internal CalendarPalette Palette;
        internal bool Today, Selected, InMonth, Weekend;
        private bool hover;
        internal CalendarDayButton() { SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true); Cursor = Cursors.Hand; }
        protected override void OnMouseEnter(EventArgs args) { hover = true; Invalidate(); base.OnMouseEnter(args); }
        protected override void OnMouseLeave(EventArgs args) { hover = false; Invalidate(); base.OnMouseLeave(args); }
        protected override void OnGotFocus(EventArgs args) { Invalidate(); base.OnGotFocus(args); }
        protected override void OnLostFocus(EventArgs args) { Invalidate(); base.OnLostFocus(args); }
        protected override void OnPaint(PaintEventArgs args)
        {
            if (Palette == null) return;
            args.Graphics.Clear(Palette.Surface);
            args.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float scale = Height / 36f;
            int diameter = Math.Min(Width, Height) - (int)Math.Round(4 * scale);
            Rectangle circle = new Rectangle((Width - diameter) / 2, (Height - diameter) / 2, diameter, diameter);
            if (Today || hover)
                using (Brush fill = new SolidBrush(Today ? Palette.Accent : Palette.Hover)) args.Graphics.FillEllipse(fill, circle);
            if (Selected && !Today)
                using (Pen outline = new Pen(Palette.Accent, 1.5f * scale)) args.Graphics.DrawEllipse(outline, circle);
            Color ink = Today ? Palette.OnAccent : !InMonth ? Palette.Adjacent : Weekend ? Palette.Weekend : Palette.Ink;
            TextRenderer.DrawText(args.Graphics, Text, Font, ClientRectangle, ink, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(args.Graphics, Rectangle.Inflate(circle, -4, -4), ink, Today ? Palette.Accent : Palette.Surface);
        }
    }

    internal sealed class CalendarArrow : Button
    {
        private readonly bool right;
        private bool hover;
        internal CalendarPalette Palette;
        internal CalendarArrow(bool forward) { right = forward; FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0; Cursor = Cursors.Hand; }
        protected override void OnMouseEnter(EventArgs args) { hover = true; Invalidate(); base.OnMouseEnter(args); }
        protected override void OnMouseLeave(EventArgs args) { hover = false; Invalidate(); base.OnMouseLeave(args); }
        protected override void OnPaint(PaintEventArgs args)
        {
            if (Palette == null) return;
            args.Graphics.Clear(hover ? Palette.Hover : Palette.Surface);
            args.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float x = Width / 2f, y = Height / 2f, direction = right ? 1 : -1, scale = Height / 32f;
            using (Pen pen = new Pen(Enabled ? Palette.Ink : Palette.Secondary, 1.5f * scale))
                args.Graphics.DrawLines(pen, new[] { new PointF(x - 2 * direction * scale, y - 5 * scale), new PointF(x + 3 * direction * scale, y), new PointF(x - 2 * direction * scale, y + 5 * scale) });
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(args.Graphics, Rectangle.Inflate(ClientRectangle, -4, -4), Palette.Ink, Palette.Surface);
        }
    }
}
