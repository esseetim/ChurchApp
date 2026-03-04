using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using ErrorOr;

namespace ChurchApp.Primitives.Members;

/// <summary>
/// Represents a validated phone number in E.164 format.
/// Ensures consistent normalization and format validation across all layers.
/// </summary>
/// <remarks>
/// Design principles:
/// - Fail Fast: Invalid phone numbers cannot be constructed
/// - Normalization: Always stored in E.164 format (+[country code][number])
/// - Type Safety: Prevents primitive obsession antipattern
/// - Shared Kernel: Single source of truth for phone validation
/// 
/// E.164 format: +[1-3 digit country code][subscriber number]
/// Example: +12345678901 (US number)
/// Maximum 15 digits total per ITU-T E.164 standard
/// </remarks>
[JsonConverter(typeof(PhoneNumberJsonConverter))]
[TypeConverter(typeof(PhoneNumberConverter))]
public readonly struct PhoneNumber : IEquatable<PhoneNumber>
{
    private readonly string _value;
    
    /// <summary>
    /// Maximum length per E.164 standard (+ plus 15 digits)
    /// </summary>
    public const int MaxLength = 20;
    
    private const int MinDigits = 10;
    private const int MaxDigits = 15;
    
    private PhoneNumber(string value) => _value = value;
    
    /// <summary>
    /// Creates a validated PhoneNumber instance.
    /// Normalizes to E.164 format (+[country code][number]).
    /// </summary>
    /// <param name="phone">The phone number to validate (accepts various formats)</param>
    /// <returns>ErrorOr containing either the validated PhoneNumber or validation errors</returns>
    /// <remarks>
    /// Accepts input formats like:
    /// - (555) 123-4567
    /// - 555-123-4567
    /// - 5551234567
    /// - +15551234567
    /// 
    /// Always stores as: +15551234567 (E.164)
    /// </remarks>
    public static ErrorOr<PhoneNumber> Create(string? phone)
    {
        // Validation Step 1: Check for null/whitespace
        if (string.IsNullOrWhiteSpace(phone))
            return Error.Validation(
                code: "PhoneNumber.Empty", 
                description: "Phone number cannot be empty");
        
        // Normalization Step 1: Trim
        phone = phone.Trim();
        
        // Normalization Step 2: Extract digits only
        var digitsOnly = new string(phone.Where(char.IsDigit).ToArray());
        
        // Validation Step 2: Check digit count
        if (digitsOnly.Length < MinDigits || digitsOnly.Length > MaxDigits)
            return Error.Validation(
                code: "PhoneNumber.InvalidLength", 
                description: $"Phone number must contain {MinDigits}-{MaxDigits} digits");
        
        // Normalization Step 3: Convert to E.164 format
        // If 10 digits, assume US (+1)
        // Otherwise, assume an international format already includes country code
        var normalized = digitsOnly.Length == 10 
            ? $"+1{digitsOnly}"
            : $"+{digitsOnly}";
        
        return new PhoneNumber(normalized);
    }
    
    /// <summary>
    /// Implicit conversion to string for database storage and serialization.
    /// </summary>
    public static implicit operator string(PhoneNumber phone) => phone._value;
    
    /// <summary>
    /// Returns the phone number in E.164 format.
    /// </summary>
    public override string ToString() => _value;
    
    /// <summary>
    /// Formats the phone number for display (US format if applicable).
    /// </summary>
    /// <returns>
    /// US numbers: (555) 123-4567
    /// International: +44234567890
    /// </returns>
    public string ToDisplayFormat()
    {
        var span = _value.AsSpan();
        // US number: +1XXXXXXXXXX (12 chars)
        if (span.Length == 12 && span.StartsWith("+1", StringComparison.Ordinal))
        {
            // Format as (XXX) XXX-XXXX
            return $"({span.Slice(2, 3)}) {span.Slice(5, 3)}-{span.Slice(8, 4)}";
        }
        
        // International: Keep E.164 format
        return _value;
    }
    
    public bool Equals(PhoneNumber other) => 
        string.Equals(_value, other._value, StringComparison.Ordinal);
    
    public override bool Equals(object? obj) => 
        obj is PhoneNumber other && Equals(other);
    
    public override int GetHashCode() => _value.GetHashCode();
    
    public static bool operator ==(PhoneNumber left, PhoneNumber right) => left.Equals(right);
    public static bool operator !=(PhoneNumber left, PhoneNumber right) => !left.Equals(right);
}

/// <summary>
/// TypeConverter for PhoneNumber to enable ASP.NET Core model binding.
/// </summary>
public sealed class PhoneNumberConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        switch (value)
        {
            case string stringValue when string.IsNullOrWhiteSpace(stringValue):
                return null;
            case string stringValue:
            {
                var result = PhoneNumber.Create(stringValue);
            
                if (result.IsError)
                {
                    throw new NotSupportedException(
                        $"Cannot convert '{stringValue}' to {nameof(PhoneNumber)}: {result.FirstError.Description}");
                }
            
                return result.Value;
            }
            default:
                return base.ConvertFrom(context, culture, value);
        }
    }
    
    public override bool CanConvertTo(ITypeDescriptorContext? context, [NotNullWhen(true)] Type? destinationType) =>
        destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (value is PhoneNumber phone && destinationType == typeof(string))
            return (string)phone;
        
        return base.ConvertTo(context, culture, value, destinationType);
    }
}

/// <summary>
/// JSON converter for PhoneNumber to enable System.Text.Json serialization.
/// </summary>
public sealed class PhoneNumberJsonConverter : JsonConverter<PhoneNumber>
{
    public override PhoneNumber Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException(
                $"Unexpected token parsing {nameof(PhoneNumber)}. Expected String, got {reader.TokenType}.");
        }
        
        var value = reader.GetString();
        
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException($"Cannot parse empty string as {nameof(PhoneNumber)}");
        }
        
        var result = PhoneNumber.Create(value);
        
        if (result.IsError)
        {
            throw new JsonException(
                $"Invalid phone number: {result.FirstError.Description}");
        }
        
        return result.Value;
    }
    
    public override void Write(Utf8JsonWriter writer, PhoneNumber value, JsonSerializerOptions options) =>
        writer.WriteStringValue((string)value);
}

/// <summary>
/// JSON converter for nullable PhoneNumber.
/// </summary>
public sealed class NullablePhoneNumberJsonConverter : JsonConverter<PhoneNumber?>
{
    public override PhoneNumber? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException(
                $"Unexpected token parsing {nameof(PhoneNumber)}. Expected String or Null, got {reader.TokenType}.");
        }
        
        var value = reader.GetString();
        
        if (string.IsNullOrWhiteSpace(value))
            return null;
        
        var result = PhoneNumber.Create(value);
        
        if (result.IsError)
        {
            throw new JsonException(
                $"Invalid phone number: {result.FirstError.Description}");
        }
        
        return result.Value;
    }
    
    public override void Write(Utf8JsonWriter writer, PhoneNumber? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue((string)value.Value);
        else
            writer.WriteNullValue();
    }
}
