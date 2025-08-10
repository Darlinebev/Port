// Dtos/ArticleDto.cs
using System;
using System.Collections.Generic;

namespace DarlineBeverly.Dtos
{
    public class ArticleDto
    {
        public int? Id { get; set; } // null for new
        public string Title { get; set; } = string.Empty;
        public string? Slug { get; set; } // optional, can be generated server-side
        public string Content { get; set; } = string.Empty; // markdown
        public string Excerpt { get; set; } = string.Empty;
        public bool IsPublished { get; set; } = false;
        public DateTime? PublishedOn { get; set; }
        public int? CategoryId { get; set; }
        public List<string> TagNames { get; set; } = new();
        public List<string> FileUrls { get; set; } = new(); // urls returned by upload endpoint
    }
}
