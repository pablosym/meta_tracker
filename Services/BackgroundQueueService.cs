namespace Tracker.Services;

public class BackgroundQueueService : BackgroundService
{
    private readonly IBackgroundTaskQueue _queue;
    private readonly ILogger<BackgroundQueueService> _logger;

    public BackgroundQueueService(
        IBackgroundTaskQueue queue,
        ILogger<BackgroundQueueService> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            Func<CancellationToken, Task>? workItem = null;

            try
            {
                // Esperamos una tarea de la cola. Si se cancela stoppingToken,
                // acá se lanza OperationCanceledException.
                workItem = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // La app se está apagando: salimos del loop de forma limpia.
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en DequeueAsync de BackgroundTaskQueue.");
                continue;
            }

            if (workItem is null)
                continue;

            try
            {
                // Ejecutamos la tarea encolada
                await workItem(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Si se cancela mientras ejecutamos la tarea, también salimos
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ejecutando trabajo en background.");
            }
        }
    }
}


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBackgroundQueue(this IServiceCollection services)
    {
        services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
        services.AddHostedService<BackgroundQueueService>();
        return services;
    }
}
