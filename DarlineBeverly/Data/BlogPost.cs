namespace DarlineBeverly.Data
{
    public class BlogPost
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Snippet { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string PdfUrl { get; set; } = string.Empty; // path to PDF in wwwroot
    }
}