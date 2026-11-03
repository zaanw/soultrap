using System.Diagnostics;
using Soultrap.Core;
namespace Soultrap.Infrastructure;
public sealed class RobloxService
{
    public Task<List<ClientInstallation>> DiscoverAsync(string? configuredRoot) => Task.Run(() =>
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var roots = new List<string>
        {
            Path.Combine(local, "Roblox", "Versions"), Path.Combine(local, "Bloxstrap", "Versions"),
            Path.Combine(local, "Fishstrap", "Versions"), Path.Combine(local, "Voidstrap", "Versions")
        };
        if (!string.IsNullOrWhiteSpace(configuredRoot)) roots.Insert(0, configuredRoot);
        var found = new List<ClientInstallation>();
        foreach (var root in roots.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(root)) continue;
            var dirs = File.Exists(Path.Combine(root, "RobloxPlayerBeta.exe")) ? new[] { root } : Directory.EnumerateDirectories(root, "version-*");
            foreach (var dir in dirs)
            {
                var exe = SafePaths.Resolve(dir, "RobloxPlayerBeta.exe");
                if (File.Exists(exe)) found.Add(new(Path.GetFullPath(dir), Path.GetFileName(dir), File.GetLastWriteTimeUtc(exe)));
            }
        }
        return found.DistinctBy(c => c.Root).OrderByDescending(c => c.LastWrite).ToList();
    });
    public bool IsRunning => OperatingSystem.IsWindows() && Process.GetProcessesByName("RobloxPlayerBeta").Any();
    public void Launch(ClientInstallation client)
    {
        var exe = SafePaths.Resolve(client.Root, "RobloxPlayerBeta.exe");
        Authenticode.VerifyRoblox(exe);
        _ = Process.Start(new ProcessStartInfo(exe) { UseShellExecute = false, WorkingDirectory = client.Root, ArgumentList = { "--app" } })
            ?? throw new IOException("Windows did not start Roblox.");
    }
    public void LaunchUri(string uri)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed) || parsed.Scheme != "roblox-player" || uri.Length > 8192) throw new InvalidDataException("Invalid Roblox launch URI.");
        Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
    }
    public static void OpenOfficialDownload() => Process.Start(new ProcessStartInfo("https://www.roblox.com/download") { UseShellExecute = true });
}
