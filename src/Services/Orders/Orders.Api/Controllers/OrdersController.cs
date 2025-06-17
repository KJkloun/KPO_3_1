using MediatR;
using Microsoft.AspNetCore.Mvc;
using Orders.Application.Commands;
using Orders.Application.Queries;

namespace Orders.Api.Controllers;

/// <summary>
/// Контроллер для работы с заказами
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IMediator mediator, ILogger<OrdersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Создание заказа
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var command = new CreateOrderCommand
        {
            UserId = request.UserId,
            Amount = request.Amount,
            Description = request.Description
        };

        var result = await _mediator.Send(command);

        if (result.IsSuccess)
        {
            return Accepted(new { OrderId = result.OrderId });
        }

        return BadRequest(new { Error = result.ErrorMessage });
    }

    /// <summary>
    /// Получение списка заказов пользователя
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] Guid userId)
    {
        var query = new GetOrdersQuery { UserId = userId };
        var orders = await _mediator.Send(query);

        return Ok(orders);
    }

    /// <summary>
    /// Получение заказа по ID
    /// </summary>
    [HttpGet("{orderId}")]
    public async Task<IActionResult> GetOrder(Guid orderId)
    {
        var query = new GetOrderByIdQuery { OrderId = orderId };
        var order = await _mediator.Send(query);

        if (order == null)
        {
            return NotFound();
        }

        return Ok(order);
    }
}

/// <summary>
/// Запрос на создание заказа
/// </summary>
public record CreateOrderRequest
{
    public Guid UserId { get; init; }
    public decimal Amount { get; init; }
    public string? Description { get; init; }
} 