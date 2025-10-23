# FluentHosting.Tests Agent Notes

## Purpose
`FluentHosting.Tests` provides xUnit coverage over the public surface of the `FluentHosting` library (`FluentHosting.Tests/FluentHosting.Tests.csproj`). Tests launch a real `FluentHost` instance on localhost to verify end-to-end behaviour.

## Current Test Suite
- `BasicTests` (`FluentHosting.Tests/BasicTests.cs`) contains two facts:
  - `ComposingAnApi_With_OneHandler_ReturningHelloWorld_ShouldReturn_HelloWorld` spins up a host with a single `GET /` route and asserts that repeated `WebClient` requests return the expected body.
  - `ComposingAnApi_With_OneHandler_AcceptingDelete_ShouldReturn_204_And_Empty_Body` registers a `DELETE /items/1` handler returning a `StringResponse` with status 204 and validates the status code using `HttpClient`.

## Runtime Considerations
- Tests rely on port `1337` and perform actual HTTP calls; avoid running them in parallel with other processes bound to the same port.
- Each test calls `host.Stop()` to release the listener, but there is no further cleanup—failures might leave the port occupied until the process exits.
- Network assertions use real timing (`Thread.Sleep(500)` in the GET test) to give the listener time to process sequential requests.

## How to Run
Execute `dotnet test FluentHosting.Tests` from the repository root. No additional tooling or environment setup is required beyond .NET 8.
