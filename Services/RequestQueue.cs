using sysprog_proj1.Models;

namespace sysprog_proj1.Services
{
    public class RequestQueue
    {
        private readonly Queue<SearchRequest> _requests = new Queue<SearchRequest>();
        private readonly object _lock = new object();
        private readonly int _capacity;
        private bool _active = true;

        public RequestQueue(int capacity)
        {
            _capacity = capacity;
        }

        public bool Enqueue(SearchRequest req)
        {
            lock (_lock)
            {
                if (_requests.Count >= _capacity)
                    return false;

                _requests.Enqueue(req);
                Monitor.Pulse(_lock);
                return true;
            }
        }

        public SearchRequest? Dequeue()
        {
            lock (_lock)
            {
                while (_requests.Count == 0 && _active)
                    Monitor.Wait(_lock);

                if (!_active && _requests.Count == 0)
                    return null;

                return _requests.Dequeue();
            }
        }

        public void Shutdown()
        {
            lock (_lock)
            {
                _active = false;
                Monitor.PulseAll(_lock);
            }
        }

        public int Count { get { lock (_lock) { return _requests.Count; } } }
    }
}
