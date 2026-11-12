using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Windows.Media.Imaging;

namespace WinCalendar
{
    internal static class CalendarIcon
    {
        internal static Bitmap Draw(DateTime date, int size)
        {
            Bitmap bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.Clear(Color.Transparent);
                g.ScaleTransform(size / 64f, size / 64f);
                using (GraphicsPath body = RoundedRectangle(1, 1, 62, 62, 6))
                using (SolidBrush paper = new SolidBrush(Color.FromArgb(255, 252, 252, 252)))
                using (SolidBrush red = new SolidBrush(Color.FromArgb(196, 43, 54)))
                using (SolidBrush ink = new SolidBrush(Color.FromArgb(24, 24, 27)))
                using (Pen border = new Pen(Color.FromArgb(215, 215, 215), 1))
                {
                    g.FillPath(paper, body);
                    g.SetClip(body);
                    g.FillRectangle(red, 0, 0, 64, 27);
                    DrawFittedText(g, DateText.WeekdayAbbreviation(date), Brushes.White, new RectangleF(5, 3, 54, 20));
                    DrawFittedText(g, date.Day.ToString(DateText.Culture), ink, new RectangleF(4, 30, 56, 30));
                    g.ResetClip();
                    g.DrawPath(border, body);
                }
            }
            return bitmap;
        }

        private static void DrawFittedText(Graphics graphics, string text, Brush ink, RectangleF target)
        {
            using (GraphicsPath glyphs = new GraphicsPath())
            using (FontFamily family = new FontFamily("Segoe UI"))
            using (Matrix transform = new Matrix())
            {
                glyphs.AddString(text, family, (int)FontStyle.Bold, 100, PointF.Empty, StringFormat.GenericTypographic);
                RectangleF bounds = glyphs.GetBounds();
                float scale = Math.Min(target.Width / bounds.Width, target.Height / bounds.Height);
                transform.Translate(-bounds.X, -bounds.Y);
                transform.Scale(scale, scale, MatrixOrder.Append);
                transform.Translate(target.X + (target.Width - bounds.Width * scale) / 2,
                    target.Y + (target.Height - bounds.Height * scale) / 2, MatrixOrder.Append);
                glyphs.Transform(transform);
                graphics.FillPath(ink, glyphs);
            }
        }

        private static GraphicsPath RoundedRectangle(float x, float y, float width, float height, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2;
            path.AddArc(x, y, d, d, 180, 90);
            path.AddArc(x + width - d, y, d, d, 270, 90);
            path.AddArc(x + width - d, y + height - d, d, d, 0, 90);
            path.AddArc(x, y + height - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        internal static byte[] Create(DateTime date)
        {
            int[] sizes = { 16, 20, 24, 32, 40, 48, 64, 128 };
            List<byte[]> images = new List<byte[]>();
            foreach (int size in sizes)
            {
                using (Bitmap bitmap = Draw(date, size))
                {
                    if (size < 256) images.Add(CreateDib(bitmap));
                    else
                    {
                        using (MemoryStream png = new MemoryStream())
                        {
                            bitmap.Save(png, ImageFormat.Png);
                            images.Add(png.ToArray());
                        }
                    }
                }
            }
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write((ushort)0); writer.Write((ushort)1); writer.Write((ushort)sizes.Length);
                int offset = 6 + 16 * sizes.Length;
                for (int i = 0; i < sizes.Length; i++)
                {
                    writer.Write((byte)(sizes[i] == 256 ? 0 : sizes[i]));
                    writer.Write((byte)(sizes[i] == 256 ? 0 : sizes[i]));
                    writer.Write((byte)0); writer.Write((byte)0);
                    writer.Write((ushort)1); writer.Write((ushort)32);
                    writer.Write(images[i].Length); writer.Write(offset);
                    offset += images[i].Length;
                }
                foreach (byte[] image in images) writer.Write(image);
                return stream.ToArray();
            }
        }

        private static byte[] CreateDib(Bitmap bitmap)
        {
            // Use native 32-bit DIB frames at taskbar sizes. Keep PNG compression
            // only for the 256px frame, as in conventional Windows application ICOs.
            int size = bitmap.Width;
            int maskStride = ((size + 31) / 32) * 4;
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(40);
                writer.Write(size); writer.Write(size * 2);
                writer.Write((ushort)1); writer.Write((ushort)32);
                writer.Write(0); writer.Write(size * size * 4 + maskStride * size);
                writer.Write(0); writer.Write(0); writer.Write(0); writer.Write(0);
                for (int y = size - 1; y >= 0; y--)
                    for (int x = 0; x < size; x++)
                    {
                        Color pixel = bitmap.GetPixel(x, y);
                        writer.Write(pixel.B); writer.Write(pixel.G); writer.Write(pixel.R); writer.Write(pixel.A);
                    }
                for (int y = size - 1; y >= 0; y--)
                {
                    byte[] mask = new byte[maskStride];
                    for (int x = 0; x < size; x++)
                        if (bitmap.GetPixel(x, y).A == 0) mask[x / 8] |= (byte)(0x80 >> (x % 8));
                    writer.Write(mask);
                }
                return stream.ToArray();
            }
        }

        internal static BitmapFrame Load(byte[] icon)
        {
            using (MemoryStream stream = new MemoryStream(icon))
            {
                BitmapFrame frame = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                frame.Freeze();
                return frame;
            }
        }

        internal static void Save(string path, byte[] icon)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            // Keep one stable path so pinned shortcuts survive restarts and day changes.
            string temporary = path + ".tmp";
            File.WriteAllBytes(temporary, icon);
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
        }
    }
}
