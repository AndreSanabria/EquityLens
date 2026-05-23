using EquityLens.Api.DTOs;
using EquityLens.Api.Models;

namespace EquityLens.Api.Services;

public class NewsRankingService : INewsRankingService
{
    private static readonly IReadOnlyDictionary<string, string[]> KeywordMap =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Earnings"] = ["earnings", "revenue", "profit", "guidance"],
            ["Legal"] = ["lawsuit", "investigation", "doj", "sec", "regulatory"],
            ["Product"] = ["launch", "product", "chip", "ai", "platform"],
            ["Leadership"] = ["ceo", "cfo", "executive", "resigns", "leadership"],
            ["M&A"] = ["acquisition", "merger", "stake"],
            ["Debt"] = ["debt", "bonds", "credit", "downgrade"],
            ["Layoffs"] = ["layoffs", "restructuring", "cuts"]
        };

    private static readonly IReadOnlyDictionary<string, int> CategoryBaseScores =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Earnings"] = 30,
            ["Legal"] = 30,
            ["M&A"] = 25,
            ["Debt"] = 25,
            ["Layoffs"] = 22,
            ["Product"] = 15,
            ["Leadership"] = 12
        };

    private static readonly HashSet<string> HighQualitySources =
    [
        "Reuters",
        "Bloomberg",
        "The Wall Street Journal",
        "CNBC"
    ];

    public IReadOnlyList<RankedNewsItemDto> Rank(IReadOnlyList<NewsArticle> articles)
    {
        return articles
            .Select(article =>
            {
                var category = Categorize(article);
                var recencyScore = GetRecencyScore(article.PublishedAt);
                var qualityScore = HighQualitySources.Contains(article.Source) ? 10 : 0;
                var baseScore = CategoryBaseScores.GetValueOrDefault(category, 8);
                var genericPenalty = category.Equals("General", StringComparison.OrdinalIgnoreCase) ? 10 : 0;

                return new RankedNewsItemDto(
                    Title: article.Title,
                    Source: article.Source,
                    Url: article.Url,
                    IsDirectArticleUrl: article.IsDirectArticleUrl,
                    PublishedAt: article.PublishedAt,
                    Category: category,
                    RelevanceScore: Math.Clamp(baseScore + recencyScore + qualityScore - genericPenalty, 0, 100));
            })
            .OrderByDescending(item => item.RelevanceScore)
            .ThenByDescending(item => item.PublishedAt)
            .ToList();
    }

    public int CalculateNewsRiskScore(IReadOnlyList<RankedNewsItemDto> rankedNews)
    {
        if (rankedNews.Count == 0)
        {
            return 0;
        }

        var weightedScores = rankedNews
            .Take(5)
            .Select(item => item.RelevanceScore * GetRiskWeight(item.Category))
            .ToList();

        return Math.Clamp((int)Math.Round(weightedScores.Average()), 0, 100);
    }

    private static string Categorize(NewsArticle article)
    {
        foreach (var pair in KeywordMap)
        {
            if (pair.Value.Any(keyword => article.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                return pair.Key;
            }
        }

        return string.IsNullOrWhiteSpace(article.Category) ? "General" : article.Category;
    }

    private static int GetRecencyScore(DateTime publishedAt)
    {
        var age = (DateTime.UtcNow.Date - publishedAt.Date).TotalDays;

        if (age <= 7)
        {
            return 20;
        }

        return age <= 30 ? 10 : 0;
    }

    private static double GetRiskWeight(string category) =>
        category switch
        {
            "Legal" => 1.0,
            "Debt" => 0.9,
            "Layoffs" => 0.8,
            "Leadership" => 0.6,
            "Earnings" => 0.5,
            "M&A" => 0.45,
            "Product" => 0.35,
            _ => 0.25
        };
}
