# FluentHosting Roadmap

This document captures the work required to evolve FluentHosting from a lightweight local/test server into a hardened hosting option that can support production workloads. Each section highlights the current gaps, why they matter, and the investments required to address them.

## Vision
- Keep the footprint minimal (no heavy frameworks, zero external dependencies by default).
- Provide a developer-friendly pipeline for local tooling, automated tests, and small services.
- Offer opt-in capabilities (packages or extension points) that address production-grade requirements without bloating the core.

## Foundational Hardening
- **Graceful lifecycle management**: implement `StartAsync`/`StopAsync`, disposal semantics, and cancellation tokens so hosts can shut down cleanly under load and integrate with hosting environments.
- **Concurrency & thread safety**: audit handler registration/lists for race conditions, guard against concurrent `Start` calls, and ensure request processing handles exceptions without crashing the listener.
- **TLS/HTTPS support**: document current lack of HTTPS, then provide configuration helpers (certificate binding, loopback certificates) or clearly state limitations if relying on external reverse proxies.
- **Request timeout & limits**: expose configuration for body size, header length, connection limits, and timeouts to avoid resource exhaustion attacks.
- **Robust error handling**: add centralized exception handling with customizable responses and structured logging hooks so unhandled exceptions do not terminate the host.

## HTTP Feature Completeness
- **Routing capabilities**: extend beyond exact + `*` routes to support parameters, constraints, and optional segments; document current behaviour until implemented.
- **Verb coverage**: confirm PATCH/HEAD/OPTIONS behaviours, allow custom verbs, and ensure responses comply with HTTP semantics (e.g., HEAD returns headers without body).
- **Content negotiation**: build helper utilities for common request parsing (JSON, form data) and response formatting with proper content-type negotiation.
- **Streaming & large payloads**: expose APIs for streaming responses and reading request bodies without buffering everything into memory.
- **CORS refinements**: handle multiple origin/header/method combinations, wildcard subdomains, and preflight caching headers; document security trade-offs.

## Developer Experience
- **Dependency injection**: provide optional integration with `Microsoft.Extensions.DependencyInjection` (e.g., `FluentHostBuilder`) so handlers can resolve services scoped per request. Maintain a thin abstraction so pure delegates remain supported.
- **Configuration model**: introduce a lightweight configuration object or builder pattern to compose prefixes, ports, handlers, and middleware in a structured way.
- **Middleware pipeline**: define a simple middleware abstraction (before/after handler) to support cross-cutting concerns like authentication, rate limiting, and compression without rewriting handlers.
- **Diagnostic tooling**: ship extensions for request logging, tracing, metrics (e.g., `EventSource`, `ILogger`), and health probes. Ensure they can be toggled off to keep the default minimal.

## Security & Compliance
- **Authentication/authorization hooks**: document how to plug in custom auth today; provide sample middleware or integration with external auth providers.
- **Input validation**: guard against header injection, path traversal, and invalid characters in URLs.
- **Transport security guidance**: if TLS termination is expected to reside upstream, document the recommended deployment topology and known limitations.

## Testing & Quality
- **Stress & load testing**: build automated load scenarios to validate throughput, latency, and stability under high concurrency.
- **Chaos/fault injection**: design tests that simulate abrupt disconnects, malformed requests, and handler exceptions.
- **Benchmarking**: publish baseline benchmarks (local + networked) to set expectations and guide future optimizations.
- **Compatibility matrix**: validate behaviour across Windows/Linux/macOS, various .NET runtimes, and containerized environments.

## Documentation & Samples
- **Clarify unsupported features**: maintain a canonical list of intentional omissions (e.g., no built-in static file hosting) with rationale and alternatives.
- **Production deployment guide**: provide guidance for running behind reverse proxies, containerizing the host, configuring systemd/Windows services, and monitoring.
- **Sample projects**: add samples showing integration with dependency injection, middleware, HTTPS, and logging.
- **Upgrade playbook**: document breaking changes, migration paths, and how to opt into advanced features progressively.

## Delivery Strategy
1. Publish the current limitations in README/Agents.md so consumers understand today’s scope.
2. Prioritize lifecycle and error-handling improvements to safeguard existing scenarios.
3. Layer in routing enhancements and middleware/DI hooks with opt-in packages or extension methods.
4. Invest in observability, security hardening, and documentation to support production adoption.
5. Keep the core package lean—consider satellite packages (e.g., `FluentHosting.Extensions.DependencyInjection`) for optional features.

By following this roadmap, FluentHosting can remain true to its lightweight roots while offering a clear, supported path for teams that need a reliable, embeddable HTTP host in production environments.
