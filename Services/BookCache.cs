using sysprog_proj1.Models;
using System.Collections.Concurrent;

namespace sysprog_proj1.Services
{
    public class BookCache
    {
        private class Entry
        {
            public List<Book> Books   { get; set; } = new();
            public DateTime   Expiry  { get; set; }
            public bool       Fetching { get; set; }
        }

        private readonly ConcurrentDictionary<string, Entry> _store = new();
        private readonly object   _pulseLock = new object(); 
        private readonly TimeSpan _ttl;
        private readonly Logger   _log;

        public BookCache(TimeSpan ttl, Logger log)
        {
            _ttl = ttl;
            _log = log;
        }
        public List<Book> GetOrFetch(string key, Func<List<Book>> fetch)
        {
            while (true)
            {
                if (_store.TryGetValue(key, out Entry? e))
                {
                    if (e.Fetching)
                    {
                        lock (_pulseLock)
                        { //dupla provera zbog lost wakeup
                            if (_store.TryGetValue(key, out e) && e.Fetching)
                                Monitor.Wait(_pulseLock);
                        }
                        continue;
                    }

                    if (DateTime.UtcNow < e.Expiry)
                    {
                        _log.Info($"Kes HIT [{key}]");
                        return e.Books;
                    }

                    _store.TryRemove(key, out _);
                }

                var placeholder = new Entry { Fetching = true };
                if (_store.TryAdd(key, placeholder))
                    break;
            }

            try
            {
                _log.Info($"Kes MISS [{key}] - pozivam API...");
                List<Book> result = fetch();

                // zamenjujemo placeholder
                _store[key] = new Entry
                {
                    Books  = result,
                    Expiry = DateTime.UtcNow + _ttl
                };

                lock (_pulseLock)
                    Monitor.PulseAll(_pulseLock);

                _log.Info($"Kes SET [{key}] ({result.Count} knjiga, TTL={_ttl.TotalMinutes}min)");
                return result;
            }
            catch
            {
                _store.TryRemove(key, out _);
                lock (_pulseLock)
                    Monitor.PulseAll(_pulseLock);
                throw;
            }
        }
        public void EvictExpired()
        {
            int count = 0;
            foreach (var kv in _store)
            {
                if (!kv.Value.Fetching && DateTime.UtcNow >= kv.Value.Expiry)
                {
                    _store.TryRemove(new KeyValuePair<string, Entry>(kv.Key, kv.Value));
                    count++;
                }
            }
            if (count > 0)
                _log.Info($"Kes: uklonjeno {count} isteklih unosa");
        }
        
        public int Size => _store.Count;
    }
}
