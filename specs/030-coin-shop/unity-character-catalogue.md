# Unity character catalogue configuration

Prepared 2026-10-30 for the empty Magical Shop investigation. These requests use
existing APIs; they are not applied automatically by migration or startup.

Run against the same backend environment as the Unity client's BackendSettings,
using a SprintLabs session belonging to an active platform administrator.
Keep the bearer token in the HTTP client's private environment, never this file.
A normal player's session cannot publish products.

## Listings

PUT `/api/v1/admin/shop/products/tech-smith`

```json
{
  "name": "Tech Smith",
  "coinPrice": 100,
  "iconKey": "shop/tech-smith",
  "prefabKey": "characters/tech-smith",
  "isActive": true
}
```

PUT `/api/v1/admin/shop/products/soul-mendrix`

```json
{
  "name": "Soul Mendrix",
  "coinPrice": 100,
  "iconKey": "shop/soul-mendrix",
  "prefabKey": "characters/soul-mendrix",
  "isActive": true
}
```

Each request returns the existing product envelope described in api.md. Repeating
these PUTs updates the same stable keys (including their price and visibility),
without purchasing, equipping, or granting ownership. Review existing listings
before applying. Each PUT is independent; if one fails, retry that stable key.
Do not substitute the example seed endpoint: it creates a different character.
Quantum Scholar remains Unity's default character and is not listed here.

## Verify

1. Require HTTP 200 with returned itemKey, coinPrice=100, isActive=true and
   isCharacter=true for each PUT. Use returned IDs, never hard-coded IDs.
2. Using the affected player's session, GET `/api/v1/Shop`. Check both keys
   and prices. If absent, verify the target environment and active listings.
3. Open Magical Shop and Refresh. Unowned characters appear as BagCards.
4. Purchase one character: the wallet decreases by 100 exactly once, the listing
   disappears from sale UI, and the character appears in Cosmetics.
5. Use it, reconnect to a rebuilt game server, and check the selected model.

Existing failure behavior: 401 requires a valid session; 403 requires platform
administrator access; 409 can mean a key belongs to an existing non-character
mission item; 503 requires retry. Do not bypass authorization or grant inventory
locally. See api.md and frontend.md for the complete shop contract.

Status: requests prepared only. No live catalogue writes or deployment performed.

Alternative now implemented: apply migration 20261030090000_SeedUnityCharacterCatalogue through normal deployment to create missing listings without an admin login. Existing listings remain unchanged. Live application remains pending.
