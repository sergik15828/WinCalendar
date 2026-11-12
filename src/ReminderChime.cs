using System;
using System.IO;
using System.Media;

namespace WinCalendar
{
    internal sealed class ReminderChime : IDisposable
    {
        private readonly MemoryStream stream;
        private readonly SoundPlayer player;

        internal ReminderChime()
        {
            stream = new MemoryStream(CreateWave(), false);
            player = new SoundPlayer(stream);
            player.Load();
        }

        internal void Play() { player.Play(); }

        // Original two-note chime, generated locally without audio assets.
        internal static byte[] CreateWave()
        {
            const int rate = 44100, samples = 48510;
            using (MemoryStream output = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(output))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples * 2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
                writer.Write((short)1); writer.Write((short)1); writer.Write(rate); writer.Write(rate * 2);
                writer.Write((short)2); writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples * 2);
                for (int i = 0; i < samples; i++)
                {
                    double time = (double)i / rate;
                    double sample = 0.19 * Note(time, 523.251, 0.86) + 0.17 * Note(time - 0.18, 783.991, 0.92);
                    writer.Write((short)Math.Round(sample * Int16.MaxValue));
                }
                return output.ToArray();
            }
        }

        private static double Note(double time, double frequency, double duration)
        {
            if (time <= 0 || time >= duration) return 0;
            double attack = Math.Sin(Math.Min(1, time / 0.022) * Math.PI / 2);
            double release = Math.Min(1, (duration - time) / 0.12);
            double phase = 2 * Math.PI * frequency * time;
            return attack * attack * release * release * Math.Exp(-4.6 * time)
                * (Math.Sin(phase) + 0.065 * Math.Sin(2 * phase) + 0.015 * Math.Sin(3 * phase));
        }

        public void Dispose() { player.Dispose(); stream.Dispose(); }
    }
}
