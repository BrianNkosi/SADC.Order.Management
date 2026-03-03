using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SADC.Order.Management.Application.Common.Interfaces;
using SADC.Order.Management.Domain.Entities;

namespace SADC.Order.Management.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behaviour that provides idempotent processing for any command
/// implementing <see cref="IIdempotentCommand"/>. Cached responses are returned for
/// non-expired keys; expired records are cleaned up and the command reprocessed.
/// </summary>
public sealed class IdempotencyBehaviour<TRequest, TResponse>(
    IOrderManagementDbContext context,
    ILogger<IdempotencyBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromDays(1);

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IIdempotentCommand idempotentRequest ||
            string.IsNullOrWhiteSpace(idempotentRequest.IdempotencyKey))
            return await next();

        var key = idempotentRequest.IdempotencyKey;

        var existing = await context.IdempotencyRecords
            .FirstOrDefaultAsync(r => r.Key == key, cancellationToken);

        if (existing is not null)
        {
            if (existing.ExpiresAtUtc > DateTime.UtcNow)
            {
                logger.LogInformation(
                    "Idempotency key {IdempotencyKey} already processed, returning cached response", key);
                return JsonSerializer.Deserialize<TResponse>(existing.ResponsePayload)!;
            }

            // Expired — remove it; this is tracked and will be saved atomically
            // with the handler's own SaveChangesAsync call
            logger.LogInformation(
                "Idempotency key {IdempotencyKey} found but expired, reprocessing", key);
            context.IdempotencyRecords.Remove(existing);
        }

        var response = await next();

        // Store the result after the handler has persisted its changes
        context.IdempotencyRecords.Add(new IdempotencyRecord
        {
            Id = Guid.NewGuid(),
            Key = key,
            ResponsePayload = JsonSerializer.Serialize(response),
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.Add(DefaultTtl)
        });
        await context.SaveChangesAsync(cancellationToken);

        return response;
    }
}
