using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace ClusterSample
{
    class Program
    {
        static async Task Main(string[] args)
        {


            var host_01 = new WebHostBuilder()
                .ConfigureLogging(factory =>
                {
                    factory.AddConsole();
                    factory.AddFilter("Console", level => level >= LogLevel.Information);
                    factory.AddDebug();
                })
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .UseUrls(new[] { "http://+:5000" })
                .Build()
                .RunAsync();

            var host_02 = new WebHostBuilder()
                .ConfigureLogging(factory =>
                {
                    factory.AddConsole();
                    factory.AddFilter("Console", level => level >= LogLevel.Information);
                    factory.AddDebug();
                })
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .UseUrls(new[] { "http://+:5001" })
                .Build()
                .RunAsync();

            await Task.Delay(5000);

            var builder1 = new HubConnectionBuilder()
                .WithUrl(new Uri("http://localhost:5000/broadcast"), HttpTransportType.WebSockets)
                .AddMessagePackProtocol();

            var client1 = builder1.Build();
            client1.Closed += (ex) => client1.StartAsync();
            await client1.StartAsync();

            var builder2 = new HubConnectionBuilder()
                .WithUrl(new Uri("http://localhost:5001/broadcast"), HttpTransportType.WebSockets)
                .AddMessagePackProtocol();

            var client2 = builder2.Build();
            client2.Closed += (ex) => client2.StartAsync();          
            await client2.StartAsync();

            while (client1.State != HubConnectionState.Connected || client2.State != HubConnectionState.Connected)
                await Task.Delay(1000);

            await Task.Delay(1000);

            client1.On<string, string>("Message", (s, m) => {
                Console.WriteLine($"client1 got : \"{m}\" from {s}");
            });
            client2.On<string, string>("Message", (s, m) => {
                Console.WriteLine($"client2 got : \"{m}\" from {s}");
            });

            

            while (true)
            {
                await Task.Delay(1000);

                await client1.SendAsync("Broadcast", "client1", "Test Message");

                await Task.Delay(1000);


                await client2.SendAsync("Broadcast", "client2", "Test Message");

            }


            //await Task.WhenAny(host_01, host_02);
        }
    }
}
