using System;
using System.Threading.Tasks;
using MassTransit;

namespace Jobba.Web.Sample;

public class SimpleMessage
{
    public string Name { get; set; }
}

public class SimpleConsumer : IConsumer<SimpleMessage>
{
    public Task Consume(ConsumeContext<SimpleMessage> context)
    {
        Console.WriteLine($"Received: {context.Message.Name}");
        return Task.CompletedTask;
    }
}
