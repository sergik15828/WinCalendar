using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace WinCalendar
{
    internal interface ICredentialStore
    {
        string Read(string name);
        void Write(string name, string value);
        void Delete(string name);
    }

    // Only the application reads its own named credentials. No credential
    // enumeration, file export, command-line arguments or diagnostic logging.
    internal sealed class WindowsCredentialStore : ICredentialStore
    {
        public string Read(string name)
        {
            IntPtr pointer;
            if (!CredRead(name, 1, 0, out pointer))
            {
                int error = Marshal.GetLastWin32Error();
                if (error == 1168) return null;
                throw new Win32Exception(error);
            }
            try
            {
                Credential credential = (Credential)Marshal.PtrToStructure(pointer, typeof(Credential));
                return Marshal.PtrToStringUni(credential.Blob, (int)credential.BlobSize / 2);
            }
            finally { CredFree(pointer); }
        }

        public void Write(string name, string value)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(value);
            if (bytes.Length > 5120) throw new InvalidOperationException("Данные подключения слишком велики для хранилища Windows.");
            IntPtr blob = Marshal.AllocHGlobal(bytes.Length);
            try
            {
                Marshal.Copy(bytes, 0, blob, bytes.Length);
                Credential credential = new Credential { Type = 1, TargetName = name, UserName = "WinCalendar",
                    BlobSize = (uint)bytes.Length, Blob = blob, Persist = 2 };
                if (!CredWrite(ref credential, 0)) throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            finally
            {
                for (int i = 0; i < bytes.Length; i++) Marshal.WriteByte(blob, i, 0);
                Marshal.FreeHGlobal(blob); Array.Clear(bytes, 0, bytes.Length);
            }
        }

        public void Delete(string name)
        {
            if (!CredDelete(name, 1, 0) && Marshal.GetLastWin32Error() != 1168)
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct Credential
        {
            internal uint Flags, Type;
            internal string TargetName, Comment;
            internal System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            internal uint BlobSize;
            internal IntPtr Blob;
            internal uint Persist, AttributeCount;
            internal IntPtr Attributes;
            internal string TargetAlias, UserName;
        }
        [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredRead(string name, uint type, uint flags, out IntPtr credential);
        [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredWrite(ref Credential credential, uint flags);
        [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredDelete(string name, uint type, uint flags);
        [DllImport("advapi32.dll")] private static extern void CredFree(IntPtr pointer);
    }
}
