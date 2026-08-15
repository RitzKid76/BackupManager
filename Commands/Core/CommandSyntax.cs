using System.Text;

namespace Backup.Commands.Core;

public class CommandSyntax(string token)
{
    public const int PADDING = 16;

    private readonly string token = token;

    private readonly List<CommandParameter> parameters = [];
    private readonly List<CommandFlag> flags = [];

    public CommandSyntax AddParameter(string name, string description, bool required = false)
    {
        parameters.Add(new(name, description, required));
        return this;
    }

    public CommandSyntax AddFlag(string flag, string description, Type? valueType = null)
    {
        flags.Add(new(flag, description, valueType));
        return this;
    }

    public string GenerateSyntax()
    {
        StringBuilder output = new();
        output.Append($"{token} ");

        foreach (CommandParameter parameter in parameters)
            output.Append($"{parameter.GetName()} ");

        if (flags.Count > 0)
            output.Append("[flags...]");

        output.AppendLine();

        foreach (CommandParameter parameter in parameters)
            output.AppendLine(parameter.ToString());

        foreach (CommandFlag flag in flags)
            output.AppendLine(flag.ToString());

        return output.ToString();
    }
}