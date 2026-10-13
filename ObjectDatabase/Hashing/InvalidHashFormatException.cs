namespace Backup.ObjectDatabase.Hashing;

public class InvalidHashFormatException(string hashString) :
    Exception($"The hash '{hashString}' is not valid.");