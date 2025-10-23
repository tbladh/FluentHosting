# FluentHost

FluentHost is a lightweight wrapper around `HttpListener` that lets you compose HTTP endpoints with a fluent, dependency-free API. The library targets .NET 8 and is designed for quick local services, prototypes, and tests.

## Getting Started

```csharp
var host = new FluentHost("http://localhost", 1337)
    .Handles("/hello/world", Verb.Get, context => new StringResponse("Hello World!"))
    .Handles("*", Verb.All, context => new StringResponse("Route not found.", 404))
    .Start();

Console.ReadLine();
host.Stop();
```

- `Name` is the prefix (e.g. `http://localhost`)
- `Port` is appended to build the listener URL (e.g. `http://localhost:1337/`)
- `Handles` registers handlers so that the most recent match wins; a `"*"` route acts as the fallback for all verbs

## Features
- Fluent handler registration over `HttpListener` with last-write-wins semantics
- Built-in response helpers: `StringResponse` and `JsonResponse<T>`
- Optional per-route `CorsConfig` that also provisions `OPTIONS` preflight handling
- Simple wildcard support: routes ending with `*` match any suffix
- No external dependencies; only the .NET base class libraries

## Limitations to Know
- No route templates or parameter binding (only literal paths and suffix `*`)
- Static file serving is not included
- Listener lifecycle is manual; call `Stop()` when finished

## Documentation
- Repository overview: `Agents.md`
- Library details: `FluentHosting/Agents.md`
- Test suite notes: `FluentHosting.IntegrationTests/Agents.md`

## Running Tests

```
dotnet test FluentHosting.sln
```

Integration tests allocate a free loopback port per run, so the suite can execute in parallel without manual coordination.

## Future Improvements (Ideas)
- Additional response helpers (file streaming, binary payloads)
- Parameterised routes and query binding helpers
- Configurable logging or diagnostics hooks
- Graceful shutdown helpers and improved error handling
