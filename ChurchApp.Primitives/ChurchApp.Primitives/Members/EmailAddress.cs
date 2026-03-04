using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ErrorOr;

namespace ChurchApp.Primitives.Members;

/// <summary>
/// Represents a validated email address following RFC 5321 guidelines.
/// Ensures consistent normalization (lowercase, trimmed) and format validation across all layers.
/// </summary>
/// <remarks>
/// Design principles:
/// - Fail Fast: Invalid emails cannot be constructed
/// - Normalization: Always stored lowercase and trimmed
/// - Type Safety: Prevents primitive obsession anti-pattern
/// - Shared Kernel: Single source of truth for email validation
/// 
/// Following Kent Beck's guidance: "Make it fail, make it work, make it fast."
/// </remarks>
[JsonConverter(typeof(EmailAddressJsonConverter))]
[TypeConverter(typeof(EmailAddressConverter))]
public readonly partial struct EmailAddress : IEquatable<EmailAddress>
{
    private readonly string _value;
    
    /// <summary>
    /// Maximum length per RFC 5321 (64 chars local + @ + 255 chars domain = 320, but 254 is practical limit)
    /// </summary>
    public const int MaxLength = 254;
    
    private EmailAddress(string value) => _value = value;
    
    /// <summary>
    /// Creates a validated EmailAddress instance.
    /// Applies normalization (trim, lowercase) and validates format.
    /// </summary>
    /// <param name="email">The email address to validate</param>
    /// <returns>ErrorOr containing either the validated EmailAddress or validation errors</returns>
    public static ErrorOr<EmailAddress> Create(string? email)
    {
        // Validation Step 1: Check for null/whitespace
        if (string.IsNullOrWhiteSpace(email))
            return Error.Validation(
                code: "EmailAddress.Empty", 
                description: "Email address cannot be empty");
        
        // Normalization: Always store lowercase and trimmed
        email = email.Trim().ToLowerInvariant();
        
        // Validation Step 2: Check length
        if (email.Length > MaxLength)
            return Error.Validation(
                code: "EmailAddress.TooLong", 
                description: $"Email address cannot exceed {MaxLength} characters");
        
        // Validation Step 3: Format validation using regex
        if (!EmailRegex().IsMatch(email))
            return Error.Validation(
                code: "EmailAddress.InvalidFormat", 
                description: "Email address format is invalid");
        
        return new EmailAddress(email);
    }
    
    /// <summary>
    /// Email validation regex (basic but robust for most use cases).
    /// Compiled for performance.
    /// </summary>
    /// <remarks>
    /// Pattern explanation:
    /// - ^[^@\s]+ : One or more non-@ non-whitespace chars (local part)
    /// - @ : Literal @ symbol
    /// - [^@\s]+ : One or more non-@ non-whitespace chars (domain)
    /// - \. : Literal dot
    /// - [^@\s]+$ : One or more non-@ non-whitespace chars (TLD)
    /// </remarks>
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();
    
    /// <summary>
    /// Implicit conversion to string for database storage and serialization.
    /// </summary>
    public static implicit operator string(EmailAddress email) => email._value;
    
    /// <summary>
    /// Returns the email address value.
    /// </summary>
    public override string ToString() => _value;
    
    public bool Equals(EmailAddress other) => 
        string.Equals(_value, other._value, StringComparison.OrdinalIgnoreCase);
    
    public override bool Equals(object? obj) => 
        obj is EmailAddress other && Equals(other);
    
    public override int GetHashCode() => 
        StringComparer.OrdinalIgnoreCase.GetHashCode(_value);
    
    public static bool operator ==(EmailAddress left, EmailAddress right) => left.Equals(right);
    public static bool operator !=(EmailAddress left, EmailAddress right) => !left.Equals(right);
}

/// <summary>
/// TypeConverter for EmailAddress to enable ASP.NET Core model binding.
/// Enables binding from query strings, route values, and form data.
/// </summary>
/// <remarks>
/// Enables scenarios like:
/// - Query strings: ?email=user@example.com
/// - Route parameters: /members/{email}
/// - Form data binding
/// </remarks>
public sealed class EmailAddressConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string stringValue)
        {
            if (string.IsNullOrWhiteSpace(stringValue))
                return null;
            
            var result = EmailAddress.Create(stringValue);
            
            if (result.IsError)
            {
                throw new NotSupportedException(
                    $"Cannot convert '{stringValue}' to {nameof(EmailAddress)}: {result.FirstError.Description}");
            }
            
            return result.Value;
        }
        
        return base.ConvertFrom(context, culture, value);
    }
    
    public override bool CanConvertTo(ITypeDescriptorContext? context, [NotNullWhen(true)] Type? destinationType) =>
        destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (value is EmailAddress email && destinationType == typeof(string))
            return (string)email;
        
        return base.ConvertTo(context, culture, value, destinationType);
    }
}

/// <summary>
/// JSON converter for EmailAddress to enable System.Text.Json serialization.
/// </summary>
public sealed class EmailAddressJsonConverter : JsonConverter<EmailAddress>
{
    public override EmailAddress Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException(
                $"Unexpected token parsing {nameof(EmailAddress)}. Expected String, got {reader.TokenType}.");
        }
        
        var value = reader.GetString();
        
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException($"Cannot parse empty string as {nameof(EmailAddress)}");
        }
        
        var result = EmailAddress.Create(value);
        
        if (result.IsError)
        {
            throw new JsonException(
                $"Invalid email address: {result.FirstError.Description}");
        }
        
        return result.Value;
    }
    
    public override void Write(Utf8JsonWriter writer, EmailAddress value, JsonSerializerOptions options) =>
        writer.WriteStringValue((string)value);
}

/// <summary>
/// JSON converter for nullable EmailAddress.
/// </summary>
public sealed class NullableEmailAddressJsonConverter : JsonConverter<EmailAddress?>
{
    public override EmailAddress? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException(
                $"Unexpected token parsing {nameof(EmailAddress)}. Expected String or Null, got {reader.TokenType}.");
        }
        
        var value = reader.GetString();
        
        if (string.IsNullOrWhiteSpace(value))
            return null;
        
        var result = EmailAddress.Create(value);
        
        if (result.IsError)
        {
            throw new JsonException(
                $"Invalid email address: {result.FirstError.Description}");
        }
        
        return result.Value;
    }
    
    public override void Write(Utf8JsonWriter writer, EmailAddress? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue((string)value.Value);
        else
            writer.WriteNullValue();
    }
}
