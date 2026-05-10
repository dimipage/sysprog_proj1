using sysprog_proj1.Services;
using sysprog_proj1.Models;
using System.Net;
using System.Text;

namespace sysprog_proj1
{
    class Program
    {
        const string ServerUrl   = "http://localhost:5000/";
        const int    WorkerCount = 4;
        const int    QueueSize   = 100;

        static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

        static readonly Logger         _log    = new Logger();
        static readonly RequestQueue   _queue  = new RequestQueue(QueueSize);
        static readonly BookCache      _cache  = new BookCache(CacheTtl, _log);
        static readonly HttpWorkerPool _pool   = new HttpWorkerPool(_queue, _cache, _log);
        static readonly HttpListener   _server = new HttpListener();

        static int _stopping = 0;

        static void Main(string[] args)
        {
            _pool.Start(WorkerCount);

            _server.Prefixes.Add(ServerUrl);
            _server.Start();

            _log.Info($"Server pokrenut na {ServerUrl}");
            _log.Info($"Primer: {ServerUrl}search?author=tolkien&sort=new");
            _log.Info("Komande: 'stats' - statistika | 'quit' - gasenje");

            // Ctrl+C gracefull shutdown
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; Shutdown(); };

            new Thread(ConsoleLoop) { IsBackground = true }.Start();

            while (_server.IsListening)
            {
                try
                {
                    HttpListenerContext ctx = _server.GetContext();
                    _log.Info($"Primljen zahtev: {ctx.Request.HttpMethod} {ctx.Request.Url?.PathAndQuery}");
                    RouteRequest(ctx);
                }
                catch (HttpListenerException) { break; }
                catch (Exception ex)          { _log.Error($"Greska pri prijemu: {ex.Message}"); }
            }

            _log.Info("Server ugasen.");
        }

        static void RouteRequest(HttpListenerContext ctx)
        {
            if (ctx.Request.HttpMethod != "GET")
            {
                _pool.SendHtml(ctx.Response, 405, "<h2>405 - Samo GET metoda je podrzana.</h2>");
                return;
            }

            string path = ctx.Request.Url?.AbsolutePath ?? "/";

            switch (path)
            {
                case "/":
                    _pool.SendHtml(ctx.Response, 200, SearchForm());
                    break;

                case "/search":
                    EnqueueOrReject(ctx);
                    break;

                default:
                    _pool.SendHtml(ctx.Response, 404,
                        "<h2>404 - Stranica nije pronadjena.</h2><a href='/'>Pocetna</a>");
                    break;
            }
        }

        static void EnqueueOrReject(HttpListenerContext ctx)
        {
            var qs      = ctx.Request.QueryString;
            string author  = qs["author"]  ?? "";
            string title   = qs["title"]   ?? "";
            string subject = qs["subject"] ?? "";
            string sort    = qs["sort"]    ?? "";

            if (string.IsNullOrWhiteSpace(author) &&
                string.IsNullOrWhiteSpace(title)  &&
                string.IsNullOrWhiteSpace(subject))
            {
                _pool.SendHtml(ctx.Response, 400,
                    "<h2>400 - Unesite bar jedan parametar: author, title ili subject.</h2><a href='/'>Nazad</a>");
                return;
            }

            var req = new SearchRequest
            {
                Author  = author,
                Title   = title,
                Subject = subject,
                Sort    = sort,
                ClientContext = ctx
            };

            bool accepted = _queue.Enqueue(req);
            if (!accepted)
            {
                _log.Warn("Red je pun, zahtev odbijen (503)");
                _pool.SendHtml(ctx.Response, 503,
                    "<h2>503 - Server je preopterecen. Pokusajte ponovo.</h2>");
            }
        }

        static void ConsoleLoop()
        {
            while (Console.ReadLine() is string cmd)
            {
                switch (cmd.Trim().ToLower())
                {
                    case "quit":
                    case "exit":
                        Shutdown();
                        return;
                    case "stats":
                        _log.Info($"Obradeno: {HttpWorkerPool.TotalProcessed} | " +
                                  $"Gresaka: {HttpWorkerPool.TotalErrors} | " +
                                  $"U kesu: {_cache.Size} | " +
                                  $"U redu: {_queue.Count}");
                        break;
                    default:
                        Console.WriteLine("Dostupne komande: stats, quit");
                        break;
                }
            }
        }

        static void Shutdown()
        {
            if (Interlocked.Exchange(ref _stopping, 1) != 0) return;
            _log.Info("Gasenje servera...");
            _server.Stop();
            _queue.Shutdown();
            _pool.WaitForShutdown();
        }

        static string SearchForm() => @"<!DOCTYPE html>
<html lang='sr'>
<head>
  <meta charset='UTF-8'>
  <title>Pretraga knjiga</title>
  <style>
    body  { font-family: Arial, sans-serif; max-width: 550px; margin: 60px auto; padding: 0 15px; }
    h1    { color: #333; }
    label { display: block; font-weight: bold; margin-top: 14px; }
    input, select { width: 100%; padding: 8px; margin-top: 4px; box-sizing: border-box;
                    border: 1px solid #ccc; border-radius: 3px; }
    button { margin-top: 18px; padding: 10px 30px; background: #444; color: #fff;
             border: none; border-radius: 3px; cursor: pointer; font-size: 15px; }
    button:hover { background: #222; }
    small  { color: #888; }
  </style>
</head>
<body>
  <h1>Pretraga knjiga</h1>
  <small>Powered by <a href='https://openlibrary.org' target='_blank'>Open Library</a>. Unesite bar jedan parametar.</small>
  <form action='/search' method='GET'>
    <label>Autor</label>
    <input type='text' name='author'  placeholder='npr. tolkien'>
    <label>Naslov</label>
    <input type='text' name='title'   placeholder='npr. lord of the rings'>
    <label>Tema</label>
    <input type='text' name='subject' placeholder='npr. fantasy'>
    <label>Sortiranje</label>
    <select name='sort'>
      <option value=''>- bez sortiranja -</option>
      <option value='new'>Najnovije</option>
      <option value='old'>Najstarije</option>
      <option value='rating'>Ocena</option>
    </select>
    <br>
    <button type='submit'>Pretrazi</button>
  </form>
</body>
</html>";
    }
}
