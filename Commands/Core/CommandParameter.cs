namespace Backup.Commands.Core;

public record CommandParameter(string Name, string Description, bool Required)
{
    public string GetName() => Required
        ? $"<{Name}>"
        : $"[{Name}]";

    public override string ToString() =>
        $"    {Util.PadString(Name, CommandSyntax.PADDING)}{Description}";
}