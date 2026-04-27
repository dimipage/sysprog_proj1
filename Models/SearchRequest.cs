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

        public string CacheKey =>
            $"a={Author.ToLower()}|t={Title.ToLower()}|s={Subject.ToLower()}|sort={Sort.ToLower()}";
    }
}
