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
    private static readonly List<long> bucketTimes = [];

    static Compressor() =>
        StashBucketTimes();

    private static void StashBucketTimes()
    {
        LinkedList<long> bucketTimes = [];

        foreach (string bucketString in Config.CompressionBuckets)
            bucketTimes.AddLast(ParseBucket(bucketString));

        LinkedListNode<long>? first = bucketTimes.First;
        if (first is null)
            return;

        LinkedListNode<long>? current = first.Next;
        while (current is not null)
        {
            long previousTime = current.Previous!.Value;
            long currentTime = current.Value;

            if (previousTime <= currentTime)
                throw new InvalidCompressionBucketConfig();

            current = current.Next;
        }

        long spacer = first.Value * 2;
        bucketTimes.AddFirst(spacer);

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
        PopulateBuckets();
        TrimBuckets();
    }

    private static void PopulateBuckets()
    {
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
            long bucketTime = bucketTimes[i];
            if (timePassed < bucketTimes[i])
                continue;

            Logger.Info($"{i} -> {backup.Name}");
            buckets.GetValueOrAdd(bucketTime, () => []).Add(backup.Name);

            return;
        }
    }

    private static void TrimBuckets()
    {
        //              dont touch the spacer bucket VVV
        foreach (List<string> bucket in buckets.Values.Skip(1))
            if (bucket.Count > 1)
                foreach (string extraBackup in bucket[1..])
                    BackupDatabase.Delete(extraBackup, true);
    }
}