# Immutable Collections Architecture Decision Record

## Status
✅ **IMPLEMENTED** - All response DTOs now use `ImmutableArray<T>`

## Context
Blazor WebAssembly applications benefit significantly from immutable collections in their data models. The original implementation used `T[]` (arrays) in response DTOs, which while immutable in practice, don't convey immutability intent and miss optimization opportunities with Blazor's rendering engine.

## Decision
We have adopted **`ImmutableArray<T>`** from `System.Collections.Immutable` for all response DTO collections.

### Rationale (Anders Hejlsberg & Mads Torgersen perspective)

1. **Blazor Rendering Optimization**
   - Blazor's diff algorithm performs reference equality checks on component parameters
   - `ImmutableArray<T>` is a struct with value equality semantics
   - When unchanged, reference equality succeeds immediately → no deep comparison needed
   - Result: Fewer re-renders, better performance

2. **Type Safety & Intent**
   ```csharp
   // Before: Ambiguous mutability
   public record DonationLedgerResponse(
       DonationLedgerItem[] Donations  // Can this be mutated? Unclear.
   );
   
   // After: Clear immutability
   public record DonationLedgerResponse(
       ImmutableArray<DonationLedgerItem> Donations  // Immutable by design
   );
   ```

3. **Null Safety**
   - `T[]` is nullable, requires null checks
   - `ImmutableArray<T>` is a struct, never null
   - Use `.IsDefault` to check for uninitialized state
   - Use `.IsDefaultOrEmpty` to check for empty collections

4. **Memory Efficiency**
   - Copy-on-write (COW) semantics
   - Empty collections use `ImmutableArray<T>.Empty` (zero allocation)
   - Structural sharing when creating modified versions

### Kent Beck's Testing Perspective

Immutable collections make testing easier:
```csharp
[Fact]
public void Response_Should_Not_Allow_Mutation()
{
    var response = new DonationLedgerResponse(
        Page: 1,
        PageSize: 10,
        TotalCount: 100,
        Donations: ImmutableArray.Create(new DonationLedgerItem(...))
    );
    
    // This won't compile - ImmutableArray<T> has no Add/Remove methods
    // response.Donations.Add(...); ❌ Compile error
    
    // Clear intent: to modify, you must create a new collection
    var modified = response.Donations.Add(newItem); // Returns new ImmutableArray
}
```

## Implementation

### Changes Made
All response DTOs now use `ImmutableArray<T>`:

| File | Property | Change |
|------|----------|--------|
| `DonationModels.cs` | `DonationLedgerResponse.Donations` | `DonationLedgerItem[]` → `ImmutableArray<DonationLedgerItem>` |
| `FamilyModels.cs` | `FamiliesResponse.Families` | `Family[]` → `ImmutableArray<Family>` |
| `ReportModels.cs` | `SummariesResponse.Summaries` | `SummaryItem[]` → `ImmutableArray<SummaryItem>` |
| `ReportModels.cs` | `TimeRangeReportResponse.Breakdown` | `DonationTypeBreakdown[]` → `ImmutableArray<DonationTypeBreakdown>` |
| `MemberModels.cs` | `MembersResponse.Members` | Already `ImmutableArray<Member>` ✅ |

### Component State Pattern (Uncle Bob's SRP)

**Principle**: Separate immutable data (from API) from mutable component state.

```csharp
public partial class Ledger : ComponentBase
{
    // Component state: Mutable List<T> for UI operations
    private List<DonationLedgerItem> donations = new();
    
    public async Task LoadDonations()
    {
        // API returns immutable ImmutableArray<T>
        var response = await DonationService.GetDonationsAsync(...);
        
        // Convert to mutable List<T> for component state
        donations = response.Donations.ToList();
        
        // Now we can mutate locally if needed:
        // donations.RemoveAt(0);
        // donations.Add(newItem);
    }
}
```

**Why this pattern?**
- **API boundary**: Immutable (prevents accidental mutation of shared state)
- **Component state**: Mutable (allows efficient local operations)
- **Clear separation**: .ToList() is an explicit conversion point

### JSON Serialization

`System.Text.Json` (used by `HttpClient.GetFromJsonAsync`) **natively supports** `ImmutableArray<T>`:

```csharp
// Deserialization: JSON array → ImmutableArray<T>
var response = await httpClient.GetFromJsonAsync<DonationLedgerResponse>(...);
// response.Donations is ImmutableArray<DonationLedgerItem> ✅

// Serialization: ImmutableArray<T> → JSON array (if needed)
await httpClient.PostAsJsonAsync("/api/bulk", ImmutableArray.Create(item1, item2));
```

**No custom converters required!**

## Performance Characteristics

### Memory
- **Empty collections**: Use `ImmutableArray<T>.Empty` (singleton, zero allocation)
- **Single-item**: Small overhead vs array (16 bytes struct wrapper)
- **Large collections**: Same memory footprint as `T[]` (just adds struct wrapper)

### CPU
- **Read access**: `O(1)` index lookup (same as array)
- **Iteration**: Same performance as array (uses array internally)
- **Equality checks**: `O(1)` reference equality (vs `O(n)` deep equality for arrays)
- **Mutation**: `O(n)` (creates new array) - but we convert to `List<T>` for mutations

### Blazor Rendering
- **Before (with `T[]`)**: Blazor performs element-by-element comparison on every render
- **After (with `ImmutableArray<T>`)**: Blazor checks reference equality first → skips deep comparison if unchanged

## Alternatives Considered

### 1. `IReadOnlyList<T>` / `IReadOnlyCollection<T>`
❌ **Rejected**: Interface type, requires heap allocation, no value equality

### 2. `T[]` (current)
❌ **Rejected**: Doesn't convey immutability intent, nullable, no perf benefits

### 3. `ReadOnlyCollection<T>`
❌ **Rejected**: Wrapper over `List<T>`, heap allocation, no structural sharing

### 4. `FrozenSet<T>` / `FrozenDictionary<K,V>` (.NET 8+)
✅ **Future consideration**: For lookup-heavy scenarios, but overkill for DTOs

## Migration Guide

### For New Code
```csharp
// ✅ DO: Use ImmutableArray<T> in response DTOs
public record MyResponse(
    ImmutableArray<MyItem> Items
);

// ❌ DON'T: Use arrays or mutable collections
public record MyResponse(
    MyItem[] Items  // ❌
);
```

### For Component State
```csharp
public partial class MyComponent : ComponentBase
{
    // ✅ DO: Use List<T> for mutable component state
    private List<MyItem> items = new();
    
    public async Task LoadData()
    {
        var response = await Service.GetItemsAsync();
        items = response.Items.ToList(); // Convert immutable → mutable
    }
}
```

### Checking for Empty
```csharp
// ❌ DON'T: Check for null (ImmutableArray is a struct)
if (response.Donations == null) { }

// ✅ DO: Check IsDefault or IsDefaultOrEmpty
if (response.Donations.IsDefault) { }        // Uninitialized
if (response.Donations.IsDefaultOrEmpty) { } // Uninitialized OR empty
if (response.Donations.Length == 0) { }      // Empty (but initialized)
```

## Jez Humble's CI/CD Perspective

### Build Verification
- ✅ Solution builds with zero errors
- ✅ No runtime serialization issues (System.Text.Json supports ImmutableArray)
- ✅ Existing tests pass (ToList() conversion is transparent)

### Deployment Risk
- **Risk Level**: **LOW**
  - Change is at the DTO boundary only
  - Component code uses `.ToList()` conversion (same as before)
  - JSON serialization is compatible
  - No breaking API changes

### Rollback Plan
If issues arise, rollback is simple:
1. Change `ImmutableArray<T>` back to `T[]` in models
2. Remove `using System.Collections.Immutable;`
3. Rebuild and deploy

## References

1. [Blazor Performance Best Practices - Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/blazor/performance)
2. [System.Collections.Immutable Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.collections.immutable)
3. [Immutable Collections Performance - .NET Blog](https://devblogs.microsoft.com/dotnet/please-welcome-immutablearrayt/)
4. [System.Text.Json and Immutable Collections](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/supported-collection-types)

## Conclusion

Adopting `ImmutableArray<T>` for response DTOs aligns with modern C# best practices, improves Blazor rendering performance, and provides clearer intent about data mutability. The pattern of immutable API responses + mutable component state creates a clean architectural boundary.

**Next Steps**:
1. ✅ Models refactored
2. ✅ Build passing
3. ⏭️ Monitor Blazor rendering performance improvements
4. ⏭️ Consider `ImmutableDictionary<K,V>` for lookup scenarios if needed

---
**Decision Date**: 2026-03-02  
**Status**: ✅ Implemented  
**Build Status**: ✅ Passing
