using System;
using System.Collections.Generic;

namespace DarlineBeverly.Dtos
{
    public class ArticleDto
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Excerpt { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
        public DateTime? PublishedOn { get; set; }
        public int? CategoryId { get; set; }
        public List<string> TagNames { get; set; } = new();
        public List<string> FileUrls { get; set; } = new();
    }
}