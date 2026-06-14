# Technical Specification: Production (التصنيع) Module

This documentation provides the technical details for the **Production (التصنيع)** module to ensure 100% parity between the server-side ASP.NET Core API and the local WPF (ASP.NET Core WPF) desktop application.

## 1. Database Design (Server SQL Server & Local SQLite)

Both databases must share the exact same structure for these tables. All Primary Keys are **GUIDs** to facilitate seamless synchronization. All tables must include the `BaseEntity` fields: `CreatedAt`, `UpdatedAt`, and `IsDeleted`.

### 1.1 Table: `ProductRecipes` (Master Data)
Defines the "Bill of Materials" for a manufactured product.

| Attribute | Type | Description |
| :--- | :--- | :--- |
| `RecipeId` | `Guid` | Primary Key. |
| `ManufacturedProductId` | `Guid` | FK to `Products`. The product being created. |
| `RawProductId` | `Guid` | FK to `Products`. Raw material needed. |
| `QuantityNeeded` | `Decimal` | Amount of raw material required for **1 unit** of the manufactured product. |

### 1.2 Table: `ProductionOperations` (Transaction Header)
Records the event of manufacturing a batch of products.

| Attribute | Type | Description |
| :--- | :--- | :--- |
| `ProductionOperationId` | `Guid` | Primary Key. |
| `LocationId` | `Guid` | FK to `Locations`. Where production happened. |
| `ManufacturedProductId` | `Guid` | FK to `Products`. |
| `QuantityProduced` | `Decimal` | The number of units produced. |
| `UnitCost` | `Decimal` | Calculated average cost of production per unit. |
| `TotalCost` | `Decimal` | Calculated total cost of the batch (sum of material costs). |
| `ProductionDate` | `DateTime` | Date/Time of production. |
| `CreatedByUserId` | `Guid` | FK to `Users`. |
| `DeviceId` | `Guid?` | Unique ID of the device. |
| `Notes` | `String?` | Optional remarks. |

### 1.3 Table: `ProductionOperationItems` (Transaction Details)
Records the specific consumption of raw materials for a production event.

| Attribute | Type | Description |
| :--- | :--- | :--- |
| `ProductionOperationItemId` | `Guid` | Primary Key. |
| `ProductionOperationId` | `Guid` | FK to `ProductionOperations`. |
| `RawProductId` | `Guid` | FK to `Products`. |
| `QuantityConsumed` | `Decimal` | Total raw material used (`Recipe.QuantityNeeded * QtyProduced`). |
| `UnitCost` | `Decimal` | The `AverageCost` of the raw material at the moment of production. |
| `TotalCost` | `Decimal` | `QuantityConsumed * UnitCost`. |

---

## 2. Business Logic & Validations

The logic must be identical in the **WPF Local Service** and the **Server API**.

### 2.1 Production Execution Logic
Production is an **Atomic Operation** (must run inside a Database Transaction).

1.  **Product Validation**: 
    - The target product must exist and have `ProductType == "Manufactured"`.
    - `QuantityProduced` must be greater than zero.
2.  **Recipe Verification**: 
    - The target product **must** have at least one valid recipe defined in `ProductRecipes`.
3.  **Stock Availability Check (Critical)**:
    - For each item in the recipe:
        - Calculate `RequiredQty = QuantityNeeded * QuantityProduced`.
        - Verify that the local `StockBalances.QuantityOnHand` for that raw material is $\ge$ `RequiredQty`.
        - If stock is insufficient, the operation **must fail**.
4.  **Costing**:
    - Fetch the `AverageCost` for each raw material from the local `StockBalances` table.
    - Calculate `TotalCost` for the batch.
    - Calculate `UnitCost` (TotalCost / QuantityProduced).
5.  **Inventory Updates (Stock Movements)**:
    - For **each** raw material: Create a `StockMovement` with type `ProductionConsume` (negative quantity).
    - For the **manufactured** product: Create a `StockMovement` with type `ProductionIn` (positive quantity).
    - Increment/Decrement corresponding `StockBalances` records.

### 2.2 Production Cancellation Logic
1.  **Safety Check**: Verify that the manufactured product still exists in stock (`QuantityOnHand >= op.QuantityProduced`).
2.  **Inventory Reversal**: 
    - Create `ProductionCancelOut` movement for the manufactured product (-qty).
    - Create `ProductionCancelReturn` movements for all raw materials (+qty).
3.  **Soft Delete**: Set `IsDeleted = true` for the operation and its detail items.

---

## 3. Local UI & API Requirements

### 3.1 Filtering & Pagination
The UI/API must support:
- **Filter by Location**: `locationId` is mandatory for branch/warehouse views.
- **Pagination**: 
    - `page` (default: 1)
    - `pageSize` (default: 50)
- **Ordering**: Always sorted by `CreatedAt` Descending.

### 3.2 Real-time Logic Parity
- When selecting a manufactured product in the UI, the app should automatically display the list of required raw materials and their current local stock availability to the user **before** they hit "Execute".

---

## 4. Synchronization Strategy

1.  **GUID Uniqueness**: Since production is recorded Local-First, the GUIDs generated in SQLite will be used as the primary keys on the server.
2.  **Sync Sequence**: Production operations and their detail items must be synced together (atomic sync) via the `SyncOutbox`.
3.  **Material Costs**: The `UnitCost` and `TotalCost` calculated locally based on local inventory rates are the "Source of Truth" that will be pushed to the server.

---

**Note to the AI Frontend Agent:**
Ensure that when a production is executed, the user is warned if stock is low for any recipe item. The transaction must roll back entirely if any single step (e.g., balance update) fails.
