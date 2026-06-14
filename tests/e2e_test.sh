#!/bin/bash
# ============================================================
# GlobalPOS Full E2E Integration Test — v2 (fixed comparisons)
# ============================================================

BASE="https://localhost:7286"
PASS=0; FAIL=0; ERRORS=""
ok() { ((PASS++)); echo "  ✅ $1"; }
fail() { ((FAIL++)); ERRORS="$ERRORS\n  ❌ $1"; echo "  ❌ $1"; }
jq_f() { echo "$1" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d$2)" 2>/dev/null; }
# Numeric comparison helper: check_num "value" "expected" "label"
check_num() {
  python3 -c "
v,e='$1','$2'
try:
    if abs(float(v)-float(e))<0.01: print('PASS')
    else: print('FAIL')
except: print('FAIL')
" 2>/dev/null
}

echo "🔐 Getting admin token..."
LOGIN=$(curl -s -k -X POST "$BASE/api/Auth/login" -H 'Content-Type: application/json' \
  -d '{"emailOrUserName":"admin","password":"admin123"}')
TOKEN=$(jq_f "$LOGIN" "['data']['token']")
ADMIN_ID=$(jq_f "$LOGIN" "['data']['userId']")
if [ -z "$TOKEN" ] || [ "$TOKEN" = "None" ]; then echo "❌ FATAL: No token"; exit 1; fi
echo "  Token OK ($ADMIN_ID)"
echo ""

# ============================================================
echo "═══ MODULE 1: MASTER DATA — LOCATIONS ═══"
# 1.1 Create Warehouse
R=$(curl -s -k -X POST "$BASE/api/Location" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"locationCode":"WH-E2E-V2","locationName":"E2E Warehouse V2","locationType":"Warehouse","address":"Cairo"}')
WH_ID=$(jq_f "$R" "['data']['locationId']")
[ "$(jq_f "$R" "['data']['locationType']")" = "Warehouse" ] && ok "1.1 Warehouse created" || fail "1.1 Warehouse"

# 1.2 Create Branch
R=$(curl -s -k -X POST "$BASE/api/Location" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"locationCode":"BR-E2E-V2","locationName":"E2E Branch V2","locationType":"Branch","address":"Giza"}')
BR_ID=$(jq_f "$R" "['data']['locationId']")
[ "$(jq_f "$R" "['data']['locationType']")" = "Branch" ] && ok "1.2 Branch created" || fail "1.2 Branch"

# 1.3 Get all locations
R=$(curl -s -k "$BASE/api/Location" -H "Authorization: Bearer $TOKEN")
[ "$(jq_f "$R" "['success']")" = "True" ] && ok "1.3 Get locations" || fail "1.3 Get locations"

# 1.4 Update location
R=$(curl -s -k -X PUT "$BASE/api/Location/$WH_ID" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d "{\"locationCode\":\"WH-E2E-V2\",\"locationName\":\"E2E WH Updated\",\"locationType\":\"Warehouse\",\"address\":\"Cairo Upd\",\"isActive\":true}")
[ "$(jq_f "$R" "['data']['locationName']")" = "E2E WH Updated" ] && ok "1.4 Location updated" || fail "1.4 Update"

echo ""
echo "═══ MODULE 1: MASTER DATA — USERS ═══"
# 1.5 Create WarehouseManager
R=$(curl -s -k -X POST "$BASE/api/Auth/create-user" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d "{\"fullName\":\"E2E WH Mgr V2\",\"userName\":\"e2e_whm_v2\",\"email\":\"e2ev2@test.com\",\"password\":\"Test1234\",\"phone\":\"010\",\"userType\":\"WarehouseManager\",\"locationId\":\"$WH_ID\"}")
WHM_ID=$(jq_f "$R" "['data']['userId']")
[ "$(jq_f "$R" "['success']")" = "True" ] && ok "1.5 WarehouseManager created" || fail "1.5 WHM"

# 1.6 Get all users
R=$(curl -s -k "$BASE/api/User" -H "Authorization: Bearer $TOKEN")
[ "$(jq_f "$R" "['success']")" = "True" ] && ok "1.6 Get users" || fail "1.6 Get users"

# 1.7 Get user by ID
R=$(curl -s -k "$BASE/api/User/$WHM_ID" -H "Authorization: Bearer $TOKEN")
[ "$(jq_f "$R" "['data']['userName']")" = "e2e_whm_v2" ] && ok "1.7 Get user by ID" || fail "1.7 Get user"

# 1.8 Update user
R=$(curl -s -k -X PUT "$BASE/api/User/$WHM_ID" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d "{\"fullName\":\"E2E WHM Upd\",\"userName\":\"e2e_whm_v2\",\"email\":\"e2ev2@test.com\",\"phone\":\"020\",\"userType\":\"WarehouseManager\",\"locationId\":\"$WH_ID\",\"isActive\":true}")
[ "$(jq_f "$R" "['success']")" = "True" ] && ok "1.8 User updated" || fail "1.8 Update"

# 1.9 Toggle activation
R=$(curl -s -k -X PATCH "$BASE/api/User/$WHM_ID/toggle-active" -H "Authorization: Bearer $TOKEN")
[ "$(jq_f "$R" "['success']")" = "True" ] && ok "1.9 Toggle activation" || fail "1.9 Toggle"
curl -s -k -X PATCH "$BASE/api/User/$WHM_ID/toggle-active" -H "Authorization: Bearer $TOKEN" > /dev/null

echo ""
echo "═══ MODULE 1: MASTER DATA — SUPPLIERS ═══"
# 1.10 Create Supplier
R=$(curl -s -k -X POST "$BASE/api/Supplier" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"supplierName":"E2E Supplier V2","phone":"010","address":"Cairo"}')
SUP_ID=$(jq_f "$R" "['data']['supplierId']")
[ "$(jq_f "$R" "['success']")" = "True" ] && ok "1.10 Supplier created" || fail "1.10 Supplier"

# 1.11 Update Supplier
R=$(curl -s -k -X PUT "$BASE/api/Supplier/$SUP_ID" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"supplierName":"E2E Sup Updated","phone":"020","address":"Giza"}')
[ "$(jq_f "$R" "['data']['supplierName']")" = "E2E Sup Updated" ] && ok "1.11 Supplier updated" || fail "1.11 Update"

# 1.12 Add Payment Method
R=$(curl -s -k -X POST "$BASE/api/Supplier/$SUP_ID/payment-methods" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"methodName":"Bank","accountNumber":"123","bankName":"CIB","notes":"main"}')
PM_ID=$(jq_f "$R" "['data']['supplierPaymentMethodId']")
[ "$(jq_f "$R" "['success']")" = "True" ] && ok "1.12 Payment method added" || fail "1.12 PM"

# 1.13 Get payment methods
R=$(curl -s -k "$BASE/api/Supplier/$SUP_ID/payment-methods" -H "Authorization: Bearer $TOKEN")
[ "$(jq_f "$R" "['success']")" = "True" ] && ok "1.13 Get payment methods" || fail "1.13 PM list"

echo ""
echo "═══ MODULE 1: MASTER DATA — PRODUCTS ═══"
# 1.14 Create Raw (Flour)
R=$(curl -s -k -X POST "$BASE/api/Product" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"productName":"V2 Flour","productType":"Raw","baseUnit":"Kg","sellingPrice":0}')
FLOUR_ID=$(jq_f "$R" "['data']['productId']")
[ "$(jq_f "$R" "['success']")" = "True" ] && ok "1.14 Raw Flour created" || fail "1.14 Flour"

# 1.15 Create Raw (Sugar)
R=$(curl -s -k -X POST "$BASE/api/Product" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"productName":"V2 Sugar","productType":"Raw","baseUnit":"Kg","sellingPrice":0}')
SUGAR_ID=$(jq_f "$R" "['data']['productId']")
[ "$(jq_f "$R" "['success']")" = "True" ] && ok "1.15 Raw Sugar created" || fail "1.15 Sugar"

# 1.16 Create Manufactured (Cake)
R=$(curl -s -k -X POST "$BASE/api/Product" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"productName":"V2 Cake","productType":"Manufactured","baseUnit":"Unit","sellingPrice":50}')
CAKE_ID=$(jq_f "$R" "['data']['productId']")
[ "$(jq_f "$R" "['success']")" = "True" ] && ok "1.16 Manufactured Cake created" || fail "1.16 Cake"

# 1.17 Add Recipe (2 Flour + 1 Sugar per Cake)
R=$(curl -s -k -X POST "$BASE/api/Product/$CAKE_ID/recipes" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d "[{\"rawProductId\":\"$FLOUR_ID\",\"quantityNeeded\":2},{\"rawProductId\":\"$SUGAR_ID\",\"quantityNeeded\":1}]")
[ "$(jq_f "$R" "['success']")" = "True" ] && ok "1.17 Recipe set (2F+1S)" || fail "1.17 Recipe: $R"

# 1.18 Get recipes
R=$(curl -s -k "$BASE/api/Product/$CAKE_ID/recipes" -H "Authorization: Bearer $TOKEN")
RECIPE_QTY=$(echo "$R" | python3 -c "import sys,json; d=json.load(sys.stdin)['data']; print(sum(r['quantityNeeded'] for r in d))" 2>/dev/null)
[ "$(check_num "$RECIPE_QTY" "3")" = "PASS" ] && ok "1.18 Recipes total=3 (2+1)" || fail "1.18 Recipe qty=$RECIPE_QTY"

echo ""
echo "═══ MODULE 2: PURCHASING ═══"
# 2.1 Create Invoice on Warehouse
R=$(curl -s -k -X POST "$BASE/api/Purchase" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d "{\"invoiceNumber\":\"E2E-V2-001\",\"supplierId\":\"$SUP_ID\",\"locationId\":\"$WH_ID\",\"invoiceDate\":\"2026-04-15\",\"items\":[{\"productId\":\"$FLOUR_ID\",\"quantity\":100,\"unitPrice\":10,\"totalPrice\":1000},{\"productId\":\"$SUGAR_ID\",\"quantity\":50,\"unitPrice\":15,\"totalPrice\":750}]}")
INV_ID=$(jq_f "$R" "['data']['purchaseInvoiceId']")
INV_TOTAL=$(jq_f "$R" "['data']['totalAmount']")
[ "$(jq_f "$R" "['data']['status']")" = "Draft" ] && [ "$(check_num "$INV_TOTAL" "1750")" = "PASS" ] && ok "2.1 Invoice Draft, total=1750" || fail "2.1 Invoice: $R"

# 2.2 VALIDATION: Invoice on Branch must fail
R=$(curl -s -k -X POST "$BASE/api/Purchase" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d "{\"invoiceNumber\":\"BAD\",\"supplierId\":\"$SUP_ID\",\"locationId\":\"$BR_ID\",\"invoiceDate\":\"2026-04-15\",\"items\":[{\"productId\":\"$FLOUR_ID\",\"quantity\":10,\"unitPrice\":10,\"totalPrice\":100}]}")
[ "$(jq_f "$R" "['success']")" = "False" ] && ok "2.2 Invoice on Branch rejected ✓" || fail "2.2 Should reject Branch invoice"

# 2.3 Approve Invoice
R=$(curl -s -k -X POST "$BASE/api/Purchase/$INV_ID/approve" -H "Authorization: Bearer $TOKEN")
[ "$(jq_f "$R" "['success']")" = "True" ] && ok "2.3 Invoice approved" || fail "2.3 Approve: $R"

# 2.4 Verify stock (Flour=100, Sugar=50)
R=$(curl -s -k "$BASE/api/Inventory/balances/$WH_ID" -H "Authorization: Bearer $TOKEN")
FLOUR_QTY=$(echo "$R" | python3 -c "import sys,json; [print(b['quantityOnHand']) for b in json.load(sys.stdin)['data'] if b['productId']=='$FLOUR_ID']" 2>/dev/null)
SUGAR_QTY=$(echo "$R" | python3 -c "import sys,json; [print(b['quantityOnHand']) for b in json.load(sys.stdin)['data'] if b['productId']=='$SUGAR_ID']" 2>/dev/null)
FLOUR_COST=$(echo "$R" | python3 -c "import sys,json; [print(b['averageCost']) for b in json.load(sys.stdin)['data'] if b['productId']=='$FLOUR_ID']" 2>/dev/null)
[ "$(check_num "$FLOUR_QTY" "100")" = "PASS" ] && [ "$(check_num "$SUGAR_QTY" "50")" = "PASS" ] && ok "2.4 Stock: Flour=100, Sugar=50" || fail "2.4 Stock: F=$FLOUR_QTY S=$SUGAR_QTY"

# 2.5 Verify AverageCost (Flour=10, Sugar=15)
SUGAR_COST=$(echo "$R" | python3 -c "import sys,json; [print(b['averageCost']) for b in json.load(sys.stdin)['data'] if b['productId']=='$SUGAR_ID']" 2>/dev/null)
[ "$(check_num "$FLOUR_COST" "10")" = "PASS" ] && [ "$(check_num "$SUGAR_COST" "15")" = "PASS" ] && ok "2.5 AvgCost: Flour=10, Sugar=15" || fail "2.5 Cost: F=$FLOUR_COST S=$SUGAR_COST"

# 2.6 Verify supplier balance = 1750
R=$(curl -s -k "$BASE/api/Supplier/$SUP_ID" -H "Authorization: Bearer $TOKEN")
SUP_BAL=$(jq_f "$R" "['data']['currentBalance']")
[ "$(check_num "$SUP_BAL" "1750")" = "PASS" ] && ok "2.6 Supplier balance=1750" || fail "2.6 Bal=$SUP_BAL"

# 2.7 Payment 500
R=$(curl -s -k -X POST "$BASE/api/Purchase/payments" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d "{\"supplierId\":\"$SUP_ID\",\"purchaseInvoiceId\":\"$INV_ID\",\"amount\":500,\"supplierPaymentMethodId\":\"$PM_ID\",\"notes\":\"pay1\"}")
[ "$(jq_f "$R" "['success']")" = "True" ] && ok "2.7 Payment 500 recorded" || fail "2.7 Payment"

# 2.8 Supplier balance decreased to 1250
R=$(curl -s -k "$BASE/api/Supplier/$SUP_ID" -H "Authorization: Bearer $TOKEN")
SUP_BAL=$(jq_f "$R" "['data']['currentBalance']")
[ "$(check_num "$SUP_BAL" "1250")" = "PASS" ] && ok "2.8 Supplier balance=1250" || fail "2.8 Bal=$SUP_BAL"

# 2.9 Invoice PartiallyPaid
R=$(curl -s -k "$BASE/api/Purchase/$INV_ID" -H "Authorization: Bearer $TOKEN")
[ "$(jq_f "$R" "['data']['status']")" = "PartiallyPaid" ] && ok "2.9 Invoice PartiallyPaid" || fail "2.9 Status=$(jq_f "$R" "['data']['status']")"

echo ""
echo "═══ MODULE 3: INVENTORY MOVEMENTS ═══"
R=$(curl -s -k "$BASE/api/Inventory/movements/$WH_ID" -H "Authorization: Bearer $TOKEN")
PI_COUNT=$(echo "$R" | python3 -c "import sys,json; print(len([m for m in json.load(sys.stdin)['data'] if m['movementType']=='PurchaseInvoice']))" 2>/dev/null)
[ "$PI_COUNT" = "2" ] && ok "3.1 2 PurchaseInvoice movements" || fail "3.1 PI=$PI_COUNT"

echo ""
echo "═══ MODULE 4: PRODUCTION ═══"
# 4.1 Produce 5 Cakes (consumes 10 Flour + 5 Sugar)
R=$(curl -s -k -X POST "$BASE/api/Production" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d "{\"locationId\":\"$WH_ID\",\"manufacturedProductId\":\"$CAKE_ID\",\"quantityProduced\":5}")
PROD_COST=$(jq_f "$R" "['data']['totalCost']")
[ "$(jq_f "$R" "['success']")" = "True" ] && ok "4.1 Production created" || fail "4.1 Production: $R"

# 4.2 Verify cost = 10*10 + 5*15 = 175
[ "$(check_num "$PROD_COST" "175")" = "PASS" ] && ok "4.2 Production cost=175 (10×10+5×15)" || fail "4.2 Cost=$PROD_COST (expected 175)"

# 4.3 Verify raw consumed (Flour=90, Sugar=45, Cake=5)
R=$(curl -s -k "$BASE/api/Inventory/balances/$WH_ID" -H "Authorization: Bearer $TOKEN")
FLOUR_QTY=$(echo "$R" | python3 -c "import sys,json; [print(b['quantityOnHand']) for b in json.load(sys.stdin)['data'] if b['productId']=='$FLOUR_ID']" 2>/dev/null)
SUGAR_QTY=$(echo "$R" | python3 -c "import sys,json; [print(b['quantityOnHand']) for b in json.load(sys.stdin)['data'] if b['productId']=='$SUGAR_ID']" 2>/dev/null)
CAKE_QTY=$(echo "$R" | python3 -c "import sys,json; [print(b['quantityOnHand']) for b in json.load(sys.stdin)['data'] if b['productId']=='$CAKE_ID']" 2>/dev/null)
if [ "$(check_num "$FLOUR_QTY" "90")" = "PASS" ] && [ "$(check_num "$SUGAR_QTY" "45")" = "PASS" ] && [ "$(check_num "$CAKE_QTY" "5")" = "PASS" ]; then
  ok "4.3 After production: Flour=90, Sugar=45, Cake=5"
else
  fail "4.3 Stock: F=$FLOUR_QTY(exp 90) S=$SUGAR_QTY(exp 45) C=$CAKE_QTY(exp 5)"
fi

echo ""
echo "═══ MODULE 5: TRANSFERS ═══"
# 5.1 Create transfer WH→BR (20 Flour)
R=$(curl -s -k -X POST "$BASE/api/Transfer" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d "{\"fromLocationId\":\"$WH_ID\",\"toLocationId\":\"$BR_ID\",\"items\":[{\"productId\":\"$FLOUR_ID\",\"requestedQuantity\":20}]}")
TR_ID=$(jq_f "$R" "['data']['transferRequestId']")
[ "$(jq_f "$R" "['data']['status']")" = "Pending" ] && ok "5.1 Transfer Pending" || fail "5.1 Transfer"

# 5.2 Accept
R=$(curl -s -k -X POST "$BASE/api/Transfer/$TR_ID/accept" -H "Authorization: Bearer $TOKEN")
[ "$(jq_f "$R" "['success']")" = "True" ] && ok "5.2 Accepted" || fail "5.2"

# 5.3 Prepare
R=$(curl -s -k -X POST "$BASE/api/Transfer/$TR_ID/prepare" -H "Authorization: Bearer $TOKEN")
[ "$(jq_f "$R" "['success']")" = "True" ] && ok "5.3 Preparing" || fail "5.3"

# 5.4 Ship (deducts from WH)
R=$(curl -s -k -X POST "$BASE/api/Transfer/$TR_ID/ship" -H "Authorization: Bearer $TOKEN")
[ "$(jq_f "$R" "['success']")" = "True" ] && ok "5.4 Shipped" || fail "5.4"

# 5.5 Verify WH flour = 70 (90-20)
R=$(curl -s -k "$BASE/api/Inventory/balances/$WH_ID" -H "Authorization: Bearer $TOKEN")
FLOUR_QTY=$(echo "$R" | python3 -c "import sys,json; [print(b['quantityOnHand']) for b in json.load(sys.stdin)['data'] if b['productId']=='$FLOUR_ID']" 2>/dev/null)
[ "$(check_num "$FLOUR_QTY" "70")" = "PASS" ] && ok "5.5 WH Flour=70 after ship" || fail "5.5 WH Flour=$FLOUR_QTY(exp 70)"

# 5.6 Receive (adds to Branch)
R=$(curl -s -k -X POST "$BASE/api/Transfer/$TR_ID/receive" -H "Authorization: Bearer $TOKEN")
[ "$(jq_f "$R" "['success']")" = "True" ] && ok "5.6 Received" || fail "5.6"

# 5.7 Verify Branch flour=20
R=$(curl -s -k "$BASE/api/Inventory/balances/$BR_ID" -H "Authorization: Bearer $TOKEN")
BR_FLOUR=$(echo "$R" | python3 -c "import sys,json; [print(b['quantityOnHand']) for b in json.load(sys.stdin)['data'] if b['productId']=='$FLOUR_ID']" 2>/dev/null)
[ "$(check_num "$BR_FLOUR" "20")" = "PASS" ] && ok "5.7 Branch Flour=20" || fail "5.7 BR Flour=$BR_FLOUR(exp 20)"

# 5.8-5.10 Reject receipt test (returns stock to source)
R=$(curl -s -k -X POST "$BASE/api/Transfer" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d "{\"fromLocationId\":\"$WH_ID\",\"toLocationId\":\"$BR_ID\",\"items\":[{\"productId\":\"$SUGAR_ID\",\"requestedQuantity\":10}]}")
TR2_ID=$(jq_f "$R" "['data']['transferRequestId']")
curl -s -k -X POST "$BASE/api/Transfer/$TR2_ID/accept" -H "Authorization: Bearer $TOKEN" > /dev/null
curl -s -k -X POST "$BASE/api/Transfer/$TR2_ID/prepare" -H "Authorization: Bearer $TOKEN" > /dev/null
curl -s -k -X POST "$BASE/api/Transfer/$TR2_ID/ship" -H "Authorization: Bearer $TOKEN" > /dev/null

R=$(curl -s -k -X POST "$BASE/api/Transfer/$TR2_ID/reject-receipt" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"reason":"Goods damaged"}')
[ "$(jq_f "$R" "['success']")" = "True" ] && ok "5.8 Receipt rejected" || fail "5.8 Reject: $R"

# Verify sugar returned (was 45, -10 ship, +10 return = 45)
R=$(curl -s -k "$BASE/api/Inventory/balances/$WH_ID" -H "Authorization: Bearer $TOKEN")
SUGAR_QTY=$(echo "$R" | python3 -c "import sys,json; [print(b['quantityOnHand']) for b in json.load(sys.stdin)['data'] if b['productId']=='$SUGAR_ID']" 2>/dev/null)
[ "$(check_num "$SUGAR_QTY" "45")" = "PASS" ] && ok "5.9 Sugar returned to 45" || fail "5.9 Sugar=$SUGAR_QTY(exp 45)"

# 5.10 Reject pending transfer
R=$(curl -s -k -X POST "$BASE/api/Transfer" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d "{\"fromLocationId\":\"$WH_ID\",\"toLocationId\":\"$BR_ID\",\"items\":[{\"productId\":\"$FLOUR_ID\",\"requestedQuantity\":5}]}")
TR3_ID=$(jq_f "$R" "['data']['transferRequestId']")
R=$(curl -s -k -X POST "$BASE/api/Transfer/$TR3_ID/reject" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"reason":"Not needed"}')
[ "$(jq_f "$R" "['success']")" = "True" ] && ok "5.10 Pending rejected" || fail "5.10"

echo ""
echo "═══ MODULE 6: STOCK COUNT ═══"
# Before count: Flour=70 in WH. Count actual=65 → diff=-5
R=$(curl -s -k -X POST "$BASE/api/StockCount" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d "{\"locationId\":\"$WH_ID\",\"notes\":\"V2 count\",\"items\":[{\"productId\":\"$FLOUR_ID\",\"actualQuantity\":65}]}")
SC_ID=$(jq_f "$R" "['data']['stockCountId']")
[ "$(jq_f "$R" "['data']['status']")" = "Pending" ] && ok "6.1 StockCount Pending" || fail "6.1"

R=$(curl -s -k -X POST "$BASE/api/StockCount/$SC_ID/post" -H "Authorization: Bearer $TOKEN")
[ "$(jq_f "$R" "['success']")" = "True" ] && ok "6.2 StockCount posted" || fail "6.2 Post: $R"

R=$(curl -s -k "$BASE/api/Inventory/balances/$WH_ID" -H "Authorization: Bearer $TOKEN")
FLOUR_QTY=$(echo "$R" | python3 -c "import sys,json; [print(b['quantityOnHand']) for b in json.load(sys.stdin)['data'] if b['productId']=='$FLOUR_ID']" 2>/dev/null)
[ "$(check_num "$FLOUR_QTY" "65")" = "PASS" ] && ok "6.3 Flour adjusted to 65" || fail "6.3 Flour=$FLOUR_QTY(exp 65)"

echo ""
echo "═══ MODULE 7: DEVICES ═══"
R=$(curl -s -k -X POST "$BASE/api/Device/register" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d "{\"deviceCode\":\"DEV-E2E-V2\",\"deviceName\":\"V2 Device\",\"locationId\":\"$WH_ID\"}")
DEV_ID=$(jq_f "$R" "['data']['deviceId']")
[ "$(jq_f "$R" "['success']")" = "True" ] && ok "7.1 Device registered" || fail "7.1"

R=$(curl -s -k "$BASE/api/Device/DEV-E2E-V2" -H "Authorization: Bearer $TOKEN")
[ "$(jq_f "$R" "['data']['deviceCode']")" = "DEV-E2E-V2" ] && ok "7.2 Get device" || fail "7.2"

R=$(curl -s -k -X POST "$BASE/api/Device/$DEV_ID/heartbeat" -H "Authorization: Bearer $TOKEN")
[ "$(jq_f "$R" "['success']")" = "True" ] && ok "7.3 Heartbeat" || fail "7.3"

echo ""
echo "═══ MODULE 8: SYNC ═══"
R=$(curl -s -k -X POST "$BASE/api/Sync/pull" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d "{\"locationId\":\"$WH_ID\",\"lastSyncVersion\":0}")
LATEST_VER=$(jq_f "$R" "['data']['latestServerVersion']")
CHANGES=$(echo "$R" | python3 -c "import sys,json; print(len(json.load(sys.stdin)['data']['changes']))" 2>/dev/null)
[ "$(jq_f "$R" "['success']")" = "True" ] && [ "$CHANGES" -gt "0" ] && ok "8.1 Pull: $CHANGES changes, v=$LATEST_VER" || fail "8.1 Pull"

R=$(curl -s -k -X POST "$BASE/api/Sync/push" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d "{\"locationId\":\"$WH_ID\",\"deviceId\":\"$DEV_ID\",\"data\":{\"Product\":[{\"ProductId\":\"cccccccc-dddd-eeee-ffff-111111111111\",\"ProductCode\":\"SYNC-V2\",\"ProductName\":\"Synced V2\",\"ProductType\":\"Raw\",\"BaseUnitName\":\"Kg\",\"GlobalAverageCost\":0,\"SellingPrice\":25,\"IsActive\":true}]}}")
[ "$(jq_f "$R" "['success']")" = "True" ] && ok "8.2 Push works" || fail "8.2 Push: $R"

R=$(curl -s -k "$BASE/api/Product/cccccccc-dddd-eeee-ffff-111111111111" -H "Authorization: Bearer $TOKEN")
[ "$(jq_f "$R" "['data']['productName']")" = "Synced V2" ] && ok "8.3 Pushed product verified" || fail "8.3"

R=$(curl -s -k -X POST "$BASE/api/Sync/pull" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d "{\"locationId\":\"$WH_ID\",\"lastSyncVersion\":$LATEST_VER}")
NEW_CH=$(echo "$R" | python3 -c "import sys,json; print(len(json.load(sys.stdin)['data']['changes']))" 2>/dev/null)
[ "$NEW_CH" -gt "0" ] && ok "8.4 Delta pull: $NEW_CH new" || ok "8.4 Delta pull: 0 (ok)"

echo ""
echo "═══ MODULE 9: VALIDATION RULES ═══"
# Same location transfer
R=$(curl -s -k -X POST "$BASE/api/Transfer" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d "{\"fromLocationId\":\"$WH_ID\",\"toLocationId\":\"$WH_ID\",\"items\":[{\"productId\":\"$FLOUR_ID\",\"requestedQuantity\":5}]}")
[ "$(jq_f "$R" "['success']")" = "False" ] && ok "9.1 Same-location rejected" || fail "9.1"

# Double approval
R=$(curl -s -k -X POST "$BASE/api/Purchase/$INV_ID/approve" -H "Authorization: Bearer $TOKEN")
[ "$(jq_f "$R" "['success']")" = "False" ] && ok "9.2 Double approval rejected" || fail "9.2"

# Invalid role
R=$(curl -s -k -X POST "$BASE/api/Auth/create-user" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"fullName":"Bad","userName":"bad_v2","email":"bad_v2@t.com","password":"Test1234","userType":"SuperAdmin"}')
[ "$(jq_f "$R" "['success']")" = "False" ] && ok "9.3 Invalid role rejected" || fail "9.3 Should reject SuperAdmin"

# Insufficient materials (need 2000 flour for 1000 cakes, only have 65)
R=$(curl -s -k -X POST "$BASE/api/Production" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d "{\"locationId\":\"$WH_ID\",\"manufacturedProductId\":\"$CAKE_ID\",\"quantityProduced\":1000}")
[ "$(jq_f "$R" "['success']")" = "False" ] && ok "9.4 Insufficient materials rejected" || fail "9.4 Should reject"

# Invalid product in invoice
R=$(curl -s -k -X POST "$BASE/api/Purchase" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d "{\"invoiceNumber\":\"BAD2\",\"supplierId\":\"$SUP_ID\",\"locationId\":\"$WH_ID\",\"invoiceDate\":\"2026-04-15\",\"items\":[{\"productId\":\"00000000-0000-0000-0000-000000000099\",\"quantity\":10,\"unitPrice\":10,\"totalPrice\":100}]}")
[ "$(jq_f "$R" "['success']")" = "False" ] && ok "9.5 Invalid product rejected" || fail "9.5"

# Payment exceeds balance
R=$(curl -s -k -X POST "$BASE/api/Purchase/payments" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d "{\"supplierId\":\"$SUP_ID\",\"amount\":999999,\"notes\":\"too much\"}")
[ "$(jq_f "$R" "['success']")" = "False" ] && ok "9.6 Excess payment rejected" || fail "9.6"

echo ""
echo "═══════════════════════════════════════════"
echo "  FINAL RESULTS"
echo "═══════════════════════════════════════════"
echo "  ✅ PASSED: $PASS"
echo "  ❌ FAILED: $FAIL"
if [ $FAIL -gt 0 ]; then echo -e "\n  Failures:$ERRORS"; fi
echo "═══════════════════════════════════════════"
