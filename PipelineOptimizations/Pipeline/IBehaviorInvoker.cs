namespace NServiceBus.Pipeline;

interface IBehaviorInvoker
{
    Task Invoke(object behavior, IBehaviorContext context, Func<IBehaviorContext, Task> next);
}