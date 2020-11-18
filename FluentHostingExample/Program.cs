using System;
using FluentHosting;

namespace FluentHostingExample
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = new FluentHost("http://localhost", 1234)
                //.Handles("/", Verb.Get, context => "Welcome!")
                .Handles("/hello/world", Verb.Get, context => new StringResponse("Hello World!"))
                .Handles("/goodbye", Verb.Get, context => new StringResponse("Good Bye!"))
                .Handles("*", Verb.All, (context) => new StringResponse($"No handler found for the route {context?.Request?.Url?.LocalPath}.", 404))
                .Handles("/json", Verb.Get, context => new JsonResponse<Foo>(new Foo { Bar = "Bar follows Foo.", Baz = "Baz is third." }))
                .Start();

            Console.WriteLine($"FluentHost serving at '{host.Name}:{host.Port}'.\n{host.Handlers.Count} handlers standing by.\n\nPress any key to terminate...");
            
            Console.ReadLine();

            host.Stop();
        }
    }

    public class Foo
    {
        public string Bar { get; set; }
        public string Baz { get; set; }
    }

}
