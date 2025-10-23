# FluentHosting.IntegrationTests Agent Notes

## Purpose
`FluentHosting.IntegrationTests` provides end-to-end coverage for the public surface of `FluentHost` using xUnit and .NET 8. Tests exercise the listener with real HTTP requests.

## Test Harness
- `FluentHostIntegrationTests` centralises wiring through `RunHostTestAsync`, which allocates a free loopback port via `TcpListener` and ensures `FluentHost.Stop()` (or `StopAsync`) runs in a `finally` block.
- Each scenario uses a scoped `HttpClient` configured with the generated base address; connections are explicitly closed between requests to release sockets quickly.
- Helper utilities (`CreateClient`, `AssertResponseAsync`) support lifecycle tests that start and stop the same host multiple times.

## Coverage
- GET and DELETE happy-path handlers, including repeated requests and empty-body `204` responses.
- Handler precedence for duplicate routes and wildcard routing (both suffix matches and the `"*"` fallback handler).
- `JsonResponse<T>` serialization and content-type validation.
- CORS preflight success and rejection paths based on `CorsConfig`.
- Lifecycle durability: `StartAsync` / `StopAsync` cycles, double-start guards, idempotent stops, and recovery after handler exceptions (ensuring a `500` response while the host keeps serving traffic).

## Runtime Considerations
- Tests no longer depend on a fixed port, so the suite can run in parallel without conflicts.
- The helper tears the host down even on assertion failures; avoid bypassing it to prevent orphaned listeners.

## How to Run
Execute `dotnet test FluentHosting.sln` from the repository root (or target the `FluentHosting.IntegrationTests` project directly). No additional tooling or environment setup is required beyond the .NET 8 SDK.
