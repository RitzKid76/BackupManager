using System.Text;

namespace Backup.Commands.Core;

public class CommandSyntax
{
    public const int PADDING = 16;

    private readonly string token;

    private readonly List<CommandParameter> parameters = [];
    private readonly List<CommandFlag> flags = [];
    private string description = string.Empty;

    public CommandSyntax(string token) =>
        this.token = token;

    public CommandSyntax Description(string description)
    {
        this.description = description;
        return this;
    }

    public CommandSyntax Parameter(string name, string description, bool required = false)
    {
        parameters.Add(new(name, description, required));
        return this;
    }

    public CommandSyntax Flag(string flag, string description, Type? valueType = null)
    {
        flags.Add(new(flag, description, valueType));
        return this;
    }

    public string GenerateSyntax(bool includeDescriptionHeader = false)
    {
        StringBuilder output = new();
        string indent = string.Empty;

        if (includeDescriptionHeader && !string.IsNullOrEmpty(description))
        {
            output.AppendLine(new string('-', 80));
            output.AppendLine(description);
            output.AppendLine(new string('-', 80));

            indent = "|   ";
        }

        output.Append($"{indent}{token} ");

        foreach (CommandParameter parameter in parameters)
            output.Append($"{parameter.GetName()} ");

        if (flags.Count > 0)
            output.Append("[flags...]");

        output.AppendLine();

        foreach (CommandParameter parameter in parameters)
            output.AppendLine($"{indent}{parameter}");

        foreach (CommandFlag flag in flags)
            output.AppendLine($"{indent}{flag}");

        return output.ToString();
    }
}