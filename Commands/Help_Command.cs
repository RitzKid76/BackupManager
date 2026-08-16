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
        .Description("list any parameters and flags for a given command\ncommand descriptions headers are given when showing a single command")
        .Parameter("command", "use this to see a description of the command");
}