using System.Text;
using System.Text.RegularExpressions;

namespace Backup.Components;

public static class PrettyStacktrace
{
    private const string TAB = "|   ";

    private const string COLLAPSABLE_LINE_IDENTIFIER = @"Microsoft|System|lambda_method";
    private const string COLLAPSABLE_LINE_PATTERN = @"((\|   )+).*";
    private const string COLLAPSABLE_LINE_REPLACEMENT = @"$1< Internal Calls >";

    private const string NATIVE_CLASS_PATTERN = @"at Backup[\.\w]+\.(\w+\(.*\)) in \w:\\[\w\\]+\\Backup\\[\w\\]+\\(\w+)\.cs";
    private const string NATIVE_CLASS_REPLACEMENT = @"$2.$1";


    private const string LINE_NUMBER_PATTERN = @":line ([0-9]+)";
    private const string LINE_NUMBER_REPLACEMENT = @": $1";

    public static string Get(Exception e)
    {
        List<string> lines = GetLines(e);

        StringBuilder output = new();
        output.AppendJoin("\n", lines);

        return output.ToString() + "\n";
    }

    public static List<string> GetLines(Exception e)
    {
        List<string> output = GetExceptionMessage(e);

        return CleanLines(output);
    }

    private static List<string> CleanLines(List<string> lines)
    {
        List<string> output = [];

        bool collapsing = false;
        foreach (string line in lines)
        {
            bool systemLine = Regex.IsMatch(line, COLLAPSABLE_LINE_IDENTIFIER);
            if (!collapsing && systemLine)
                output.Add(Regex.Replace(line, COLLAPSABLE_LINE_PATTERN, COLLAPSABLE_LINE_REPLACEMENT));

            collapsing = systemLine;
            if (collapsing)
                continue;

            string replacedLine = Regex.Replace(line, NATIVE_CLASS_PATTERN, NATIVE_CLASS_REPLACEMENT);
            replacedLine = Regex.Replace(replacedLine, LINE_NUMBER_PATTERN, LINE_NUMBER_REPLACEMENT);

            output.Add(replacedLine);
        }

        return output;
    }

    private static List<string> GetExceptionMessage(Exception e, int tabs = 0)
    {
        string baseTabbing = Tabbing(tabs);
        string subTabbing = Tabbing(tabs + 1);

        List<string> output = [];
        AddMessage(output, e, subTabbing, $"{baseTabbing}[{e.GetType()}] ");

        HandleSubExceptions(output, e, tabs + 1);
        AddStackTrace(output, e, subTabbing);

        return output;
    }

    private static void AddMessage(List<string> output, Exception e, string subTabbing, string fistMessagePrefix)
    {
        string[] messageLines = e.Message.Split("\n");

        string firstMessage = fistMessagePrefix + messageLines[0];
        output.Add(firstMessage.Trim());

        messageLines = messageLines[1..];
        foreach (string line in messageLines)
            output.Add(subTabbing + line.Trim());
    }

    private static void AddStackTrace(List<string> output, Exception e, string subTabbing)
    {
        string[]? stackTraceLines = e.StackTrace?.Split("\n");
        if (stackTraceLines is null)
            return;

        foreach (string line in stackTraceLines)
            output.Add(subTabbing + line.Trim());
    }

    private static void HandleSubExceptions(List<string> output, Exception e, int tabs)
    {
        if (e is AggregateException ae)
            HandleAggregateException(output, ae, tabs);
        else if (e.InnerException is not null)
            output.AddRange(GetExceptionMessage(e.InnerException, tabs));
    }

    private static void HandleAggregateException(List<string> output, AggregateException ae, int tabs)
    {
        foreach (Exception e in ae.InnerExceptions)
            output.AddRange(GetExceptionMessage(e, tabs));
    }

    private static string Tabbing(int tabs)
    {
        if (tabs <= 0)
            return string.Empty;

        StringBuilder output = new();
        for (int i = 0; i < tabs; i++)
            output.Append(TAB);

        return output.ToString();
    }
}