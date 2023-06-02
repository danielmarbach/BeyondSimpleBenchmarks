using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus;

namespace PipelinePublishV6
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            var endpointConfiguration = new EndpointConfiguration("PublishSample");
            endpointConfiguration.UseSerialization<JsonSerializer>();
            var transport = endpointConfiguration.UseTransport<MsmqTransport>();
            transport.Routing().RegisterPublisher(typeof(MyEvent), "PublishSample");
            endpointConfiguration.UsePersistence<InMemoryPersistence>();
            endpointConfiguration.EnableInstallers();
            endpointConfiguration.SendFailedMessagesTo("error");
           
            var endpointInstance = await Endpoint.Start(endpointConfiguration);

            Console.WriteLine("Attach the profiler and hit <enter>.");
            Console.ReadLine();

            var tasks = new List<Task>(1000);
            for (int i = 0; i < 1000; i++)
            {
                tasks.Add(endpointInstance.Publish(new MyEvent()));
            }
            await Task.WhenAll(tasks);
            
            Console.WriteLine("Publish 1000 done. Get a snapshot");
            Console.ReadLine();
            
            tasks = new List<Task>(10000);
            for (int i = 0; i < 10000; i++)
            {
                tasks.Add(endpointInstance.Publish(new MyEvent()));
            }
            await Task.WhenAll(tasks);
            
            Console.WriteLine("Publish done. Hit <enter> to shut down.");
            Console.ReadLine();

            await endpointInstance.Stop();
        }
    }
}