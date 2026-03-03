namespace SADC.Order.Management.Application.Common.Errors;

/// <summary>
/// Represents a typed error with a machine-readable code and human-readable message.
/// </summary>
public sealed record Error(string Code, string Message)
{
    public override string ToString() => $"{Code}: {Message}";
}
