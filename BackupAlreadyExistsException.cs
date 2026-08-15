namespace Backup;

public class BackupAlreadyExistsException(string backupName) :
    Exception($"The backup with name '{backupName}' already exists");