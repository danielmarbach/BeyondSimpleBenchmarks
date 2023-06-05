namespace NServiceBus.Pipeline;

public abstract class StageConnector<TFromContext, TToContext> : IBehavior<TFromContext, TToContext>,
    IStageConnector
    where TFromContext : IBehaviorContext
    where TToContext : IBehaviorContext
{
    public abstract Task Invoke(TFromContext context, Func<TToContext, Task> stage);
}