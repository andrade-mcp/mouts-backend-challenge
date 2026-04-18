# Developer Evaluation - Sales API

> This is my submission for the AB InBev .NET test - Sales API 
 DDD, CQRS, and the discount tiers from the challenge brief.

Submission for the Coodesh / Mouts / AB InBev .NET challenge. Adds a complete
**Sales** feature to the provided template, mirroring the existing `Users` /
`Auth` architecture (Clean Architecture + DDD + CQRS/MediatR + EF Core +
Postgres).

- **Fork:** https://github.com/andrade-mcp/mouts-backend-challenge
- **Branch:** `feat/sales-api`
- **Original brief:** see the collapsed section at the bottom.

---

## Quick start

Prereqs: Docker Desktop (or any local Postgres 13+), .NET 8 SDK.

```bash
cd template/backend

# 1. Bring up Postgres (docker-compose also boots the API container, but you
#    can also run the WebApi directly with `dotnet run` and connect to the db).
docker-compose up -d

# 2. Apply migrations (creates Users + Sales + SaleItems)
dotnet ef database update \
    --project src/Ambev.DeveloperEvaluation.ORM \
    --startup-project src/Ambev.DeveloperEvaluation.WebApi

# 3. Run the API
dotnet run --project src/Ambev.DeveloperEvaluation.WebApi
```

Swagger: `https://localhost:8081/swagger` (or whatever the dev port prints).

### Get an auth token

All `/api/sales` endpoints are gated with `[Authorize]`. Flow:

```bash
# Create a user
curl -X POST http://localhost:8080/api/users -H 'content-type: application/json' -d '{
  "username": "admin", "email": "admin@ambev.local",
  "password": "Test@123", "phone": "+5511999998888",
  "status": 1, "role": 3
}'

# Authenticate → returns a JWT in the "token" field
curl -X POST http://localhost:8080/api/auth -H 'content-type: application/json' -d '{
  "email": "admin@ambev.local", "password": "Test@123"
}'
```

Paste the token into Swagger's **Authorize** button (`Bearer <token>`).

---

## API reference

| Verb   | Route                                          | Purpose                                        |
|--------|------------------------------------------------|------------------------------------------------|
| POST   | `/api/sales`                                   | Create a sale                                  |
| GET    | `/api/sales/{id}`                              | Fetch a single sale with all items             |
| GET    | `/api/sales`                                   | Paged + filtered list (header only)            |
| PUT    | `/api/sales/{id}`                              | Full-replacement update (header + items)       |
| DELETE | `/api/sales/{id}`                              | Soft-cancel (sets `IsCancelled=true`)          |
| POST   | `/api/sales/{saleId}/items/{itemId}/cancel`    | Cancel a single line within a sale             |

**List filters** (all optional, AND-combined):
`saleNumber`, `customerId`, `branchId`, `startDate`, `endDate`,
`includeCancelled`, `orderBy` (e.g. `saleDate desc, saleNumber asc`),
`page`, `pageSize` (capped at 100).

Example: `GET /api/sales?branchId=<rj-copa>&startDate=2026-04-01` filters the
Rio de Janeiro - Copacabana branch from April onwards, ordered by
`saleDate desc` by default.

### Sample create payload

```json
{
  "saleNumber": "S-0001",
  "saleDate": "2026-04-17T12:00:00Z",
  "customerId": "11111111-1111-1111-1111-111111111111",
  "customerName": "Distribuidora de Bebidas Sergio Andrade",
  "branchId":   "22222222-2222-2222-2222-222222222222",
  "branchName": "São Paulo - Centro",
  "items": [
    { "productId": "33333333-3333-3333-3333-333333333333", "productName": "Brahma Duplo Malte 350ml", "quantity": 3,  "unitPrice": 4.50 },
    { "productId": "44444444-4444-4444-4444-444444444444", "productName": "Skol Pilsen 600ml",        "quantity": 12, "unitPrice": 9.90 }
  ]
}
```

Expected computed output on the line items:
- Item 1 (qty 3)  -> discount 0%,  total R$ 13.50
- Item 2 (qty 12) -> discount 20%, total R$ 95.04
- Sale total: R$ 108.54

### Status codes

| Code | Meaning                                                                 |
|------|-------------------------------------------------------------------------|
| 200  | OK - read + mutations                                                   |
| 201  | Created (POST)                                                          |
| 400  | Validation failure (FluentValidation, envelope lists each error)        |
| 401  | Missing or invalid JWT                                                  |
| 404  | Unknown sale id                                                         |
| 409  | Domain conflict: duplicate `SaleNumber`, already-cancelled sale/item, or optimistic-concurrency mismatch on update |

All responses use the template's envelope (`ApiResponse`,
`ApiResponseWithData<T>`, `PaginatedResponse<T>`).

---

## Business rules

Per the challenge README:

| Quantity  | Discount | Behavior                                 |
|-----------|----------|------------------------------------------|
| 1–3       | 0%       | No discount                              |
| 4–9       | 10%      | Tier 1                                   |
| 10–20     | 20%      | Tier 2                                   |
| 21+       | reject   | `DomainException` → 409 (validator → 400 on the API) |

Discount and line total are **always** computed by the domain - never accepted
from the client. Constants live on `SaleItem` so the mapping and the
validators share one ceiling.

---

## Design decisions

- **External Identities for customer / branch / product.** Those live in other
  bounded contexts in a real DDD landscape; the aggregate stores
  `id + denormalized display name`.
- **Soft-cancel instead of hard delete.** `DELETE /sales/{id}` flips
  `IsCancelled` and raises `SaleCancelledEvent`, matching the README's event
  vocabulary and preserving audit trail.
- **Log-only event handlers.** The README explicitly permits skipping a real
  broker. Events are `MediatR INotification` records published by the command
  handler after `SaveChangesAsync` succeeds; swapping in RabbitMQ /
  Azure Service Bus / Kafka is a pure handler replacement with no call-site
  change. Publish failures are logged and swallowed so a broken sink can't
  undo a committed write.
- **xmin as shadow concurrency token on Sales.** Postgres' system
  row-version column is tracked via an EF shadow property, so the domain
  stays free of ORM details and mismatches surface as
  `DbUpdateConcurrencyException` → 409.
- **Full-replacement semantics for Update.** PUT sends the whole header +
  item set; the aggregate's `ReplaceItems` swaps lines and raises one
  `SaleModifiedEvent`. Simpler than a per-line diff and keeps the audit
  story clean.
- **Indexes on real filter columns** (`SaleNumber` unique, `CustomerId`,
  `BranchId`, `SaleDate`) so the list endpoint is index-friendly on day one.
- **Decimal precision** pinned `(18,2)` for money, `(5,4)` for the discount
  fraction.
- **Client-side `Guid.NewGuid()`** on aggregate construction so children have
  stable ids before the first `SaveChangesAsync` (a domain test caught an
  earlier bug where two new items both had `Guid.Empty`).
- **Global exception middleware** maps `KeyNotFoundException → 404`,
  `DomainException / InvalidOperationException / DbUpdateConcurrencyException → 409`,
  everything else → 500 with the error logged server-side. The existing
  `ValidationExceptionMiddleware` keeps its dedicated 400 handler.

### Template fixes made along the way

Called out in the commit log:

- `fix(ORM): point correct migrations project` - design-time
  `DbContextFactory` said `WebApi`, `Program.cs` said `ORM`; scaffolder
  refused to run until aligned.
- `fix(ORM): migration` (xmin shadow token) - Npgsql can't map `byte[]` to
  `xid`; moved the concurrency token to a shadow property.
- `fix(domain): assign Id at construction for Sale and SaleItem` - caught
  by a unit test where `CancelItem(id)` matched the wrong line because
  both items had `Guid.Empty`.

---

## Tests

```bash
dotnet test Ambev.DeveloperEvaluation.sln
```

**94 tests** currently pass:

- **23 domain** - discount tiers (`SaleItemTests`), aggregate invariants and
  event emission (`SaleTests`).
- **17 handlers** - one class per application slice, happy paths + edge
  cases (not-found, duplicate key, already-cancelled, etc.).
- **5 functional** (`WebApplicationFactory<Program>`) - HTTP pipeline
  end-to-end with `ISaleRepository` mocked, real middleware + auth +
  envelopes. Tests: empty body → 400, qty-21 → 400, valid payload → 201,
  unknown id → 404, duplicate SaleNumber → 409.
- **49 pre-existing** - the template's User / Auth tests, still green.

> Real-Postgres integration coverage (xmin concurrency, decimal precision,
> index usage) is left for the `Integration` project once a test DB
> (Testcontainers or local Postgres) is wired up - explicitly out of scope
> for this submission's time-box.

---

## Project structure (Sales-related only)

```
src/
├── Ambev.DeveloperEvaluation.Domain/
│   ├── Entities/           Sale.cs, SaleItem.cs
│   ├── Events/             DomainEvent.cs + Sale{Created,Modified,Cancelled}Event.cs, ItemCancelledEvent.cs
│   ├── Repositories/       ISaleRepository.cs
│   └── Validation/         SaleValidator.cs, SaleItemValidator.cs
├── Ambev.DeveloperEvaluation.Application/Sales/
│   ├── CreateSale/         command + handler + validator + result + profile
│   ├── GetSale/            query + handler + validator + result + profile
│   ├── ListSales/          query + handler + validator + result (summary) + profile
│   ├── UpdateSale/         command + handler + validator + result + profile
│   ├── CancelSale/         command + handler + validator + result + profile
│   ├── CancelSaleItem/     command + handler + validator + result
│   └── Events/             log-only INotificationHandler<T> per event
├── Ambev.DeveloperEvaluation.ORM/
│   ├── Mapping/            SaleConfiguration.cs, SaleItemConfiguration.cs
│   └── Repositories/       SaleRepository.cs
└── Ambev.DeveloperEvaluation.WebApi/
    ├── Features/Sales/     SalesController.cs + per-slice Request/Response/Validator/Profile
    └── Middleware/         ExceptionHandlingMiddleware.cs (domain → HTTP status mapping)

tests/
├── Ambev.DeveloperEvaluation.Unit/
│   ├── Domain/Entities/    SaleTests.cs, SaleItemTests.cs, TestData/SaleTestData.cs
│   └── Application/Sales/  one *HandlerTests.cs per slice + TestData/SalesHandlerTestData.cs
└── Ambev.DeveloperEvaluation.Functional/
    └── Sales/              SalesApiFactory.cs, SalesEndpointsTests.cs
```

---

## Commit history

The branch is 23 commits; each one builds cleanly, and the log reads as an
iterative narrative (Domain -> tests -> ORM -> Application -> WebApi -> Functional
tests -> README) with three fix commits inserted where tests or tooling caught
real issues:

```
git log --oneline main..feat/sales-api
```

---

<details>
<summary>Original challenge statement (unchanged)</summary>

`READ CAREFULLY`

## Use Case
**You are a developer on the DeveloperStore team. Now we need to implement the API prototypes.**

As we work with `DDD`, to reference entities from other domains, we use the `External Identities` pattern with denormalization of entity descriptions.

Therefore, you will write an API (complete CRUD) that handles sales records. The API needs to be able to inform:

* Sale number
* Date when the sale was made
* Customer
* Total sale amount
* Branch where the sale was made
* Products
* Quantities
* Unit prices
* Discounts
* Total amount for each item
* Cancelled/Not Cancelled

It's not mandatory, but it would be a differential to build code for publishing events of:
* SaleCreated
* SaleModified
* SaleCancelled
* ItemCancelled

If you write the code, **it's not required** to actually publish to any Message Broker. You can log a message in the application log or however you find most convenient.

### Business Rules

* Purchases above 4 identical items have a 10% discount
* Purchases between 10 and 20 identical items have a 20% discount
* It's not possible to sell above 20 identical items
* Purchases below 4 items cannot have a discount

## Template documentation

- [Overview](/.doc/overview.md)
- [Tech Stack](/.doc/tech-stack.md)
- [Frameworks](/.doc/frameworks.md)
- [Project Structure](/.doc/project-structure.md)

</details>

---

## Sobre o autor

Submitted by **Sergio Andrade** - Senior Full Stack Developer (.NET + React),
based in São Paulo. Candidate for the AB InBev position via Mouts TI.

- GitHub: [@andrade-mcp](https://github.com/andrade-mcp)
- Submission date: 17 abr 2026
