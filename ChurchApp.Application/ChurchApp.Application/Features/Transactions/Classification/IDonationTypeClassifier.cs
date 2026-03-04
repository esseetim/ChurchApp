
namespace ChurchApp.Application.Features.Transactions.Classification;

/// <summary>
/// Defines a strategy for classifying donation type from transaction memos.
/// Follows Strategy Pattern to support Open-Closed Principle.
/// </summary>
/// <remarks>
/// Each classifier is independently testable and can be added/removed
/// without modifying existing code - Uncle Bob would approve!
/// </remarks>
public interface IDonationTypeClassifier
{
    /// <summary>
    /// Determines if this classifier can handle the given memo
    /// </summary>
    bool CanClassify(ReadOnlySpan<char> memo);
    
    /// <summary>
    /// Returns the donation type for this classifier
    /// </summary>
    DonationType GetDonationType();
    
    /// <summary>
    /// Priority for this classifier. Higher values run first.
    /// Allows handling overlapping keywords (e.g., "Building Tithe" → Building wins if priority is higher)
    /// </summary>
    int Priority { get; }
}
