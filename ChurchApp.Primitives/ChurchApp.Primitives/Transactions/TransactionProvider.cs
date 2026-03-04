namespace ChurchApp.Primitives.Transactions;

/// <summary>
/// Represents the external payment provider for a transaction.
/// </summary>
public enum TransactionProvider
{
    /// <summary>
    /// CashApp (Square) payment provider.
    /// </summary>
    CashApp = 1,
    
    /// <summary>
    /// Zelle payment provider.
    /// </summary>
    Zelle = 2
}
