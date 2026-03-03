using MediatR;
using SADC.Order.Management.Application.Reports.DTOs;

namespace SADC.Order.Management.Application.Reports.Queries;

/// <summary>
/// Query to get order totals converted to ZAR with per-currency breakdown.
/// </summary>
public sealed record GetOrderTotalsInZarQuery : IRequest<OrderTotalsInZarDto>;
