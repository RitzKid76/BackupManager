using System.Text.RegularExpressions;
using Backup.Configs;
using Backup.Extensions;
using Backup.ObjectDatabase;

namespace Backup.Components;

public static class Compressor
{
    private static readonly Dictionary<char, long> timeMap = new() {
        {'s', 1_000},
        {'m', 60_000},
        {'h', 3_600_000},
        {'d', 86_400_000},
        {'w', 604_800_000},
        {'M', 2_678_400_000},
        {'y', 31_536_000_000}
    };

    private static readonly Dictionary<long, List<string>> buckets = [];
    private static readonly List<(long, string)> bucketTimes = [];

    static Compressor() =>
        StashBucketTimes();

    private static void StashBucketTimes()
    {
        LinkedList<(long, string)> bucketTimes = [];

        foreach (string bucketString in Config.CompressionBuckets)
            bucketTimes.AddLast((ParseBucket(bucketString), bucketString));

        LinkedListNode<(long, string)>? first = bucketTimes.First;
        if (first is null)
            return;

        LinkedListNode<(long, string)>? current = first.Next;
        while (current is not null)
        {
            long previousTime = current.Previous!.Value.Item1;
            long currentTime = current.Value.Item1;

            if (previousTime <= currentTime)
                throw new InvalidCompressionBucketConfig();

            current = current.Next;
        }

        long spacer = first.Value.Item1 * 2;
        bucketTimes.AddFirst((spacer, "*"));

        Compressor.bucketTimes.AddRange(bucketTimes);
    }

    private static long ParseBucket(string bucketString)
    {
        const string pattern = @"([0-9]+)(s|m|h|d|w|M|y)";
        Match match = Regex.Match(bucketString, pattern);

        int count = int.Parse(match.Groups[1].Value);
        char timeType = match.Groups[2].Value[0];

        return count * timeMap[timeType];
    }

    public static void Run()
    {
        Logger.Info("compressing...");

        PopulateBuckets();
        TrimBuckets();
    }

    private static void PopulateBuckets()
    {
        Logger.Info("populating buckets...");
        buckets.Clear();
        long timeOfCompression = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        foreach (BackupEntry entry in BackupDatabase.GetBackups().Reverse())
            AddBackupToBucket(timeOfCompression, entry);
    }

    private static void AddBackupToBucket(long timeOfCompression, BackupEntry backup)
    {
        long timePassed = timeOfCompression - backup.CreationTime;

        for (int i = 0; i < bucketTimes.Count; i++)
        {
            long bucketTime = bucketTimes[i].Item1;
            if (timePassed < bucketTime)
                continue;

            Logger.Info($"{bucketTimes[i].Item2} -> {backup.Name}");
            buckets.GetValueOrAdd(bucketTime, () => []).Add(backup.Name);

            return;
        }
    }

    private static void TrimBuckets()
    {
        Logger.Info("trimming buckets...");

        //              dont touch the spacer bucket VVV
        foreach (long bucketTime in bucketTimes.Skip(1).Select(b => b.Item1))
        {
            if (!buckets.TryGetValue(bucketTime, out List<string>? bucket))
                continue;

            if (bucket.Count <= 1)
                continue;

            foreach (string extraBackup in bucket[1..])
                BackupDatabase.Delete(extraBackup, true);
        }
    }
}