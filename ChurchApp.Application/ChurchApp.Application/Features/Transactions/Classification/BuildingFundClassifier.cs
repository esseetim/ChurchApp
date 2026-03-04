
namespace ChurchApp.Application.Features.Transactions.Classification;

/// <summary>
/// Classifies donations for building fund campaigns.
/// </summary>
public sealed class BuildingFundClassifier : IDonationTypeClassifier
{
    private static readonly string[] Keywords = 
    [
        "building",
        "construction",
        "renovation",
        "expansion",
        "facility"
    ];
    
    public bool CanClassify(ReadOnlySpan<char> memo)
    {
        foreach (var keyword in Keywords)
        {
            if (memo.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
    
    public DonationType GetDonationType() => DonationType.BuildingFund;
    
    public int Priority => 90;
}
