namespace ApiAggregator.Api.Models;

/// <summary>
/// GitHub repository data
/// </summary>
public class GitHubRepository
{
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public int Stars { get; set; }
    public int Forks { get; set; }
    public int OpenIssues { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Raw response from GitHub Search API
/// </summary>
public class GitHubSearchResponse
{
    public int Total_count { get; set; }
    public bool Incomplete_results { get; set; }
    public List<GitHubRepoItem>? Items { get; set; }
}

public class GitHubRepoItem
{
    public string? Name { get; set; }
    public string? Full_name { get; set; }
    public string? Description { get; set; }
    public string? Html_url { get; set; }
    public string? Language { get; set; }
    public int Stargazers_count { get; set; }
    public int Forks_count { get; set; }
    public int Open_issues_count { get; set; }
    public DateTime? Created_at { get; set; }
    public DateTime? Updated_at { get; set; }
}
