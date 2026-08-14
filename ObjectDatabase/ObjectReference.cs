using Backup.ObjectDatabase.Hashing;
using Backup.ObjectDatabase.ObjectTypes;

namespace Backup.ObjectDatabase;

public record ObjectReference(string Name, DatabaseObjectType Format, Hash Pointer)
{
    public override string ToString() =>
        $"{Format.GetFormatChar()} {Pointer} {Name}";
}