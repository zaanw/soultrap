namespace Soultrap.Infrastructure;
public static class SafePaths
{
    public static string Resolve(string root, string relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || relative.Contains('\\') || relative.Contains(':') || relative.StartsWith('/') || relative.Split('/').Any(p => p is "" or "." or ".."))
            throw new InvalidDataException("Unsafe relative path.");
        root = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);
        var path = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Path escapes root.");
        for (string? current = path; current is not null; current = Path.GetDirectoryName(current))
            if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Links and junctions are not accepted.");
        return path;
    }
    public static readonly string[] CursorTargets = ["content/textures/Cursors/KeyboardMouse/ArrowCursor.png", "content/textures/Cursors/KeyboardMouse/ArrowFarCursor.png"];
    public static bool IsCursorTarget(string path) => CursorTargets.Contains(path, StringComparer.Ordinal);
    public static void ValidatePng(byte[] bytes)
    {
        ReadOnlySpan<byte> signature = [137,80,78,71,13,10,26,10];
        if (bytes.Length < 33 || bytes.Length > 2_000_000 || !bytes.AsSpan(0,8).SequenceEqual(signature) || !bytes.AsSpan(12,4).SequenceEqual("IHDR"u8))
            throw new InvalidDataException("Invalid PNG.");
        int w = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16,4));
        int h = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20,4));
        if (w is < 1 or > 256 || h is < 1 or > 256) throw new InvalidDataException("Cursor must be 1–256 px.");
    }
}
