namespace Backup.Commands.Core;

[AttributeUsage(AttributeTargets.Class)]
public class CommandAttribute(string token) : Attribute
{
    public string Token { get; } = token;
}