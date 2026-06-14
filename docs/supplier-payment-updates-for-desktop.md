# Supplier Payment Updates - Desktop Application Synchronization Guide

## 1. Schema Changes (SQLite)
The data models have been refactored to support global payment methods instead of supplier-specific ones.

### **Removed Table:**
- `SupplierPaymentMethods` (Drop this table from SQLite or remove its entity and mappings).

### **New Table: `SystemPaymentMethods`**
Create a new table/entity to store global payment methods:
- `SystemPaymentMethodId` (Guid, Primary Key)
- `MethodType` (String, MaxLength 50, Required) -> e.g., "Cash", "BankTransfer", "VodafoneCash"
- `AccountData` (String, Nullable)
- `Notes` (String, Nullable)
- `IsActive` (Boolean)
- `CreatedAt` (DateTime)
- `UpdatedAt` (DateTime, Nullable)
- `IsDeleted` (Boolean)

### **Updated Table: `SupplierPayments`**
- Rename column/property `SupplierPaymentMethodId` to `SystemPaymentMethodId` (Guid, Required).
- The foreign key relationship now points to `SystemPaymentMethods` instead of `SupplierPaymentMethods`.

### **Updated Table: `Suppliers`**
- Remove the `PaymentMethods` collection navigation property. The system payment methods are no longer tightly coupled to a single supplier.

---

## 2. Business Logic & Validation (Local Service Layer)
When a user attempts to add a supplier payment in the offline/desktop app, the following strict validations must be enforced locally to match the server logic.

### **Scenario A: Payment against a specific Purchase Invoice**
If `PurchaseInvoiceId` has a value:
1. Ensure the invoice is not fully paid: `if (invoice.RemainingAmount == 0 || invoice.Status == "Paid")` -> Reject with message: "This invoice is already fully paid."
2. Ensure the payment amount does not exceed the remaining balance on the invoice: `if (amount > invoice.RemainingAmount)` -> Reject with message: "Payment amount cannot be greater than the remaining invoice amount."
3. **If Valid:** Decrease `RemainingAmount` by `amount`. Increase `PaidAmount` by `amount`. Update invoice status to `"Paid"` if `RemainingAmount == 0`, else `"PartiallyPaid"`.
4. Update Supplier's balance: Decrease `CurrentBalance` by `amount`.

### **Scenario B: Payment directly to Supplier Account (No Invoice)**
If `PurchaseInvoiceId` is null:
1. Ensure the supplier has an outstanding balance: `if (supplier.CurrentBalance <= 0)` -> Reject with message: "The supplier does not have any outstanding balance to pay."
2. Ensure the payment amount does not exceed the overall supplier balance: `if (amount > supplier.CurrentBalance)` -> Reject with message: "Payment amount cannot be greater than the overall supplier balance."
3. **If Valid:** Decrease `CurrentBalance` by `amount`.

---

## 3. UI/UX Changes
1. **System Payment Methods Module:** The Admin needs a new settings screen to manage global `SystemPaymentMethods` (Add, Update, Toggle Active, Delete). This data should sync from the server to all branches.
2. **Supplier Screen:** Remove the "Payment Methods" tab/section from the individual Supplier details screen, as methods are no longer per-supplier.
3. **Supplier Payment Dialog:** 
   - The dropdown for "Payment Method" should now load from `SystemPaymentMethods` where `IsActive == true`.
   - Ensure the new validation rules (Scenario A & B) are actively handled and display user-friendly error messages if validation fails.
