# حذف عملية التصنيع — مواصفات الخادم (Web API Parity Spec)
### للـ AI Agent المسؤول عن بناء Web API

---

## 1. نظرة عامة

> **تحذير:** حذف عملية التصنيع **ليس** مجرد soft-delete.  
> هو عملية **عكس كامل للمخزون** — كأن العملية لم تحدث أبداً.

---

## 2. Endpoint المطلوب

```
DELETE /api/production/{operationId}
Authorization: Bearer {token}
Roles allowed: Admin, WarehouseManager
```

**Response:**
```json
{ "message": "تم إلغاء عملية التصنيع بنجاح" }
```

---

## 3. الخطوات التفصيلية (Transaction واحدة)

كل الخطوات التالية **لازم** تتنفذ داخل SQL Transaction واحدة.

### الخطوة 1 — التحقق من وجود العملية
- جلب `ProductionOperation` مع جميع `ProductionOperationItems` المرتبطة بها.
- إذا لم توجد أو `IsDeleted = true` → خطأ 404: `"عملية التصنيع غير موجودة أو تم حذفها مسبقاً."`

### الخطوة 2 — فحص المخزون (Safety Guard)
- جلب `StockBalance` للمنتج المصنّع في الموقع الأصلي للعملية.
- **شرط:** `StockBalance.QuantityOnHand >= ProductionOperation.QuantityProduced`
- إذا فشل الشرط → خطأ 422:  
  `"لا يمكن إلغاء العملية — الكمية المصنوعة ({X}) غير متوفرة في المخزن (المتاح: {Y})."`

### الخطوة 3 — خصم المنتج المصنّع + إعادة حساب متوسط تكلفته ⚠️ حرج
إضافة حركة مخزون:
- `ProductId` = المنتج المصنّع
- `LocationId` = الموقع الأصلي للعملية
- `QtyOut` = `QuantityProduced`
- `MovementType` = `"ProductionCancelOut"`
- `ReferenceId` = `ProductionOperationId`

**تحديث `StockBalance` للمنتج المصنّع:**
```
newQty = QuantityOnHand - QuantityProduced

إذا newQty > 0:
    AverageCost = (QuantityOnHand × CurrentAvgCost − QuantityProduced × op.UnitCost) / newQty
    (لو النتيجة سالبة → اجعلها 0)

إذا newQty ≤ 0:
    AverageCost = 0

QuantityOnHand = newQty
```

> **سبب هذا الحساب:** عند إنشاء العملية، تم دمج تكلفة الدفعة المصنوعة في المتوسط المرجح. عند الإلغاء يجب **عكس** هذا الدمج لاسترداد المتوسط السابق.

### الخطوة 4 — إعادة المواد الخام للمخزون + إعادة حساب متوسطها
لكل `ProductionOperationItem`:
- إضافة حركة مخزون:
  - `ProductId` = `RawProductId`
  - `LocationId` = الموقع الأصلي
  - `QtyIn` = `QuantityConsumed`
  - `UnitCost` = `UnitCost` المحفوظ في عنصر العملية
  - `MovementType` = `"ProductionCancelReturn"`
  - `ReferenceId` = `ProductionOperationId`

**تحديث `StockBalance` للمادة الخام:**
```
newQty = QuantityOnHand + QuantityConsumed

إذا newQty > 0:
    AverageCost = (QuantityOnHand × CurrentAvgCost + QuantityConsumed × item.UnitCost) / newQty

QuantityOnHand = newQty
```

- تحديث `ProductionOperationItem.IsDeleted = true`

---

## 6. ملخص معادلات المتوسط المرجح

| الحالة | المعادلة |
|---|---|
| **إنشاء إنتاج** (إضافة منتج مصنّع) | `NewAvg = (OldQty × OldAvg + NewQty × NewCost) / (OldQty + NewQty)` |
| **إلغاء إنتاج** (خصم منتج مصنّع) | `OldAvg = (CurrentQty × CurrentAvg − RemovedQty × RemovedCost) / (CurrentQty − RemovedQty)` |
| **إعادة خام** (إضافة مادة خام) | `NewAvg = (OldQty × OldAvg + ReturnedQty × ReturnedCost) / (OldQty + ReturnedQty)` |

> **⚠️ تحذير:** تجاهل إعادة حساب `AverageCost` عند الإلغاء يُبقي سعر المنتج المصنّع مرتفعاً خطأً — وهو الخطأ الذي تم إصلاحه في هذا التحديث.

### الخطوة 5 — Soft Delete للعملية
```sql
UPDATE ProductionOperations
SET IsDeleted = 1, UpdatedAt = GETUTCDATE()
WHERE ProductionOperationId = @id
```

### الخطوة 6 — تسجيل في Audit Log
```
Action: "DeleteProduction"
Entity: "ProductionOperations"
EntityId: {operationId}
PerformedBy: {userId from token}
```

### الخطوة 7 — Commit
- Commit Transaction.

---

## 4. قواعد العمل

| القاعدة | التطبيق |
|---|---|
| **الصلاحيات** | `Admin` أو `WarehouseManager` فقط |
| **فحص المخزون** | **إلزامي** — لا تتخطّى الخطوة 2 تحت أي ظرف |
| **Atomicity** | Transaction واحدة — أي خطأ → Rollback كامل |
| **Soft Delete فقط** | لا تحذف صفوف من قاعدة البيانات — فقط `IsDeleted = true` |
| **التكاليف** | استخدم `UnitCost` المحفوظ وقت الإنتاج (وليس السعر الحالي) |

---

## 5. أنواع حركات المخزون

```csharp
"ProductionCancelOut"    // خصم المنتج المصنّع
"ProductionCancelReturn" // إعادة المواد الخام
```

---

## 6. تحديث حساب المتوسط المرجح عند إعادة الخامات

عند إعادة المواد الخام، يجب إعادة حساب `AverageCost` في `StockBalances`:

```
NewAverageCost = (CurrentQty * CurrentAvgCost + ReturnedQty * ReturnedUnitCost)
                 / (CurrentQty + ReturnedQty)
```

---

## 7. استراتيجية المزامنة (Sync)

عند نجاح العملية على الخادم، إذا كانت هناك سجلات في `SyncOutbox` من التطبيق المحلي:
- حذف أو تحديث حالة `SyncOutbox` لـ `EntityId = operationId` إلى `"Synced"`.

---

## 8. ملاحظة للـ Desktop Agent (Local Logic)

التطبيق المحلي يستخدم `IProductionService.CancelOperationAsync(Guid operationId)` التي تطبق نفس هذا المنطق على قاعدة بيانات SQLite المحلية. تأكد من أن التطبيق المحلي:
1. يتحقق من المخزون قبل الإلغاء.
2. يعكس حركات المخزون.
3. يضيف سجلاً في `SyncOutbox` بـ `Operation = "DELETE"` لمزامنته لاحقاً مع الخادم.
