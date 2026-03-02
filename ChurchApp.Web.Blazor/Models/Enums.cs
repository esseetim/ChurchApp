namespace ChurchApp.Web.Blazor.Models;

public enum DonationType
{
    GeneralOffering = 1,
    Tithe = 2,
    BuildingFund = 3
}

public enum DonationMethod
{
    Cash = 1,
    CashApp = 2,
    Zelle = 3,
    Check = 4,
    Card = 5,
    Other = 6
}

public enum DonationStatus
{
    Active = 1,
    Voided = 2
}

public enum SummaryPeriodType
{
    Day = 1,
    Month = 2,
    Quarter = 3,
    Year = 4,
    CustomRange = 5
}
