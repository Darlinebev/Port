using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace DarlineBeverly.Models
{
    public class Article
    {
        public int Id { get; set; }
        [Required]
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Excerpt { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
        public DateTime? PublishedOn { get; set; }
        public int? CategoryId { get; set; }
         public Category? Category { get; set; } 
         
       
        public List<ArticleTag> ArticleTags { get; set; } = new();
        public List<ArticleFile> Files { get; set; } = new();
    }
}