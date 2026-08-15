using Backup.Commands.Arguments;

namespace Backup.Commands.Core;

public interface ICommand
{
    bool Execute(ArgumentSet argSet);
    CommandSyntax GetSyntax(CommandSyntax syntax);
}