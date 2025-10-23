# FluentHosting Repository Notes

## Layout
- `FluentHosting/` – reusable library that wraps `HttpListener` with a fluent handler pipeline (targets .NET 8).
- `FluentHosting.Tests/` – xUnit integration-style tests that exercise the host against real HTTP requests.
- `FluentHostingExample/` – console sample that wires several routes and a fallback handler to demonstrate usage.

## Quick Facts
- Library has **no external dependencies**; only BCL packages are used.
- Core entry point is `FluentHost`, which accepts a `Name` (prefix such as `http://localhost`) and `Port`.
- Handlers are registered via the `Handles` extension method; the most recently added handler runs first.
- `CorsConfig` is optional per handler and automatically provisions an `OPTIONS` preflight route.
- Response helpers (`StringResponse`, `JsonResponse<T>`) cover current built-in scenarios. Nothing ships for static files.

## Working With the Code
- When adding new endpoints or features, update the relevant `Agents.md` alongside the implementation to keep documentation accurate.
- Tests currently bind to port `1337`; coordinate or parameterize additional tests to avoid conflicts.
- The example application is a simple smoke test; it mirrors library capabilities and is safe to run locally.
