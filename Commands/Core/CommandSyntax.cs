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

    public List<string> GenerateSyntax(bool includeDescriptionHeader = false)
    {
        List<string> output = [];
        string indent = string.Empty;

        if (includeDescriptionHeader && !string.IsNullOrEmpty(description))
        {
            indent = "|   ";

            output.Add(new string('-', 80));
            output.Add(description);
            output.Add(new string('-', 80));
        }

        StringBuilder builder = new();
        builder.Append($"{indent}{token} ");

        foreach (CommandParameter parameter in parameters)
            builder.Append($"{parameter.GetName()} ");

        if (flags.Count > 0)
            builder.Append("[flags...]");

        output.Add(builder.ToString());

        foreach (CommandParameter parameter in parameters)
            output.Add($"{indent}{parameter}");

        foreach (CommandFlag flag in flags)
            output.Add($"{indent}{flag}");

        return output;
    }
}