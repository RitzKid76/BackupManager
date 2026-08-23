# Backup System
## About
This backup system was made to mimic the way that git stores files. I made it because I frequently created backups on my computer but only had room for 2 or 3 before my entire drive was full. I realized that most of the data being stored was duplicated, and I knew that git solved this problem already through trees of hashes, so I decided to learn about git and make my own version.

This project was made for Windows computers, but the code should be straightforward to modify for Linux if needed. This is a command line tool only, which means there are no pretty buttons or colors for you to click on. This is mostly because I have no experience making window apps from scratch, but I also like CLI a lot better.

Because this was just a passion project, I wouldn't recommend that you choose this over a professionally designed local backup system. This system also may have some bugs in it, so use it at your own risk.

## Getting Started
This project is a .NET project, so you can run it with `dotnet run` or build it into an executable with `dotnet build`.

Running the backup system with no arguments will enter 'continuous mode' where you can keep entering commands until you decide to exit. You can still run one-off commands directly by passing in arguments, but these arguments are the same as those in the continous mode

Running the project for the first time will generate the config files necessary. Some of the values inside the default files will be incorrect and must be changed. Continue reading below for more information.

## Path Config - `paths.yml`
### Description
The path config tells the entire system what folders or files to include in the backup. The config file is a YAML file with the keys acting as folders and list values acting as files.

### Format
This is the general structure you will want to follow when creating path configs for your backups

```yaml
# the root level entry must always be a drive letter
C:
  # you can add sub folders to the backup like this, and can even stack folders into the same line for deep nested files
  Users\ritz:
    Desktop:
    # you can add files like this
    - .gitconfig
```

You may be wondering how you prevent certain sub folders and files from being included, and for that I created the blacklist key

```yaml
# this is a blacklist and it accepts glob format. some recommendations are provided already
^:
  - "*.tmp"
  - "*.temp"
  - "*.log"
  - "*.log.*"
  - "*.bak"
  - "*.cache"
  - "*.dmp"
```

You can of course nest these blacklists in any folder that you want. Let's say you want to back up your users folder, but you don't want to include your AppData folder:

```yaml
C:
  Users\ritz:
    ^:
      - AppData
```

The blacklist only affects the parent folder and its sub folders. This means that a blacklist defined at the root of a drive applies to the entire drive. You can even include one as a drive itself which acts as a global blacklist for all drives

```yaml
C:
    # ...
^:
    # blacklist items
```

## Config - `config.json`
### Description
The config file itself holds all the other settings in the backup system. It controls where to put backups and how to maintain them.

### Format
There are 4 entries in the configuration file. Be sure to read carefully to fully understand what these do as some of them can cause data loss if you misconfigure them.

### Backup Folder
This is the folder that holds the backup file pointers, but not the contents themselves. Backup files will usually be a few kilobytes because of their structure

```json
"backupFolder": "C:\\path\\to\\your\\backup\\folder"
```

### Backup Database
This is the folder that holds the actual files in your backup. This directory will generally be very large since it holds your backup files, so ideally this is on its own dedicated hard drive

```json
"databaseFolder": "C:\\path\\to\\your\\database"
```

### Compression Buckets
As much as this may sound like it compresses your data, it's not the case. Instead of compressing data, this reduces the number of backups retained over time.

First and foremost so it isn't missed:<br>
**If you want to disable this feature because it deletes history, set the compression buckets to an empty set: `[]`**

Let's say that you take a backup daily over the next year. 5 months ago you uninstalled an app and all its contents, but realized that you wanted to keep the saves. Whether that date of removal was exactly 5 months ago or 6 or maybe 4, you don't really care. You will likely rollback to a date that is far older than the time that you remember losing your data just to be safe. This compression system aims to solve that problem by keeping more backups made recently but fewer made less recently. Continue reading for an explaination.

The configured buckets allow these possible time lengths, and each time length must be prepended with a number:

```properties
y = Year
M = Month
w = Week
d = Day
h = Hour
m = Minute
s = Second
```

The bucket entries must be ordered from the largest time period to the smallest. The system uses this order when determining which backups should be retained:

```json
"compressionBuckets": [
  "1y",
  "6M",
  "3M",
  "1M",
  "2w",
  "1w",
  "3d",
  "1d",
  "12h",
  "6h",
  "3h",
  "1h"
]
```

Every bucket can only hold **1** backup, but it will always keep the **oldest** backup that fits into that bucket. The reason we keep the oldest is so that the buckets keep filling as time progresses, but for clarity let's walk through an example with the following backups and their ages:

```properties
A - 10m
B - 30m
C - 2h
D - 4h
E - 7h
F - 10h
G - 15h
H - 19h
I - 1d
```

With the compression config above, assuming that buckets could hold infinite backups, they would fill as follows:
|  1h   |  3h   |  6h   |  1d   |
| :---: | :---: | :---: | :---: |
|   C   |   D   |   H   |   I   |
|       |       |   G   |       |
|       |       |   F   |       |
|       |       |   E   |       |

Backups A and B do not populate any buckets, so they are both kept. This also of course means that if you never specify any buckets, none of your backups will be deleted.

Backups will always fill the largest bucket that is less than their age. With the current example config, the kept backups will be the top row in the table: 
|  1h   |  3h   |  6h   |  1d   |
| :---: | :---: | :---: | :---: |
|   C   |   D   |   H   |   I   |

This ensures that the **oldest** backups are prioritized while removing some redundant ones in the process. Keeping the oldest may sound counter intuitive, but if you instead prioritize the newest backups, any older ones would simply get deleted. You would never accumulate any backups past the first bucket that is less frequent that your typical backup period.

### Garbage Collection
Garbage collection is a process that cleans up the database to ensure that it only holds important data. Each backup points to a series of objects, and the garbage collector will find any objects that are not referenced and delete them.

You can disable this process by setting the config value to false, but there should realistically be no reason to disable this unless you know what you're doing.

```json
"garbageCollect": true
```