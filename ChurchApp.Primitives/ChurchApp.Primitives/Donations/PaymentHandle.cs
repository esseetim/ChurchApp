using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using ErrorOr;

namespace ChurchApp.Primitives.Donations;

/// <summary>
/// Represents a validated payment handle (CashApp $cashtag, Zelle email/phone, etc.).
/// Applies method-specific validation rules to ensure handles are properly formatted.
/// </summary>
/// <remarks>
/// Design principles:
/// - Context-Aware Validation: Different rules for different payment methods
/// - Fail Fast: Invalid handles cannot be constructed
/// - Type Safety: Prevents storing invalid payment handles
/// - Shared Kernel: Single source of truth for handle validation
/// 
/// Validation rules:
/// - CashApp: Must start with $, 3-20 alphanumeric characters
/// - Zelle: Valid email OR phone number
/// - Other methods: Basic validation (non-empty, reasonable length)
/// </remarks>
[JsonConverter(typeof(PaymentHandleJsonConverter))]
[TypeConverter(typeof(PaymentHandleConverter))]
public readonly struct PaymentHandle : IEquatable<PaymentHandle>
{
    private readonly string _value;
    
    public const int MaxLength = 100;
    
    private PaymentHandle(string value) => _value = value;
    
    /// <summary>
    /// Creates a validated PaymentHandle instance with method-specific validation.
    /// </summary>
    /// <param name="handle">The payment handle to validate</param>
    /// <param name="method">The donation method (determines validation rules)</param>
    /// <returns>ErrorOr containing either the validated PaymentHandle or validation errors</returns>
    public static ErrorOr<PaymentHandle> Create(string? handle, DonationMethod method)
    {
        // Validation Step 1: Check for null/whitespace
        if (string.IsNullOrWhiteSpace(handle))
            return Error.Validation(
                code: "PaymentHandle.Empty", 
                description: "Payment handle cannot be empty");
        
        // Normalization: Trim
        handle = handle.Trim();
        
        // Validation Step 2: Check length
        if (handle.Length > MaxLength)
            return Error.Validation(
                code: "PaymentHandle.TooLong", 
                description: $"Payment handle cannot exceed {MaxLength} characters");
        
        // Validation Step 3: Method-specific validation
        return method switch
        {
            DonationMethod.CashApp => ValidateCashTag(handle),
            DonationMethod.Zelle => ValidateZelleHandle(handle),
            DonationMethod.Cash => Error.Validation(
                code: "PaymentHandle.CashNotAllowed",
                description: "Cash donations do not have payment handles"),
            DonationMethod.Check => ValidateCheckHandle(handle),
            DonationMethod.Card => ValidateCardHandle(handle),
            DonationMethod.Other => ValidateOtherHandle(handle),
            _ => Error.Validation(
                code: "PaymentHandle.UnknownMethod",
                description: $"Unknown donation method: {method}")
        };
    }
    
    /// <summary>
    /// Validates a CashApp $cashtag.
    /// Must start with $, followed by 3-20 alphanumeric characters.
    /// </summary>
    /// <remarks>
    /// Valid examples: $johnsmith, $JohnSmith123, $abc
    /// Invalid examples: johnsmith (missing $), $ab (too short), $john@smith (special chars)
    /// </remarks>
    private static ErrorOr<PaymentHandle> ValidateCashTag(string handle)
    {
        // Must start with $
        if (!handle.StartsWith('$'))
            return Error.Validation(
                code: "PaymentHandle.CashApp.MissingDollarSign", 
                description: "CashApp handle must start with $");
        
        // Extract tag (everything after $)
        var tag = handle[1..];
        
        // Check length (3-20 chars)
        if (tag.Length < 3 || tag.Length > 20)
            return Error.Validation(
                code: "PaymentHandle.CashApp.InvalidLength", 
                description: "CashApp tag must be 3-20 characters (excluding $)");
        
        // Check alphanumeric only
        if (!tag.All(c => char.IsLetterOrDigit(c)))
            return Error.Validation(
                code: "PaymentHandle.CashApp.InvalidCharacters", 
                description: "CashApp tag must contain only letters and numbers");
        
        // Normalize to lowercase for consistency
        return new PaymentHandle(handle.ToLowerInvariant());
    }
    
    /// <summary>
    /// Validates a Zelle handle (email or phone number).
    /// </summary>
    /// <remarks>
    /// Zelle accepts either:
    /// - Valid email address
    /// - Valid phone number
    /// </remarks>
    private static ErrorOr<PaymentHandle> ValidateZelleHandle(string handle)
    {
        // Try email validation first
        var emailResult = Members.EmailAddress.Create(handle);
        if (!emailResult.IsError)
            return new PaymentHandle(handle.ToLowerInvariant());
        
        // Try phone validation
        var phoneResult = Members.PhoneNumber.Create(handle);
        if (!phoneResult.IsError)
            return new PaymentHandle(phoneResult.Value); // Already in E.164 format
        
        // Neither worked - return error
        return Error.Validation(
            code: "PaymentHandle.Zelle.InvalidFormat", 
            description: "Zelle handle must be a valid email address or phone number");
    }
    
    /// <summary>
    /// Validates a check-related handle (e.g., bank routing number, account last 4 digits).
    /// </summary>
    private static ErrorOr<PaymentHandle> ValidateCheckHandle(string handle)
    {
        // Basic validation: non-empty, reasonable length
        if (handle.Length < 2)
            return Error.Validation(
                code: "PaymentHandle.Check.TooShort", 
                description: "Check handle must be at least 2 characters");
        
        return new PaymentHandle(handle);
    }
    
    /// <summary>
    /// Validates a card-related handle (e.g., last 4 digits, cardholder name).
    /// </summary>
    private static ErrorOr<PaymentHandle> ValidateCardHandle(string handle)
    {
        // Basic validation: non-empty, reasonable length
        if (handle.Length < 2)
            return Error.Validation(
                code: "PaymentHandle.Card.TooShort", 
                description: "Card handle must be at least 2 characters");
        
        return new PaymentHandle(handle);
    }
    
    /// <summary>
    /// Validates a handle for "Other" payment methods.
    /// Most permissive validation.
    /// </summary>
    private static ErrorOr<PaymentHandle> ValidateOtherHandle(string handle)
    {
        // Minimal validation: just non-empty (already checked)
        return new PaymentHandle(handle);
    }
    
    /// <summary>
    /// Implicit conversion to string for database storage and serialization.
    /// </summary>
    public static implicit operator string(PaymentHandle handle) => handle._value;
    
    /// <summary>
    /// Returns the payment handle value.
    /// </summary>
    public override string ToString() => _value;
    
    public bool Equals(PaymentHandle other) => 
        string.Equals(_value, other._value, StringComparison.OrdinalIgnoreCase);
    
    public override bool Equals(object? obj) => 
        obj is PaymentHandle other && Equals(other);
    
    public override int GetHashCode() => 
        StringComparer.OrdinalIgnoreCase.GetHashCode(_value);
    
    public static bool operator ==(PaymentHandle left, PaymentHandle right) => left.Equals(right);
    public static bool operator !=(PaymentHandle left, PaymentHandle right) => !left.Equals(right);
}

/// <summary>
/// TypeConverter for PaymentHandle to enable ASP.NET Core model binding.
/// Note: Cannot validate without knowing the DonationMethod, so basic validation only.
/// Full validation happens in Create method with method parameter.
/// </summary>
public sealed class PaymentHandleConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string stringValue)
        {
            if (string.IsNullOrWhiteSpace(stringValue))
                return null;
            
            // TypeConverter doesn't have access to DonationMethod context
            // So we do minimal validation here and rely on domain validation
            if (stringValue.Length > PaymentHandle.MaxLength)
            {
                throw new NotSupportedException(
                    $"Cannot convert '{stringValue}' to {nameof(PaymentHandle)}: exceeds maximum length");
            }
            
            // Create with "Other" method for most permissive validation
            var result = PaymentHandle.Create(stringValue, DonationMethod.Other);
            
            if (result.IsError)
            {
                throw new NotSupportedException(
                    $"Cannot convert '{stringValue}' to {nameof(PaymentHandle)}: {result.FirstError.Description}");
            }
            
            return result.Value;
        }
        
        return base.ConvertFrom(context, culture, value);
    }
    
    public override bool CanConvertTo(ITypeDescriptorContext? context, [NotNullWhen(true)] Type? destinationType) =>
        destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (value is PaymentHandle handle && destinationType == typeof(string))
            return (string)handle;
        
        return base.ConvertTo(context, culture, value, destinationType);
    }
}

/// <summary>
/// JSON converter for PaymentHandle to enable System.Text.Json serialization.
/// </summary>
public sealed class PaymentHandleJsonConverter : JsonConverter<PaymentHandle>
{
    public override PaymentHandle Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException(
                $"Unexpected token parsing {nameof(PaymentHandle)}. Expected String, got {reader.TokenType}.");
        }
        
        var value = reader.GetString();
        
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException($"Cannot parse empty string as {nameof(PaymentHandle)}");
        }
        
        // JSON deserialization doesn't have DonationMethod context
        // Use "Other" for most permissive validation
        var result = PaymentHandle.Create(value, DonationMethod.Other);
        
        if (result.IsError)
        {
            throw new JsonException(
                $"Invalid payment handle: {result.FirstError.Description}");
        }
        
        return result.Value;
    }
    
    public override void Write(Utf8JsonWriter writer, PaymentHandle value, JsonSerializerOptions options) =>
        writer.WriteStringValue((string)value);
}

/// <summary>
/// JSON converter for nullable PaymentHandle.
/// </summary>
public sealed class NullablePaymentHandleJsonConverter : JsonConverter<PaymentHandle?>
{
    public override PaymentHandle? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException(
                $"Unexpected token parsing {nameof(PaymentHandle)}. Expected String or Null, got {reader.TokenType}.");
        }
        
        var value = reader.GetString();
        
        if (string.IsNullOrWhiteSpace(value))
            return null;
        
        // JSON deserialization doesn't have DonationMethod context
        // Use "Other" for most permissive validation
        var result = PaymentHandle.Create(value, DonationMethod.Other);
        
        if (result.IsError)
        {
            throw new JsonException(
                $"Invalid payment handle: {result.FirstError.Description}");
        }
        
        return result.Value;
    }
    
    public override void Write(Utf8JsonWriter writer, PaymentHandle? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue((string)value.Value);
        else
            writer.WriteNullValue();
    }
}
