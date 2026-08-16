using Backup.ObjectDatabase.ObjectTypes;

namespace Backup.Components.Differences;

public class ObjectFormatMismatchException(ObjectFormat current, ObjectFormat previous) : Exception(
    $"Objects have different formats. current: {current}, previous: {previous}"
);