namespace Backup.ObjectDatabase.ObjectTypes;

public enum ObjectFormat
{
    TREE,
    BLOB,
}

public static class Extensions_ObjectFormat
{
    public static char GetFormatChar(this ObjectFormat type) => type switch
    {
        ObjectFormat.TREE => 'T',
        ObjectFormat.BLOB => 'B',

        _ => throw new ArgumentException($"Unknown {nameof(ObjectFormat)}: {type}")
    };

    public static ObjectFormat Parse(char formatChar) => formatChar switch
    {
        'T' => ObjectFormat.TREE,
        'B' => ObjectFormat.BLOB,

        _ => throw new ArgumentException($"Unknown {nameof(ObjectFormat)} format char: {formatChar}")
    };
}