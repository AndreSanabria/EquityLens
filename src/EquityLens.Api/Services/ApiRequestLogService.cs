using EquityLens.Api.Data;
using EquityLens.Api.Models;

namespace EquityLens.Api.Services;

public class ApiRequestLogService(EquityLensDbContext dbContext) : IApiRequestLogService
{
    public async Task LogAsync(
        string provider,
        string endpointName,
        string ticker,
        int statusCode,
        bool success,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        dbContext.ApiRequestLogs.Add(new ApiRequestLog
        {
            Provider = provider,
            EndpointName = endpointName,
            Ticker = ticker,
            StatusCode = statusCode,
            Success = success,
            ErrorMessage = errorMessage ?? string.Empty,
            RequestedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
