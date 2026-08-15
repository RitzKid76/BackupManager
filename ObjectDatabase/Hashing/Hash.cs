using System.Security.Cryptography;
using System.Text;

namespace Backup.ObjectDatabase.Hashing;

public class Hash
{
    private readonly byte[] hash;

    private Hash(byte[] hash) =>
        this.hash = hash;

    public static Hash? Create(FileInfo file)
    {
        try
        {
            using SHA1 sha1 = SHA1.Create();
            using FileStream stream = file.Open(FileMode.Open);

            byte[] hash = sha1.ComputeHash(stream);
            return new(hash);
        }
        catch (Exception e)
            when (e
                is IOException
                or UnauthorizedAccessException
            )
        {
            return null;
        }
    }

    public static Hash Create(string content)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(content);

        byte[] hash = SHA1.HashData(bytes);
        return new(hash);
    }

    public static Hash Parse(string hashString)
    {
        byte[] hash = Convert.FromHexString(hashString);
        return new(hash);
    }

    public override bool Equals(object? obj) =>
        obj is Hash other &&
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