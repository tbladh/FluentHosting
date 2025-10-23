# FluentHosting Agent Notes

## Purpose
`FluentHosting` is a minimalist wrapper around `HttpListener` that targets .NET 8 (`FluentHosting/FluentHosting.csproj`). It exposes a fluent API for registering handlers and returning lightweight response objects without introducing additional dependencies.

## Core Types
- `FluentHost` (`FluentHosting/FluentHost.cs`): manages listener lifecycle, keeps a `List<IRouteHandler>` and dispatches incoming requests. `Start()` registers the appropriate prefix (`{Name}:{Port}/`) and begins the asynchronous accept loop; `Stop()` halts the listener but does not dispose it.
- `RouteHandler` (`FluentHosting/RouteHandler.cs`): immutable record of a route pattern, allowed verbs, handler delegate, and optional `CorsConfig`.
- `IRouteHandler` (`FluentHosting/IRouteHandler.cs`) / `Verb` (`FluentHosting/Verb.cs`): contract and bit flag enum covering `GET`, `PUT`, `POST`, `DELETE`, and `OPTIONS`. `Verb.All` combines every literal verb.

## Routing Behaviour
- Matching is performed in `FluentHost.GetContextCallback`. Routes are checked in registration order.
- `IsRouteMatch` allows either an exact, case-insensitive match or a prefix match when the handler route ends with `*` (currently the only wildcard form).
- If no route matches, a handler with route `"*"` is treated as the fallback for the verb. Otherwise the host returns a bare `404`.
- `FluentHostExtensions.Handles` (`FluentHosting/FluentHostExtensions.cs`) registers handlers and, when a `CorsConfig` is supplied, automatically adds an `OPTIONS` preflight handler for the same route.

## Response Helpers
- `HandlerResponse` (`FluentHosting/HandlerResponse.cs`) implements `IHandlerResponse` with mutable properties consumed when writing to the `HttpListenerResponse`.
- `StringResponse` (`FluentHosting/StringResponse.cs`) builds plain-text responses (custom encoding and content type supported).
- `JsonResponse<T>` (`FluentHosting/JsonResponse.cs`) serializes payloads with `System.Text.Json` using camelCase naming and ignores nulls.

## CORS Support
- `CorsConfig` (`FluentHosting/CorsConfig.cs`) stores allowed origins, verbs, headers, and max age. `CorsConfig.AllowAll` is a convenience instance using `Verb.All` and wildcard origins/headers.
- When a request carries an `Origin` header and the matched handler has a `CorsConfig`, `FluentHost` writes the headers returned from `CorsConfig.ToHeaders`. Preflight requests share the same config via the auto-generated handler.

## Utility Extensions
- `PathToContentType` converts common extensions to MIME types; it is unused in the current codebase but available for consumers.
- `ToVerb`, `GetFlagsString`, `GetFlags`, and `ParseFlags<T>` simplify converting between HTTP method strings and the `Verb` flags.

## Current Limitations
- No dynamic route parameters or template parsing (only literal paths and suffix `*` wildcards).
- No static file middleware—consumers must implement their own handler using the provided primitives.
- Listener disposal and error handling are minimal; callers are responsible for wrapping start/stop calls appropriately.
