
namespace ChurchApp.Application.Features.Transactions.Classification;

/// <summary>
/// Orchestrates donation type classification using registered classifiers.
/// Follows Dependency Inversion Principle - depends on abstractions, not concrete classifiers.
/// </summary>
public sealed class DonationTypeClassificationService
{
    private readonly IReadOnlyList<IDonationTypeClassifier> _classifiers;

    public DonationTypeClassificationService(IEnumerable<IDonationTypeClassifier> classifiers)
    {
        // Sort by priority descending at construction time (performance optimization)
        _classifiers = classifiers
            .OrderByDescending(c => c.Priority)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Classifies a donation based on the memo field.
    /// Returns GeneralOffering as the default if no classifier matches.
    /// </summary>
    public DonationType Classify(string? memo)
    {
        if (string.IsNullOrWhiteSpace(memo))
            return DonationType.GeneralOffering;

        ReadOnlySpan<char> memoSpan = memo.AsSpan();
        
        // First matching classifier wins (ordered by priority)
        foreach (var classifier in _classifiers)
        {
            if (classifier.CanClassify(memoSpan))
                return classifier.GetDonationType();
        }
        
        return DonationType.GeneralOffering;
    }
}
