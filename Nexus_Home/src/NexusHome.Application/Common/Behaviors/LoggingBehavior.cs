namespace NexusHome.Application.Common.Behaviors;

using MediatR;
using Microsoft.Extensions.Logging;

/// <summary>
/// MediatR pipeline behavior for structured logging.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var requestGuid = Guid.NewGuid().ToString("N")[..8];

        _logger.LogInformation("Handling {RequestName} [{RequestGuid}]", requestName, requestGuid);

        try
        {
            var response = await next();

            _logger.LogInformation("Handled {RequestName} [{RequestGuid}] - Success", requestName, requestGuid);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Handled {RequestName} [{RequestGuid}] - Failed: {Message}", requestName, requestGuid, ex.Message);
            throw;
        }
    }
}