namespace Backup.ObjectDatabase.ObjectTypes;

public enum DatabaseObjectType
{
    TREE,
    BLOB,
}

public static class Extensions_DatabaseObjectType
{
    public static char GetFormatChar(this DatabaseObjectType type) => type switch
    {
        DatabaseObjectType.TREE => 'T',
        DatabaseObjectType.BLOB => 'B',

        _ => throw new ArgumentException($"Unknown {nameof(DatabaseObjectType)}: {type}")
    };
}