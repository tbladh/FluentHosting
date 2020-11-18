# FluentHost

FluentHost is a trivial fluently composable web host based on HttpListener intended for development and testing.

## Example

	var host = new FluentHost("http://localhost", 1337)
				.Handles("/hello/world", Verb.Get, context => new StringResponse("Hello World!"))
				.Start();
	Console.ReadLine();
	host.Stop();

## Roadmap
- Authentication Support
- Improved Cors support
- More response types (currently string, json, and the generic HostResponse)
- Support for global settings (json serialization, authentication, etc.)
- Better support for wildcards and parameters (e.g. '/foo/{id}?bar={option}') in routes.
- Wrapper for HttpListenerContext with some form of automatic wiring for query and uri parameters.
- Global pre- and post processing for handlers.