using System.Linq.Expressions;
using ChurchApp.Primitives.Donations;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ChurchApp.Application.Infrastructure.ValueConverters;

internal sealed class DonationAmountValueConverter() : ValueConverter<DonationAmount, decimal>(
    amount => amount, 
    value => SFromProvider(value))
{
    private static readonly Func<decimal, DonationAmount> SFromProvider = value => DonationAmount.Create(value).Match(
        onValue: amount => amount,
        onError: _ => DonationAmount.Zero);
    
    public override Expression<Func<DonationAmount, decimal>> ConvertToProviderExpression => amount => amount;

    public override Expression<Func<decimal, DonationAmount>> ConvertFromProviderExpression => value => SFromProvider(value);
}

internal sealed class DonationAmountValueComparer() : ValueComparer<DonationAmount>(
    equalsExpression: (left, right) => left == right,
    hashCodeExpression: amount => amount.GetHashCode(),
    snapshotExpression: amount => amount);