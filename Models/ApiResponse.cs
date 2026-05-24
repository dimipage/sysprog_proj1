using Newtonsoft.Json;

namespace sysprog_proj1.Models
{
    public class ApiResponse
    {
        [JsonProperty("numFound")]
        public int NumFound { get; set; }

        [JsonProperty("docs")]
        public List<Book> Books { get; set; } = new();
    }
}
