# Update Specification: Initial Inventory Balance (رصيد أول المدة)

This document provides specific instructions for the **Frontend AI Agent** to update the WPF Desktop Application to support the new **Initial Balance** feature. This ensures that the local application maintains full logic and design parity with the Server API.

## 1. Objective
Allow users to set a starting quantity and unit cost for products that are being introduced to the system for the very first time. This operation is restricted to products with **zero transaction history** at a specific location.

---

## 2. Local Database Design (SQLite)

No structural changes are required to the existing `StockBalances` or `StockMovements` tables. However, the application must now recognize and support a new standard value for the `MovementType` column.

- **Table**: `StockMovements`
- **New Value**: `InitialBalance` (String)
- **Data Impact**: This movement will be the first record for a product/location pair and will establish the baseline for the Weighted Average Cost (WAC).

---

## 3. Local Business Logic (WPF Service Layer)

The logic in the local `InventoryService` (or equivalent) must mirror the server-side validation exactly:

### 3.1 Validation: History Check
Before allowing an initial balance entry, the local service **must** check if any movements exist for the target product at the selected location:
```csharp
// Local SQLite Check
bool hasHistory = await _localDb.StockMovements
    .AnyAsync(m => m.ProductId == productId && m.LocationId == locationId && !m.IsDeleted);

if (hasHistory) {
    throw new InvalidOperationException("Cannot set initial stock: history already exists.");
}
```

### 3.2 Execution: Movement & Balance
When setting the initial balance:
1.  **Create Movement**: Insert a record into `StockMovements`.
    - `MovementType` = `"InitialBalance"`
    - `QuantityIn` = `[User Input]`
    - `UnitCost` = `[User Input]`
    - `ReferenceType` = `null` (or `"InitialSetup"`)
2.  **Initialize Balance**: Create a record in `StockBalances` (or update if an empty one exists).
    - `QuantityOnHand` = `[User Input]`
    - `AverageCost` = `[User Input]`

---

## 4. UI/UX Design Requirements

The frontend agent should update the **Inventory** or **Product Details** screen with the following:

### 4.1 "Set Initial Balance" Interface
- **Trigger**: Add a button "Set Initial Balance" (ضبط رصيد أول مودة).
- **Visibility**: This button should be **disabled** or **hidden** if the selected product/location pair has any existing movements.
- **Input Form (Modal/Dialog)**:
    - **Location**: Dropdown selector (Mandatory).
    - **Quantity**: Numeric input (Decimal, >0).
    - **Unit Cost**: Numeric input (Decimal, >0).
    - **Note**: A small text note explaining that this is a one-time operation.

### 4.2 Local Validation
- The UI should proactively check the local SQLite database when a location is selected in the product view and toggle the "Initial Balance" feature availability accordingly.

---

## 5. API Integration (Sync Strategy)

When the application is **Online**, the sync service should push this local operation to the server using the new endpoint:

- **Method**: `POST`
- **URL**: `/api/Inventory/initial` (See `InventoryController` on Server)
- **Body**:
  ```json
  {
    "productId": "GUID",
    "locationId": "GUID",
    "quantity": 100.50,
    "unitCost": 15.25
  }
  ```

---

**Note to the AI Frontend Agent:**
Ensure that all operations are wrapped in a local database transaction. If the sync fails due to existing history already present on the server (conflict), the local app should notify the user that the product is no longer "new" and the balance must be adjusted via a **Stock Count** instead.
