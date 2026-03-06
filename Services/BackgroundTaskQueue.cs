using System.Collections.Concurrent;

namespace Tracker.Services;

public interface IBackgroundTaskQueue
{
    void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem);
    Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
}

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly ConcurrentQueue<Func<CancellationToken, Task>> _workItems = new ConcurrentQueue<Func<CancellationToken, Task>>();
    private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);

    public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem)
    {
        if (workItem == null)
        {
            throw new ArgumentNullException(nameof(workItem));
        }

        _workItems.Enqueue(workItem);
        _signal.Release();
    }

    public async Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
    {
        await _signal.WaitAsync(cancellationToken);

        if (_workItems.TryDequeue(out var workItem) && workItem is not null)
            return workItem;

        // Esto nunca debería pasar si el semáforo se administra bien
        throw new InvalidOperationException("Señal adquirida pero la cola está vacía.");
    }

}
