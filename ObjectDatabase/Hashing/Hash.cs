using Blake3;
using System.Buffers;
using System.Text;

namespace Backup.ObjectDatabase.Hashing;

public class Hash : IEquatable<Hash>
{
    private const int HashLength = 20;
    private const int BufferSize = 1024 * 1024;

    private static readonly ArrayPool<byte> bufferPool = ArrayPool<byte>.Shared;

    private readonly byte[] hash;

    private Hash(byte[] hash) =>
        this.hash = hash;

    public static Hash? Create(FileInfo file)
    {
        byte[] buffer = bufferPool.Rent(BufferSize);

        try
        {
            using FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read);

            using Hasher hasher = Hasher.New();

            int bytesRead;
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                hasher.Update(buffer.AsSpan(0, bytesRead));

            byte[] hash = new byte[HashLength];
            hasher.Finalize(hash);

            return new(hash);
        }
        catch (Exception e)
            when (e is
                IOException or
                UnauthorizedAccessException
            )
        {
            return null;
        }
        finally
        {
            bufferPool.Return(buffer);
        }
    }

    public static Hash Create(string content)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(content);

        using Hasher hasher = Hasher.New();
        hasher.Update(bytes);

        byte[] hash = new byte[HashLength];
        hasher.Finalize(hash);

        return new(hash);
    }

    public static Hash Parse(string hashString)
    {
        byte[] hash = Convert.FromHexString(hashString);
        return new(hash);
    }

    public bool Equals(Hash? other) =>
        other is not null &&
        hash.SequenceEqual(other.hash);

    public override int GetHashCode()
    {
        HashCode output = new();
        output.AddBytes(hash);

        return output.ToHashCode();
    }

    public override string ToString() =>
        Convert.ToHexStringLower(hash);
}