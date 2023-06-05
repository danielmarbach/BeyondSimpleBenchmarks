namespace NServiceBus.Pipeline;

public abstract class Behavior<TContext> : IBehavior<TContext, TContext> where TContext : IBehaviorContext
{
    public Task Invoke(TContext context, Func<TContext, Task> next)
    {
        return Invoke(context, () => next(context));
    }

    public abstract Task Invoke(TContext context, Func<Task> next);
}