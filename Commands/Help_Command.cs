using Backup.Commands.Arguments;
using Backup.Commands.Core;

namespace Backup.Commands;

[Command("help")]
public class Help_Command : ICommand
{
    public bool Execute(ArgumentSet argSet)
    {
        List<string> args = argSet.GetArguments();

        string? command = args.Count > 0
            ? args[0]
            : null;

        CommandExecutor.SendHelp(command);
        return true;
    }

    public CommandSyntax GetSyntax(CommandSyntax syntax) => syntax
        .AddParameter("command", "the command you need help with");
}