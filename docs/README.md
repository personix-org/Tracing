# Personix.Tracing

Distributed tracing helpers for .NET services. Gives an application one `ActivitySource` behind a
static helper, and carries W3C Trace Context across process boundaries so a request stays a single
trace even when it crosses a queue, a webhook, or a background worker.

## Contents

- `ActivityHelper` – static wrapper over a single `ActivitySource`: configure its name once, start
  activities, read the current `traceparent`, and continue a trace from an incoming one.
- `TracingServiceRegistration` – extension method `AddApplicationTracing()` that configures the
  source during DI registration.

## Installation

```xml
<PackageReference Include="Personix.Tracing" Version="1.0.0" />
```

## Usage

### 1. Register during start-up

Register before the OpenTelemetry pipeline, so the source name is set by the time the exporter
starts listening.

```csharp
using Personix.Tracing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationTracing("MyApp.Application");
```

Called without an argument the source is named `Application`.

The source name must match what the OpenTelemetry pipeline listens to:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource("MyApp.Application"));
```

### 2. Start an activity

```csharp
using Personix.Tracing;

using var activity = ActivityHelper.StartActivity("LoadCustomer");
activity?.SetTag("customer.id", customerId);
```

`StartActivity` returns `null` when nothing listens to the source — that is normal and the `using`
statement handles it. Use `activity?.` rather than `activity!.`.

### 3. Carry the trace across a process boundary

On the sending side, read the current context and put it on the message or request:

```csharp
var traceparent = ActivityHelper.GetCurrentTraceparent();
message.Headers["traceparent"] = traceparent;
```

On the receiving side, continue the same trace instead of starting a new one:

```csharp
using var activity = ActivityHelper.StartActivityFromTraceparent(
    "ProcessMessage",
    message.Headers["traceparent"],
    ActivityKind.Consumer);
```

If the header is missing or malformed, a new root activity is started — the call never throws.

## Notes

- `ActivityHelper` holds the source in a static field. `Configure` is meant to be called once during
  start-up, not per request.
- `GetCurrentTraceparent` returns the W3C format `00-{traceId}-{spanId}-{flags}`, where the flags are
  `01` when the activity is recorded and `00` when it is not.

## Licence

MIT — see [LICENSE](LICENSE).
