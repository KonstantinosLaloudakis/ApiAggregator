namespace ApiAggregator.Api.Models;

/// <summary>
/// News article from News API
/// </summary>
public class NewsArticle
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
}

/// <summary>
/// Raw response from News API
/// </summary>
public class NewsApiResponse
{
    public string? Status { get; set; }
    public int TotalResults { get; set; }
    public List<NewsApiArticle>? Articles { get; set; }
}

public class NewsApiArticle
{
    public NewsApiSource? Source { get; set; }
    public string? Author { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Url { get; set; }
    public string? UrlToImage { get; set; }
    public DateTime? PublishedAt { get; set; }
}

public class NewsApiSource
{
    public string? Id { get; set; }
    public string? Name { get; set; }
}
