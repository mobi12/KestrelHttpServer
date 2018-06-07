using System;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Http2SampleApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var hostBuilder = new WebHostBuilder()
                .ConfigureLogging((_, factory) =>
                {
                    // Set logging to the MAX.
                    factory.SetMinimumLevel(LogLevel.Trace);
                    factory.AddConsole();
                })
                .UseKestrel((context, options) =>
                {
                    var basePort = context.Configuration.GetValue<int?>("BASE_PORT") ?? 5000;

                    // Run callbacks on the transport thread
                    options.ApplicationSchedulingMode = SchedulingMode.Inline;

                    // Http/1.1 endpoint for comparison
                    options.Listen(IPAddress.Any, basePort, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http1;
                        listenOptions.UseConnectionLogging();
                    });

                    // TLS Http/1.1 or HTTP/2 endpoint negotiated via ALPN
                    options.Listen(IPAddress.Any, basePort + 1, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                        listenOptions.UseHttps("testCert.pfx", "testPassword");
                        listenOptions.UseConnectionLogging();
                    });

                    // Prior knowledge, no TLS handshake. WARNING: Not supported by browsers
                    // but useful for the h2spec tests
                    options.Listen(IPAddress.Any, basePort + 5, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http2;
                        listenOptions.UseConnectionLogging();
                    });
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>();

            hostBuilder.Build().Run();
        }
    }
}
