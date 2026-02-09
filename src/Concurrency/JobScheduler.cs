namespace Shiron.Lib.Concurrency;

public sealed class JobScheduler : IDisposable {
    private readonly record struct JobWrapper(IJob Job, TaskCompletionSource? Promise);

    private readonly record struct LambdaJob<T>(Func<T> Lambda, TaskCompletionSource<T> Promise) : IJob {
        public void Execute() {
            try
            {
                Promise.SetResult(Lambda());
            } catch (Exception e)
            {
                Promise.SetException(e);
            }
        }
    }

    private readonly record struct ParallelJobWrapper<T>(T Job, int Start, int End) : IJob
        where T : IParallelJob {
        public void Execute() {
            for (var i = Start; i < End; i++) Job.Execute(i);
        }
    }

    private readonly PriorityQueue<JobWrapper, int> _queue = new();
    private readonly Thread[] _workers;
    private readonly object _lock = new();
    private volatile bool _isRunning = true;

    public JobScheduler(int workerCount = -1) {
        if (workerCount <= 0) workerCount = Math.Max(1, Environment.ProcessorCount - 2);
        _workers = new Thread[workerCount];
        for (var i = 0; i < workerCount; ++i)
        {
            var t = new Thread(WorkerLoop) {
                Name = $"Shiron_Lib_Worker_{i}",
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal
            };
            _workers[i] = t;
            t.Start();
        }
    }

    public void Schedule(IJob job, int priority = 5) {
        lock (_lock)
        {
            _queue.Enqueue(new JobWrapper(job, null), priority);
            Monitor.Pulse(_lock);
        }
    }
    public void ScheduleParallel<T>(T job, int count, int batchSize) where T : IParallelJob {
        // 1. Calculate how many batches we need
        // Example: 100 items, batchSize 32 -> 4 batches (32, 32, 32, 4)
        var batchCount = (count + batchSize - 1) / batchSize;

        lock (_lock)
        {
            for (var b = 0; b < batchCount; b++)
            {
                var start = b * batchSize;
                var end = Math.Min(start + batchSize, count);

                // 2. Create a wrapper that knows the specific range for this batch
                var wrapper = new ParallelJobWrapper<T>(job, start, end);

                // 3. Queue it normally (Priority 0 is usually highest)
                // Note: We create a new JobWrapper to fit your existing queue system
                _queue.Enqueue(new JobWrapper(wrapper, null), 0);
            }

            // Wake up enough workers to handle the new load
            Monitor.PulseAll(_lock);
        }
    }
    public Task<T> RunAsync<T>(Func<T> lambda, int priority = 5) {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        Schedule(new LambdaJob<T>(lambda, tcs), priority);
        return tcs.Task;
    }

    private void WorkerLoop() {
        while (_isRunning)
        {
            JobWrapper item;
            lock (_lock)
            {
                while (_queue.Count == 0 && _isRunning) Monitor.Wait(_lock);
                if (!_isRunning) return;
                item = _queue.Dequeue();
            }

            try
            {
                item.Job.Execute();
                item.Promise?.SetResult();
            } catch (Exception e)
            {
                Console.WriteLine($"Job failed!: {e}");
                item.Promise?.SetException(e);
            }
        }
    }

    public void Dispose() {
        _isRunning = false;
        lock (_lock)
        {
            Monitor.PulseAll(_lock);
        }
    }
}
