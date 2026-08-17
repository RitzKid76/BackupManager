using System.IO.Compression;

namespace Backup.ObjectDatabase;

public static class GZIP
{
    private static readonly HashSet<string> alreadyCompressed = new(StringComparer.OrdinalIgnoreCase) {
        // archives
        ".zip", ".7z", ".rar",
        ".gz", ".tgz", ".bz2",
        ".xz", ".cab", ".iso",

        // video
        ".mp4", ".mkv", ".avi",
        ".mov", ".flv", ".webm",

        // audio
        ".mp3", ".aac", ".m4a",
        ".ogg", ".flac", ".wma",
        
        // images
        ".jpg", ".jpeg", ".png",
        ".webp", ".gif", ".heic",
        ".avif",
        
        // documents
        ".docx", ".xlsx", ".pptx",
        ".odt", ".ods", ".odp",
        ".pdf",
        
        // fonts
        ".woff2",
        
        // encrypted
        ".gpg", ".enc", ".aes"
    };

    public static bool Write(FileInfo source, string destination)
    {
        if (alreadyCompressed.Contains(source.Extension))
        {
            source.CopyTo(destination);
            return false;
        }

        using FileStream sourceStream = source.OpenRead();
        using FileStream destinationStream = File.Create(destination);

        using GZipStream compressedStream = new(destinationStream, CompressionLevel.Optimal, true);
        sourceStream.CopyTo(compressedStream);

        return true;
    }

    public static void Read(FileInfo source, string destination)
    {
        using FileStream sourceStream = source.OpenRead();
        using FileStream destinationStream = File.Create(destination);

        using GZipStream decompressedStream = new(sourceStream, CompressionMode.Decompress, true);
        decompressedStream.CopyTo(destinationStream);
    }
}