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
    class PingHealthCheck : IHttpProxyHealthCheckFeature
    {
        private readonly ILogger _logger;

        public PingHealthCheck(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(typeof(PingHealthCheck));
        }
        
        public async Task<bool> IsAliveAsync(IHttpProxy proxy, CancellationToken cancellation)
        {
            using (var ping = new Ping())
            {
                var isOk = false;

                try
                {
                    var uriBuilder = new UriBuilder(proxy.Address);
                    if (!proxy.Address.IsDefaultPort)
                        uriBuilder.Port = -1;
                    
                    var reply = await ping.SendPingAsync(uriBuilder.Uri.Host, 2000);
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

    class TestProxyRefill : IHttpProxyRefill
    {
        /// <inheritdoc />
        public async Task<IReadOnlyCollection<HttpProxyDefinition>> GetProxiesAsync(
            IReadOnlyCollection<IHttpProxy> aliveProxies, int maxProxiesCount, CancellationToken cancellation)
        {
            await Task.CompletedTask;
            
            return new[]
            {
                new HttpProxyDefinition
                {
                    Host = "http://91.186.120.116",
                    Port = 52173
                },
                new HttpProxyDefinition
                {
                    Host = "http://222.252.15.114",
                    Port = 56277
                },                
                new HttpProxyDefinition
                {
                    Host = "http://68.183.228.133",
                    Port = 8080
                }
            };
        }
    }
    
    class TestDefinitionProvider : IHttpProxyDefinitionFactory
    {
        public async Task<IReadOnlyCollection<HttpProxyDefinition>> CreateDefinitionsAsync(string name, CancellationToken cancellation)
        {
            await Task.CompletedTask;
            
            return new[]
            {
                new HttpProxyDefinition
                {
                    Host = "http://35.237.231.146",
                    Port = 80
                },
                new HttpProxyDefinition
                {
                    Host = "http://13.230.73.61",
                    Port = 82
                },                
                new HttpProxyDefinition
                {
                    Host = "http://212.101.74.68",
                    Port = 443
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
                .UseHealthCheck<PingHealthCheck>(period: TimeSpan.FromSeconds(15))
                .UseRefill<TestProxyRefill>(TimeSpan.FromSeconds(15), minProxyCountInclusive: 7, 10);

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var logger = serviceProvider.GetService<ILoggerFactory>()
                .CreateLogger("HTTP CLIENT LOG");
            
            var httpProxyFactory = serviceProvider.GetService<IHttpProxyClientFactory>();
            
            Func<Task> action = async () =>
            {
                while (true)
                {
                    try
                    {
                        var creationSw = new Stopwatch();
                        var requestSw = new Stopwatch();
                        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
                        {
                            creationSw.Start();
                            
                            using (var client = await httpProxyFactory.CreateDefaultClientAsync(cts.Token))
                            {
                                creationSw.Stop();
                                requestSw.Start();
                                
                                var result = client.GetAsync("https://api.ipify.org?format=json", cts.Token)
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
            

            var tasks = new[]
            {
                Task.Run(action),
                Task.Run(action), 
            };

            await Task.WhenAll(tasks);
        }
    }
}