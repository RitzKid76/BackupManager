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
        List<string> args = argSet.GetArguments();
        if (args.Count == 0)
            return false;

        string path = args[0];
        path = path.ToLower();

        IEnumerable<BackupEntry> backups = BackupDatabase.GetBackups();
        Dictionary<string, BackupEntry> versions = [];

        foreach (BackupEntry backup in backups.Reverse())
        {
            ObjectReference? reference = backup.FindReferenceByPath(path);
            if (reference is null)
                continue;

            string pointer = reference.Pointer.ToString();
            versions.TryAdd(pointer, backup);
        }

        int versionCount = versions.Count;
        if (versionCount == 0)
        {
            Logger.Log($"no versions found for path '{path}'");
            return true;
        }

        Logger.Log($"found {versionCount} versions for path '{path}'");

        IEnumerable<string> lines = versions
            .OrderByDescending(p => p.Value.CreationTime)
            .Select(p => $"{p.Key} - {p.Value.GetCreationTimeString()}");

        foreach (string line in lines)
            Logger.Log(line);

        return true;
    }



    public CommandSyntax GetSyntax(CommandSyntax syntax) => syntax
        .Description("returns stored versions for a given file or folder tree")
        .Parameter("file_path", "the path of the file to check stored versions for");
}