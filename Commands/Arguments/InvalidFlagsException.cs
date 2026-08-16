namespace Backup.Commands.Arguments;

public class InvalidFlagsException(string arg) : Exception($"""
    The flag '{arg}' was not expected. Valid forms are:
        -a -b -c
        -abc
        --flag [value]
""");