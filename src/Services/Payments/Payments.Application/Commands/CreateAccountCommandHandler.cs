using MediatR;
using Microsoft.Extensions.Logging;
using Payments.Domain.Entities;
using Payments.Domain.Repositories;

namespace Payments.Application.Commands;

/// <summary>
/// Обработчик команды создания счета
/// </summary>
public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, CreateAccountResult>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<CreateAccountCommandHandler> _logger;

    public CreateAccountCommandHandler(
        IAccountRepository accountRepository,
        ILogger<CreateAccountCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _logger = logger;
    }

    public async Task<CreateAccountResult> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Проверяем, что у пользователя еще нет счета
            var existingAccount = await _accountRepository.GetByUserIdAsync(request.UserId, cancellationToken);
            if (existingAccount != null)
            {
                return new CreateAccountResult
                {
                    AccountId = Guid.Empty,
                    IsSuccess = false,
                    ErrorMessage = "Account already exists for this user"
                };
            }

            // Создаем новый счет
            var account = new Account(request.UserId);
            await _accountRepository.AddAsync(account, cancellationToken);
            await _accountRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Account {AccountId} created for user {UserId}", account.Id, request.UserId);

            return new CreateAccountResult
            {
                AccountId = account.Id,
                IsSuccess = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create account for user {UserId}", request.UserId);

            return new CreateAccountResult
            {
                AccountId = Guid.Empty,
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }
} 