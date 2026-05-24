using sysprog_proj1.Models;
using System.Collections.Concurrent;

namespace sysprog_proj1.Services
{
    public class BookCache
    {
        private class Entry
        {
            public List<Book> Books  { get; set; } = new();
            public DateTime   Expiry { get; set; }
            public bool       IsExpired => DateTime.UtcNow >= Expiry;
        }

        private readonly ConcurrentDictionary<string, TaskCompletionSource<Entry>> _store = new();
        private readonly TimeSpan _ttl;
        private readonly Logger   _log;

        public BookCache(TimeSpan ttl, Logger log)
        {
            _ttl = ttl;
            _log = log;
        }

        public Task<List<Book>> GetOrFetchAsync(string key, Func<Task<List<Book>>> fetch)
        {
            while (true)
            {
                if (_store.TryGetValue(key, out TaskCompletionSource<Entry>? existingTcs))
                {
                    return existingTcs.Task.ContinueWith(entryTask =>
                    {
                        if (entryTask.IsFaulted)
                            return Task.FromException<List<Book>>(
                                entryTask.Exception!.GetBaseException());

                        if (!entryTask.Result.IsExpired)
                        {
                            _log.Info($"Kes HIT [{key}]");
                            return Task.FromResult(entryTask.Result.Books);
                        }

                        _store.TryRemove(key, out _);
                        return GetOrFetchAsync(key, fetch);

                    }, TaskScheduler.Default).Unwrap();
                }

                var tcs = new TaskCompletionSource<Entry>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

                if (!_store.TryAdd(key, tcs))
                    continue;

                _log.Info($"Kes MISS [{key}] - pozivam API...");

                fetch().ContinueWith(fetchTask =>
                {
                    if (fetchTask.IsFaulted)
                    {
                        _store.TryRemove(key, out _);
                        tcs.SetException(fetchTask.Exception!.GetBaseException());
                    }
                    else
                    {
                        var entry = new Entry
                        {
                            Books  = fetchTask.Result,
                            Expiry = DateTime.UtcNow + _ttl
                        };
                        _log.Info($"Kes SET [{key}] ({fetchTask.Result.Count} knjiga, TTL={_ttl.TotalMinutes}min)");
                        tcs.SetResult(entry);
                    }
                }, TaskScheduler.Default);

                return tcs.Task.ContinueWith(t =>
                {
                    if (t.IsFaulted) throw t.Exception!.GetBaseException();
                    return t.Result.Books;
                }, TaskScheduler.Default);
            }
        }

        public void EvictExpired()
        {
            int count = 0;
            foreach (var kv in _store)
            {
                TaskCompletionSource<Entry> tcs = kv.Value;
                if (tcs.Task.IsCompletedSuccessfully && tcs.Task.Result.IsExpired)
                {
                    if (_store.TryRemove(new KeyValuePair<string, TaskCompletionSource<Entry>>(kv.Key, tcs)))
                        count++;
                }
            }
            if (count > 0)
                _log.Info($"Kes: uklonjeno {count} isteklih unosa");
        }

        public int Size => _store.Count(kv =>
            kv.Value.Task.IsCompletedSuccessfully && !kv.Value.Task.Result.IsExpired);
    }
}
