# FluentHosting.IntegrationTests Agent Notes

## Purpose
`FluentHosting.IntegrationTests` provides end-to-end coverage for the public surface of `FluentHost` using xUnit and .NET 8. Tests exercise the listener with real HTTP requests.

## Test Harness
- `FluentHostIntegrationTests` centralises wiring through `RunHostTestAsync`, which allocates a free loopback port via `TcpListener` and ensures `FluentHost.Stop()` runs in a `finally` block.
- Each scenario uses a scoped `HttpClient` configured with the generated base address; connections are explicitly closed between requests to release sockets quickly.
- Follow this harness when adding new tests so that port management and cleanup stay consistent.

## Coverage
- GET handler happy path with repeated requests.
- DELETE handler returning `204 No Content` with an empty body.
- Handler precedence assertions (most recent registration wins for a route).
- Wildcard routing checks for both suffix matches and the `"*"` fallback handler.
- `JsonResponse<T>` serialization and content-type validation.
- CORS preflight success and rejection paths based on `CorsConfig`.

## Runtime Considerations
- Tests no longer depend on a fixed port, so the suite can run in parallel without conflicts.
- The helper tears the host down even on assertion failures; avoid bypassing it to prevent orphaned listeners.

## How to Run
Execute `dotnet test FluentHosting.sln` from the repository root (or target the `FluentHosting.IntegrationTests` project directly). No additional tooling or environment setup is required beyond the .NET 8 SDK.
