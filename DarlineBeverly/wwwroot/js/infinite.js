@page "/blog/{slug}"
@inject HttpClient Http
@inject NavigationManager Nav
@using Markdig
@using Ganss.XSS
@code {
    [Parameter] public string slug { get; set; } = string.Empty;
    private ArticleFull? article;
    private MarkupString renderedHtml;

    protected override async Task OnInitializedAsync()
    {
        article = await Http.GetFromJsonAsync<ArticleFull>($"/api/blog/articles/{slug}");
        if (article is null) { Nav.NavigateTo("/404"); return; }

        // convert markdown to HTML server-side (we assume content is markdown)
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var html = Markdig.Markdown.ToHtml(article.Content ?? "", pipeline);
        var sanitizer = new HtmlSanitizer();
        var cleaned = sanitizer.Sanitize(html);
        renderedHtml = new MarkupString(cleaned);
    }

    private class ArticleFull
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Slug { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime? PublishedOn { get; set; }
        public bool IsPublished { get; set; }
        public string? Category { get; set; }
        public IEnumerable<string>? Tags { get; set; }
    }
}

@if (article == null)
{
    <p>Loading...</p>
}
else
{
    <article class="blog-article">
        <h1>@article.Title</h1>
        <div class="meta">@article.PublishedOn?.ToLocalTime().ToString("MMMM dd, yyyy") • @article.Category</div>
        <div class="content">@renderedHtml</div>
    </article>
}
// wwwroot/js/infinite.js
window.initInfiniteScroll = (dotNetObjRef, sentinelId) => {
  const sentinel = document.getElementById(sentinelId);
  if (!sentinel) return;
  const observer = new IntersectionObserver(async (entries) => {
    for (const entry of entries) {
      if (entry.isIntersecting) {
        await dotNetObjRef.invokeMethodAsync('LoadPage');
      }
    }
  }, { rootMargin: '300px' });
  observer.observe(sentinel);
};
