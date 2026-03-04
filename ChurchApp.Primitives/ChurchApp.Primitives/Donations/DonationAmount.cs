using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using ErrorOr;

namespace ChurchApp.Primitives.Donations;

[JsonConverter(typeof(DonationAmountJsonConverter))]
[TypeConverter(typeof(DonationAmountConverter))]
public readonly struct DonationAmount :
        IEquatable<DonationAmount>, 
        IComparable<DonationAmount>, 
        IComparable,
        ISpanFormattable,
        IUtf8SpanFormattable
{
    private readonly decimal _amount;
    
    public const decimal Minimum = 1;
    private DonationAmount(decimal amount) => _amount = amount;
    
    public static ErrorOr<DonationAmount> Create(decimal amount) => amount < Minimum
        ? Error.Validation(code: "DonationAmount.Negative", description: "Donation amount must be positive")
        : new DonationAmount(amount);
    
    public static implicit operator decimal(DonationAmount amount) => amount._amount;

    public override string ToString() => $"{nameof(_amount)}: {_amount}";

    public bool Equals(DonationAmount other) => _amount == other._amount;
    public override bool Equals(object? obj) => obj is DonationAmount other && Equals(other);
    public override int GetHashCode() => _amount.GetHashCode();

    public static bool operator ==(DonationAmount left, DonationAmount right) => left.Equals(right);
    public static bool operator !=(DonationAmount left, DonationAmount right) => !left.Equals(right);

    public int CompareTo(DonationAmount other) => _amount.CompareTo(other._amount);

    public int CompareTo(object? obj) => obj switch
    {
        null => 1,
        DonationAmount other => CompareTo(other),
        _ => throw new ArgumentException($"Object must be of type {nameof(DonationAmount)}")
    };

    public static bool operator <(DonationAmount left, DonationAmount right) => left.CompareTo(right) < 0;
    public static bool operator >(DonationAmount left, DonationAmount right) => left.CompareTo(right) > 0;
    public static bool operator <=(DonationAmount left, DonationAmount right) => left.CompareTo(right) <= 0;
    public static bool operator >=(DonationAmount left, DonationAmount right) => left.CompareTo(right) >= 0;
    
    public static readonly DonationAmount Zero = new(0);
    public static readonly DonationAmount One = new(1);
    public static readonly DonationAmount Five = new(5);
    public static readonly DonationAmount Ten = new(10);
    public static readonly DonationAmount Twenty = new(20);
    public static readonly DonationAmount Fifty = new(50);
    public static readonly DonationAmount Hundred = new(100);
    
    public static DonationAmount operator +(DonationAmount left, DonationAmount right) => new(left._amount + right._amount);
    public static DonationAmount operator -(DonationAmount left, DonationAmount right) => new(left._amount - right._amount);
    public static DonationAmount operator *(DonationAmount left, DonationAmount right) => new(left._amount * right._amount);
    public static DonationAmount operator /(DonationAmount left, DonationAmount right) => new(left._amount / right._amount);

    public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider) => _amount.ToString(format, provider);

    public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format,
        IFormatProvider? provider) =>
        _amount.TryFormat(destination, out charsWritten, format, provider);

    public bool TryFormat(Span<byte> destination, out int bytesWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format,
        IFormatProvider? provider) =>
        _amount.TryFormat(destination, out bytesWritten, format, provider);
}

/// <summary>
/// TypeConverter for DonationAmount to enable model binding in ASP.NET Core.
/// Implements bidirectional conversion between DonationAmount and primitive types.
/// </summary>
/// <remarks>
/// Following Anders Hejlsberg's guidance: "Type converters should be transparent bridges
/// between your domain types and the framework's type system."
/// 
/// This enables scenarios like:
/// - Query string parsing: ?amount=50.00
/// - Form data binding
/// - Route value constraints
/// - Default value providers
/// </remarks>
public sealed class DonationAmountConverter : TypeConverter
{
    /// <summary>
    /// Determines if this converter can convert from the specified source type.
    /// Supports: string, decimal, int, double, float
    /// </summary>
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) 
            || sourceType == typeof(decimal)
            || sourceType == typeof(int)
            || sourceType == typeof(double)
            || sourceType == typeof(float)
            || base.CanConvertFrom(context, sourceType);
    }

    /// <summary>
    /// Converts from source type to DonationAmount.
    /// Uses ErrorOr pattern internally but throws for compatibility with TypeConverter contract.
    /// </summary>
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        return value switch
        {
            // Direct decimal conversion
            decimal decimalValue => ConvertDecimalToAmount(decimalValue),
            
            // Integer types - safe upcast to decimal
            int intValue => ConvertDecimalToAmount(intValue),
            long longValue => ConvertDecimalToAmount(longValue),
            
            // Floating point - precision loss acceptable for donations
            double doubleValue => ConvertDecimalToAmount((decimal)doubleValue),
            float floatValue => ConvertDecimalToAmount((decimal)floatValue),
            
            // String parsing - most common in model binding
            string stringValue => ParseStringToAmount(stringValue, culture),
            
            _ => base.ConvertFrom(context, culture, value)
        };
    }

    /// <summary>
    /// Determines if this converter can convert to the specified destination type.
    /// Supports: string, decimal (for implicit operator)
    /// </summary>
    public override bool CanConvertTo(ITypeDescriptorContext? context, [NotNullWhen(true)] Type? destinationType)
    {
        return destinationType == typeof(string)
            || destinationType == typeof(decimal)
            || destinationType == typeof(DonationAmount)
            || base.CanConvertTo(context, destinationType);
    }

    /// <summary>
    /// Converts DonationAmount to a destination type.
    /// </summary>
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (value is not DonationAmount amount)
            return base.ConvertTo(context, culture, value, destinationType);

        if (destinationType == typeof(string))
        {
            // Format as currency for string conversion
            return ((decimal)amount).ToString("C", culture ?? CultureInfo.CurrentCulture);
        }

        if (destinationType == typeof(decimal))
        {
            // Use implicit operator
            return (decimal)amount;
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }

    /// <summary>
    /// Helper method to convert decimal to DonationAmount.
    /// Throws NotSupportedException if validation fails (TypeConverter contract requirement).
    /// </summary>
    private static DonationAmount ConvertDecimalToAmount(decimal value)
    {
        var result = DonationAmount.Create(value);
        
        if (result.IsError)
        {
            // TypeConverter contract requires throwing exceptions for invalid conversions
            // This integrates with ASP.NET Core's ModelState validation
            throw new NotSupportedException(
                $"Cannot convert {value} to {nameof(DonationAmount)}: {result.FirstError.Description}");
        }

        return result.Value;
    }

    /// <summary>
    /// Helper method to parse string to DonationAmount.
    /// Handles currency symbols and different number formats.
    /// </summary>
    private static DonationAmount ParseStringToAmount(string value, CultureInfo? culture)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new NotSupportedException(
                $"Cannot convert empty string to {nameof(DonationAmount)}");
        }

        culture ??= CultureInfo.CurrentCulture;

        // Try parsing as currency first (handles $ symbol)
        if (decimal.TryParse(value, NumberStyles.Currency, culture, out var currencyAmount))
        {
            return ConvertDecimalToAmount(currencyAmount);
        }

        // Fallback to standard number parsing
        if (decimal.TryParse(value, NumberStyles.Number, culture, out var numberAmount))
        {
            return ConvertDecimalToAmount(numberAmount);
        }

        throw new FormatException(
            $"Cannot parse '{value}' as {nameof(DonationAmount)}. Expected a valid number or currency format.");
    }
}

public sealed class DonationAmountJsonConverter : JsonConverter<DonationAmount>
{
    public override DonationAmount Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.Number when reader.TryGetDecimal(out decimal amount) => DonationAmount.Create(amount)
                .Match(onValue: value => value, onError: _ => DonationAmount.Zero),
            JsonTokenType.String when reader.TryGetDecimal(out decimal stringAmount) => DonationAmount
                .Create(stringAmount)
                .Match(onValue: value => value, onError: _ => DonationAmount.Zero),
            _ => throw new JsonException(
                $"Unexpected token parsing {nameof(DonationAmount)}. Expected Number or String, got {reader.TokenType}.")
        };

    public override void Write(Utf8JsonWriter writer, DonationAmount value, JsonSerializerOptions options) => 
        writer.WriteNumberValue(value);
}