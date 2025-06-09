using MediatR;

namespace Payments.Application.Queries;

/// <summary>
/// Запрос для получения баланса счета
/// </summary>
public record GetAccountBalanceQuery : IRequest<AccountBalanceDto?>
{
    public Guid AccountId { get; init; }
}

/// <summary>
/// DTO для баланса счета
/// </summary>
public record AccountBalanceDto
{
    public Guid AccountId { get; init; }
    public Guid UserId { get; init; }
    public decimal Balance { get; init; }
} 