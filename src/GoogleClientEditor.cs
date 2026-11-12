using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WinCalendar
{
    internal sealed class GoogleClientEditor : UserControl
    {
        private readonly TextBox id, secret;
        private readonly Label error;
        private readonly Button save, cancel;
        private readonly LinkLabel guide;
        private readonly Dictionary<Control, Rectangle> logicalBounds = new Dictionary<Control, Rectangle>();
        private readonly List<Font> fonts = new List<Font>();
        private int layoutDpi;
        internal event EventHandler SaveRequested, CancelRequested;
        internal string ClientId { get { return id.Text.Trim(); } }
        internal string ClientSecret { get { return secret.Text.Trim(); } }
        internal Button SaveButton { get { return save; } }
        internal Button CancelButton { get { return cancel; } }

        internal GoogleClientEditor()
        {
            AutoScaleMode = AutoScaleMode.None; AutoScroll = true; Size = new Size(688, 380);
            AddLabel(UiText.Get("Подключение"), new Rectangle(16, 12, 312, 32), true);
            AddLabel(UiText.Get("Данные OAuth-клиента Google"), new Rectangle(16, 46, 312, 24), false);
            AddLabel("Client ID", new Rectangle(16, 98, 312, 24), false);
            id = new TextBox { Bounds = new Rectangle(16, 126, 312, 28), MaxLength = 512, AccessibleName = "OAuth Desktop Client ID" };
            Controls.Add(id);
            AddLabel("Client secret", new Rectangle(360, 98, 312, 24), false);
            secret = new TextBox { Bounds = new Rectangle(360, 126, 312, 28), MaxLength = 1024, UseSystemPasswordChar = true, AccessibleName = "OAuth Client secret" };
            Controls.Add(secret);
            guide = new LinkLabel { Bounds = new Rectangle(16, 190, 312, 28), Text = UiText.Get("Как настроить Google Cloud") };
            guide.LinkClicked += delegate { Program.OpenBrowser("https://github.com/sergik15828/WinCalendar/blob/main/GOOGLE_SETUP.md"); };
            Controls.Add(guide);
            AddLabel(UiText.Get("Данные сохраняются в хранилище Windows.\nПустое поле секрета сохраняет прежний\nсекрет для того же Client ID."), new Rectangle(360, 184, 312, 74), false);
            error = AddLabel("", new Rectangle(16, 234, 312, 60), false);
            cancel = ActionButton(UiText.Get("Назад"), new Rectangle(16, 330, 96, 34));
            save = ActionButton(UiText.Get("Сохранить"), new Rectangle(464, 330, 208, 34));
            cancel.Click += delegate { Clear(); if (CancelRequested != null) CancelRequested(this, EventArgs.Empty); };
            save.Click += delegate
            {
                if (!ClientId.EndsWith(".apps.googleusercontent.com", StringComparison.Ordinal)) { ShowError(UiText.Get("Нужен Client ID клиента типа Desktop app.")); return; }
                if (SaveRequested != null) SaveRequested(this, EventArgs.Empty);
            };
            foreach (Control control in Controls) logicalBounds.Add(control, control.Bounds);
            SetDpi(96);
        }

        private Label AddLabel(string text, Rectangle bounds, bool heading)
        {
            Label label = new Label { Text = text, Bounds = bounds, Tag = heading, TextAlign = ContentAlignment.MiddleLeft };
            Controls.Add(label); return label;
        }
        private Button ActionButton(string text, Rectangle bounds)
        {
            Button button = new Button { Text = text, Bounds = bounds, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            button.FlatAppearance.BorderSize = 0; Controls.Add(button); return button;
        }
        internal void BeginEdit(string currentId) { id.Text = currentId ?? ""; secret.Clear(); error.Text = ""; }
        internal void Clear() { id.Clear(); secret.Clear(); error.Text = ""; }
        internal void ShowError(string message) { error.Text = message; save.Enabled = true; }
        internal void SetBusy(bool busy) { save.Enabled = cancel.Enabled = !busy; }
        internal void SetDpi(int dpi, bool? dark = null)
        {
            List<Font> previous = new List<Font>();
            if (layoutDpi != dpi)
            {
                previous.AddRange(fonts); fonts.Clear();
                fonts.Add(new Font("Segoe UI", 10 * dpi / 72f, FontStyle.Regular, GraphicsUnit.Pixel));
                fonts.Add(new Font("Segoe UI", 17 * dpi / 72f, FontStyle.Bold, GraphicsUnit.Pixel));
                layoutDpi = dpi;
            }
            Font body = fonts[0], heading = fonts[1];
            CalendarPalette palette = CalendarPalette.Create(dark); BackColor = palette.Surface;
            foreach (Control control in Controls)
            {
                Rectangle logical = logicalBounds[control];
                control.Font = Object.Equals(control.Tag, true) ? heading : body;
                control.Bounds = new Rectangle(Scale(logical.X, dpi), Scale(logical.Y, dpi), Scale(logical.Width, dpi), Scale(logical.Height, dpi));
                control.BackColor = control is TextBox ? palette.Hover : palette.Surface;
                control.ForeColor = palette.Ink;
                Button button = control as Button;
                if (button != null) { button.BackColor = palette.Hover; button.FlatAppearance.MouseOverBackColor = palette.Hover; }
            }
            guide.LinkColor = palette.Accent; error.ForeColor = palette.Accent;
            save.BackColor = palette.Accent; save.ForeColor = palette.OnAccent;
            AutoScrollMinSize = new Size(Scale(688, dpi), Scale(380, dpi));
            foreach (Font font in previous) font.Dispose();
        }
        private static int Scale(int value, int dpi) { return (int)Math.Round(value * dpi / 96.0); }
        protected override bool ProcessCmdKey(ref Message message, Keys keys)
        {
            if (keys == Keys.Escape) { cancel.PerformClick(); return true; }
            return base.ProcessCmdKey(ref message, keys);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing && !IsDisposed) Clear(); base.Dispose(disposing);
            if (disposing) foreach (Font font in fonts) font.Dispose();
        }
    }
}
