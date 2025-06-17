using MediatR;
using Microsoft.Extensions.Logging;
using Payments.Domain.Entities;
using Payments.Domain.Repositories;

namespace Payments.Application.Commands;

/// <summary>
/// Обработчик команды пополнения счета
/// </summary>
public class TopUpAccountCommandHandler : IRequestHandler<TopUpAccountCommand, TopUpAccountResult>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILogger<TopUpAccountCommandHandler> _logger;

    public TopUpAccountCommandHandler(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        ILogger<TopUpAccountCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _logger = logger;
    }

    public async Task<TopUpAccountResult> Handle(TopUpAccountCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
            if (account == null)
            {
                return new TopUpAccountResult
                {
                    NewBalance = 0,
                    IsSuccess = false,
                    ErrorMessage = "Account not found"
                };
            }

            // Пополняем счет
            account.TopUp(request.Amount);

            // Создаем запись о транзакции
            var transaction = new Transaction(
                account.Id, 
                TransactionType.TopUp, 
                request.Amount, 
                "Account top-up");

            await _transactionRepository.AddAsync(transaction, cancellationToken);
            await _accountRepository.UpdateAsync(account, cancellationToken);
            await _accountRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Account {AccountId} topped up with {Amount}. New balance: {Balance}", 
                account.Id, request.Amount, account.Balance);

            return new TopUpAccountResult
            {
                NewBalance = account.Balance,
                IsSuccess = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to top up account {AccountId}", request.AccountId);

            return new TopUpAccountResult
            {
                NewBalance = 0,
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }
} 