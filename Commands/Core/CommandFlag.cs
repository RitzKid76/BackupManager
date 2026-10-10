namespace Backup.Commands.Core;

public record CommandFlag(string Flag, string Description, Type? ValueType)
{
    public override string ToString()
    {
        string start;
        if (ValueType is Type type)
        {
            start = $"--{Flag} {type}";
            return $"    {start,-CommandSyntax.PADDING}{Description}";
        }

        string dash = Flag.Length > 1
            ? "--"
            : "-";

        start = $"{dash}{Flag}";
        return $"    {start,-CommandSyntax.PADDING}{Description}";
    }
}