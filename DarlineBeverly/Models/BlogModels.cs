// Models/BlogModels.cs
using System;
using System.Collections.Generic;

namespace DarlineBeverly.Models
{
    public class Article
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty; // markdown or sanitized HTML
        public string Excerpt { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime? PublishedOn { get; set; }
        public bool IsPublished { get; set; } = false;
        public string AuthorId { get; set; } = string.Empty;
        public ICollection<ArticleTag> ArticleTags { get; set; } = new List<ArticleTag>();
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }
        public ICollection<UploadedFile> Files { get; set; } = new List<UploadedFile>();
    }

    public class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ICollection<ArticleTag> ArticleTags { get; set; } = new List<ArticleTag>();
    }

    public class ArticleTag
    {
        public int ArticleId { get; set; }
        public Article Article { get; set; } = null!;
        public int TagId { get; set; }
        public Tag Tag { get; set; } = null!;
    }

    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public ICollection<Article> Articles { get; set; } = new List<Article>();
    }

    public class UploadedFile
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty; // relative url or blob url
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
        public int? ArticleId { get; set; }
        public Article? Article { get; set; }
    }
}
