using MediatR;

namespace Payments.Application.Commands;

/// <summary>
/// Команда для пополнения счета
/// </summary>
public record TopUpAccountCommand : IRequest<TopUpAccountResult>
{
    public Guid AccountId { get; init; }
    public decimal Amount { get; init; }
}

/// <summary>
/// Результат пополнения счета
/// </summary>
public record TopUpAccountResult
{
    public decimal NewBalance { get; init; }
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
} 