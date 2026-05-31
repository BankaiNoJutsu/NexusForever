# Storefront "Catalogue Unavailable" — Investigation Findings

**Status:** Implemented (server + local launch path)  
**Symptom:** WildStar client dialog *Something went wrong somewhere* / *Catalogue Unavailable* when opening the in-game store or account inventory.  
**Client enum:** `CodeEnumStoreError.CatalogUnavailable` (`0`) via opcode `0x098A` (`StoreError` event).  
**Last updated:** 2026-05-31

## Summary

Three separate failure modes were investigated. All three were addressed in the repo; the **client launch configuration** was the tightest explanation once server logs and client error logs were read together.

| Layer | Cause | Fix | Proof |
| --- | --- | --- | --- |
| **Client launch** | Local launcher hardcoded `realmDataCenterId 9` (retail NA row with dead `StoreBannerDataUrlTemplate`) | Configurable `RealmDataCenterId`, default **`6`** (localhost row, empty banner URL) | Client log `malformed store banner data: 80072ee7`; `RealmDataCenter.tbl.sql` rows 4 vs 6 |
| **World server (catalog timing)** | Catalog deferred to a later event tick after `082D`; client tied `StoreCatalogReady` to the request response | Send catalog synchronously in `SendCatalogResponse`; gate only when pregame packets or player loading are not ready | World log showed `(catalog deferred)` then catalog packets one tick later |
| **World server (0989 loop)** | `0x0989` plus full catalog on bootstrap/store hooks made the client spam `0x082D`; repeat responses also sent `0x0989` | `0x082D` always answered with `0x0988`→`0x098B`→`0x0987` only; post-create bootstrap sends `0x0989` alone when pregame already delivered | World log showed many `082D` + `refresh=True` catalog bursts per store open |
| **World server (catalog packets)** | Initial catalog ended with `ServerStoreCatalogUpdated` (`0x0989`), which retail treats as dirty | Initial response: `0x0988` → `0x098B` → `0x0987` only; reserve `0x0989` for real refreshes | Decompile `Storefront_HandleStoreCatalogUpdated`; live logs showed no `0989` after fix |

For local emulator sessions, **restart the world server and relaunch the client** after pulling these changes. A running WildStar process keeps the old `realmDataCenterId` and connector bits until exit.

---

## Primary root cause — realm data center row (client)

### Evidence

Extracted client table `wildstar_client.RealmDataCenter` (`wildstar_client_mysql/RealmDataCenter.tbl.sql`):

| ID | `authServer` | `storeBannerDataUrlTemplate` | Use |
| --- | --- | --- | --- |
| **6** | `localhost` | *(empty)* | Local / offline emulator row |
| **9** | `auth.na.wildstar-online.com` | `http://static.wildstar-online.com/banners/` | Retail NA row (host retired) |

Local launch paths previously passed `/realmDataCenterId 9` (hardcoded in `NexusForever.ClientConnector` and `Start-NexusForeverLocal.ps1`). The client then tried to load store banner data from the dead retail static host. Client error logs reported:

```text
malformed store banner data: 80072ee7
```

`80072ee7` is the Windows HRESULT for **WININET_E_NAME_NOT_RESOLVED** (hostname could not be resolved), consistent with fetching a defunct retail CDN URL.

The storefront UI surfaces this as the generic **Catalogue Unavailable** path even when the world server later sends a valid catalog (`0x0988` / `0x098B` / `0x0987`).

### Implemented fix

- `Source/NexusForever.ClientConnector/Configuration/ClientConfiguration.cs` — `RealmDataCenterId` property, default **`6`**.
- `Source/NexusForever.ClientConnector/Program.cs` — builds `/realmDataCenterId {id}` from config (fallback `6` if unset/invalid).
- `Tools/Setup/Start-NexusForeverLocal.ps1` — `-RealmDataCenterId` parameter (default **`6`**), written into staged `Client64\config.json` and direct `WildStar64.exe` fallback argv.

Operational notes: `Tools/Setup/README.md` documents the parameter and the banner URL mismatch. After `dotnet build` for ClientConnector, restage the connector into the WildStar `Client64` folder and ensure `config.json` contains `"RealmDataCenterId": 6` before the next launch.

### Falsification test

1. Launch with default local setup (`RealmDataCenterId 6`): store/account inventory should open without banner fetch errors.
2. Launch with `-RealmDataCenterId 9`: should reproduce `malformed store banner data: 80072ee7` and catalogue failure (only use when intentionally testing retail row behavior).

---

## Secondary — world server catalog request timing

### Evidence

`CharacterListManager` comment (retail-aligned): *"Retail can request the storefront before the character query completes."*

Live world log (account 1, character select):

1. `ClientStorefrontRequestCatalog` (`0x082D`) received.
2. Handler deferred: `characterListPacketsSent=False`.
3. Many lines of async character/equipment work.
4. `ServerCharacterList` sent.
5. Catalog response finally sent (full categories/offers/finalise).

The client issued `082D` immediately after pregame packets (`0x0966`, `0x0968`, `0x097F`, …) but the server waited for the full character DB load. That gap is long enough for the UI to fail with **Catalogue Unavailable** before the server responds.

### Implemented fix

- `IWorldSession.HasSentPregameAccountPackets` — set `true` when `SendPregameAccountPackets` completes in `CharacterListManager.QueueCharacterListPackets`.
- `ClientStorefrontRequestCatalogHandler.CanProcessCatalogRequest` — requires `HasSentPregameAccountPackets` and, if `Player != null`, `!Player.IsLoading`. Does **not** require `HasSentCharacterListPackets`.

Catalog handler still sends account inventory via `InventoryManager.SendInitialPackets()` then `StorePurchaseHistoryManager` + `GlobalStorefrontManager.HandleCatalogRequest`.

---

## Secondary — initial `ServerStoreCatalogUpdated` (`0x0989`)

### Evidence

Decompile (`INITIAL_FINDINGS.md`, storefront `0x0987..0x0991` section):

- `Storefront_HandleStoreCatalogUpdated` (`14044cf40`) marks the catalog dirty; it is not a success completion.
- Historical NexusForever `HandleCatalogRequest` enqueued `ServerStoreCatalogUpdated` before categories/offers/finalise.

Live logs **after** removing `0989` from the initial response still showed a healthy catalog (`30` categories, `348` offer groups, `478` offers, `1301` item rows) ending in `ServerStoreFinalise` only. Server-side packet shape and DB content were not the blocker once client banner fetch was fixed.

### Implemented fix

`GlobalStorefrontManager.HandleCatalogRequest`:

1. `ServerStoreCategories` (`0x0988`)
2. `ServerStoreOffers` (`0x098B`, 20 groups per packet)
3. `ServerStoreFinalise` (`0x0987`) — **no** `ServerStoreCatalogUpdated` on first open

---

## What the server sends when healthy

From `StorefrontCatalogDiagnostics` on a seeded world DB (`laughingws_store_catalog_seed.sql`):

- Pregame: `ServerAccountCurrencySet`, entitlements, tier (character list flow).
- On catalog request: account item cache/list, pending clear, cooldowns, daily login, purchase history ready.
- Store: categories → offer batches → finalise.

No `ServerStoreError` with `CatalogUnavailable` is emitted by current server purchase handlers; the UI string comes from the client-side `StoreError` event mapping.

---

## Verification checklist

### Build

```powershell
dotnet build Source\NexusForever.ClientConnector\NexusForever.ClientConnector.csproj -v minimal --nologo
dotnet build Source\NexusForever.WorldServer\NexusForever.WorldServer.csproj -v minimal --nologo
dotnet test Source\NexusForever.Game.Tests\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~Storefront|FullyQualifiedName~CharacterListManagerTests.QueueCharacterListPackets"
```

### Launcher script parse

```powershell
powershell -NoProfile -Command "& { $null = [System.Management.Automation.Language.Parser]::ParseFile('Tools/Setup/Start-NexusForeverLocal.ps1', [ref]$null, [ref]$errors); if ($errors) { $errors; exit 1 } }"
```

### Runtime

1. Restart auth/world (`Restart-NexusForeverAuthWorldLocal.ps1` or full `Start-NexusForeverLocal.ps1`).
2. **Exit any running WildStar process**; restage ClientConnector + `Client64\config.json` with `RealmDataCenterId: 6`.
3. Open store from character select and in-world; confirm no `malformed store banner data` in `Errors\WildStar64*.log`.
4. World log: catalog should start soon after `082D` with `pregameAccountPacketsSent=True`, without waiting for `ServerCharacterList`.

### World DB sanity (empty catalog guard)

```sql
SELECT COUNT(*) FROM store_category;
SELECT COUNT(*) FROM store_offer_group;
```

Counts should be non-zero when `laughingws_store_catalog_seed.sql` was imported via setup.

---

## Related code and references

| Artifact | Role |
| --- | --- |
| `Source/NexusForever.Game.Static/Storefront/StoreError.cs` | `CatalogUnavailable = 0` |
| `Source/NexusForever.WorldServer/.../ClientStorefrontRequestCatalogHandler.cs` | Catalog request orchestration |
| `Source/NexusForever.Game/Storefront/GlobalStorefrontManager.cs` | Catalog packet cache and send order |
| `Source/NexusForever.GameTable/Model/RealmDataCenterEntry.cs` | `StoreBannerDataUrlTemplate` field |
| `wildstar_client_mysql/RealmDataCenter.tbl.sql` | Extracted realm rows |
| `Decomp/Analysis/INITIAL_FINDINGS.md` | Storefront opcode consumer notes (`0x0987`–`0x0991`) |
| `Tools/Setup/README.md` | Local launch + `RealmDataCenterId` override |

---

## Regression (2026-05-30 follow-up)

`ClientStorefrontRequestCatalogHandler` briefly required `HasSentCharacterListPackets`, a non-null `Player`, and `!Player.IsLoading` before answering `0x082D`. That blocked **character-select** store and account inventory (no player yet) and could defer in-world opens until after a long async character DB load. The handler was corrected again to match the table above: gate on `HasSentPregameAccountPackets` only, defer when `Player != null && Player.IsLoading`, and call `InventoryManager.SendInitialPackets()` on every catalog response.

### Confirmed wire-format root cause (client logs 2026-05-30)

With `realmDataCenterId 6`, world logs showed a full `0x0988` → `0x098B` → `0x0987` catalog, but WildStar still logged:

- `Malformed Packet: unPackFromStreamFn failure : <2440` (`0x0988` `ServerStoreCategories`)
- then `<2443` (`0x098B` `ServerStoreOffers`) and `Invalid Message Id #988` (stream desync)

Ghidra readers (`Server0x098F_ReadPayload` @ `1400a1230`, `ServerStoreOffers_Offer_ReadPayload` @ `1400a0dc0`):

| Field | Client reads | Buggy server write (May 2026 packet rename) |
| --- | --- | --- |
| Nested currency row `CurrencyType` | **32-bit** `uint32` | `writer.Write(CurrencyType)` → **64-bit** enum |
| Offer `field_7` @ `+0x30` | **8 bits** via `FUN_14006be30` @ `1400a0dc0` | `writer.Write(byte)` (8 bits); do not use `Write(ulong)` |
| Offer prices @ `+0x18` (8 bytes) | `FUN_140337160` @ `1400a0dc0` | Sequential `Write(float)` ×2 (misaligns after wide strings) |
| Group category id/index arrays | `FUN_140337160` ×2 @ `1400a0fa0` | Sequential `Write(uint)` loops |

Three currency packages at 64 bits each put the client **96 bits** past the real `0x0988` tail, which explains `2440` and the follow-on `098B` / `#988` errors even when the catalog DB and handler timing are correct.

**Fix:** emit nested currency rows through `ServerStoreCurrencyPackageRow` (32-bit tail), write offer `field_7` as `byte RetailCatalogWireByte` (8 bits via `FUN_14006be30`, not `ulong`), encode offer prices with `WriteRetailCompositeByteSpan` (8 bytes) and category arrays with `WriteRetailCompositeUInt32Array` per Ghidra `1400a0dc0` / `1400a0fa0`. Startup validation must use the same composite reads in `RetailStoreOffersWireReader` (sequential float/uint reads falsely pass).

## Single catalog delivery (2026-05-31)

Live world log showed **two** full catalog passes per login:

1. Character-select `0x082D` (`player=0`) → full `0x0988`/`0x098B`/`0x0987`
2. World login → `0x0989` + the same full catalog again inside / right after the login burst

That matched a broken client state: Featured tiles visible, then *Catalogue Unavailable*, with no
`Malformed` lines at `logDefaultLevel 4`.

Mitigation:

- Character-select `0x082D` sends account inventory + `0x098E` only (**no** catalog).
- After world login, `Player.SendDeferredInWorldStorefrontCatalog` (next event tick) sends purchase
  history + the **first and only** full catalog (`0x0988` → `0x098B` → `0x0987`, no leading `0x0989`).
- In-world repeat `0x082D` still uses `0x0989` + catalog when the account already has a catalog.
- Catalog delivery remains keyed by **account id** (not network session id).

### Defer catalog out of login / 082D bursts

- In-world `0x082D` defers `HandleCatalogRequest` one session event tick after inventory/`0x098E`.
- World bootstrap catalog defers one tick after `SendPacketsAfterAddToMap` (not in the
  `ServerPlayerCreate` burst).

## Resolution state

| Item | State |
| --- | --- |
| Configurable local `RealmDataCenterId` (default 6) | **Implemented** |
| Pregame-gated catalog response (no player/char-list gate) | **Implemented** |
| No initial `0x0989` on catalog open | **Implemented** |
| Live client confirmation after relaunch | **User verification** — requires new process with restaged connector/config and rebuilt world server |
