# Update Specification: Stock Count Nullable Quantities & Deletion

This document is for the **Frontend/Backend AI Agent** to synchronize the Server API schema and logic with modifications made to the WPF Desktop Application regarding the Stock Count (الجرد) module.

## 1. Objective
Enhance the stock count usability to allow for fast keyboard data entry, showing an empty cell for items that haven't been counted instead of defaulting them to 0. Also, allow users to delete unposted/pending stock counts.

## 2. Server Database changes (SQL Server & EF Core)

The schema for `StockCountItem` must be updated to support nullable `ActualQty` and `DifferenceQty` so that an item can be explicitly tracked as "not yet counted" vs "counted as zero".

- **Table**: `StockCountItems`
- **Impacted Columns**:
  - `ActualQty` -> `decimal?` (Nullable)
  - `DifferenceQty` -> `decimal?` (Nullable)

### Update EF Model
```csharp
public class StockCountItem : BaseEntity
{
    // ... Existing Properties 
    public decimal SystemQty { get; set; }
    
    // Make these two nullable
    public decimal? ActualQty { get; set; }
    public decimal? DifferenceQty { get; set; }
}
```

## 3. Core Business Logic Changes

### Creating a Stock Count
When auto-populating items for a new stock count, the system should default to `null` for uncounted items:
```csharp
ActualQty = null,
DifferenceQty = null,
```

### Posting / Approving a Stock Count
If a StockCount is approved while some items still have `ActualQty == null`, the system should ignore these items (treat them as not adjusted), by assessing the difference as `0`. 
Example calculation in approve loop:
```csharp
item.DifferenceQty = item.ActualQty.HasValue ? (item.ActualQty.Value - item.SystemQty) : 0m;

if (balance != null && item.DifferenceQty.GetValueOrDefault() != 0)
{
    // Apply Adjustment
}
```

## 4. API Endpoint Changes

### Add DELETE Endpoint: `DELETE /api/StockCounts/{stockCountId}`
Create a new endpoint that allows the deletion of a `StockCount` **only if** its Status is `"Pending"`.
1. Validate that the Stock Count exists and its status is "Pending".
2. Hard-delete or Soft-delete the associated `StockCountItems`.
3. Hard-delete or Soft-delete the `StockCount`.
4. Ensure this coordinates with the Sync queue (Sync action = `Delete`).

## 5. Status String Localization (Arabic)

Ensure the Backend API validates and uses Arabic terminology for the `Status` column to maintain exact parity with the desktop application:
1. **Pending Status:** The string is now `"قيد الانتظار"` (was `"Pending"`).
2. **Posted/Approved Status:** The string is now `"معتمد"` (was `"Posted"`).
Ensure all validation checks, creation workflows, and database constraints recognize these precise Arabic strings.
