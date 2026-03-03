using ChurchApp.Application.Domain.Donations;
using ChurchApp.Application.Domain.Members;

namespace ChurchApp.Application.Domain.Obligations;

/// <summary>
/// Represents a financial commitment made by a member (pledge or due).
/// </summary>
public sealed class FinancialObligation
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;

    public ObligationType Type { get; set; }
    
    /// <summary>
    /// The descriptive title for this obligation (e.g., "2026 Men's Club Dues", "New Roof Campaign").
    /// </summary>
    public required string Title { get; set; }
    
    /// <summary>
    /// The total amount committed.
    /// </summary>
    public decimal TotalAmount { get; set; }
    
    public DateOnly StartDate { get; set; }
    public DateOnly DueDate { get; set; }
    public ObligationStatus Status { get; set; }

    /// <summary>
    /// All payments made against this obligation.
    /// </summary>
    public ICollection<Donation> Payments { get; set; } = [];

    /// <summary>
    /// Calculates the total amount paid from active donations linked to this obligation.
    /// </summary>
    public decimal AmountPaid => Payments
        .Where(p => p.Status == DonationStatus.Active)
        .Sum(p => p.Amount);

    /// <summary>
    /// Calculates the remaining balance on this obligation.
    /// </summary>
    public decimal BalanceRemaining => Math.Max(0, TotalAmount - AmountPaid);

    /// <summary>
    /// Creates a new financial obligation.
    /// </summary>
    public static FinancialObligation Create(
        Guid memberId,
        ObligationType type,
        string title,
        decimal totalAmount,
        DateOnly startDate,
        DateOnly dueDate)
    {
        ArgumentNullException.ThrowIfNull(title);
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be empty.", nameof(title));
        }
        if (totalAmount <= 0)
        {
            throw new ArgumentException("Total amount must be greater than zero.", nameof(totalAmount));
        }
        if (dueDate < startDate)
        {
            throw new ArgumentException("Due date cannot be before start date.", nameof(dueDate));
        }

        return new FinancialObligation
        {
            Id = Guid.NewGuid(),
            MemberId = memberId,
            Type = type,
            Title = title.Trim(),
            TotalAmount = totalAmount,
            StartDate = startDate,
            DueDate = dueDate,
            Status = ObligationStatus.Active
        };
    }

    /// <summary>
    /// Updates the obligation details.
    /// </summary>
    public void Update(string title, decimal totalAmount, DateOnly dueDate)
    {
        ArgumentNullException.ThrowIfNull(title);
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be empty.", nameof(title));
        }
        if (totalAmount <= 0)
        {
            throw new ArgumentException("Total amount must be greater than zero.", nameof(totalAmount));
        }
        if (dueDate < StartDate)
        {
            throw new ArgumentException("Due date cannot be before start date.", nameof(dueDate));
        }

        Title = title.Trim();
        TotalAmount = totalAmount;
        DueDate = dueDate;
    }

    /// <summary>
    /// Cancels the obligation.
    /// </summary>
    public void Cancel()
    {
        if (Status == ObligationStatus.Cancelled)
        {
            return;
        }

        Status = ObligationStatus.Cancelled;
    }

    /// <summary>
    /// Marks the obligation as fulfilled if the amount paid meets or exceeds the total.
    /// </summary>
    public void CheckFulfillment()
    {
        if (Status == ObligationStatus.Active && AmountPaid >= TotalAmount)
        {
            Status = ObligationStatus.Fulfilled;
        }
    }
}
