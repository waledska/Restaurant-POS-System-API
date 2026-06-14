# Technical Specification: Inventory (المخزون) Module

This documentation provides the technical details for the **Inventory (المخزون)** module to ensure 100% parity between the server-side ASP.NET Core API and the local WPF (ASP.NET Core WPF) desktop application.

## 1. Database Design (Server SQL Server & Local SQLite)

The Inventory module uses two primary tables to track current state (Balances) and change history (Movements). Both must implement the `BaseEntity` structure and use **GUIDs** for all primary and foreign keys.

### 1.1 Table: `StockBalances` (Current Snapshot)
Stores the real-time stock levels and weighted average costs for each product at each location.

| Attribute | Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `StockBalanceId` | `Guid` | Primary Key | |
| `LocationId` | `Guid` | Foreign Key | Link to `Locations`. |
| `ProductId` | `Guid` | Foreign Key | Link to `Products`. |
| `QuantityOnHand` | `Decimal` | Default: 0 | Total physical stock currently available. |
| `AverageCost` | `Decimal` | Default: 0 | Weighted Average Cost (WAC) per unit. |
| `LastMovementAt` | `DateTime` | Nullable | Timestamp of the last transaction. |

**Indexes:**
- **Unique Constraint**: `UQ_StockBalances_LocationId_ProductId`. Only one balance record per product/location pair is allowed.

### 1.2 Table: `StockMovements` (Audit Trail)
Records every single transaction that changes inventory levels.

| Attribute | Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `StockMovementId` | `Guid` | Primary Key | |
| `MovementDate` | `DateTime` | Required | Standard UTC timestamp of the operation. |
| `LocationId` | `Guid` | Foreign Key | Link to `Locations`. |
| `ProductId` | `Guid` | Foreign Key | Link to `Products`. |
| `MovementType` | `String` | Required | See Section 2.2 for types. |
| `QuantityIn` | `Decimal` | Default: 0 | Quantity added to stock (>0). |
| `QuantityOut` | `Decimal` | Default: 0 | Quantity removed from stock (>0). |
| `UnitCost` | `Decimal` | Required | Cost per unit for this specific movement. |
| `TotalCost` | `Decimal` | Required | `(QtyIn or QtyOut) * UnitCost`. |
| `ReferenceType` | `String` | Optional | Source module (e.g., `PurchaseInvoice`). |
| `ReferenceId` | `Guid` | Optional | ID of the source document. |
| `CreatedByUserId` | `Guid` | Foreign Key | |

---

## 2. Business Logic & Validations

The local WPF application must implement the following logic in its **Local Service Layer** to maintain data integrity before synchronization.

### 2.1 Balance Update Rules
Inventory updates are **Triggers Only**. There are no direct `POST` endpoints for inventory. Updates occur when other modules (Purchasing, Transfers, Production) call the `ApplyStockMovement` logic.

**Process for every movement:**
1.  **Upsert Balance**: If no `StockBalance` record exists for the product/location, create a new one with `QuantityOnHand = 0` and `AverageCost = 0`.
2.  **Weighted Average Cost (WAC) Calculation**:
    - **Inbound Only**: Cost is updated ONLY when `QuantityIn > 0`.
    - **Formula**: `NewAverageCost = ((OldQty * OldAvgCost) + (NewQty * CurrentUnitCost)) / (OldQty + NewQty)`.
3.  **Quantity Adjustment**:
    - `QuantityOnHand = QuantityOnHand + (QtyIn - QtyOut)`.
4.  **Movement Logging**: A record must be created in `StockMovements` with the correct `ReferenceType`.

### 2.2 Supported Movement Types
The `MovementType` and `ReferenceType` must strictly match these strings:
- `PurchaseIn`: Stock received from a supplier invoice.
- `TransferOut`: Stock leaving a warehouse for a transfer.
- `TransferIn`: Stock arriving at a branch/warehouse from a transfer.
- `ProductionConsume`: Raw materials used in manufacturing.
- `ProductionIn`: Manufactured products added to stock.
- `StockCountAdjustment`: Variance detected during a physical count (الجرد).
- `Waste`: Deliberate removal of damaged/expired stock.
- `InitialBalance`: Used for opening balances during setup.

---

## 3. Handling Initial Balances (الرصيد الافتتاحي)

The system provides a specific feature for setting the initial inventory state during setup.

### 3.1 Initial Stock Feature
Users can set an initial quantity and unit price for a product at a specific location via the `SetInitialStock` logic.

**Rules & Constraints:**
1.  **One-Time Operation**: This feature can only be used for a (Product, Location) pair that has **no existing transaction history** (no records in `StockMovements`).
2.  **Snapshot Creation**:
    - Creates a `StockMovement` with type `InitialBalance`.
    - Initializes the `StockBalance` with the provided `QuantityOnHand` and `AverageCost`.
3.  **Atomic**: The operation captures both quantity and cost simultaneously to establish the baseline for future Weighted Average Cost (WAC) calculations.

### 3.2 Alternative Processes
If the product already has history, stock must be adjusted via:
1.  **Stock Count (الجرد)**: Perform a physical count if there are variances.
2.  **Purchase Invoice**: Record a purchase if the stock was acquired from a vendor.

---

## 4. API & UI Functionality

### 4.1 Filtering & Pagination
The UI must allow users to view their inventory with the following rules:

- **Stock Balances**:
    - **Filter**: Always filter locally by `LocationId`.
    - **Search**: By `ProductName` or `ProductCode`.
    - **Pagination**: Default 50 items per page.
    - **Ordering**: Alphabetical by `ProductName`.

- **Stock Movements**:
    - **Filter**: By `LocationId` and/or `ProductId`, with Date Range filtering (From/To).
    - **Pagination**: Default 100 items per page.
    - **Ordering**: Chronological by `MovementDate` (Descending - newest first).

### 4.2 Offline Parity and UI Translation Note
The WPF app must perform **all WAC and Balance updates locally** in the SQLite database first. 
**IMPORTANT**: The client UI displays everything in Arabic (e.g. types are presented as `رصيد افتتاحي` instead of `InitialBalance` using UI Converters). However, the Web API endpoints MUST strictly accept and respond with the **English string values** (`InitialBalance`, `PurchaseIn`, etc.). Do not change Web API enums or DB columns to Arabic.

---

**Note to the AI Frontend Agent:**
Do not allow `QuantityOnHand` to go negative unless specifically permitted by the admin settings. Always use `Guid` for identifying products and locations to prevent sync collisions.
