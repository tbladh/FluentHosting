using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;

namespace FluentHosting.IntegrationTests
{
    public class FluentHostIntegrationTests
    {
        private const string HostName = "http://localhost";

        [Fact]
        public async Task Handles_get_route_and_repeated_requests()
        {
            await RunHostTestAsync(
                host => host.Handles("/", Verb.Get, _ => new StringResponse("Hello World!")),
                async client =>
                {
                    var first = await client.GetStringAsync(string.Empty);
                    Assert.Equal("Hello World!", first);

                    var second = await client.GetStringAsync(string.Empty);
                    Assert.Equal("Hello World!", second);
                });
        }

        [Fact]
        public async Task Handles_delete_route_and_returns_no_content()
        {
            await RunHostTestAsync(
                host => host.Handles("/items/1", Verb.Delete, _ => new StringResponse(string.Empty, 204)),
                async client =>
                {
                    var response = await client.DeleteAsync("items/1");
                    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
                    var payload = await response.Content.ReadAsByteArrayAsync();
                    Assert.Empty(payload);
                });
        }

        [Fact]
        public async Task Most_recent_handler_runs_first_for_same_route()
        {
            await RunHostTestAsync(
                host => host
                    .Handles("/greeting", Verb.Get, _ => new StringResponse("first"))
                    .Handles("/greeting", Verb.Get, _ => new StringResponse("second")),
                async client =>
                {
                    var response = await client.GetStringAsync("greeting");
                    Assert.Equal("second", response);
                });
        }

        [Fact]
        public async Task Unhandled_route_without_default_returns_404()
        {
            await RunHostTestAsync(
                host => host.Handles("/known", Verb.Get, _ => new StringResponse("known")),
                async client =>
                {
                    var response = await client.GetAsync("unknown");
                    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
                });
        }

        [Fact]
        public async Task Default_wildcard_handler_serves_custom_fallback()
        {
            await RunHostTestAsync(
                host => host.Handles("*", Verb.Get, _ => new StringResponse("fallback", 404)),
                async client =>
                {
                    var response = await client.GetAsync("not-there");
                    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
                    var content = await response.Content.ReadAsStringAsync();
                    Assert.Equal("fallback", content);
                });
        }

        [Fact]
        public async Task Wildcard_route_matches_variable_segments()
        {
            await RunHostTestAsync(
                host => host
                    .Handles("/products/*", Verb.Get, context =>
                    {
                        var productId = context.Request.Url.LocalPath.Split('/').Last();
                        return new StringResponse($"product-{productId}");
                    }),
                async client =>
                {
                    var response = await client.GetStringAsync("products/123");
                    Assert.Equal("product-123", response);
                });
        }

        [Fact]
        public async Task Json_response_serializes_payload_with_expected_headers()
        {
            await RunHostTestAsync(
                host => host.Handles("/widgets/42", Verb.Get, _ => new JsonResponse<WidgetDto>(new WidgetDto("42", "example"))),
                async client =>
                {
                    var response = await client.GetAsync("widgets/42");
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
                    var payload = await response.Content.ReadAsStringAsync();
                    Assert.Equal("{\"id\":\"42\",\"name\":\"example\"}", payload);
                });
        }

        [Fact]
        public async Task Cors_preflight_allows_configured_origin()
        {
            var cors = new CorsConfig(new[] { "https://allowed.test" }, Verb.Get, new[] { "content-type" });

            await RunHostTestAsync(
                host => host.Handles("/cors", Verb.Get, _ => new StringResponse("ok"), cors),
                async client =>
                {
                    using var request = new HttpRequestMessage(HttpMethod.Options, "cors");
                    request.Headers.Add("Origin", "https://allowed.test");
                    request.Headers.Add("Access-Control-Request-Method", "GET");

                    var response = await client.SendAsync(request);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Equal("https://allowed.test", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
                    Assert.Contains("GET", response.Headers.GetValues("Access-Control-Allow-Methods").Single());
                    Assert.Equal("content-type", response.Headers.GetValues("Access-Control-Allow-Headers").Single());
                    Assert.Equal("86400", response.Headers.GetValues("Access-Control-Max-Age").Single());
                });
        }

        [Fact]
        public async Task Cors_preflight_rejects_disallowed_origin()
        {
            var cors = new CorsConfig(new[] { "https://allowed.test" }, Verb.Get, new[] { "content-type" });

            await RunHostTestAsync(
                host => host.Handles("/cors", Verb.Get, _ => new StringResponse("ok"), cors),
                async client =>
                {
                    using var request = new HttpRequestMessage(HttpMethod.Options, "cors");
                    request.Headers.Add("Origin", "https://blocked.test");
                    request.Headers.Add("Access-Control-Request-Method", "GET");

                    var response = await client.SendAsync(request);
                    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                    Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
                });
        }

        [Fact]
        public async Task Start_async_stop_async_cycle_restarts_listener_on_same_port()
        {
            var port = GetAvailablePort();
            await using var host = new FluentHost(HostName, port)
                .Handles("/", Verb.Get, _ => new StringResponse("cycle"));

            await host.StartAsync();
            await AssertResponseAsync(port, "cycle");

            await host.StopAsync();

            await host.StartAsync();
            await AssertResponseAsync(port, "cycle");

            await host.StopAsync();
        }

        [Fact]
        public async Task Starting_host_twice_without_stopping_throws_invalid_operation()
        {
            var port = GetAvailablePort();
            await using var host = new FluentHost(HostName, port)
                .Handles("/", Verb.Get, _ => new StringResponse("cycle"));

            await host.StartAsync();
            await Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync());
            await host.StopAsync();
        }

        [Fact]
        public async Task Stop_async_is_idempotent()
        {
            var port = GetAvailablePort();
            await using var host = new FluentHost(HostName, port)
                .Handles("/", Verb.Get, _ => new StringResponse("cycle"));

            await host.StartAsync();
            await host.StopAsync();
            await host.StopAsync();
        }

        [Fact]
        public async Task Handler_exception_returns_internal_server_error_and_keeps_listening()
        {
            await RunHostTestAsync(
                host => host
                    .Handles("/healthy", Verb.Get, _ => new StringResponse("ok"))
                    .Handles("/boom", Verb.Get, _ => throw new InvalidOperationException("boom")),
                async client =>
                {
                    var failing = await client.GetAsync("boom");
                    Assert.Equal(HttpStatusCode.InternalServerError, failing.StatusCode);
                    var payload = await failing.Content.ReadAsByteArrayAsync();
                    Assert.Empty(payload);

                    var healthy = await client.GetStringAsync("healthy");
                    Assert.Equal("ok", healthy);
                });
        }

        private static async Task RunHostTestAsync(
            Func<FluentHost, FluentHost> configureHost,
            Func<HttpClient, Task> testBody)
        {
            var port = GetAvailablePort();
            var baseAddress = new Uri($"{HostName}:{port}/");
            var host = configureHost(new FluentHost(HostName, port)).Start();

            try
            {
                using var client = new HttpClient { BaseAddress = baseAddress };
                client.DefaultRequestHeaders.ConnectionClose = true;
                await testBody(client);
            }
            finally
            {
                host.Stop();
            }
        }

        private static int GetAvailablePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            try
            {
                listener.Start();
                return ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
        }

        private static async Task AssertResponseAsync(int port, string expectedBody)
        {
            using var client = CreateClient(port);
            var body = await client.GetStringAsync(string.Empty);
            Assert.Equal(expectedBody, body);
        }

        private static HttpClient CreateClient(int port)
        {
            var baseAddress = new Uri($"{HostName}:{port}/");
            var client = new HttpClient { BaseAddress = baseAddress };
            client.DefaultRequestHeaders.ConnectionClose = true;
            return client;
        }

        private sealed record WidgetDto(string Id, string Name);
    }
}
