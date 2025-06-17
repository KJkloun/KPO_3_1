using MediatR;

namespace Payments.Application.Commands;

/// <summary>
/// Команда для создания счета
/// </summary>
public record CreateAccountCommand : IRequest<CreateAccountResult>
{
    public Guid UserId { get; init; }
}

/// <summary>
/// Результат создания счета
/// </summary>
public record CreateAccountResult
{
    public Guid AccountId { get; init; }
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
} 