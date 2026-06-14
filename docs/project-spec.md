System Overview
النظام عبارة عن Desktop Application WPF لكل الفروع والمخازن.
الـ Backend الرئيسي على السيرفر هيكون ASP.NET Core Web API.
الـ Main Database على السيرفر هيكون SQL Server.
كل جهاز في كل فرع أو مخزن هيكون عليه Local SQLite Database علشان يقدر يشتغل أوفلاين.
نفس البرنامج يشتغل في كل الأماكن، والاختلاف فقط يكون حسب:
نوع المستخدم
المكان المرتبط به المستخدم
النظام في المرحلة الحالية يغطي إدارة المخازن بالكامل، وبعدها يتبني عليه POS / Cashier.
Main Business Goal
إدارة مخزون مطعم كبير فيه أكثر من 4 فروع ومخازن.
دعم العمل Online + Offline.
تسجيل كل الحركات المهمة:
مشتريات
دفعات الموردين
المخزون
التصنيع
التحويلات
الجرد
مزامنة البيانات بين الفروع والمخازن والسيرفر بمجرد رجوع الإنترنت.
الحفاظ على الأداء العالي والثبات مع عدد كبير من العمليات.
Core Operating Principle
التطبيق يعمل Local-First.
أي شاشة تقرأ من SQLite المحلي.
أي عملية جديدة تتسجل محليًا أولًا.
لو الإنترنت موجود:
تتزامن مع السيرفر مباشرة أو شبه مباشرة.
لو الإنترنت غير موجود:
تظل العملية محفوظة محليًا
وتدخل في queue للمزامنة
وعند رجوع الإنترنت تترفع للسيرفر ثم يتم تنزيل التحديثات الجديدة.
User Roles
Admin
يرى كل الفروع والمخازن
يدير كل البيانات
يدير المستخدمين
يراجع التقارير
BranchManager
يدير الفرع المرتبط به فقط
يرى مخزون الفرع والتحويلات والجرد والعمليات الخاصة بفرعه
WarehouseManager
يدير المخزن المرتبط به فقط
يعتمد التحويلات والتجهيزات والتصنيع والجرد داخل مخزنه
Cashier
يشتغل على جزء الكاشير فقط داخل الفرع
لا يدير المخزون بالكامل
WarehouseWorker
يشتغل على العمليات اليومية للمخزن
مثل تجهيز الطلبات، الاستلام، الجرد، إدخال الحركات حسب الصلاحية
Main Modules
1) Master Data
Locations
Users
Suppliers
System Payment Methods
Products
Product Recipes
2) Purchasing
Purchase Invoices
Purchase Invoice Items
Supplier Payments
3) Inventory
Stock Balances
Stock Movements
4) Production
Production Operations
Production Operation Items
5) Transfers
Transfer Requests
Transfer Request Items
6) Stock Count
Stock Counts
Stock Count Items
7) Audit / Tracking
Audit Logs
8) Sync & Device Management
Devices
ServerChangeLog
SyncOutbox
SyncState
LocalDeviceSettings
Locations Concept
أي مكان في النظام اسمه Location.
الـ Location يكون نوعه:
Branch
Warehouse
كل العمليات الأساسية مرتبطة بـ Location:
المخزون
الجرد
التحويلات
المشتريات
المستخدمين
Product and Cost Concept
المنتج قد يكون:
Raw
Manufactured
كل منتج له وحدة أساسية واحدة.
سعر المنتج غير ثابت.
لذلك النظام يعتمد على:
UnitPrice في فاتورة الشراء = السعر الحقيقي وقت الشراء
StockBalances.AverageCost = متوسط التكلفة الحقيقي داخل كل مكان
Products.GlobalAverageCost = متوسط عام للعرض فقط
Products.SellingPrice = سعر البيع إن وجد
Supplier Flow
Supplier Invoice
المستخدم يسجل فاتورة شراء من المورد.
يحدد:
المورد
التاريخ
المخزن المستلم
الأصناف والكميات والأسعار
عند اعتماد الفاتورة:
يتم إضافة الأصناف للمخزون
يتم إنشاء حركات مخزون PurchaseIn
يتم تحديث StockBalances
يتم زيادة Supplier.CurrentBalance
Supplier Payment
المستخدم يسجل دفعة للمورد.
الدفعة يجب أن تسجل باستخدام إحدى "طرق الدفع العامة" المعرفة في النظام (System Payment Methods) والتي يتم إدارتها بواسطة الـ Admin.
الدفعة قد تكون:
على فاتورة معينة (Scenario A):
يتم التحقق من أن الفاتورة غير مدفوعة بالكامل والمبلغ لا يتجاوز المتبقي.
عند تسجيل الدفعة تقل RemainingAmount وتتحدث حالة الفاتورة (Paid/PartiallyPaid).
تقل مديونية المورد (CurrentBalance).
أو دفعة عامة على المورد مباشرتاً (Scenario B):
يتم التحقق من أن المورد له مديونية باقية وأن المبلغ لا يتجاوزها.
عند تسجيل الدفعة تقل مديونية المورد الإجمالية.
Inventory Flow
StockBalances يمثل الرصيد الحالي السريع لكل منتج في كل مكان.
StockMovements يمثل دفتر الحركة الكامل.
أي تغيير في المخزون لازم ينتج عنه:
تحديث StockBalances
وإضافة سطر في StockMovements
أنواع الحركات الأساسية:
PurchaseIn
TransferOut
TransferIn
ProductionConsume
ProductionIn
StockCountAdjustment
Waste
Production Flow
المخزن يقدر يصنع منتج مصنع من خامات.
كل منتج مصنع له recipe بسيطة.
عند تنفيذ عملية تصنيع:
يتم اختيار المنتج المصنع
تحديد الكمية المنتجة
تسجيل الخامات المستهلكة
عند اعتماد التصنيع:
الخامات تقل من المخزون
المنتج المصنع يزيد في المخزون
يتم حساب تكلفة المنتج المصنع من مجموع تكلفة الخامات المستهلكة
يتم تسجيل حركات مخزون مناسبة
Transfer Flow
Transfer Type
Warehouse → Branch
Warehouse → Warehouse
Online Flow
الجهة الطالبة تنشئ Transfer Request.
الطلب يصل لحظيًا للطرف الآخر عبر SignalR.
المخزن المرسل يراجع الطلب.
يمكنه:
Accept
Reject
إذا تم القبول:
يبدأ تجهيز الطلب
ويتفتح تايمر عند ال طالب الطلب وال بيعمل الطلب
تتحدث الحالة تدريجيًا:
Pending
Accepted
Preparing
Ready
InTransit
Received
الجهة المستلمة تؤكد الاستلام أو ترفض الاستلام.
عند الاستلام:
يخرج المخزون من المصدر
يدخل المخزون إلى الجهة المستلمة
وبيكون قدام المستلم يقبلو او يرفضو لو هيرفضو بيكون قدامو يحط السبب وبيكون اكتيار من 3 1- البضاعه ناقصه 2- البضاعه فاسده 3- اخري مع امكانيه كتابه السبب
Offline Flow
إذا أحد الطرفين أو كلاهما أوفلاين:
يتم التنسيق هاتفيًا
المخزن المرسل ينشئ الطلبية محليًا
النظام يولد TransferCode فريد
يمكن طباعة ورقة بها بيانات الطلبية والكود
عند الاستلام يتم إدخال الكود في الجهة المستلمة
بعد رجوع الإنترنت يتم ربط العمليات ورفعها للسيرفر واستكمال الحالة
Transfer Status Tracking
كل طلبية لها:
RequestDate
PreparedAt
ShippedAt
ReceivedAt
منها يمكن حساب:
وقت التجهيز
وقت النقل
الوقت الكلي للطلبية
لو الاستلام مرفوض:
يتم حفظ السبب
Stock Count Flow
الجرد يتم على أي Location.
المستخدم يبدأ جلسة جرد.
يسجل الكميات الفعلية.
النظام يقارن:
SystemQty
ActualQty
يحسب الفرق.
عند اعتماد الجرد:
ينزل فرق الجرد كحركة مخزون
يتم تحديث الرصيد الحالي
Audit Flow
كل العمليات المهمة تسجل في AuditLogs.
أمثلة:
إنشاء فاتورة
تسجيل دفعة
اعتماد تحويل
رفض استلام
اعتماد جرد
تنفيذ تصنيع
الهدف:
التتبع
المراجعة
معرفة من عمل ماذا ومتى
Online / Offline Architecture
Server Side
ASP.NET Core Web API
SQL Server
Business database
Authentication / JWT
OTP password reset
ServerChangeLog
Devices
SignalR Hub
Client Side
WPF desktop application
Local SQLite database
Local data access
Sync service
Local login support
SignalR client when online
Sync Flow
Local Write
أي عملية تتسجل في SQLite أولًا.
العملية تتسجل أيضًا في SyncOutbox لو تحتاج رفع.
Push
عند توفر الإنترنت:
التطبيق يرفع العمليات pending من SyncOutbox
يرسلها للسيرفر كـ business operations
Pull
بعد الرفع:
التطبيق يطلب من السيرفر كل التغييرات الجديدة بعد آخر ChangeVersion
ثم يحدّث SQLite المحلي
Sync Order
Push first
Then Pull
Authentication and Security
Online login:
باستخدام API + JWT
Offline login:
باستخدام بيانات المستخدم المخزنة محليًا
مع PasswordHash
Password reset:
Online only via OTP to email
كل الاتصال مع السيرفر عبر HTTPS
لا يتم تخزين كلمات المرور plain text
استخدام hashing آمن
الصلاحيات تعتمد على:
UserType
LocationId
Realtime Features
SignalR يستخدم فقط في حالة Online
الاستخدامات:
إشعار الطلبات والتحويلات لحظيًا
تحديث حالات الطلبية
معرفة الفروع/المخازن الأونلاين
إشعارات الإدارة
Validation Rules
الفاتورة تدخل على Warehouse فقط
التحويل يبدأ من Warehouse فقط
التحويل يمكن أن ينتهي إلى Warehouse أو Branch
BranchManager لازم يكون مرتبط بـ Branch
WarehouseManager و WarehouseWorker لازم يكونوا مرتبطين بـ Warehouse
Cashier لازم يكون مرتبط بـ Branch
أي تعديل في المخزون لازم ينتج عنه StockMovement
لا يتم حذف المستندات التشغيلية حذفًا فعليًا
العمليات المركبة تنفذ داخل database transaction
Performance and Stability Requirements
استخدام GUID keys لتجنب مشاكل المزامنة
استخدام indexes على الحقول كثيرة البحث
استخدام local-first approach لسرعة الواجهة
عدم تحميل أرشيف ضخم جدًا محليًا على كل جهاز
الاحتفاظ محليًا بالداتا التشغيلية المهمة فقط
السيرفر هو المصدر النهائي الكامل للتاريخ
SQLite تستخدم للتشغيل اليومي والأوفلاين فقط
What Antigravity Should Build First
Shared business entities
Server DbContext
Local DbContext
Migrations for SQL Server
Migrations for SQLite
Sync models and sync service
Authentication and authorization layer
Purchasing module
Inventory module
Transfer module
Stock count module
Production module
Audit logging
SignalR integration
Offline-safe transactions
Final Expected Result
نفس البرنامج يعمل في كل الفروع والمخازن
نفس الداتا الأساسية متاحة في كل مكان حسب آخر مزامنة
الاختلاف فقط حسب المستخدم وصلاحيته
إذا انقطع الإنترنت:
الجهاز يكمل شغل طبيعي محليًا
إذا عاد الإنترنت:
ترفع البيانات الجديدة
وتنزل تحديثات السيرفر
الأدمن يقدر يفتح من أي جهاز ويراجع النظام كله حسب الصلاحيات وحالة الاتصال


----
المشروع ال انتا فيه حاليا هيكون ال web api project وال هيكون علي السيرفر

----

## Performance & Scalability Standards
### هذه المعايير إلزامية لكل Feature جديدة في المشروع

#### 1. قاعدة البيانات والاستعلامات
- لا يتم جلب أكثر من 50 سطراً في أي قائمة بدون Pagination.
- الفلترة والبحث يتمان دائماً على مستوى SQL (Server-Side) وليس في الذاكرة.
- يُمنع استخدام `foreach` يحتوي داخله على استعلام لقاعدة البيانات (N+1 Query Problem).
- استخدام `AsNoTracking()` لكل استعلامات القراءة (Read-Only).
- استخدام SQL JOIN عبر EF Core بدل الربط اليدوي في الذاكرة.
- استخدام `ExecuteUpdateAsync` و `ExecuteDeleteAsync` للعمليات الكمية بدل Load + SaveChanges.
- كل جدول جديد يجب أن يحتوي على Indexes على الحقول التي يتم البحث أو الفرز بها.

#### 2. تحميل البيانات في الواجهة (ViewModel)
- البيانات الثابتة (Lookups) مثل المخازن وطرق الدفع والموردين: تُحمَّل مرة واحدة عند فتح الشاشة وتُخزَّن في الذاكرة.
- لا يُعاد تحميل الـ Lookups مع كل بحث أو فلترة.
- يُضاف زر Refresh صغير (⟳) بجانب القوائم المنسدلة للتحديث اليدوي عند الحاجة.
- بعد عمليات الحفظ/التحديث/الحذف: يُحدَّث العنصر مباشرة في الـ `ObservableCollection` (Local Update) بدل إعادة جلب كل البيانات من DB.
- كل عملية تستغرق وقتاً طويلاً تُنفَّذ في `Task.Run` أو `async/await` مع `ConfigureAwait(false)`.

#### 3. واجهة المستخدم (XAML)
- كل `DataGrid` يحتوي على `EnableRowVirtualization="True"` و `EnableColumnVirtualization="True"`.
- استخدام `VirtualizingStackPanel.IsVirtualizing="True"` في كل `ListBox` و `ListView`.
- كل شاشة تحتوي على أرقام الصفحات (Page 1 of N) مع أزرار التنقل (السابق / التالي).
- مربعات البحث تعمل بنظام Debounce (300ms تأخير) قبل إرسال أي استعلام.

#### 4. التصفح (Pagination)
- الحد الافتراضي: 50 سطر لكل صفحة.
- يجب أن يعمل الـ Skip/Take على مستوى SQL وليس بعد جلب كل البيانات.
- Total Count يُحسَّب بـ `CountAsync()` قبل جلب البيانات.

#### 5. SQLite المحلية
- SQLite مخصصة للتشغيل اليومي فقط (آخر 90 يوم من الحركات والعمليات).
- البيانات الأقدم من 90 يوم تُؤرشف على السيرفر (SQL Server).
- التقارير التاريخية الشاملة تُطلب من الـ Web API عبر الاتصال بالإنترنت.

#### 6. مزامنة البيانات (Sync)
- لا تُجرى عمليات مزامنة ثقيلة على الـ UI Thread.
- الـ Sync يعمل في خلفية منفصلة (Background Service).
- كل عملية جديدة تُضاف للـ `SyncOutbox` محلياً ثم تُرسل للسيرفر عند توفر الإنترنت.

#### 7. التكلفة والأسعار
- `RecalculateAverageCostAsync` تجمع كل البيانات المطلوبة بـ IN Query واحد قبل أي loop.
- يُمنع استدعاء قاعدة البيانات داخل `foreach` عند إعادة حساب التكلفة.
- `GlobalAverageCost` يُحسَّب من الأرصدة الموجودة فقط ولا يُصفَّر عند نفاد المخزون.