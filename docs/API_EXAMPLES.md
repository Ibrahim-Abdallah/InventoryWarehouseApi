# API examples

These examples use the HTTPS Development profile (`https://localhost:7140`) and non-secret placeholders. Replace IDs and credentials locally; never store tokens in source control.

## Login and Bearer authorization

```http
POST /api/auth/login
Content-Type: application/json

{ "email": "admin@example.local", "password": "<development-admin-password>" }
```

The response includes access/refresh tokens, their UTC expiries, and user details. Use `Authorization: Bearer <access-token>` on protected requests. Refresh and logout accept `{ "refreshToken": "<refresh-token>" }` at `/api/auth/refresh` and `/api/auth/logout`.

## Create Product

```http
POST /api/products
Authorization: Bearer <access-token>
Content-Type: application/json

{ "sku": "BOLT-M8-30", "name": "M8 x 30 mm Bolt", "description": "Zinc-plated hex bolt" }
```

## Stock In

```http
POST /api/inventory/products/{productId}/warehouses/{warehouseId}/locations/{locationId}/stock-in
Authorization: Bearer <access-token>
Content-Type: application/json

{ "quantity": 100, "referenceType": "GoodsReceipt", "referenceId": "GR-2026-0042" }
```

## Stock Out

```http
POST /api/inventory/products/{productId}/warehouses/{warehouseId}/locations/{locationId}/stock-out
Authorization: Bearer <access-token>
Content-Type: application/json

{ "quantity": 12, "referenceType": "WorkOrder", "referenceId": "WO-1048" }
```

Insufficient available quantity returns 409 without changing balance or ledger.

## Create and fulfill a Reservation

```http
POST /api/inventory-reservations
Authorization: Bearer <access-token>
Content-Type: application/json

{
  "productId": "{productId}",
  "warehouseId": "{warehouseId}",
  "warehouseLocationId": "{locationId}",
  "quantity": 8,
  "referenceType": "Order",
  "referenceId": "ORDER-2381"
}
```

```http
POST /api/inventory-reservations/{reservationId}/fulfill
Authorization: Bearer <access-token>
```

Fulfillment consumes the full reservation and links one StockOut movement.

## Create and complete a Transfer

```http
POST /api/warehouse-transfers
Authorization: Bearer <access-token>
Content-Type: application/json

{
  "sourceWarehouseId": "{warehouseId}",
  "sourceWarehouseLocationId": "{locationId}",
  "destinationWarehouseId": "{destinationWarehouseId}",
  "destinationWarehouseLocationId": "{destinationLocationId}",
  "items": [{ "productId": "{productId}", "quantity": 20 }]
}
```

```http
POST /api/warehouse-transfers/{transferId}/complete
Authorization: Bearer <access-token>
```

Creation is Pending and does not reserve or move stock. Completion revalidates and atomically writes paired movements.

## Configure Low Stock Threshold

```http
PUT /api/low-stock-thresholds/products/{productId}/warehouses/{warehouseId}/locations/{locationId}
Authorization: Bearer <access-token>
Content-Type: application/json

{ "thresholdQuantity": 15, "isEnabled": true }
```

## Inventory Summary report

```http
GET /api/reports/inventory-summary?search=bolt&pageNumber=1&pageSize=20&sortBy=available&sortDirection=desc
Authorization: Bearer <access-token>
```

The page contains aggregated on-hand, reserved, and derived available quantities.

## Product Stock History report

```http
GET /api/reports/products/{productId}/stock-history?fromUtc=2026-01-01T00:00:00Z&toUtc=2027-01-01T00:00:00Z&pageNumber=1&pageSize=20&sortDirection=asc
Authorization: Bearer <access-token>
```

Inbound types are signed positively and outbound types negatively. `fromUtc` is inclusive and `toUtc` exclusive.
