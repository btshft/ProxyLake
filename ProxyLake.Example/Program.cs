using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProxyLake.Http;
using ProxyLake.Http.Abstractions;
using ProxyLake.Http.DependencyInjection;

namespace ProxyLake.Example
{
    class TestDefinitionProvider : IHttpProxyDefinitionProvider
    {
        public IEnumerable<HttpProxyDefinition> GetProxyDefinitions()
        {
            return new[]
            {
                new HttpProxyDefinition
                {
                    Host = "http://14.142.122.134",
                    Port = 8080
                },
                new HttpProxyDefinition
                {
                    Host = "http://14.142.122.134",
                    Port = 8080
                },                
                new HttpProxyDefinition
                {
                    Host = "http://14.142.122.134",
                    Port = 8080
                }
            };
        }
    }
    
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            var serviceCollection = new ServiceCollection();

            serviceCollection
                .AddLogging(o => o.AddConsole().SetMinimumLevel(LogLevel.Debug))
                .AddHttpProxyClient<TestDefinitionProvider>();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var httpProxyFactory = serviceProvider.GetService<IHttpProxyClientFactory>();
            var client = httpProxyFactory.CreateProxyClient();

            while (true)
            {
                var result = client.GetAsync("https://api.ipify.org?format=json")
                    .GetAwaiter().GetResult()
                    .Content.ReadAsStringAsync().GetAwaiter().GetResult();
            
                Console.WriteLine($"Answer: {result}");
                
                Thread.Sleep(TimeSpan.FromSeconds(45));
            }
            
            client.Dispose();
        }
    }
}