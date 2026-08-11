using System.Collections.Concurrent;
using UTMPro.RedirectEngine.Models;

namespace UTMPro.RedirectEngine.Services;

public class ClickQueueService
{
    private readonly ConcurrentQueue<ClickQueueItem> _queue = new();

    public void Enqueue(ClickQueueItem item)
        => _queue.Enqueue(item);

    public IEnumerable<ClickQueueItem> DrainBatch(int maxSize)
    {
        var batch = new List<ClickQueueItem>(maxSize);
        while (batch.Count < maxSize && _queue.TryDequeue(out var item))
            batch.Add(item);
        return batch;
    }

    public int Count => _queue.Count;
}
