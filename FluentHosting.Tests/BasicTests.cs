using System;
using System.Net;
using System.Threading;
using Xunit;

namespace FluentHosting.Tests
{
    public class BasicTests
    {
        public const string ApiUrl = "http://localhost";
        public const int ApiPort = 1337;

        public string BaseUrl => $"{ApiUrl}:{ApiPort}/";

        [Fact]
        public void ComposingAnApi_With_OneHandler_ReturningHelloWorld_ShouldReturn_HelloWorld()
        {
            var host = new FluentHost(ApiUrl, ApiPort)
                .Handles("/", Verb.Get, context => new StringResponse("Hello World!"))
                .Start();

            var client = new WebClient();
            var data = client.DownloadString(BaseUrl);
            Assert.Equal("Hello World!", data);
            Thread.Sleep(500);
            data = client.DownloadString(BaseUrl);
            Assert.Equal("Hello World!", data);

            host.Stop();
        }

    }
}
