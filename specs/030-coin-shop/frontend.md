# Game integration: coin shop

## UI behavior matching the screenshot

- Open shop: GET `/api/v1/Shop`; display name, icon/model, coinPrice, current coins.
- Map iconKey/prefabKey to locally bundled Unity assets. King of Knowledge is a backend example; map its keys before making its listing available to players.
- quantityOwned=0: show the coin-price Buy button. Disable it if the current balance is insufficient or the item asset is unavailable in this build.
- quantityOwned>0 and isEquipped=false: enable Use; hide/disable Buy.
- isEquipped=true: show Equipped/Using; hide/disable Buy.
- Buy: create a UUID once, POST `/api/v1/Shop/products/{productId}/purchase`, then refresh catalogue/inventory and progression. Never subtract coins locally as persistence authority.
- Use: PUT `/api/v1/Shop/me/equipped-character` with itemId; apply the confirmed selection. There is no additional charge.
- Load GET `/api/v1/Shop/me/inventory` at login/session restore to recover the selected character, including retired owned items. Null selection uses the existing default character.
- Purchase history: optional existing-profile section using GET `/api/v1/Shop/me/purchases`; columns are date, product/item, coinsSpent, historical coinsAfter. No new screen is mandatory.

## Requests

Use the game's centralized BaseUrl and HTTP/JWT transport. Relative routes and full contracts are in [api.md](api.md).

```http
POST /api/v1/Shop/products/1/purchase
Authorization: Bearer <SPRINTLABS_ACCESS_TOKEN>
Content-Type: application/json

{"requestId":"0884ca08-91eb-4225-9a8d-96838e9f7209"}
```

```http
PUT /api/v1/Shop/me/equipped-character
Authorization: Bearer <SPRINTLABS_ACCESS_TOKEN>
Content-Type: application/json

{"itemId":1}
```

Never put administrator tokens into the game. Operators populate listings using PUT `/api/v1/admin/shop/products/{itemKey}` or the explicit admin-only POST `/api/v1/admin/shop/seed/example`. No player-facing price-setting or inventory-grant endpoint exists.

## Loading, retry, and error states

- While loading show a spinner/skeleton, then an empty-shop state if products=[].
- Disable duplicate clicks while a purchase/equip is pending. Keep the intended purchase UUID across timeout, disconnect and 503 retries. If the client restarts, reconcile inventory before buying again; an already-owned check prevents a second charge even if its UUID was lost.
- isReplay receipts contain the original coinsAfter snapshot; fetch current wallet before updating the top coin display. Do not roll a newer balance backwards.
- On 409 refresh wallet/inventory: distinguish insufficient coins, already owned and invalid selection. Never issue a new UUID merely to bypass a timeout.
- On 401 use existing refresh/login; on 403 stop writes and show unavailable; on 404 refresh the catalogue because the listing may have been retired.
- On 400 show invalid-request feedback; on 503 preserve the request and offer retry.
- Clear state when accounts change; discard responses from the previous authentication session.
- Do not display unresolved prefab/icon keys as an empty purchasable card. Keys are identifiers, not remote download URLs.

## Gameplay boundary

Purchasing persists ownership; equipping persists a cosmetic selection. This backend task does not modify Unity, grant movement/score/reward buffs, or change a live match actor. When wiring character selection into multiplayer, treat the client's chosen item as intent and validate against backend ownership/selection on the game server. Do not let a client spoof an unrestricted prefab ID. Existing Google/profile avatar picture selection is separate from shop character selection.

## Minimal manual test

1. Deploy backend with AddCoinShop migration to the intended test environment.
2. As platform admin, add the example through POST `/api/v1/admin/shop/seed/example`, or PUT a listing with keys mapped in the game. Capture returned productId/itemId rather than assuming numeric values.
3. Sign in as a test player with at least 500 coins; GET shop. Expect the example at 500, not owned.
4. Buy with one UUID; expect wallet minus 500 and quantityOwned=1. XP/rank remain unchanged.
5. Repeat the same request: same purchaseId, isReplay=true, no further debit. Send a new UUID for the owned item: 409, no debit.
6. Use it, restart/sign in again, fetch inventory: same selection. Clear with itemId:null and confirm the default.
7. Try a player with insufficient coins, another player's item, inactive listing, and ordinary-player admin writes: all rejected without wallet/inventory changes.
8. Retire the listing; new purchases stop, but its owner can still use it through inventory.

Scope: permanent unlocks only. Live MySQL concurrency checks are opt-in and require a disposable test database; they do not run against staging. Seed the shop explicitly after deployment; Unity UI integration remains separate.

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
