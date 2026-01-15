using MediatR;

namespace FinLedger.Application.Behaviors;
/// <summary>
/// Logging behavior - Logs all requests/responses
///Cross-Cutting Concern
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger) => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Handling {RequestName}", requestName);

        var startTime = DateTime.UtcNow;
        var response = await next();
        var duration = DateTime.UtcNow - startTime;

        _logger.LogInformation(
            " {RequestName} completed in {DurationMs}ms",
            requestName, duration.TotalMilliseconds);

        return response;
    }
}
