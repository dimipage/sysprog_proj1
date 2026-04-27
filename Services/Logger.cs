namespace sysprog_proj1.Services
{
    public class Logger
    {
        private readonly object _consoleLock = new object();

        public void Log(string level, string message)
        {
            string line = $"[{DateTime.Now:HH:mm:ss.fff}] [{level}] [nit-{Thread.CurrentThread.ManagedThreadId}] {message}";
            lock (_consoleLock)
            {
                Console.WriteLine(line);
            }
        }

        public void Info(string msg)  => Log("INFO", msg);
        public void Warn(string msg)  => Log("WARN", msg);
        public void Error(string msg) => Log("ERR ", msg);
    }
}
