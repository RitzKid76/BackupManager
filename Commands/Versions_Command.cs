using Backup.Commands.Arguments;
using Backup.Commands.Core;
using Backup.Components;
using Backup.ObjectDatabase;

namespace Backup.Commands;

[Command("versions")]
public class Versions_Command : ICommand
{
    public bool Execute(ArgumentSet argSet)
    {
        if (!argSet.HasArgs(out List<string> args))
            return false;

        string path = args[0];
        path = path.ToLower();

        List<FileVersion> versions = BackupDatabase.GetFileVersions(path).ToList();

        int versionCount = versions.Count;
        if (versionCount == 0)
        {
            Logger.Colorize($"no versions found for path '{path}'", ConsoleColor.Red);
            return true;
        }

        Logger.Colorize($"found {versionCount} versions for path '{path}'", ConsoleColor.Blue);

        foreach (FileVersion version in versions)
            Logger.Colorize(version, ConsoleColor.Cyan);

        return true;
    }



    public CommandSyntax GetSyntax(CommandSyntax syntax) => syntax
        .Description("returns stored versions for a given file or folder tree")
        .Parameter("file_path", "the path of the file to check stored versions for")
        .SupportsVerbosity();
}