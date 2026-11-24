namespace CapstoneProjectPUSH_API.Dtos
{
    // What the app sends to add or edit an article -- the photo is handled separately
    public class ArticleRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Citation { get; set; } = string.Empty;
        public string CitationUrl { get; set; } = string.Empty;
        public DateTime DateTimePublished { get; set; }
    }
}