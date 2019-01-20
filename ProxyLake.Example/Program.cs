using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProxyLake.Http;
using ProxyLake.Http.DependencyInjection;

namespace ProxyLake.Example
{
    
    
    class TestDefinitionProvider : IHttpProxyDefinitionProvider
    {
        public IEnumerable<HttpProxyDefinition> GetProxyDefinitions(string name)
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
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            var serviceCollection = new ServiceCollection();

            serviceCollection
                .AddLogging(o => o.AddConsole().SetMinimumLevel(LogLevel.Debug))
                .AddHttpProxyClient<TestDefinitionProvider>();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var httpProxyFactory = serviceProvider.GetService<IHttpProxyClientFactory>();
            Action action = () =>
            {
                while (true)
                {
                    using (var client = httpProxyFactory.CreateClient())
                    {
                        var result = client.GetAsync("https://api.ipify.org?format=json")
                            .GetAwaiter().GetResult()
                            .Content.ReadAsStringAsync().GetAwaiter().GetResult();

                        Console.WriteLine($"Answer: {result}");
                    }
                    
                    Thread.Sleep(TimeSpan.FromSeconds(30));
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