# SprintLabs game integration: B2C player setup and coin shop

Updated: 2026-10-29. Audience: Unity/game developers.

## 1. Availability and scope

Delivery: both backend features are integrated on `codex/coin-shop` for publication to staging. Confirm the deployment has completed before pointing the game at these routes.

| Feature | Migration |
|---|---|
| B2C player setup | `20261028230451_AddB2CPlayerSetup` |
| Coin shop | `20261029005140_AddCoinShop` |

The combined EF model and migration target have been reconciled. Both migrations are required, in the order shown.

B2C setup saves nickname, grade, curriculum and language. B2B education remains community-managed. The shop is available to both account types and sells permanent characters/cosmetics bought once, then selected through Use/Equip. Unity integration is still required.

## 2. Shared API rules

Use the existing centralized backend BaseUrl, HTTP client, token refresh and JSON handling. All routes below require the **SprintLabs access token**, not a Firebase or Google token.

```http
Authorization: Bearer <SPRINTLABS_ACCESS_TOKEN>
Content-Type: application/json
```

Content-Type is needed for JSON request bodies. No body is required for GET requests or the example-seed POST. Never embed tokens in source, scenes, assets, URLs or logs.

Success responses use the existing envelope:

```json
{
  "data": {},
  "message": "Success",
  "statusCode": 200,
  "errorCode": 1
}
```

The response examples below show the **contents of `data`**, unless explicitly labeled otherwise. IDs are illustrative: use IDs returned by the API. Use `long`/Int64 for numeric player, product, item and purchase IDs in C#.

The server selects the player from the authenticated user. Do not send userId or playerProfileId to choose whose profile, wallet or inventory to change. Backend prices, ownership and balances are authoritative.

## 3. Complete login flow

1. Complete the existing login and establish the SprintLabs token/session.
2. GET `/api/v1/PlayerProfiles/me/setup`.
3. If `isCommunityManaged=true`, skip the B2C form and continue the existing community profile flow. Do not use the B2C save endpoint for this account.
4. For B2C, if `requiresSetup=true`, fetch setup options and show the form described below. Wait for a successful save before continuing.
5. Refresh GET `/api/v1/Users/me/player-profile`. Its `name` is the gameplay nickname; do not overwrite it with the provider's display name.
6. GET `/api/v1/Shop/me/inventory` to restore owned items, current coins and selected character.
7. Resolve the equipped item's prefabKey using the game's bundled asset mapping; a null selection uses the existing default character.
8. Enter the lobby. Load the shop catalogue when the player opens the shop.

Repeat the setup check after each login/session restore. A local completion flag is not authoritative. Clear drafts, pending UI state and cached player-specific data on sign-out/account switch; ignore responses from a previous session.

## 4. B2C first-time setup

### Check saved data

**GET `/api/v1/PlayerProfiles/me/setup`**

Example complete B2C profile:

```json
{
  "playerProfileId": 23,
  "nickname": "Space Explorer",
  "grade": 7,
  "curriculum": "Egyptian National",
  "language": "English",
  "isCommunityManaged": false,
  "requiresSetup": false
}
```

For a first-time B2C player, grade/curriculum/language may be null and requiresSetup=true. Setup is required if the name is blank, grade is outside 7-12, or curriculum/language is blank. A provider-created name alone does not complete setup.

A non-revoked community student license matching player ID, user ID or email, including pending invitations, makes the profile community-managed. It returns isCommunityManaged=true and requiresSetup=false. Missing B2B education should be handled by the community flow, not by showing this B2C form.

### Load dropdowns

**GET `/api/v1/PlayerProfiles/setup-options`**

```json
{
  "grades": [7, 8, 9, 10, 11, 12],
  "educationOptions": [
    { "curriculum": "Egyptian National", "language": "Arabic" },
    { "curriculum": "Egyptian National", "language": "English" },
    { "curriculum": "IG", "language": "English" }
  ]
}
```

The actual list combines seeded example pairs and distinct curriculum/language pairs from questions for grades 7-12. More options may be returned. Selectable examples do not guarantee questions exist for every grade/unit.

| Screen field | Request field | Validation and mapping |
|---|---|---|
| Name | `nickname` | Required, trimmed, 1-100 characters, no control characters. Duplicate nicknames are allowed. |
| Grade | `grade` | Integer from returned grades, 7-12. This is the grade value, not a community Grade row ID. |
| Education | `curriculum` | Distinct curriculum strings from educationOptions; max 120 characters. |
| Language | `language` | Only languages paired with the selected curriculum; max 40 characters. |

Submit exact returned option strings. Clear an incompatible language when curriculum changes. Prefill existing values where available; do not silently select missing choices for the player. Age, school and picture selection are not part of this form.

### Save all four values

**POST `/api/v1/PlayerProfiles/me/setup`**

Request body:

```json
{
  "nickname": "Space Explorer",
  "grade": 7,
  "curriculum": "Egyptian National",
  "language": "English"
}
```

Returns the same setup response shape with requiresSetup=false. All four fields are required; null, blank, unsupported grade, unknown curriculum/language pairs and wrong types are rejected.

The nickname reuses Players.Name. Saving does not change the account/Google name, coins, XP, rank or pictures. Repeated valid saves are safe; later saves may update these four B2C values. A community-managed player receives 403.

After saving, refresh `/api/v1/Users/me/player-profile` and update the cached gameplay name. The profile response exposes nickname as `name`, plus `grade`, `curriculum` and `language`.

## 5. Shop catalogue and card states

**GET `/api/v1/Shop`**

```json
{
  "coins": 1000,
  "products": [
    {
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
    }
  ]
}
```

Map iconKey and prefabKey to bundled Unity art/prefabs. They are identifiers, not download URLs. Do not offer an item for purchase if this build cannot resolve its assets.

| Item state | Card behavior |
|---|---|
| quantityOwned=0 | Show price/Buy; disable when coins are insufficient or a request is pending. |
| quantityOwned>0, isEquipped=false | Show Use; hide/disable Buy. |
| isEquipped=true | Show Equipped/Using; hide/disable Buy. |
| products=[] | Show an empty-shop state. |

Only active direct character listings are returned. Old mission boxes are not sold through this version of the shop.

## 6. Purchase with coins

**POST `/api/v1/Shop/products/{productId}/purchase`**

Request body:

```json
{"requestId":"0884ca08-91eb-4225-9a8d-96838e9f7209"}
```

Create one UUID per intended purchase (`Guid.NewGuid()` in C#). Keep it and reuse it for retries after timeout, disconnect or 503. Do not generate a new UUID on every retry.

Example response data:

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

- The backend uses its current database price. Refresh an old catalogue before purchase; client-supplied price/balance fields are ignored.
- Coin deduction, ownership and receipt are committed together. Buying does not grant XP/rank or automatically equip the item.
- Same player + requestId + product returns the original receipt with isReplay=true and no extra charge, including after price changes/deactivation.
- Reusing the UUID for a different product returns 409.
- A new request for an already-owned item returns 409 without charging, including ownership obtained through missions.
- coinsAfter is a historical snapshot. On a replay, it may differ from the current wallet. Refresh catalogue/inventory or the existing progression API; do not overwrite a newer balance with an old receipt.

After success, refresh catalogue and inventory, update the card to Use, and refresh the coin display from current backend data. After an app restart with an uncertain purchase, reconcile inventory before buying again.

## 7. Inventory and Use/Equip

### Restore ownership and selection

**GET `/api/v1/Shop/me/inventory`**

```json
{
  "coins": 500,
  "equippedCharacterItemId": 1,
  "items": [
    {
      "itemId": 1,
      "itemKey": "king-of-knowledge",
      "name": "King of Knowledge",
      "iconKey": "shop/king-of-knowledge",
      "prefabKey": "characters/king-of-knowledge",
      "quantityOwned": 1,
      "isCharacter": true,
      "isEquipped": true
    }
  ]
}
```

Includes positive owned quantities, including mission reward items. Only isCharacter=true items can use the character-selection endpoint. A retired sale listing does not remove ownership; its owner can still select it from inventory.

### Select an owned character

**PUT `/api/v1/Shop/me/equipped-character`**

Request:

```json
{"itemId":1}
```

Returns the inventory response above. Apply the confirmed selected item's prefabKey. This does not consume coins or quantity. Repeated selection is safe. The itemId field must be present.

To restore the existing default character:

```json
{"itemId":null}
```

One character is selected at a time. Shop character selection is separate from avatar/profile-picture selection. This API saves the selection; it does not replace a live Mirror actor or add gameplay buffs. During multiplayer integration, validate client selection against backend ownership/selection on the game server rather than trusting an arbitrary client prefab ID.

### Optional purchase history

**GET `/api/v1/Shop/me/purchases?page=1&pageSize=30`**

Response data is an array of receipts in the purchase response shape, newest first; empty pages return []. History entries have isReplay=false. Page starts at 1 (maximum 1,000,000), pageSize is 1-100.

If displayed, columns can be date, product/item, coins spent and historical balance. No new history screen is required for the Buy/Use flow.

## 8. Shared loading and error handling

Show loading while checking setup/options or opening the shop. Disable Save/Buy/Use during their requests. Keep the entered form or purchase UUID on recoverable errors.

| HTTP status | Game action |
|---|---|
| 400 | Show invalid-input feedback. Setup must stay on the form. Wrong JSON types may return ProblemDetails. |
| 401 | Use the existing token-refresh/login flow. Do not clear ownership based on an auth failure. |
| 403 | Stop the denied action. For setup, refresh management status: the player may now be community-managed. |
| 404 | Profile unavailable, or product unavailable/inactive. Retry profile loading or refresh the catalogue as appropriate. |
| 409 | Shop conflict: insufficient coins, already owned, unowned character, or UUID reused for another product. Refresh wallet/inventory. |
| 503 | Shop/database temporarily unavailable. Offer retry using the same purchase UUID. |

Controlled failures use message/statusCode/errorCode, with errorCode=3. Example:

```json
{"message":"Insufficient coins.","statusCode":409,"errorCode":3}
```

B2C validation uses message "Invalid Data". Controlled B2C forbidden responses currently use "InvalidAccessToken"; use status and the setup management flag, not that text, to decide the UI. Authentication can return an empty body. Handle HTTP status even when no regular envelope is present.

Do not mark a form saved, grant ownership, debit the persistent wallet or apply a selected character as confirmed before the backend succeeds. Discard stale responses after account changes. Do not manufacture profile IDs or use an offline DEV identity as a real backend account.

## 9. Backend/operator preparation

These calls are for a platform administrator outside the game. Never ship administrator tokens in Unity.

**PUT `/api/v1/admin/shop/products/{itemKey}`** creates/updates a permanent character listing:

```json
{
  "name":"King of Knowledge",
  "coinPrice":500,
  "iconKey":"shop/king-of-knowledge",
  "prefabKey":"characters/king-of-knowledge",
  "isActive":true
}
```

The response data is a catalogue product entry. The itemKey is stable: 1-100 lowercase ASCII letters/digits/hyphen/underscore. Name and asset keys are required and trimmed, maximum 200 characters, no control characters. Price must be a positive Int32. Send isActive explicitly when editing. Set it false to retire sales while retaining ownership. The endpoint cannot convert an existing non-character mission reward item into a character.

**POST `/api/v1/admin/shop/seed/example`**, no body, explicitly creates King of Knowledge at 500 coins with the keys above. Repeating it preserves an administrator-edited price/visibility. The example asset keys must be mapped in Unity before allowing players to purchase. No shop items are seeded automatically at startup or by migration.

The B2C migration seeds Egyptian National/Arabic, Egyptian National/English and IG/English. Setup options additionally include question-bank pairs. No fake questions or wallet balances are seeded by these migrations.

## 10. Developer acceptance checklist

1. New B2C account: requiresSetup=true; save all four values; lobby shows the selected nickname.
2. Restart/sign out/in: requiresSetup=false; nickname/education persist. Community student skips the B2C form; setup POST is denied.
3. Unsupported grade 6/13, blank nickname, invalid pair: 400; previous values and coins/XP remain unchanged.
4. Map and add the example item. With 1000 coins, buy at 500: own one item and have 500 coins; XP/rank unchanged.
5. Repeat the same purchase UUID: same purchaseId, isReplay=true, no second debit. New UUID for the same owned item: 409, no debit.
6. Use the item; reload inventory after restart: selection persists. Clear with itemId:null: default restored.
7. Insufficient coins, unowned/non-character selection and ordinary-player admin calls fail without grants/debits.
8. Retire the item: it disappears from sale but remains usable for its owner.
9. Switch accounts during setup/loading/saving: no draft, inventory or stale response appears under the new account.
10. Test request timeout/503 and retry with the original UUID. A replay receipt must not reset a newer coin balance.

Combined validation: build passed; 700 tests passed, 13 MySQL-only tests skipped. Model consistency and generated migration SQL checks passed. The three shop MySQL concurrency tests require a disposable database and have not run. Unity UI integration remains separate; verify deployment completion before manual API tests.
