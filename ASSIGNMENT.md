# Assignment: Unit Testing the Mini Checkout Engine

## Goal

You receive a small, working checkout codebase. Your task is **not** to implement new features.

Your task is to **write automated tests** that prove the existing business rules work correctly, that dependencies are used at the right time, and that the project architecture stays clean.

## Business scenario

A store wants a mini checkout engine.

A customer places an order with one or more products. The system:

1. Validates the order
2. Checks inventory
3. Calculates money totals
4. Saves the order when checkout succeeds
5. Sends a confirmation notification when checkout succeeds
6. Returns a structured receipt

There is no real database, no login system, and no API. External systems are represented by interfaces so you can replace them with mocks in tests.

## Existing project structure

| Project | What it contains | What it is responsible for |
|---|---|---|
| `Simulation.Checkout.Domain` | `Order`, `OrderItem`, `CheckoutReceipt`, exceptions | Core business data and error types |
| `Simulation.Checkout.Application` | `CheckoutService`, `IOrderRepository`, `IInventoryService`, `INotificationService` | Checkout rules and orchestration |
| `Simulation.Checkout.Infrastructure` | `InMemoryOrderRepository` | Simple in-memory storage implementation |
| `Simulation.Checkout.Tests` | *(empty on purpose)* | **You write all tests here** |

### Simple explanation of the layers

Think of the solution as three boxes:

1. **Domain** — the words of the business: order, item, receipt, errors.
2. **Application** — the brain: how checkout works and which helpers it needs.
3. **Infrastructure** — the tools: how an order is stored in memory.

Rules for how the boxes may talk to each other:

- Domain must stay independent. It must not know Application or Infrastructure.
- Application may use Domain, but must not know Infrastructure.
- Infrastructure may use Application and Domain.
- Tests may look at all projects.

## Business rules you must cover

These rules are already implemented in `CheckoutService`. Your tests must verify them.

1. An order must contain **at least one item**.
2. Every item quantity must be **greater than zero**.
3. Product unit prices **cannot be negative**.
4. Subtotal is calculated as: `sum(unitPrice × quantity)`.
5. If subtotal is **at least 1,000 EGP**, apply a **10% discount**.
6. Discount must **never exceed 100 EGP**.
7. Inventory must be checked **before** completing checkout.
8. If inventory is insufficient, checkout **fails**, and the system must **not**:
   - save the order
   - send a notification
9. On success, the system must:
   - save the order
   - send a confirmation notification
10. A successful checkout returns a `CheckoutReceipt` with customer info, items, subtotal, discount, and total.

Money values use `decimal`.

Useful constants in `CheckoutService`:

- `DiscountThreshold = 1000`
- `DiscountRate = 0.10`
- `MaxDiscount = 100`

## Libraries you must practice

The test project already references these packages. Use them intentionally.

| Library | Practice goal |
|---|---|
| **xUnit** | Write tests with `[Fact]`, `[Theory]`, and `[InlineData]` |
| **FluentAssertions** | Assert results, exceptions, and collections clearly |
| **Moq** | Mock repository, inventory, and notification dependencies; use `Verify` |
| **AutoFixture** | Generate orders/items automatically instead of hard-coding every object |
| **Bogus** | Generate realistic customer/product test data |
| **Verify.Xunit** | Snapshot-test a `CheckoutReceipt` |
| **coverlet.collector** | Collect code coverage |
| **ReportGenerator** | Produce an HTML coverage report |
| **NetArchTest.Rules** (`NetArchTest.Rules` NuGet package) | Enforce architecture and naming rules |

## Required testing scenarios

Aim for about **15–18 tests**, distributed roughly like this:

| Category | Suggested count | What to focus on |
|---|---:|---|
| Validation & business rules | 5 | Empty order, invalid quantity, negative price, discount threshold, discount cap |
| Parameterized tests | 2 | Multiple inputs with `[Theory]` / `[InlineData]` |
| Mocking & interaction verification | 3 | Inventory failure does not save/notify; success saves and notifies; inventory checked before save |
| AutoFixture & Bogus | 2 | Generated data still exercises real rules |
| Snapshot testing | 1 | Stable snapshot of `CheckoutReceipt` |
| Architecture tests | 4 | Dependency direction + naming conventions |
| Coverage report | task | Generate and submit/share the HTML report |

### Failure-path requirement

Do **not** write only happy-path tests.

You must include cases where checkout fails, and you must prove that:

- the order repository is **not** called when checkout fails because of inventory
- the notification service is **not** called when checkout fails because of inventory

## Architecture rules to verify

Write architecture tests that confirm:

1. Domain does **not** depend on Application.
2. Domain does **not** depend on Infrastructure.
3. Application does **not** depend on Infrastructure.
4. Naming conventions:
   - classes ending with `Service` live in Application (for this small solution)
   - repository implementations end with `Repository`
   - service/repository interfaces start with `I`

Do not weaken production references to make architecture tests pass. The starter already follows these rules.

## Coverage report requirement

After your tests pass:

1. Run tests with coverlet coverage collection
2. Generate an HTML report with ReportGenerator
3. Review the report and make sure important checkout logic is covered

See `README.md` for the exact commands.

## Acceptance criteria

Your submission is complete when:

- [ ] The solution builds
- [ ] All of your tests pass
- [ ] You covered validation rules, discount rules, and inventory failure behavior
- [ ] You used xUnit `Fact` and `Theory`
- [ ] You used FluentAssertions
- [ ] You used Moq with interaction verification (`Verify`)
- [ ] You used AutoFixture and Bogus at least once each
- [ ] You included at least one Verify.Xunit snapshot test for `CheckoutReceipt`
- [ ] You included architecture tests for dependency direction and naming
- [ ] You generated a coverage HTML report

## What not to do

- Do not rewrite `CheckoutService` business rules
- Do not add a database, API controllers, authentication, or UI
- Do not delete the provided interfaces
- Do not submit empty tests that always pass
- Do not copy test solutions from classmates or AI without understanding them

## Where to start

1. Read `CheckoutService`
2. Read the domain models and exceptions
3. Create your first failing validation test
4. Expand into discount, inventory, mocking, snapshot, and architecture tests
5. Generate coverage and review gaps

Good luck.
