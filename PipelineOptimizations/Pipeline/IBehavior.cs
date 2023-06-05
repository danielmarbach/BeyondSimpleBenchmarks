using NServiceBus.Pipeline;

namespace NServiceBus.Pipeline;

public interface IBehavior<in TInContext, out TOutContext> : IBehavior
    where TInContext : IBehaviorContext
    where TOutContext : IBehaviorContext
{
    Task Invoke(TInContext context, Func<TOutContext, Task> next);
}

public interface IBehavior
{
}