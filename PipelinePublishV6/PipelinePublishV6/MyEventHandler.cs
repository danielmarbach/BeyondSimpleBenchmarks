using System;
using System.Threading.Tasks;
using NServiceBus;

namespace PipelinePublishV6
{
    public class MyEventHandler : IHandleMessages<MyEvent>
    {
        public Task Handle(MyEvent message, IMessageHandlerContext context)
        {
            Console.WriteLine("Event received");
            return Task.CompletedTask;
        }
    }
}