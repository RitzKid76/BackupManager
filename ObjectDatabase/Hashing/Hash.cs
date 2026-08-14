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
        catch(IOException)
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

    public override string ToString()
    {
        StringBuilder output = new();

        for (int i = 0; i < hash.Length; i++)
            output.Append($"{hash[i]:x2}");

        return output.ToString();
    }
}