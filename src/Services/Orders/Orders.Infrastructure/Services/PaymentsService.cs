using Microsoft.Extensions.Logging;
using Orders.Application.Services;
using System.Text.Json;

namespace Orders.Infrastructure.Services;

/// <summary>
/// Сервис для взаимодействия с Payments API через HTTP
/// </summary>
public class PaymentsService : IPaymentsService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaymentsService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public PaymentsService(HttpClient httpClient, ILogger<PaymentsService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<decimal?> GetUserBalanceAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/payments/accounts/{userId}/balance", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get balance for user {UserId}. Status: {StatusCode}", 
                    userId, response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var balanceResponse = JsonSerializer.Deserialize<BalanceResponse>(content, _jsonOptions);
            
            return balanceResponse?.Balance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting balance for user {UserId}", userId);
            return null;
        }
    }
}

/// <summary>
/// Ответ от Payments API с балансом
/// </summary>
public class BalanceResponse
{
    public Guid AccountId { get; set; }
    public decimal Balance { get; set; }
} 