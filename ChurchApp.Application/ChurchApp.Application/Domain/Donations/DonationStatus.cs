namespace ChurchApp.Application.Domain.Donations;

public enum DonationStatus
{
    Unspecified = 0, // Sentinel value for EF Core (Anders Hejlsberg's explicit configuration)
    Active = 1,
    Voided = 2
}
