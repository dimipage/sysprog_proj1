using Newtonsoft.Json;

namespace sysprog_proj1.Models
{
    public class Book
    {
        [JsonProperty("title")]
        public string? Title { get; set; }

        // Polje moze da nedostaje u API odgovoru, mora nullable
        [JsonProperty("author_name")]
        public List<string>? AuthorName { get; set; }

        [JsonProperty("first_publish_year")]
        public int? FirstPublishYear { get; set; }

        // ovaj property mozda ne treba
        [JsonProperty("key")]
        public string? Key { get; set; }

        public string GetAuthors()
        {
            if (AuthorName == null || AuthorName.Count == 0)
                return "Nepoznat autor";
            return string.Join(", ", AuthorName);
        }

        public string GetYear() =>
            FirstPublishYear.HasValue ? FirstPublishYear.Value.ToString() : "N/A";
    }
}
