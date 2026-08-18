# Order Processor

My solution to the .NET refactoring and architecture exercise. The original
brief is kept in [docs/brief.md](docs/brief.md), and the inherited code sits
untouched in the first commit, so the git history reads as the refactor
itself: each commit is one reviewable step from a god-class to a working,
tested API.

## Running it

The only requirement is the .NET 10 SDK. In Development the API uses a local
SQLite file which it creates and seeds on startup, so there is nothing to
install or configure.

```bash
cd OrderProcessor/OrderProcessor
dotnet run
```

The API listens on http://localhost:5080. The seed data is customer 1
(Ada Lovelace) and three products: Keyboard (49.99), Mouse (24.50) and
Monitor (189.00).

Place an order:

```bash
curl -X POST http://localhost:5080/api/orders \
  -H "Content-Type: application/json" \
  -d '{"customerId":1,"customerEmail":"ada@example.com","items":["Keyboard","Mouse"],"paymentMethod":"CreditCard","discountPercentage":10}'
```

The curl examples use bash quoting; on Windows, run them from Git Bash.

Fetch a customer's order history:

```bash
curl http://localhost:5080/api/orders/history/1
```

Payment methods are CreditCard, PayPal and BankTransfer. In Development the
two card providers resolve to mock endpoints under /mock on the same host,
so the whole flow runs locally. Bank transfers involve no provider call and
are recorded as pending. The OpenAPI document is served at /openapi/v1.json.

Run the tests from the solution folder:

```bash
cd OrderProcessor
dotnet test
```

## Architecture

A single ASP.NET Core project organised by concern:

| Folder | Responsibility |
|---|---|
| Controllers | The HTTP edge: orders endpoint, mock payment providers, global exception handler |
| Services | IOrderProcessor and the orchestrator |
| Payments | IPaymentStrategy, the three provider strategies, payment result types |
| Email | IEmailSender with SMTP and no-op implementations |
| Data | EF Core DbContext and the Development seeder |
| Models | Entities, the request DTO and result types |
| Options | Typed configuration classes |

A request enters OrdersController and is validated before any work happens.
The orchestrator then does one job: resolve the payment strategy, confirm
the customer exists, price the items in a single query, apply the discount,
charge through the strategy, record the order inside a transaction, and send
the confirmation email. Every external concern sits behind an interface and
arrives by constructor injection.

Decisions worth explaining:

**Dependency rule inside one project.** The layout follows the Clean
Architecture dependency rule: the orchestrator depends on abstractions,
adapters implement them, and Program.cs composes everything. The boundaries
are folders and interfaces because there is a single use case. The seams
where separate projects would be cut if this grew are already in place:
IOrderProcessor, IPaymentStrategy and IEmailSender. The brief's
locality-of-behaviour point weighed in this choice; one readable project
serves a single flow better than four assemblies.

**DbContext used directly in the service.** EF's DbContext already provides
repository and unit-of-work semantics, so wrapping it in a repository
interface for one aggregate would add indirection without a second consumer
to justify it. This is the one conscious deviation from a strict layered
split, and it is the first seam to introduce if another data consumer
appears.

**Transaction scope.** The order insert runs inside an explicit database
transaction. The transaction covers database work only; it is never held
open across the payment or email network calls.

**Email failure does not fail the order.** The legacy code intended this
policy, and it is kept, made explicit and tested: the order commits first,
and a failed confirmation is logged and reported as CompletedEmailFailed.

**Wide logging.** Each order attempt emits exactly one structured event on
every exit path, including unhandled exceptions: customer, item count,
payment method, payment outcome with provider code, order outcome, total
and duration. HTTP request logging covers the edge.

**Money.** Totals are decimal end to end and rounded to cents (half away
from zero) after the discount, so the charged amount and the stored amount
are the same number. Amounts sent to providers are formatted with the
invariant culture.

**Configuration and secrets.** Non-secret settings live in appsettings.json
with per-environment overlays. Secrets come from user-secrets in Development
and environment variables elsewhere; .env.example documents them. Provider
response codes (202, 400, 409, 500) live in the PaymentStatusCode enum, and
enums serialise as names over JSON.

## Issues found in the inherited code

Beyond the problems the brief lists (SQL injection, hard-coded credentials,
the god-class, exception handling, naming, undisposed connections, tight
coupling), working through the file surfaced these:

- The project did not compile: the code referenced System.Data.SqlClient
  and the project never declared the package.
- `(double)cmd.ExecuteScalar()` unboxes a SQL decimal into a double, which
  throws InvalidCastException on the first product, and throws a null
  reference when a product name is unknown, so the happy path could never
  complete.
- A pending bank transfer was inserted with status 'Completed'.
- `"?amount=" + total` formats with the server culture; a comma-decimal
  locale sends `amount=12,50` to the payment provider.
- Payment success was detected with `Contains("success")` on a raw response
  body while the providers document proper status codes.
- The order was charged and inserted before anyone checked that the
  customer exists, and nothing validated the request at all.
- FindHistory took the customer id as a string, used SELECT *, never
  disposed its reader, and returned display strings instead of data.
- Money was computed in double while the entities store decimal.
- WebClient has been obsolete since .NET 6.
- Nothing stops the same request charging twice; the providers' 409
  duplicate code hints at the missing idempotency (see next steps).

## Where the brief's asks live

| Ask | Where |
|---|---|
| Strategy pattern (bonus) | Payments/PaymentStrategies.cs |
| Dependency injection | Program.cs composition root, constructor injection throughout |
| Validation of data | Models/ProcessRequest.cs, enforced at the controller |
| Wide logging | Services/OrderProcessor.cs, one event per attempt |
| Error handling | Controllers/GlobalExceptionHandler.cs, typed outcomes, tested email policy |
| DB transactions | Explicit transaction around the order insert |
| No secrets or magic values | Options pattern, user-secrets, enums and named constants |
| Mock endpoints | Controllers/MockPaymentProvidersController.cs |
| .NET 8-10 concepts | Primary constructors, collection expressions, records, file-scoped namespaces, IExceptionHandler, built-in OpenAPI |

## Next steps

- Idempotency keys on order submission so a retried request cannot charge
  twice.
- An outbox for the confirmation email, so a crash between commit and send
  cannot lose the message.
- EF migrations in place of EnsureCreated outside Development.
- Retry and circuit-breaker policies on the provider HTTP clients.
- A settlement flow that moves pending bank-transfer orders to completed.
- Authentication and authorisation on the API.
- Separate projects along the existing interface seams once a second use
  case arrives.
