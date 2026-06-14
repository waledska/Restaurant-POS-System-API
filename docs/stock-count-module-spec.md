# Technical Specification: StockCount (الجرد) Module

This documentation provides the technical details for the **StockCount (الجرد)** module to ensure 100% parity between the server-side ASP.NET Core API and the local WPF (ASP.NET Core WPF) desktop application.

## 1. Database Schema (Server SQL Server & Local SQLite)

Both databases must share the exact same structure for these tables. All Primary Keys are **GUIDs** to facilitate seamless synchronization.

### 1.1 Base Entity Fields
All tables include these audit and sync fields (from `BaseEntity`):
- `CreatedAt`: `DateTime` (Default: UTC Now)
- `UpdatedAt`: `DateTime?` (Nullable)
- `IsDeleted`: `Boolean` (Default: `false`)

### 1.2 Table: `StockCounts`
Stores the header information for a stock count session.

| Attribute | Type | Description |
| :--- | :--- | :--- |
| `StockCountId` | `Guid` | Primary Key |
| `LocationId` | `Guid` | FK to `Locations`. The branch or warehouse being counted. |
| `CountDate` | `DateTime` | The date/time the count was performed. |
| `CreatedByUserId` | `Guid` | FK to `Users`. Who initiated the count. |
| `Status` | `String` | Status of the count: `Pending` or `Posted`. |
| `PostedAt` | `DateTime?` | When the count was approved and adjusted in inventory. |
| `DeviceId` | `Guid?` | The unique ID of the device that created the record. |
| `Notes` | `String?` | Optional remarks. |

**Indexes:**
- `IX_StockCounts_LocationId_CountDate`: Composite index for faster retrieval by location and date.
- `IX_StockCounts_Status_CountDate`: Search by status.

### 1.3 Table: `StockCountItems`
Stores the individual product counts within a session.

| Attribute | Type | Description |
| :--- | :--- | :--- |
| `StockCountItemId` | `Guid` | Primary Key |
| `StockCountId` | `Guid` | FK to `StockCounts`. |
| `ProductId` | `Guid` | FK to `Products`. |
| `SystemQty` | `Decimal` | The quantity recorded in the system at the moment of counting. |
| `ActualQty` | `Decimal` | The physical quantity counted by the user. |
| `DifferenceQty` | `Decimal` | Calculated as: `ActualQty - SystemQty`. |

**Indexes:**
- `UQ_StockCountItems_StockCountId_ProductId`: Unique constraint to prevent duplicate products in the same count.

---

## 2. Business Logic & Validations

The local WPF application must mirror the server logic exactly to prevent data integrity issues during synchronization.

### 2.1 Creation Logic (Draft/Pending)
1.  **Product Validation**: Ensure all selected products exist and are active (`IsDeleted = false`).
2.  **System Quantity Capture**:
    - At the moment of saving the count, the application **must** fetch the current `QuantityOnHand` from the `StockBalances` table for the specified `LocationId`.
    - This value is saved into `SystemQty`.
3.  **Difference Calculation**: `DifferenceQty` = `ActualQty - SystemQty`.
4.  **Initial Status**: Every new count is saved with `Status = "Pending"`.

### 2.2 Posting (Approval) Logic
Posting is an **Atomic Operation** (must run inside a Database Transaction).
1.  **State Check**: Only records with `Status == "Pending"` can be posted.
2.  **Inventory Adjustment**:
    - For every item where `DifferenceQty != 0`:
        - A `StockMovement` must be created with type `StockCountAdjustment`.
        - The `QuantityChange` matches `DifferenceQty`.
        - The `UnitCost` should be the current `AverageCost` from `StockBalances`.
    - Update `StockBalances.QuantityOnHand` by adding the `DifferenceQty`.
3.  **Finalize**:
    - Set `Status = "Posted"`.
    - Set `PostedAt = DateTime.UtcNow`.
    - Log the action in `AuditLogs`.

---

## 3. Local UI & API Parity

### 3.1 Filtering & Pagination
The UI/API must support the following for the StockCount list:
- **Filter by Location**: `locationId` (Mandatory for branch users, optional for admins).
- **Pagination**: 
    - `page` (default: 1)
    - `pageSize` (default: 50)
- **Ordering**: Always results sorted by `CreatedAt` Descending.

### 3.2 Data Presentation
When viewing a specific count:
- Header details: Date, Status, Location Name, Creator Name.
- Item details: Product Name, System Quantity, Actual Quantity, Difference, and the Highlight of variances (positive/negative).

---

## 4. Synchronization Strategy (Local-First)

1.  **Offline Storage**: When offline, the app stores the `StockCount` and `StockCountItems` in the local SQLite database.
2.  **Logic Execution**: The logic described in Section 2 (System Qty capture and Posting) is executed against the **Local SQLite** database.
3.  **Background Sync**:
    - Once the device is online, the `SyncOutbox` pattern picks up these records.
    - The local `StockCountId` (GUID) ensures that the record is unique on the server.
    - **Important**: The server will receive the `SystemQty` that was calculated locally. This preserves the "Snapshot" of the inventory state as seen by the user during the offline count.

---

**Note to the AI Frontend Agent:**
Ensure that all forms for StockCount include validation for negative quantities (unless specifically allowed for certain product types) and that the "Post" button is only enabled for users with appropriate roles (BranchManager or Admin).
