# Coin shop API

Delivery branch: `codex/coin-shop`, integrating the shop and B2C player setup. Apply both migrations through normal deployment. Shop catalogue seeding is explicit, never automatic. The shop currently sells permanent character/cosmetic unlocks, bought once, then selected with Use/Equip. No consumables, real-money checkout, refunds, boxes for sale, trading, gameplay stats or Unity networking changes.

## Data model

| Existing table | Shop change |
|---|---|
| Items | Existing itemKey/name/iconKey/prefabKey; new IsCharacter flag. Keys map to bundled Unity art/models. |
| ShopProducts | New nullable ItemId and CoinPrice; existing IsActive controls visibility/purchases. One direct listing per item. Old mission boxes retain null ItemId/CoinPrice. |
| PlayerInventoryItems | Reused ownership table, unique (PlayerProfileId, ItemId). A shop purchase grants quantity 1. Mission rewards keep their existing quantities. |
| Players | Existing Gold is the coin wallet. New nullable EquippedCharacterItemId saves one active character; null means default. |
| ShopPurchases | New immutable receipts: player, product, item, requestId, coins spent, coins after, purchase time. Unique per player/requestId and per player/item. |

The backend derives identity from JWT userId, prices/items from its catalogue, and ownership/wallet from its database. A serializable transaction locks the player row on MySQL with FOR UPDATE, matching mission/match wallet locking. Debit, ownership and receipt commit together or roll back. A tracked profile is refreshed after acquiring the lock to avoid losing a recent reward. No database credentials or image/prefab binaries are sent to Unity.

Migration: `AddCoinShop` adds the fields, receipt table, uniqueness and foreign-key/check constraints. It does not modify player balances or seed live catalogue items. Existing box rewards remain compatible. Apply it using normal backend deployment before these APIs are called on a persistent database. MySQL deadlocks/constraint conflicts are normalized to 503 and may be retried using the same requestId.

## Authentication and envelopes

All routes require the existing SprintLabs bearer token. Player routes require an active, unlocked account with a player profile. Admin routes additionally check IsPlatformAdmin in the database; normal players cannot configure prices/items. B2C and B2B players share the shop and their own wallet.

Success: HTTP 200, JSON envelope:

```json
{"data": {}, "message":"Success", "statusCode":200, "errorCode":1}
```

Do not send playerProfileId, userId, coin balances or a price as purchase authority. Unknown body properties are ignored.

## Player endpoints

### GET `/api/v1/Shop`

No body. Returns active direct character listings, coin prices, owned quantities and selection flags for the authenticated player. Excludes old reward boxes and inactive products. The complete response is:

```json
{
  "data": {
    "coins": 1000,
    "products": [{
      "productId": 1,
      "coinPrice": 500,
      "isActive": true,
      "item": {
        "itemId": 1,
        "itemKey": "king-of-knowledge",
        "name": "King of Knowledge",
        "iconKey": "shop/king-of-knowledge",
        "prefabKey": "characters/king-of-knowledge",
        "quantityOwned": 0,
        "isCharacter": true,
        "isEquipped": false
      }
    }]
  },
  "message": "Success", "statusCode": 200, "errorCode": 1
}
```

IDs above are illustrative. Always use the returned IDs.

### POST `/api/v1/Shop/products/{productId}/purchase`

```json
{"requestId":"0884ca08-91eb-4225-9a8d-96838e9f7209"}
```

requestId must be a nonempty UUID. Generate it once per intended purchase and reuse it for all retries after timeout/503. The backend charges the current database price; refresh the catalogue before purchase if the displayed price may be stale. Client-supplied price/balance fields are ignored.

Example data within the success envelope:

```json
{
  "purchaseId": 1,
  "productId": 1,
  "itemId": 1,
  "requestId": "0884ca08-91eb-4225-9a8d-96838e9f7209",
  "coinsSpent": 500,
  "coinsAfter": 500,
  "purchasedAt": "2026-10-29T10:00:00Z",
  "isReplay": false
}
```

A retry with the same player/requestId/product returns that receipt with isReplay=true and no second debit/grant, even after price changes or deactivation. A reused requestId for a different product returns 409. Another request for an owned item returns 409, including items obtained through missions. Purchases do not automatically equip.

coinsAfter is the wallet snapshot at purchase time, not necessarily the present balance on a replay. After a purchase or replay, refresh the catalogue/inventory or existing progression API for the current wallet. Treat purchasedAt as UTC, consistent with existing backend timestamps.

### GET `/api/v1/Shop/me/inventory`

Returns data:

```json
{
  "coins": 500,
  "equippedCharacterItemId": 1,
  "items": [{
    "itemId": 1, "itemKey": "king-of-knowledge", "name": "King of Knowledge",
    "iconKey": "shop/king-of-knowledge", "prefabKey": "characters/king-of-knowledge",
    "quantityOwned": 1, "isCharacter": true, "isEquipped": true
  }]
}
```

Only the caller's positive quantities are included; includes existing mission items. isCharacter=false entries cannot be selected through the character endpoint. Owned characters remain in inventory and usable after their sale listing is deactivated.

### PUT `/api/v1/Shop/me/equipped-character`

```json
{"itemId":1}
```

The field must be present. Only an owned character can be selected. Repeating it is safe. To restore the game's default character:

```json
{"itemId":null}
```

Returns the same inventory data shape. Does not consume coins/items. One selected character per player. Unity applies the returned prefabKey using its bundled catalogue; this API does not itself change Mirror actors or gameplay stats.

### GET `/api/v1/Shop/me/purchases?page=1&pageSize=30`

Returns a data array of purchase receipts, newest first. Page >=1 (max 1,000,000), pageSize 1–100. Ownership follows JWT, with no client-selected player ID. An empty page returns []. History entries use isReplay=false; coinsAfter remains the historical snapshot.

## Admin endpoints

### PUT `/api/v1/admin/shop/products/{itemKey}`

Creates or updates one character listing by stable key. Example key: king-of-knowledge.

```json
{
  "name":"King of Knowledge",
  "coinPrice":500,
  "iconKey":"shop/king-of-knowledge",
  "prefabKey":"characters/king-of-knowledge",
  "isActive":true
}
```

Returns a product using the same shape as a catalogue entry; quantityOwned=0 and isEquipped=false are not a player's inventory here. Existing item/listing IDs are retained. itemKey: 1–100 lowercase ASCII letters/digits/hyphen/underscore. Name: trimmed 1–200 characters. Icon/prefab keys: trimmed 1–200 characters. Control characters rejected. Price: positive integer, max Int32. IsActive defaults true; send it explicitly when editing.

iconKey and prefabKey are opaque bundled-asset identifiers, not uploaded files or download URLs. Admin updates do not verify assets exist in a game build. Do not publish a visible listing until the game maps those keys. Existing non-character mission items cannot be repurposed through this API (409). Use isActive=false to retire an item from sale while preserving receipts and ownership; there is no delete endpoint.

### POST `/api/v1/admin/shop/seed/example`

No body. Explicitly creates the screenshot's King of Knowledge at 500 coins with the example keys shown above. Repeat-safe: returns an existing matching listing without changing an administrator-edited price/visibility. Nothing is seeded at startup or by migration. Concurrent admin creation may return 503; retry the same stable key. Example keys require Unity mapping before player testing.

## Error responses

- 400: missing/empty requestId, malformed types/JSON, invalid product configuration, invalid item ID or pagination. Binding errors may use ASP.NET ProblemDetails.
- 401: absent/expired JWT or missing/invalid userId claim.
- 403: suspended/locked account or non-admin catalogue write.
- 404: missing player/product, inactive/unpurchasable product.
- 409: `Insufficient coins.`, `Item already owned.`, `Character is not owned.`, requestId reused for a different product, or protected reward item conversion.
- 503: temporary database/concurrency failure. Retry purchases with the SAME requestId.

Controlled failures use the usual message/statusCode/errorCode envelope (errorCode=3). Do not infer success from HTTP completion alone; require a successful envelope and reconcile the server inventory/wallet.


## Verification (2026-10-29)

- `dotnet build SprintLabs.sln`: passed (existing warnings, no errors).
- `dotnet test SprintLabs.sln --no-restore`: 682 passed, 13 skipped, 0 failed. Includes 18 new SQLite HTTP/migration checks; three new MySQL concurrency checks are opt-in and were skipped alongside 10 existing MySQL checks.
- Tests cover fresh-context persistence, identity spoofing, duplicate/replayed requests, retired items, insufficient coins, ownership from mission inventory, non-character equipment rejection, admin permissions, input validation, rollback after a forced receipt-write failure, and stale tracked-wallet refresh.
- `dotnet ef migrations has-pending-model-changes --project Infrastructure --startup-project API --no-build`: no pending model differences.
- `git diff --check`: passed. No package/project reference changes. Unity files and existing B2C work are untouched except the living handoff entry.
- Real MySQL concurrency and end-to-end Unity testing require the intended test environment. Verify deployment migrations separately; no purchase or player-balance edits are part of deployment verification.

## 2026-10-30 - Bundled Unity character listings

Migration `20261030090000_SeedUnityCharacterCatalogue` creates missing Tech Smith
and Soul Mendrix character items and active 100-coin listings. Keys match the
Unity build: `shop/tech-smith` / `characters/tech-smith` and
`shop/soul-mendrix` / `characters/soul-mendrix`.

Apply through the normal backend database migration process after AddCoinShop.
The new migration does not change the schema, wallets, ownership or selection.
Existing items and listings (including administrator-edited prices/inactive flags)
are preserved. A non-character item occupying a stable key is skipped rather than
converted; resolve that collision explicitly through catalogue administration.
The Down operation retains catalogue rows to protect receipts and ownership;
retire a listing through the existing admin PUT with isActive=false if required.
No administrator token is needed by Unity or embedded in the migration.

After deployment/database update, sign in as a player and GET /api/v1/Shop,
then Refresh Magical Shop. Only unowned characters appear for sale in Unity.
See unity-character-catalogue.md for the existing manual admin configuration path.
