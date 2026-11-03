using System.Security.Cryptography;
using Soultrap.Core;
namespace Soultrap.Infrastructure;
public sealed record BackupEntry(string RelativePath, bool Existed, string? OriginalHash, string AppliedHash, string BackupFile);
public sealed record BackupJournal(string Id, string Root, string Version, DateTimeOffset Created, string Label, List<BackupEntry> Files)
{
    public bool Restored { get; set; }
    public override string ToString() => $"{Created.LocalDateTime:g} · {Label} · {Version} {(Restored ? "[restored]" : "")}";
}
public sealed class CustomizationEngine(string backups)
{
    private readonly SemaphoreSlim gate = new(1,1);
    public static string Hash(byte[] value) => Convert.ToHexString(SHA256.HashData(value));
    public static Compatibility Check(ClientInstallation client)
    {
        if (!Directory.Exists(client.Root) || Path.GetFileName(client.Root) != client.Version) return new(false, "Client version directory changed.");
        foreach (var relative in SafePaths.CursorTargets)
            if (!File.Exists(SafePaths.Resolve(client.Root, relative))) return new(false, "Client layout unsupported: cursor file missing.");
        return new(true, "Expected cursor files present. Visual effect requires an in-game Windows check; updates may replace these files.");
    }
    private static void RequireStopped()
    {
        if (OperatingSystem.IsWindows() && System.Diagnostics.Process.GetProcessesByName("RobloxPlayerBeta").Any())
            throw new IOException("Close Roblox before modifying or restoring client files.");
    }
    public async Task<BackupJournal> ApplyAsync(ClientInstallation client, IReadOnlyDictionary<string, byte[]> files, string label)
    {
        await gate.WaitAsync();
        try
        {
            RequireStopped();
            var status = Check(client); if (!status.CanApply) throw new InvalidDataException(status.Reason);
            if (files.Count is < 1 or > 2) throw new InvalidDataException("A cursor package must contain one or two files.");
            foreach (var (relative, bytes) in files)
            { if (!SafePaths.IsCursorTarget(relative)) throw new InvalidDataException("Unsupported modification target."); SafePaths.ValidatePng(bytes); }
            var id = Guid.NewGuid().ToString("N");
            var dir = SafePaths.Resolve(backups, id); Directory.CreateDirectory(dir);
            var entries = new List<BackupEntry>();
            foreach (var (relative, bytes) in files)
            {
                var target = SafePaths.Resolve(client.Root, relative);
                bool exists = File.Exists(target); var backupFile = entries.Count + ".bin";
                byte[]? original = exists ? await File.ReadAllBytesAsync(target) : null;
                if (original is not null) await File.WriteAllBytesAsync(Path.Combine(dir, backupFile), original);
                entries.Add(new(relative, exists, original is null ? null : Hash(original), Hash(bytes), backupFile));
            }
            var journal = new BackupJournal(id, Path.GetFullPath(client.Root), client.Version, DateTimeOffset.UtcNow, label, entries);
            // Durable journal and originals precede every client write. An interrupted transaction is restorable.
            await JsonFiles.WriteAsync(Path.Combine(dir, "journal.json"), journal);
            try
            {
                foreach (var (relative, bytes) in files)
                {
                    var target = SafePaths.Resolve(client.Root, relative);
                    await File.WriteAllBytesAsync(target, bytes);
                    if (Hash(await File.ReadAllBytesAsync(target)) != Hash(bytes)) throw new IOException("Post-write verification failed.");
                }
            }
            catch { await RestoreInternalAsync(client, journal); throw; }
            return journal;
        }
        finally { gate.Release(); }
    }
    public async Task<List<BackupJournal>> ListAsync()
    {
        Directory.CreateDirectory(backups);
        var result = new List<BackupJournal>();
        foreach (var dir in Directory.EnumerateDirectories(backups))
        {
            var path = SafePaths.Resolve(backups, Path.GetFileName(dir) + "/journal.json");
            if (File.Exists(path)) result.Add(await JsonFiles.ReadAsync<BackupJournal>(path));
        }
        return result.OrderByDescending(x => x.Created).ToList();
    }
    public async Task RestoreAsync(ClientInstallation client, BackupJournal journal)
    {
        await gate.WaitAsync();
        try { RequireStopped(); await RestoreInternalAsync(client, journal); }
        finally { gate.Release(); }
    }
    private async Task RestoreInternalAsync(ClientInstallation client, BackupJournal journal)
    {
        if (journal.Restored) throw new InvalidOperationException("Backup already restored.");
        if (Path.GetFullPath(client.Root) != journal.Root || client.Version != journal.Version || Path.GetFileName(client.Root) != journal.Version)
            throw new InvalidDataException("Backup belongs to a different Roblox installation/version. No files changed.");
        if (!Guid.TryParseExact(journal.Id, "N", out _) || journal.Files.Count is < 1 or > 2 || journal.Files.Select(e=>e.RelativePath).Distinct().Count() != journal.Files.Count)
            throw new InvalidDataException("Invalid backup manifest.");
        // Validate all targets and backup hashes before touching the installation.
        var restored = new List<(string Target, byte[]? Bytes)>();
        foreach (var entry in journal.Files)
        {
            if (!SafePaths.IsCursorTarget(entry.RelativePath)) throw new InvalidDataException("Unsafe backup target.");
            var target = SafePaths.Resolve(client.Root, entry.RelativePath);
            string? current = File.Exists(target) ? Hash(await File.ReadAllBytesAsync(target)) : null;
            if (current != entry.AppliedHash && current != entry.OriginalHash) throw new IOException("File changed after this backup. Restore newer backups first; no files changed.");
            byte[]? bytes = entry.Existed ? await File.ReadAllBytesAsync(SafePaths.Resolve(backups, journal.Id + "/" + entry.BackupFile)) : null;
            if (bytes is not null && Hash(bytes) != entry.OriginalHash) throw new InvalidDataException("Backup checksum mismatch.");
            restored.Add((target, bytes));
        }
        foreach (var (target, bytes) in restored)
        {
            if (bytes is null) File.Delete(target); else await File.WriteAllBytesAsync(target, bytes);
        }
        journal.Restored = true;
        await JsonFiles.WriteAsync(SafePaths.Resolve(backups, journal.Id + "/journal.json"), journal);
    }
    public async Task SnapshotAsync(ClientInstallation client)
    {
        var originals = new Dictionary<string, byte[]>();
        foreach (var file in SafePaths.CursorTargets) originals[file] = await File.ReadAllBytesAsync(SafePaths.Resolve(client.Root, file));
        await ApplyAsync(client, originals, "Manual snapshot");
    }
    public void Delete(BackupJournal journal)
    {
        if (!journal.Restored) throw new InvalidOperationException("Restore a backup before deleting it to preserve rollback.");
        if (!Guid.TryParseExact(journal.Id, "N", out _)) throw new InvalidDataException("Invalid backup id.");
        var dir = SafePaths.Resolve(backups, journal.Id);
        foreach (var file in Directory.EnumerateFiles(dir)) SafePaths.Resolve(dir, Path.GetFileName(file));
        if (Directory.EnumerateDirectories(dir).Any()) throw new InvalidDataException("Unexpected backup subdirectory.");
        Directory.Delete(dir, true);
    }
}
