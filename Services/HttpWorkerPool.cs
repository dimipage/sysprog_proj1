using Newtonsoft.Json;
using sysprog_proj1.Models;
using System.Net;
using System.Text;

namespace sysprog_proj1.Services
{
    public class HttpWorkerPool
    {
        private readonly RequestQueue _queue;
        private readonly BookCache _cache;
        private readonly Logger _log;
        private readonly List<Thread> _workers = new List<Thread>();

        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15),
            DefaultRequestHeaders = { { "User-Agent", "BookSearchServer/1.0" } }
        };

        public static int TotalProcessed;
        public static int TotalErrors;

        public HttpWorkerPool(RequestQueue queue, BookCache cache, Logger log)
        {
            _queue = queue;
            _cache = cache;
            _log   = log;
        }

        public void Start(int workerCount)
        {
            for (int i = 0; i < workerCount; i++)
            {
                var t = new Thread(WorkLoop)
                {
                    IsBackground = true,
                    Name = $"Worker-{i + 1}"
                };
                t.Start();
                _workers.Add(t);
            }

            new Thread(() => {
                while (true)
                {
                    Thread.Sleep(TimeSpan.FromMinutes(5));
                    _cache.EvictExpired();
                }
            }) { IsBackground = true, Name = "CacheEvictor" }.Start();

            _log.Info($"Pokrenuto {workerCount} worker niti");
        }

        void WorkLoop()
        {
            _log.Info($"{Thread.CurrentThread.Name} pokrenut");

            while (true)
            {
                SearchRequest? req = _queue.Dequeue();
                if (req == null)
                    break;

                _log.Info($"{Thread.CurrentThread.Name} obradjuje: {req.CacheKey}");
                Process(req);
            }

            _log.Info($"{Thread.CurrentThread.Name} zavrsen");
        }

        void Process(SearchRequest req)
        {
            try
            {
                List<Book> books = _cache.GetOrFetch(req.CacheKey, () => CallApi(req));
                Interlocked.Increment(ref TotalProcessed);
                SendHtml(req.ClientContext.Response, 200, BuildHtml(books, req));
            }
            catch (HttpRequestException ex)
            {
                _log.Error($"API greska: {ex.Message}");
                Interlocked.Increment(ref TotalErrors);
                SendHtml(req.ClientContext.Response, 502,
                    $"<h2>Greska: Open Library API nije dostupan.</h2><a href='/'>Nazad</a>");
            }
            catch (Exception ex)
            {
                _log.Error($"Interna greska: {ex.Message}");
                Interlocked.Increment(ref TotalErrors);
                SendHtml(req.ClientContext.Response, 500,
                    $"<h2>Interna greska servera.</h2><a href='/'>Nazad</a>");
            }
        }

        List<Book> CallApi(SearchRequest req)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(req.Author))  parts.Add("author="  + Uri.EscapeDataString(req.Author));
            if (!string.IsNullOrWhiteSpace(req.Title))   parts.Add("title="   + Uri.EscapeDataString(req.Title));
            if (!string.IsNullOrWhiteSpace(req.Subject)) parts.Add("subject=" + Uri.EscapeDataString(req.Subject));
            if (!string.IsNullOrWhiteSpace(req.Sort))    parts.Add("sort="    + Uri.EscapeDataString(req.Sort));
            parts.Add("limit=50");

            string url = "https://openlibrary.org/search.json?" + string.Join("&", parts);
            _log.Info($"API poziv: {url}");

            using var response = _http.Send(new HttpRequestMessage(HttpMethod.Get, url));
            response.EnsureSuccessStatusCode();

            string body = response.Content.ReadAsStringAsync().Result;
            ApiResponse? parsed = JsonConvert.DeserializeObject<ApiResponse>(body);
            return parsed?.Books ?? new List<Book>();
        }

        public void WaitForShutdown()
        {
            foreach (var t in _workers)
                t.Join(TimeSpan.FromSeconds(10));
        }

        static string BuildHtml(List<Book> books, SearchRequest req)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang='sr'><head><meta charset='UTF-8'><title>Rezultati</title>");
            sb.AppendLine(PageStyle());
            sb.AppendLine("</head><body>");
            sb.AppendLine("<h1>Rezultati pretrage knjiga</h1>");
            sb.AppendLine($"<p>Upit: <code>{WebUtility.HtmlEncode(req.CacheKey)}</code></p>");
            sb.AppendLine("<p><a href='/'>&#8592; Nova pretraga</a></p>");

            if (books.Count == 0)
            {
                sb.AppendLine("<p class='empty'>Nisu pronadjene knjige za zadate parametre.</p>");
            }
            else
            {
                sb.AppendLine($"<p>Pronadjeno <strong>{books.Count}</strong> knjiga:</p>");
                sb.AppendLine("<table>");
                sb.AppendLine("<tr><th>#</th><th>Naslov</th><th>Autor</th><th>Godina</th></tr>");
                for (int i = 0; i < books.Count; i++)
                {
                    Book b = books[i];
                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td>{i + 1}</td>");
                    sb.AppendLine($"<td>{WebUtility.HtmlEncode(b.Title ?? "-")}</td>");
                    sb.AppendLine($"<td>{WebUtility.HtmlEncode(b.GetAuthors())}</td>");
                    sb.AppendLine($"<td>{WebUtility.HtmlEncode(b.GetYear())}</td>");
                    sb.AppendLine("</tr>");
                }
                sb.AppendLine("</table>");
            }

            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        static string PageStyle() => @"<style>
body  { font-family: Arial, sans-serif; max-width: 900px; margin: 30px auto; padding: 0 15px; }
h1    { color: #333; }
code  { background: #f0f0f0; padding: 2px 6px; border-radius: 3px; }
table { border-collapse: collapse; width: 100%; margin-top: 10px; }
th    { background: #444; color: #fff; padding: 8px; text-align: left; }
td    { border: 1px solid #ddd; padding: 8px; }
tr:nth-child(even) td { background: #f9f9f9; }
.empty { color: #c00; font-weight: bold; }
a     { color: #0066cc; }
</style>";

        public void SendHtml(HttpListenerResponse resp, int status, string html)
        {
            try
            {
                string page = html.StartsWith("<!DOCTYPE") ? html
                    : $"<!DOCTYPE html><html><head><meta charset='UTF-8'>{PageStyle()}</head><body>{html}</body></html>";

                byte[] data = Encoding.UTF8.GetBytes(page);
                resp.StatusCode      = status;
                resp.ContentType     = "text/html; charset=utf-8";
                resp.ContentLength64 = data.Length;
                resp.OutputStream.Write(data);
                resp.OutputStream.Close();
            }
            catch (IOException) {  }
            catch (HttpListenerException) {  }
            catch (Exception ex)
            {
                _log.Error($"Greška pri slanju odgovora: {ex.Message}");
            }
        }
    }
}
