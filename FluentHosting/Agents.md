# FluentHosting Agent Notes

## Purpose
`FluentHosting` is a minimalist wrapper around `HttpListener` that targets .NET 8 (`FluentHosting/FluentHosting.csproj`). It exposes a fluent API for registering handlers and returning lightweight response objects without introducing additional dependencies.

## Core Types
- `FluentHost` (`FluentHosting/FluentHost.cs`): manages listener lifecycle with synchronous (`Start`/`Stop`) and asynchronous (`StartAsync`/`StopAsync`) APIs, keeps a thread-safe handler list, and dispatches incoming requests via an async accept loop. Disposing (sync or async) stops the listener and frees resources.
- `RouteHandler` (`FluentHosting/RouteHandler.cs`): immutable record of a route pattern, allowed verbs, handler delegate, and optional `CorsConfig`.
- `IRouteHandler` (`FluentHosting/IRouteHandler.cs`) / `Verb` (`FluentHosting/Verb.cs`): contract and bit flag enum covering `GET`, `PUT`, `POST`, `DELETE`, and `OPTIONS`. `Verb.All` combines every literal verb.

## Routing Behaviour
- Matching is performed inside `FluentHost`'s async loop. Non-fallback handlers are evaluated in LIFO order so the most recently registered handler wins for a route, while `"*"` fallback handlers are always evaluated last.
- `IsRouteMatch` allows either an exact, case-insensitive match or a prefix match when the handler route ends with `*` (currently the only wildcard form).
- If no route matches, a handler with route `"*"` is treated as the fallback for the verb. Otherwise the host returns a bare `404`.
- `FluentHostExtensions.Handles` (`FluentHosting/FluentHostExtensions.cs`) delegates to `RegisterHandler`, preserving the fallback ordering and, when a `CorsConfig` is supplied, automatically adds an `OPTIONS` preflight handler for the same route.

## Response Helpers
- `HandlerResponse` (`FluentHosting/HandlerResponse.cs`) implements `IHandlerResponse` with mutable properties consumed when writing to the `HttpListenerResponse`.
- `StringResponse` (`FluentHosting/StringResponse.cs`) builds plain-text responses (custom encoding and content type supported).
- `JsonResponse<T>` (`FluentHosting/JsonResponse.cs`) serializes payloads with `System.Text.Json` using camelCase naming and ignores nulls.

## CORS Support
- `CorsConfig` (`FluentHosting/CorsConfig.cs`) stores allowed origins, verbs, headers, and max age. `CorsConfig.AllowAll` is a convenience instance using `Verb.All` and wildcard origins/headers.
- When a request carries an `Origin` header and the matched handler has a `CorsConfig`, `FluentHost` writes the headers returned from `CorsConfig.ToHeaders`. Preflight requests share the same config via the auto-generated handler. Missing or disallowed origins produce a `400` preflight response, and handler exceptions translate to `500` responses without terminating the listener loop.

## Utility Extensions
- `PathToContentType` converts common extensions to MIME types; it is unused in the current codebase but available for consumers.
- `ToVerb`, `GetFlagsString`, `GetFlags`, and `ParseFlags<T>` simplify converting between HTTP method strings and the `Verb` flags.

## Current Limitations
- No dynamic route parameters or template parsing (only literal paths and suffix `*` wildcards).
- No static file middleware; consumers must implement their own handler using the provided primitives.
- HTTPS support, request timeouts/limits, and structured diagnostics still rely on host applications to layer additional infrastructure.
