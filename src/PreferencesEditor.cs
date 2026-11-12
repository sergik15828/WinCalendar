using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace WinCalendar
{
    internal sealed class PreferencesEditor : UserControl, IMessageFilter
    {
        private readonly Panel viewport, content;
        private readonly List<Panel> pages = new List<Panel>();
        private readonly List<Button> tabs = new List<Button>();
        private int selectedPage;
        private bool? theme;
        private readonly CheckedListBox calendars;
        private readonly ComboBox color, sound, strength, language, frequency;
        private readonly CheckBox audio, quiet, privacy, nextTip, weatherWidget, startup, paydayEnabled;
        private readonly RadioButton eventTiming, fiveMinutes, twoMinutes;
        private readonly NumericUpDown firstDay, secondDay;
        private readonly TrackBar volume;
        private readonly DateTimePicker from, to;
        private readonly Label title, error, volumeLabel, paydayPreview;
        private readonly Button back, save, connection;
        private readonly CalendarSymbolButton close;
        private readonly Dictionary<Control, Rectangle> bounds = new Dictionary<Control, Rectangle>();
        private readonly string[] colorCodes = { null, "#C42B36", "#247A53", "#2563A6", "#7950B3", "#A35F18", "#B03B7F" };
        private Font body, heading, sectionFont;
        private int dpi, wheel;
        private bool loading;
        private AppPreferences draft;
        internal Func<Point> PointerPosition = delegate { return Cursor.Position; };
        internal event EventHandler BackRequested, CloseRequested, ConnectionRequested, ReminderPreviewRequested, CatalogRefreshRequested;
        internal event Action<AppPreferences> SaveRequested;
        internal AppPreferences Draft { get { return draft; } }
        internal Size NaturalSize { get { return new Size(Scale(720), Scale(460)); } }
        internal int SelectedPage { get { return selectedPage; } }
        internal Button ConnectionButton { get { return connection; } }
        internal Button BackButton { get { return back; } }
        internal bool LayoutFits { get { return bounds.Keys.All(c => c == content || c.Parent.ClientRectangle.Contains(c.Bounds)); } }
        internal string LayoutIssues { get { return String.Join("; ", bounds.Keys.Where(c => c != content && !c.Parent.ClientRectangle.Contains(c.Bounds)).Select(c => c.GetType().Name + " " + c.Text + " " + c.Bounds + " in " + c.Parent.ClientRectangle)); } }
        internal int ScrollOffset { get { return -viewport.AutoScrollPosition.Y; } }
        internal Bitmap CaptureFrame()
        {
            // DrawToBitmap does not preserve nested scrolling HWND clips.
            // Capture each root child in its own bounded surface instead.
            Bitmap frame = new Bitmap(Width, Height);
            using (Graphics g = Graphics.FromImage(frame))
            {
                g.Clear(BackColor);
                foreach (Control child in Controls)
                    if (child.Visible && child.Width > 0 && child.Height > 0)
                        using (Bitmap part = new Bitmap(child.Width, child.Height))
                        { child.DrawToBitmap(part, new Rectangle(Point.Empty, child.Size)); g.DrawImageUnscaled(part, child.Left, child.Top); }
            }
            return frame;
        }

        internal PreferencesEditor()
        {
            AutoScaleMode = AutoScaleMode.None; Size = new Size(720, 460);
            title = Label(this, UiText.Get("Параметры"), new Rectangle(20, 14, 660, 34)); title.Tag = "heading";
            Label version = Label(this, "WinCalendar · " + typeof(PreferencesEditor).Assembly.GetName().Version.ToString(3), new Rectangle(20, 44, 500, 16)); version.Tag = "muted";
            close = new CalendarSymbolButton { Bounds = new Rectangle(668, 16, 32, 32), IsClose = true, AccessibleName = UiText.Get("Закрыть календарь") };
            close.Click += delegate { if (CloseRequested != null) CloseRequested(this, EventArgs.Empty); }; Controls.Add(close);
            viewport = new BufferedPanel { Bounds = new Rectangle(20, 112, 680, 276), AutoScroll = true, TabStop = false };
            content = new BufferedPanel { Bounds = new Rectangle(0, 0, 680, 276) };
            viewport.Controls.Add(content); Controls.Add(viewport);
            Panel general = Section(UiText.Get("Основное"), new Rectangle(0, 0, 336, 264));
            Label(general, UiText.Get("Язык интерфейса"), new Rectangle(0, 32, 312, 22));
            language = Combo(general, new Rectangle(0, 58, 312, 30), new[] { "Русский", "English" }); language.AccessibleName = "Language / Язык";
            weatherWidget = Check(general, UiText.Get("Виджет рядом с погодой"), new Rectangle(0, 100, 324, 28));
            startup = Check(general, UiText.Get("Запускать с Windows"), new Rectangle(0, 132, 324, 28));
            privacy = Check(general, UiText.Get("Приватный режим: скрывать названия встреч и календарей"), new Rectangle(0, 168, 330, 40));
            nextTip = Check(general, UiText.Get("Ближайшая встреча при наведении на дату в панели задач"), new Rectangle(0, 216, 330, 40));
            privacy.CheckedChanged += delegate { if (draft != null) { draft.PrivateMode = privacy.Checked; calendars.Refresh(); } };

            Panel reminder = Section(UiText.Get("Напоминания"), new Rectangle(360, 0, 336, 264));
            eventTiming = Option(reminder, UiText.Get("Из настроек события"), new Rectangle(0, 36, 312, 26));
            fiveMinutes = Option(reminder, UiText.Get("За 5 минут"), new Rectangle(0, 66, 312, 26));
            twoMinutes = Option(reminder, UiText.Get("За 2 минуты"), new Rectangle(0, 96, 312, 26));
            Label(reminder, UiText.Get("События Google не меняются.\nВесь день — по настройкам Google."), new Rectangle(0, 128, 312, 34));
            Label(reminder, UiText.Get("Анимация значка"), new Rectangle(0, 168, 312, 22));
            strength = Combo(reminder, new Rectangle(0, 194, 312, 30), new[] { UiText.Get("Деликатная"), UiText.Get("Обычная"), UiText.Get("Выразительная") });
            strength.AccessibleName = UiText.Get("Интенсивность пульсации");
            Button visualPreview = Action(reminder, UiText.Get("Проверить напоминание"), new Rectangle(0, 232, 312, 30));
            visualPreview.Click += delegate { if (ReminderPreviewRequested != null) ReminderPreviewRequested(this, EventArgs.Empty); };

            Panel sounds = Section(UiText.Get("Звук"), new Rectangle(0, 288, 336, 248));
            audio = Check(sounds, UiText.Get("Звук напоминаний"), new Rectangle(0, 36, 312, 28));
            Label(sounds, UiText.Get("Мелодия"), new Rectangle(0, 76, 312, 22));
            sound = Combo(sounds, new Rectangle(0, 102, 312, 30), new[] { UiText.Get("Мягкий перезвон"), UiText.Get("Кристалл"), UiText.Get("Тёплый тон") }); sound.AccessibleName = UiText.Get("Мелодия напоминания");
            volumeLabel = Label(sounds, UiText.Get("Громкость"), new Rectangle(0, 140, 312, 22));
            volume = new TrackBar { Bounds = new Rectangle(0, 166, 312, 40), Minimum = 0, Maximum = 100, TickFrequency = 10, AccessibleName = UiText.Get("Громкость напоминания") }; sounds.Controls.Add(volume);
            volume.ValueChanged += delegate { volumeLabel.Text = UiText.Get("Громкость · ") + volume.Value + "%"; };
            Button preview = Action(sounds, UiText.Get("Прослушать"), new Rectangle(0, 214, 312, 30));
            preview.Click += async delegate
            {
                preview.Enabled = false; int style = sound.SelectedIndex, level = volume.Value;
                try { await Task.Run(delegate { using (ReminderChime chime = new ReminderChime(style, level)) chime.PlaySync(); }); }
                catch (Exception) { if (!IsDisposed) error.Text = UiText.Get("Не удалось воспроизвести звук."); }
                finally { if (!IsDisposed) preview.Enabled = true; }
            };
            Panel silence = Section(UiText.Get("Тихие часы"), new Rectangle(360, 288, 336, 248));
            quiet = Check(silence, UiText.Get("Включить тихие часы"), new Rectangle(0, 36, 312, 28));
            Label(silence, UiText.Get("С"), new Rectangle(0, 76, 140, 22)); Label(silence, UiText.Get("До"), new Rectangle(172, 76, 140, 22));
            from = Time(silence, new Rectangle(0, 102, 140, 30)); to = Time(silence, new Rectangle(172, 102, 140, 30));
            from.AccessibleName = UiText.Get("Начало тихих часов"); to.AccessibleName = UiText.Get("Конец тихих часов");
            Label(silence, UiText.Get("Звук выключен, а иконка продолжает\nподпрыгивать. Одинаковое время —\nтишина весь день. Прослушивание\nработает вручную."), new Rectangle(0, 154, 312, 82));

            Panel salary = Section(UiText.Get("Зарплата"), new Rectangle(0, 566, 696, 212));
            paydayEnabled = Check(salary, UiText.Get("Показывать дни до зарплаты"), new Rectangle(0, 36, 336, 28));
            Label(salary, UiText.Get("Выплаты"), new Rectangle(0, 76, 312, 22));
            frequency = Combo(salary, new Rectangle(0, 102, 312, 30), new[] { UiText.Get("1 раз в месяц"), UiText.Get("2 раза в месяц") });
            Label(salary, UiText.Get("В коротком месяце — последний день.\nВыходные не сдвигают дату выплаты."), new Rectangle(0, 150, 336, 52));
            Label(salary, UiText.Get("Первая дата"), new Rectangle(360, 36, 140, 22)); Label(salary, UiText.Get("Вторая дата"), new Rectangle(532, 36, 140, 22));
            firstDay = Day(salary, new Rectangle(360, 66, 140, 30)); secondDay = Day(salary, new Rectangle(532, 66, 140, 30));
            firstDay.AccessibleName = UiText.Get("Первая дата"); secondDay.AccessibleName = UiText.Get("Вторая дата");
            paydayPreview = Label(salary, "", new Rectangle(360, 118, 312, 50));
            frequency.SelectedIndexChanged += delegate { UpdateSalaryPreview(); }; paydayEnabled.CheckedChanged += delegate { UpdateSalaryPreview(); };
            firstDay.ValueChanged += delegate { UpdateSalaryPreview(); }; secondDay.ValueChanged += delegate { UpdateSalaryPreview(); };

            Panel sources = Section(UiText.Get("Календари"), new Rectangle(0, 808, 696, 236));
            calendars = new CheckedListBox { Bounds = new Rectangle(0, 42, 336, 170), CheckOnClick = true, IntegralHeight = false, BorderStyle = BorderStyle.FixedSingle, AccessibleName = UiText.Get("Отображаемые календари"), FormattingEnabled = true };
            calendars.Format += delegate(object sender, ListControlConvertEventArgs args)
            { if (draft != null && draft.PrivateMode) args.Value = UiText.Get("Календарь ") + Math.Max(1, calendars.Items.IndexOf(args.ListItem) + 1); };
            sources.Controls.Add(calendars);
            Label(sources, UiText.Get("Цвет выбранного календаря"), new Rectangle(360, 42, 312, 24));
            color = Combo(sources, new Rectangle(360, 76, 312, 30), new[] { UiText.Get("Цвет Google"), UiText.Get("Красный"), UiText.Get("Зелёный"), UiText.Get("Синий"), UiText.Get("Фиолетовый"), UiText.Get("Янтарный"), UiText.Get("Розовый") }); color.AccessibleName = UiText.Get("Цвет календаря");
            Label(sources, UiText.Get("Флажки влияют на встречи, отметки\nдней и напоминания. Цвета меняются\nтолько в WinCalendar."), new Rectangle(360, 116, 312, 66));
            Button refresh = Action(sources, UiText.Get("Обновить список"), new Rectangle(360, 190, 312, 30)); refresh.Click += delegate { if (CatalogRefreshRequested != null) CatalogRefreshRequested(this, EventArgs.Empty); };
            calendars.SelectedIndexChanged += delegate { LoadCalendarColor(); };
            color.SelectedIndexChanged += delegate
            { if (!loading && draft != null && calendars.SelectedIndex >= 0) { string id = ((CalendarChoice)calendars.SelectedItem).Id; if (color.SelectedIndex <= 0) draft.CalendarColors.Remove(id); else draft.CalendarColors[id] = colorCodes[color.SelectedIndex]; } };

            Panel pinned = Section(UiText.Get("Закреплённый значок"), new Rectangle(0, 1076, 696, 154));
            Label(pinned, UiText.Get("Нажмите правой кнопкой значок WinCalendar\nв панели задач → Открепить от панели задач.\nДля закрепления откройте ярлык ниже."), new Rectangle(0, 42, 336, 100));
            Button shortcut = Action(pinned, UiText.Get("Показать ярлык приложения"), new Rectangle(360, 42, 312, 34));
            shortcut.Click += delegate
            {
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", "/select,\"" + ShellIntegration.ShortcutPath + "\"") { UseShellExecute = true }); }
                catch (System.ComponentModel.Win32Exception) { error.Text = UiText.Get("Не удалось открыть ярлык приложения."); }
            };
            Label(pinned, UiText.Get("Скрытие виджета не выключает напоминания.\nОткрыть настройки можно через меню Пуск\n→ WinCalendar Settings."), new Rectangle(360, 88, 312, 62));
            Panel account = Section("Google Calendar", new Rectangle(0, 1260, 696, 126));
            Label(account, UiText.Get("Подключение аккаунта и обновление встреч"), new Rectangle(0, 42, 336, 60));
            connection = Action(account, UiText.Get("Настроить подключение"), new Rectangle(360, 42, 312, 34));
            connection.Click += delegate { if (ConnectionRequested != null) ConnectionRequested(this, EventArgs.Empty); };

            AddPage(UiText.Get("Основное"), general, pinned);
            AddPage(UiText.Get("Напоминания"), reminder);
            AddPage(UiText.Get("Звук"), sounds, silence);
            AddPage(UiText.Get("Зарплата"), salary);
            AddPage(UiText.Get("Календари"), sources, account);
            general.Location = Point.Empty;
            pinned.Bounds = new Rectangle(344, 0, 336, 196);
            pinned.Controls.OfType<Label>().First(c => Object.Equals(c.Tag, "section")).Width = 336;
            pinned.Controls.OfType<Label>().First(c => c.Top == 42).Bounds = new Rectangle(0, 32, 330, 62);
            shortcut.Bounds = new Rectangle(0, 100, 312, 30);
            pinned.Controls.OfType<Label>().First(c => c.Top == 88).Bounds = new Rectangle(0, 138, 330, 54);
            account.Bounds = new Rectangle(344, 236, 336, 40);
            foreach (Label label in account.Controls.OfType<Label>().ToArray()) { account.Controls.Remove(label); label.Dispose(); }
            connection.Bounds = new Rectangle(0, 0, 312, 34);
            reminder.Bounds = new Rectangle(0, 0, 680, 264);
            reminder.Controls.OfType<Label>().First(c => c.Top == 168).Bounds = new Rectangle(344, 36, 312, 22);
            strength.Bounds = new Rectangle(344, 66, 312, 30);
            visualPreview.Bounds = new Rectangle(344, 112, 312, 34);
            sounds.Location = Point.Empty; silence.Location = new Point(344, 0);
            salary.Location = sources.Location = Point.Empty;
            salary.Width = sources.Width = 680;
            foreach (Panel section in new[] { salary, sources })
                foreach (Control child in section.Controls)
                { if (child.Left >= 360) child.Left -= 16; if (Object.Equals(child.Tag, "section")) child.Width = section.Width; }

            error = Label(this, "", new Rectangle(132, 406, 340, 34)); error.AutoEllipsis = true;
            back = Action(this, UiText.Get("Назад"), new Rectangle(20, 406, 96, 34));
            save = Action(this, UiText.Get("Сохранить"), new Rectangle(496, 406, 204, 34)); save.Tag = "primary";
            viewport.TabIndex = 5; back.TabIndex = 6; save.TabIndex = 7; close.TabIndex = 8;
            back.Click += delegate { if (BackRequested != null) BackRequested(this, EventArgs.Empty); };
            save.Click += delegate { Collect(); if (SaveRequested != null) SaveRequested(draft); };
            foreach (Control control in CalendarPanel.Descendants(this)) if (!(control.Parent is NumericUpDown)) bounds[control] = control.Bounds;
            SetDpi(96); Application.AddMessageFilter(this);
        }

        private void AddPage(string text, params Panel[] sections)
        {
            int index = pages.Count;
            Panel page = new BufferedPanel { Bounds = new Rectangle(0, 0, 680, 276), Visible = index == 0 };
            content.Controls.Add(page); foreach (Panel section in sections) page.Controls.Add(section); pages.Add(page);
            Button tab = Action(this, text, new Rectangle(20 + index * 136, 68, 136, 32));
            tab.AccessibleRole = AccessibleRole.PageTab; tab.TabIndex = index;
            tab.Click += delegate { ShowPage(index); };
            tabs.Add(tab);
        }

        internal void BeginEdit(AppPreferences value)
        {
            draft = new JavaScriptSerializer().Deserialize<AppPreferences>(Json.Serialize(value)); draft.Normalize(); loading = true;
            calendars.Items.Clear(); foreach (CalendarChoice item in draft.Calendars) calendars.Items.Add(item, !draft.DisabledCalendars.Contains(item.Id));
            audio.Checked = draft.SoundEnabled; sound.SelectedIndex = draft.SoundStyle; volume.Value = draft.Volume;
            quiet.Checked = draft.QuietEnabled; from.Value = DateTime.Today.AddMinutes(draft.QuietStart); to.Value = DateTime.Today.AddMinutes(draft.QuietEnd);
            privacy.Checked = draft.PrivateMode; nextTip.Checked = draft.ShowNextTooltip; strength.SelectedIndex = draft.PulseStrength;
            language.SelectedIndex = draft.Language == "en" ? 1 : 0; weatherWidget.Checked = WeatherDateWindow.IsEnabled; startup.Checked = ShellIntegration.StartsWithWindows;
            eventTiming.Checked = draft.ReminderTiming == 0; fiveMinutes.Checked = draft.ReminderTiming == 1; twoMinutes.Checked = draft.ReminderTiming == 2;
            paydayEnabled.Checked = draft.PaydayEnabled; frequency.SelectedIndex = draft.PaydayFrequency == 2 ? 1 : 0; firstDay.Value = draft.PaydayFirst; secondDay.Value = draft.PaydaySecond;
            volumeLabel.Text = UiText.Get("Громкость · ") + volume.Value + "%"; error.Text = "";
            loading = false; if (calendars.Items.Count > 0) calendars.SelectedIndex = 0; LoadCalendarColor(); UpdateSalaryPreview(); ShowPage(0);
        }
        private void UpdateSalaryPreview()
        {
            if (paydayPreview == null) return;
            frequency.Enabled = firstDay.Enabled = paydayEnabled.Checked; secondDay.Enabled = paydayEnabled.Checked && frequency.SelectedIndex == 1;
            DateTime next = PaydaySchedule.Next(DateTime.Today, frequency.SelectedIndex == 1 ? 2 : 1, (int)firstDay.Value, (int)secondDay.Value);
            paydayPreview.Text = UiText.Get("Следующая выплата: ") + next.ToString("d MMMM", DateText.Culture);
        }
        private void LoadCalendarColor()
        {
            loading = true; string code = null; if (draft != null && calendars.SelectedIndex >= 0) draft.CalendarColors.TryGetValue(((CalendarChoice)calendars.SelectedItem).Id, out code);
            int index = Array.IndexOf(colorCodes, code); color.SelectedIndex = index < 0 ? 0 : index; color.Enabled = calendars.SelectedIndex >= 0; loading = false;
        }
        private void Collect()
        {
            draft.DisabledCalendars = calendars.Items.Cast<CalendarChoice>().Where((item, index) => !calendars.GetItemChecked(index)).Select(c => c.Id).ToList();
            draft.SoundEnabled = audio.Checked; draft.SoundStyle = sound.SelectedIndex; draft.Volume = volume.Value;
            draft.QuietEnabled = quiet.Checked; draft.QuietStart = from.Value.Hour * 60 + from.Value.Minute; draft.QuietEnd = to.Value.Hour * 60 + to.Value.Minute;
            draft.PrivateMode = privacy.Checked; draft.ShowNextTooltip = nextTip.Checked; draft.PulseStrength = strength.SelectedIndex;
            draft.Language = language.SelectedIndex == 1 ? "en" : "ru"; draft.WeatherWidgetChange = weatherWidget.Checked; draft.StartupChange = startup.Checked;
            draft.ReminderTiming = fiveMinutes.Checked ? 1 : twoMinutes.Checked ? 2 : 0;
            draft.PaydayEnabled = paydayEnabled.Checked; draft.PaydayFrequency = frequency.SelectedIndex == 1 ? 2 : 1; draft.PaydayFirst = (int)firstDay.Value; draft.PaydaySecond = (int)secondDay.Value; draft.Normalize();
        }
        internal void ShowError(string message) { error.Text = message; }
        internal void SetCatalog(List<CalendarChoice> values)
        {
            if (draft == null) return; Collect(); draft.Calendars = values; loading = true; calendars.Items.Clear();
            foreach (CalendarChoice item in values) calendars.Items.Add(item, !draft.DisabledCalendars.Contains(item.Id));
            loading = false; if (calendars.Items.Count > 0) calendars.SelectedIndex = 0; LoadCalendarColor();
        }
        internal void ShowPage(int index)
        {
            selectedPage = Math.Max(0, Math.Min(pages.Count - 1, index));
            for (int i = 0; i < pages.Count; i++) pages[i].Visible = i == selectedPage;
            viewport.AutoScrollPosition = Point.Empty; ApplyColors(theme);
        }
        internal void SetDpi(int value, bool? dark = null)
        {
            if (dpi != value)
            {
                int oldDpi = dpi; Point scroll = viewport.AutoScrollPosition; viewport.AutoScrollPosition = Point.Empty;
                Font oldBody = body, oldHeading = heading, oldSection = sectionFont; dpi = value;
                body = new Font("Segoe UI", 9.5f * dpi / 72f, FontStyle.Regular, GraphicsUnit.Pixel);
                heading = new Font("Segoe UI", 17 * dpi / 72f, FontStyle.Bold, GraphicsUnit.Pixel);
                sectionFont = new Font("Segoe UI Semibold", 12 * dpi / 72f, FontStyle.Regular, GraphicsUnit.Pixel);
                SuspendLayout(); content.SuspendLayout();
                foreach (var pair in bounds)
                {
                    Rectangle r = pair.Value; pair.Key.Bounds = new Rectangle(Scale(r.X), Scale(r.Y), Scale(r.Width), Scale(r.Height));
                    pair.Key.Font = Object.Equals(pair.Key.Tag, "heading") ? heading : Object.Equals(pair.Key.Tag, "section") ? sectionFont : body;
                }
                content.ResumeLayout(); ResumeLayout(); viewport.AutoScrollMinSize = content.Size; LayoutFrame();
                if (oldDpi > 0) viewport.AutoScrollPosition = new Point(0, (int)(-scroll.Y * (double)dpi / oldDpi));
                if (oldBody != null) oldBody.Dispose(); if (oldHeading != null) oldHeading.Dispose(); if (oldSection != null) oldSection.Dispose();
            }
            theme = dark; ApplyColors(dark);
        }
        protected override void OnResize(EventArgs e) { base.OnResize(e); if (viewport != null && dpi > 0) LayoutFrame(); }
        private void LayoutFrame()
        {
            viewport.Bounds = new Rectangle(Scale(20), Scale(112), Math.Max(100, Width - Scale(40)), Math.Max(80, Height - Scale(184)));
            back.Top = save.Top = error.Top = Height - Scale(54); save.Left = Width - Scale(224); close.Left = Width - Scale(52);
            error.Width = Math.Max(0, save.Left - error.Left - Scale(12));
            title.Width = Math.Max(80, Width - Scale(100));
        }
        private int Scale(int value) { return (int)Math.Round(value * dpi / 96.0); }
        private void ApplyColors(bool? dark)
        {
            CalendarPalette palette = CalendarPalette.Create(dark); BackColor = palette.Surface;
            foreach (Control c in CalendarPanel.Descendants(this))
            {
                c.BackColor = c is ComboBox || c is CheckedListBox || c is NumericUpDown ? palette.EventSurface : palette.Surface; c.ForeColor = palette.Ink;
                if (Object.Equals(c.Tag, "muted")) c.ForeColor = palette.Secondary;
                Button b = c as Button; if (b != null) b.FlatAppearance.MouseOverBackColor = palette.Hover;
                if (Object.Equals(c.Tag, "primary")) { c.BackColor = palette.Accent; c.ForeColor = palette.OnAccent; }
            }
            close.Palette = palette; error.ForeColor = palette.Accent;
            for (int i = 0; i < tabs.Count; i++)
            {
                tabs[i].BackColor = i == selectedPage ? palette.Hover : palette.Surface;
                tabs[i].ForeColor = i == selectedPage ? palette.Accent : palette.Secondary;
                tabs[i].AccessibleDescription = i == selectedPage ? UiText.Get("Выбрано") : null;
            }
        }
        private Panel Section(string title, Rectangle area)
        { Panel panel = new BufferedPanel { Bounds = area }; content.Controls.Add(panel); Label label = Label(panel, title, new Rectangle(0, 0, area.Width, 28)); label.Tag = "section"; return panel; }
        private static Label Label(Control p, string text, Rectangle r) { Label c = new Label { Text = text, Bounds = r, AutoSize = false }; p.Controls.Add(c); return c; }
        private static Button Action(Control p, string text, Rectangle r) { Button c = new Button { Text = text, Bounds = r, FlatStyle = FlatStyle.Flat }; c.FlatAppearance.BorderSize = 0; p.Controls.Add(c); return c; }
        private static CheckBox Check(Control p, string text, Rectangle r) { CheckBox c = new CheckBox { Text = text, Bounds = r, AutoSize = false }; p.Controls.Add(c); return c; }
        private static RadioButton Option(Control p, string text, Rectangle r) { RadioButton c = new RadioButton { Text = text, Bounds = r, AutoSize = false }; p.Controls.Add(c); return c; }
        private static ComboBox Combo(Control p, Rectangle r, string[] choices) { ComboBox c = new ComboBox { Bounds = r, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat }; c.Items.AddRange(choices); p.Controls.Add(c); return c; }
        private static DateTimePicker Time(Control p, Rectangle r) { DateTimePicker c = new DateTimePicker { Bounds = r, Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm", ShowUpDown = true }; p.Controls.Add(c); return c; }
        private static NumericUpDown Day(Control p, Rectangle r) { NumericUpDown c = new NumericUpDown { Bounds = r, Minimum = 1, Maximum = 31 }; p.Controls.Add(c); return c; }
        public bool PreFilterMessage(ref Message message)
        {
            if (message.Msg != 0x20a || !Visible || FindForm() == null || GetAncestor(message.HWnd, 2) != FindForm().Handle
                || !viewport.ClientRectangle.Contains(viewport.PointToClient(PointerPosition())) || CalendarPanel.Descendants(this).OfType<ComboBox>().Any(c => c.DroppedDown)) return false;
            wheel += (short)(message.WParam.ToInt64() >> 16); int notches = wheel / 120; wheel %= 120;
            int lines = SystemInformation.MouseWheelScrollLines;
            int step = lines < 0 ? viewport.ClientSize.Height - Scale(24) : Scale(16) * Math.Max(0, lines);
            viewport.AutoScrollPosition = new Point(-viewport.AutoScrollPosition.X, Math.Max(0, -viewport.AutoScrollPosition.Y - notches * step)); return true;
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys key)
        {
            if ((key == Keys.Left || key == Keys.Right) && tabs.Any(tab => tab.Focused))
            {
                ShowPage((selectedPage + (key == Keys.Right ? 1 : tabs.Count - 1)) % tabs.Count);
                tabs[selectedPage].Focus(); return true;
            }
            if (key == Keys.Escape) { if (BackRequested != null) BackRequested(this, EventArgs.Empty); return true; }
            return base.ProcessCmdKey(ref msg, key);
        }
        protected override void Dispose(bool disposing)
        { if (disposing) Application.RemoveMessageFilter(this); base.Dispose(disposing); if (disposing) { if (body != null) body.Dispose(); if (heading != null) heading.Dispose(); if (sectionFont != null) sectionFont.Dispose(); } }
        private sealed class BufferedPanel : Panel { internal BufferedPanel() { DoubleBuffered = true; } }
        [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr window, uint flags);
    }
}
