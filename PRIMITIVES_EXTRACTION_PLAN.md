# Primitives Library Extraction Plan

## 🎯 Goal
Move shared types to `ChurchApp.Primitives` to ensure:
- **Consistent validation** across all layers (Application → API → UI)
- **Type safety** instead of primitive obsession
- **Reusability** without circular dependencies
- **Single source of truth** for domain rules

---

## 📊 Candidate Types Analysis

### ✅ Already Extracted
- `DonationAmount` - Decimal wrapper with validation

---

## 🔴 HIGH PRIORITY - Move Immediately

These types are used across ALL layers and benefit most from extraction:

### 1. Enums (Simple but Critical)

#### `DonationType`
```csharp
// Location: Application/Domain/Donations/DonationType.cs
// Usage: API contracts, UI dropdowns, database mapping
// Why extract: UI needs display names, validation, serialization

public enum DonationType
{
    GeneralOffering = 1,
    Tithe = 2,
    BuildingFund = 3,
    PledgePayment = 4,
    ClubDuePayment = 5
}
```

**Benefits of extraction:**
- UI can use for dropdowns without referencing Application layer
- API contracts become self-documenting
- Validation rules centralized

#### `DonationMethod`
```csharp
// Same rationale as DonationType
public enum DonationMethod
{
    Cash = 1,
    CashApp = 2,
    Zelle = 3,
    Check = 4,
    Card = 5,
    Other = 6
}
```

#### `DonationStatus`
```csharp
public enum DonationStatus
{
    Active = 1,
    Voided = 2
}
```

#### `ObligationType`
```csharp
public enum ObligationType
{
    FundraisingPledge = 1,
    ClubDue = 2
}
```

#### `ObligationStatus`
```csharp
public enum ObligationStatus
{
    Active = 1,
    Fulfilled = 2,
    Cancelled = 3
}
```

#### `TransactionProvider`
```csharp
public enum TransactionProvider
{
    CashApp = 1,
    Zelle = 2
}
```

#### `RawTransactionStatus`
```csharp
public enum RawTransactionStatus
{
    Pending = 1,
    Resolved = 2,
    Unmatched = 3,
    Ignored = 4
}
```

---

### 2. Value Objects (High Impact)

#### `EmailAddress`
**Current state:** `string? Email` in Member
**Problem:** No validation, inconsistent format checks
**Solution:**
```csharp
[TypeConverter(typeof(EmailAddressConverter))]
[JsonConverter(typeof(EmailAddressJsonConverter))]
public readonly struct EmailAddress : IEquatable<EmailAddress>
{
    private readonly string _value;
    
    public const int MaxLength = 254; // RFC 5321
    
    private EmailAddress(string value) => _value = value;
    
    public static ErrorOr<EmailAddress> Create(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Error.Validation("EmailAddress.Empty", "Email cannot be empty");
        
        email = email.Trim().ToLowerInvariant(); // Normalize
        
        if (email.Length > MaxLength)
            return Error.Validation("EmailAddress.TooLong", $"Email cannot exceed {MaxLength} characters");
        
        // Basic regex (adjust as needed)
        if (!EmailRegex().IsMatch(email))
            return Error.Validation("EmailAddress.Invalid", "Email format is invalid");
        
        return new EmailAddress(email);
    }
    
    public static implicit operator string(EmailAddress email) => email._value;
    
    public override string ToString() => _value;
    
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();
}
```

**Benefits:**
- ✅ Validation centralized
- ✅ Normalization (lowercase, trim) consistent
- ✅ Model binding in API
- ✅ No invalid emails in database
- ✅ UI can validate before submission

#### `PhoneNumber`
**Current state:** `string? PhoneNumber` in Member
**Problem:** No format validation, inconsistent storage
**Solution:**
```csharp
[TypeConverter(typeof(PhoneNumberConverter))]
[JsonConverter(typeof(PhoneNumberJsonConverter))]
public readonly struct PhoneNumber : IEquatable<PhoneNumber>
{
    private readonly string _value;
    
    public const int MaxLength = 20; // E.164 format
    
    private PhoneNumber(string value) => _value = value;
    
    public static ErrorOr<PhoneNumber> Create(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return Error.Validation("PhoneNumber.Empty", "Phone number cannot be empty");
        
        // Remove formatting characters
        phone = phone.Trim();
        var digitsOnly = new string(phone.Where(char.IsDigit).ToArray());
        
        if (digitsOnly.Length < 10 || digitsOnly.Length > 15)
            return Error.Validation("PhoneNumber.Invalid", "Phone must be 10-15 digits");
        
        // Store in E.164 format: +1234567890
        var normalized = digitsOnly.Length == 10 
            ? $"+1{digitsOnly}"  // Assume US
            : $"+{digitsOnly}";
        
        return new PhoneNumber(normalized);
    }
    
    public static implicit operator string(PhoneNumber phone) => phone._value;
    
    public string ToDisplayFormat() => 
        _value.Length == 12 && _value.StartsWith("+1")
            ? $"({_value[2..5]}) {_value[5..8]}-{_value[8..12]}" // US format
            : _value;
    
    public override string ToString() => _value;
}
```

#### `PaymentHandle`
**Current state:** `string Handle` in DonationAccount
**Problem:** No validation for $cashtags, Zelle emails/phones
**Solution:**
```csharp
[TypeConverter(typeof(PaymentHandleConverter))]
[JsonConverter(typeof(PaymentHandleJsonConverter))]
public readonly struct PaymentHandle : IEquatable<PaymentHandle>
{
    private readonly string _value;
    
    public const int MaxLength = 100;
    
    private PaymentHandle(string value) => _value = value;
    
    public static ErrorOr<PaymentHandle> Create(string? handle, DonationMethod method)
    {
        if (string.IsNullOrWhiteSpace(handle))
            return Error.Validation("PaymentHandle.Empty", "Handle cannot be empty");
        
        handle = handle.Trim();
        
        return method switch
        {
            DonationMethod.CashApp => ValidateCashTag(handle),
            DonationMethod.Zelle => ValidateZelleHandle(handle),
            _ => new PaymentHandle(handle) // Other methods are flexible
        };
    }
    
    private static ErrorOr<PaymentHandle> ValidateCashTag(string handle)
    {
        // Must start with $, 3-20 alphanumeric chars
        if (!handle.StartsWith('$'))
            return Error.Validation("PaymentHandle.CashApp", "CashApp handle must start with $");
        
        var tag = handle[1..];
        if (tag.Length is < 3 or > 20 || !tag.All(char.IsLetterOrDigit))
            return Error.Validation("PaymentHandle.CashApp", 
                "CashApp tag must be 3-20 alphanumeric characters");
        
        return new PaymentHandle(handle.ToLowerInvariant());
    }
    
    private static ErrorOr<PaymentHandle> ValidateZelleHandle(string handle)
    {
        // Email or phone number
        var emailResult = EmailAddress.Create(handle);
        if (!emailResult.IsError)
            return new PaymentHandle(handle.ToLowerInvariant());
        
        var phoneResult = PhoneNumber.Create(handle);
        if (!phoneResult.IsError)
            return new PaymentHandle(phoneResult.Value);
        
        return Error.Validation("PaymentHandle.Zelle", 
            "Zelle handle must be a valid email or phone number");
    }
    
    public static implicit operator string(PaymentHandle handle) => handle._value;
    
    public override string ToString() => _value;
}
```

#### `PersonName`
**Current state:** Separate `FirstName`, `LastName` strings
**Problem:** No validation, inconsistent trimming/casing
**Solution:**
```csharp
[TypeConverter(typeof(PersonNameConverter))]
[JsonConverter(typeof(PersonNameJsonConverter))]
public readonly struct PersonName : IEquatable<PersonName>
{
    private readonly string _value;
    
    public const int MaxLength = 100;
    
    private PersonName(string value) => _value = value;
    
    public static ErrorOr<PersonName> Create(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation("PersonName.Empty", "Name cannot be empty");
        
        name = name.Trim();
        
        if (name.Length > MaxLength)
            return Error.Validation("PersonName.TooLong", $"Name cannot exceed {MaxLength} characters");
        
        // Title case normalization
        name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.ToLowerInvariant());
        
        return new PersonName(name);
    }
    
    public static implicit operator string(PersonName name) => name._value;
    
    public override string ToString() => _value;
}
```

**Alternative approach:** Keep separate First/Last but use PersonName for each:
```csharp
public class Member
{
    public PersonName FirstName { get; set; }
    public PersonName LastName { get; set; }
    
    public string FullName => $"{FirstName} {LastName}";
}
```

---

## 🟡 MEDIUM PRIORITY - Consider Extracting

### `MemberId` (Strongly-typed ID)
**Current state:** `Guid Id`
**Problem:** Easy to mix up different entity IDs
**Solution:**
```csharp
[TypeConverter(typeof(MemberIdConverter))]
[JsonConverter(typeof(MemberIdJsonConverter))]
public readonly struct MemberId : IEquatable<MemberId>
{
    private readonly Guid _value;
    
    private MemberId(Guid value) => _value = value;
    
    public static MemberId New() => new(Guid.CreateVersion7());
    
    public static MemberId From(Guid guid) => new(guid);
    
    public static implicit operator Guid(MemberId id) => id._value;
    
    public override string ToString() => _value.ToString();
}
```

**Benefits:**
- Type safety: Can't accidentally pass `DonationId` where `MemberId` expected
- Self-documenting code
- Prevents bugs at compile time

**Similar candidates:**
- `DonationId`
- `FamilyId`
- `ObligationId`
- `RawTransactionId`

**Trade-off:** More boilerplate, but **massive** safety improvement

---

## 🟢 LOW PRIORITY - Maybe Extract Later

### `FamilyName`
**Current state:** `string Name` in Family
**Benefit:** Could use same PersonName primitive or create separate FamilyName

### `IdempotencyKey`
**Current state:** `string IdempotencyKey`
**Benefit:** Validation, format enforcement

### `Notes` / `Memo`
**Current state:** `string? Notes`, `string? Memo`
**Benefit:** Length limits, sanitization
**Risk:** Probably overkill unless you need XSS protection

---

## 🏗️ Implementation Strategy

### Phase 1: Enums (Low Risk, High Impact)
**Time:** 1-2 hours
**Risk:** Very low (no validation logic)

1. Create `ChurchApp.Primitives/Enums/` folder
2. Copy all enum files
3. Update namespaces to `ChurchApp.Primitives.Enums`
4. Add reference to Primitives in Application, API, Blazor
5. Update using statements (global using for convenience)
6. Build & test

### Phase 2: EmailAddress & PhoneNumber (High Impact)
**Time:** 3-4 hours
**Risk:** Medium (need migration for existing data)

1. Create value objects with full validation
2. Create TypeConverters & JsonConverters
3. Update Member entity
4. Create EF value converter
5. Generate migration to validate existing data
6. Update API endpoints
7. Update Blazor UI forms
8. Write unit tests

### Phase 3: PaymentHandle (Medium Impact)
**Time:** 2-3 hours
**Risk:** Medium

1. Create PaymentHandle with method-specific validation
2. Update DonationAccount entity
3. Create migration
4. Update parsers (Gmail extraction)
5. Update UI

### Phase 4: PersonName (Optional)
**Time:** 2 hours
**Risk:** Low

1. Decide: Single PersonName or FirstName/LastName each as PersonName
2. Create value object
3. Update Member entity
4. Migration for normalization

### Phase 5: Strongly-typed IDs (Optional, Advanced)
**Time:** 1 day
**Risk:** High (touches everything)

1. Create all ID types
2. Update ALL entity references
3. Update ALL repositories
4. Update ALL API contracts
5. Extensive testing

---

## 📁 Recommended Folder Structure

```
ChurchApp.Primitives/
├── ChurchApp.Primitives.csproj
├── Donations/
│   ├── DonationAmount.cs             ✅ (done)
│   ├── DonationType.cs                🔴 (move)
│   ├── DonationMethod.cs              🔴 (move)
│   ├── DonationStatus.cs              🔴 (move)
│   └── PaymentHandle.cs               🟡 (new)
├── Members/
│   ├── EmailAddress.cs                🟡 (new)
│   ├── PhoneNumber.cs                 🟡 (new)
│   ├── PersonName.cs                  🟡 (new)
│   └── MemberId.cs                    🟢 (optional)
├── Obligations/
│   ├── ObligationType.cs              🔴 (move)
│   ├── ObligationStatus.cs            🔴 (move)
│   └── ObligationId.cs                🟢 (optional)
├── Transactions/
│   ├── TransactionProvider.cs         🔴 (move)
│   ├── RawTransactionStatus.cs        🔴 (move)
│   └── ProviderTransactionId.cs       🟢 (optional)
└── Common/
    ├── ErrorOrExtensions.cs           🟢 (helpers)
    └── ValidationErrors.cs            🟢 (shared error codes)
```

---

## 🎓 Design Principles Applied

### 1. **Shared Kernel Pattern (DDD)**
> "Designate some subset of the domain model that teams agree to share. Keep this kernel small." - Eric Evans

Primitives are the **shared kernel** - minimal but crucial.

### 2. **Single Responsibility (SOLID)**
> "A class should have one, and only one, reason to change." - Uncle Bob

Each primitive has ONE job: represent a valid value.

### 3. **Type Safety (Anders Hejlsberg)**
> "I consider it almost as important to save you from shooting yourself in the foot as to give you the gun in the first place."

Strong types prevent entire classes of bugs at compile time.

### 4. **Fail Fast (Kent Beck)**
> "Make it fail, make it work, make it fast."

Validation at the boundary (Create method) ensures invalid data never enters the system.

---

## 🧪 Testing Strategy

### Unit Tests for Each Primitive

```csharp
public sealed class EmailAddressTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("user+tag@example.com")]
    [InlineData("user.name@sub.example.com")]
    public void Create_ValidEmail_Succeeds(string email)
    {
        var result = EmailAddress.Create(email);
        Assert.False(result.IsError);
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    public void Create_InvalidEmail_ReturnsError(string email)
    {
        var result = EmailAddress.Create(email);
        Assert.True(result.IsError);
    }
    
    [Fact]
    public void Create_NormalizesToLowerCase()
    {
        var result = EmailAddress.Create("USER@EXAMPLE.COM");
        Assert.Equal("user@example.com", result.Value.ToString());
    }
    
    [Fact]
    public void TypeConverter_ParsesFromString()
    {
        var converter = TypeDescriptor.GetConverter(typeof(EmailAddress));
        var result = converter.ConvertFromString("user@example.com");
        Assert.IsType<EmailAddress>(result);
    }
}
```

---

## 🚀 Migration Guide

### Step 1: Add Primitives Reference
```xml
<!-- In Application, API, Blazor projects -->
<ItemGroup>
  <ProjectReference Include="../ChurchApp.Primitives/ChurchApp.Primitives.csproj" />
</ItemGroup>
```

### Step 2: Global Usings (Optional but Recommended)
```csharp
// ChurchApp.Application/GlobalUsings.cs
global using ChurchApp.Primitives.Donations;
global using ChurchApp.Primitives.Members;
global using ChurchApp.Primitives.Obligations;
global using ChurchApp.Primitives.Transactions;
```

### Step 3: Update Entities (Example: Member)
```csharp
// Before
public class Member
{
    public string FirstName { get; set; }
    public string? Email { get; set; }
}

// After
public class Member
{
    public PersonName FirstName { get; set; }
    public EmailAddress? Email { get; set; }
}
```

### Step 4: EF Core Value Converters
```csharp
builder.Property(x => x.Email)
    .HasConversion(
        email => email.HasValue ? (string)email.Value : null,
        value => value != null ? EmailAddress.Create(value).Value : null)
    .HasMaxLength(EmailAddress.MaxLength);
```

### Step 5: Migration for Data Validation
```sql
-- Validate existing emails before migration
UPDATE "Members" 
SET "Email" = LOWER(TRIM("Email")) 
WHERE "Email" IS NOT NULL;

-- Check for invalid emails
SELECT * FROM "Members" 
WHERE "Email" IS NOT NULL 
  AND "Email" NOT LIKE '%@%.%';
```

---

## 📊 Impact Analysis

| Primitive | Files Affected | Complexity | Value |
|-----------|----------------|------------|-------|
| DonationAmount | ~20 | Medium | ✅ Done |
| DonationType enum | ~15 | Low | Very High |
| DonationMethod enum | ~15 | Low | Very High |
| EmailAddress | ~5 | Medium | High |
| PhoneNumber | ~5 | Medium | High |
| PaymentHandle | ~8 | Medium | High |
| PersonName | ~10 | Medium | Medium |
| Strongly-typed IDs | ~50 | Very High | Medium |

---

## ✅ Recommendation

**Do Phase 1 (Enums) IMMEDIATELY:**
- Low risk, high value
- No migration needed
- Enables UI consistency
- Takes <2 hours

**Do Phase 2 (Email/Phone) NEXT:**
- High value for data quality
- Catches bugs early
- Improves user experience

**Consider Phase 3 (PaymentHandle) if Gmail integration active**

**Skip Phase 5 (Strongly-typed IDs) unless you have type-related bugs**

---

**Next Step:** Start with moving the enums? I can help you execute Phase 1 right now.
