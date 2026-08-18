using System.Text.RegularExpressions;
using Backup.Components;
using Backup.Commands.Core;

namespace Backup;

public static class BackupController
{
    public static void Main(string[] args)
    {
        if (args.Length == 0)
            ContinuousMode();
        else
            CommandExecutor.Execute(args);
    }

    private static void ContinuousMode()
    {
        Logger.Log("started continuous mode\nrun 'help' for options, and 'exit' to exit");

        const string tokenPattern = @"[^\s""']+|""([^""]*)""|'([^']*)'";

        while (true)
        {
            string? input = Logger.Prompt(">");

            if (input is null)
                continue;

            if (input == "exit")
                return;

            string[] args = Regex.Matches(input, tokenPattern)
                .Select(m => m.Value)
                .ToArray();

            CommandExecutor.Execute(args);
        }
    }
}