using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WinCalendar
{
    internal sealed class GoogleSetupDialog : Form
    {
        private readonly TextBox clientId, secret;
        private Font ownedFont;
        internal string ClientId { get { return clientId.Text.Trim(); } }
        internal string ClientSecret { get { return secret.Text.Trim(); } }

        internal GoogleSetupDialog()
        {
            Text = "Подключение Google Calendar";
            ShowInTaskbar = false; FormBorderStyle = FormBorderStyle.FixedToolWindow;
            StartPosition = FormStartPosition.CenterScreen; MaximizeBox = MinimizeBox = false;
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(472, 374); ownedFont = new Font("Segoe UI", 10); Font = ownedFont;
            CalendarPalette colors = CalendarPalette.Create(null);
            BackColor = colors.Surface; ForeColor = colors.Ink;
            Controls.Add(new Label { Text = "Сначала создайте OAuth-клиент типа Desktop app\nв своём Google Cloud проекте и включите Calendar API.",
                Bounds = new Rectangle(20, 18, 432, 56) });
            LinkLabel guide = new LinkLabel { Text = "Пошаговая настройка Google Cloud", Bounds = new Rectangle(20, 80, 432, 28), LinkColor = colors.Accent };
            guide.LinkClicked += delegate { Program.OpenBrowser("https://github.com/sergik15828/WinCalendar/blob/main/GOOGLE_SETUP.md"); };
            Controls.Add(guide);
            Controls.Add(new Label { Text = "Client ID", Bounds = new Rectangle(20, 122, 432, 24) });
            clientId = new TextBox { Bounds = new Rectangle(20, 148, 432, 28), MaxLength = 512, AccessibleName = "OAuth Desktop Client ID" };
            Controls.Add(clientId);
            Controls.Add(new Label { Text = "Client secret (если выдан Google)", Bounds = new Rectangle(20, 192, 432, 24) });
            secret = new TextBox { Bounds = new Rectangle(20, 218, 432, 28), MaxLength = 1024, UseSystemPasswordChar = true, AccessibleName = "OAuth Client secret" };
            Controls.Add(secret);
            Controls.Add(new Label { Text = "Данные сохраняются в хранилище Windows.\nСмена клиента потребует повторного входа в аккаунт.",
                Bounds = new Rectangle(20, 264, 432, 44), ForeColor = colors.Secondary });
            Button cancel = new Button { Text = "Отмена", Bounds = new Rectangle(230, 326, 96, 30), DialogResult = DialogResult.Cancel };
            Button save = new Button { Text = "Продолжить", Bounds = new Rectangle(334, 326, 118, 30), DialogResult = DialogResult.OK };
            save.Click += delegate
            {
                if (!ClientId.EndsWith(".apps.googleusercontent.com", StringComparison.Ordinal))
                { DialogResult = DialogResult.None; MessageBox.Show(this, "Вставьте Client ID клиента типа Desktop app.", "WinCalendar"); }
            };
            Controls.Add(cancel); Controls.Add(save); AcceptButton = save; CancelButton = cancel;
            foreach (Control control in Controls)
            {
                if (control is TextBox) { control.BackColor = colors.Hover; control.ForeColor = colors.Ink; }
                Button button = control as Button;
                if (button != null) { button.FlatStyle = FlatStyle.Flat; button.FlatAppearance.BorderSize = 0; button.BackColor = colors.Hover; button.ForeColor = colors.Ink; }
            }
            save.BackColor = colors.Accent; save.ForeColor = colors.OnAccent;
        }

        protected override void OnLoad(EventArgs args)
        {
            float scale = GetDpiForWindow(Handle) / 96f;
            Rectangle[] bounds = new Rectangle[Controls.Count];
            for (int i = 0; i < Controls.Count; i++) bounds[i] = Controls[i].Bounds;
            Font previous = ownedFont;
            ownedFont = new Font("Segoe UI", 10 * 96f / 72 * scale, FontStyle.Regular, GraphicsUnit.Pixel); Font = ownedFont;
            previous.Dispose();
            ClientSize = new Size((int)Math.Round(472 * scale), (int)Math.Round(374 * scale));
            for (int i = 0; i < Controls.Count; i++)
            {
                Rectangle original = bounds[i];
                Controls[i].Bounds = new Rectangle((int)Math.Round(original.X * scale), (int)Math.Round(original.Y * scale),
                    (int)Math.Round(original.Width * scale), (int)Math.Round(original.Height * scale));
            }
            base.OnLoad(args);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { clientId.Clear(); secret.Clear(); }
            base.Dispose(disposing);
            if (disposing) ownedFont.Dispose();
        }

        [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr window);
    }
}
