using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace WinCalendar
{
    internal sealed class PreferencesEditor : UserControl
    {
        private readonly Panel[] pages = new Panel[3];
        private readonly Button[] tabs = new Button[3];
        private readonly CheckedListBox calendars;
        private readonly ComboBox color, sound, strength;
        private readonly CheckBox audio, quiet, privacy, nextTip;
        private readonly TrackBar volume;
        private readonly DateTimePicker from, to;
        private readonly Label error, volumeLabel;
        private readonly CalendarSymbolButton close;
        private readonly Dictionary<Control, Rectangle> bounds = new Dictionary<Control, Rectangle>();
        private Font body, heading;
        private int dpi, selectedPage;
        private bool loading;
        private AppPreferences draft;
        private readonly string[] colorCodes = { null, "#C42B36", "#247A53", "#2563A6", "#7950B3", "#A35F18", "#B03B7F" };
        internal event EventHandler BackRequested, CloseRequested;
        internal event EventHandler CatalogRefreshRequested;
        internal event Action<AppPreferences> SaveRequested;
        internal AppPreferences Draft { get { return draft; } }

        internal PreferencesEditor()
        {
            AutoScaleMode = AutoScaleMode.None; Size = new Size(688, 380);
            Label title = Label(this, "Параметры", new Rectangle(16, 12, 590, 34)); title.Tag = "heading";
            close = new CalendarSymbolButton { Bounds = new Rectangle(640, 16, 32, 32), IsClose = true, AccessibleName = "Закрыть календарь" };
            close.Click += delegate { if (CloseRequested != null) CloseRequested(this, EventArgs.Empty); }; Controls.Add(close);
            string[] names = { "Календари", "Звук и тишина", "Вид и приватность" };
            for (int i = 0; i < 3; i++)
            {
                int page = i;
                tabs[i] = Action(this, names[i], new Rectangle(16 + i * 224, 58, 208, 32));
                tabs[i].Click += delegate { ShowPage(page); };
                pages[i] = new Panel { Bounds = new Rectangle(16, 104, 656, 210) }; Controls.Add(pages[i]);
            }
            calendars = new CheckedListBox { Bounds = new Rectangle(0, 0, 312, 210), CheckOnClick = true, IntegralHeight = false, BorderStyle = BorderStyle.FixedSingle, AccessibleName = "Отображаемые календари" };
            calendars.FormattingEnabled = true;
            calendars.Format += delegate(object sender, ListControlConvertEventArgs args)
            { if (draft != null && draft.PrivateMode) args.Value = "Календарь " + Math.Max(1, calendars.Items.IndexOf(args.ListItem) + 1); };
            pages[0].Controls.Add(calendars);
            Label(pages[0], "Цвет выбранного календаря", new Rectangle(344, 0, 312, 28));
            color = Combo(pages[0], new Rectangle(344, 38, 312, 30), new[] { "Цвет Google", "Красный", "Зелёный", "Синий", "Фиолетовый", "Янтарный", "Розовый" });
            color.AccessibleName = "Цвет календаря";
            Label(pages[0], "Флажки влияют на встречи, отметки\nдней и напоминания. Цвета меняются\nтолько в WinCalendar.", new Rectangle(344, 88, 312, 94));
            Button refreshCatalog = Action(pages[0], "Обновить список", new Rectangle(344, 178, 312, 30));
            refreshCatalog.Click += delegate { if (CatalogRefreshRequested != null) CatalogRefreshRequested(this, EventArgs.Empty); };
            calendars.SelectedIndexChanged += delegate { LoadCalendarColor(); };
            color.SelectedIndexChanged += delegate
            {
                if (loading || draft == null || calendars.SelectedIndex < 0) return;
                string id = ((CalendarChoice)calendars.SelectedItem).Id;
                if (color.SelectedIndex <= 0) draft.CalendarColors.Remove(id); else draft.CalendarColors[id] = colorCodes[color.SelectedIndex];
            };
            audio = Check(pages[1], "Звук напоминаний", new Rectangle(0, 0, 312, 28));
            Label(pages[1], "Мелодия", new Rectangle(0, 34, 312, 24));
            sound = Combo(pages[1], new Rectangle(0, 60, 312, 30), new[] { "Мягкий перезвон", "Кристалл", "Тёплый тон" });
            sound.AccessibleName = "Мелодия напоминания";
            volumeLabel = Label(pages[1], "Громкость", new Rectangle(0, 103, 312, 24));
            volume = new TrackBar { Bounds = new Rectangle(0, 130, 312, 42), Minimum = 0, Maximum = 100, TickFrequency = 10, AccessibleName = "Громкость напоминания" }; pages[1].Controls.Add(volume);
            volume.ValueChanged += delegate { volumeLabel.Text = "Громкость · " + volume.Value + "%"; };
            Button preview = Action(pages[1], "Прослушать", new Rectangle(0, 178, 312, 30));
            preview.Click += async delegate
            {
                preview.Enabled = false; int style = sound.SelectedIndex, level = volume.Value;
                try { await Task.Run(delegate { using (ReminderChime chime = new ReminderChime(style, level)) chime.PlaySync(); }); }
                catch (Exception) { if (!IsDisposed) error.Text = "Не удалось воспроизвести звук."; }
                finally { if (!IsDisposed) preview.Enabled = true; }
            };
            quiet = Check(pages[1], "Тихие часы", new Rectangle(344, 0, 312, 28));
            Label(pages[1], "С", new Rectangle(344, 34, 140, 24)); Label(pages[1], "До", new Rectangle(516, 34, 140, 24));
            from = Time(pages[1], new Rectangle(344, 60, 140, 30)); to = Time(pages[1], new Rectangle(516, 60, 140, 30));
            from.AccessibleName = "Начало тихих часов"; to.AccessibleName = "Конец тихих часов";
            Label(pages[1], "В это время звук выключен,\nпульсация остаётся. Одинаковое\nвремя — тишина весь день.\nПрослушивание работает вручную.", new Rectangle(344, 107, 312, 100));
            privacy = Check(pages[2], "Приватный режим: скрывать названия встреч и календарей", new Rectangle(0, 0, 656, 32));
            privacy.CheckedChanged += delegate { if (draft != null) { draft.PrivateMode = privacy.Checked; calendars.Refresh(); } };
            nextTip = Check(pages[2], "Ближайшая встреча при наведении на дату в панели задач", new Rectangle(0, 42, 656, 32));
            Label(pages[2], "Интенсивность пульсации", new Rectangle(0, 99, 312, 26));
            strength = Combo(pages[2], new Rectangle(0, 131, 312, 30), new[] { "Деликатная", "Обычная", "Выразительная" });
            strength.AccessibleName = "Интенсивность пульсации";
            Label(pages[2], "Учитываются настройки анимации\nи высокой контрастности Windows.\nНапоминание остаётся до клика.", new Rectangle(344, 99, 312, 92));
            error = Label(this, "", new Rectangle(128, 330, 320, 34));
            Button back = Action(this, "Назад", new Rectangle(16, 330, 96, 34));
            Button save = Action(this, "Сохранить", new Rectangle(464, 330, 208, 34)); save.Tag = "primary";
            back.Click += delegate { if (BackRequested != null) BackRequested(this, EventArgs.Empty); };
            save.Click += delegate { Collect(); if (SaveRequested != null) SaveRequested(draft); };
            foreach (Control control in CalendarPanel.Descendants(this)) bounds[control] = control.Bounds;
            SetDpi(96); ShowPage(0);
        }

        internal void BeginEdit(AppPreferences value)
        {
            draft = new JavaScriptSerializer().Deserialize<AppPreferences>(Json.Serialize(value)); draft.Normalize(); loading = true;
            calendars.Items.Clear(); foreach (CalendarChoice item in draft.Calendars) calendars.Items.Add(item, !draft.DisabledCalendars.Contains(item.Id));
            audio.Checked = draft.SoundEnabled; sound.SelectedIndex = draft.SoundStyle; volume.Value = draft.Volume;
            quiet.Checked = draft.QuietEnabled; from.Value = DateTime.Today.AddMinutes(draft.QuietStart); to.Value = DateTime.Today.AddMinutes(draft.QuietEnd);
            privacy.Checked = draft.PrivateMode; nextTip.Checked = draft.ShowNextTooltip; strength.SelectedIndex = draft.PulseStrength;
            error.Text = ""; loading = false; if (calendars.Items.Count > 0) calendars.SelectedIndex = 0; LoadCalendarColor(); ShowPage(0);
        }
        private void LoadCalendarColor()
        {
            loading = true; string code = null;
            if (draft != null && calendars.SelectedIndex >= 0) draft.CalendarColors.TryGetValue(((CalendarChoice)calendars.SelectedItem).Id, out code);
            int index = Array.IndexOf(colorCodes, code); color.SelectedIndex = index < 0 ? 0 : index; color.Enabled = calendars.SelectedIndex >= 0; loading = false;
        }
        private void Collect()
        {
            draft.DisabledCalendars = calendars.Items.Cast<CalendarChoice>().Where((item, index) => !calendars.GetItemChecked(index)).Select(c => c.Id).ToList();
            draft.SoundEnabled = audio.Checked; draft.SoundStyle = sound.SelectedIndex; draft.Volume = volume.Value;
            draft.QuietEnabled = quiet.Checked; draft.QuietStart = from.Value.Hour * 60 + from.Value.Minute; draft.QuietEnd = to.Value.Hour * 60 + to.Value.Minute;
            draft.PrivateMode = privacy.Checked; draft.ShowNextTooltip = nextTip.Checked; draft.PulseStrength = strength.SelectedIndex; draft.Normalize();
        }
        internal void ShowError(string message) { error.Text = message; }
        internal void SetCatalog(System.Collections.Generic.List<CalendarChoice> values)
        {
            if (draft == null) return;
            Collect(); draft.Calendars = values; loading = true; calendars.Items.Clear();
            foreach (CalendarChoice item in values) calendars.Items.Add(item, !draft.DisabledCalendars.Contains(item.Id));
            loading = false; if (calendars.Items.Count > 0) calendars.SelectedIndex = 0;
            LoadCalendarColor();
        }
        internal void ShowPage(int index)
        {
            selectedPage = index;
            for (int i = 0; i < 3; i++) { pages[i].Visible = i == index; tabs[i].Font = body; }
            ApplyColors(null);
        }
        internal void SetDpi(int value, bool? dark = null)
        {
            if (dpi != value)
            {
                Font oldBody = body, oldHeading = heading; dpi = value;
                body = new Font("Segoe UI", 9.5f * dpi / 72f, FontStyle.Regular, GraphicsUnit.Pixel); heading = new Font("Segoe UI", 17 * dpi / 72f, FontStyle.Bold, GraphicsUnit.Pixel);
                foreach (var pair in bounds)
                {
                    Rectangle r = pair.Value; pair.Key.Bounds = new Rectangle(Scale(r.X), Scale(r.Y), Scale(r.Width), Scale(r.Height));
                    pair.Key.Font = Object.Equals(pair.Key.Tag, "heading") ? heading : body;
                }
                if (oldBody != null) oldBody.Dispose(); if (oldHeading != null) oldHeading.Dispose();
            }
            ApplyColors(dark);
        }
        private int Scale(int value) { return (int)Math.Round(value * dpi / 96.0); }
        private void ApplyColors(bool? dark)
        {
            CalendarPalette palette = CalendarPalette.Create(dark); BackColor = palette.Surface;
            foreach (Control c in CalendarPanel.Descendants(this))
            {
                c.BackColor = c is ComboBox || c is CheckedListBox ? palette.EventSurface : palette.Surface; c.ForeColor = palette.Ink;
                Button button = c as Button; if (button != null) { button.FlatAppearance.MouseOverBackColor = palette.Hover; }
                if (Object.Equals(c.Tag, "primary")) { c.BackColor = palette.Accent; c.ForeColor = palette.OnAccent; }
            }
            for (int i = 0; i < 3; i++) { tabs[i].BackColor = i == selectedPage ? palette.Hover : palette.Surface; }
            close.Palette = palette; error.ForeColor = palette.Accent;
        }
        private static Label Label(Control parent, string text, Rectangle area)
        { Label c = new Label { Text = text, Bounds = area, AutoSize = false }; parent.Controls.Add(c); return c; }
        private static Button Action(Control parent, string text, Rectangle area)
        { Button c = new Button { Text = text, Bounds = area, FlatStyle = FlatStyle.Flat }; c.FlatAppearance.BorderSize = 0; parent.Controls.Add(c); return c; }
        private static CheckBox Check(Control parent, string text, Rectangle area)
        { CheckBox c = new CheckBox { Text = text, Bounds = area, AutoSize = false }; parent.Controls.Add(c); return c; }
        private static ComboBox Combo(Control parent, Rectangle area, string[] choices)
        { ComboBox c = new ComboBox { Bounds = area, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat }; c.Items.AddRange(choices); parent.Controls.Add(c); return c; }
        private static DateTimePicker Time(Control parent, Rectangle area)
        { DateTimePicker c = new DateTimePicker { Bounds = area, Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm", ShowUpDown = true }; parent.Controls.Add(c); return c; }
        protected override bool ProcessCmdKey(ref Message msg, Keys key)
        { if (key == Keys.Escape) { if (BackRequested != null) BackRequested(this, EventArgs.Empty); return true; } return base.ProcessCmdKey(ref msg, key); }
        protected override void Dispose(bool disposing)
        { base.Dispose(disposing); if (disposing) { if (body != null) body.Dispose(); if (heading != null) heading.Dispose(); } }
    }
}
