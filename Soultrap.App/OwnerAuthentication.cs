using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
namespace Soultrap.App;
public static class OwnerAuthentication
{
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
    private struct CredUi { public int Size; public IntPtr Parent; public string Message; public string Caption; public IntPtr Banner; }
    [DllImport("credui.dll",CharSet=CharSet.Unicode)]
    private static extern int CredUIPromptForCredentialsW(ref CredUi info,string target,IntPtr reserved,int error,StringBuilder user,int userMax,StringBuilder password,int passMax,ref bool save,int flags);
    [DllImport("advapi32.dll",CharSet=CharSet.Unicode,SetLastError=true)]
    private static extern bool LogonUser(string user,string? domain,IntPtr password,int logonType,int provider,out Microsoft.Win32.SafeHandles.SafeAccessTokenHandle token);
    public static void Authenticate(string root,IntPtr parent)
    {
        using var current=WindowsIdentity.GetCurrent();
        var sid=current.User?.Value ?? throw new SecurityException("Windows SID unavailable.");
        var info=new CredUi { Size=Marshal.SizeOf<CredUi>(),Parent=parent,Caption="SOULTRAP DEVELOPER CONSOLE",Message="Confirm the password of the current Windows account. First successful confirmation activates local owner mode. Credentials are not saved." };
        var user=new StringBuilder(current.Name,513);var password=new StringBuilder(256);bool save=false;
        int result=CredUIPromptForCredentialsW(ref info,"Soultrap local owner",IntPtr.Zero,0,user,513,password,256,ref save,0x40000|0x80|0x2|0x8);
        try
        {
            if(result!=0)throw new SecurityException(result==1223?"Authentication cancelled.":$"Windows credential prompt failed ({result}).");
            using var secure=new SecureString();for(int i=0;i<password.Length;i++)secure.AppendChar(password[i]);secure.MakeReadOnly();
            var pointer=Marshal.SecureStringToGlobalAllocUnicode(secure);
            try
            {
                var text=user.ToString();var split=text.Split('\\',2);var name=split.Length==2?split[1]:text;var domain=split.Length==2?split[0]:null;
                if(!LogonUser(name,domain,pointer,2,0,out var token))throw new SecurityException($"Windows authentication failed ({Marshal.GetLastWin32Error()}). A Windows Hello PIN is not an account password.");
                using(token)using(var identity=new WindowsIdentity(token.DangerousGetHandle()))
                    if(identity.User?.Value!=sid)throw new SecurityException("Sign in as the Windows account currently running Soultrap.");
            }
            finally{Marshal.ZeroFreeGlobalAllocUnicode(pointer);}
            Directory.CreateDirectory(root);var marker=Path.Combine(root,"owner.dpapi");
            if(File.Exists(marker))
            {
                var stored=Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(marker),null,DataProtectionScope.CurrentUser));
                if(!stored.StartsWith(sid+"\n",StringComparison.Ordinal))throw new SecurityException("Owner profile does not match current SID.");
            }
            else
            {
                var data=Encoding.UTF8.GetBytes(sid+"\n"+Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));
                File.WriteAllBytes(marker,ProtectedData.Protect(data,null,DataProtectionScope.CurrentUser));CryptographicOperations.ZeroMemory(data);
            }
        }
        finally{for(int i=0;i<password.Length;i++)password[i]='\0';password.Clear();}
    }
}
