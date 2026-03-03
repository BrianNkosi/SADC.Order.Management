namespace SADC.Order.Management.Application.Common.Interfaces;

/// <summary>
/// Marker interface for MediatR commands that support idempotent processing.
/// The IdempotencyBehaviour pipeline will short-circuit with the cached response
/// if the key has been seen before and has not expired.
/// </summary>
public interface IIdempotentCommand
{
    string? IdempotencyKey { get; }
}
