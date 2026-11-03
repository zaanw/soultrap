using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
namespace Soultrap.Infrastructure;
public static class Authenticode
{
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    private struct FileInfoNative { public uint Size; [MarshalAs(UnmanagedType.LPWStr)] public string FilePath; public IntPtr FileHandle; public IntPtr KnownSubject; }
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    private struct TrustData
    {
        public uint Size; public IntPtr PolicyCallback; public IntPtr SipClient; public uint UiChoice; public uint Revocation;
        public uint UnionChoice; public IntPtr File; public uint StateAction; public IntPtr State; public IntPtr Url; public uint ProviderFlags; public uint UiContext;
    }
    [DllImport("wintrust.dll", ExactSpelling=true, CharSet=CharSet.Unicode)]
    private static extern int WinVerifyTrust(IntPtr hwnd, ref Guid action, ref TrustData data);
    public static void VerifyRoblox(string path)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Authenticode requires Windows.");
        var file = new FileInfoNative { Size=(uint)Marshal.SizeOf<FileInfoNative>(), FilePath=Path.GetFullPath(path) };
        var ptr = Marshal.AllocHGlobal(Marshal.SizeOf<FileInfoNative>());
        try
        {
            Marshal.StructureToPtr(file, ptr, false);
            var data = new TrustData { Size=(uint)Marshal.SizeOf<TrustData>(), UiChoice=2, Revocation=1, UnionChoice=1, File=ptr, StateAction=1, ProviderFlags=0x80 };
            var action = new Guid("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");
            try
            {
                if (WinVerifyTrust(IntPtr.Zero, ref action, ref data) != 0) throw new InvalidDataException("Roblox executable signature could not be verified. Check Windows trust and network connectivity.");
                using var cert = new X509Certificate2(X509Certificate.CreateFromSignedFile(path));
                if (!cert.Subject.Split(',').Any(s => s.Trim().Equals("O=Roblox Corporation", StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidDataException("Executable is not signed by Roblox Corporation.");
            }
            finally { data.StateAction=2; WinVerifyTrust(IntPtr.Zero, ref action, ref data); }
        }
        finally { Marshal.DestroyStructure<FileInfoNative>(ptr); Marshal.FreeHGlobal(ptr); }
    }
}
