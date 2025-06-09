using MediatR;
using Microsoft.AspNetCore.Mvc;
using Payments.Application.Commands;
using Payments.Application.Queries;

namespace Payments.Api.Controllers;

/// <summary>
/// Контроллер для работы с платежами
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IMediator mediator, ILogger<PaymentsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Создание счета
    /// </summary>
    [HttpPost("accounts")]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
    {
        var command = new CreateAccountCommand { UserId = request.UserId };
        var result = await _mediator.Send(command);

        if (result.IsSuccess)
        {
            return Created($"/api/payments/accounts/{result.AccountId}", new { AccountId = result.AccountId });
        }

        if (result.ErrorMessage?.Contains("already exists") == true)
        {
            return Conflict(new { Error = result.ErrorMessage });
        }

        return BadRequest(new { Error = result.ErrorMessage });
    }

    /// <summary>
    /// Пополнение счета
    /// </summary>
    [HttpPost("accounts/{accountId}/top-up")]
    public async Task<IActionResult> TopUpAccount(Guid accountId, [FromBody] TopUpAccountRequest request)
    {
        var command = new TopUpAccountCommand 
        { 
            AccountId = accountId, 
            Amount = request.Amount 
        };

        var result = await _mediator.Send(command);

        if (result.IsSuccess)
        {
            return Ok(new { NewBalance = result.NewBalance });
        }

        return BadRequest(new { Error = result.ErrorMessage });
    }

    /// <summary>
    /// Получение баланса счета
    /// </summary>
    [HttpGet("accounts/{accountId}/balance")]
    public async Task<IActionResult> GetAccountBalance(Guid accountId)
    {
        var query = new GetAccountBalanceQuery { AccountId = accountId };
        var result = await _mediator.Send(query);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }
}

/// <summary>
/// Запрос на создание счета
/// </summary>
public record CreateAccountRequest
{
    public Guid UserId { get; init; }
}

/// <summary>
/// Запрос на пополнение счета
/// </summary>
public record TopUpAccountRequest
{
    public decimal Amount { get; init; }
} 