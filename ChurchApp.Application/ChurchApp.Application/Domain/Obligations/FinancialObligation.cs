using System.Diagnostics.CodeAnalysis;
using ChurchApp.Application.Domain.Members;

namespace ChurchApp.Application.Domain.Obligations;

/// <summary>
/// Represents a financial commitment made by a member (pledge or due).
/// </summary>
public abstract class FinancialObligation
{
    private protected FinancialObligation()
    {
    }

    [SetsRequiredMembers]
    protected FinancialObligation(
        Guid memberId, 
        ObligationType type, 
        string title, 
        decimal totalAmount, 
        DateOnly startDate, 
        DateOnly dueDate, 
        ObligationStatus status)
    {
        Id = Guid.CreateVersion7();
        MemberId = memberId;
        Type = type;
        Title = title;
        TotalAmount = totalAmount;
        StartDate = startDate;
        DueDate = dueDate;
        Status = status;
    }

    public Guid Id { get; init; }
    public Guid MemberId { get; private set; }
    public Member Member { get; private set; } = null!;

    public ObligationType Type { get; private set; }
    
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

        return type switch
        {
            ObligationType.FundraisingPledge => new Pledge(memberId, title, totalAmount, startDate, dueDate, ObligationStatus.Active),
            ObligationType.Dues => new Dues(memberId, title, totalAmount, startDate, dueDate, ObligationStatus.Active),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
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

public sealed class Pledge : FinancialObligation
{
    [SetsRequiredMembers]
    public Pledge(
        Guid memberId, 
        string title, 
        decimal totalAmount, 
        DateOnly startDate, 
        DateOnly dueDate, 
        ObligationStatus status) : base(memberId, ObligationType.FundraisingPledge, title, totalAmount, startDate, dueDate, status)
    {
        
    }
    
    private Pledge() { }
}

public sealed class Dues : FinancialObligation
{
    [SetsRequiredMembers]
    public Dues(
        Guid memberId, 
        string title, 
        decimal totalAmount, 
        DateOnly startDate, 
        DateOnly dueDate, 
        ObligationStatus status) : base(memberId, ObligationType.Dues, title, totalAmount, startDate, dueDate, status)
    {
        
    }
    
    private Dues() { }   
}