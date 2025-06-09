using MediatR;
using Payments.Domain.Repositories;

namespace Payments.Application.Queries;

/// <summary>
/// Обработчик запроса для получения баланса счета
/// </summary>
public class GetAccountBalanceQueryHandler : IRequestHandler<GetAccountBalanceQuery, AccountBalanceDto?>
{
    private readonly IAccountRepository _accountRepository;

    public GetAccountBalanceQueryHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<AccountBalanceDto?> Handle(GetAccountBalanceQuery request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);

        if (account == null)
            return null;

        return new AccountBalanceDto
        {
            AccountId = account.Id,
            UserId = account.UserId,
            Balance = account.Balance
        };
    }
} 