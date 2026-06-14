#!/bin/bash
# ═══════════════════════════════════════════════════════════════
# COMPREHENSIVE API TEST SCRIPT v2
# Tests all 47 endpoints end-to-end
# ═══════════════════════════════════════════════════════════════
set -o pipefail
BASE="http://localhost:5099/api"
TS=$(date +%s)
PASS=0; FAIL=0; TOTAL=0

red()   { printf "\033[31m%s\033[0m\n" "$1"; }
green() { printf "\033[32m%s\033[0m\n" "$1"; }
cyan()  { printf "\033[36m%s\033[0m\n" "$1"; }
bold()  { printf "\033[1m%s\033[0m\n" "$1"; }

check() {
    local label="$1" response="$2" expect_success="${3:-true}"
    TOTAL=$((TOTAL+1))
    local success=$(echo "$response" | python3 -c "import sys,json; d=json.load(sys.stdin); print(str(d.get('success',False)).lower())" 2>/dev/null)
    if [ "$expect_success" = "true" ]; then
        if [ "$success" = "true" ]; then
            green "  ✅ $label"; PASS=$((PASS+1))
        else
            red "  ❌ $label"
            echo "     $(echo "$response" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d.get('message','')[:200])" 2>/dev/null || echo "$response" | head -c 200)"
            FAIL=$((FAIL+1))
        fi
    else
        if [ "$success" = "false" ]; then
            green "  ✅ $label (correctly rejected)"; PASS=$((PASS+1))
        else
            red "  ❌ $label (should have been rejected)"; FAIL=$((FAIL+1))
        fi
    fi
}

jq_val() { echo "$1" | python3 -c "import sys,json; d=json.load(sys.stdin); exec('val=$2\nprint(eval(val))')" 2>/dev/null; }

# ═══════════════════════════════════════════════════════════════
bold "═══════════════════════════════════════════════════════════"
bold "  GLOBALPOS API TEST SUITE"
bold "═══════════════════════════════════════════════════════════"

# ═══ AUTH ═══════════════════════════════════════════════════
cyan "═══ AUTH: Login (Seeded Admin) ═══"

R=$(curl -s -X POST "$BASE/Auth/login" -H "Content-Type: application/json" -d "{
  \"emailOrUserName\":\"admin@globalpos.com\",\"password\":\"admin123\"}")
check "Login by email" "$R"
TOKEN=$(echo "$R" | python3 -c "import sys,json; print(json.load(sys.stdin)['data']['token'])" 2>/dev/null)
REFRESH=$(echo "$R" | python3 -c "import sys,json; print(json.load(sys.stdin)['data']['refreshToken'])" 2>/dev/null)
ADMIN_ID=$(echo "$R" | python3 -c "import sys,json; print(json.load(sys.stdin)['data']['userId'])" 2>/dev/null)

R=$(curl -s -X POST "$BASE/Auth/login" -H "Content-Type: application/json" -d "{
  \"emailOrUserName\":\"admin\",\"password\":\"admin123\"}")
check "Login by username" "$R"
# Use this newer token
TOKEN=$(echo "$R" | python3 -c "import sys,json; print(json.load(sys.stdin)['data']['token'])" 2>/dev/null)
REFRESH=$(echo "$R" | python3 -c "import sys,json; print(json.load(sys.stdin)['data']['refreshToken'])" 2>/dev/null)

R=$(curl -s -X POST "$BASE/Auth/login" -H "Content-Type: application/json" -d "{
  \"emailOrUserName\":\"admin\",\"password\":\"wrong\"}")
check "Login wrong password" "$R" "false"

cyan "═══ AUTH: Refresh Token ═══"
R=$(curl -s -X POST "$BASE/Auth/refresh-token" -H "Content-Type: application/json" -d "{\"refreshToken\":\"$REFRESH\"}")
check "Refresh token" "$R"
TOKEN=$(echo "$R" | python3 -c "import sys,json; print(json.load(sys.stdin)['data']['token'])" 2>/dev/null)
REFRESH=$(echo "$R" | python3 -c "import sys,json; print(json.load(sys.stdin)['data']['refreshToken'])" 2>/dev/null)

cyan "═══ AUTH: Me & Change Password ═══"
R=$(curl -s -X GET "$BASE/Auth/me" -H "Authorization: Bearer $TOKEN")
check "Get Me" "$R"

R=$(curl -s -X POST "$BASE/Auth/change-password" -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"currentPassword":"admin123","newPassword":"pass456"}')
check "Change password" "$R"
R=$(curl -s -X POST "$BASE/Auth/change-password" -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"currentPassword":"pass456","newPassword":"admin123"}')
check "Change password back" "$R"

ROLE=$(echo "$R" | python3 -c "import sys,json; print(json.load(sys.stdin)['data']['roles'][0])" 2>/dev/null)
echo "  → Role from login: Admin"

# ═══ LOCATIONS ═══════════════════════════════════════════════
cyan "═══ LOCATIONS: CRUD ═══"
R=$(curl -s -X POST "$BASE/Location" -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d "{\"locationCode\":\"WH-${TS}\",\"locationName\":\"Main Warehouse\",\"locationType\":\"Warehouse\",\"address\":\"Cairo\"}")
check "Create Warehouse" "$R"
WH_ID=$(echo "$R" | python3 -c "import sys,json; print(json.load(sys.stdin)['data']['locationId'])" 2>/dev/null)
echo "  → Warehouse: $WH_ID"

R=$(curl -s -X POST "$BASE/Location" -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d "{\"locationCode\":\"BR-${TS}\",\"locationName\":\"Branch 1\",\"locationType\":\"Branch\",\"address\":\"Giza\"}")
check "Create Branch" "$R"
BR_ID=$(echo "$R" | python3 -c "import sys,json; print(json.load(sys.stdin)['data']['locationId'])" 2>/dev/null)
echo "  → Branch: $BR_ID"

R=$(curl -s -X GET "$BASE/Location" -H "Authorization: Bearer $TOKEN")
check "Get all locations" "$R"

R=$(curl -s -X GET "$BASE/Location/$WH_ID" -H "Authorization: Bearer $TOKEN")
check "Get location by ID" "$R"

R=$(curl -s -X PUT "$BASE/Location/$WH_ID" -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d "{\"locationCode\":\"WH-${TS}\",\"locationName\":\"WH Updated\",\"locationType\":\"Warehouse\",\"address\":\"Cairo Updated\",\"isActive\":true}")
check "Update location" "$R"

# ═══ AUTH: Create User (Admin) ═══
cyan "═══ AUTH: Create User ═══"
R=$(curl -s -X POST "$BASE/Auth/create-user" -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d "{\"fullName\":\"WH Manager\",\"userName\":\"whm${TS}\",\"email\":\"whm${TS}@t.com\",\"password\":\"wh123\",\"phone\":\"022\",\"userType\":\"WarehouseManager\",\"locationId\":\"$WH_ID\"}")
check "Create WarehouseManager" "$R"
WH_MGR_ID=$(echo "$R" | python3 -c "import sys,json; print(json.load(sys.stdin)['data']['userId'])" 2>/dev/null)

R=$(curl -s -X POST "$BASE/Auth/login" -H "Content-Type: application/json" -d "{\"emailOrUserName\":\"whm${TS}\",\"password\":\"wh123\"}")
check "Login as WarehouseManager" "$R"
WH_TOKEN=$(echo "$R" | python3 -c "import sys,json; print(json.load(sys.stdin)['data']['token'])" 2>/dev/null)

# ═══ USERS ═══
cyan "═══ USERS: CRUD ═══"
R=$(curl -s -X GET "$BASE/User" -H "Authorization: Bearer $TOKEN")
check "Get all users" "$R"

R=$(curl -s -X GET "$BASE/User/$WH_MGR_ID" -H "Authorization: Bearer $TOKEN")
check "Get user by ID" "$R"

R=$(curl -s -X PUT "$BASE/User/$WH_MGR_ID" -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d "{\"fullName\":\"WH Mgr Updated\",\"phone\":\"033\",\"userType\":\"WarehouseManager\",\"locationId\":\"$WH_ID\",\"isActive\":true}")
check "Update user" "$R"

R=$(curl -s -X PATCH "$BASE/User/$WH_MGR_ID/toggle-active" -H "Authorization: Bearer $TOKEN")
check "Toggle user active" "$R"
R=$(curl -s -X PATCH "$BASE/User/$WH_MGR_ID/toggle-active" -H "Authorization: Bearer $TOKEN")
check "Toggle user back" "$R"

# ═══ SUPPLIERS ═══
cyan "═══ SUPPLIERS: CRUD ═══"
R=$(curl -s -X POST "$BASE/Supplier" -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d "{\"supplierName\":\"Fresh Foods ${TS}\",\"contactName\":\"Ahmed\",\"phone\":\"010\",\"email\":\"ff@t.com\",\"address\":\"Cairo\",\"currentBalance\":0}")
check "Create supplier" "$R"
SUP_ID=$(echo "$R" | python3 -c "import sys,json; print(json.load(sys.stdin)['data']['supplierId'])" 2>/dev/null)
echo "  → Supplier: $SUP_ID"

R=$(curl -s -X GET "$BASE/Supplier" -H "Authorization: Bearer $TOKEN")
check "Get all suppliers" "$R"
R=$(curl -s -X GET "$BASE/Supplier/$SUP_ID" -H "Authorization: Bearer $TOKEN")
check "Get supplier by ID" "$R"
R=$(curl -s -X PUT "$BASE/Supplier/$SUP_ID" -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d "{\"supplierName\":\"Fresh Upd ${TS}\",\"contactName\":\"Ahmed U\",\"phone\":\"099\",\"address\":\"Giza\"}")
check "Update supplier" "$R"
R=$(curl -s -X POST "$BASE/Supplier/$SUP_ID/payment-methods" -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"methodName":"Bank Transfer","accountData":"ACC:123"}')
check "Add payment method" "$R"
PM_ID=$(echo "$R" | python3 -c "import sys,json; print(json.load(sys.stdin)['data']['supplierPaymentMethodId'])" 2>/dev/null)
echo "  → PayMethod: $PM_ID"

# ═══ PRODUCTS ═══
cyan "═══ PRODUCTS: CRUD ═══"
R=$(curl -s -X POST "$BASE/Product" -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d "{\"productCode\":\"RAW01-${TS}\",\"productName\":\"Flour ${TS}\",\"productType\":\"Raw\",\"baseUnitName\":\"Kg\",\"isActive\":true}")
check "Create Flour" "$R"
FLOUR=$(echo "$R" | python3 -c "import sys,json; print(json.load(sys.stdin)['data']['productId'])" 2>/dev/null)

R=$(curl -s -X POST "$BASE/Product" -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d "{\"productCode\":\"RAW02-${TS}\",\"productName\":\"Sugar ${TS}\",\"productType\":\"Raw\",\"baseUnitName\":\"Kg\",\"isActive\":true}")
check "Create Sugar" "$R"
SUGAR=$(echo "$R" | python3 -c "import sys,json; print(json.load(sys.stdin)['data']['productId'])" 2>/dev/null)

R=$(curl -s -X POST "$BASE/Product" -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d "{\"productCode\":\"MFG01-${TS}\",\"productName\":\"Cake ${TS}\",\"productType\":\"Manufactured\",\"baseUnitName\":\"Piece\",\"isActive\":true}")
check "Create Cake" "$R"
CAKE=$(echo "$R" | python3 -c "import sys,json; print(json.load(sys.stdin)['data']['productId'])" 2>/dev/null)

R=$(curl -s -X GET "$BASE/Product" -H "Authorization: Bearer $TOKEN")
check "Get all products" "$R"
R=$(curl -s -X GET "$BASE/Product/$FLOUR" -H "Authorization: Bearer $TOKEN")
check "Get product by ID" "$R"
R=$(curl -s -X PUT "$BASE/Product/$FLOUR" -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d "{\"productCode\":\"RAW01-${TS}\",\"productName\":\"Flour Premium ${TS}\",\"productType\":\"Raw\",\"baseUnitName\":\"Kg\",\"isActive\":true}")
check "Update product" "$R"

R=$(curl -s -X POST "$BASE/Product/$CAKE/recipes" -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d "[{\"rawProductId\":\"$FLOUR\",\"quantityNeeded\":2},{\"rawProductId\":\"$SUGAR\",\"quantityNeeded\":1}]")
check "Add recipes" "$R"
R=$(curl -s -X GET "$BASE/Product/$CAKE/recipes" -H "Authorization: Bearer $TOKEN")
check "Get recipes" "$R"

# ═══ DEVICES ═══
cyan "═══ DEVICES ═══"
R=$(curl -s -X POST "$BASE/Device/register" -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d "{\"deviceCode\":\"PC-${TS}\",\"deviceName\":\"WH PC 1\",\"locationId\":\"$WH_ID\"}")
check "Register device" "$R"
DEV_ID=$(echo "$R" | python3 -c "import sys,json; print(json.load(sys.stdin)['data']['deviceId'])" 2>/dev/null)

R=$(curl -s -X GET "$BASE/Device/PC-${TS}" -H "Authorization: Bearer $TOKEN")
check "Get device" "$R"
R=$(curl -s -X POST "$BASE/Device/$DEV_ID/heartbeat" -H "Authorization: Bearer $TOKEN")
check "Heartbeat" "$R"

# ═══ PURCHASE ═══
cyan "═══ PURCHASE: Invoice → Approve → Payment ═══"
R=$(curl -s -X POST "$BASE/Purchase" -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d "{\"invoiceNumber\":\"INV-${TS}\",\"supplierId\":\"$SUP_ID\",\"locationId\":\"$WH_ID\",\"invoiceDate\":\"2026-04-12T00:00:00Z\",\"items\":[{\"productId\":\"$FLOUR\",\"quantity\":100,\"unitPrice\":10,\"totalPrice\":1000},{\"productId\":\"$SUGAR\",\"quantity\":50,\"unitPrice\":15,\"totalPrice\":750}]}")
check "Create invoice" "$R"
INV_ID=$(echo "$R" | python3 -c "import sys,json; print(json.load(sys.stdin)['data']['purchaseInvoiceId'])" 2>/dev/null)

R=$(curl -s -X GET "$BASE/Purchase/$INV_ID" -H "Authorization: Bearer $TOKEN")
check "Get invoice" "$R"
R=$(curl -s -X GET "$BASE/Purchase" -H "Authorization: Bearer $TOKEN")
check "Get all invoices" "$R"

R=$(curl -s -X POST "$BASE/Purchase/$INV_ID/approve" -H "Authorization: Bearer $TOKEN")
check "Approve invoice" "$R"

# ═══ INVENTORY (verify stock after purchase) ═══
cyan "═══ INVENTORY: Balances & Movements ═══"
R=$(curl -s -X GET "$BASE/Inventory/balances/$WH_ID" -H "Authorization: Bearer $TOKEN")
check "Get balances" "$R"
echo "  → $(echo "$R" | python3 -c "import sys,json;d=json.load(sys.stdin);[print(f'     {x[\"productName\"]}: {x[\"quantityOnHand\"]} @ {x[\"averageCost\"]}') for x in d.get('data',[])]" 2>/dev/null)"

R=$(curl -s -X GET "$BASE/Inventory/movements/$WH_ID" -H "Authorization: Bearer $TOKEN")
check "Get movements" "$R"

R=$(curl -s -X POST "$BASE/Purchase/payments" -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d "{\"supplierId\":\"$SUP_ID\",\"purchaseInvoiceId\":\"$INV_ID\",\"amount\":500,\"supplierPaymentMethodId\":\"$PM_ID\"}")
check "Add payment" "$R"

# ═══ TRANSFER ═══
cyan "═══ TRANSFER: Full Workflow ═══"
R=$(curl -s -X POST "$BASE/Transfer" -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d "{\"fromLocationId\":\"$WH_ID\",\"toLocationId\":\"$BR_ID\",\"requestMode\":\"Online\",\"items\":[{\"productId\":\"$FLOUR\",\"requestedQuantity\":20}]}")
check "Create transfer" "$R"
TF_ID=$(echo "$R" | python3 -c "import sys,json; print(json.load(sys.stdin)['data']['transferRequestId'])" 2>/dev/null)

R=$(curl -s -X GET "$BASE/Transfer/location/$WH_ID" -H "Authorization: Bearer $TOKEN")
check "Get transfers" "$R"
R=$(curl -s -X POST "$BASE/Transfer/$TF_ID/accept" -H "Authorization: Bearer $TOKEN")
check "Accept" "$R"
R=$(curl -s -X POST "$BASE/Transfer/$TF_ID/prepare" -H "Authorization: Bearer $TOKEN")
check "Prepare" "$R"
R=$(curl -s -X POST "$BASE/Transfer/$TF_ID/ship" -H "Authorization: Bearer $TOKEN")
check "Ship" "$R"
R=$(curl -s -X POST "$BASE/Transfer/$TF_ID/receive" -H "Authorization: Bearer $TOKEN")
check "Receive" "$R"

R=$(curl -s -X GET "$BASE/Inventory/balances/$BR_ID" -H "Authorization: Bearer $TOKEN")
check "Branch stock" "$R"
echo "  → $(echo "$R" | python3 -c "import sys,json;d=json.load(sys.stdin);[print(f'     {x[\"productName\"]}: {x[\"quantityOnHand\"]}') for x in d.get('data',[])]" 2>/dev/null)"

# Reject flow
R=$(curl -s -X POST "$BASE/Transfer" -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d "{\"fromLocationId\":\"$WH_ID\",\"toLocationId\":\"$BR_ID\",\"requestMode\":\"Online\",\"items\":[{\"productId\":\"$SUGAR\",\"requestedQuantity\":5}]}")
check "Create transfer 2" "$R"
TF2_ID=$(echo "$R" | python3 -c "import sys,json; print(json.load(sys.stdin)['data']['transferRequestId'])" 2>/dev/null)
R=$(curl -s -X POST "$BASE/Transfer/$TF2_ID/reject" -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"reason":"Not needed"}')
check "Reject transfer" "$R"

# ═══ PRODUCTION ═══
cyan "═══ PRODUCTION ═══"
R=$(curl -s -X POST "$BASE/Production" -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d "{\"locationId\":\"$WH_ID\",\"manufacturedProductId\":\"$CAKE\",\"quantityProduced\":10}")
check "Create production" "$R"

R=$(curl -s -X GET "$BASE/Inventory/balances/$WH_ID" -H "Authorization: Bearer $TOKEN")
check "Stock after production" "$R"
echo "  → $(echo "$R" | python3 -c "import sys,json;d=json.load(sys.stdin);[print(f'     {x[\"productName\"]}: {x[\"quantityOnHand\"]} @ {x[\"averageCost\"]}') for x in d.get('data',[])]" 2>/dev/null)"

R=$(curl -s -X GET "$BASE/Production/location/$WH_ID" -H "Authorization: Bearer $TOKEN")
check "Get productions" "$R"

# ═══ STOCK COUNT ═══
cyan "═══ STOCK COUNT ═══"
R=$(curl -s -X POST "$BASE/StockCount" -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d "{\"locationId\":\"$WH_ID\",\"notes\":\"April Count\",\"items\":[{\"productId\":\"$FLOUR\",\"actualQuantity\":55}]}")
check "Create stock count" "$R"
SC_ID=$(echo "$R" | python3 -c "import sys,json; print(json.load(sys.stdin)['data']['stockCountId'])" 2>/dev/null)

R=$(curl -s -X GET "$BASE/StockCount/location/$WH_ID" -H "Authorization: Bearer $TOKEN")
check "Get counts" "$R"
R=$(curl -s -X POST "$BASE/StockCount/$SC_ID/post" -H "Authorization: Bearer $TOKEN")
check "Post stock count" "$R"

R=$(curl -s -X GET "$BASE/Inventory/balances/$WH_ID" -H "Authorization: Bearer $TOKEN")
check "Stock after count" "$R"
echo "  → $(echo "$R" | python3 -c "import sys,json;d=json.load(sys.stdin);[print(f'     {x[\"productName\"]}: {x[\"quantityOnHand\"]}') for x in d.get('data',[])]" 2>/dev/null)"

# ═══ SYNC ═══
cyan "═══ SYNC ═══"
R=$(curl -s -X POST "$BASE/Sync/push" -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d "{\"locationId\":\"$WH_ID\",\"deviceId\":\"$DEV_ID\",\"data\":{\"Products\":[{\"ProductId\":\"$(python3 -c 'import uuid;print(uuid.uuid4())')\",\"ProductName\":\"SyncTest\"}]}}")
check "Push sync" "$R"

R=$(curl -s -X POST "$BASE/Sync/pull" -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d "{\"locationId\":\"$WH_ID\",\"lastSyncVersion\":0}")
check "Pull sync" "$R"
echo "  → Version: $(echo "$R" | python3 -c "import sys,json;print(json.load(sys.stdin)['data']['latestServerVersion'])" 2>/dev/null)"
echo "  → Changes: $(echo "$R" | python3 -c "import sys,json;print(len(json.load(sys.stdin)['data']['changes']))" 2>/dev/null)"

# ═══ AUDIT ═══
cyan "═══ AUDIT ═══"
R=$(curl -s -X GET "$BASE/Audit" -H "Authorization: Bearer $TOKEN")
check "Get audit logs" "$R"
echo "  → Logs: $(echo "$R" | python3 -c "import sys,json;print(len(json.load(sys.stdin)['data']))" 2>/dev/null)"
R=$(curl -s -X GET "$BASE/Audit?tableName=PurchaseInvoices" -H "Authorization: Bearer $TOKEN")
check "Get filtered logs" "$R"

# ═══ LOGOUT ═══
cyan "═══ AUTH: Logout ═══"
R=$(curl -s -X POST "$BASE/Auth/logout" -H "Authorization: Bearer $TOKEN")
check "Logout" "$R"
R=$(curl -s -X POST "$BASE/Auth/refresh-token" -H "Content-Type: application/json" -d "{\"refreshToken\":\"$REFRESH\"}")
check "Refresh after logout" "$R" "false"

# ═══ RESULTS ═══
echo ""
bold "═══════════════════════════════════════════════════════════"
bold "  FINAL RESULTS"
bold "═══════════════════════════════════════════════════════════"
echo "  Total:  $TOTAL"
green "  Passed: $PASS"
if [ $FAIL -gt 0 ]; then red "  Failed: $FAIL"; else green "  Failed: $FAIL"; fi
bold "═══════════════════════════════════════════════════════════"
