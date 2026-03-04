
namespace ChurchApp.Application.Features.Transactions.Classification;

/// <summary>
/// Classifies donations with tithe-related keywords.
/// </summary>
public sealed class TitheClassifier : IDonationTypeClassifier
{
    private static readonly string[] Keywords = ["tithe", "tenth", "10%"];
    
    public bool CanClassify(ReadOnlySpan<char> memo)
    {
        // Performance: Use Span<char> to avoid string allocations
        foreach (var keyword in Keywords)
        {
            if (memo.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
    
    public DonationType GetDonationType() => DonationType.Tithe;
    
    public int Priority => 100;
}
