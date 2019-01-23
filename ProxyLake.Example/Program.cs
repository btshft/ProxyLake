using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProxyLake.Http;
using ProxyLake.Http.DependencyInjection;
using ProxyLake.Http.Extensions;
using ProxyLake.Http.Features;

namespace ProxyLake.Example
{
    class NullHealthCheck : IHttpProxyHealthCheckFeature
    {
        private readonly ILogger _logger;

        public NullHealthCheck(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(typeof(NullHealthCheck));
        }

        public bool IsAlive(IHttpProxy proxy, CancellationToken cancellation)
        {
            _logger.LogInformation("Null health check called");
            return true;
        }
    }
    
    class PingHealthCheck : IHttpProxyHealthCheckFeature
    {
        private readonly ILogger _logger;

        public PingHealthCheck(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(typeof(PingHealthCheck));
        }
        
        public bool IsAlive(IHttpProxy proxy, CancellationToken cancellation)
        {
            using (var ping = new Ping())
            {
                var isOk = false;

                try
                {
                    var uriBuilder = new UriBuilder(proxy.Address);
                    if (!proxy.Address.IsDefaultPort)
                        uriBuilder.Port = -1;
                    
                    var reply = ping.Send(uriBuilder.Uri.Host, 2000);
                    isOk = reply != null && reply.Status == IPStatus.Success;
                }
                catch
                {
                    isOk = false;
                }
                finally
                {
                    _logger.LogInformation($"Ping health check '{proxy.Address}': {(isOk ? "passed" : "failed")}");
                }

                return isOk;
            }
        }
    }
    
    class TestDefinitionProvider : IHttpProxyDefinitionFactory
    {
        public IReadOnlyCollection<HttpProxyDefinition> CreateDefinitions(string name, CancellationToken cancellation)
        {
            return new[]
            {
                new HttpProxyDefinition
                {
                    Host = "http://167.99.55.84",
                    Port = 80
                },
                new HttpProxyDefinition
                {
                    Host = "http://87.248.171.168",
                    Port = 44576
                },                
                new HttpProxyDefinition
                {
                    Host = "http://167.99.59.250",
                    Port = 80
                }
            };
        }
    }
    
    class Program
    {       
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            var serviceCollection = new ServiceCollection();

            serviceCollection
                .AddLogging(o => o.AddConsole().SetMinimumLevel(LogLevel.Debug))
                .AddDefaultHttpProxyClient<TestDefinitionProvider>()
                .UseHealthCheck<PingHealthCheck>(period: TimeSpan.FromSeconds(15));

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var logger = serviceProvider.GetService<ILoggerFactory>()
                .CreateLogger("HTTP CLIENT LOG");
            
            var httpProxyFactory = serviceProvider.GetService<IHttpProxyClientFactory>();
            
            Action action = () =>
            {
                while (true)
                {
                    try
                    {
                        var creationSw = new Stopwatch();
                        var requestSw = new Stopwatch();
                        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                        {
                            creationSw.Start();
                            
                            using (var client = httpProxyFactory.CreateDefaultClient(cts.Token))
                            {
                                creationSw.Stop();
                                requestSw.Start();
                                
                                var result = client.GetAsync("https://api.ipify.org?format=json")
                                    .GetAwaiter().GetResult()
                                    .Content.ReadAsStringAsync().GetAwaiter().GetResult();

                                requestSw.Stop();
                                
                                Console.WriteLine($"Answer: {result}");
                                Console.WriteLine(
                                    $"Client acquire took: {creationSw.ElapsedMilliseconds} ms | " +
                                    $"Request took: {requestSw.ElapsedMilliseconds} ms");
                                
                                creationSw.Reset();
                                requestSw.Reset();
                            }
                        }

                        Thread.Sleep(TimeSpan.FromSeconds(20));
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "client1 failed");
                    }
                }
            };
            

            var threads = new[]
            {
                new Thread(new ThreadStart(action)),
                new Thread(new ThreadStart(action)),
            };

            foreach (var thread in threads)
            {
                thread.Start();
            }
        }
    }
}