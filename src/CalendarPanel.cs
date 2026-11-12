using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
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
        private readonly Label selectedDate, emptyEvents, syncStatus, updatedStatus;
        private readonly Label agendaHeading;
        private readonly CalendarEventList eventsList;
        private readonly Button connectButton, refreshButton, disconnectButton;
        private readonly CalendarSymbolButton settingsButton, closeButton;
        private readonly Panel settingsPage;
        private readonly CheckBox startupCheck;
        private readonly Button configureButton, previewButton, exitButton;
        private readonly Label reminderInfo;
        private readonly Button optionsButton, weekButton, snooze5, snooze10;
        private readonly Label nextMeeting;
        private AppPreferences preferences = new AppPreferences();
        private bool weekly;
        private List<CalendarEvent> upcomingEvents;
        private readonly List<Control> calendarControls;
        private bool settingsVisible;
        private CalendarViewData viewData = new CalendarViewData { Message = "Подключите Google, чтобы видеть встречи." };
        private DateTime renderedMonth;
        private readonly Label[] weekLabels = new Label[7];
        private readonly CalendarDayButton[] dayButtons = new CalendarDayButton[42];
        private readonly Font headingFont = new Font("Segoe UI", 17, FontStyle.Bold);
        private readonly Font monthFont = new Font("Segoe UI", 11.5f, FontStyle.Bold);
        private readonly Font bodyFont = new Font("Segoe UI", 10);
        private readonly Font smallFont = new Font("Segoe UI", 9);
        private CalendarPalette palette;
        private readonly Dictionary<Control, Rectangle> logicalBounds = new Dictionary<Control, Rectangle>();
        private readonly Dictionary<Control, Font> logicalFonts = new Dictionary<Control, Font>();
        private readonly List<Font> scaledFonts = new List<Font>();
        private int layoutDpi;
        internal const int LogicalWidth = 688, LogicalHeight = 380;
        internal Size NaturalSize { get { return new Size(Pixels(LogicalWidth), Pixels(LogicalHeight)); } }
        internal CalendarState State { get; private set; }
        internal event EventHandler GoogleRequested;
        internal event EventHandler DismissRequested;
        internal event EventHandler RangeChanged, ConnectRequested, RefreshRequested, DisconnectRequested;
        internal event EventHandler ConfigureRequested, PreviewRequested, ExitRequested;
        internal event EventHandler SettingsOpened;
        internal event Action<bool> StartupRequested;
        internal event Action<string> EventRequested;
        internal event Action<string> MeetRequested;
        internal event EventHandler OptionsRequested;
        internal event Action<int> SnoozeRequested;
        internal bool Weekly { get { return weekly; } }
        internal Button WeekButton { get { return weekButton; } }
        internal Button TodayButton { get { return todayButton; } }
        internal Button GoogleButton { get { return googleButton; } }
        internal Button NextButton { get { return next; } }
        internal Button PreviousButton { get { return previous; } }
        internal Button ConnectButton { get { return connectButton; } }
        internal Button RefreshButton { get { return refreshButton; } }
        internal Button DisconnectButton { get { return disconnectButton; } }
        internal CalendarEventList EventList { get { return eventsList; } }
        internal string StatusText { get { return syncStatus.Text; } }
        internal Button CloseButton { get { return closeButton; } }
        internal Button SettingsButton { get { return settingsButton; } }
        internal bool SettingsVisible { get { return settingsVisible; } }

        internal CalendarPanel(DateTime today)
        {
            // Scale this hosted control explicitly; a dropdown has no Form to
            // coordinate automatic DPI scaling of its host, children and paint.
            AutoScaleMode = AutoScaleMode.None;
            Size = new Size(LogicalWidth, LogicalHeight);
            AutoScroll = true;
            AutoScrollMinSize = Size;
            Font = bodyFont;
            DoubleBuffered = true;
            AccessibleName = "Календарь";
            State = new CalendarState(today);
            heading = LabelAt(new Rectangle(16, 10, 272, 32), headingFont);
            weekday = LabelAt(new Rectangle(16, 42, 272, 20), smallFont);
            settingsButton = new CalendarSymbolButton { Bounds = new Rectangle(600, 16, 32, 32), AccessibleName = "Настройки", TabIndex = 0 };
            settingsButton.Click += delegate { ShowSettings(!settingsVisible); };
            Controls.Add(settingsButton);
            closeButton = new CalendarSymbolButton { Bounds = new Rectangle(640, 16, 32, 32), IsClose = true, AccessibleName = "Закрыть календарь", TabIndex = 8 };
            closeButton.Click += delegate { if (DismissRequested != null) DismissRequested(this, EventArgs.Empty); };
            Controls.Add(closeButton);
            month = LabelAt(new Rectangle(16, 68, 166, 28), monthFont);
            previous = new CalendarArrow(false) { Bounds = new Rectangle(260, 68, 32, 28), AccessibleName = "Предыдущий месяц", TabIndex = 2 };
            next = new CalendarArrow(true) { Bounds = new Rectangle(296, 68, 32, 28), AccessibleName = "Следующий месяц", TabIndex = 3 };
            previous.Click += delegate { MoveMonth(-1, false); };
            next.Click += delegate { MoveMonth(1, false); };
            Controls.Add(previous); Controls.Add(next);
            string[] names = { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс" };
            for (int column = 0; column < 7; column++)
            {
                int left = 16 + column * 312 / 7;
                int width = (column + 1) * 312 / 7 - column * 312 / 7;
                weekLabels[column] = LabelAt(new Rectangle(left, 102, width, 22), smallFont);
                weekLabels[column].Text = names[column];
                weekLabels[column].TextAlign = ContentAlignment.MiddleCenter;
                for (int row = 0; row < 6; row++)
                {
                    int index = row * 7 + column;
                    CalendarDayButton button = new CalendarDayButton
                    {
                        Bounds = new Rectangle(left + 2, 126 + row * 32, width - 4, 30),
                        Font = bodyFont, TabIndex = 4, FlatStyle = FlatStyle.Flat
                    };
                    button.Click += delegate { State.Select((DateTime)button.Tag); RenderState(true); };
                    dayButtons[index] = button; Controls.Add(button);
                }
            }
            agendaHeading = LabelAt(new Rectangle(360, 16, 226, 28), monthFont);
            agendaHeading.Text = "Встречи";
            selectedDate = LabelAt(new Rectangle(360, 48, 216, 24), smallFont);
            weekButton = FooterButton("Неделя", new Rectangle(580, 46, 92, 28), 6); weekButton.Font = smallFont;
            weekButton.Click += delegate { weekly = !weekly; weekButton.Text = weekly ? "День" : "Неделя"; RenderEvents(); if (RangeChanged != null) RangeChanged(this, EventArgs.Empty); };
            eventsList = new CalendarEventList { Bounds = new Rectangle(360, 86, 312, 278), Font = bodyFont, TabIndex = 5 };
            eventsList.OpenRequested += delegate(string url) { if (EventRequested != null) EventRequested(url); };
            eventsList.MeetRequested += delegate(string url) { if (MeetRequested != null) MeetRequested(url); };
            Controls.Add(eventsList);
            emptyEvents = LabelAt(eventsList.Bounds, bodyFont);
            todayButton = FooterButton("Сегодня", new Rectangle(184, 68, 72, 28), 1);
            todayButton.Font = smallFont;
            todayButton.AccessibleName = "Перейти к сегодняшней дате";
            todayButton.Click += delegate { State.GoToToday(); RenderState(false); };
            nextMeeting = LabelAt(new Rectangle(16, 322, 312, 22), smallFont); nextMeeting.AutoEllipsis = true; nextMeeting.Text = "Ближайшая встреча появится после загрузки";
            snooze5 = FooterButton("Отложить на 5 мин", new Rectangle(16, 348, 144, 26), 7); snooze5.Font = smallFont;
            snooze10 = FooterButton("Отложить на 10 мин", new Rectangle(184, 348, 144, 26), 8); snooze10.Font = smallFont;
            snooze5.Click += delegate { if (SnoozeRequested != null) SnoozeRequested(5); };
            snooze10.Click += delegate { if (SnoozeRequested != null) SnoozeRequested(10); };
            snooze5.Visible = snooze10.Visible = false;
            calendarControls = Controls.Cast<Control>().Where(c => c != heading && c != weekday && c != settingsButton && c != closeButton).ToList();
            settingsPage = new Panel { Bounds = new Rectangle(16, 70, 656, 294), Visible = false };
            Controls.Add(settingsPage);
            syncStatus = SettingsLabel(new Rectangle(0, 0, 312, 38), bodyFont);
            syncStatus.AutoEllipsis = true;
            updatedStatus = SettingsLabel(new Rectangle(0, 40, 312, 20), smallFont);
            connectButton = SettingsAction("Войти в Google", 76, 0);
            refreshButton = SettingsAction("Обновить встречи", 114, 1);
            googleButton = SettingsAction("Открыть Google Calendar", 152, 2);
            previewButton = SettingsAction("Проверить напоминание", 190, 3);
            configureButton = SettingsAction("Настроить подключение", 0, 4);
            configureButton.Left = 344;
            disconnectButton = SettingsAction("Отключить Google", 38, 5);
            disconnectButton.Left = 344;
            connectButton.Click += delegate { if (ConnectRequested != null) ConnectRequested(this, EventArgs.Empty); };
            refreshButton.Click += delegate { if (RefreshRequested != null) RefreshRequested(this, EventArgs.Empty); };
            disconnectButton.Click += delegate { if (DisconnectRequested != null) DisconnectRequested(this, EventArgs.Empty); };
            googleButton.Click += delegate { if (GoogleRequested != null) GoogleRequested(this, EventArgs.Empty); };
            configureButton.Click += delegate { if (ConfigureRequested != null) ConfigureRequested(this, EventArgs.Empty); };
            previewButton.Click += delegate { if (PreviewRequested != null) PreviewRequested(this, EventArgs.Empty); };
            optionsButton = SettingsAction("Календари, звук и вид", 78, 6); optionsButton.Left = 344;
            optionsButton.Click += delegate { if (OptionsRequested != null) OptionsRequested(this, EventArgs.Empty); };
            reminderInfo = SettingsLabel(new Rectangle(344, 120, 312, 42), smallFont);
            reminderInfo.Text = "Напоминания — как в событии Google.\nПульсация остаётся до нажатия на дату.";
            startupCheck = new CheckBox { Bounds = new Rectangle(344, 168, 312, 26), Font = smallFont, Text = "Запускать с Windows", TabIndex = 6 };
            startupCheck.Click += delegate { if (StartupRequested != null) StartupRequested(startupCheck.Checked); };
            settingsPage.Controls.Add(startupCheck);
            exitButton = SettingsAction("Выйти", 218, 7);
            exitButton.Left = 344;
            exitButton.Height = 32;
            exitButton.Click += delegate { if (ExitRequested != null) ExitRequested(this, EventArgs.Empty); };
            foreach (Control control in Descendants(this)) { logicalBounds.Add(control, control.Bounds); logicalFonts.Add(control, control.Font); }
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
            foreach (Control control in Descendants(this))
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
            ResumeLayout(); if (palette != null) RenderEvents(); Invalidate();
        }

        private Label LabelAt(Rectangle bounds, Font font)
        {
            Label label = new Label { Bounds = bounds, Font = font, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false };
            Controls.Add(label); return label;
        }

        private Label SettingsLabel(Rectangle bounds, Font font)
        {
            Label label = new Label { Bounds = bounds, Font = font, TextAlign = ContentAlignment.MiddleLeft };
            settingsPage.Controls.Add(label); return label;
        }

        private Button SettingsAction(string text, int top, int index)
        {
            Button button = new Button { Bounds = new Rectangle(0, top, 312, 34), Text = text, Font = bodyFont,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, TabIndex = index };
            button.FlatAppearance.BorderSize = 0;
            settingsPage.Controls.Add(button); return button;
        }

        internal static IEnumerable<Control> Descendants(Control parent)
        {
            foreach (Control child in parent.Controls)
            {
                yield return child;
                foreach (Control nested in Descendants(child)) yield return nested;
            }
        }

        internal void ShowSettings(bool show)
        {
            settingsVisible = show;
            foreach (Control control in calendarControls) control.Visible = !show;
            settingsPage.Visible = show;
            settingsButton.IsClose = false;
            settingsButton.AccessibleName = show ? "Вернуться к календарю" : "Настройки";
            RenderState(false);
            if (show && SettingsOpened != null) SettingsOpened(this, EventArgs.Empty);
        }

        internal void SetStartup(bool enabled) { startupCheck.Checked = enabled; }
        internal void SetPreferences(AppPreferences value)
        { preferences = value; eventsList.Preferences = value; RenderState(false); if (upcomingEvents != null) RenderUpcoming(); }
        private bool canSnooze;
        internal void SetUpcoming(IEnumerable<CalendarEvent> events, bool available)
        {
            upcomingEvents = events.ToList(); canSnooze = available; RenderUpcoming();
        }
        private void RenderUpcoming()
        {
            CalendarEvent nextEvent = MeetingTools.Next(upcomingEvents.Where(preferences.Includes), DateTimeOffset.Now);
            string text = MeetingTools.NextLabel(nextEvent, DateTimeOffset.Now, preferences.PrivateMode);
            if (preferences.Calendars.Count > 0 && preferences.Calendars.All(c => preferences.DisabledCalendars.Contains(c.Id))) text = "Календари скрыты";
            if (nextMeeting.Text != text) { nextMeeting.Text = text; nextMeeting.AccessibleName = text; }
            snooze5.Visible = snooze10.Visible = !settingsVisible && canSnooze;
        }

        private Button FooterButton(string text, Rectangle bounds, int tabIndex)
        {
            Button button = new Button { Text = text, Bounds = bounds, Font = bodyFont, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, TabIndex = tabIndex };
            button.FlatAppearance.BorderSize = 0;
            Controls.Add(button); return button;
        }

        internal void Reset(DateTime today)
        {
            State.UpdateToday(today); State.GoToToday(); ShowSettings(false); ApplyTheme(null);
        }

        internal void UpdateToday(DateTime today) { State.UpdateToday(today); RenderState(false); }

        internal void ApplyTheme(bool? dark)
        {
            palette = CalendarPalette.Create(dark);
            BackColor = palette.Surface;
            foreach (Control control in Descendants(this)) control.BackColor = palette.Surface;
            heading.ForeColor = month.ForeColor = agendaHeading.ForeColor = palette.Ink;
            weekday.ForeColor = palette.Secondary;
            selectedDate.ForeColor = palette.Secondary;
            nextMeeting.ForeColor = palette.Secondary;
            weekButton.ForeColor = snooze5.ForeColor = snooze10.ForeColor = palette.Accent;
            emptyEvents.ForeColor = syncStatus.ForeColor = updatedStatus.ForeColor = reminderInfo.ForeColor = palette.Secondary;
            startupCheck.ForeColor = palette.Ink;
            settingsButton.Palette = closeButton.Palette = palette;
            foreach (Button action in new[] { connectButton, refreshButton, disconnectButton, googleButton, configureButton, previewButton, exitButton, optionsButton })
            { action.ForeColor = palette.Ink; action.FlatAppearance.MouseOverBackColor = palette.Hover; }
            disconnectButton.ForeColor = palette.Accent;
            previous.Palette = next.Palette = palette;
            todayButton.ForeColor = palette.Accent;
            todayButton.FlatAppearance.MouseOverBackColor = palette.Hover;
            for (int i = 0; i < 7; i++) weekLabels[i].ForeColor = i >= 5 ? palette.Weekend : palette.Secondary;
            RenderState(false);
        }

        private void MoveMonth(int amount, bool focus) { State.MoveMonth(amount); RenderState(focus); }

        private void RenderState(bool focus)
        {
            heading.Text = settingsVisible ? "Настройки" : State.Today.ToString("d MMMM", DateText.Russian);
            weekday.Text = settingsVisible ? "WinCalendar" : DateText.Russian.TextInfo.ToTitleCase(State.Today.ToString("dddd", DateText.Russian));
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
                button.HasEvents = viewData.Events.Any(e => preferences.Includes(e) && e.OccursOn(date, TimeZoneInfo.Local));
                button.TabStop = button.Selected;
                button.Enabled = date >= CalendarState.Minimum && date <= CalendarState.Maximum;
                button.AccessibleName = date.ToString("dddd, d MMMM yyyy", DateText.Russian)
                    + (button.Today ? ", сегодня" : "") + (button.Selected ? ", выбрано" : "") + (button.HasEvents ? ", есть встречи" : "");
                button.Invalidate();
                if (focus && button.Selected) button.Focus();
            }
            RenderEvents();
            previous.Invalidate(); next.Invalidate(); Invalidate();
            if (renderedMonth != State.Month)
            {
                renderedMonth = State.Month;
                if (RangeChanged != null) RangeChanged(this, EventArgs.Empty);
            }
        }

        internal void SetData(CalendarViewData data) { viewData = data; RenderState(false); }

        private void RenderEvents()
        {
            CalendarEvent[] day = viewData.Events.Where(e => preferences.Includes(e) && (weekly ? Enumerable.Range(0, 7).Any(i => e.OccursOn(State.Selected.AddDays(i), TimeZoneInfo.Local)) : e.OccursOn(State.Selected, TimeZoneInfo.Local))).OrderBy(e => e.SortOrder).ToArray();
            selectedDate.Text = (weekly ? State.Selected.ToString("d MMM", DateText.Russian) + " — " + State.Selected.AddDays(6).ToString("d MMM", DateText.Russian) : State.Selected.ToString("d MMMM yyyy", DateText.Russian)) + (day.Length == 0 ? "" : " · " + day.Length);
            eventsList.Weekly = weekly;
            eventsList.SetEvents(day, State.Selected, TimeZoneInfo.Local, palette, layoutDpi);
            eventsList.Visible = !settingsVisible && day.Length > 0; emptyEvents.Visible = !settingsVisible && day.Length == 0;
            emptyEvents.Text = viewData.Connection == CalendarConnection.Disconnected ? "Подключите календари Google,\nчтобы видеть встречи выбранного дня."
                : viewData.Connection == CalendarConnection.NeedsLogin ? "Войдите в Google, чтобы проверить встречи\nво всех доступных календарях."
                : viewData.Loading ? "Проверяем встречи…"
                : viewData.LoadError ? "Не удалось проверить встречи.\n" + (viewData.HasSnapshot ? "Показаны последние сохранённые данные." : "Список встреч пока недоступен.")
                : viewData.HasSnapshot ? "На этот день встреч нет." : "Список встреч пока недоступен.";
            if (day.Length == 0 && weekly && viewData.HasSnapshot && !viewData.LoadError && !viewData.Loading && viewData.Connection == CalendarConnection.Connected) emptyEvents.Text = "На эту неделю встреч нет.";
            if (day.Length == 0 && !viewData.LoadError && !viewData.Loading && viewData.Connection == CalendarConnection.Connected && preferences.Calendars.Count > 0
                && preferences.Calendars.All(c => preferences.DisabledCalendars.Contains(c.Id))) emptyEvents.Text = "Календари скрыты.\nВыберите их в настройках.";
            snooze5.Visible = snooze10.Visible = !settingsVisible && canSnooze;
            syncStatus.Text = viewData.Message;
            updatedStatus.Text = viewData.UpdatedUtc.HasValue ? "Данные: " + viewData.UpdatedUtc.Value.ToLocalTime().ToString("d MMM HH:mm", DateText.Russian) : "";
            bool connected = viewData.Connection == CalendarConnection.Connected, busy = viewData.Connection == CalendarConnection.Busy;
            settingsButton.HasAttention = viewData.LoadError || viewData.Connection == CalendarConnection.NeedsLogin;
            settingsButton.Invalidate();
            connectButton.Text = busy ? "Отмена входа" : connected ? "Сменить аккаунт" : "Войти в Google";
            connectButton.ForeColor = connected ? palette.Ink : palette.Accent;
            refreshButton.Enabled = connected && !viewData.Loading;
            disconnectButton.Enabled = connected || viewData.Connection == CalendarConnection.NeedsLogin;
        }

        internal void FocusSelectedDay()
        {
            foreach (CalendarDayButton button in dayButtons) if (button.Selected) { button.Focus(); return; }
        }

        protected override bool ProcessCmdKey(ref Message message, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                if (settingsVisible) { ShowSettings(false); FocusSelectedDay(); return true; }
                if (DismissRequested != null) DismissRequested(this, EventArgs.Empty);
                return true;
            }
            if (settingsVisible) return base.ProcessCmdKey(ref message, keyData);
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
            int inset = Pixels(16);
            using (Pen rule = new Pen(palette.Rule))
                args.Graphics.DrawLine(rule, Pixels(344) + AutoScrollPosition.X, inset, Pixels(344) + AutoScrollPosition.X, Height - inset);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) { foreach (Font font in scaledFonts) font.Dispose(); headingFont.Dispose(); monthFont.Dispose(); bodyFont.Dispose(); smallFont.Dispose(); }
        }
    }

    internal sealed class CalendarPalette
    {
        internal Color Surface, EventSurface, Ink, Secondary, Adjacent, Accent, OnAccent, Hover, Weekend, Rule;
        internal static CalendarPalette Create(bool? dark)
        {
            if (SystemInformation.HighContrast) return new CalendarPalette { Surface = SystemColors.Window, EventSurface = SystemColors.Window, Ink = SystemColors.WindowText,
                Secondary = SystemColors.WindowText, Adjacent = SystemColors.WindowText, Accent = SystemColors.Highlight,
                OnAccent = SystemColors.HighlightText, Hover = SystemColors.Control, Weekend = SystemColors.WindowText, Rule = SystemColors.WindowText };
            bool isDark = false;
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                if (key != null) isDark = Object.Equals(key.GetValue("AppsUseLightTheme"), 0);
            isDark = dark ?? isDark;
            return new CalendarPalette { Surface = Hex(isDark ? "202020" : "FAFAFA"), EventSurface = Hex(isDark ? "292929" : "FFFFFF"), Ink = Hex(isDark ? "F5F5F5" : "202020"),
                Secondary = Hex(isDark ? "BDBDBD" : "626262"), Adjacent = Hex(isDark ? "A0A0A0" : "727272"),
                Accent = Hex(isDark ? "FF8B91" : "C42B36"), OnAccent = Hex(isDark ? "202020" : "FFFFFF"),
                Hover = Hex(isDark ? "383838" : "E8E8E8"), Weekend = Hex(isDark ? "FFB6BB" : "A82330"), Rule = Hex(isDark ? "444444" : "DEDEDE") };
        }
        private static Color Hex(string text) { return ColorTranslator.FromHtml("#" + text); }
    }

    internal sealed class CalendarDayButton : Button
    {
        internal CalendarPalette Palette;
        internal bool Today, Selected, InMonth, Weekend, HasEvents;
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
            if (HasEvents)
            {
                float dot = 3 * scale;
                using (Brush brush = new SolidBrush(Today ? Palette.OnAccent : Palette.Accent))
                    args.Graphics.FillEllipse(brush, (Width - dot) / 2, Height - 8 * scale, dot, dot);
            }
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
