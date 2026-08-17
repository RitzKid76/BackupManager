namespace Backup.Components.Differences;

public enum DifferenceType
{
    RENAME,
    ADDITION,
    REMOVAL,
    CHANGE,
}

public static class Extensions_DifferenceType
{
    public static char GetTypeChar(this DifferenceType type) => type switch
    {
        DifferenceType.RENAME => '#',
        DifferenceType.ADDITION => '+',
        DifferenceType.REMOVAL => '-',
        DifferenceType.CHANGE => '~',

        _ => throw new ArgumentException($"Unknown {nameof(DifferenceType)}: {type}")
    };

    public static DifferenceType Parse(char typeChar) => typeChar switch
    {
        '#' => DifferenceType.RENAME,
        '+' => DifferenceType.ADDITION,
        '-' => DifferenceType.REMOVAL,
        '~' => DifferenceType.CHANGE,

        _ => throw new ArgumentException($"Unknown {nameof(DifferenceType)} type char: {typeChar}")
    };
}