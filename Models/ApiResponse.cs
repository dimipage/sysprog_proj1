using Newtonsoft.Json;

namespace sysprog_proj1.Models
{
    // Open Library API (https://openlibrary.org/search.json?author=tolkien)
    // {
    //   "numFound": 142,
    //   "docs": [ { "title": "...", "author_name": [...], "first_publish_year": 1954 }, ... ]
    // }
    public class ApiResponse
    {
        [JsonProperty("numFound")]
        public int NumFound { get; set; }

        [JsonProperty("docs")]
        public List<Book> Books { get; set; } = new();
    }
}
