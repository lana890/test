# Frontend API Performance Triage (from `review_Apies.txt`)

**Date:** 2026-06-24
**Scope:** the exact endpoints the website calls. All of them pass through the API-wrapper + user-lockout + auth middleware on every request, so per-request overhead is shared across the whole list (the API-wrapper compact-JSON fix already helps all of them).

**Cross-cutting root cause:** the generic repository `Query()` returns a **tracked** `IQueryable` (`EskaCMSModules/EskaCMS.Core/Data/RepositoryWithTypedId.cs:74`). Read-only endpoints that **materialize entities** (return entities, or project entity-typed members) pay change-tracking CPU+RAM for nothing. Note: queries that project to a **scalar DTO** are already non-tracked, so `AsNoTracking` there is a no-op — each candidate must be checked (entity vs scalar projection) before editing.

> ⚠️ Build note: the build sandbox is unavailable in this session, so the applied fixes below are **not compiler-verified**. Build `EskaCMS.WebHost` before deploy.

## Status legend
- ✅ **Fixed** — applied in this session (safe, verified read-only + EF `using` present).
- 🟢 **Safe fix (recommended)** — one-line `AsNoTracking`/cache, but verify entity-vs-projection first.
- 🔴 **Deferred (needs build/test)** — structural change (remove includes / rewrite / parallelize); could change behavior, so needs the compile+test loop.
- ⚪ **OK / already optimized / sensitive (no cache)**.

---

## ✅ Fixes applied this session (safe, read-only)
| Endpoint | Method (file:line) | Change |
|---|---|---|
| Menus/GetMenuGroupsByCategoryId | `MenuService.GetMenuGroupsList` (`MenuService.cs:2277`) | `+ AsNoTracking()` (projects `Category`/`Menus` entities → were tracked) |
| Elements/GetSharedElements | `ElementService.GetAllSheardElements` (`ElementService.cs:277`) | `+ AsNoTracking()` (projects `Category` entity) |
| UserPreferences/GetUserPreferences | `UserPreferanceService.GetUserPreferencesByUserId` (`UserPreferanceService.cs:104`) | `+ AsNoTracking()` (eager entity graph) |
| currency/get-all-currencies | `CurrencyRatesApiService.GetAllCurrencies(long)` (`CurrencyRatesApiService.cs:31`) | `+ AsNoTracking()` (returns entities) |

(Plus earlier, related to this list: `OrderApiService.GetFilteredUserOrders` paged+AsNoTracking; `StockService`; the API-wrapper compact-JSON fix that benefits every endpoint here.)

---

## 🔴 Top issues by impact (DEFERRED — need build + test)
These are the biggest wins but are **not** one-line-safe; they change how data is loaded and must be tested.

1. **product/GetProductByCategoryId, GetProductsbyCategoryIds, catalog/GetCategoryProductsById, product-overview** → all hit `ProductWebServices.GetProductsForWeb` which loads `DiscountProducts…CustomerGroup.Users` for **every product on every listing page** (`ProductWebServices.cs:1652`). Highest traffic × worst fan-out. **Fix:** drop the `.Users` include; resolve discount eligibility via a membership check (current user's group ids).
2. **product/ProductDetails** — same `CustomerGroup.Users` fan-out (`ProductWebServices.cs:715`), no `AsNoTracking`.
3. **cart/cart-list** (+ apply-coupon) — 25-include graph incl. `…CustomerGroup.Users`, then a second 16-include re-query; double-loads the cart (`CartService.cs:222-224, 724, 750-773`). *(Cart = read-then-mutate; do not blindly add AsNoTracking to the shared loaders.)*
4. **product/ViewProductDetails / GeneralProduct/GetProductById** — `CustomerGroup.Users` fan-out (`ProductWebServices.cs:213`).
5. **Sites/GetSiteMap** — 5 large sequential queries, uncached (`AdminApiController.cs:587-591`). **Fix:** `await Task.WhenAll(...)` to parallelize (+ cache).
6. **Categories/GetCategoryBreadCrumb** — loads ALL site categories to compute one breadcrumb (`CategoryWebServices.cs:293-341`). **Fix:** walk the parent chain only.
7. **Categories/GetCategoriesNavigator** — recursion + loads all published product-category rows with `.Distinct()` (`CategoryWebServices.cs:100-218`).
8. **product/GetMinAndMaxPriceProduct** — Min and Max run as **two** queries (`ProductWebServices.cs:1797/1817/1835`). **Fix:** one aggregate query.
9. **product/GetMappingProductByAttributeValue** — `Value.ToUpper().Equals` (non-SARGable) inside a DB **transaction** for a read (`ProductWebServices.cs:900-933`).
10. **ApiInvoker/GetDataSourceListBySiteId** — N+1 in a `foreach` (`ApiMapperServiceNew.cs:1524-1526`); **ApiInvoker/Invoke** is a synchronous outbound HTTP proxy (use `IHttpClientFactory` + await).

Also: **orders/{orderId}, orders/history, Order/User/OrderDetails** already use `AsNoTracking + AsSplitQuery` but over-fetch via duplicated includes (`OrderService.cs:2071-2129`, `OrderController` history `:111-149`) — trim includes / DTO-project (medium).

---

## 🟢 Remaining safe one-line candidates (verify entity-vs-projection, then add `AsNoTracking`)
Confirmed **entity-returning** reads (good candidates) — each must have `using Microsoft.EntityFrameworkCore;`:

- `UserService` — getProfile (`:2178`), GetUserOptions (`:660`), GetUserContactInfo (`:379`)
- `UserApiService.GetUser` (`:699`)
- `LoggedInUserServices.GetUserSessions` (`:442`)
- `StateOrProvinceApiService` states (`:73`), ShippableStates (`:192`)
- `DistrictApiController` districts (`:123`)
- `WarehouseApiController` list (`~:58`)
- `SitesService.GetUsedSiteCulturesByStatus` (`:1322`)
- `PagesService` GetAllPageTemplates (`:1614`)
- `RoleService.GetRoles` (`:50`), `UserRoleService.GetAllRoleByRoleType` (`:58`)
- `PaymentProviderApiController` payments-providers (`:33`)

**Skip (no-op — scalar projections, already non-tracked):** `GeneralServices.GetCulturebysiteId` (`:570`), `TagsServices.GetTags`/`GetTagsByEntityType`. For these the real win is **caching**, not AsNoTracking.

**Do NOT add AsNoTracking to (shared read-then-write):** `OrderService.GetOrderById` (`:2035`), `CartService.GetActiveCart*` — a save on the same entity would silently stop persisting.

---

## Caching candidates (read-heavy, stable; add with keys incl. SiteId/CultureId/etc.)
`Menus/GetMenuGroupsByCategoryId`, `Categories/GetAllCategoriesByTypeId`, `General/GetCultureBySiteId`, `currency/get-all-currencies`, `warehouses`, `Elements/GetSharedElements`, `PageTemplate/GetAllPageTemplates`, `states-provinces/*`, `Sites/GetSiteMap`. Use the existing `OutputCache` (`reference`/`catalog` policies) for GETs, or `IMemoryCache.GetOrCreateAsync` with stampede protection. **Never** cache cart, checkout, payments, orders, or per-user data unless the key fully includes the user.

---

## Recommended next steps
1. **Build the accumulated changes** (diagnostics + EF batch 1 + the 4 AsNoTracking fixes above + API-wrapper) and smoke-test.
2. Once green, I take the **🔴 deferred TOP-10** one group at a time (starting with the `CustomerGroup.Users` removal on the product listing — the single biggest win), with the build/test loop so each is verified.
3. Apply the **SQL indexes** (`Performance_Indexes_MySQL.sql`) — they back many of these endpoints (product listing, menus, orders).
4. Add **caching** to the stable read endpoints above.
