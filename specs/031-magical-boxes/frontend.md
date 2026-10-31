# Magical boxes frontend

Use the existing Shop and Bag sections and the existing authored card style. Backend-fetched boxes
replace direct character purchases. Show art, name, coin price, display-level label, quantity and
category odds; no level gate. Header coins come from confirmed inventory/wallet state.

| Action | API | UI result |
|---|---|---|
| Open shop |GET Shop|Three active boxes; empty catalogue has a clear empty state|
| Buy |POST boxes/{id}/purchase|Busy until confirmed; unopened box appears in Bag|
| Open Bag |GET Shop/me/inventory|One card per positive stacked item/box|
| Open owned box |POST Shop/me/boxes/{id}/open|Exactly five reward cards, including duplicate cards, in a dismissible overlay|
| Use item |PUT Shop/me/equipped-item|Refresh inventory and reflect confirmed character/cosmetic equipment|
| Sell one |POST Shop/me/items/{id}/sell|Refresh quantity and wallet; remove zero-quantity card|
| Clear slot |PUT Shop/me/equipped-item with null itemId/category|Reflect backend-confirmed default/empty slot|

Item cards show quantity plus Use/Sell. Opening reward cards hide Use/Sell. Disable last-equipped-copy
sale in the UI while still displaying backend refusals. Use never consumes inventory. The default
character remains available without a purchase. Cosmetic key values map to bundled outfit1/2/3,
with0 unequipped. Never save local fake inventory, coin debits, reward choices or ownership.

Keep each unresolved buy/open/sale UUID per authenticated owner/action across timeouts/restarts;
retry the same request instead of generating another UUID. Busy prevents accidental duplicate
intent. Display request errors and preserve pending retry state on uncertain outcomes. Refresh after
success/replay and on reopen; historical receipt coinsAfter must not roll a newer wallet backward.
Show rewards only after confirmed or replayed open. Session/account switches clear old presentation
and reject late replies before updating the current account.

Ordinary users cannot see admin seeding. Administrators explicitly call the protected seed route
with their own normal administrator JWT; never ship that credential. Deploy/apply the additive
migration before seed/client release. Missing pool/config errors leave coins/boxes unchanged.

Validation still required on a deployed instance: authenticate a dedicated development account,
buy/open, stack duplicates, equip all categories, sell a duplicate, refuse last equipped copy,
clear and sell, relaunch/reload, retry a saved open UUID and verify exactly the same ordered rewards.
Unity card layout/import and actual staging behavior are separate from backend automated tests.
