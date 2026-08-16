using System.Collections;

namespace Backup.Components;

public static class Logger
{
    private static bool enabled = false;

    public static void DisableInfo() =>
        enabled = false;

    public static void EnableInfo() =>
        enabled = true;

    public static void Info(params object[] inputs)
    {
        if (!enabled)
            return;

        foreach (object input in inputs)
            Console.WriteLine(input);
    }

    public static void LogPartial(params object[] inputs)
    {
        foreach (object input in inputs)
            Console.Write(input);
    }

    public static void Log(params object[] inputs)
    {
        foreach (object input in inputs)
            Console.WriteLine(input);
    }

    public static void Log(ICollection inputs)
    {
        foreach (object input in inputs)
            Log(input);
    }
}