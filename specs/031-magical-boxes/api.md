# Magical boxes API

Implemented locally 2026-10-31; deployment, migration application and live seeding are separate steps.
Requires additive migration `20261031163813_AddMagicalBoxes`. All responses use the existing
`BaseResponse<T>` envelope (`data` below). IDs are Int64; coins and quantities are Int32.

## Authentication and authority

All player routes require the normal SprintLabs JWT. The user/profile comes only from that bearer.
Unknown users/profiles return404; suspended or locked accounts return403. Client fields such as
userId, playerProfileId, price, coins, category rewards or quantities cannot grant authority.
Admin seed requires an active, unlocked platform administrator from the database.

## Read endpoints

`GET /api/v1/Shop` returns `{coins, products:[], boxes:ShopBox[]}`. Direct character listings are hidden.
Only active magical boxes appear. `GET /api/v1/Shop/me/inventory` returns
`{coins,equippedCharacterItemId,items:ShopItem[],boxes:ShopBox[],equippedCosmetics:[{category,itemId}]}`.
Inventory includes positive owned quantities, including owned inactive/retired content.

ShopBox: `{productId,boxKey,name,iconKey,displayLevel,coinPrice,drawCount:5,isActive,quantityOwned,
odds:[{category,percent}]}`. Display levels1/5/9 are labels and never restrict buying/opening.
ShopItem retains itemId/itemKey/name/iconKey/prefabKey/quantityOwned/isCharacter/isEquipped and adds
nullable category and sellPrice. Legacy mission items can have null category and zero sale value.
Categories are exact strings: Character, Skin, Pet, Accessory, Effect.

| Default box | Price | Label | Character/Skin/Pet/Accessory/Effect per draw |
|---|---:|---:|---|
| normal-box |100|1|5/30/25/25/15|
| crystal-box |200|5|10/30/20/20/20|
| galaxy-box |300|9|20/30/15/15/20|

## Mutations

Each buy/open/sell accepts only intent IDs in the route and `{ "requestId": "<nonempty UUID>" }`.
Reuse the same UUID after timeout or503. Its scope is the authenticated player across all three actions
and older direct purchases. Reusing a UUID for a different action or target returns409.
Transactions lock/reload the player wallet and resolve dependent inventory rows after the lock.
The saved response and exact reward order commit atomically with all balances/quantities.

| Method and endpoint | Success data |
|---|---|
| POST `/api/v1/Shop/boxes/{productId}/purchase` | `{requestId,productId,coinsSpent,coinsAfter,quantityOwned,isReplay}` |
| POST `/api/v1/Shop/me/boxes/{productId}/open` | `{requestId,productId,rewards:ShopItem[5],quantityOwned,coinsAfter,isReplay}` |
| POST `/api/v1/Shop/me/items/{itemId}/sell` | `{requestId,itemId,quantitySold:1,coinsGained,coinsAfter,quantityOwned,isReplay}` |
| PUT `/api/v1/Shop/me/equipped-item` | ShopInventory |

Buy stores one unopened box and charges its database price. Open consumes one owned box and performs
exactly five independent cryptographic category draws with replacement; each category's eligible item
is selected uniformly, also with replacement. All enabled category pools must contain a recognized
bundled asset before either buying or opening. An inactive owned box can still be opened.
Duplicate draws stack in the existing player inventory. Draw results report final quantity after that
opening and preserve the generation-time item fields on replay, even after later sale/equip/config edits.

Sell removes one positive owned copy and credits its database sale price: defaults Character50,
Skin20,Pet15,Accessory10,Effect10. The last equipped copy cannot sell; duplicates can.
Quantum Scholar remains free/default and is never a box reward or sellable item. Legacy mission
items retain their old category/price semantics. Arithmetic overflow refuses the transaction.

Equip body: `{ "itemId": 123 }` derives the slot from the owned item; client category cannot override it.
Clearing requires `{ "itemId": null, "category": "Skin" }` (or another exact category).
Character null restores the default. Equip consumes no items/coins. Non-character visuals must map to
an explicitly bundled key. Existing `PUT /me/equipped-character` remains compatible, including null.
The itemId property must be present even when null; omitted property returns400.

Common responses:400 invalid UUID/ID/slot;401 missing bearer;403 unavailable account/admin denied;
404 missing product/user/profile;409 insufficient coins, missing ownership, unavailable catalogue,
last equipped copy, nonsellable item or UUID mismatch;503 transient database conflict/write failure.
A failed transaction grants/spends nothing and the same UUID can safely retry.
After successful mutation or replay, refresh inventory; receipt coinsAfter/quantities are historical
snapshots and must never overwrite newer confirmed wallet/inventory reads.

## Explicit default seed

`POST /api/v1/admin/shop/seed/magical-boxes` has no request body and returns ShopBox[]. It creates
missing box definitions and fourteen bundled items: Tech Smith/Soul Mendrix plus three each of the
four cosmetic categories. It does not automatically run at startup. Stable item and box keys make it
repeat-safe. Existing prices, odds, visibility, names, visuals and configured reward eligibility are
retained; existing pre-feature character rows with no category receive only their new character
category/sale/reward fields once. Existing ownership, box stock, equipment and wallets never reset.
A unique-key seed race returns503 and may be retried. No player rewards or mission records are seeded.

Bundled keys: characters/tech-smith,characters/soul-mendrix; skins/emerald,skins/rose,skins/royal;
pets/frost,pets/amber,pets/moon; accessories/crystal-crown,accessories/amethyst-crown,
accessories/royal-crown; effects/frost,effects/golden,effects/void.
Box icon keys: boxes/normal,boxes/crystal,boxes/galaxy. No remote asset loading is authorized.

## Compatibility and persistence

New direct `POST /products/{productId}/purchase` requests return409. A previously completed matching
request still returns its original direct receipt and does not charge/grant again. Existing direct
history, unique ownership receipt indexes, mission BoxRewardEntries and mission box claim behavior
are retained. New receipt storage is ShopOperations; unopened stock is PlayerBoxes; cosmetic slots
are PlayerCosmeticEquipment. The new migration writes no player, mission or catalogue data.
Do not downgrade the database after creating magical products without a deliberate data plan;
the previous product constraint cannot represent those new rows.
