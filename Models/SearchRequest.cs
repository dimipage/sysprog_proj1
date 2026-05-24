using System.Linq;
using System.Net;

namespace sysprog_proj1.Models
{
    public class SearchRequest
    {
        public string Author  { get; set; } = "";
        public string Title   { get; set; } = "";
        public string Subject { get; set; } = "";
        public string Sort    { get; set; } = "";

        public required HttpListenerContext ClientContext { get; set; }

        private static string NormalizeAuthor(string value) =>
            string.Join(" ", value.Trim()
                                  .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(w => w.ToLower())
                                  .OrderBy(w => w));

        public string CacheKey =>
            $"a={NormalizeAuthor(Author)}|t={Title.Trim().ToLower()}|s={Subject.Trim().ToLower()}|sort={Sort.ToLower()}";
    }
}
