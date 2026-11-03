namespace Soultrap.Infrastructure;
public sealed record CleanupResult(int Deleted, long Bytes, int Skipped);
public static class LogMaintenance
{
    public static Task<CleanupResult> CleanAsync(IEnumerable<string> logDirectories, int retentionDays) => Task.Run(() =>
    {
        if (retentionDays is < 1 or > 365) throw new InvalidDataException("Log retention must be 1–365 days.");
        int deleted = 0, skipped = 0; long bytes = 0;
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        foreach (var directory in logDirectories.Distinct())
        {
            if (!Directory.Exists(directory)) continue;
            // Validate ancestry before enumeration; never follow log-directory junctions.
            SafePaths.Resolve(directory, ".soultrap-log-check");
            foreach (var path in Directory.EnumerateFiles(directory))
            {
                var file = new FileInfo(SafePaths.Resolve(directory, Path.GetFileName(path)));
                if (file.Name == "soultrap.jsonl" || !new[] { ".log", ".jsonl", ".previous" }.Contains(file.Extension, StringComparer.OrdinalIgnoreCase) || file.LastWriteTimeUtc >= cutoff) continue;
                try { var size = file.Length; file.Delete(); bytes += size; deleted++; }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException) { skipped++; }
            }
        }
        return new CleanupResult(deleted, bytes, skipped);
    });
}
